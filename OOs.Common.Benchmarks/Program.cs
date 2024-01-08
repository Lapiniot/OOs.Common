BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config: BuildGlobalConfig());

internal sealed partial class Program
{
    private static ManualConfig BuildGlobalConfig()
    {
        var config = ManualConfig.CreateMinimumViable()
            .WithOption(ConfigOptions.DisableLogFile, true)
            .WithOption(ConfigOptions.LogBuildOutput, false)
            .WithOption(ConfigOptions.GenerateMSBuildBinLog, false)
            .WithSummaryStyle(SummaryStyle.Default.WithRatioStyle(RatioStyle.Percentage));

        config.AddJob(Job.Default
            .WithArguments([new MsBuildArgument("/p:UseArtifactsOutput=false")])
        // .WithEnvironmentVariable(new EnvironmentVariable("DOTNET_JitDisasm", "System.Net.Http.Base64UrlSafe:EncodeToUtf8"))
        // .WithEnvironmentVariable(new EnvironmentVariable("DOTNET_JitDiffableDasm", "1"))
        // .WithEnvironmentVariable(new EnvironmentVariable("DOTNET_JitStdOutFile", $"{Environment.CurrentDirectory}/EncodeToUtf8.asm"))
        );

        return config;
    }
}