using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http
{
    [SuppressMessage("Performance", "CA1819: Properties should not return arrays")]
    public struct SubscriptionKeys : IEquatable<SubscriptionKeys>
    {
        public byte[] P256DHKey { get; }
        public byte[] AuthKey { get; }

        public SubscriptionKeys(byte[] p256DHKey, byte[] authKey)
        {
            P256DHKey = p256DHKey;
            AuthKey = authKey;
        }

        public override bool Equals(object obj)
        {
            return obj is SubscriptionKeys keys && Equals(keys);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(P256DHKey, AuthKey);
        }

        public bool Equals(SubscriptionKeys other)
        {
            return EqualityComparer<byte[]>.Default.Equals(P256DHKey, other.P256DHKey) &&
                   EqualityComparer<byte[]>.Default.Equals(AuthKey, other.AuthKey);
        }

        public static bool operator ==(SubscriptionKeys a, SubscriptionKeys b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(SubscriptionKeys a, SubscriptionKeys b)
        {
            return !a.Equals(b);
        }
    }
}