namespace System.Common.CommandLine
{
    public interface ICommandMetadata
    {
        string Name { get; }
        bool Default { get; }
    }
}