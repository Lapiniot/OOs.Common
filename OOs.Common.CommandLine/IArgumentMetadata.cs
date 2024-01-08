namespace OOs.CommandLine;

public interface IArgumentMetadata
{
    string Name { get; }
    string ShortName { get; }
    Type Type { get; }
    string Description { get; }
}