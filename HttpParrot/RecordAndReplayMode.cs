namespace HttpParrot
{
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
}