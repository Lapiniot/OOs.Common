namespace System.Common.CommandLine;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ArgumentAttribute : Attribute, IArgumentMetadata
{
    public ArgumentAttribute(string name, Type type)
    {
        Name = !string.IsNullOrEmpty(name)
            ? name
            : throw new ArgumentException($"{nameof(name)} cannot be null or empty");
        Type = type ?? throw new ArgumentException($"{nameof(type)} cannot be null");
    }

    public ArgumentAttribute(string name, Type type, string shortName) : this(name, type) => ShortName = shortName;

    public string Name { get; }

    public Type Type { get; }

    public string ShortName { get; }

    public string Description { get; set; }
}