namespace System.Common.CommandLine;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class CommandAttribute(string name) : Attribute, ICommandMetadata
{
    public string Name { get; } = name;

    public bool IsDefault { get; set; }
}