window.BENCHMARK_DATA = {
  "lastUpdate": 1788448658157,
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
    ],
    "Tables Benchmarks": [
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
        "date": 1788448657183,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 950376.3010110294,
            "unit": "ns",
            "range": "± 18624.001844587016"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 29062695.465425532,
            "unit": "ns",
            "range": "± 1124631.4914052705"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 3868161.5151041667,
            "unit": "ns",
            "range": "± 51723.0814274104"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 73575926.76530613,
            "unit": "ns",
            "range": "± 955092.0868090979"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 2700377.7158854166,
            "unit": "ns",
            "range": "± 25958.804351789397"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 65901707.96428572,
            "unit": "ns",
            "range": "± 958899.4263633731"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 279681.7744489397,
            "unit": "ns",
            "range": "± 810.166312666795"
          }
        ]
      }
    ],
    "Values Benchmarks": [
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
        "date": 1788448657375,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 79154039.74725273,
            "unit": "ns",
            "range": "± 709015.4798708223"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 64960485.95192308,
            "unit": "ns",
            "range": "± 449957.5781121145"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 14558476.650841346,
            "unit": "ns",
            "range": "± 7246.371474750881"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 5907654.77421875,
            "unit": "ns",
            "range": "± 74922.35867533264"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 3621224.816666667,
            "unit": "ns",
            "range": "± 50081.864598065724"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 5663940.837239583,
            "unit": "ns",
            "range": "± 87913.68367274747"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 3194959.769921875,
            "unit": "ns",
            "range": "± 42523.56261196336"
          }
        ]
      }
    ],
    "EndToEnd Benchmarks": [
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
        "date": 1788448657564,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 1645391.2239583333,
            "unit": "ns",
            "range": "± 27272.116323939"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 19097037.997596152,
            "unit": "ns",
            "range": "± 515247.91579925903"
          }
        ]
      }
    ],
    "Diagnostics Benchmarks": [
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
        "date": 1788448657757,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 1924402.765625,
            "unit": "ns",
            "range": "± 20063.13887130032"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 1918284.6479166667,
            "unit": "ns",
            "range": "± 23892.828558990037"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 206940.40947614398,
            "unit": "ns",
            "range": "± 565.7906420554957"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 214088.36576021634,
            "unit": "ns",
            "range": "± 383.82861642471346"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 1225248.2060546875,
            "unit": "ns",
            "range": "± 4240.9331295821885"
          }
        ]
      }
    ],
    "Engine Memory": [
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
        "date": 1788448657947,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 2440321,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 772320,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 2768,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 1200937,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 1224,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 396,
            "unit": "bytes"
          }
        ]
      }
    ],
    "Strategies Memory": [
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
        "date": 1788448658139,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 345,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 345,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 376,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 382,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 2718,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 532,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 377,
            "unit": "bytes"
          }
        ]
      }
    ]
  }
}