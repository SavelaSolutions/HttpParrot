# Record And Replay utilities

Contains tools for recording data that can later be used to "replay" the communication in tests,
instead of having to create complicated mocks for the dependencies.

## Included components

### RecordAndReplayEnabledMessageHandler
A `System.Net.Http.DelegatingHandler` implementation that can record HTTP-responses and replay them at a later time.
Example test setup for ASP.NET Core application using a `IHttpClientFactory` "typed client":

```c#
services.AddHttpClient<ISomeApiClient, SomeApiClient>()
    .AddHttpMessageHandler(serviceProvider =>
    {
        // The below path to the cache directory is normally what's needed to put the cache in the
        // test projects folder and not in the build output, so it can be source controlled.
        return new RecordAndReplayEnabledMessageHandler(RecordAndReplayMode.RecordAndReplay,
                    @"..\..\..\RecordReplayCache", identityProvider);
    });
```

The behavior of the `RecordAndReplayEnabledMessageHandler` is controlled by the `RecordAndReplayMode` enum
passed to the constructor.

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

The request body, query parameters and user identity (provided by a `IRecordAndReplayIdentityProvider` implementation
passed to the constructor) are used to identify a specific request. The identity provider is only needed when a user identity is relevant for the calls,
and not already part of the request body or query parameters.

Note that sometimes requests might contain a unique correlation id or similar (in the body, not as a header). In these cases the requests will not be
determined identical, so steps has to be taken in the test setup to fix the correlation id to a specific value.