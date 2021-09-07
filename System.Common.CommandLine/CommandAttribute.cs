namespace System.Common.CommandLine;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class CommandAttribute : Attribute, ICommandMetadata
{
    public CommandAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public bool IsDefault { get; set; }
}