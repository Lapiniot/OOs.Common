namespace OOs.CommandLine;

public interface IArgumentMetadata
{
    string Name { get; }
    string LongAlias { get; }
    char ShortAlias { get; }
    string Description { get; }
    Type Type { get; }
}