namespace System.Common.CommandLine
{
    public interface IArgumentMetadata
    {
        string Name { get; }
        string ShortName { get; }
        Type Type { get; }
        object DefaultValue { get; }
        bool Optional { get; }
        string Description { get; }
    }
}