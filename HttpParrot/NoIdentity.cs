namespace HttpParrot
{
    public class NoIdentity : IRecordAndReplayIdentityProvider
    {
        public string GetUserIdentifier()
        {
            return string.Empty;
        }
    }
}