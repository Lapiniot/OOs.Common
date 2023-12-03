using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config: BuildGlobalConfig(args));

internal sealed partial class Program
{
    private static ManualConfig BuildGlobalConfig(string[] args)
    {
        var config = ManualConfig.CreateMinimumViable()
            .WithOption(ConfigOptions.DisableLogFile, true)
            .WithOption(ConfigOptions.LogBuildOutput, false)
            .WithOption(ConfigOptions.GenerateMSBuildBinLog, false)
            .WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Percentage));

        config.AddJob(Job.Default
            .WithArguments([new MsBuildArgument("/p:UseArtifactsOutput=false")])
        // .WithEnvironmentVariable(new EnvironmentVariable("DOTNET_JitDisasm", "WriteBufferAdvSmd"))
        // .WithEnvironmentVariable(new EnvironmentVariable("DOTNET_JitDiffableDasm", "1"))
        // .WithEnvironmentVariable(new EnvironmentVariable("DOTNET_JitStdOutFile", ""))
        );

        return config;
    }
}