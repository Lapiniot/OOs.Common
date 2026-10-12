# OOs.Common.CommandLine

Command-line argument parsing utilities and helpers.

This package provides a lightweight way to define and parse command-line arguments. 
It also includes a Roslyn source generator that emits efficient command-line parsing code automatically 
based on declarative metadata attributes. Such technique delivers strongly-typed reflection free argument parsing 
code, which is crucial for some deployment scenarious like AOT compileation e.g.

## Source generator example

Install the `OOs.Common.CommandLine` package. It includes the source generator as a Roslyn analyzer, so no separate generator package reference is needed:

```sh
dotnet add package OOs.Common.CommandLine
```

Declare a `partial` type and add an `Option<T>` attribute for each supported option:

```csharp
using OOs.CommandLine;

[Option<string>("Output", "output", shortAlias: 'o', Description = "Path to the output file")]
[Option<bool>("Verbose", "verbose", shortAlias: 'v', Description = "Enable verbose output")]
public sealed partial class CliArguments
{
}
```

The generator adds a static `Parse(ReadOnlySpan<string>)` method to `CliArguments`. It returns an `Options` dictionary keyed by each attribute's `name`, and an `Arguments` immutable array containing positional arguments:

```csharp
var parsed = CliArguments.Parse(args);

var outputPath = parsed.Options.GetValueOrDefault("Output");
var verbose = parsed.Options.TryGetValue("Verbose", out var value) && bool.Parse(value);
var inputFiles = parsed.Arguments;
```

For example, parsing `--output result.txt --verbose input.csv` produces the `Output` option with value `result.txt`, the `Verbose` option with value `True`, and `input.csv` as a positional argument. Option values are returned as strings; convert them to the types your application needs.