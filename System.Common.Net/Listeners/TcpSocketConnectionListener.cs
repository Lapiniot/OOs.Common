namespace System.Net.Listeners
{
    public class TcpSocketConnectionListener : ConnectionListener
    {
        private readonly IPEndPoint ipEndPoint;

        public TcpSocketConnectionListener(IPEndPoint ipEndPoint)
        {
            this.ipEndPoint = ipEndPoint;
        }

        public override void Start()
        {
            throw new NotImplementedException();
        }

        public override void Stop()
        {
            throw new NotImplementedException();
        }
    }
}