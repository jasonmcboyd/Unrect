window.BENCHMARK_DATA = {
  "lastUpdate": 1788448657018,
  "repoUrl": "https://github.com/jasonmcboyd/Unrect",
  "entries": {
    "Engine Benchmarks": [
      {
        "commit": {
          "author": {
            "email": "jason.boyd.ce@gmail.com",
            "name": "Jason Boyd",
            "username": "jasonmcboyd"
          },
          "committer": {
            "email": "jason.boyd.ce@gmail.com",
            "name": "Jason Boyd",
            "username": "jasonmcboyd"
          },
          "distinct": true,
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "tree_id": "bd7e49af5e38882562658dcaf4456abff4c6794a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788448656432,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 1008528.7074497768,
            "unit": "ns",
            "range": "± 8544.14310601732"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 237577.27736253006,
            "unit": "ns",
            "range": "± 1317.0538348882576"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 288813.7011021205,
            "unit": "ns",
            "range": "± 451.71944246461"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 496099.135811942,
            "unit": "ns",
            "range": "± 2798.5858534133195"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 103608.21028019831,
            "unit": "ns",
            "range": "± 71.13379250611139"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 12246216.028846154,
            "unit": "ns",
            "range": "± 48079.52826762986"
          }
        ]
      }
    ],
    "Strategies Benchmarks": [
      {
        "commit": {
          "author": {
            "email": "jason.boyd.ce@gmail.com",
            "name": "Jason Boyd",
            "username": "jasonmcboyd"
          },
          "committer": {
            "email": "jason.boyd.ce@gmail.com",
            "name": "Jason Boyd",
            "username": "jasonmcboyd"
          },
          "distinct": true,
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "tree_id": "bd7e49af5e38882562658dcaf4456abff4c6794a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788448656995,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 612169.4254807692,
            "unit": "ns",
            "range": "± 7054.91772207518"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 576623.5556126644,
            "unit": "ns",
            "range": "± 12321.990907953443"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 285473.9003342849,
            "unit": "ns",
            "range": "± 707.9718398097974"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 4148382.796614583,
            "unit": "ns",
            "range": "± 48491.59031178897"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 4649774.760044643,
            "unit": "ns",
            "range": "± 35260.51825676534"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 5739752.708854167,
            "unit": "ns",
            "range": "± 74928.93049273759"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1106456.598858173,
            "unit": "ns",
            "range": "± 1732.8196144353826"
          }
        ]
      }
    ]
  }
}