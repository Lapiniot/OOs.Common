using Microsoft.CodeAnalysis;

namespace OOs.Common.CommandLine.Generators;

internal record struct KnownTypes(int SystemBoolean, int SystemTimeSpan)
{
    public static KnownTypes FromCompilation(Compilation compilation) => new(
        compilation.GetSpecialType(SpecialType.System_Boolean).MetadataToken,
        compilation.GetTypeByMetadataName("System.TimeSpan")?.MetadataToken ?? -1
    );
}