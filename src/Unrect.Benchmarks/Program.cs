using System;

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

namespace Unrect.Benchmarks
{
  public class Program
  {
    public static void Main(string[] args)
    {
      var isCI = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI"))
              || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

      // BenchmarkSwitcher discovers every benchmark class in the assembly and supports the
      // command-line filters the workflow uses (--allCategories, --job, --exporters).
      BenchmarkSwitcher
        .FromAssembly(typeof(Program).Assembly)
        .Run(args, GetConfig(isCI));
    }

    private static IConfig GetConfig(bool isCI)
    {
      var config = ManualConfig
        .Create(DefaultConfig.Instance)
        .WithOptions(ConfigOptions.JoinSummary);

      // CI measures for real; local runs trade accuracy for a fast edit-measure loop. Either way
      // the log file is off -- the workflow keeps the JSON exports, not BDN's console transcript.
      return config
        .AddJob(isCI ? Job.Default : Job.ShortRun)
        .WithOptions(ConfigOptions.DisableLogFile);
    }
  }
}
