window.BENCHMARK_DATA = {
  "lastUpdate": 1788472136705,
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
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788449669770,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 1048744.6022786458,
            "unit": "ns",
            "range": "± 14565.080188403246"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 246152.837890625,
            "unit": "ns",
            "range": "± 3665.704356575879"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 264675.2246791295,
            "unit": "ns",
            "range": "± 542.8726398710696"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 518220.03388671874,
            "unit": "ns",
            "range": "± 7290.058419558245"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 87248.77011343148,
            "unit": "ns",
            "range": "± 133.79600049037106"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 11833196.595833333,
            "unit": "ns",
            "range": "± 122155.13512031641"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788451024514,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 1106702.2029296875,
            "unit": "ns",
            "range": "± 9147.114515975281"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 260204.71432291667,
            "unit": "ns",
            "range": "± 3573.57470711858"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 267317.301175631,
            "unit": "ns",
            "range": "± 409.80572234340605"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 540659.7218889509,
            "unit": "ns",
            "range": "± 6605.01550445132"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 89873.20478703425,
            "unit": "ns",
            "range": "± 163.1402849642681"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 12813163.914583333,
            "unit": "ns",
            "range": "± 190596.93290731363"
          }
        ]
      },
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
          "distinct": false,
          "id": "37bb6bef3d2e23e9778f5b9e84c650537b11688b",
          "message": "The rig meets the struct: delete the null-fill helper\n\nCanonicalSpaces.Fill pre-filled sparse builders' null slots with Blank\n— meaningless under the struct, where default(CellValue) IS Blank and\n??= on a value type rightly refuses to compile. The compiler was the\ntest; the helper joins SpreadsheetSpace's pre-fill loop in the bin.\n(The rig postdates the spike, so this branch first built it in CI.)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T16:07:53Z",
          "tree_id": "7ee5fa22b9a124939f19ebe64c0fe44407a9744e",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/37bb6bef3d2e23e9778f5b9e84c650537b11688b"
        },
        "date": 1788453186497,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 943203.8921595982,
            "unit": "ns",
            "range": "± 10133.905211067296"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 265035.773453776,
            "unit": "ns",
            "range": "± 3974.7339242572534"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 301783.29715983075,
            "unit": "ns",
            "range": "± 227.43367073754538"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 564160.3654436384,
            "unit": "ns",
            "range": "± 7454.688464300909"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 93245.11617606027,
            "unit": "ns",
            "range": "± 127.86491766525754"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 8399028.886904761,
            "unit": "ns",
            "range": "± 195297.8155740664"
          }
        ]
      },
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
          "id": "3e69dc58aa0c9a0300fe0f43a33218891c36e566",
          "message": "Docs: the struct era, on the record\n\nCLAUDE.md's singleton line becomes the struct story (default IS Blank,\nadopted 2026-09-03, judged by the rig: creation allocations -42%/-61%,\nzero-heap double/string/date/bool cells); test count 905. The\ncanonical-model design doc's \"revisit before million-row workloads\"\ngets its strike-through and its account: both halves revisited — the\nrepresentation by spike, patch, and branch verdict; the eager\nmaterialization by the parked windowed-space prototype (681 MB -> 2 MB)\nawaiting the area-resolution fusion.\n\nThat sentence, written before wave 1 shipped, called both problems and\ntheir order. Some prophecies keep.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T17:01:58Z",
          "tree_id": "ee45abc46b58f0dc515d34a15cb71482009b1b9d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/3e69dc58aa0c9a0300fe0f43a33218891c36e566"
        },
        "date": 1788456120104,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 1088902.4057992788,
            "unit": "ns",
            "range": "± 9577.176715823778"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 269645.84542643226,
            "unit": "ns",
            "range": "± 2242.507127935791"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 310659.0350516183,
            "unit": "ns",
            "range": "± 831.5608240327953"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 548531.3220703125,
            "unit": "ns",
            "range": "± 4309.744151384665"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 106302.79598294772,
            "unit": "ns",
            "range": "± 301.48588023127695"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 10841723.238839285,
            "unit": "ns",
            "range": "± 53561.12440733413"
          }
        ]
      },
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
          "id": "ef348dd370a754a5e4d2cce5dbea9a4328100c95",
          "message": "Streaming Part 1: Workbook, the windowed store, the lead/chase pool\n\ndocs/design/streaming-spec.md made real. The memory investigation's\nanswer, built on the algebra's own monotonicity: a million-row workbook\nparses in a ~1 MB window instead of 214 MB resident.\n\n- Workbook.Open(path) owns the apparatus — file handles, reader pool,\n  chunk stores — and vends lent Sheet(name) views: pure ISpace values,\n  invalidated only by the owner's Dispose (a fault, never absorbable).\n  Sheet is idempotent per name; a second declaration over the same open\n  book rides warm readers and hot chunks. The motivating idiom: one\n  shape over a year of monthly closes, one using-block per file,\n  Parallel.ForEach-ready\n- The IRowSource seam (blankness decided adapter-side, faults\n  injectable, benchmarks workbook-free), the chunked SheetStore\n  (BytesPerCell = 24, no pre-fill — default IS Blank; window >= tallest\n  open band is the sizing law; WindowOverruns says a band didn't fit,\n  ChunkReloads says what it cost), and the ReaderPool: lexicographic\n  lead/chase positioning, adoption-slot reservation made structural,\n  adaptive warming grown only on evidence (spare open or reopen —\n  contention is not pressure), BorrowAnywhere catalogue walks\n- IO fault discipline: IsProjectionFault became IsFault and grew\n  IOException/ObjectDisposedException/OutOfMemoryException at all four\n  wrap sites — .Optional() can never swallow a disk failure as a\n  missing section. Bounds unified across every door: any ISpace overrun\n  is OutOfBoundsException, a data condition, pinned by a contract suite\n- Four concurrency races found by review and QA, fixed and pinned\n  deterministically (FakeRowSource gates, no sleeps; the hang-shaped\n  one timeout-armored so its regression fails in seconds, never wedges\n  CI): the InUse leak that turned one disk error into a hung workbook,\n  the pulse Dispose forgot, and the warm-vs-Fill pair the reservation\n  invariant now excludes by construction\n- The Streaming benchmark family (7 rows in 3 same-run pairs, fixtures\n  sized against store statistics after two inert first drafts) joins\n  the rig: 41 benchmarks, seven families, 14 store steps\n- Two committed fixtures (multi-sheet.xlsx, tall-ledger.xlsx), 175\n  streaming tests among 1,080 total, and the full doc set: streaming.md\n  user guide, README's Large files, CLAUDE.md, vocabulary.md,\n  benchmarking.md — every claim verified against shipped code\n\nPart 2 (lazy extents — bound+project fusion, opening with the\nheader-derived Table width decision) is specced at streaming-spec §11,\ngated on this merge.\n\n1,080 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T21:43:30Z",
          "tree_id": "9f817ac162237f132ebb583899d911728ccb09a0",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/ef348dd370a754a5e4d2cce5dbea9a4328100c95"
        },
        "date": 1788472135355,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 969968.2890625,
            "unit": "ns",
            "range": "± 2009.8406683811838"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 244485.35735212054,
            "unit": "ns",
            "range": "± 1843.4144261854462"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 313721.1159667969,
            "unit": "ns",
            "range": "± 1261.0688260679424"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 508251.43365885416,
            "unit": "ns",
            "range": "± 1077.954554050938"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 100986.89883188102,
            "unit": "ns",
            "range": "± 66.54061716498842"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 10402735.93638393,
            "unit": "ns",
            "range": "± 76844.86389138008"
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
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788449671316,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 502442.39892578125,
            "unit": "ns",
            "range": "± 5234.432896671859"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 570161.462109375,
            "unit": "ns",
            "range": "± 8192.902717514904"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 264782.19081333705,
            "unit": "ns",
            "range": "± 510.30183151780784"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 4893908.103645833,
            "unit": "ns",
            "range": "± 37676.67663476687"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 5511274.629947917,
            "unit": "ns",
            "range": "± 44250.48339369378"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 6135567.006770833,
            "unit": "ns",
            "range": "± 74953.94254910928"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1058011.6325520833,
            "unit": "ns",
            "range": "± 2057.589349456533"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788451025501,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 1246774.2698730468,
            "unit": "ns",
            "range": "± 256374.94716234322"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 1031905.1229858398,
            "unit": "ns",
            "range": "± 370695.7330430645"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 288422.1310471755,
            "unit": "ns",
            "range": "± 908.0579350623565"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 4192908.3563701925,
            "unit": "ns",
            "range": "± 41840.57358902713"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 4716552.375600962,
            "unit": "ns",
            "range": "± 53342.237094764336"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 5719864.368303572,
            "unit": "ns",
            "range": "± 93020.36876739282"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1100727.652278646,
            "unit": "ns",
            "range": "± 4752.086206998873"
          }
        ]
      },
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
          "distinct": false,
          "id": "37bb6bef3d2e23e9778f5b9e84c650537b11688b",
          "message": "The rig meets the struct: delete the null-fill helper\n\nCanonicalSpaces.Fill pre-filled sparse builders' null slots with Blank\n— meaningless under the struct, where default(CellValue) IS Blank and\n??= on a value type rightly refuses to compile. The compiler was the\ntest; the helper joins SpreadsheetSpace's pre-fill loop in the bin.\n(The rig postdates the spike, so this branch first built it in CI.)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T16:07:53Z",
          "tree_id": "7ee5fa22b9a124939f19ebe64c0fe44407a9744e",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/37bb6bef3d2e23e9778f5b9e84c650537b11688b"
        },
        "date": 1788453187202,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 489301.35281808034,
            "unit": "ns",
            "range": "± 1629.9741951056224"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 491301.875906808,
            "unit": "ns",
            "range": "± 1726.1914740117365"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 309868.18297400844,
            "unit": "ns",
            "range": "± 231.88029542808715"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 2790112.2674278845,
            "unit": "ns",
            "range": "± 1923.3039441283252"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 3170090.056770833,
            "unit": "ns",
            "range": "± 7381.875698310584"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 3271776.23828125,
            "unit": "ns",
            "range": "± 5308.5588658899205"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1338235.6740234375,
            "unit": "ns",
            "range": "± 857.4522919226143"
          }
        ]
      },
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
          "id": "3e69dc58aa0c9a0300fe0f43a33218891c36e566",
          "message": "Docs: the struct era, on the record\n\nCLAUDE.md's singleton line becomes the struct story (default IS Blank,\nadopted 2026-09-03, judged by the rig: creation allocations -42%/-61%,\nzero-heap double/string/date/bool cells); test count 905. The\ncanonical-model design doc's \"revisit before million-row workloads\"\ngets its strike-through and its account: both halves revisited — the\nrepresentation by spike, patch, and branch verdict; the eager\nmaterialization by the parked windowed-space prototype (681 MB -> 2 MB)\nawaiting the area-resolution fusion.\n\nThat sentence, written before wave 1 shipped, called both problems and\ntheir order. Some prophecies keep.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T17:01:58Z",
          "tree_id": "ee45abc46b58f0dc515d34a15cb71482009b1b9d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/3e69dc58aa0c9a0300fe0f43a33218891c36e566"
        },
        "date": 1788456121018,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 493261.1505301339,
            "unit": "ns",
            "range": "± 22609.676810958303"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 471421.1591389974,
            "unit": "ns",
            "range": "± 23180.4622376945"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 315735.67647879466,
            "unit": "ns",
            "range": "± 501.3327717186516"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 2841925.8903459823,
            "unit": "ns",
            "range": "± 11931.726998967713"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 3148553.1690104166,
            "unit": "ns",
            "range": "± 22916.28425879564"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 3480631.84765625,
            "unit": "ns",
            "range": "± 149667.2691985299"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1236732.7483723958,
            "unit": "ns",
            "range": "± 1351.5268297297707"
          }
        ]
      },
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
          "id": "ef348dd370a754a5e4d2cce5dbea9a4328100c95",
          "message": "Streaming Part 1: Workbook, the windowed store, the lead/chase pool\n\ndocs/design/streaming-spec.md made real. The memory investigation's\nanswer, built on the algebra's own monotonicity: a million-row workbook\nparses in a ~1 MB window instead of 214 MB resident.\n\n- Workbook.Open(path) owns the apparatus — file handles, reader pool,\n  chunk stores — and vends lent Sheet(name) views: pure ISpace values,\n  invalidated only by the owner's Dispose (a fault, never absorbable).\n  Sheet is idempotent per name; a second declaration over the same open\n  book rides warm readers and hot chunks. The motivating idiom: one\n  shape over a year of monthly closes, one using-block per file,\n  Parallel.ForEach-ready\n- The IRowSource seam (blankness decided adapter-side, faults\n  injectable, benchmarks workbook-free), the chunked SheetStore\n  (BytesPerCell = 24, no pre-fill — default IS Blank; window >= tallest\n  open band is the sizing law; WindowOverruns says a band didn't fit,\n  ChunkReloads says what it cost), and the ReaderPool: lexicographic\n  lead/chase positioning, adoption-slot reservation made structural,\n  adaptive warming grown only on evidence (spare open or reopen —\n  contention is not pressure), BorrowAnywhere catalogue walks\n- IO fault discipline: IsProjectionFault became IsFault and grew\n  IOException/ObjectDisposedException/OutOfMemoryException at all four\n  wrap sites — .Optional() can never swallow a disk failure as a\n  missing section. Bounds unified across every door: any ISpace overrun\n  is OutOfBoundsException, a data condition, pinned by a contract suite\n- Four concurrency races found by review and QA, fixed and pinned\n  deterministically (FakeRowSource gates, no sleeps; the hang-shaped\n  one timeout-armored so its regression fails in seconds, never wedges\n  CI): the InUse leak that turned one disk error into a hung workbook,\n  the pulse Dispose forgot, and the warm-vs-Fill pair the reservation\n  invariant now excludes by construction\n- The Streaming benchmark family (7 rows in 3 same-run pairs, fixtures\n  sized against store statistics after two inert first drafts) joins\n  the rig: 41 benchmarks, seven families, 14 store steps\n- Two committed fixtures (multi-sheet.xlsx, tall-ledger.xlsx), 175\n  streaming tests among 1,080 total, and the full doc set: streaming.md\n  user guide, README's Large files, CLAUDE.md, vocabulary.md,\n  benchmarking.md — every claim verified against shipped code\n\nPart 2 (lazy extents — bound+project fusion, opening with the\nheader-derived Table width decision) is specced at streaming-spec §11,\ngated on this merge.\n\n1,080 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T21:43:30Z",
          "tree_id": "9f817ac162237f132ebb583899d911728ccb09a0",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/ef348dd370a754a5e4d2cce5dbea9a4328100c95"
        },
        "date": 1788472136521,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 384184.22716346156,
            "unit": "ns",
            "range": "± 2572.436851010054"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 387218.74668666295,
            "unit": "ns",
            "range": "± 6147.8206514389285"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 239997.0117563101,
            "unit": "ns",
            "range": "± 233.00650611568446"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 2232079.163762019,
            "unit": "ns",
            "range": "± 6868.853225188102"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 2454727.779597356,
            "unit": "ns",
            "range": "± 6760.530190215174"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 2552381.4422433036,
            "unit": "ns",
            "range": "± 8732.625691154779"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 979536.765625,
            "unit": "ns",
            "range": "± 2100.2714177608664"
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
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788449671944,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1022195.8191008391,
            "unit": "ns",
            "range": "± 28404.773071523137"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 40319837.152499996,
            "unit": "ns",
            "range": "± 3806929.1186842173"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 3493747.2606833586,
            "unit": "ns",
            "range": "± 185151.09546243848"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 105135552.90714283,
            "unit": "ns",
            "range": "± 3013496.2655419633"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 2292669.7584918477,
            "unit": "ns",
            "range": "± 51385.83533808688"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 76203009.93217894,
            "unit": "ns",
            "range": "± 5292731.020988784"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 230989.80587332588,
            "unit": "ns",
            "range": "± 1416.86942903687"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788451025955,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1196526.9749098558,
            "unit": "ns",
            "range": "± 7215.02467958296"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 37353209.94901961,
            "unit": "ns",
            "range": "± 446381.41201495694"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 5022827.014583333,
            "unit": "ns",
            "range": "± 84787.70269304274"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 91537286.48888889,
            "unit": "ns",
            "range": "± 1360285.0455338638"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 3435196.3950520833,
            "unit": "ns",
            "range": "± 43158.93629950295"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 80195288.48571429,
            "unit": "ns",
            "range": "± 1037449.9880549246"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 354140.1109900841,
            "unit": "ns",
            "range": "± 572.4859228808249"
          }
        ]
      },
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
          "distinct": false,
          "id": "37bb6bef3d2e23e9778f5b9e84c650537b11688b",
          "message": "The rig meets the struct: delete the null-fill helper\n\nCanonicalSpaces.Fill pre-filled sparse builders' null slots with Blank\n— meaningless under the struct, where default(CellValue) IS Blank and\n??= on a value type rightly refuses to compile. The compiler was the\ntest; the helper joins SpreadsheetSpace's pre-fill loop in the bin.\n(The rig postdates the spike, so this branch first built it in CI.)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T16:07:53Z",
          "tree_id": "7ee5fa22b9a124939f19ebe64c0fe44407a9744e",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/37bb6bef3d2e23e9778f5b9e84c650537b11688b"
        },
        "date": 1788453187417,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1138172.2774832589,
            "unit": "ns",
            "range": "± 13613.247065629706"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 34038002.57647059,
            "unit": "ns",
            "range": "± 674237.0680600378"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 5199434.716145833,
            "unit": "ns",
            "range": "± 80142.83287150368"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 105857894.44999997,
            "unit": "ns",
            "range": "± 2394716.3245211234"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 3993814.875,
            "unit": "ns",
            "range": "± 63620.400795283815"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 120989823.66896549,
            "unit": "ns",
            "range": "± 3535918.459749721"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 353720.4766927083,
            "unit": "ns",
            "range": "± 1819.454854426795"
          }
        ]
      },
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
          "id": "3e69dc58aa0c9a0300fe0f43a33218891c36e566",
          "message": "Docs: the struct era, on the record\n\nCLAUDE.md's singleton line becomes the struct story (default IS Blank,\nadopted 2026-09-03, judged by the rig: creation allocations -42%/-61%,\nzero-heap double/string/date/bool cells); test count 905. The\ncanonical-model design doc's \"revisit before million-row workloads\"\ngets its strike-through and its account: both halves revisited — the\nrepresentation by spike, patch, and branch verdict; the eager\nmaterialization by the parked windowed-space prototype (681 MB -> 2 MB)\nawaiting the area-resolution fusion.\n\nThat sentence, written before wave 1 shipped, called both problems and\ntheir order. Some prophecies keep.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T17:01:58Z",
          "tree_id": "ee45abc46b58f0dc515d34a15cb71482009b1b9d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/3e69dc58aa0c9a0300fe0f43a33218891c36e566"
        },
        "date": 1788456121222,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1227939.0518465908,
            "unit": "ns",
            "range": "± 14811.835720408459"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 33379321.687179487,
            "unit": "ns",
            "range": "± 510753.63142436626"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 5277923.513950893,
            "unit": "ns",
            "range": "± 64404.19162584165"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 104574821.45333335,
            "unit": "ns",
            "range": "± 1770414.4496411162"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 4140980.3229166665,
            "unit": "ns",
            "range": "± 60230.98420405315"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 114067145.52307692,
            "unit": "ns",
            "range": "± 1263164.9983336485"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 338927.742578125,
            "unit": "ns",
            "range": "± 1481.7708546154106"
          }
        ]
      },
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
          "id": "ef348dd370a754a5e4d2cce5dbea9a4328100c95",
          "message": "Streaming Part 1: Workbook, the windowed store, the lead/chase pool\n\ndocs/design/streaming-spec.md made real. The memory investigation's\nanswer, built on the algebra's own monotonicity: a million-row workbook\nparses in a ~1 MB window instead of 214 MB resident.\n\n- Workbook.Open(path) owns the apparatus — file handles, reader pool,\n  chunk stores — and vends lent Sheet(name) views: pure ISpace values,\n  invalidated only by the owner's Dispose (a fault, never absorbable).\n  Sheet is idempotent per name; a second declaration over the same open\n  book rides warm readers and hot chunks. The motivating idiom: one\n  shape over a year of monthly closes, one using-block per file,\n  Parallel.ForEach-ready\n- The IRowSource seam (blankness decided adapter-side, faults\n  injectable, benchmarks workbook-free), the chunked SheetStore\n  (BytesPerCell = 24, no pre-fill — default IS Blank; window >= tallest\n  open band is the sizing law; WindowOverruns says a band didn't fit,\n  ChunkReloads says what it cost), and the ReaderPool: lexicographic\n  lead/chase positioning, adoption-slot reservation made structural,\n  adaptive warming grown only on evidence (spare open or reopen —\n  contention is not pressure), BorrowAnywhere catalogue walks\n- IO fault discipline: IsProjectionFault became IsFault and grew\n  IOException/ObjectDisposedException/OutOfMemoryException at all four\n  wrap sites — .Optional() can never swallow a disk failure as a\n  missing section. Bounds unified across every door: any ISpace overrun\n  is OutOfBoundsException, a data condition, pinned by a contract suite\n- Four concurrency races found by review and QA, fixed and pinned\n  deterministically (FakeRowSource gates, no sleeps; the hang-shaped\n  one timeout-armored so its regression fails in seconds, never wedges\n  CI): the InUse leak that turned one disk error into a hung workbook,\n  the pulse Dispose forgot, and the warm-vs-Fill pair the reservation\n  invariant now excludes by construction\n- The Streaming benchmark family (7 rows in 3 same-run pairs, fixtures\n  sized against store statistics after two inert first drafts) joins\n  the rig: 41 benchmarks, seven families, 14 store steps\n- Two committed fixtures (multi-sheet.xlsx, tall-ledger.xlsx), 175\n  streaming tests among 1,080 total, and the full doc set: streaming.md\n  user guide, README's Large files, CLAUDE.md, vocabulary.md,\n  benchmarking.md — every claim verified against shipped code\n\nPart 2 (lazy extents — bound+project fusion, opening with the\nheader-derived Table width decision) is specced at streaming-spec §11,\ngated on this merge.\n\n1,080 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T21:43:30Z",
          "tree_id": "9f817ac162237f132ebb583899d911728ccb09a0",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/ef348dd370a754a5e4d2cce5dbea9a4328100c95"
        },
        "date": 1788472136684,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1144205.9255208333,
            "unit": "ns",
            "range": "± 11519.973805231322"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 33536387.740350872,
            "unit": "ns",
            "range": "± 720436.6616340508"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 5266439.4203125,
            "unit": "ns",
            "range": "± 70983.0941785871"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 111790915.98863636,
            "unit": "ns",
            "range": "± 2652442.185074288"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 3911558.144270833,
            "unit": "ns",
            "range": "± 55068.829839657345"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 112473923.98214285,
            "unit": "ns",
            "range": "± 1860977.3279315352"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 355971.83231026784,
            "unit": "ns",
            "range": "± 1339.1257733882217"
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
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788449672604,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 75706784.46938775,
            "unit": "ns",
            "range": "± 1027090.1164039002"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 63915099.008928575,
            "unit": "ns",
            "range": "± 409758.0287399911"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 14477232.657552084,
            "unit": "ns",
            "range": "± 10176.656385524157"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 6812181.683293269,
            "unit": "ns",
            "range": "± 93350.4515866067"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 3540559.9592633927,
            "unit": "ns",
            "range": "± 35342.24492655773"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 5579559.18359375,
            "unit": "ns",
            "range": "± 22232.4690844124"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 3260289.294456845,
            "unit": "ns",
            "range": "± 74756.21221194195"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788451026377,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 62441856.275,
            "unit": "ns",
            "range": "± 808453.9698979758"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 54971018.15384615,
            "unit": "ns",
            "range": "± 225118.10681727546"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 11204808.912259616,
            "unit": "ns",
            "range": "± 5897.20521583639"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 5239238.502403846,
            "unit": "ns",
            "range": "± 25960.21529177962"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 2829519.61953125,
            "unit": "ns",
            "range": "± 29372.147771938788"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 4398559.239783654,
            "unit": "ns",
            "range": "± 8507.843919161634"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 2551807.6846354166,
            "unit": "ns",
            "range": "± 40032.559972058894"
          }
        ]
      },
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
          "distinct": false,
          "id": "37bb6bef3d2e23e9778f5b9e84c650537b11688b",
          "message": "The rig meets the struct: delete the null-fill helper\n\nCanonicalSpaces.Fill pre-filled sparse builders' null slots with Blank\n— meaningless under the struct, where default(CellValue) IS Blank and\n??= on a value type rightly refuses to compile. The compiler was the\ntest; the helper joins SpreadsheetSpace's pre-fill loop in the bin.\n(The rig postdates the spike, so this branch first built it in CI.)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T16:07:53Z",
          "tree_id": "7ee5fa22b9a124939f19ebe64c0fe44407a9744e",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/37bb6bef3d2e23e9778f5b9e84c650537b11688b"
        },
        "date": 1788453187661,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 67907750.42016806,
            "unit": "ns",
            "range": "± 1380302.7971648688"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 32181577.9625,
            "unit": "ns",
            "range": "± 300820.6104633273"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 19921997.75669643,
            "unit": "ns",
            "range": "± 15442.91222481531"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 2667624.3580729165,
            "unit": "ns",
            "range": "± 17986.455906881845"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 1732037.1286458333,
            "unit": "ns",
            "range": "± 27065.563330751986"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 7773322.544170673,
            "unit": "ns",
            "range": "± 10269.598126166473"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 595143.7081380208,
            "unit": "ns",
            "range": "± 16636.167674800345"
          }
        ]
      },
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
          "id": "3e69dc58aa0c9a0300fe0f43a33218891c36e566",
          "message": "Docs: the struct era, on the record\n\nCLAUDE.md's singleton line becomes the struct story (default IS Blank,\nadopted 2026-09-03, judged by the rig: creation allocations -42%/-61%,\nzero-heap double/string/date/bool cells); test count 905. The\ncanonical-model design doc's \"revisit before million-row workloads\"\ngets its strike-through and its account: both halves revisited — the\nrepresentation by spike, patch, and branch verdict; the eager\nmaterialization by the parked windowed-space prototype (681 MB -> 2 MB)\nawaiting the area-resolution fusion.\n\nThat sentence, written before wave 1 shipped, called both problems and\ntheir order. Some prophecies keep.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T17:01:58Z",
          "tree_id": "ee45abc46b58f0dc515d34a15cb71482009b1b9d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/3e69dc58aa0c9a0300fe0f43a33218891c36e566"
        },
        "date": 1788456121399,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 65008981.51666669,
            "unit": "ns",
            "range": "± 5877788.412310781"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 24675656.50721154,
            "unit": "ns",
            "range": "± 139476.13459295192"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 17504255.783333335,
            "unit": "ns",
            "range": "± 39796.52299729252"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 5187243.80573694,
            "unit": "ns",
            "range": "± 245047.3212956613"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 2201928.5100446427,
            "unit": "ns",
            "range": "± 22982.678421381093"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 6425601.688058035,
            "unit": "ns",
            "range": "± 15663.724089028503"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 954488.8591796875,
            "unit": "ns",
            "range": "± 8817.067901733397"
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
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788449673188,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 1922062.5592447917,
            "unit": "ns",
            "range": "± 10678.468108958632"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 27523479.489583332,
            "unit": "ns",
            "range": "± 441757.043717631"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788451026837,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 1887288.1440805288,
            "unit": "ns",
            "range": "± 6559.232681853489"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 25593684.789583333,
            "unit": "ns",
            "range": "± 96732.36711854265"
          }
        ]
      },
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
          "distinct": false,
          "id": "37bb6bef3d2e23e9778f5b9e84c650537b11688b",
          "message": "The rig meets the struct: delete the null-fill helper\n\nCanonicalSpaces.Fill pre-filled sparse builders' null slots with Blank\n— meaningless under the struct, where default(CellValue) IS Blank and\n??= on a value type rightly refuses to compile. The compiler was the\ntest; the helper joins SpreadsheetSpace's pre-fill loop in the bin.\n(The rig postdates the spike, so this branch first built it in CI.)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T16:07:53Z",
          "tree_id": "7ee5fa22b9a124939f19ebe64c0fe44407a9744e",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/37bb6bef3d2e23e9778f5b9e84c650537b11688b"
        },
        "date": 1788453187886,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 1966466.04296875,
            "unit": "ns",
            "range": "± 38559.812536800666"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 27605507.370535713,
            "unit": "ns",
            "range": "± 161961.85182625495"
          }
        ]
      },
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
          "id": "3e69dc58aa0c9a0300fe0f43a33218891c36e566",
          "message": "Docs: the struct era, on the record\n\nCLAUDE.md's singleton line becomes the struct story (default IS Blank,\nadopted 2026-09-03, judged by the rig: creation allocations -42%/-61%,\nzero-heap double/string/date/bool cells); test count 905. The\ncanonical-model design doc's \"revisit before million-row workloads\"\ngets its strike-through and its account: both halves revisited — the\nrepresentation by spike, patch, and branch verdict; the eager\nmaterialization by the parked windowed-space prototype (681 MB -> 2 MB)\nawaiting the area-resolution fusion.\n\nThat sentence, written before wave 1 shipped, called both problems and\ntheir order. Some prophecies keep.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T17:01:58Z",
          "tree_id": "ee45abc46b58f0dc515d34a15cb71482009b1b9d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/3e69dc58aa0c9a0300fe0f43a33218891c36e566"
        },
        "date": 1788456121577,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 1928309.5490885417,
            "unit": "ns",
            "range": "± 19319.51661356278"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 26798508.202083334,
            "unit": "ns",
            "range": "± 264343.9224987995"
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
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788449673810,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 1964099.7179129464,
            "unit": "ns",
            "range": "± 13192.67929946629"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 1998857.56640625,
            "unit": "ns",
            "range": "± 19488.314023750143"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 207842.65604654947,
            "unit": "ns",
            "range": "± 323.49239842652236"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 218605.8466045673,
            "unit": "ns",
            "range": "± 349.4978918070785"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 1235181.9893229166,
            "unit": "ns",
            "range": "± 6347.851637437034"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788451027271,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 1932142.2627604166,
            "unit": "ns",
            "range": "± 20858.447347581452"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 1945398.09296875,
            "unit": "ns",
            "range": "± 15330.349956763066"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 207150.82301548548,
            "unit": "ns",
            "range": "± 357.1148395643677"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 235910.57348632812,
            "unit": "ns",
            "range": "± 488.8595541119038"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 1238935.1954520089,
            "unit": "ns",
            "range": "± 7983.0364044679245"
          }
        ]
      },
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
          "distinct": false,
          "id": "37bb6bef3d2e23e9778f5b9e84c650537b11688b",
          "message": "The rig meets the struct: delete the null-fill helper\n\nCanonicalSpaces.Fill pre-filled sparse builders' null slots with Blank\n— meaningless under the struct, where default(CellValue) IS Blank and\n??= on a value type rightly refuses to compile. The compiler was the\ntest; the helper joins SpreadsheetSpace's pre-fill loop in the bin.\n(The rig postdates the spike, so this branch first built it in CI.)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T16:07:53Z",
          "tree_id": "7ee5fa22b9a124939f19ebe64c0fe44407a9744e",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/37bb6bef3d2e23e9778f5b9e84c650537b11688b"
        },
        "date": 1788453188095,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 2032876.3973958334,
            "unit": "ns",
            "range": "± 28031.79861367632"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 2070522.2013020834,
            "unit": "ns",
            "range": "± 29201.731334308366"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 236181.95477701823,
            "unit": "ns",
            "range": "± 2014.0064686936755"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 245592.23932291666,
            "unit": "ns",
            "range": "± 2290.845260474672"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 1333440.4614955357,
            "unit": "ns",
            "range": "± 11559.197260018465"
          }
        ]
      },
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
          "id": "3e69dc58aa0c9a0300fe0f43a33218891c36e566",
          "message": "Docs: the struct era, on the record\n\nCLAUDE.md's singleton line becomes the struct story (default IS Blank,\nadopted 2026-09-03, judged by the rig: creation allocations -42%/-61%,\nzero-heap double/string/date/bool cells); test count 905. The\ncanonical-model design doc's \"revisit before million-row workloads\"\ngets its strike-through and its account: both halves revisited — the\nrepresentation by spike, patch, and branch verdict; the eager\nmaterialization by the parked windowed-space prototype (681 MB -> 2 MB)\nawaiting the area-resolution fusion.\n\nThat sentence, written before wave 1 shipped, called both problems and\ntheir order. Some prophecies keep.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T17:01:58Z",
          "tree_id": "ee45abc46b58f0dc515d34a15cb71482009b1b9d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/3e69dc58aa0c9a0300fe0f43a33218891c36e566"
        },
        "date": 1788456121762,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 1933562.8,
            "unit": "ns",
            "range": "± 20127.769820703656"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 1936719.7114583333,
            "unit": "ns",
            "range": "± 28723.443788358192"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 233360.1021484375,
            "unit": "ns",
            "range": "± 1940.114073802441"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 243127.1758188101,
            "unit": "ns",
            "range": "± 1697.6322878197923"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 1245081.3322916667,
            "unit": "ns",
            "range": "± 13451.14661416706"
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
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788449674447,
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
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788451027742,
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
      },
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
          "distinct": false,
          "id": "37bb6bef3d2e23e9778f5b9e84c650537b11688b",
          "message": "The rig meets the struct: delete the null-fill helper\n\nCanonicalSpaces.Fill pre-filled sparse builders' null slots with Blank\n— meaningless under the struct, where default(CellValue) IS Blank and\n??= on a value type rightly refuses to compile. The compiler was the\ntest; the helper joins SpreadsheetSpace's pre-fill loop in the bin.\n(The rig postdates the spike, so this branch first built it in CI.)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T16:07:53Z",
          "tree_id": "7ee5fa22b9a124939f19ebe64c0fe44407a9744e",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/37bb6bef3d2e23e9778f5b9e84c650537b11688b"
        },
        "date": 1788453188306,
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
      },
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
          "id": "3e69dc58aa0c9a0300fe0f43a33218891c36e566",
          "message": "Docs: the struct era, on the record\n\nCLAUDE.md's singleton line becomes the struct story (default IS Blank,\nadopted 2026-09-03, judged by the rig: creation allocations -42%/-61%,\nzero-heap double/string/date/bool cells); test count 905. The\ncanonical-model design doc's \"revisit before million-row workloads\"\ngets its strike-through and its account: both halves revisited — the\nrepresentation by spike, patch, and branch verdict; the eager\nmaterialization by the parked windowed-space prototype (681 MB -> 2 MB)\nawaiting the area-resolution fusion.\n\nThat sentence, written before wave 1 shipped, called both problems and\ntheir order. Some prophecies keep.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T17:01:58Z",
          "tree_id": "ee45abc46b58f0dc515d34a15cb71482009b1b9d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/3e69dc58aa0c9a0300fe0f43a33218891c36e566"
        },
        "date": 1788456121963,
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
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788449675041,
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
            "value": 526,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 377,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788451028195,
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
            "value": 526,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 377,
            "unit": "bytes"
          }
        ]
      },
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
          "distinct": false,
          "id": "37bb6bef3d2e23e9778f5b9e84c650537b11688b",
          "message": "The rig meets the struct: delete the null-fill helper\n\nCanonicalSpaces.Fill pre-filled sparse builders' null slots with Blank\n— meaningless under the struct, where default(CellValue) IS Blank and\n??= on a value type rightly refuses to compile. The compiler was the\ntest; the helper joins SpreadsheetSpace's pre-fill loop in the bin.\n(The rig postdates the spike, so this branch first built it in CI.)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T16:07:53Z",
          "tree_id": "7ee5fa22b9a124939f19ebe64c0fe44407a9744e",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/37bb6bef3d2e23e9778f5b9e84c650537b11688b"
        },
        "date": 1788453188524,
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
            "value": 379,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 2715,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 523,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 377,
            "unit": "bytes"
          }
        ]
      },
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
          "id": "3e69dc58aa0c9a0300fe0f43a33218891c36e566",
          "message": "Docs: the struct era, on the record\n\nCLAUDE.md's singleton line becomes the struct story (default IS Blank,\nadopted 2026-09-03, judged by the rig: creation allocations -42%/-61%,\nzero-heap double/string/date/bool cells); test count 905. The\ncanonical-model design doc's \"revisit before million-row workloads\"\ngets its strike-through and its account: both halves revisited — the\nrepresentation by spike, patch, and branch verdict; the eager\nmaterialization by the parked windowed-space prototype (681 MB -> 2 MB)\nawaiting the area-resolution fusion.\n\nThat sentence, written before wave 1 shipped, called both problems and\ntheir order. Some prophecies keep.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T17:01:58Z",
          "tree_id": "ee45abc46b58f0dc515d34a15cb71482009b1b9d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/3e69dc58aa0c9a0300fe0f43a33218891c36e566"
        },
        "date": 1788456122146,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 344,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 344,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 376,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 379,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 2715,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 523,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 377,
            "unit": "bytes"
          }
        ]
      }
    ],
    "Tables Memory": [
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
        "date": 1788448658325,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2481795,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 24802360,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 10641398,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 106401797,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 5680883,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 56801486,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 13099,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788449675669,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2481795,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 24801915,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 10641395,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 106401683,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 5680883,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 56801106,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 13082,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788451028645,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2481795,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 24802910,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 10641398,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 106401751,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 5680883,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 56801485,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 13099,
            "unit": "bytes"
          }
        ]
      },
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
          "distinct": false,
          "id": "37bb6bef3d2e23e9778f5b9e84c650537b11688b",
          "message": "The rig meets the struct: delete the null-fill helper\n\nCanonicalSpaces.Fill pre-filled sparse builders' null slots with Blank\n— meaningless under the struct, where default(CellValue) IS Blank and\n??= on a value type rightly refuses to compile. The compiler was the\ntest; the helper joins SpreadsheetSpace's pre-fill loop in the bin.\n(The rig postdates the spike, so this branch first built it in CI.)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T16:07:53Z",
          "tree_id": "7ee5fa22b9a124939f19ebe64c0fe44407a9744e",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/37bb6bef3d2e23e9778f5b9e84c650537b11688b"
        },
        "date": 1788453188754,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2481820,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 24802390,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 10641414,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 106402125,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 6800902,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 68001917,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 13099,
            "unit": "bytes"
          }
        ]
      },
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
          "id": "3e69dc58aa0c9a0300fe0f43a33218891c36e566",
          "message": "Docs: the struct era, on the record\n\nCLAUDE.md's singleton line becomes the struct story (default IS Blank,\nadopted 2026-09-03, judged by the rig: creation allocations -42%/-61%,\nzero-heap double/string/date/bool cells); test count 905. The\ncanonical-model design doc's \"revisit before million-row workloads\"\ngets its strike-through and its account: both halves revisited — the\nrepresentation by spike, patch, and branch verdict; the eager\nmaterialization by the parked windowed-space prototype (681 MB -> 2 MB)\nawaiting the area-resolution fusion.\n\nThat sentence, written before wave 1 shipped, called both problems and\ntheir order. Some prophecies keep.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T17:01:58Z",
          "tree_id": "ee45abc46b58f0dc515d34a15cb71482009b1b9d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/3e69dc58aa0c9a0300fe0f43a33218891c36e566"
        },
        "date": 1788456122342,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2481812,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 24802394,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 10641414,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 106402125,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 6800902,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 68001955,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 13099,
            "unit": "bytes"
          }
        ]
      }
    ],
    "Values Memory": [
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
        "date": 1788448658515,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 96001073,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 78400256,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 12,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 6,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 3,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 6,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 3,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788449676300,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 96001240,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 78400261,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 12,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 6,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 3,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 6,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 3,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788451029069,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 96000360,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 78400296,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 12,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 6,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 3,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 6,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 3,
            "unit": "bytes"
          }
        ]
      },
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
          "distinct": false,
          "id": "37bb6bef3d2e23e9778f5b9e84c650537b11688b",
          "message": "The rig meets the struct: delete the null-fill helper\n\nCanonicalSpaces.Fill pre-filled sparse builders' null slots with Blank\n— meaningless under the struct, where default(CellValue) IS Blank and\n??= on a value type rightly refuses to compile. The compiler was the\ntest; the helper joins SpreadsheetSpace's pre-fill loop in the bin.\n(The rig postdates the spike, so this branch first built it in CI.)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T16:07:53Z",
          "tree_id": "7ee5fa22b9a124939f19ebe64c0fe44407a9744e",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/37bb6bef3d2e23e9778f5b9e84c650537b11688b"
        },
        "date": 1788453188986,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56000437,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 30400166,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 23,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 3,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 6,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 1,
            "unit": "bytes"
          }
        ]
      },
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
          "id": "3e69dc58aa0c9a0300fe0f43a33218891c36e566",
          "message": "Docs: the struct era, on the record\n\nCLAUDE.md's singleton line becomes the struct story (default IS Blank,\nadopted 2026-09-03, judged by the rig: creation allocations -42%/-61%,\nzero-heap double/string/date/bool cells); test count 905. The\ncanonical-model design doc's \"revisit before million-row workloads\"\ngets its strike-through and its account: both halves revisited — the\nrepresentation by spike, patch, and branch verdict; the eager\nmaterialization by the parked windowed-space prototype (681 MB -> 2 MB)\nawaiting the area-resolution fusion.\n\nThat sentence, written before wave 1 shipped, called both problems and\ntheir order. Some prophecies keep.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T17:01:58Z",
          "tree_id": "ee45abc46b58f0dc515d34a15cb71482009b1b9d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/3e69dc58aa0c9a0300fe0f43a33218891c36e566"
        },
        "date": 1788456122529,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56000982,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 30400145,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 23,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 6,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 3,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 6,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 1,
            "unit": "bytes"
          }
        ]
      }
    ],
    "EndToEnd Memory": [
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
        "date": 1788448658702,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 3861681,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 38507735,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788449676970,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 3861681,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 38507735,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788451029549,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 3861681,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 38507735,
            "unit": "bytes"
          }
        ]
      },
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
          "distinct": false,
          "id": "37bb6bef3d2e23e9778f5b9e84c650537b11688b",
          "message": "The rig meets the struct: delete the null-fill helper\n\nCanonicalSpaces.Fill pre-filled sparse builders' null slots with Blank\n— meaningless under the struct, where default(CellValue) IS Blank and\n??= on a value type rightly refuses to compile. The compiler was the\ntest; the helper joins SpreadsheetSpace's pre-fill loop in the bin.\n(The rig postdates the spike, so this branch first built it in CI.)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T16:07:53Z",
          "tree_id": "7ee5fa22b9a124939f19ebe64c0fe44407a9744e",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/37bb6bef3d2e23e9778f5b9e84c650537b11688b"
        },
        "date": 1788453189214,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 3874499,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 38635859,
            "unit": "bytes"
          }
        ]
      },
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
          "id": "3e69dc58aa0c9a0300fe0f43a33218891c36e566",
          "message": "Docs: the struct era, on the record\n\nCLAUDE.md's singleton line becomes the struct story (default IS Blank,\nadopted 2026-09-03, judged by the rig: creation allocations -42%/-61%,\nzero-heap double/string/date/bool cells); test count 905. The\ncanonical-model design doc's \"revisit before million-row workloads\"\ngets its strike-through and its account: both halves revisited — the\nrepresentation by spike, patch, and branch verdict; the eager\nmaterialization by the parked windowed-space prototype (681 MB -> 2 MB)\nawaiting the area-resolution fusion.\n\nThat sentence, written before wave 1 shipped, called both problems and\ntheir order. Some prophecies keep.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T17:01:58Z",
          "tree_id": "ee45abc46b58f0dc515d34a15cb71482009b1b9d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/3e69dc58aa0c9a0300fe0f43a33218891c36e566"
        },
        "date": 1788456122712,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 3874497,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 38635858,
            "unit": "bytes"
          }
        ]
      }
    ],
    "Diagnostics Memory": [
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
        "date": 1788448658898,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 3861681,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 3862699,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 5688,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 4384,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 2152385,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788449677555,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 3861681,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 3862699,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 5688,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 4384,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 2152385,
            "unit": "bytes"
          }
        ]
      },
      {
        "commit": {
          "author": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "committer": {
            "name": "Jason Boyd",
            "username": "jasonmcboyd",
            "email": "jason.boyd.ce@gmail.com"
          },
          "id": "16017b750b8e22d895c32aba953f6dff549436ab",
          "message": "Continuous benchmarking: the Copse rig, stolen faithfully\n\nsrc/Unrect.Benchmarks: 34 benchmarks in six one-class families —\nEngine (layout composites), Strategies (scans and anchors), Tables\n(the ladder at 10k/100k plus binder construction), Values (the\nrepresentation-sensitive family: space construction, accessor and\nequality sweeps), EndToEnd (the investor-IRR document at 400/4,000\ninvestors), Diagnostics (Map vs MapWithDiagnostics, rollback and\nabsorption costs) — over GridSpace-built synthetic fixtures, no\nworkbooks on runners.\n\nWorkflows adapted from copselib/copse-dotnet: per-family matrix legs\n(comparisons never cross the shared-runner CPU lottery), per-CPU\ntestbed recording, gh-pages trend dashboard (master-only), optional\nBencher overlay with branch-vs-master baselining. deploy-dashboard\nsyncs benchmark-dashboard/ to gh-pages.\n\nConventions in docs/benchmarking.md, including the load-bearing rule\ndiscovered while building: one benchmark class per family, because the\nexport is named for the class and the publish step takes the first\nmatch — a split family silently publishes half its rows. Two fixture\nfidelity bugs found by checking outputs rather than timings, fixed:\na sparse fixture whose all-blank rows truncated every scan, and a\nkind-cycle resonance that blanked two columns in every row.\n\nFirst findings on record: the diagnostics channel is free on a clean\nparse (ratio 0.98), and Values.Create_FromInts allocates ~96 MB/op —\nthe number the parked CellValue struct patch exists to move, now with\na trend line waiting to judge it.\n\n906 tests, 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T01:51:32Z",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16017b750b8e22d895c32aba953f6dff549436ab"
        },
        "date": 1788451029979,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 3861683,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 3862699,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 5688,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 4384,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 2152385,
            "unit": "bytes"
          }
        ]
      },
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
          "distinct": false,
          "id": "37bb6bef3d2e23e9778f5b9e84c650537b11688b",
          "message": "The rig meets the struct: delete the null-fill helper\n\nCanonicalSpaces.Fill pre-filled sparse builders' null slots with Blank\n— meaningless under the struct, where default(CellValue) IS Blank and\n??= on a value type rightly refuses to compile. The compiler was the\ntest; the helper joins SpreadsheetSpace's pre-fill loop in the bin.\n(The rig postdates the spike, so this branch first built it in CI.)\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T16:07:53Z",
          "tree_id": "7ee5fa22b9a124939f19ebe64c0fe44407a9744e",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/37bb6bef3d2e23e9778f5b9e84c650537b11688b"
        },
        "date": 1788453189438,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 3874499,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 3875515,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 5688,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 4384,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 2158801,
            "unit": "bytes"
          }
        ]
      },
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
          "id": "3e69dc58aa0c9a0300fe0f43a33218891c36e566",
          "message": "Docs: the struct era, on the record\n\nCLAUDE.md's singleton line becomes the struct story (default IS Blank,\nadopted 2026-09-03, judged by the rig: creation allocations -42%/-61%,\nzero-heap double/string/date/bool cells); test count 905. The\ncanonical-model design doc's \"revisit before million-row workloads\"\ngets its strike-through and its account: both halves revisited — the\nrepresentation by spike, patch, and branch verdict; the eager\nmaterialization by the parked windowed-space prototype (681 MB -> 2 MB)\nawaiting the area-resolution fusion.\n\nThat sentence, written before wave 1 shipped, called both problems and\ntheir order. Some prophecies keep.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T17:01:58Z",
          "tree_id": "ee45abc46b58f0dc515d34a15cb71482009b1b9d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/3e69dc58aa0c9a0300fe0f43a33218891c36e566"
        },
        "date": 1788456122896,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 3874499,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 3875515,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 5688,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 4384,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 2158801,
            "unit": "bytes"
          }
        ]
      }
    ]
  }
}