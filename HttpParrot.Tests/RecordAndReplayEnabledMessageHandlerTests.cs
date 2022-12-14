using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace HttpParrot.Tests
{
    public class RecordAndReplayEnabledMessageHandlerTests
    {
        private static readonly string CacheLocation = "RecordedResponses";
        private static readonly string DataUrl = "http://echo.jsontest.com/foo/bar";
        private static readonly string DataUrlFilePath = $"{CacheLocation}/echo.jsontest.com-foo-bar-GET-E3B0C44298FC1C149AFBF4C8996FB92427AE41E4649B934CA495991B7852B855.json";

        public RecordAndReplayEnabledMessageHandlerTests()
        {
            if (Directory.Exists(CacheLocation)) Directory.Delete(CacheLocation, true);
        }
        
        [Theory]
        [InlineData(RecordAndReplayMode.RecordOnly)]
        [InlineData(RecordAndReplayMode.RecordAndReplay)]
        public async Task ShouldRecordResponse_WhenUsingRecordModes(RecordAndReplayMode mode)
        {
            // Arrange
            var options = new RecordAndRelayOptions { Mode = mode, RelativeCacheDirectoryPath = CacheLocation };
            var client = new HttpClient(new RecordAndReplayEnabledMessageHandler(options) { InnerHandler = new HttpClientHandler() });
            
            // Act
            await client.GetAsync(DataUrl);
            
            // Assert
            File.Exists(DataUrlFilePath).Should().BeTrue();
        }
        
        [Theory]
        [InlineData(RecordAndReplayMode.ReplayOnly)]
        [InlineData(RecordAndReplayMode.Passthrough)]
        public async Task ShouldNotRecordResponse_WhenWhenUsingRecordModes(RecordAndReplayMode mode)
        {
            // Arrange
            var options = new RecordAndRelayOptions { Mode = mode, RelativeCacheDirectoryPath = CacheLocation };
            var client = new HttpClient(new RecordAndReplayEnabledMessageHandler(options) { InnerHandler = new HttpClientHandler() });
            
            // Act
            await client.GetAsync(DataUrl);
            
            // Assert
            File.Exists(DataUrlFilePath).Should().BeFalse();
        }
        
        [Theory]
        [InlineData(RecordAndReplayMode.Passthrough)]
        [InlineData(RecordAndReplayMode.RecordOnly)]
        public async Task ShouldNotUseRecordedResponse_WhenUsingNonReplayMode(RecordAndReplayMode mode)
        {
            // Arrange
            var options = new RecordAndRelayOptions { Mode = mode, RelativeCacheDirectoryPath = CacheLocation };
            var client = new HttpClient(new RecordAndReplayEnabledMessageHandler(options) { InnerHandler = new HttpClientHandler() });
            var cachedResponse = "cached response";
            Directory.CreateDirectory(CacheLocation);
            File.WriteAllText(DataUrlFilePath, cachedResponse);
            
            // Act
            var actual = await client.GetStringAsync(DataUrl);
            
            // Assert
            actual.Should().NotBe(cachedResponse);
        }
        
        [Theory]
        [InlineData(RecordAndReplayMode.RecordAndReplay)]
        [InlineData(RecordAndReplayMode.ReplayOnly)]
        public async Task ShouldUseRecordedResponse_WhenUsingReplayMode(RecordAndReplayMode mode)
        {
            // Arrange
            var options = new RecordAndRelayOptions { Mode = mode, RelativeCacheDirectoryPath = CacheLocation };
            var client = new HttpClient(new RecordAndReplayEnabledMessageHandler(options) { InnerHandler = new HttpClientHandler() });
            var cachedResponse = "cached response";
            Directory.CreateDirectory(CacheLocation);
            File.WriteAllText(DataUrlFilePath, cachedResponse);
            
            // Act
            var actual = await client.GetStringAsync(DataUrl);
            
            // Assert
            actual.Should().Be(cachedResponse);
        }
        
        [Theory]
        [InlineData(RecordAndReplayMode.RecordAndReplay)]
        [InlineData(RecordAndReplayMode.ReplayOnly)]
        public async Task ShouldUseRecordedResponse_WhenOtherQueryParametersAndQueryParametersNotIncludedInDeterminingCacheHit(RecordAndReplayMode mode)
        {
            // Arrange
            var options = new RecordAndRelayOptions
                { Mode = mode, RelativeCacheDirectoryPath = CacheLocation, IncludeQueryParametersWhenMatchingResponse = false };
            var client = new HttpClient(new RecordAndReplayEnabledMessageHandler(options) { InnerHandler = new HttpClientHandler() });
            var cachedResponse = "cached response";
            Directory.CreateDirectory(CacheLocation);
            File.WriteAllText(DataUrlFilePath, cachedResponse);
            
            // Act
            var actual = await client.GetStringAsync(DataUrl + "?foo=bar");
            
            // Assert
            actual.Should().Be(cachedResponse);
        }
        
        [Theory]
        [InlineData(RecordAndReplayMode.RecordAndReplay)]
        public async Task ShouldNotUseRecordedResponse_WhenOtherQueryParametersAndQueryParametersIncludedInDeterminingCacheHit(RecordAndReplayMode mode)
        {
            // Arrange
            var options = new RecordAndRelayOptions
                { Mode = mode, RelativeCacheDirectoryPath = CacheLocation, IncludeQueryParametersWhenMatchingResponse = true };
            var client = new HttpClient(new RecordAndReplayEnabledMessageHandler(options) { InnerHandler = new HttpClientHandler() });
            var cachedResponse = "cached response";
            Directory.CreateDirectory(CacheLocation);
            File.WriteAllText(DataUrlFilePath, cachedResponse);
            
            // Act
            var actual = await client.GetStringAsync(DataUrl + "?foo=bar");
            
            // Assert
            actual.Should().NotBe(cachedResponse);
        }
    }
}