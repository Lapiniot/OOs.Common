using System.Reflection;

internal static class ProductInfo
{
    static ProductInfo()
    {
        var assembly = typeof(ProductInfo).Assembly;
        Product = assembly.GetCustomAttribute<AssemblyProductAttribute>().Product;
        Version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        Copyright = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;
    }

    public static string Product { get; }
    public static string Version { get; }
    public static string Copyright { get; }
}