namespace System.Net.Http
{
    public struct SubscriptionKeys
    {
        public byte[] P256DHKey { get; set; }
        public byte[] AuthKey { get; set; }

        public SubscriptionKeys(byte[] p256DHKey, byte[] authKey)
        {
            P256DHKey = p256DHKey;
            AuthKey = authKey;
        }
    }
}