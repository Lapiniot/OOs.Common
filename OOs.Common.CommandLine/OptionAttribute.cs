namespace OOs.CommandLine;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public abstract class OptionAttribute : Attribute, IArgumentMetadata
{
    protected OptionAttribute(string name, Type type, string longAlias)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrEmpty(longAlias);

        Name = name;
        Type = type;
        LongAlias = longAlias;
    }

    public string Name { get; }
    public Type Type { get; }
    public string LongAlias { get; }
    public char ShortAlias { get; set; }
    public string Description { get; set; }
    public string Hint { get; set; }
}

public sealed class OptionAttribute<T>(string name, string longAlias) : OptionAttribute(name, typeof(T), longAlias)
{
    public OptionAttribute(string name, string longAlias, char shortAlias) :
        this(name, longAlias) => ShortAlias = shortAlias;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class ArgumentParserGenerationOptionsAttribute() : Attribute
{
    public bool GenerateSynopsis { get; set; }
}