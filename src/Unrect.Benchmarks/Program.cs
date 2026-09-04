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
      // The retention family is measured, not benchmarked: a deterministic live-set reading needs one
      // build and one forced collection, where a benchmark engine's whole job is to run an operation
      // thousands of times and keep none of the results. It rides the same matrix leg, the same
      // --artifacts convention and the same stored JSON shape as everything else; only the instrument
      // differs. See Retention and docs/benchmarking.md.
      if (Array.IndexOf(args, "--retention") >= 0)
      {
        Retention.Run(args);

        return;
      }

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
