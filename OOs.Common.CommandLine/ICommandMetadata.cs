namespace OOs.CommandLine;

public interface ICommandMetadata
{
    string Name { get; }
    bool IsDefault { get; }
}