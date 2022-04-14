namespace System.Common.CommandLine;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class ArgumentAttribute : Attribute, IArgumentMetadata
{
    public ArgumentAttribute(string name, Type type)
    {
        Verify.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(type);

        Name = name;
        Type = type;
    }

    public ArgumentAttribute(string name, Type type, string shortName) : this(name, type) => ShortName = shortName;

    public string Name { get; }

    public Type Type { get; }

    public string ShortName { get; }

    public string Description { get; set; }
}