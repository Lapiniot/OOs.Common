namespace System.Net.Connections;

public sealed class UriEndPoint : EndPoint
{
    private readonly Uri uri;

    public UriEndPoint(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        this.uri = uri;
    }

    public Uri Uri => uri;

    public override string ToString() => uri.ToString();
}