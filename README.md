# HTTP Parrot ("Record and Replay utilities")

Contains tools for recording HTTP responses when using HttpClient that can later be used to "replay" the communication in tests,
instead of having to create complicated mocks for the external dependencies. That is, let the recorded HTTP responses be your mock data,
that can also be changed if needed.

## Included components

### RecordAndReplayEnabledMessageHandler
A `System.Net.Http.DelegatingHandler` implementation that can record HTTP-responses and replay them at a later time.
Example test setup for ASP.NET Core application using a `IHttpClientFactory` "typed client":

```c#
services.AddHttpClient<ISomeApiClient, SomeApiClient>()
    .AddHttpMessageHandler(serviceProvider =>
    {
        // The below path to the cache directory is normally what's needed to put the cache in the project folder and not in the build output.
        return new RecordAndReplayEnabledMessageHandler(new RecordAndRelayOptions
        {
            Mode = RecordAndReplayMode.RecordAndReplay,
            RelativeCacheDirectoryPath = @"..\..\..\RecordReplayCache",
            IdentityProvider = identityProvider // Custom implementation of IRecordAndReplayIdentityProvider, if needed
        });
    });
```

If you want to add record & replay handlers too all clients generated from the IHttpClientFactory you can instead use
`RecordAndReplayEnabledMessageHandlerExtensions.AddRecordAndReplayEnabledMessageHandlerToDefaultHttpClientFactory` as follows:

```c#
// Adds HttpParrot "record and replay" handler to all HttpClient instances generated from the default http client factory
services.AddRecordAndReplayEnabledMessageHandlerToDefaultHttpClientFactory(new RecordAndRelayOptions
{
    Mode = RecordAndReplayMode.RecordAndReplay,
    RelativeCacheDirectoryPath = @"..\..\..\RecordReplayCache"
});
```

The behavior of the `RecordAndReplayEnabledMessageHandler` is controlled by the `RecordAndReplayMode` enum.

```c#
public enum RecordAndReplayMode
{
    /// <summary>
    /// Do not record or replay. Always do the actual call.
    /// </summary>
    Passthrough,

    /// <summary>
    /// Only replay, never do actual call even if no recorded response exists.
    /// </summary>
    ReplayOnly,

    /// <summary>
    /// Always do the actual call and record the response, overwriting any matching recorded response.
    /// </summary>
    RecordOnly,

    /// <summary>
    /// Replay if matching data exists, otherwise do actual call and record the response.
    /// </summary>
    RecordAndReplay
}
```

The request body, query parameters and user identity can also be used to identify a specific request by setting the corresponding properties in the
`RecordAndRelayOptions` like so:

```c#
new RecordAndReplayEnabledMessageHandler(new RecordAndRelayOptions
    {
        ...
        IncludeQueryParametersWhenMatchingResponse = true,
        IncludeBodyWhenMatchingResponse = true
        IdentityProvider = new CustomIdentityProvider() // Implementation of IRecordAndReplayIdentityProvider
    });
```

The identity provider is only needed when the user identity is relevant for the calls, and not already part of the request body or query parameters.

Note that sometimes requests might contain a unique correlation id or similar (in the body, not as a header). In these cases the requests
will not be determined identical, so steps has to be taken in the test setup to pin the correlation id to a specific value.