namespace HttpParrot
{
    public class RecordAndRelayOptions
    {
        /// <summary>
        /// The record and replay mode that this handler should use. See documentation on <see cref="RecordAndReplayMode"/> enum for details.
        /// </summary>
        /// <default>RecordAndReplayMode.RecordAndReplay</default>
        public RecordAndReplayMode Mode { get; set; } = RecordAndReplayMode.RecordAndReplay;
        
        /// <summary>
        /// The relative path to the cache directory. Note that when running tests, the current path is normally the build output folder,
        /// so setting a path two levels up will put them in the project directory, to be able to be source controlled.
        /// </summary>
        public string RelativeCacheDirectoryPath { get; set; }

        /// <summary>
        /// Should the request query parameters be included when determining if the request has a recorded response?
        /// </summary>
        /// <default>false</default>
        public bool IncludeQueryParametersWhenMatchingResponse { get; set; } = false;
        
        /// <summary>
        /// Should the request body be included when determining if the request has a recorded response?
        /// Defaults to false.
        /// </summary>
        /// <default>false</default>
        public bool IncludeBodyWhenMatchingResponse { get; set; } = false;
        
        /// <summary>
        /// Optional identity provider. The identity will be included when generating the filename hashes, to be able to differentiate
        /// between users if the user identity is not part of the request payload or query. Defaults to NoIdentityProvider.
        /// </summary>
        /// <default>NoIdentity</default>
        public IRecordAndReplayIdentityProvider IdentityProvider { get; set; } = new NoIdentity();
    }
}