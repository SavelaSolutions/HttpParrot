using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HttpParrot
{
    /// <summary>
    /// DelegatingHandler that can record and/or replay responses, depending on parameter passed to constructor.
    /// Used for generating http responses from real systems, to be used as mock data for the corresponding requests in tests.
    /// </summary>
    /// <example>
    /// In ASP.NET Core application using the IHttpClientFactory:s "typed client":
    /// <code>
    /// services.AddHttpClient&lt;IApiClient, ApiClient&gt;().AddHttpMessageHandler(serviceProvider =>
    /// {
    ///     // The below path to the cache directory is normally what's needed to put the cache in the project folder and not in the build output.
    ///     return new RecordAndReplayEnabledMessageHandler(new RecordAndRelayOptions
    ///     {
    ///         Mode = RecordAndReplayMode.RecordAndReplay,
    ///         RelativeCacheDirectoryPath = @"..\..\..\RecordReplayCache",
    ///         IdentityProvider = identityProvider // Custom implementation of IRecordAndReplayIdentityProvider, if needed
    ///     });
    /// });
    /// </code>
    /// If you want to add handlers too all clients generated from the IHttpClientFactory you can instead use
    /// <see cref="RecordAndReplayEnabledMessageHandlerExtensions.AddRecordAndReplayEnabledMessageHandlerToDefaultHttpClientFactory"/>.
    /// </example>
    public class RecordAndReplayEnabledMessageHandler : DelegatingHandler
    {
        private readonly IRecordAndReplayIdentityProvider _identityProvider;
        private static readonly SemaphoreSlim FileSemaphore = new SemaphoreSlim(1, 1);
        private readonly SHA256 _sha256 = SHA256.Create();
        
        public string RelativeCacheDirectoryPath { get; }
        public string AbsoluteCacheDirectoryPath =>
            Path.Combine(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).FullName, RelativeCacheDirectoryPath);
        public RecordAndReplayMode Mode { get; }
        // TODO: Rename
        public bool IncludeQueryParameters { get; }
        // TODO: Rename
        public bool IncludeBody { get; }

        /// <summary>
        /// Creates a DelegatingHandler that will record and replay requests going through it. The mode parameter is used to define the behavior.
        /// </summary>
        /// <param name="options">Options for this message handler.</param>
        public RecordAndReplayEnabledMessageHandler(RecordAndRelayOptions options)
        {
            _identityProvider = options.IdentityProvider ?? new NoIdentity();
            Mode = options.Mode;
            RelativeCacheDirectoryPath = options.RelativeCacheDirectoryPath?.Replace('\\', Path.DirectorySeparatorChar) ?? "cache";
            IncludeQueryParameters = options.IncludeQueryParametersWhenMatchingResponse;
            IncludeBody = options.IncludeBodyWhenMatchingResponse;
        }

        private void EnsureCacheDirectoryExists()
        {
            if (Directory.Exists(AbsoluteCacheDirectoryPath)) return;
            Directory.CreateDirectory(AbsoluteCacheDirectoryPath);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                await FileSemaphore.WaitAsync(cancellationToken);
                EnsureCacheDirectoryExists();
                switch (Mode)
                {
                    case RecordAndReplayMode.Passthrough:
                        return await base.SendAsync(request, cancellationToken);
                    case RecordAndReplayMode.RecordAndReplay:
                        return await RecordAndOrReplaySendAsync(request, cancellationToken);
                    case RecordAndReplayMode.ReplayOnly:
                        return await ReplayResponseAsync(request);
                    case RecordAndReplayMode.RecordOnly:
                        return await SendAndRecordAsync(request, cancellationToken);
                    default:
                        throw new ArgumentOutOfRangeException("Mode");
                }
            }
            finally
            {
                FileSemaphore.Release();
            }
        }

        private async Task<HttpResponseMessage> RecordAndOrReplaySendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var responseStreamAsync = await GetRecordedResponseStreamAsync(request);
            if (responseStreamAsync == null)
                return await SendAndRecordAsync(request, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(responseStreamAsync)
            };
        }

        private async Task<HttpResponseMessage> SendAndRecordAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return response;
            var content = await SaveResponseAsync(request, response);
            return new HttpResponseMessage(response.StatusCode)
            {
                Content = new StreamContent(content),
                RequestMessage = request,
                Version = response.Version,
                ReasonPhrase = response.ReasonPhrase
            };
        }

        private static async Task<string> ExtractRequestContentAsync(HttpRequestMessage request)
        {
            var str = string.Empty;
            if (request.Content != null)
                str = await request.Content.ReadAsStringAsync().ConfigureAwait(false);
            return str;
        }

        private async Task<Stream> SaveResponseAsync(
            HttpRequestMessage request,
            HttpResponseMessage response)
        {
            var fileName = await GetFileName(response.RequestMessage.Method, response.RequestMessage.RequestUri, request);
            var fileNameWithPath = Path.Combine(AbsoluteCacheDirectoryPath, fileName);

            // Copy the single-read source stream contents
            var memoryStream = new MemoryStream();
            using (var contentStream = await response.Content.ReadAsStreamAsync())
            {
                await contentStream.CopyToAsync(memoryStream);
                memoryStream.Seek(0L, SeekOrigin.Begin);
            }

            // Read source stream and store to file
            var text = Encoding.UTF8.GetString(memoryStream.ToArray());
            try
            {
                // Attempt json prettify if text might be json
                if ((text.StartsWith("{") && text.EndsWith("}")) || (text.StartsWith("[") && text.EndsWith("]")))
                    text = text.NonDestructiveJsonPrettify();
            }
            catch
            {
                // ignored
            }

            using (var streamWriter = new StreamWriter(fileNameWithPath, false))
            {
                await streamWriter.WriteLineAsync(text);
                streamWriter.Close();
            }

            // Restore return stream to beginning to allow consuming code to read it
            memoryStream.Seek(0L, SeekOrigin.Begin);

            return memoryStream;
        }

        private async Task<HttpResponseMessage> ReplayResponseAsync(
            HttpRequestMessage request)
        {
            var responseStreamAsync = await GetRecordedResponseStreamAsync(request);
            if (responseStreamAsync == null)
                return new HttpResponseMessage(HttpStatusCode.NotFound)
                {
                    RequestMessage = request,
                    Version = request.Version,
                    Content = new StringContent("")
                };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(responseStreamAsync),
                RequestMessage = request
            };
        }

        private async Task<Stream> GetRecordedResponseStreamAsync(HttpRequestMessage request)
        {
            var fileName = await GetFileName(request.Method, request.RequestUri, request);
            var fileNameWithPath = Path.Combine(AbsoluteCacheDirectoryPath, fileName);
            if (!File.Exists(fileNameWithPath)) return null;

            using (var stream = File.OpenRead(fileNameWithPath))
            {
                var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Seek(0L, SeekOrigin.Begin);
                return memoryStream;
            }
        }

        private async Task<string> GetFileName(HttpMethod method, Uri requestUri, HttpRequestMessage request)
        {
            var body = await ExtractRequestContentAsync(request);
            var queryParameters = request.RequestUri.Query;
            var identity = _identityProvider.GetUserIdentifier();
            var hashInput = (IncludeBody ? body : "") + (IncludeQueryParameters ? queryParameters : "") + identity;
            var hash = Hash(hashInput);
            return $"{(requestUri.Host + requestUri.AbsolutePath).Replace('/', '-')}-{method}-{hash}.json";
        }

        private string Hash(string input)
        {
            var hash = _sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var stringBuilder = new StringBuilder();
            foreach (var num in hash)
                stringBuilder.Append(num.ToString("X2"));
            return stringBuilder.ToString();
        }
    }
}