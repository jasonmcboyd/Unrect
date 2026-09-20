window.BENCHMARK_DATA = {
  "lastUpdate": 1789931369178,
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
          "id": "f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb",
          "message": "Fix the 2-core CI flake: a blocked-borrower proof needs a started borrower\n\nAReachWaitsForAWarmerRatherThanStartingASecondOpenOfTheSameFile failed on\nthe GitHub runner (ef348dd) on \"the wait is counted\": WarmWaitMilliseconds\nwas 0, and 0 was the honest count. The pool's warmers ride Task.Run and\nthe gated arrangement BLOCKS them inside their opens, one pool thread\neach — on a two-core runner that is the entire starting thread pool, so\nthe test's own Task.Run borrower never started until thread injection got\naround to it. Both blocked-ness assertions passed vacuously (not finished\nbecause not scheduled), and by the time the reach ran, the warm reader was\nparked and there was nothing left to wait for.\n\nReproduced under taskset -c 0,1: three failures in four runs before the\nfix, none in six Debug runs plus a Release run after. The fix is\nOnItsOwnThread (TaskCreationOptions.LongRunning) at the four sites that\nassert a borrower is blocked — a dedicated thread starts unconditionally,\nso \"started, and still not finished\" really does mean \"parked inside\nBorrow\". The three sibling sites could only pass vacuously, never fail,\nbut their proofs were the same lie under starvation. The burst tests\nalready stood on structural evidence (SpinUntil on OpensStarted) and are\nuntouched.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T22:37:18Z",
          "tree_id": "c37ffff8e7e618f8d8cdb3778c429c1bd5259fc9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb"
        },
        "date": 1788475701766,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 943859.9670222356,
            "unit": "ns",
            "range": "± 842.0431328270303"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 234064.59924316406,
            "unit": "ns",
            "range": "± 288.2890747292641"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 308026.571492513,
            "unit": "ns",
            "range": "± 500.8613342815659"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 530274.1835123698,
            "unit": "ns",
            "range": "± 811.9788435492129"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 105636.0556553432,
            "unit": "ns",
            "range": "± 121.86667887264402"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 9460623.7875,
            "unit": "ns",
            "range": "± 90631.12037168966"
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
          "id": "10027e9f1d263aac70041f0f7166b186324129e8",
          "message": "Both doors measure a sheet that will not say how big it is\n\nSpreadsheetSpace.Create sized its grid from reader.RowCount/FieldCount and\nsilently yielded an empty space when the reader would not give them — the\none outcome an adapter must not have, and a divergence from the streaming\ndoor, which has measured such sheets since Part 2 step 7. The fill is now\ntwo named siblings behind one dichotomy: ReadDeclared (the original loop,\nunchanged) and ReadMeasured (rows collected at their own width, the widest\nrow wins, absent trailing cells Blank — the same answer Workbook.Measure\ngives). The guard is rowCount <= 0 alone, deliberately mirroring the\nstreaming door so the two can never disagree about the same file.\n\nThe recorded cause was wrong, and is corrected everywhere it appeared: a\nmissing dimension element does not trigger this — ExcelDataReader derives\nboth counts from a pre-scan of the cells on every format it handles. The\nreachable trigger is a sheet with NO valued cell (rows of formatted-but-\nvalueless cells, a pre-formatted export region). Pinned by the committed\nTestData/no-extent.xlsx (dimensionless AND valueless, with the survey's\nRowsMeasured == 4 doubling as the fixture's own guard against a\nregeneration that quietly stops reaching the path) and a both-doors\nidentity test.\n\nRides along, both owner decisions from this session's discussion:\n- MaxReaders: spec §14 Q2 DECIDED — 3 stays and stops being provisional,\n  because no number is right: reader demand is the declaration's monotone-\n  cursor count, unbounded in principle, data-independent in practice, and\n  the ceiling fails gently (Reopens is the counted, named signal to raise\n  it). Sizing guidance added to docs/streaming.md; per-reader economics\n  (~5s CPU per open, position must be walked, reader-per-row is O(n^2))\n  recorded in the spec.\n- Table's header-derived width: spec §14 Q1 DEFERRED, superseding the\n  2026-09-03 yes — the step-8 interleave delivered the lazy win with\n  today's denotation intact, so the K-1 campaign votes before the\n  denotation change is paid for.\n\nSuite 1,382 -> 1,387; gates green in Debug and 2-core Release.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T14:29:21Z",
          "tree_id": "fc431b0954d2e3a5115a177bd1a21d63c169ffae",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/10027e9f1d263aac70041f0f7166b186324129e8"
        },
        "date": 1788533062539,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 1273399.4747596155,
            "unit": "ns",
            "range": "± 3256.9052021380426"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 281261.7595214844,
            "unit": "ns",
            "range": "± 965.8597427933928"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 313730.40594951925,
            "unit": "ns",
            "range": "± 321.94852077555726"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 592461.2146183894,
            "unit": "ns",
            "range": "± 1434.1390734384167"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 108062.34780883789,
            "unit": "ns",
            "range": "± 338.6372869904981"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 13880713.251041668,
            "unit": "ns",
            "range": "± 95472.78681980725"
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
          "id": "c01531cec6968e544acc578291244292172a00a5",
          "message": "Docs: Part 3 deferred on principle, and .Sized's composite role stated honestly\n\nSpec §13 gains the Part 3 row (bound-aware composite placement): the\nengine's remaining greed sorted into one necessary force (Repeat items —\nthe item's existence is the question), one free force (post-Project\nconsumption, amortised by the root's accounting), and one debt (composite\nchild placement, whose questions have lazy answers nobody asks for).\nDeferred until the first tall sized composite pays the debt — sized\ncomposites in the corpus are short header bands, where settling eagerly\ncosts nothing. The K-1 campaign is the likely judge; the census pin is the\ntripwire.\n\ndocs/streaming.md stops saying \"put the .Sized on the leaf\" as if it were\na law: a sized composite is a legitimate spelling with no leaf equivalent\n— a composite has no intrinsic extent, and the declared band is what\nscopes its internal seeks and settles its consumption.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T15:37:53Z",
          "tree_id": "6188ce68af3130bfba604f38845b0c515958cb34",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/c01531cec6968e544acc578291244292172a00a5"
        },
        "date": 1788537628448,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 1307589.5005208333,
            "unit": "ns",
            "range": "± 16624.447289978205"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 266878.91411132814,
            "unit": "ns",
            "range": "± 4149.778593553969"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 309909.04042271205,
            "unit": "ns",
            "range": "± 1031.463033962792"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 647877.0159040178,
            "unit": "ns",
            "range": "± 6205.464018138521"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 107552.81268310547,
            "unit": "ns",
            "range": "± 83.59251064520937"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 13924500.870404411,
            "unit": "ns",
            "range": "± 279892.4114123886"
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
          "id": "2d73985e95c70f51a2b26d7dc98c3936f1f52d5d",
          "message": "Retention: the live-set floor for the interning change, with the target on the chart\n\nAn eighth CI leg that is not a BenchmarkDotNet family: interning reduces\nRETAINED bytes, not allocations (a duplicate string is allocated by the\nreader before the adapter sees it and dies young after dedup), so the\nAllocated column cannot see it — and retention is deterministic, so it\nneeds no statistical engine. A one-shot job measures live bytes with the\nresult held, emits the same JSON document the rig already stores, and\nrides the same workflow and dashboard as everything else.\n\nBuilding it surfaced two facts worth more than the plumbing:\n\n- The eager door's duplication depends on how the file spells its text.\n  Shared-string cells come back already deduped (the reader returns its\n  table's own instance); inline strings and formula-result cells\n  materialise fresh per cell. A real Excel export is both (the local K-1:\n  9,049 text cells, 2,876 values, 4,016 instances — the formula results\n  are the duplicated half). The family brackets it, and the shared-string\n  row is the priced TARGET: the same cells read 112.0 MB duplicated vs\n  58.2 MB deduped, so ~48% is what a complete eager interner is worth on\n  this shape — short of that is unfinished, not failed.\n- The first fixture boxed decimals a real read never produces (16 MB of\n  boxes in a retained-bytes measurement); the retention fixtures now\n  yield doubles like a reader does. StreamingSpaces is deliberately\n  untouched — changing it would re-baseline that family's history.\n\nScenarios exercise the real seams the interning change will live in: the\neager rows go through SpreadsheetSpace.Create over generated workbooks\n(RetentionWorkbooks: a minimal hand-rolled OOXML writer, no new package;\nthe one deliberate exception to the no-workbooks rule, recorded in\ndocs/benchmarking.md), the streaming rows through the store's chunk fill.\nFloor: eager space held 106.8 MB, results held 82.1 MB both doors\n(byte-identical — streaming's promise stated in the metric), controls\nbyte-identical to their duplicated twins by fixed-width padding. Leg\nruns ~65s, the shortest in the matrix.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T16:47:38Z",
          "tree_id": "0c756ae6dd2d4f17cd84e585c99d7d3ae08fd409",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/2d73985e95c70f51a2b26d7dc98c3936f1f52d5d"
        },
        "date": 1788542159211,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 1346003.789341518,
            "unit": "ns",
            "range": "± 17325.027216562623"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 284373.5279259315,
            "unit": "ns",
            "range": "± 3611.553358738709"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 308236.8565848214,
            "unit": "ns",
            "range": "± 481.3513901294236"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 642570.9265950521,
            "unit": "ns",
            "range": "± 7153.810520462548"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 107295.43564547025,
            "unit": "ns",
            "range": "± 94.59030633566061"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 14501139.392708333,
            "unit": "ns",
            "range": "± 231982.53961076154"
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
          "id": "eddc5d17c38c715f41cd95d041452deb66f8354c",
          "message": "Interning: equal text shares one instance through both doors\n\nAdapter-level string interning — a find-my-twin table at each door's\nadapt seam. The eager door threads a per-Create-call HashSet through both\nfill paths (one instance per distinct value across every sheet of the\ncall); the streaming door hangs one capped ConcurrentDictionary on the\nWorkbook, plumbed into every store's chunk fill, so a chase reader's\nre-parse dedupes against the first parse. Strings only: every other kind\nis inline in the 24-byte struct or unreachable from the spreadsheet door.\n\nThe win is retention, not allocation — the duplicate is allocated by the\nreader before the adapter sees it and dies in gen0 after dedup, which is\nwhy the Retention leg is the judge and MemoryDiagnoser is blind to it.\nMeasured against the committed floor: eager space held 106.8 -> 55.5 MB,\nlanding on the priced shared-string target to the byte; held results\n82.1 -> 30.8 MB, byte-identical across doors; all three unique controls\nflat to the byte; wall time noise on the 1M-row parse.\n\nThe cap (WorkbookOptions.MaxInternedStrings, default 65,536; 0 = off)\nand the 256-char length guard bound what the book-lifetime table can\npin. Documented as a two-way knob: a full table costs its entries for\nthe book's life — some 40 MB at the default cap — so it turns DOWN, to\n0, for known-unique text, and the docs say so at every site (the rig is\nstructurally blind to the table's own live set: readings are taken with\nthe book closed). Workbook.InterningStatistics reports hits, distinct,\nand estimated bytes (64-bit layout, exact for it; Hits counts fills, so\na reloaded chunk counts again — read against ChunkReloads).\n\nExcelDataReader fact on the record: shared-string cells arrive\npre-deduped from the reader's own SST; the duplication this kills comes\nfrom inline-string cells, formula-result cells, and .xls. Pinned by 36\ntests including a cross-door sharing differential, mutation-checked both\ndirections, and a WeakReference proof that Dispose releases the strings.\nSuite 1,423.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T19:19:56Z",
          "tree_id": "fea5150f427d5b65cf652662879afb3b470547f2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/eddc5d17c38c715f41cd95d041452deb66f8354c"
        },
        "date": 1788556078238,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 1248897.4529854911,
            "unit": "ns",
            "range": "± 7452.5219824084525"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 276988.36311848956,
            "unit": "ns",
            "range": "± 4184.931528616032"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 314668.888671875,
            "unit": "ns",
            "range": "± 691.0191451241412"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 643710.9020182291,
            "unit": "ns",
            "range": "± 10587.442133263221"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 108576.3515625,
            "unit": "ns",
            "range": "± 81.95637496697884"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 13859309.707291666,
            "unit": "ns",
            "range": "± 62235.65862573716"
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
          "id": "16fef6aa0fb6978c79559b92910716e6ad44c995",
          "message": "netstandard2.0: the packages install on .NET Framework, one source, no forks\n\nAll four libraries multi-target netstandard2.0;netstandard2.1. The rule\nthat made it clean: no #if anywhere — netstandard2.1-only constructs are\nremoved rather than branched on, so both targets compile one source and\ncannot drift.\n\nThe structural change is de-DIMing the incremental calculus. .NET\nFramework's runtime cannot dispatch a default interface member, so the\ndefinitional folds move from DIM bodies to the static Unrect.Core.Scans\n(Fold/FoldSize/FoldArea) and internal ColumnAccumulators.Fold, with every\nimplementation delegating in one line. The guarantee shifts and every doc\nthat claimed it says so honestly: \"eager and lazy cannot disagree by\nconstruction\" is now \"cannot disagree, pinned by the fold-identity suite\"\n— IncrementalStrategyTests asserts SelectRows == a hand-written fold for\nevery factory, and a new incremental strategy carries the obligation to\njoin that theory data. Corollary recorded in capability-seam-notes: the\nDIM \"safety net\" for evolving ISpace is withdrawn — a member added to a\npublished Core interface is now a breaking change with no escape hatch;\ntype-testing is the whole capability recipe.\n\nThe rest the compiler found: System.HashCode (Core hand-rolls its combine\nrather than take Microsoft.Bcl.HashCode — the bundling would have\nsuppressed the dependency from the nuspec and thrown FileNotFoundException\non Framework; both packages stay at zero new dependencies), the\nDictionary-from-pairs constructor, and HashSet<string>.TryGetValue (the\neager intern table is a Dictionary now; the comment that argued HashSet\nwas the honest structure records why it changed). The two polyfills are\nunconditional because neither netstandard has the types; a net5.0+ target\nis what would force an #if.\n\nTests multi-target net8.0;net48 on Windows only (OS-conditioned csproj,\nso Linux CI runs exactly what it ran — verified against all three\nworkflows). Two portable shims replace .NET-5+/6+ APIs the suite used:\nWithTimeout for Task.WaitAsync(TimeSpan), ByReference for\nReferenceEqualityComparer. The full suite has run green on the Framework\nCLR itself: 1,423 on net48 against the netstandard2.0 build, ExcelDataReader's\nown Framework assembly included — verified by the owner on Windows.\nBoth nupkgs verified to carry lib/netstandard2.0 and lib/netstandard2.1.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-05T17:36:21Z",
          "tree_id": "67b64bc3184652344901c8a243f8ce1256d858b9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16fef6aa0fb6978c79559b92910716e6ad44c995"
        },
        "date": 1788630322273,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 1195445.0048828125,
            "unit": "ns",
            "range": "± 844.8933528184418"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 263169.77678571426,
            "unit": "ns",
            "range": "± 547.7672575685309"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 323368.8993326823,
            "unit": "ns",
            "range": "± 224.74225276446506"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 587142.1451171875,
            "unit": "ns",
            "range": "± 624.3060852747094"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 99383.80214280348,
            "unit": "ns",
            "range": "± 60.872229540949895"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 13725519.802884616,
            "unit": "ns",
            "range": "± 32100.11603834277"
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
          "id": "e1d21c2d706b054d252bbea3a996b97f6f7bfc4b",
          "message": "Hygiene: .gitattributes and editorconfig trim — line endings become a decision\n\nThe repo had no .gitattributes, so line endings were whatever tool\ntouched a file last — four .linq files sat CRLF in the index beside an\nLF corpus, and the vocabulary respell nearly flipped three of them\nwholesale (caught in review; a 10-line diff had become 366). One rule\nnow: the index holds LF (* text=auto); .linq and .sln check out CRLF\nbecause their native editors are Windows-bound; spreadsheets and images\nare explicitly binary. This commit carries the one-time renormalization\nso the flip lives here, deliberately, and nowhere else.\n\n.editorconfig gains trailing-whitespace trimming, with markdown exempt\n(a trailing double-space is a hard line break).\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-06T00:49:13Z",
          "tree_id": "ce25b3593543c4145d69070ff009c9f67ff1aa09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/e1d21c2d706b054d252bbea3a996b97f6f7bfc4b"
        },
        "date": 1788712512044,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 1385313.9666466345,
            "unit": "ns",
            "range": "± 37894.893001742064"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 277890.5498453776,
            "unit": "ns",
            "range": "± 7128.626295831257"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 309930.13420222356,
            "unit": "ns",
            "range": "± 550.3178692028774"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 830064.1730608259,
            "unit": "ns",
            "range": "± 9546.086820846842"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 105152.72922926683,
            "unit": "ns",
            "range": "± 266.62563803701147"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 13663396.039930556,
            "unit": "ns",
            "range": "± 381773.07958637574"
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
          "id": "bf1fc2851544d9f87ffb3b76df4fa0b714780243",
          "message": "Merge experiment/typed-spaces: the projection model\n\nThe deepest cut since wave 2: what was called Shape is a projection; the\nspace and its subspaces are the shapes. Seven phases plus pre-merge\ncleanup (docs/design/projection-model-spec.md, judgment record in §10):\nthe modifier doubling killed (43 -> 6 irreducible), IShape -> IProjection\nwith namespace Unrect.Projections, the capability stack (IFormulaSpace,\nCapability<T>() transport, the slicing law, shared-formula\nreconstruction), OrBlank and the eachRow slot, the CaptionMap bind, the\nscoped entry Over<TSpace>() and MapWorkbook, phase-7 acceptance with the\nfourteen recorded refusals, and the three-sweep cleanup.\n\nSuite 1,709 green (net8.0 and net48); both library TFMs 0 warnings.\nBreaking: the rename, the Table family, the SpreadsheetSpace shell\nretirement, and the entries.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-07T23:11:42Z",
          "tree_id": "d091a9f7437fb1ec8d2b35119ab628b2a54047a3",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bf1fc2851544d9f87ffb3b76df4fa0b714780243"
        },
        "date": 1788823319749,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 871320.63507952,
            "unit": "ns",
            "range": "± 24299.755102489704"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 203118.21534559462,
            "unit": "ns",
            "range": "± 4225.982442062531"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 209200.69792829241,
            "unit": "ns",
            "range": "± 2406.3415112497573"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 455019.5196668837,
            "unit": "ns",
            "range": "± 9430.374345569593"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 56304.80897623698,
            "unit": "ns",
            "range": "± 810.7724926239663"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 7459148.407291667,
            "unit": "ns",
            "range": "± 40805.91183703567"
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
          "id": "4a44b787ac6b8b6fa5c487623e316bfe0e364292",
          "message": "Presence semantics and three hygiene fixes: the zero disambiguated\n\nThe algebra's highest-priority open problem (v0.4 §7/§14.1): geometric\nzero carried four meanings. The internal Presence { Read, Empty,\nAbsorbed } now rides beside Consumed on the engine's results — stamped\nat tolerance boundaries (Absorbed), settled-at-zero extents (Empty, read\noff the SETTLED extent so evaluation order and doors agree by\nconstruction), joined through layouts (Read if any child Read, else\nEmpty), defaulting Read. The internal epsilon (NothingProjection) makes\nthe flow-unit law stateable: VerticalFlow is a monoid at L3 for\nsuccessful readings, boundaries pinned.\n\nThe law, earned the hard way: presence explains a stop; the extent\ndecides one. The first implementation let presence decide the repeat\nguard; the law-tests caught a real L1/L2 change on\nVerticalRepeat(item.Optional().Sized(...)) — an Absorbed boundary under\na declared area legitimately consumes it and always kept the run going.\nThe guard is byte-identical to before; presence powers only the new\nteaching Info when a run ends at an absorbed item (D2), and the caught\nshape is pinned as the compatibility-law section of PresenceLawTests.\n\nDesign record: docs/design/presence-and-unit-spec.md (DECIDED, with the\nD5 amendment trail).\n\nAlso, with pins: ChoiceProjection.Summarise renders nested aggregates\ndepth-indented with one location per line; MinOffset() is one canonical\ninstance (HasDeclaredOffset correct by construction); the content-\nmatching rule reduced to one implementation (CellMatching primitives,\nTableView's dictionary keyed on TextComparer; CaptionComparer's\ndeliberate divergence untouched). spike/Phase1Probe removed (a husk).\n\nSuite 1,820 -> 1,862 green; both library TFMs 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-09T15:34:18Z",
          "tree_id": "883452644caa55f06a0f9c2d530ee9c0cece2b92",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4a44b787ac6b8b6fa5c487623e316bfe0e364292"
        },
        "date": 1788969079581,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 962863.9478665865,
            "unit": "ns",
            "range": "± 3788.2430614684167"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 220461.25729166667,
            "unit": "ns",
            "range": "± 1682.3703339003869"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 243224.89076450892,
            "unit": "ns",
            "range": "± 134.3656478511538"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 473033.3479817708,
            "unit": "ns",
            "range": "± 2209.3430237105567"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 77289.41880446214,
            "unit": "ns",
            "range": "± 59.65011529815012"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 11446572.613715278,
            "unit": "ns",
            "range": "± 205391.64392930403"
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
          "id": "d06708df60e381758b11dde07836c674565409b6",
          "message": "Merge experiment/record-primitive: streaming composites over a bound, the band tiler, Table composed from primitives\n\nThe labels-as-context primitives (ColumnLabels, WithColumnLabels, Record),\nSkipToFirstNonBlankCell, and the uniform offset law; the engine streaming a\ndiscovered bound through a composite (TailSpace, a bound-aware Exceeds, lazy\nflow and repeat slices); VerticalBands/HorizontalBands, the tiler — a\nrepeating fixed-dimension space, distinct from the pattern repeat;\nTable(headerRows, eachRow) composed over the tiler under a UnitProjection,\nwith AsScaffolding and a mark-driven path fold giving it the leaf's own\ndiagnostics; ColumnLabels self-contained, its header parse the one home in\nLabelMap.FromHeader; and the repeat returned to a pure walk, onBlank living\non the tiler alone.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-14T17:43:35Z",
          "tree_id": "1c80a0bed7aeb311bb6e1bdb0933ac5a05562434",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/d06708df60e381758b11dde07836c674565409b6"
        },
        "date": 1789408531942,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 1262189.2859375,
            "unit": "ns",
            "range": "± 8711.206252166216"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 317644.5191080729,
            "unit": "ns",
            "range": "± 3157.884353868536"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 244963.71856219953,
            "unit": "ns",
            "range": "± 978.0092642959728"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 688965.1098632812,
            "unit": "ns",
            "range": "± 6849.879177017001"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 81505.16843959263,
            "unit": "ns",
            "range": "± 292.1614216452988"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 22672191.198660713,
            "unit": "ns",
            "range": "± 33921.79297418954"
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
          "id": "33f1afa3496afb1fabd2e0242197d1c5d9889568",
          "message": "Merge experiment/point-and-line: the Point substrate — one vocabulary over any ISpace, planes and points as the locators, the kinds in Unrect.Spreadsheets\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-16T04:05:17Z",
          "tree_id": "09d4c757d0f1502df340fb2a2481c31f78e8d8b2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/33f1afa3496afb1fabd2e0242197d1c5d9889568"
        },
        "date": 1789531987132,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 1756096.8190569195,
            "unit": "ns",
            "range": "± 8030.393315833669"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 549372.2880483774,
            "unit": "ns",
            "range": "± 1042.796829738943"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 454106.14188058034,
            "unit": "ns",
            "range": "± 1062.1630859129766"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 930229.7663736979,
            "unit": "ns",
            "range": "± 3825.0890972134066"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 133347.87198893228,
            "unit": "ns",
            "range": "± 170.40501472608767"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 14364354.085416667,
            "unit": "ns",
            "range": "± 90712.64290832733"
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
          "id": "0f34f159151554afcc0555f7afc83fdd486b8c06",
          "message": "Merge experiment/point-follow-ups: Extents into Plane, Unrect.Interactive, and the typed-predicate lift\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-17T02:37:19Z",
          "tree_id": "cfa1d003f75555e9d6f330d316cdd949a1da2b25",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/0f34f159151554afcc0555f7afc83fdd486b8c06"
        },
        "date": 1789613135649,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 820477.1328125,
            "unit": "ns",
            "range": "± 15166.140082512868"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 273429.0503540039,
            "unit": "ns",
            "range": "± 5155.144884039172"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 225449.4067545573,
            "unit": "ns",
            "range": "± 3911.3836120487913"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 429047.8026012074,
            "unit": "ns",
            "range": "± 13262.491450882571"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 63912.35034649189,
            "unit": "ns",
            "range": "± 517.3919155434099"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 5180270.985514323,
            "unit": "ns",
            "range": "± 203751.55354333832"
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
          "id": "717d1ea3b61c226c608194aae5ff909fd4603455",
          "message": "Packaging: Unrect.Interactive ships as its own package\n\nThe project was already packable — id, description, tags, and a\ndependency on exactly Unrect and Unrect.Spreadsheets at the same MinVer\nversion — but the release workflow packed and asserted two packages, and\nthe README and build props named two. Now three.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T01:12:25Z",
          "tree_id": "f01ec5a705245aededd07d5455e7b03ad4f8880d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/717d1ea3b61c226c608194aae5ff909fd4603455"
        },
        "date": 1789796480040,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 5082059.899553572,
            "unit": "ns",
            "range": "± 54861.81651206726"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 2743867653.733333,
            "unit": "ns",
            "range": "± 21511444.996773534"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 1889762786.0714285,
            "unit": "ns",
            "range": "± 6661978.390090641"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 3801076.8098958335,
            "unit": "ns",
            "range": "± 57607.77653049564"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 210081813.7142857,
            "unit": "ns",
            "range": "± 419769.5548210949"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 19461809.260416668,
            "unit": "ns",
            "range": "± 39607.34693938164"
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
          "id": "4818c36f9e9b0c2b75a4093dbbce6981329fd14b",
          "message": "Merge feature/scaffold-shapes: ScaffoldRecord and ScaffoldClass, on a sheet and as leaves\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T15:35:04Z",
          "tree_id": "d48076b68bb0a93dc6ab6797b10f0ead882156a7",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4818c36f9e9b0c2b75a4093dbbce6981329fd14b"
        },
        "date": 1789847936044,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 6481187.342633928,
            "unit": "ns",
            "range": "± 40574.140298288374"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 3252711151.4,
            "unit": "ns",
            "range": "± 26905787.983636923"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 2332782366.071429,
            "unit": "ns",
            "range": "± 1959716.9256726245"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 4523384.653645833,
            "unit": "ns",
            "range": "± 40293.453728200264"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 274192946.6923077,
            "unit": "ns",
            "range": "± 336456.69736067933"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 22707968.10267857,
            "unit": "ns",
            "range": "± 59814.84900374497"
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
          "id": "bd027f2136cee944da88c1eae17a06b14a05579d",
          "message": "Merge feature/bind-from-row: Table<T> fills a member from the row - Column(member, row => ...)\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T19:49:16Z",
          "tree_id": "88c877501aeddcefb436f136842b9904cda55a09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bd027f2136cee944da88c1eae17a06b14a05579d"
        },
        "date": 1789863950779,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 6461626.988541666,
            "unit": "ns",
            "range": "± 17482.091646599503"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 3262270869.8,
            "unit": "ns",
            "range": "± 24904537.441815373"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 2316092620.3333335,
            "unit": "ns",
            "range": "± 1885276.296097648"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 4896206.452083333,
            "unit": "ns",
            "range": "± 47646.98227922682"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 267522327.1,
            "unit": "ns",
            "range": "± 368422.02113280166"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 22512244.183333334,
            "unit": "ns",
            "range": "± 72990.21382853741"
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
          "id": "96b946d5920955eb81be3eab46697a3e4639e2e4",
          "message": "Merge feature/header-bands: headers of several rows, columns addressed by path, flat binding over bands\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T03:17:51Z",
          "tree_id": "33ccb96a2f1d1cb7795dde522c6c586ee8ce479a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/96b946d5920955eb81be3eab46697a3e4639e2e4"
        },
        "date": 1789888949438,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 5584748.771033654,
            "unit": "ns",
            "range": "± 25912.471202873257"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 2818159564.2,
            "unit": "ns",
            "range": "± 23416095.078063704"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 1885972316.357143,
            "unit": "ns",
            "range": "± 1677014.8685133543"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 3956627.1189903845,
            "unit": "ns",
            "range": "± 20080.679688597043"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 213799670.625,
            "unit": "ns",
            "range": "± 5353984.713108946"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 19899664.910714287,
            "unit": "ns",
            "range": "± 57501.47989654001"
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
          "id": "191ad7e666be18cd9916eb0132108e83e54bd6db",
          "message": "Open questions: IsText is the wrong shape - what it is for, the better shape, and the larger question of facets, recorded and not implemented\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T04:23:54Z",
          "tree_id": "5e6c0e37d52a9f501293d06ad42b1a125ed81250",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/191ad7e666be18cd9916eb0132108e83e54bd6db"
        },
        "date": 1789931366275,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 4590605.879340278,
            "unit": "ns",
            "range": "± 209232.3547392277"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 2591153872.4666667,
            "unit": "ns",
            "range": "± 36059001.652071916"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 1489108122.6666667,
            "unit": "ns",
            "range": "± 22264106.470164757"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 3355244.7918526786,
            "unit": "ns",
            "range": "± 53742.84281983721"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 165488582.70967743,
            "unit": "ns",
            "range": "± 5045975.581268618"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 18660318.5472973,
            "unit": "ns",
            "range": "± 619522.7510613274"
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
          "id": "f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb",
          "message": "Fix the 2-core CI flake: a blocked-borrower proof needs a started borrower\n\nAReachWaitsForAWarmerRatherThanStartingASecondOpenOfTheSameFile failed on\nthe GitHub runner (ef348dd) on \"the wait is counted\": WarmWaitMilliseconds\nwas 0, and 0 was the honest count. The pool's warmers ride Task.Run and\nthe gated arrangement BLOCKS them inside their opens, one pool thread\neach — on a two-core runner that is the entire starting thread pool, so\nthe test's own Task.Run borrower never started until thread injection got\naround to it. Both blocked-ness assertions passed vacuously (not finished\nbecause not scheduled), and by the time the reach ran, the warm reader was\nparked and there was nothing left to wait for.\n\nReproduced under taskset -c 0,1: three failures in four runs before the\nfix, none in six Debug runs plus a Release run after. The fix is\nOnItsOwnThread (TaskCreationOptions.LongRunning) at the four sites that\nassert a borrower is blocked — a dedicated thread starts unconditionally,\nso \"started, and still not finished\" really does mean \"parked inside\nBorrow\". The three sibling sites could only pass vacuously, never fail,\nbut their proofs were the same lie under starvation. The burst tests\nalready stood on structural evidence (SpinUntil on OpensStarted) and are\nuntouched.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T22:37:18Z",
          "tree_id": "c37ffff8e7e618f8d8cdb3778c429c1bd5259fc9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb"
        },
        "date": 1788475702661,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 374193.86832682294,
            "unit": "ns",
            "range": "± 370.8943234580414"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 373917.1791428786,
            "unit": "ns",
            "range": "± 716.0797401461543"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 239585.18111165366,
            "unit": "ns",
            "range": "± 132.5176659513383"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 2228937.911848958,
            "unit": "ns",
            "range": "± 2799.516403052259"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 2447497.45703125,
            "unit": "ns",
            "range": "± 3778.0993223238343"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 2526902.421875,
            "unit": "ns",
            "range": "± 4146.051724256944"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1035683.824358259,
            "unit": "ns",
            "range": "± 1376.308572273359"
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
          "id": "10027e9f1d263aac70041f0f7166b186324129e8",
          "message": "Both doors measure a sheet that will not say how big it is\n\nSpreadsheetSpace.Create sized its grid from reader.RowCount/FieldCount and\nsilently yielded an empty space when the reader would not give them — the\none outcome an adapter must not have, and a divergence from the streaming\ndoor, which has measured such sheets since Part 2 step 7. The fill is now\ntwo named siblings behind one dichotomy: ReadDeclared (the original loop,\nunchanged) and ReadMeasured (rows collected at their own width, the widest\nrow wins, absent trailing cells Blank — the same answer Workbook.Measure\ngives). The guard is rowCount <= 0 alone, deliberately mirroring the\nstreaming door so the two can never disagree about the same file.\n\nThe recorded cause was wrong, and is corrected everywhere it appeared: a\nmissing dimension element does not trigger this — ExcelDataReader derives\nboth counts from a pre-scan of the cells on every format it handles. The\nreachable trigger is a sheet with NO valued cell (rows of formatted-but-\nvalueless cells, a pre-formatted export region). Pinned by the committed\nTestData/no-extent.xlsx (dimensionless AND valueless, with the survey's\nRowsMeasured == 4 doubling as the fixture's own guard against a\nregeneration that quietly stops reaching the path) and a both-doors\nidentity test.\n\nRides along, both owner decisions from this session's discussion:\n- MaxReaders: spec §14 Q2 DECIDED — 3 stays and stops being provisional,\n  because no number is right: reader demand is the declaration's monotone-\n  cursor count, unbounded in principle, data-independent in practice, and\n  the ceiling fails gently (Reopens is the counted, named signal to raise\n  it). Sizing guidance added to docs/streaming.md; per-reader economics\n  (~5s CPU per open, position must be walked, reader-per-row is O(n^2))\n  recorded in the spec.\n- Table's header-derived width: spec §14 Q1 DEFERRED, superseding the\n  2026-09-03 yes — the step-8 interleave delivered the lazy win with\n  today's denotation intact, so the K-1 campaign votes before the\n  denotation change is paid for.\n\nSuite 1,382 -> 1,387; gates green in Debug and 2-core Release.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T14:29:21Z",
          "tree_id": "fc431b0954d2e3a5115a177bd1a21d63c169ffae",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/10027e9f1d263aac70041f0f7166b186324129e8"
        },
        "date": 1788533063727,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 1332175.9207389988,
            "unit": "ns",
            "range": "± 117564.92501436928"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 1120149.7840246775,
            "unit": "ns",
            "range": "± 51301.03954080077"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 320264.1707589286,
            "unit": "ns",
            "range": "± 297.38166950931844"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 2785476.7340959823,
            "unit": "ns",
            "range": "± 4873.622380510542"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 3325361.4296875,
            "unit": "ns",
            "range": "± 25909.029917480784"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 3972605.4122916665,
            "unit": "ns",
            "range": "± 199460.46451643918"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1593382.947544643,
            "unit": "ns",
            "range": "± 1916.1591141937213"
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
          "id": "c01531cec6968e544acc578291244292172a00a5",
          "message": "Docs: Part 3 deferred on principle, and .Sized's composite role stated honestly\n\nSpec §13 gains the Part 3 row (bound-aware composite placement): the\nengine's remaining greed sorted into one necessary force (Repeat items —\nthe item's existence is the question), one free force (post-Project\nconsumption, amortised by the root's accounting), and one debt (composite\nchild placement, whose questions have lazy answers nobody asks for).\nDeferred until the first tall sized composite pays the debt — sized\ncomposites in the corpus are short header bands, where settling eagerly\ncosts nothing. The K-1 campaign is the likely judge; the census pin is the\ntripwire.\n\ndocs/streaming.md stops saying \"put the .Sized on the leaf\" as if it were\na law: a sized composite is a legitimate spelling with no leaf equivalent\n— a composite has no intrinsic extent, and the declared band is what\nscopes its internal seeks and settles its consumption.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T15:37:53Z",
          "tree_id": "6188ce68af3130bfba604f38845b0c515958cb34",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/c01531cec6968e544acc578291244292172a00a5"
        },
        "date": 1788537629181,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 892472.4880719866,
            "unit": "ns",
            "range": "± 3770.7625112615096"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 865834.60078125,
            "unit": "ns",
            "range": "± 10114.05218223816"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 307628.42693219864,
            "unit": "ns",
            "range": "± 259.262546150526"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 2770471.802734375,
            "unit": "ns",
            "range": "± 13057.051291626973"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 3234241.2170758927,
            "unit": "ns",
            "range": "± 10203.727320747214"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 3682464.8577008927,
            "unit": "ns",
            "range": "± 40350.695902064275"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1596455.0910993305,
            "unit": "ns",
            "range": "± 4147.305986590257"
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
          "id": "2d73985e95c70f51a2b26d7dc98c3936f1f52d5d",
          "message": "Retention: the live-set floor for the interning change, with the target on the chart\n\nAn eighth CI leg that is not a BenchmarkDotNet family: interning reduces\nRETAINED bytes, not allocations (a duplicate string is allocated by the\nreader before the adapter sees it and dies young after dedup), so the\nAllocated column cannot see it — and retention is deterministic, so it\nneeds no statistical engine. A one-shot job measures live bytes with the\nresult held, emits the same JSON document the rig already stores, and\nrides the same workflow and dashboard as everything else.\n\nBuilding it surfaced two facts worth more than the plumbing:\n\n- The eager door's duplication depends on how the file spells its text.\n  Shared-string cells come back already deduped (the reader returns its\n  table's own instance); inline strings and formula-result cells\n  materialise fresh per cell. A real Excel export is both (the local K-1:\n  9,049 text cells, 2,876 values, 4,016 instances — the formula results\n  are the duplicated half). The family brackets it, and the shared-string\n  row is the priced TARGET: the same cells read 112.0 MB duplicated vs\n  58.2 MB deduped, so ~48% is what a complete eager interner is worth on\n  this shape — short of that is unfinished, not failed.\n- The first fixture boxed decimals a real read never produces (16 MB of\n  boxes in a retained-bytes measurement); the retention fixtures now\n  yield doubles like a reader does. StreamingSpaces is deliberately\n  untouched — changing it would re-baseline that family's history.\n\nScenarios exercise the real seams the interning change will live in: the\neager rows go through SpreadsheetSpace.Create over generated workbooks\n(RetentionWorkbooks: a minimal hand-rolled OOXML writer, no new package;\nthe one deliberate exception to the no-workbooks rule, recorded in\ndocs/benchmarking.md), the streaming rows through the store's chunk fill.\nFloor: eager space held 106.8 MB, results held 82.1 MB both doors\n(byte-identical — streaming's promise stated in the metric), controls\nbyte-identical to their duplicated twins by fixed-width padding. Leg\nruns ~65s, the shortest in the matrix.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T16:47:38Z",
          "tree_id": "0c756ae6dd2d4f17cd84e585c99d7d3ae08fd409",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/2d73985e95c70f51a2b26d7dc98c3936f1f52d5d"
        },
        "date": 1788542160494,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 963477.2245107323,
            "unit": "ns",
            "range": "± 111405.51048123329"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 1070869.0748242186,
            "unit": "ns",
            "range": "± 155718.0360881759"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 247071.1071026142,
            "unit": "ns",
            "range": "± 479.41559997891"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 2188615.0850360575,
            "unit": "ns",
            "range": "± 6103.604691368299"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 2485002.3141741073,
            "unit": "ns",
            "range": "± 9965.111883701258"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 3093533.4194957386,
            "unit": "ns",
            "range": "± 131526.57714078037"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1235341.212890625,
            "unit": "ns",
            "range": "± 2015.3393685804056"
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
          "id": "eddc5d17c38c715f41cd95d041452deb66f8354c",
          "message": "Interning: equal text shares one instance through both doors\n\nAdapter-level string interning — a find-my-twin table at each door's\nadapt seam. The eager door threads a per-Create-call HashSet through both\nfill paths (one instance per distinct value across every sheet of the\ncall); the streaming door hangs one capped ConcurrentDictionary on the\nWorkbook, plumbed into every store's chunk fill, so a chase reader's\nre-parse dedupes against the first parse. Strings only: every other kind\nis inline in the 24-byte struct or unreachable from the spreadsheet door.\n\nThe win is retention, not allocation — the duplicate is allocated by the\nreader before the adapter sees it and dies in gen0 after dedup, which is\nwhy the Retention leg is the judge and MemoryDiagnoser is blind to it.\nMeasured against the committed floor: eager space held 106.8 -> 55.5 MB,\nlanding on the priced shared-string target to the byte; held results\n82.1 -> 30.8 MB, byte-identical across doors; all three unique controls\nflat to the byte; wall time noise on the 1M-row parse.\n\nThe cap (WorkbookOptions.MaxInternedStrings, default 65,536; 0 = off)\nand the 256-char length guard bound what the book-lifetime table can\npin. Documented as a two-way knob: a full table costs its entries for\nthe book's life — some 40 MB at the default cap — so it turns DOWN, to\n0, for known-unique text, and the docs say so at every site (the rig is\nstructurally blind to the table's own live set: readings are taken with\nthe book closed). Workbook.InterningStatistics reports hits, distinct,\nand estimated bytes (64-bit layout, exact for it; Hits counts fills, so\na reloaded chunk counts again — read against ChunkReloads).\n\nExcelDataReader fact on the record: shared-string cells arrive\npre-deduped from the reader's own SST; the duplication this kills comes\nfrom inline-string cells, formula-result cells, and .xls. Pinned by 36\ntests including a cross-door sharing differential, mutation-checked both\ndirections, and a WeakReference proof that Dispose releases the strings.\nSuite 1,423.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T19:19:56Z",
          "tree_id": "fea5150f427d5b65cf652662879afb3b470547f2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/eddc5d17c38c715f41cd95d041452deb66f8354c"
        },
        "date": 1788556079001,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 845259.6068209135,
            "unit": "ns",
            "range": "± 1716.6554435347466"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 845685.4287109375,
            "unit": "ns",
            "range": "± 972.8236279553379"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 305564.8957519531,
            "unit": "ns",
            "range": "± 269.97213703507913"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 2728517.584435096,
            "unit": "ns",
            "range": "± 3072.248547426237"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 3126949.9526041667,
            "unit": "ns",
            "range": "± 4106.660364226221"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 3596272.7584134615,
            "unit": "ns",
            "range": "± 4810.219362715007"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1579827.6502511161,
            "unit": "ns",
            "range": "± 1034.8434960648142"
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
          "id": "16fef6aa0fb6978c79559b92910716e6ad44c995",
          "message": "netstandard2.0: the packages install on .NET Framework, one source, no forks\n\nAll four libraries multi-target netstandard2.0;netstandard2.1. The rule\nthat made it clean: no #if anywhere — netstandard2.1-only constructs are\nremoved rather than branched on, so both targets compile one source and\ncannot drift.\n\nThe structural change is de-DIMing the incremental calculus. .NET\nFramework's runtime cannot dispatch a default interface member, so the\ndefinitional folds move from DIM bodies to the static Unrect.Core.Scans\n(Fold/FoldSize/FoldArea) and internal ColumnAccumulators.Fold, with every\nimplementation delegating in one line. The guarantee shifts and every doc\nthat claimed it says so honestly: \"eager and lazy cannot disagree by\nconstruction\" is now \"cannot disagree, pinned by the fold-identity suite\"\n— IncrementalStrategyTests asserts SelectRows == a hand-written fold for\nevery factory, and a new incremental strategy carries the obligation to\njoin that theory data. Corollary recorded in capability-seam-notes: the\nDIM \"safety net\" for evolving ISpace is withdrawn — a member added to a\npublished Core interface is now a breaking change with no escape hatch;\ntype-testing is the whole capability recipe.\n\nThe rest the compiler found: System.HashCode (Core hand-rolls its combine\nrather than take Microsoft.Bcl.HashCode — the bundling would have\nsuppressed the dependency from the nuspec and thrown FileNotFoundException\non Framework; both packages stay at zero new dependencies), the\nDictionary-from-pairs constructor, and HashSet<string>.TryGetValue (the\neager intern table is a Dictionary now; the comment that argued HashSet\nwas the honest structure records why it changed). The two polyfills are\nunconditional because neither netstandard has the types; a net5.0+ target\nis what would force an #if.\n\nTests multi-target net8.0;net48 on Windows only (OS-conditioned csproj,\nso Linux CI runs exactly what it ran — verified against all three\nworkflows). Two portable shims replace .NET-5+/6+ APIs the suite used:\nWithTimeout for Task.WaitAsync(TimeSpan), ByReference for\nReferenceEqualityComparer. The full suite has run green on the Framework\nCLR itself: 1,423 on net48 against the netstandard2.0 build, ExcelDataReader's\nown Framework assembly included — verified by the owner on Windows.\nBoth nupkgs verified to carry lib/netstandard2.0 and lib/netstandard2.1.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-05T17:36:21Z",
          "tree_id": "67b64bc3184652344901c8a243f8ce1256d858b9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16fef6aa0fb6978c79559b92910716e6ad44c995"
        },
        "date": 1788630323555,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 1005387.9090401785,
            "unit": "ns",
            "range": "± 1798.961997431243"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 1035272.4966107537,
            "unit": "ns",
            "range": "± 28368.14166681288"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 318072.92340959824,
            "unit": "ns",
            "range": "± 315.7918655374804"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 2793430.1361177885,
            "unit": "ns",
            "range": "± 3616.634849322778"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 3249356.6268028845,
            "unit": "ns",
            "range": "± 3848.0226717806486"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 3803441.102120536,
            "unit": "ns",
            "range": "± 11484.050511813892"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1605165.2389322917,
            "unit": "ns",
            "range": "± 3617.1205466288093"
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
          "id": "e1d21c2d706b054d252bbea3a996b97f6f7bfc4b",
          "message": "Hygiene: .gitattributes and editorconfig trim — line endings become a decision\n\nThe repo had no .gitattributes, so line endings were whatever tool\ntouched a file last — four .linq files sat CRLF in the index beside an\nLF corpus, and the vocabulary respell nearly flipped three of them\nwholesale (caught in review; a 10-line diff had become 366). One rule\nnow: the index holds LF (* text=auto); .linq and .sln check out CRLF\nbecause their native editors are Windows-bound; spreadsheets and images\nare explicitly binary. This commit carries the one-time renormalization\nso the flip lives here, deliberately, and nowhere else.\n\n.editorconfig gains trailing-whitespace trimming, with markdown exempt\n(a trailing double-space is a hard line break).\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-06T00:49:13Z",
          "tree_id": "ce25b3593543c4145d69070ff009c9f67ff1aa09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/e1d21c2d706b054d252bbea3a996b97f6f7bfc4b"
        },
        "date": 1788712512802,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 851355.2435128348,
            "unit": "ns",
            "range": "± 4320.804323160854"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 850854.9916015625,
            "unit": "ns",
            "range": "± 2631.816231020582"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 306444.71349158656,
            "unit": "ns",
            "range": "± 388.8597580927375"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 2785118.716536458,
            "unit": "ns",
            "range": "± 9701.525098346014"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 3247446.809988839,
            "unit": "ns",
            "range": "± 8639.956673505836"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 3964145.0868164063,
            "unit": "ns",
            "range": "± 140878.5637053738"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1586267.1114676339,
            "unit": "ns",
            "range": "± 2401.7001035425656"
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
          "id": "bf1fc2851544d9f87ffb3b76df4fa0b714780243",
          "message": "Merge experiment/typed-spaces: the projection model\n\nThe deepest cut since wave 2: what was called Shape is a projection; the\nspace and its subspaces are the shapes. Seven phases plus pre-merge\ncleanup (docs/design/projection-model-spec.md, judgment record in §10):\nthe modifier doubling killed (43 -> 6 irreducible), IShape -> IProjection\nwith namespace Unrect.Projections, the capability stack (IFormulaSpace,\nCapability<T>() transport, the slicing law, shared-formula\nreconstruction), OrBlank and the eachRow slot, the CaptionMap bind, the\nscoped entry Over<TSpace>() and MapWorkbook, phase-7 acceptance with the\nfourteen recorded refusals, and the three-sweep cleanup.\n\nSuite 1,709 green (net8.0 and net48); both library TFMs 0 warnings.\nBreaking: the rename, the Table family, the SpreadsheetSpace shell\nretirement, and the entries.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-07T23:11:42Z",
          "tree_id": "d091a9f7437fb1ec8d2b35119ab628b2a54047a3",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bf1fc2851544d9f87ffb3b76df4fa0b714780243"
        },
        "date": 1788823321140,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 959935.8661295573,
            "unit": "ns",
            "range": "± 24669.63495902785"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 932531.9928260216,
            "unit": "ns",
            "range": "± 6181.314873388345"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 304764.08196614584,
            "unit": "ns",
            "range": "± 443.77892312300236"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 2801487.5365885417,
            "unit": "ns",
            "range": "± 31015.646987127613"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 3140656.041015625,
            "unit": "ns",
            "range": "± 8506.997434973187"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 3733392.42890625,
            "unit": "ns",
            "range": "± 52574.15105985724"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1584053.291294643,
            "unit": "ns",
            "range": "± 2221.498382420207"
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
          "id": "4a44b787ac6b8b6fa5c487623e316bfe0e364292",
          "message": "Presence semantics and three hygiene fixes: the zero disambiguated\n\nThe algebra's highest-priority open problem (v0.4 §7/§14.1): geometric\nzero carried four meanings. The internal Presence { Read, Empty,\nAbsorbed } now rides beside Consumed on the engine's results — stamped\nat tolerance boundaries (Absorbed), settled-at-zero extents (Empty, read\noff the SETTLED extent so evaluation order and doors agree by\nconstruction), joined through layouts (Read if any child Read, else\nEmpty), defaulting Read. The internal epsilon (NothingProjection) makes\nthe flow-unit law stateable: VerticalFlow is a monoid at L3 for\nsuccessful readings, boundaries pinned.\n\nThe law, earned the hard way: presence explains a stop; the extent\ndecides one. The first implementation let presence decide the repeat\nguard; the law-tests caught a real L1/L2 change on\nVerticalRepeat(item.Optional().Sized(...)) — an Absorbed boundary under\na declared area legitimately consumes it and always kept the run going.\nThe guard is byte-identical to before; presence powers only the new\nteaching Info when a run ends at an absorbed item (D2), and the caught\nshape is pinned as the compatibility-law section of PresenceLawTests.\n\nDesign record: docs/design/presence-and-unit-spec.md (DECIDED, with the\nD5 amendment trail).\n\nAlso, with pins: ChoiceProjection.Summarise renders nested aggregates\ndepth-indented with one location per line; MinOffset() is one canonical\ninstance (HasDeclaredOffset correct by construction); the content-\nmatching rule reduced to one implementation (CellMatching primitives,\nTableView's dictionary keyed on TextComparer; CaptionComparer's\ndeliberate divergence untouched). spike/Phase1Probe removed (a husk).\n\nSuite 1,820 -> 1,862 green; both library TFMs 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-09T15:34:18Z",
          "tree_id": "883452644caa55f06a0f9c2d530ee9c0cece2b92",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4a44b787ac6b8b6fa5c487623e316bfe0e364292"
        },
        "date": 1788969080515,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 1092070.9018554688,
            "unit": "ns",
            "range": "± 24814.704151620947"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 1106310.0936035155,
            "unit": "ns",
            "range": "± 39136.76095782286"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 309435.1823730469,
            "unit": "ns",
            "range": "± 408.9451511411657"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 2975065.3463541665,
            "unit": "ns",
            "range": "± 5039.099211716671"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 3234296.450420673,
            "unit": "ns",
            "range": "± 7052.424234492743"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 4014596.8058268228,
            "unit": "ns",
            "range": "± 96138.89086028811"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1605181.5733173077,
            "unit": "ns",
            "range": "± 2303.507922175638"
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
          "id": "d06708df60e381758b11dde07836c674565409b6",
          "message": "Merge experiment/record-primitive: streaming composites over a bound, the band tiler, Table composed from primitives\n\nThe labels-as-context primitives (ColumnLabels, WithColumnLabels, Record),\nSkipToFirstNonBlankCell, and the uniform offset law; the engine streaming a\ndiscovered bound through a composite (TailSpace, a bound-aware Exceeds, lazy\nflow and repeat slices); VerticalBands/HorizontalBands, the tiler — a\nrepeating fixed-dimension space, distinct from the pattern repeat;\nTable(headerRows, eachRow) composed over the tiler under a UnitProjection,\nwith AsScaffolding and a mark-driven path fold giving it the leaf's own\ndiagnostics; ColumnLabels self-contained, its header parse the one home in\nLabelMap.FromHeader; and the repeat returned to a pure walk, onBlank living\non the tiler alone.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-14T17:43:35Z",
          "tree_id": "1c80a0bed7aeb311bb6e1bdb0933ac5a05562434",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/d06708df60e381758b11dde07836c674565409b6"
        },
        "date": 1789408532990,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 938534.6659458706,
            "unit": "ns",
            "range": "± 3178.0353262141366"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 909090.1068960336,
            "unit": "ns",
            "range": "± 2496.5786476720705"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 305571.8028041295,
            "unit": "ns",
            "range": "± 405.3577751294936"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 2748130.901785714,
            "unit": "ns",
            "range": "± 6103.477007026456"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 3175321.6751802885,
            "unit": "ns",
            "range": "± 6922.531236902856"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 3644230.1497395835,
            "unit": "ns",
            "range": "± 22716.60381939452"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1575911.7297712055,
            "unit": "ns",
            "range": "± 2571.7380256339957"
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
          "id": "33f1afa3496afb1fabd2e0242197d1c5d9889568",
          "message": "Merge experiment/point-and-line: the Point substrate — one vocabulary over any ISpace, planes and points as the locators, the kinds in Unrect.Spreadsheets\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-16T04:05:17Z",
          "tree_id": "09d4c757d0f1502df340fb2a2481c31f78e8d8b2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/33f1afa3496afb1fabd2e0242197d1c5d9889568"
        },
        "date": 1789531988354,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 1278734.3587239583,
            "unit": "ns",
            "range": "± 937.2490684699335"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 1278399.013392857,
            "unit": "ns",
            "range": "± 3009.15650285855"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 512257.4171424279,
            "unit": "ns",
            "range": "± 1369.511901900653"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 4600230.8421875,
            "unit": "ns",
            "range": "± 3536.13086118767"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 4694166.650111607,
            "unit": "ns",
            "range": "± 4876.7249068348265"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 5879702.623325893,
            "unit": "ns",
            "range": "± 17696.427860261483"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1616162.5226004464,
            "unit": "ns",
            "range": "± 4438.74717351412"
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
          "id": "0f34f159151554afcc0555f7afc83fdd486b8c06",
          "message": "Merge experiment/point-follow-ups: Extents into Plane, Unrect.Interactive, and the typed-predicate lift\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-17T02:37:19Z",
          "tree_id": "cfa1d003f75555e9d6f330d316cdd949a1da2b25",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/0f34f159151554afcc0555f7afc83fdd486b8c06"
        },
        "date": 1789613136653,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 1202457.7467447917,
            "unit": "ns",
            "range": "± 1149.9826580108734"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 1207292.6450195312,
            "unit": "ns",
            "range": "± 503.1613484525089"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 486217.7124399039,
            "unit": "ns",
            "range": "± 535.7541077593071"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 4445156.791466346,
            "unit": "ns",
            "range": "± 6679.960987488347"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 4555125.137834822,
            "unit": "ns",
            "range": "± 5226.768197596514"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 5651704.450892857,
            "unit": "ns",
            "range": "± 12004.816876718824"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 1900557.6078725962,
            "unit": "ns",
            "range": "± 1019.7284694814193"
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
          "id": "717d1ea3b61c226c608194aae5ff909fd4603455",
          "message": "Packaging: Unrect.Interactive ships as its own package\n\nThe project was already packable — id, description, tags, and a\ndependency on exactly Unrect and Unrect.Spreadsheets at the same MinVer\nversion — but the release workflow packed and asserted two packages, and\nthe README and build props named two. Now three.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T01:12:25Z",
          "tree_id": "f01ec5a705245aededd07d5455e7b03ad4f8880d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/717d1ea3b61c226c608194aae5ff909fd4603455"
        },
        "date": 1789796481736,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 16251038.798076924,
            "unit": "ns",
            "range": "± 21880.671420304338"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 15684389.435096154,
            "unit": "ns",
            "range": "± 57825.45729752776"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 2338586988.857143,
            "unit": "ns",
            "range": "± 3918104.7885795077"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 189577506991.46667,
            "unit": "ns",
            "range": "± 110297687.58994472"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 232361873861.7143,
            "unit": "ns",
            "range": "± 144378549.42651984"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 183740639404.6,
            "unit": "ns",
            "range": "± 110908900.99953048"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 5726705.344350962,
            "unit": "ns",
            "range": "± 11668.668821307996"
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
          "id": "4818c36f9e9b0c2b75a4093dbbce6981329fd14b",
          "message": "Merge feature/scaffold-shapes: ScaffoldRecord and ScaffoldClass, on a sheet and as leaves\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T15:35:04Z",
          "tree_id": "d48076b68bb0a93dc6ab6797b10f0ead882156a7",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4818c36f9e9b0c2b75a4093dbbce6981329fd14b"
        },
        "date": 1789847937344,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 15624203.6875,
            "unit": "ns",
            "range": "± 74323.99825407306"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 15734315.554166667,
            "unit": "ns",
            "range": "± 149986.5800004569"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 2302086432.1538463,
            "unit": "ns",
            "range": "± 2194093.1786350003"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 186651161684.33334,
            "unit": "ns",
            "range": "± 246555374.1273836"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 231537244408.8,
            "unit": "ns",
            "range": "± 285634746.03145874"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 184041798028.86667,
            "unit": "ns",
            "range": "± 87084333.12860934"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 5757755.729910715,
            "unit": "ns",
            "range": "± 19680.03864211862"
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
          "id": "bd027f2136cee944da88c1eae17a06b14a05579d",
          "message": "Merge feature/bind-from-row: Table<T> fills a member from the row - Column(member, row => ...)\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T19:49:16Z",
          "tree_id": "88c877501aeddcefb436f136842b9904cda55a09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bd027f2136cee944da88c1eae17a06b14a05579d"
        },
        "date": 1789863952011,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 15636139.939583333,
            "unit": "ns",
            "range": "± 78522.13709607521"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 15606915.02455357,
            "unit": "ns",
            "range": "± 25305.31587037506"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 2318068950,
            "unit": "ns",
            "range": "± 1369971.155029073"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 186289884283.4,
            "unit": "ns",
            "range": "± 115653684.74544835"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 231556239347.2,
            "unit": "ns",
            "range": "± 222623414.7776874"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 186559506798.26666,
            "unit": "ns",
            "range": "± 204999265.22035566"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 5745230.305803572,
            "unit": "ns",
            "range": "± 10683.273224188668"
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
          "id": "96b946d5920955eb81be3eab46697a3e4639e2e4",
          "message": "Merge feature/header-bands: headers of several rows, columns addressed by path, flat binding over bands\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T03:17:51Z",
          "tree_id": "33ccb96a2f1d1cb7795dde522c6c586ee8ce479a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/96b946d5920955eb81be3eab46697a3e4639e2e4"
        },
        "date": 1789888950621,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 15751201.375,
            "unit": "ns",
            "range": "± 45127.45785094736"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 15695653.685416667,
            "unit": "ns",
            "range": "± 107256.9762781008"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 2327974110,
            "unit": "ns",
            "range": "± 2053017.7696420457"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 187647183671.53333,
            "unit": "ns",
            "range": "± 126970457.20092788"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 232220419745.7857,
            "unit": "ns",
            "range": "± 87671549.38208495"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 187002347116.30768,
            "unit": "ns",
            "range": "± 132347291.12258095"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 5715796.555803572,
            "unit": "ns",
            "range": "± 19564.9830221517"
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
          "id": "191ad7e666be18cd9916eb0132108e83e54bd6db",
          "message": "Open questions: IsText is the wrong shape - what it is for, the better shape, and the larger question of facets, recorded and not implemented\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T04:23:54Z",
          "tree_id": "5e6c0e37d52a9f501293d06ad42b1a125ed81250",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/191ad7e666be18cd9916eb0132108e83e54bd6db"
        },
        "date": 1789931367384,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 15743104.799107144,
            "unit": "ns",
            "range": "± 120637.88441500191"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 18758626.210416667,
            "unit": "ns",
            "range": "± 172949.45040209236"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 2321786465.75,
            "unit": "ns",
            "range": "± 632492.0898584245"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 187203728570.5,
            "unit": "ns",
            "range": "± 65376695.557531215"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 231122180000.93332,
            "unit": "ns",
            "range": "± 188565893.36926425"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 186266773770.53333,
            "unit": "ns",
            "range": "± 150917058.32570305"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 5707022.987379808,
            "unit": "ns",
            "range": "± 23947.876206797686"
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
          "id": "f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb",
          "message": "Fix the 2-core CI flake: a blocked-borrower proof needs a started borrower\n\nAReachWaitsForAWarmerRatherThanStartingASecondOpenOfTheSameFile failed on\nthe GitHub runner (ef348dd) on \"the wait is counted\": WarmWaitMilliseconds\nwas 0, and 0 was the honest count. The pool's warmers ride Task.Run and\nthe gated arrangement BLOCKS them inside their opens, one pool thread\neach — on a two-core runner that is the entire starting thread pool, so\nthe test's own Task.Run borrower never started until thread injection got\naround to it. Both blocked-ness assertions passed vacuously (not finished\nbecause not scheduled), and by the time the reach ran, the warm reader was\nparked and there was nothing left to wait for.\n\nReproduced under taskset -c 0,1: three failures in four runs before the\nfix, none in six Debug runs plus a Release run after. The fix is\nOnItsOwnThread (TaskCreationOptions.LongRunning) at the four sites that\nassert a borrower is blocked — a dedicated thread starts unconditionally,\nso \"started, and still not finished\" really does mean \"parked inside\nBorrow\". The three sibling sites could only pass vacuously, never fail,\nbut their proofs were the same lie under starvation. The burst tests\nalready stood on structural evidence (SpinUntil on OpensStarted) and are\nuntouched.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T22:37:18Z",
          "tree_id": "c37ffff8e7e618f8d8cdb3778c429c1bd5259fc9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb"
        },
        "date": 1788475702837,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1322062.4272460938,
            "unit": "ns",
            "range": "± 121897.83957174656"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 35509863.772327036,
            "unit": "ns",
            "range": "± 1459481.07753367"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 4867011.364746094,
            "unit": "ns",
            "range": "± 91120.73575858535"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 122012007.26200002,
            "unit": "ns",
            "range": "± 9947970.404135173"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 4471830.072150735,
            "unit": "ns",
            "range": "± 86783.3070654835"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 110945551.88928573,
            "unit": "ns",
            "range": "± 4763137.293110714"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 349003.1975097656,
            "unit": "ns",
            "range": "± 660.2602406323265"
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
          "id": "10027e9f1d263aac70041f0f7166b186324129e8",
          "message": "Both doors measure a sheet that will not say how big it is\n\nSpreadsheetSpace.Create sized its grid from reader.RowCount/FieldCount and\nsilently yielded an empty space when the reader would not give them — the\none outcome an adapter must not have, and a divergence from the streaming\ndoor, which has measured such sheets since Part 2 step 7. The fill is now\ntwo named siblings behind one dichotomy: ReadDeclared (the original loop,\nunchanged) and ReadMeasured (rows collected at their own width, the widest\nrow wins, absent trailing cells Blank — the same answer Workbook.Measure\ngives). The guard is rowCount <= 0 alone, deliberately mirroring the\nstreaming door so the two can never disagree about the same file.\n\nThe recorded cause was wrong, and is corrected everywhere it appeared: a\nmissing dimension element does not trigger this — ExcelDataReader derives\nboth counts from a pre-scan of the cells on every format it handles. The\nreachable trigger is a sheet with NO valued cell (rows of formatted-but-\nvalueless cells, a pre-formatted export region). Pinned by the committed\nTestData/no-extent.xlsx (dimensionless AND valueless, with the survey's\nRowsMeasured == 4 doubling as the fixture's own guard against a\nregeneration that quietly stops reaching the path) and a both-doors\nidentity test.\n\nRides along, both owner decisions from this session's discussion:\n- MaxReaders: spec §14 Q2 DECIDED — 3 stays and stops being provisional,\n  because no number is right: reader demand is the declaration's monotone-\n  cursor count, unbounded in principle, data-independent in practice, and\n  the ceiling fails gently (Reopens is the counted, named signal to raise\n  it). Sizing guidance added to docs/streaming.md; per-reader economics\n  (~5s CPU per open, position must be walked, reader-per-row is O(n^2))\n  recorded in the spec.\n- Table's header-derived width: spec §14 Q1 DEFERRED, superseding the\n  2026-09-03 yes — the step-8 interleave delivered the lazy win with\n  today's denotation intact, so the K-1 campaign votes before the\n  denotation change is paid for.\n\nSuite 1,382 -> 1,387; gates green in Debug and 2-core Release.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T14:29:21Z",
          "tree_id": "fc431b0954d2e3a5115a177bd1a21d63c169ffae",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/10027e9f1d263aac70041f0f7166b186324129e8"
        },
        "date": 1788533063932,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1409763.4736328125,
            "unit": "ns",
            "range": "± 7446.608708650432"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 14889799.942307692,
            "unit": "ns",
            "range": "± 184530.74980468425"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 6466378.534598215,
            "unit": "ns",
            "range": "± 45614.34676070969"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 71785207.46323529,
            "unit": "ns",
            "range": "± 1463238.2291306686"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 10346188.094840117,
            "unit": "ns",
            "range": "± 382940.238020055"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 121230096.67878789,
            "unit": "ns",
            "range": "± 3735848.782029507"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 355413.4931315104,
            "unit": "ns",
            "range": "± 1692.3921464682194"
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
          "id": "c01531cec6968e544acc578291244292172a00a5",
          "message": "Docs: Part 3 deferred on principle, and .Sized's composite role stated honestly\n\nSpec §13 gains the Part 3 row (bound-aware composite placement): the\nengine's remaining greed sorted into one necessary force (Repeat items —\nthe item's existence is the question), one free force (post-Project\nconsumption, amortised by the root's accounting), and one debt (composite\nchild placement, whose questions have lazy answers nobody asks for).\nDeferred until the first tall sized composite pays the debt — sized\ncomposites in the corpus are short header bands, where settling eagerly\ncosts nothing. The K-1 campaign is the likely judge; the census pin is the\ntripwire.\n\ndocs/streaming.md stops saying \"put the .Sized on the leaf\" as if it were\na law: a sized composite is a legitimate spelling with no leaf equivalent\n— a composite has no intrinsic extent, and the declared band is what\nscopes its internal seeks and settles its consumption.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T15:37:53Z",
          "tree_id": "6188ce68af3130bfba604f38845b0c515958cb34",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/c01531cec6968e544acc578291244292172a00a5"
        },
        "date": 1788537629398,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1458095.1534598214,
            "unit": "ns",
            "range": "± 9290.095088302503"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 14351786.985677084,
            "unit": "ns",
            "range": "± 32267.824888697567"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 8608660.751041668,
            "unit": "ns",
            "range": "± 79132.26203297981"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 93451655.61111112,
            "unit": "ns",
            "range": "± 1593170.2597476132"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 9811919.9578125,
            "unit": "ns",
            "range": "± 287532.55183138174"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 112326454.65333334,
            "unit": "ns",
            "range": "± 1692930.2500263725"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 355717.7154622396,
            "unit": "ns",
            "range": "± 2837.7249469511416"
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
          "id": "2d73985e95c70f51a2b26d7dc98c3936f1f52d5d",
          "message": "Retention: the live-set floor for the interning change, with the target on the chart\n\nAn eighth CI leg that is not a BenchmarkDotNet family: interning reduces\nRETAINED bytes, not allocations (a duplicate string is allocated by the\nreader before the adapter sees it and dies young after dedup), so the\nAllocated column cannot see it — and retention is deterministic, so it\nneeds no statistical engine. A one-shot job measures live bytes with the\nresult held, emits the same JSON document the rig already stores, and\nrides the same workflow and dashboard as everything else.\n\nBuilding it surfaced two facts worth more than the plumbing:\n\n- The eager door's duplication depends on how the file spells its text.\n  Shared-string cells come back already deduped (the reader returns its\n  table's own instance); inline strings and formula-result cells\n  materialise fresh per cell. A real Excel export is both (the local K-1:\n  9,049 text cells, 2,876 values, 4,016 instances — the formula results\n  are the duplicated half). The family brackets it, and the shared-string\n  row is the priced TARGET: the same cells read 112.0 MB duplicated vs\n  58.2 MB deduped, so ~48% is what a complete eager interner is worth on\n  this shape — short of that is unfinished, not failed.\n- The first fixture boxed decimals a real read never produces (16 MB of\n  boxes in a retained-bytes measurement); the retention fixtures now\n  yield doubles like a reader does. StreamingSpaces is deliberately\n  untouched — changing it would re-baseline that family's history.\n\nScenarios exercise the real seams the interning change will live in: the\neager rows go through SpreadsheetSpace.Create over generated workbooks\n(RetentionWorkbooks: a minimal hand-rolled OOXML writer, no new package;\nthe one deliberate exception to the no-workbooks rule, recorded in\ndocs/benchmarking.md), the streaming rows through the store's chunk fill.\nFloor: eager space held 106.8 MB, results held 82.1 MB both doors\n(byte-identical — streaming's promise stated in the metric), controls\nbyte-identical to their duplicated twins by fixed-width padding. Leg\nruns ~65s, the shortest in the matrix.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T16:47:38Z",
          "tree_id": "0c756ae6dd2d4f17cd84e585c99d7d3ae08fd409",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/2d73985e95c70f51a2b26d7dc98c3936f1f52d5d"
        },
        "date": 1788542160713,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1097571.3247514204,
            "unit": "ns",
            "range": "± 24580.076730919693"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 11148894.033333333,
            "unit": "ns",
            "range": "± 192748.65262456593"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 6232807.198939732,
            "unit": "ns",
            "range": "± 177472.9725978925"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 70401987.52604167,
            "unit": "ns",
            "range": "± 1765244.675258392"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 8263578.767113095,
            "unit": "ns",
            "range": "± 192880.95370470514"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 89831365.45238096,
            "unit": "ns",
            "range": "± 2291108.2665542467"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 233252.4802207341,
            "unit": "ns",
            "range": "± 10706.039575832347"
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
          "id": "eddc5d17c38c715f41cd95d041452deb66f8354c",
          "message": "Interning: equal text shares one instance through both doors\n\nAdapter-level string interning — a find-my-twin table at each door's\nadapt seam. The eager door threads a per-Create-call HashSet through both\nfill paths (one instance per distinct value across every sheet of the\ncall); the streaming door hangs one capped ConcurrentDictionary on the\nWorkbook, plumbed into every store's chunk fill, so a chase reader's\nre-parse dedupes against the first parse. Strings only: every other kind\nis inline in the 24-byte struct or unreachable from the spreadsheet door.\n\nThe win is retention, not allocation — the duplicate is allocated by the\nreader before the adapter sees it and dies in gen0 after dedup, which is\nwhy the Retention leg is the judge and MemoryDiagnoser is blind to it.\nMeasured against the committed floor: eager space held 106.8 -> 55.5 MB,\nlanding on the priced shared-string target to the byte; held results\n82.1 -> 30.8 MB, byte-identical across doors; all three unique controls\nflat to the byte; wall time noise on the 1M-row parse.\n\nThe cap (WorkbookOptions.MaxInternedStrings, default 65,536; 0 = off)\nand the 256-char length guard bound what the book-lifetime table can\npin. Documented as a two-way knob: a full table costs its entries for\nthe book's life — some 40 MB at the default cap — so it turns DOWN, to\n0, for known-unique text, and the docs say so at every site (the rig is\nstructurally blind to the table's own live set: readings are taken with\nthe book closed). Workbook.InterningStatistics reports hits, distinct,\nand estimated bytes (64-bit layout, exact for it; Hits counts fills, so\na reloaded chunk counts again — read against ChunkReloads).\n\nExcelDataReader fact on the record: shared-string cells arrive\npre-deduped from the reader's own SST; the duplication this kills comes\nfrom inline-string cells, formula-result cells, and .xls. Pinned by 36\ntests including a cross-door sharing differential, mutation-checked both\ndirections, and a WeakReference proof that Dispose releases the strings.\nSuite 1,423.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T19:19:56Z",
          "tree_id": "fea5150f427d5b65cf652662879afb3b470547f2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/eddc5d17c38c715f41cd95d041452deb66f8354c"
        },
        "date": 1788556079181,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1418822.713671875,
            "unit": "ns",
            "range": "± 7972.096855333469"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 15317606.220982144,
            "unit": "ns",
            "range": "± 170044.1417292127"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 6839936.037946428,
            "unit": "ns",
            "range": "± 113273.57491806589"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 70538813.29166667,
            "unit": "ns",
            "range": "± 823995.0267483857"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 10173719.647048611,
            "unit": "ns",
            "range": "± 567088.5904077116"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 115435497.04210524,
            "unit": "ns",
            "range": "± 2562657.6236226805"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 356831.38298688614,
            "unit": "ns",
            "range": "± 2222.8057213746074"
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
          "id": "16fef6aa0fb6978c79559b92910716e6ad44c995",
          "message": "netstandard2.0: the packages install on .NET Framework, one source, no forks\n\nAll four libraries multi-target netstandard2.0;netstandard2.1. The rule\nthat made it clean: no #if anywhere — netstandard2.1-only constructs are\nremoved rather than branched on, so both targets compile one source and\ncannot drift.\n\nThe structural change is de-DIMing the incremental calculus. .NET\nFramework's runtime cannot dispatch a default interface member, so the\ndefinitional folds move from DIM bodies to the static Unrect.Core.Scans\n(Fold/FoldSize/FoldArea) and internal ColumnAccumulators.Fold, with every\nimplementation delegating in one line. The guarantee shifts and every doc\nthat claimed it says so honestly: \"eager and lazy cannot disagree by\nconstruction\" is now \"cannot disagree, pinned by the fold-identity suite\"\n— IncrementalStrategyTests asserts SelectRows == a hand-written fold for\nevery factory, and a new incremental strategy carries the obligation to\njoin that theory data. Corollary recorded in capability-seam-notes: the\nDIM \"safety net\" for evolving ISpace is withdrawn — a member added to a\npublished Core interface is now a breaking change with no escape hatch;\ntype-testing is the whole capability recipe.\n\nThe rest the compiler found: System.HashCode (Core hand-rolls its combine\nrather than take Microsoft.Bcl.HashCode — the bundling would have\nsuppressed the dependency from the nuspec and thrown FileNotFoundException\non Framework; both packages stay at zero new dependencies), the\nDictionary-from-pairs constructor, and HashSet<string>.TryGetValue (the\neager intern table is a Dictionary now; the comment that argued HashSet\nwas the honest structure records why it changed). The two polyfills are\nunconditional because neither netstandard has the types; a net5.0+ target\nis what would force an #if.\n\nTests multi-target net8.0;net48 on Windows only (OS-conditioned csproj,\nso Linux CI runs exactly what it ran — verified against all three\nworkflows). Two portable shims replace .NET-5+/6+ APIs the suite used:\nWithTimeout for Task.WaitAsync(TimeSpan), ByReference for\nReferenceEqualityComparer. The full suite has run green on the Framework\nCLR itself: 1,423 on net48 against the netstandard2.0 build, ExcelDataReader's\nown Framework assembly included — verified by the owner on Windows.\nBoth nupkgs verified to carry lib/netstandard2.0 and lib/netstandard2.1.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-05T17:36:21Z",
          "tree_id": "67b64bc3184652344901c8a243f8ce1256d858b9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16fef6aa0fb6978c79559b92910716e6ad44c995"
        },
        "date": 1788630323738,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1372042.4166666667,
            "unit": "ns",
            "range": "± 11299.882410114382"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 14975916.14955357,
            "unit": "ns",
            "range": "± 83447.34639881004"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 6078766.656770834,
            "unit": "ns",
            "range": "± 46614.453463835314"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 65565106.8,
            "unit": "ns",
            "range": "± 514290.5191419196"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 9148530.63111413,
            "unit": "ns",
            "range": "± 225122.16110529826"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 111861381.89333336,
            "unit": "ns",
            "range": "± 1736867.0422871995"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 336499.547921317,
            "unit": "ns",
            "range": "± 1300.4262194274"
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
          "id": "e1d21c2d706b054d252bbea3a996b97f6f7bfc4b",
          "message": "Hygiene: .gitattributes and editorconfig trim — line endings become a decision\n\nThe repo had no .gitattributes, so line endings were whatever tool\ntouched a file last — four .linq files sat CRLF in the index beside an\nLF corpus, and the vocabulary respell nearly flipped three of them\nwholesale (caught in review; a 10-line diff had become 366). One rule\nnow: the index holds LF (* text=auto); .linq and .sln check out CRLF\nbecause their native editors are Windows-bound; spreadsheets and images\nare explicitly binary. This commit carries the one-time renormalization\nso the flip lives here, deliberately, and nowhere else.\n\n.editorconfig gains trailing-whitespace trimming, with markdown exempt\n(a trailing double-space is a hard line break).\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-06T00:49:13Z",
          "tree_id": "ce25b3593543c4145d69070ff009c9f67ff1aa09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/e1d21c2d706b054d252bbea3a996b97f6f7bfc4b"
        },
        "date": 1788712512990,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1380596.4295372595,
            "unit": "ns",
            "range": "± 8989.480940324864"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 15113627.475,
            "unit": "ns",
            "range": "± 258712.9770912019"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 6261288.320746528,
            "unit": "ns",
            "range": "± 128518.44319431254"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 67796503.59166667,
            "unit": "ns",
            "range": "± 1084735.4595997913"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 9218107.750600962,
            "unit": "ns",
            "range": "± 243443.6675671111"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 113025287.6451613,
            "unit": "ns",
            "range": "± 3377408.3480226044"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 334138.58558872767,
            "unit": "ns",
            "range": "± 960.2315675890964"
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
          "id": "bf1fc2851544d9f87ffb3b76df4fa0b714780243",
          "message": "Merge experiment/typed-spaces: the projection model\n\nThe deepest cut since wave 2: what was called Shape is a projection; the\nspace and its subspaces are the shapes. Seven phases plus pre-merge\ncleanup (docs/design/projection-model-spec.md, judgment record in §10):\nthe modifier doubling killed (43 -> 6 irreducible), IShape -> IProjection\nwith namespace Unrect.Projections, the capability stack (IFormulaSpace,\nCapability<T>() transport, the slicing law, shared-formula\nreconstruction), OrBlank and the eachRow slot, the CaptionMap bind, the\nscoped entry Over<TSpace>() and MapWorkbook, phase-7 acceptance with the\nfourteen recorded refusals, and the three-sweep cleanup.\n\nSuite 1,709 green (net8.0 and net48); both library TFMs 0 warnings.\nBreaking: the rename, the Table family, the SpreadsheetSpace shell\nretirement, and the entries.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-07T23:11:42Z",
          "tree_id": "d091a9f7437fb1ec8d2b35119ab628b2a54047a3",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bf1fc2851544d9f87ffb3b76df4fa0b714780243"
        },
        "date": 1788823321341,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1103795.1287667411,
            "unit": "ns",
            "range": "± 15875.341780261742"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 11766050.028492646,
            "unit": "ns",
            "range": "± 233397.49242534352"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 6624956.045955882,
            "unit": "ns",
            "range": "± 126678.04411861612"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 73710892.35326087,
            "unit": "ns",
            "range": "± 1858685.341611843"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 8391091.986458333,
            "unit": "ns",
            "range": "± 149679.22359221763"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 82327838.76923075,
            "unit": "ns",
            "range": "± 1224739.2696057209"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 228360.5376953125,
            "unit": "ns",
            "range": "± 2005.704880274489"
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
          "id": "4a44b787ac6b8b6fa5c487623e316bfe0e364292",
          "message": "Presence semantics and three hygiene fixes: the zero disambiguated\n\nThe algebra's highest-priority open problem (v0.4 §7/§14.1): geometric\nzero carried four meanings. The internal Presence { Read, Empty,\nAbsorbed } now rides beside Consumed on the engine's results — stamped\nat tolerance boundaries (Absorbed), settled-at-zero extents (Empty, read\noff the SETTLED extent so evaluation order and doors agree by\nconstruction), joined through layouts (Read if any child Read, else\nEmpty), defaulting Read. The internal epsilon (NothingProjection) makes\nthe flow-unit law stateable: VerticalFlow is a monoid at L3 for\nsuccessful readings, boundaries pinned.\n\nThe law, earned the hard way: presence explains a stop; the extent\ndecides one. The first implementation let presence decide the repeat\nguard; the law-tests caught a real L1/L2 change on\nVerticalRepeat(item.Optional().Sized(...)) — an Absorbed boundary under\na declared area legitimately consumes it and always kept the run going.\nThe guard is byte-identical to before; presence powers only the new\nteaching Info when a run ends at an absorbed item (D2), and the caught\nshape is pinned as the compatibility-law section of PresenceLawTests.\n\nDesign record: docs/design/presence-and-unit-spec.md (DECIDED, with the\nD5 amendment trail).\n\nAlso, with pins: ChoiceProjection.Summarise renders nested aggregates\ndepth-indented with one location per line; MinOffset() is one canonical\ninstance (HasDeclaredOffset correct by construction); the content-\nmatching rule reduced to one implementation (CellMatching primitives,\nTableView's dictionary keyed on TextComparer; CaptionComparer's\ndeliberate divergence untouched). spike/Phase1Probe removed (a husk).\n\nSuite 1,820 -> 1,862 green; both library TFMs 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-09T15:34:18Z",
          "tree_id": "883452644caa55f06a0f9c2d530ee9c0cece2b92",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4a44b787ac6b8b6fa5c487623e316bfe0e364292"
        },
        "date": 1788969080794,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1580735.281640625,
            "unit": "ns",
            "range": "± 9678.151511985403"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 17485293.585416667,
            "unit": "ns",
            "range": "± 109682.0749769822"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 6631656.333333333,
            "unit": "ns",
            "range": "± 61310.80736729679"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 69556702.3,
            "unit": "ns",
            "range": "± 642225.0978607185"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 10434338.959918479,
            "unit": "ns",
            "range": "± 261146.31543949601"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 126478246.53333333,
            "unit": "ns",
            "range": "± 2223308.587232241"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 338125.8026216947,
            "unit": "ns",
            "range": "± 1366.823720197043"
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
          "id": "d06708df60e381758b11dde07836c674565409b6",
          "message": "Merge experiment/record-primitive: streaming composites over a bound, the band tiler, Table composed from primitives\n\nThe labels-as-context primitives (ColumnLabels, WithColumnLabels, Record),\nSkipToFirstNonBlankCell, and the uniform offset law; the engine streaming a\ndiscovered bound through a composite (TailSpace, a bound-aware Exceeds, lazy\nflow and repeat slices); VerticalBands/HorizontalBands, the tiler — a\nrepeating fixed-dimension space, distinct from the pattern repeat;\nTable(headerRows, eachRow) composed over the tiler under a UnitProjection,\nwith AsScaffolding and a mark-driven path fold giving it the leaf's own\ndiagnostics; ColumnLabels self-contained, its header parse the one home in\nLabelMap.FromHeader; and the repeat returned to a pure walk, onBlank living\non the tiler alone.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-14T17:43:35Z",
          "tree_id": "1c80a0bed7aeb311bb6e1bdb0933ac5a05562434",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/d06708df60e381758b11dde07836c674565409b6"
        },
        "date": 1789408533262,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1782745.52421875,
            "unit": "ns",
            "range": "± 22666.928331358107"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 18312856.90625,
            "unit": "ns",
            "range": "± 71131.1310881252"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 6776965.0796875,
            "unit": "ns",
            "range": "± 68499.35198744149"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 71422175.76190476,
            "unit": "ns",
            "range": "± 635171.3326030968"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 10267975.362723215,
            "unit": "ns",
            "range": "± 112798.49857559001"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 122608353.69642857,
            "unit": "ns",
            "range": "± 2100128.087730315"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 335114.0589076451,
            "unit": "ns",
            "range": "± 979.7315945536106"
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
          "id": "33f1afa3496afb1fabd2e0242197d1c5d9889568",
          "message": "Merge experiment/point-and-line: the Point substrate — one vocabulary over any ISpace, planes and points as the locators, the kinds in Unrect.Spreadsheets\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-16T04:05:17Z",
          "tree_id": "09d4c757d0f1502df340fb2a2481c31f78e8d8b2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/33f1afa3496afb1fabd2e0242197d1c5d9889568"
        },
        "date": 1789531988635,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1791990.405048077,
            "unit": "ns",
            "range": "± 4666.015939377596"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 19258234.573660713,
            "unit": "ns",
            "range": "± 153242.16016846977"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 24119529.010416668,
            "unit": "ns",
            "range": "± 140253.99473146227"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 241961253.5714286,
            "unit": "ns",
            "range": "± 3154643.8048442416"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 10434225.625,
            "unit": "ns",
            "range": "± 372234.9790163716"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 110229960.09375003,
            "unit": "ns",
            "range": "± 3424444.0255424585"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 361339.0285295759,
            "unit": "ns",
            "range": "± 634.762094116892"
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
          "id": "0f34f159151554afcc0555f7afc83fdd486b8c06",
          "message": "Merge experiment/point-follow-ups: Extents into Plane, Unrect.Interactive, and the typed-predicate lift\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-17T02:37:19Z",
          "tree_id": "cfa1d003f75555e9d6f330d316cdd949a1da2b25",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/0f34f159151554afcc0555f7afc83fdd486b8c06"
        },
        "date": 1789613136921,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 1438499.1756417411,
            "unit": "ns",
            "range": "± 14616.2514095985"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 15010985.360416668,
            "unit": "ns",
            "range": "± 230953.08046344068"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 22187012.625,
            "unit": "ns",
            "range": "± 380046.21561405016"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 216189526.86111107,
            "unit": "ns",
            "range": "± 1915228.1605235203"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 7401455.592927632,
            "unit": "ns",
            "range": "± 154968.62859620407"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 84449328.18367347,
            "unit": "ns",
            "range": "± 1388263.6386527957"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 293918.2037434896,
            "unit": "ns",
            "range": "± 4534.32991764471"
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
          "id": "717d1ea3b61c226c608194aae5ff909fd4603455",
          "message": "Packaging: Unrect.Interactive ships as its own package\n\nThe project was already packable — id, description, tags, and a\ndependency on exactly Unrect and Unrect.Spreadsheets at the same MinVer\nversion — but the release workflow packed and asserted two packages, and\nthe README and build props named two. Now three.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T01:12:25Z",
          "tree_id": "f01ec5a705245aededd07d5455e7b03ad4f8880d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/717d1ea3b61c226c608194aae5ff909fd4603455"
        },
        "date": 1789796482072,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2374969.925223214,
            "unit": "ns",
            "range": "± 28504.140046815708"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 30548265.65625,
            "unit": "ns",
            "range": "± 535643.387647573"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 54047076.053333335,
            "unit": "ns",
            "range": "± 679185.2224803317"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 537629854.7333333,
            "unit": "ns",
            "range": "± 9646122.03238889"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 4600913.5380859375,
            "unit": "ns",
            "range": "± 87549.82564509576"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 83832742.5350877,
            "unit": "ns",
            "range": "± 1856628.992005894"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 233115.81805889422,
            "unit": "ns",
            "range": "± 1951.8056159502526"
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
          "id": "4818c36f9e9b0c2b75a4093dbbce6981329fd14b",
          "message": "Merge feature/scaffold-shapes: ScaffoldRecord and ScaffoldClass, on a sheet and as leaves\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T15:35:04Z",
          "tree_id": "d48076b68bb0a93dc6ab6797b10f0ead882156a7",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4818c36f9e9b0c2b75a4093dbbce6981329fd14b"
        },
        "date": 1789847937593,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 3062082.3515625,
            "unit": "ns",
            "range": "± 12533.282725009"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 36894511.48214285,
            "unit": "ns",
            "range": "± 675958.8107809628"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 72891171.9489796,
            "unit": "ns",
            "range": "± 203379.21828091022"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 731355730.7857143,
            "unit": "ns",
            "range": "± 946614.09481146"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 5927067.5484375,
            "unit": "ns",
            "range": "± 92113.03695678042"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 88894691.56600003,
            "unit": "ns",
            "range": "± 9008886.329173451"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 350595.16692708334,
            "unit": "ns",
            "range": "± 1899.2006371697973"
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
          "id": "bd027f2136cee944da88c1eae17a06b14a05579d",
          "message": "Merge feature/bind-from-row: Table<T> fills a member from the row - Column(member, row => ...)\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T19:49:16Z",
          "tree_id": "88c877501aeddcefb436f136842b9904cda55a09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bd027f2136cee944da88c1eae17a06b14a05579d"
        },
        "date": 1789863952290,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 3173699.2640625,
            "unit": "ns",
            "range": "± 32143.999191787785"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 41293191.81656805,
            "unit": "ns",
            "range": "± 587326.1684272866"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 73134183.28571428,
            "unit": "ns",
            "range": "± 1342030.69276093"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 718037377.5,
            "unit": "ns",
            "range": "± 8486307.664708892"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 6319139.38125,
            "unit": "ns",
            "range": "± 43655.321397177664"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 95268406.76923077,
            "unit": "ns",
            "range": "± 543093.9533777939"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 348257.25435965403,
            "unit": "ns",
            "range": "± 1349.9552199622142"
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
          "id": "96b946d5920955eb81be3eab46697a3e4639e2e4",
          "message": "Merge feature/header-bands: headers of several rows, columns addressed by path, flat binding over bands\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T03:17:51Z",
          "tree_id": "33ccb96a2f1d1cb7795dde522c6c586ee8ce479a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/96b946d5920955eb81be3eab46697a3e4639e2e4"
        },
        "date": 1789888950953,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 3167384.4693509615,
            "unit": "ns",
            "range": "± 9192.355717572436"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 43151996.92307692,
            "unit": "ns",
            "range": "± 180736.2299442164"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 73375276.04081632,
            "unit": "ns",
            "range": "± 413810.3392921326"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 719905653.6666666,
            "unit": "ns",
            "range": "± 2550511.6906590015"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 6261548.133333334,
            "unit": "ns",
            "range": "± 29638.66681944068"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 95629718.75641026,
            "unit": "ns",
            "range": "± 716162.9661894713"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 349297.9844726563,
            "unit": "ns",
            "range": "± 2392.412681982134"
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
          "id": "191ad7e666be18cd9916eb0132108e83e54bd6db",
          "message": "Open questions: IsText is the wrong shape - what it is for, the better shape, and the larger question of facets, recorded and not implemented\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T04:23:54Z",
          "tree_id": "5e6c0e37d52a9f501293d06ad42b1a125ed81250",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/191ad7e666be18cd9916eb0132108e83e54bd6db"
        },
        "date": 1789931367668,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 3210225.8270833334,
            "unit": "ns",
            "range": "± 29834.74731698321"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 40532177.218934916,
            "unit": "ns",
            "range": "± 295174.58423809696"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 74685792.81632651,
            "unit": "ns",
            "range": "± 316200.36959569"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 724434851,
            "unit": "ns",
            "range": "± 4795290.6150084855"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 6308665.636979166,
            "unit": "ns",
            "range": "± 54294.858860744076"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 95500941.22222221,
            "unit": "ns",
            "range": "± 479555.78395029024"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 347118.4306989397,
            "unit": "ns",
            "range": "± 1688.1684817518594"
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
        "date": 1788472136842,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 64589153.43956043,
            "unit": "ns",
            "range": "± 305180.96468711575"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 31193895.2,
            "unit": "ns",
            "range": "± 238470.89301429634"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 18291059.60044643,
            "unit": "ns",
            "range": "± 22012.131402014482"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 3020276.885986328,
            "unit": "ns",
            "range": "± 57239.899688717465"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 1681687.7149832589,
            "unit": "ns",
            "range": "± 14020.683166412708"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 6166645.220833333,
            "unit": "ns",
            "range": "± 9002.910224719824"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 560643.3574880826,
            "unit": "ns",
            "range": "± 24738.77683826045"
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
          "id": "f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb",
          "message": "Fix the 2-core CI flake: a blocked-borrower proof needs a started borrower\n\nAReachWaitsForAWarmerRatherThanStartingASecondOpenOfTheSameFile failed on\nthe GitHub runner (ef348dd) on \"the wait is counted\": WarmWaitMilliseconds\nwas 0, and 0 was the honest count. The pool's warmers ride Task.Run and\nthe gated arrangement BLOCKS them inside their opens, one pool thread\neach — on a two-core runner that is the entire starting thread pool, so\nthe test's own Task.Run borrower never started until thread injection got\naround to it. Both blocked-ness assertions passed vacuously (not finished\nbecause not scheduled), and by the time the reach ran, the warm reader was\nparked and there was nothing left to wait for.\n\nReproduced under taskset -c 0,1: three failures in four runs before the\nfix, none in six Debug runs plus a Release run after. The fix is\nOnItsOwnThread (TaskCreationOptions.LongRunning) at the four sites that\nassert a borrower is blocked — a dedicated thread starts unconditionally,\nso \"started, and still not finished\" really does mean \"parked inside\nBorrow\". The three sibling sites could only pass vacuously, never fail,\nbut their proofs were the same lie under starvation. The burst tests\nalready stood on structural evidence (SpinUntil on OpensStarted) and are\nuntouched.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T22:37:18Z",
          "tree_id": "c37ffff8e7e618f8d8cdb3778c429c1bd5259fc9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb"
        },
        "date": 1788475702992,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 66186695.26785714,
            "unit": "ns",
            "range": "± 792625.1699853378"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 32102647.9125,
            "unit": "ns",
            "range": "± 247769.5184992883"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 19967492.551339287,
            "unit": "ns",
            "range": "± 40346.36081990338"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 2354752.7578125,
            "unit": "ns",
            "range": "± 48871.758927090916"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 1413222.1060697115,
            "unit": "ns",
            "range": "± 10965.96801630918"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 7786177.987379808,
            "unit": "ns",
            "range": "± 7336.023425488484"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 604317.6751302084,
            "unit": "ns",
            "range": "± 6731.79881921444"
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
          "id": "10027e9f1d263aac70041f0f7166b186324129e8",
          "message": "Both doors measure a sheet that will not say how big it is\n\nSpreadsheetSpace.Create sized its grid from reader.RowCount/FieldCount and\nsilently yielded an empty space when the reader would not give them — the\none outcome an adapter must not have, and a divergence from the streaming\ndoor, which has measured such sheets since Part 2 step 7. The fill is now\ntwo named siblings behind one dichotomy: ReadDeclared (the original loop,\nunchanged) and ReadMeasured (rows collected at their own width, the widest\nrow wins, absent trailing cells Blank — the same answer Workbook.Measure\ngives). The guard is rowCount <= 0 alone, deliberately mirroring the\nstreaming door so the two can never disagree about the same file.\n\nThe recorded cause was wrong, and is corrected everywhere it appeared: a\nmissing dimension element does not trigger this — ExcelDataReader derives\nboth counts from a pre-scan of the cells on every format it handles. The\nreachable trigger is a sheet with NO valued cell (rows of formatted-but-\nvalueless cells, a pre-formatted export region). Pinned by the committed\nTestData/no-extent.xlsx (dimensionless AND valueless, with the survey's\nRowsMeasured == 4 doubling as the fixture's own guard against a\nregeneration that quietly stops reaching the path) and a both-doors\nidentity test.\n\nRides along, both owner decisions from this session's discussion:\n- MaxReaders: spec §14 Q2 DECIDED — 3 stays and stops being provisional,\n  because no number is right: reader demand is the declaration's monotone-\n  cursor count, unbounded in principle, data-independent in practice, and\n  the ceiling fails gently (Reopens is the counted, named signal to raise\n  it). Sizing guidance added to docs/streaming.md; per-reader economics\n  (~5s CPU per open, position must be walked, reader-per-row is O(n^2))\n  recorded in the spec.\n- Table's header-derived width: spec §14 Q1 DEFERRED, superseding the\n  2026-09-03 yes — the step-8 interleave delivered the lazy win with\n  today's denotation intact, so the K-1 campaign votes before the\n  denotation change is paid for.\n\nSuite 1,382 -> 1,387; gates green in Debug and 2-core Release.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T14:29:21Z",
          "tree_id": "fc431b0954d2e3a5115a177bd1a21d63c169ffae",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/10027e9f1d263aac70041f0f7166b186324129e8"
        },
        "date": 1788533064134,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 63318990.208333336,
            "unit": "ns",
            "range": "± 445914.3843933183"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 31378065.6375,
            "unit": "ns",
            "range": "± 256126.16690296723"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 18274715.957291666,
            "unit": "ns",
            "range": "± 53293.47734740174"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 2905304.8684895835,
            "unit": "ns",
            "range": "± 17084.53446220274"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 1624612.9776041666,
            "unit": "ns",
            "range": "± 12927.440707844973"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 6223539.868303572,
            "unit": "ns",
            "range": "± 6271.822483183774"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 518097.71240234375,
            "unit": "ns",
            "range": "± 2327.7510245913313"
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
          "id": "c01531cec6968e544acc578291244292172a00a5",
          "message": "Docs: Part 3 deferred on principle, and .Sized's composite role stated honestly\n\nSpec §13 gains the Part 3 row (bound-aware composite placement): the\nengine's remaining greed sorted into one necessary force (Repeat items —\nthe item's existence is the question), one free force (post-Project\nconsumption, amortised by the root's accounting), and one debt (composite\nchild placement, whose questions have lazy answers nobody asks for).\nDeferred until the first tall sized composite pays the debt — sized\ncomposites in the corpus are short header bands, where settling eagerly\ncosts nothing. The K-1 campaign is the likely judge; the census pin is the\ntripwire.\n\ndocs/streaming.md stops saying \"put the .Sized on the leaf\" as if it were\na law: a sized composite is a legitimate spelling with no leaf equivalent\n— a composite has no intrinsic extent, and the declared band is what\nscopes its internal seeks and settles its consumption.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T15:37:53Z",
          "tree_id": "6188ce68af3130bfba604f38845b0c515958cb34",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/c01531cec6968e544acc578291244292172a00a5"
        },
        "date": 1788537629620,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 67426628,
            "unit": "ns",
            "range": "± 1056837.2544795931"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 31949915.223214287,
            "unit": "ns",
            "range": "± 205107.4207241165"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 19903536.17857143,
            "unit": "ns",
            "range": "± 19048.71779195367"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 2262926.245535714,
            "unit": "ns",
            "range": "± 50992.074067126676"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 1399995.2109375,
            "unit": "ns",
            "range": "± 6117.0743321034315"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 7640837.922475962,
            "unit": "ns",
            "range": "± 16252.776660260208"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 526028.1548461914,
            "unit": "ns",
            "range": "± 10163.653420516202"
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
          "id": "2d73985e95c70f51a2b26d7dc98c3936f1f52d5d",
          "message": "Retention: the live-set floor for the interning change, with the target on the chart\n\nAn eighth CI leg that is not a BenchmarkDotNet family: interning reduces\nRETAINED bytes, not allocations (a duplicate string is allocated by the\nreader before the adapter sees it and dies young after dedup), so the\nAllocated column cannot see it — and retention is deterministic, so it\nneeds no statistical engine. A one-shot job measures live bytes with the\nresult held, emits the same JSON document the rig already stores, and\nrides the same workflow and dashboard as everything else.\n\nBuilding it surfaced two facts worth more than the plumbing:\n\n- The eager door's duplication depends on how the file spells its text.\n  Shared-string cells come back already deduped (the reader returns its\n  table's own instance); inline strings and formula-result cells\n  materialise fresh per cell. A real Excel export is both (the local K-1:\n  9,049 text cells, 2,876 values, 4,016 instances — the formula results\n  are the duplicated half). The family brackets it, and the shared-string\n  row is the priced TARGET: the same cells read 112.0 MB duplicated vs\n  58.2 MB deduped, so ~48% is what a complete eager interner is worth on\n  this shape — short of that is unfinished, not failed.\n- The first fixture boxed decimals a real read never produces (16 MB of\n  boxes in a retained-bytes measurement); the retention fixtures now\n  yield doubles like a reader does. StreamingSpaces is deliberately\n  untouched — changing it would re-baseline that family's history.\n\nScenarios exercise the real seams the interning change will live in: the\neager rows go through SpreadsheetSpace.Create over generated workbooks\n(RetentionWorkbooks: a minimal hand-rolled OOXML writer, no new package;\nthe one deliberate exception to the no-workbooks rule, recorded in\ndocs/benchmarking.md), the streaming rows through the store's chunk fill.\nFloor: eager space held 106.8 MB, results held 82.1 MB both doors\n(byte-identical — streaming's promise stated in the metric), controls\nbyte-identical to their duplicated twins by fixed-width padding. Leg\nruns ~65s, the shortest in the matrix.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T16:47:38Z",
          "tree_id": "0c756ae6dd2d4f17cd84e585c99d7d3ae08fd409",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/2d73985e95c70f51a2b26d7dc98c3936f1f52d5d"
        },
        "date": 1788542160944,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 40673780.430769235,
            "unit": "ns",
            "range": "± 880254.599195841"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 18849364.4890625,
            "unit": "ns",
            "range": "± 393492.44298441283"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 11858537.963235294,
            "unit": "ns",
            "range": "± 231756.27532724966"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 1610375.4836774555,
            "unit": "ns",
            "range": "± 24313.107628003716"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 907154.3475811298,
            "unit": "ns",
            "range": "± 14764.754632953636"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 4085400.6654411764,
            "unit": "ns",
            "range": "± 83606.37205704306"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 274136.31973078224,
            "unit": "ns",
            "range": "± 14396.196789966763"
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
          "id": "eddc5d17c38c715f41cd95d041452deb66f8354c",
          "message": "Interning: equal text shares one instance through both doors\n\nAdapter-level string interning — a find-my-twin table at each door's\nadapt seam. The eager door threads a per-Create-call HashSet through both\nfill paths (one instance per distinct value across every sheet of the\ncall); the streaming door hangs one capped ConcurrentDictionary on the\nWorkbook, plumbed into every store's chunk fill, so a chase reader's\nre-parse dedupes against the first parse. Strings only: every other kind\nis inline in the 24-byte struct or unreachable from the spreadsheet door.\n\nThe win is retention, not allocation — the duplicate is allocated by the\nreader before the adapter sees it and dies in gen0 after dedup, which is\nwhy the Retention leg is the judge and MemoryDiagnoser is blind to it.\nMeasured against the committed floor: eager space held 106.8 -> 55.5 MB,\nlanding on the priced shared-string target to the byte; held results\n82.1 -> 30.8 MB, byte-identical across doors; all three unique controls\nflat to the byte; wall time noise on the 1M-row parse.\n\nThe cap (WorkbookOptions.MaxInternedStrings, default 65,536; 0 = off)\nand the 256-char length guard bound what the book-lifetime table can\npin. Documented as a two-way knob: a full table costs its entries for\nthe book's life — some 40 MB at the default cap — so it turns DOWN, to\n0, for known-unique text, and the docs say so at every site (the rig is\nstructurally blind to the table's own live set: readings are taken with\nthe book closed). Workbook.InterningStatistics reports hits, distinct,\nand estimated bytes (64-bit layout, exact for it; Hits counts fills, so\na reloaded chunk counts again — read against ChunkReloads).\n\nExcelDataReader fact on the record: shared-string cells arrive\npre-deduped from the reader's own SST; the duplication this kills comes\nfrom inline-string cells, formula-result cells, and .xls. Pinned by 36\ntests including a cross-door sharing differential, mutation-checked both\ndirections, and a WeakReference proof that Dispose releases the strings.\nSuite 1,423.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T19:19:56Z",
          "tree_id": "fea5150f427d5b65cf652662879afb3b470547f2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/eddc5d17c38c715f41cd95d041452deb66f8354c"
        },
        "date": 1788556079368,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 63483972.516666666,
            "unit": "ns",
            "range": "± 418537.63834044593"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 31480946.066666666,
            "unit": "ns",
            "range": "± 358069.0981488561"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 18252032.47544643,
            "unit": "ns",
            "range": "± 32523.534905197983"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 2958551.6430664062,
            "unit": "ns",
            "range": "± 74565.20003035464"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 1617251.601171875,
            "unit": "ns",
            "range": "± 25499.622037258338"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 6173632.94921875,
            "unit": "ns",
            "range": "± 12453.891692879053"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 513005.9673828125,
            "unit": "ns",
            "range": "± 4548.228802511252"
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
          "id": "16fef6aa0fb6978c79559b92910716e6ad44c995",
          "message": "netstandard2.0: the packages install on .NET Framework, one source, no forks\n\nAll four libraries multi-target netstandard2.0;netstandard2.1. The rule\nthat made it clean: no #if anywhere — netstandard2.1-only constructs are\nremoved rather than branched on, so both targets compile one source and\ncannot drift.\n\nThe structural change is de-DIMing the incremental calculus. .NET\nFramework's runtime cannot dispatch a default interface member, so the\ndefinitional folds move from DIM bodies to the static Unrect.Core.Scans\n(Fold/FoldSize/FoldArea) and internal ColumnAccumulators.Fold, with every\nimplementation delegating in one line. The guarantee shifts and every doc\nthat claimed it says so honestly: \"eager and lazy cannot disagree by\nconstruction\" is now \"cannot disagree, pinned by the fold-identity suite\"\n— IncrementalStrategyTests asserts SelectRows == a hand-written fold for\nevery factory, and a new incremental strategy carries the obligation to\njoin that theory data. Corollary recorded in capability-seam-notes: the\nDIM \"safety net\" for evolving ISpace is withdrawn — a member added to a\npublished Core interface is now a breaking change with no escape hatch;\ntype-testing is the whole capability recipe.\n\nThe rest the compiler found: System.HashCode (Core hand-rolls its combine\nrather than take Microsoft.Bcl.HashCode — the bundling would have\nsuppressed the dependency from the nuspec and thrown FileNotFoundException\non Framework; both packages stay at zero new dependencies), the\nDictionary-from-pairs constructor, and HashSet<string>.TryGetValue (the\neager intern table is a Dictionary now; the comment that argued HashSet\nwas the honest structure records why it changed). The two polyfills are\nunconditional because neither netstandard has the types; a net5.0+ target\nis what would force an #if.\n\nTests multi-target net8.0;net48 on Windows only (OS-conditioned csproj,\nso Linux CI runs exactly what it ran — verified against all three\nworkflows). Two portable shims replace .NET-5+/6+ APIs the suite used:\nWithTimeout for Task.WaitAsync(TimeSpan), ByReference for\nReferenceEqualityComparer. The full suite has run green on the Framework\nCLR itself: 1,423 on net48 against the netstandard2.0 build, ExcelDataReader's\nown Framework assembly included — verified by the owner on Windows.\nBoth nupkgs verified to carry lib/netstandard2.0 and lib/netstandard2.1.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-05T17:36:21Z",
          "tree_id": "67b64bc3184652344901c8a243f8ce1256d858b9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16fef6aa0fb6978c79559b92910716e6ad44c995"
        },
        "date": 1788630323929,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 64201463.0625,
            "unit": "ns",
            "range": "± 270568.43076331535"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 30999436.8984375,
            "unit": "ns",
            "range": "± 292923.9185522011"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 17991461.0875,
            "unit": "ns",
            "range": "± 88901.38294907912"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 2710981.734765625,
            "unit": "ns",
            "range": "± 14660.736305047727"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 1652159.947265625,
            "unit": "ns",
            "range": "± 8179.681641509029"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 6150592.0921875,
            "unit": "ns",
            "range": "± 11832.355871780917"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 504295.9893973214,
            "unit": "ns",
            "range": "± 641.4769095489011"
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
          "id": "e1d21c2d706b054d252bbea3a996b97f6f7bfc4b",
          "message": "Hygiene: .gitattributes and editorconfig trim — line endings become a decision\n\nThe repo had no .gitattributes, so line endings were whatever tool\ntouched a file last — four .linq files sat CRLF in the index beside an\nLF corpus, and the vocabulary respell nearly flipped three of them\nwholesale (caught in review; a 10-line diff had become 366). One rule\nnow: the index holds LF (* text=auto); .linq and .sln check out CRLF\nbecause their native editors are Windows-bound; spreadsheets and images\nare explicitly binary. This commit carries the one-time renormalization\nso the flip lives here, deliberately, and nowhere else.\n\n.editorconfig gains trailing-whitespace trimming, with markdown exempt\n(a trailing double-space is a hard line break).\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-06T00:49:13Z",
          "tree_id": "ce25b3593543c4145d69070ff009c9f67ff1aa09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/e1d21c2d706b054d252bbea3a996b97f6f7bfc4b"
        },
        "date": 1788712513181,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 64053306.75,
            "unit": "ns",
            "range": "± 692918.7370297275"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 31223547.835416667,
            "unit": "ns",
            "range": "± 349093.61068088654"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 18233188.1,
            "unit": "ns",
            "range": "± 110854.62092174467"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 2896366.0016741073,
            "unit": "ns",
            "range": "± 27478.001446367794"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 1721638.3388671875,
            "unit": "ns",
            "range": "± 20807.679055778324"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 6220577.225446428,
            "unit": "ns",
            "range": "± 5144.370834153205"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 514570.7911283053,
            "unit": "ns",
            "range": "± 7374.495465095196"
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
          "id": "bf1fc2851544d9f87ffb3b76df4fa0b714780243",
          "message": "Merge experiment/typed-spaces: the projection model\n\nThe deepest cut since wave 2: what was called Shape is a projection; the\nspace and its subspaces are the shapes. Seven phases plus pre-merge\ncleanup (docs/design/projection-model-spec.md, judgment record in §10):\nthe modifier doubling killed (43 -> 6 irreducible), IShape -> IProjection\nwith namespace Unrect.Projections, the capability stack (IFormulaSpace,\nCapability<T>() transport, the slicing law, shared-formula\nreconstruction), OrBlank and the eachRow slot, the CaptionMap bind, the\nscoped entry Over<TSpace>() and MapWorkbook, phase-7 acceptance with the\nfourteen recorded refusals, and the three-sweep cleanup.\n\nSuite 1,709 green (net8.0 and net48); both library TFMs 0 warnings.\nBreaking: the rename, the Table family, the SpreadsheetSpace shell\nretirement, and the entries.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-07T23:11:42Z",
          "tree_id": "d091a9f7437fb1ec8d2b35119ab628b2a54047a3",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bf1fc2851544d9f87ffb3b76df4fa0b714780243"
        },
        "date": 1788823321563,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 53758990.120000005,
            "unit": "ns",
            "range": "± 552235.9943806041"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 25191709.627403848,
            "unit": "ns",
            "range": "± 114864.50327212874"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 15269223.05357143,
            "unit": "ns",
            "range": "± 21533.99273833546"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 2291833.0599459135,
            "unit": "ns",
            "range": "± 12355.00931762503"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 1294125.30078125,
            "unit": "ns",
            "range": "± 4743.249995587621"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 6061319.510216346,
            "unit": "ns",
            "range": "± 4467.3067618000105"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 445074.7571049904,
            "unit": "ns",
            "range": "± 31837.567182173"
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
          "id": "4a44b787ac6b8b6fa5c487623e316bfe0e364292",
          "message": "Presence semantics and three hygiene fixes: the zero disambiguated\n\nThe algebra's highest-priority open problem (v0.4 §7/§14.1): geometric\nzero carried four meanings. The internal Presence { Read, Empty,\nAbsorbed } now rides beside Consumed on the engine's results — stamped\nat tolerance boundaries (Absorbed), settled-at-zero extents (Empty, read\noff the SETTLED extent so evaluation order and doors agree by\nconstruction), joined through layouts (Read if any child Read, else\nEmpty), defaulting Read. The internal epsilon (NothingProjection) makes\nthe flow-unit law stateable: VerticalFlow is a monoid at L3 for\nsuccessful readings, boundaries pinned.\n\nThe law, earned the hard way: presence explains a stop; the extent\ndecides one. The first implementation let presence decide the repeat\nguard; the law-tests caught a real L1/L2 change on\nVerticalRepeat(item.Optional().Sized(...)) — an Absorbed boundary under\na declared area legitimately consumes it and always kept the run going.\nThe guard is byte-identical to before; presence powers only the new\nteaching Info when a run ends at an absorbed item (D2), and the caught\nshape is pinned as the compatibility-law section of PresenceLawTests.\n\nDesign record: docs/design/presence-and-unit-spec.md (DECIDED, with the\nD5 amendment trail).\n\nAlso, with pins: ChoiceProjection.Summarise renders nested aggregates\ndepth-indented with one location per line; MinOffset() is one canonical\ninstance (HasDeclaredOffset correct by construction); the content-\nmatching rule reduced to one implementation (CellMatching primitives,\nTableView's dictionary keyed on TextComparer; CaptionComparer's\ndeliberate divergence untouched). spike/Phase1Probe removed (a husk).\n\nSuite 1,820 -> 1,862 green; both library TFMs 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-09T15:34:18Z",
          "tree_id": "883452644caa55f06a0f9c2d530ee9c0cece2b92",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4a44b787ac6b8b6fa5c487623e316bfe0e364292"
        },
        "date": 1788969081056,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 63456277.866071425,
            "unit": "ns",
            "range": "± 410111.37194144697"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 31269293.566666666,
            "unit": "ns",
            "range": "± 334035.17568638985"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 18009429.160714287,
            "unit": "ns",
            "range": "± 61084.3850566905"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 2873291.499441964,
            "unit": "ns",
            "range": "± 20309.383068384115"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 1514126.8428385416,
            "unit": "ns",
            "range": "± 14695.751955221092"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 6177491.53515625,
            "unit": "ns",
            "range": "± 4369.964302586009"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 544601.6577524039,
            "unit": "ns",
            "range": "± 7138.132542649517"
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
          "id": "d06708df60e381758b11dde07836c674565409b6",
          "message": "Merge experiment/record-primitive: streaming composites over a bound, the band tiler, Table composed from primitives\n\nThe labels-as-context primitives (ColumnLabels, WithColumnLabels, Record),\nSkipToFirstNonBlankCell, and the uniform offset law; the engine streaming a\ndiscovered bound through a composite (TailSpace, a bound-aware Exceeds, lazy\nflow and repeat slices); VerticalBands/HorizontalBands, the tiler — a\nrepeating fixed-dimension space, distinct from the pattern repeat;\nTable(headerRows, eachRow) composed over the tiler under a UnitProjection,\nwith AsScaffolding and a mark-driven path fold giving it the leaf's own\ndiagnostics; ColumnLabels self-contained, its header parse the one home in\nLabelMap.FromHeader; and the repeat returned to a pure walk, onBlank living\non the tiler alone.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-14T17:43:35Z",
          "tree_id": "1c80a0bed7aeb311bb6e1bdb0933ac5a05562434",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/d06708df60e381758b11dde07836c674565409b6"
        },
        "date": 1789408533542,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 53056281.65714286,
            "unit": "ns",
            "range": "± 814267.2282783953"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 25361681.802083332,
            "unit": "ns",
            "range": "± 49241.36875494045"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 24366940.042067308,
            "unit": "ns",
            "range": "± 5495.786678205097"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 2009089.6515066964,
            "unit": "ns",
            "range": "± 14856.869707724421"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_TryGetByKind",
            "value": 1160891.4548527645,
            "unit": "ns",
            "range": "± 7157.782467599668"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Equality",
            "value": 6055232.077008928,
            "unit": "ns",
            "range": "± 15832.063125323732"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_Blankness",
            "value": 407757.4429274339,
            "unit": "ns",
            "range": "± 3738.5108424772857"
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
          "id": "33f1afa3496afb1fabd2e0242197d1c5d9889568",
          "message": "Merge experiment/point-and-line: the Point substrate — one vocabulary over any ISpace, planes and points as the locators, the kinds in Unrect.Spreadsheets\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-16T04:05:17Z",
          "tree_id": "09d4c757d0f1502df340fb2a2481c31f78e8d8b2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/33f1afa3496afb1fabd2e0242197d1c5d9889568"
        },
        "date": 1789531988904,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 16.17250609199206,
            "unit": "ns",
            "range": "± 0.22647055272329347"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 31010945.894230768,
            "unit": "ns",
            "range": "± 159900.28973665374"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsBlank_Million",
            "value": 1916572.0933314732,
            "unit": "ns",
            "range": "± 3688.1645715535533"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsText_Million",
            "value": 1915752.9077845982,
            "unit": "ns",
            "range": "± 4341.47037563269"
          },
          {
            "name": "Unrect.Benchmarks.Values.AsText_Million_Text",
            "value": 4770284.9859375,
            "unit": "ns",
            "range": "± 9975.616515636564"
          },
          {
            "name": "Unrect.Benchmarks.Values.Decimal_Million",
            "value": 19823991.99330357,
            "unit": "ns",
            "range": "± 65330.09250095602"
          },
          {
            "name": "Unrect.Benchmarks.Values.Point_Mint_Million",
            "value": 1408513.0199497768,
            "unit": "ns",
            "range": "± 1534.7078942238916"
          },
          {
            "name": "Unrect.Benchmarks.Values.Slice_Million",
            "value": 2414630.28359375,
            "unit": "ns",
            "range": "± 1537.6277696085472"
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
          "id": "0f34f159151554afcc0555f7afc83fdd486b8c06",
          "message": "Merge experiment/point-follow-ups: Extents into Plane, Unrect.Interactive, and the typed-predicate lift\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-17T02:37:19Z",
          "tree_id": "cfa1d003f75555e9d6f330d316cdd949a1da2b25",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/0f34f159151554afcc0555f7afc83fdd486b8c06"
        },
        "date": 1789613137186,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 17.39461833437284,
            "unit": "ns",
            "range": "± 0.06338066913162303"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 26360375.06919643,
            "unit": "ns",
            "range": "± 77714.84711852283"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsBlank_Million",
            "value": 1631436.8850260417,
            "unit": "ns",
            "range": "± 3677.4657681367303"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsText_Million",
            "value": 1603074.2071814905,
            "unit": "ns",
            "range": "± 4781.803799134386"
          },
          {
            "name": "Unrect.Benchmarks.Values.AsText_Million_Text",
            "value": 3974732.6651785714,
            "unit": "ns",
            "range": "± 94561.43747718295"
          },
          {
            "name": "Unrect.Benchmarks.Values.Decimal_Million",
            "value": 17698402.43125,
            "unit": "ns",
            "range": "± 49350.65769604408"
          },
          {
            "name": "Unrect.Benchmarks.Values.Point_Mint_Million",
            "value": 889707.3586425781,
            "unit": "ns",
            "range": "± 4082.205577824721"
          },
          {
            "name": "Unrect.Benchmarks.Values.Predicate_Million",
            "value": 6395538.551041666,
            "unit": "ns",
            "range": "± 28373.602205301067"
          },
          {
            "name": "Unrect.Benchmarks.Values.TypedPredicate_Million",
            "value": 31851370.416666668,
            "unit": "ns",
            "range": "± 197940.5090848057"
          },
          {
            "name": "Unrect.Benchmarks.Values.Slice_Million",
            "value": 1942252.405403646,
            "unit": "ns",
            "range": "± 22318.03523396171"
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
          "id": "717d1ea3b61c226c608194aae5ff909fd4603455",
          "message": "Packaging: Unrect.Interactive ships as its own package\n\nThe project was already packable — id, description, tags, and a\ndependency on exactly Unrect and Unrect.Spreadsheets at the same MinVer\nversion — but the release workflow packed and asserted two packages, and\nthe README and build props named two. Now three.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T01:12:25Z",
          "tree_id": "f01ec5a705245aededd07d5455e7b03ad4f8880d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/717d1ea3b61c226c608194aae5ff909fd4603455"
        },
        "date": 1789796482333,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 15.459977902968724,
            "unit": "ns",
            "range": "± 0.2612970099561459"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 29622738.5625,
            "unit": "ns",
            "range": "± 71684.10131881226"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsBlank_Million",
            "value": 2020898.6283482143,
            "unit": "ns",
            "range": "± 2150.6491790627892"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsText_Million",
            "value": 2026188.599888393,
            "unit": "ns",
            "range": "± 3557.2126149887413"
          },
          {
            "name": "Unrect.Benchmarks.Values.AsText_Million_Text",
            "value": 5510534.140925481,
            "unit": "ns",
            "range": "± 13138.859732630406"
          },
          {
            "name": "Unrect.Benchmarks.Values.Decimal_Million",
            "value": 18272474.725,
            "unit": "ns",
            "range": "± 150990.66229639173"
          },
          {
            "name": "Unrect.Benchmarks.Values.Point_Mint_Million",
            "value": 934978.6253004808,
            "unit": "ns",
            "range": "± 542.8845263584311"
          },
          {
            "name": "Unrect.Benchmarks.Values.Predicate_Million",
            "value": 3328239.316666667,
            "unit": "ns",
            "range": "± 9798.81219456727"
          },
          {
            "name": "Unrect.Benchmarks.Values.TypedPredicate_Million",
            "value": 37023103.03333333,
            "unit": "ns",
            "range": "± 352104.4295230494"
          },
          {
            "name": "Unrect.Benchmarks.Values.Slice_Million",
            "value": 1662558.2010323661,
            "unit": "ns",
            "range": "± 1760.614574005392"
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
          "id": "4818c36f9e9b0c2b75a4093dbbce6981329fd14b",
          "message": "Merge feature/scaffold-shapes: ScaffoldRecord and ScaffoldClass, on a sheet and as leaves\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T15:35:04Z",
          "tree_id": "d48076b68bb0a93dc6ab6797b10f0ead882156a7",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4818c36f9e9b0c2b75a4093dbbce6981329fd14b"
        },
        "date": 1789847937854,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 15.459708855549495,
            "unit": "ns",
            "range": "± 0.23806473272145343"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 29657870.16964286,
            "unit": "ns",
            "range": "± 79484.48922404004"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsBlank_Million",
            "value": 1981279.198939732,
            "unit": "ns",
            "range": "± 9917.843591583744"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsText_Million",
            "value": 1815582.4256310095,
            "unit": "ns",
            "range": "± 9281.587557624354"
          },
          {
            "name": "Unrect.Benchmarks.Values.AsText_Million_Text",
            "value": 4959688.823660715,
            "unit": "ns",
            "range": "± 6303.3872527032045"
          },
          {
            "name": "Unrect.Benchmarks.Values.Decimal_Million",
            "value": 18010942.645089287,
            "unit": "ns",
            "range": "± 71611.5235267695"
          },
          {
            "name": "Unrect.Benchmarks.Values.Point_Mint_Million",
            "value": 935190.5194561298,
            "unit": "ns",
            "range": "± 939.5446140233543"
          },
          {
            "name": "Unrect.Benchmarks.Values.Predicate_Million",
            "value": 3426148.6239583334,
            "unit": "ns",
            "range": "± 27006.71593440725"
          },
          {
            "name": "Unrect.Benchmarks.Values.TypedPredicate_Million",
            "value": 36764913.58095238,
            "unit": "ns",
            "range": "± 63242.538167242914"
          },
          {
            "name": "Unrect.Benchmarks.Values.Slice_Million",
            "value": 1662251.6079101562,
            "unit": "ns",
            "range": "± 1439.3676152550609"
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
          "id": "bd027f2136cee944da88c1eae17a06b14a05579d",
          "message": "Merge feature/bind-from-row: Table<T> fills a member from the row - Column(member, row => ...)\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T19:49:16Z",
          "tree_id": "88c877501aeddcefb436f136842b9904cda55a09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bd027f2136cee944da88c1eae17a06b14a05579d"
        },
        "date": 1789863952572,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 14.924247757593792,
            "unit": "ns",
            "range": "± 0.2196414036910694"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 29608000.73660714,
            "unit": "ns",
            "range": "± 99512.20323034341"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsBlank_Million",
            "value": 1992197.7896205357,
            "unit": "ns",
            "range": "± 11679.755662844338"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsText_Million",
            "value": 1976887.4349888393,
            "unit": "ns",
            "range": "± 6981.830216438225"
          },
          {
            "name": "Unrect.Benchmarks.Values.AsText_Million_Text",
            "value": 5450607.505580357,
            "unit": "ns",
            "range": "± 10060.569962258385"
          },
          {
            "name": "Unrect.Benchmarks.Values.Decimal_Million",
            "value": 18166002.714285713,
            "unit": "ns",
            "range": "± 158728.78775940326"
          },
          {
            "name": "Unrect.Benchmarks.Values.Point_Mint_Million",
            "value": 795204.0483022836,
            "unit": "ns",
            "range": "± 940.4229538699584"
          },
          {
            "name": "Unrect.Benchmarks.Values.Predicate_Million",
            "value": 3317318.6065848214,
            "unit": "ns",
            "range": "± 7995.076850676191"
          },
          {
            "name": "Unrect.Benchmarks.Values.TypedPredicate_Million",
            "value": 36821749.933673464,
            "unit": "ns",
            "range": "± 55123.301930548994"
          },
          {
            "name": "Unrect.Benchmarks.Values.Slice_Million",
            "value": 1661346.6010742188,
            "unit": "ns",
            "range": "± 1001.6245352576912"
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
          "id": "96b946d5920955eb81be3eab46697a3e4639e2e4",
          "message": "Merge feature/header-bands: headers of several rows, columns addressed by path, flat binding over bands\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T03:17:51Z",
          "tree_id": "33ccb96a2f1d1cb7795dde522c6c586ee8ce479a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/96b946d5920955eb81be3eab46697a3e4639e2e4"
        },
        "date": 1789888951229,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 12.404605298240979,
            "unit": "ns",
            "range": "± 0.15742087753128614"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 24073568.323660713,
            "unit": "ns",
            "range": "± 33135.52507901845"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsBlank_Million",
            "value": 1503189.7027064732,
            "unit": "ns",
            "range": "± 933.1093261550741"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsText_Million",
            "value": 1489666.683984375,
            "unit": "ns",
            "range": "± 1241.507380497275"
          },
          {
            "name": "Unrect.Benchmarks.Values.AsText_Million_Text",
            "value": 3722262.294921875,
            "unit": "ns",
            "range": "± 2822.199007669658"
          },
          {
            "name": "Unrect.Benchmarks.Values.Decimal_Million",
            "value": 15211737.127604166,
            "unit": "ns",
            "range": "± 4948.681623380626"
          },
          {
            "name": "Unrect.Benchmarks.Values.Point_Mint_Million",
            "value": 696125.2970252404,
            "unit": "ns",
            "range": "± 230.1608188497128"
          },
          {
            "name": "Unrect.Benchmarks.Values.Predicate_Million",
            "value": 2584977.3909040177,
            "unit": "ns",
            "range": "± 3253.6008634788536"
          },
          {
            "name": "Unrect.Benchmarks.Values.TypedPredicate_Million",
            "value": 32973277.070833333,
            "unit": "ns",
            "range": "± 63214.214218829424"
          },
          {
            "name": "Unrect.Benchmarks.Values.Slice_Million",
            "value": 1425187.000279018,
            "unit": "ns",
            "range": "± 1752.092970665323"
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
          "id": "191ad7e666be18cd9916eb0132108e83e54bd6db",
          "message": "Open questions: IsText is the wrong shape - what it is for, the better shape, and the larger question of facets, recorded and not implemented\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T04:23:54Z",
          "tree_id": "5e6c0e37d52a9f501293d06ad42b1a125ed81250",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/191ad7e666be18cd9916eb0132108e83e54bd6db"
        },
        "date": 1789931367948,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 15.700141364336014,
            "unit": "ns",
            "range": "± 0.1160888109889222"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 29459164.71153846,
            "unit": "ns",
            "range": "± 62294.87592092241"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsBlank_Million",
            "value": 1824861.92890625,
            "unit": "ns",
            "range": "± 5772.471176339919"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsText_Million",
            "value": 1836527.480189732,
            "unit": "ns",
            "range": "± 7137.018741290231"
          },
          {
            "name": "Unrect.Benchmarks.Values.AsText_Million_Text",
            "value": 5002892.635416667,
            "unit": "ns",
            "range": "± 14261.845444598945"
          },
          {
            "name": "Unrect.Benchmarks.Values.Decimal_Million",
            "value": 18136718.195833333,
            "unit": "ns",
            "range": "± 107781.82139270582"
          },
          {
            "name": "Unrect.Benchmarks.Values.Point_Mint_Million",
            "value": 795705.9063197544,
            "unit": "ns",
            "range": "± 1933.0937263667943"
          },
          {
            "name": "Unrect.Benchmarks.Values.Predicate_Million",
            "value": 3327354.7848772323,
            "unit": "ns",
            "range": "± 7217.409647896141"
          },
          {
            "name": "Unrect.Benchmarks.Values.TypedPredicate_Million",
            "value": 36855075.614285715,
            "unit": "ns",
            "range": "± 90738.85427017602"
          },
          {
            "name": "Unrect.Benchmarks.Values.Slice_Million",
            "value": 1661619.8068359375,
            "unit": "ns",
            "range": "± 2347.6710575422626"
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
        "date": 1788472137014,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 2028014.994233631,
            "unit": "ns",
            "range": "± 47650.09215926118"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 27768677.054166667,
            "unit": "ns",
            "range": "± 498824.2481398282"
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
          "id": "f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb",
          "message": "Fix the 2-core CI flake: a blocked-borrower proof needs a started borrower\n\nAReachWaitsForAWarmerRatherThanStartingASecondOpenOfTheSameFile failed on\nthe GitHub runner (ef348dd) on \"the wait is counted\": WarmWaitMilliseconds\nwas 0, and 0 was the honest count. The pool's warmers ride Task.Run and\nthe gated arrangement BLOCKS them inside their opens, one pool thread\neach — on a two-core runner that is the entire starting thread pool, so\nthe test's own Task.Run borrower never started until thread injection got\naround to it. Both blocked-ness assertions passed vacuously (not finished\nbecause not scheduled), and by the time the reach ran, the warm reader was\nparked and there was nothing left to wait for.\n\nReproduced under taskset -c 0,1: three failures in four runs before the\nfix, none in six Debug runs plus a Release run after. The fix is\nOnItsOwnThread (TaskCreationOptions.LongRunning) at the four sites that\nassert a borrower is blocked — a dedicated thread starts unconditionally,\nso \"started, and still not finished\" really does mean \"parked inside\nBorrow\". The three sibling sites could only pass vacuously, never fail,\nbut their proofs were the same lie under starvation. The burst tests\nalready stood on structural evidence (SpinUntil on OpensStarted) and are\nuntouched.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T22:37:18Z",
          "tree_id": "c37ffff8e7e618f8d8cdb3778c429c1bd5259fc9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb"
        },
        "date": 1788475703154,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 1523103.4118861607,
            "unit": "ns",
            "range": "± 49715.00690925135"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 19879164.47198276,
            "unit": "ns",
            "range": "± 577465.6909489478"
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
          "id": "10027e9f1d263aac70041f0f7166b186324129e8",
          "message": "Both doors measure a sheet that will not say how big it is\n\nSpreadsheetSpace.Create sized its grid from reader.RowCount/FieldCount and\nsilently yielded an empty space when the reader would not give them — the\none outcome an adapter must not have, and a divergence from the streaming\ndoor, which has measured such sheets since Part 2 step 7. The fill is now\ntwo named siblings behind one dichotomy: ReadDeclared (the original loop,\nunchanged) and ReadMeasured (rows collected at their own width, the widest\nrow wins, absent trailing cells Blank — the same answer Workbook.Measure\ngives). The guard is rowCount <= 0 alone, deliberately mirroring the\nstreaming door so the two can never disagree about the same file.\n\nThe recorded cause was wrong, and is corrected everywhere it appeared: a\nmissing dimension element does not trigger this — ExcelDataReader derives\nboth counts from a pre-scan of the cells on every format it handles. The\nreachable trigger is a sheet with NO valued cell (rows of formatted-but-\nvalueless cells, a pre-formatted export region). Pinned by the committed\nTestData/no-extent.xlsx (dimensionless AND valueless, with the survey's\nRowsMeasured == 4 doubling as the fixture's own guard against a\nregeneration that quietly stops reaching the path) and a both-doors\nidentity test.\n\nRides along, both owner decisions from this session's discussion:\n- MaxReaders: spec §14 Q2 DECIDED — 3 stays and stops being provisional,\n  because no number is right: reader demand is the declaration's monotone-\n  cursor count, unbounded in principle, data-independent in practice, and\n  the ceiling fails gently (Reopens is the counted, named signal to raise\n  it). Sizing guidance added to docs/streaming.md; per-reader economics\n  (~5s CPU per open, position must be walked, reader-per-row is O(n^2))\n  recorded in the spec.\n- Table's header-derived width: spec §14 Q1 DEFERRED, superseding the\n  2026-09-03 yes — the step-8 interleave delivered the lazy win with\n  today's denotation intact, so the K-1 campaign votes before the\n  denotation change is paid for.\n\nSuite 1,382 -> 1,387; gates green in Debug and 2-core Release.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T14:29:21Z",
          "tree_id": "fc431b0954d2e3a5115a177bd1a21d63c169ffae",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/10027e9f1d263aac70041f0f7166b186324129e8"
        },
        "date": 1788533064336,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 1670755.5390625,
            "unit": "ns",
            "range": "± 8628.594781712825"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 23384441.272916667,
            "unit": "ns",
            "range": "± 73232.51093150073"
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
          "id": "c01531cec6968e544acc578291244292172a00a5",
          "message": "Docs: Part 3 deferred on principle, and .Sized's composite role stated honestly\n\nSpec §13 gains the Part 3 row (bound-aware composite placement): the\nengine's remaining greed sorted into one necessary force (Repeat items —\nthe item's existence is the question), one free force (post-Project\nconsumption, amortised by the root's accounting), and one debt (composite\nchild placement, whose questions have lazy answers nobody asks for).\nDeferred until the first tall sized composite pays the debt — sized\ncomposites in the corpus are short header bands, where settling eagerly\ncosts nothing. The K-1 campaign is the likely judge; the census pin is the\ntripwire.\n\ndocs/streaming.md stops saying \"put the .Sized on the leaf\" as if it were\na law: a sized composite is a legitimate spelling with no leaf equivalent\n— a composite has no intrinsic extent, and the declared band is what\nscopes its internal seeks and settles its consumption.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T15:37:53Z",
          "tree_id": "6188ce68af3130bfba604f38845b0c515958cb34",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/c01531cec6968e544acc578291244292172a00a5"
        },
        "date": 1788537629844,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 2135144.3922991073,
            "unit": "ns",
            "range": "± 10032.691283197897"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 29386863.272916667,
            "unit": "ns",
            "range": "± 202259.85652034282"
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
          "id": "2d73985e95c70f51a2b26d7dc98c3936f1f52d5d",
          "message": "Retention: the live-set floor for the interning change, with the target on the chart\n\nAn eighth CI leg that is not a BenchmarkDotNet family: interning reduces\nRETAINED bytes, not allocations (a duplicate string is allocated by the\nreader before the adapter sees it and dies young after dedup), so the\nAllocated column cannot see it — and retention is deterministic, so it\nneeds no statistical engine. A one-shot job measures live bytes with the\nresult held, emits the same JSON document the rig already stores, and\nrides the same workflow and dashboard as everything else.\n\nBuilding it surfaced two facts worth more than the plumbing:\n\n- The eager door's duplication depends on how the file spells its text.\n  Shared-string cells come back already deduped (the reader returns its\n  table's own instance); inline strings and formula-result cells\n  materialise fresh per cell. A real Excel export is both (the local K-1:\n  9,049 text cells, 2,876 values, 4,016 instances — the formula results\n  are the duplicated half). The family brackets it, and the shared-string\n  row is the priced TARGET: the same cells read 112.0 MB duplicated vs\n  58.2 MB deduped, so ~48% is what a complete eager interner is worth on\n  this shape — short of that is unfinished, not failed.\n- The first fixture boxed decimals a real read never produces (16 MB of\n  boxes in a retained-bytes measurement); the retention fixtures now\n  yield doubles like a reader does. StreamingSpaces is deliberately\n  untouched — changing it would re-baseline that family's history.\n\nScenarios exercise the real seams the interning change will live in: the\neager rows go through SpreadsheetSpace.Create over generated workbooks\n(RetentionWorkbooks: a minimal hand-rolled OOXML writer, no new package;\nthe one deliberate exception to the no-workbooks rule, recorded in\ndocs/benchmarking.md), the streaming rows through the store's chunk fill.\nFloor: eager space held 106.8 MB, results held 82.1 MB both doors\n(byte-identical — streaming's promise stated in the metric), controls\nbyte-identical to their duplicated twins by fixed-width padding. Leg\nruns ~65s, the shortest in the matrix.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T16:47:38Z",
          "tree_id": "0c756ae6dd2d4f17cd84e585c99d7d3ae08fd409",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/2d73985e95c70f51a2b26d7dc98c3936f1f52d5d"
        },
        "date": 1788542161189,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 2076444.8777901786,
            "unit": "ns",
            "range": "± 13385.928616176698"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 29326084.011160713,
            "unit": "ns",
            "range": "± 156385.51161527037"
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
          "id": "eddc5d17c38c715f41cd95d041452deb66f8354c",
          "message": "Interning: equal text shares one instance through both doors\n\nAdapter-level string interning — a find-my-twin table at each door's\nadapt seam. The eager door threads a per-Create-call HashSet through both\nfill paths (one instance per distinct value across every sheet of the\ncall); the streaming door hangs one capped ConcurrentDictionary on the\nWorkbook, plumbed into every store's chunk fill, so a chase reader's\nre-parse dedupes against the first parse. Strings only: every other kind\nis inline in the 24-byte struct or unreachable from the spreadsheet door.\n\nThe win is retention, not allocation — the duplicate is allocated by the\nreader before the adapter sees it and dies in gen0 after dedup, which is\nwhy the Retention leg is the judge and MemoryDiagnoser is blind to it.\nMeasured against the committed floor: eager space held 106.8 -> 55.5 MB,\nlanding on the priced shared-string target to the byte; held results\n82.1 -> 30.8 MB, byte-identical across doors; all three unique controls\nflat to the byte; wall time noise on the 1M-row parse.\n\nThe cap (WorkbookOptions.MaxInternedStrings, default 65,536; 0 = off)\nand the 256-char length guard bound what the book-lifetime table can\npin. Documented as a two-way knob: a full table costs its entries for\nthe book's life — some 40 MB at the default cap — so it turns DOWN, to\n0, for known-unique text, and the docs say so at every site (the rig is\nstructurally blind to the table's own live set: readings are taken with\nthe book closed). Workbook.InterningStatistics reports hits, distinct,\nand estimated bytes (64-bit layout, exact for it; Hits counts fills, so\na reloaded chunk counts again — read against ChunkReloads).\n\nExcelDataReader fact on the record: shared-string cells arrive\npre-deduped from the reader's own SST; the duplication this kills comes\nfrom inline-string cells, formula-result cells, and .xls. Pinned by 36\ntests including a cross-door sharing differential, mutation-checked both\ndirections, and a WeakReference proof that Dispose releases the strings.\nSuite 1,423.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T19:19:56Z",
          "tree_id": "fea5150f427d5b65cf652662879afb3b470547f2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/eddc5d17c38c715f41cd95d041452deb66f8354c"
        },
        "date": 1788556079554,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 2192229.6067708335,
            "unit": "ns",
            "range": "± 27828.191225879906"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 30611985.14732143,
            "unit": "ns",
            "range": "± 315388.2032903082"
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
          "id": "16fef6aa0fb6978c79559b92910716e6ad44c995",
          "message": "netstandard2.0: the packages install on .NET Framework, one source, no forks\n\nAll four libraries multi-target netstandard2.0;netstandard2.1. The rule\nthat made it clean: no #if anywhere — netstandard2.1-only constructs are\nremoved rather than branched on, so both targets compile one source and\ncannot drift.\n\nThe structural change is de-DIMing the incremental calculus. .NET\nFramework's runtime cannot dispatch a default interface member, so the\ndefinitional folds move from DIM bodies to the static Unrect.Core.Scans\n(Fold/FoldSize/FoldArea) and internal ColumnAccumulators.Fold, with every\nimplementation delegating in one line. The guarantee shifts and every doc\nthat claimed it says so honestly: \"eager and lazy cannot disagree by\nconstruction\" is now \"cannot disagree, pinned by the fold-identity suite\"\n— IncrementalStrategyTests asserts SelectRows == a hand-written fold for\nevery factory, and a new incremental strategy carries the obligation to\njoin that theory data. Corollary recorded in capability-seam-notes: the\nDIM \"safety net\" for evolving ISpace is withdrawn — a member added to a\npublished Core interface is now a breaking change with no escape hatch;\ntype-testing is the whole capability recipe.\n\nThe rest the compiler found: System.HashCode (Core hand-rolls its combine\nrather than take Microsoft.Bcl.HashCode — the bundling would have\nsuppressed the dependency from the nuspec and thrown FileNotFoundException\non Framework; both packages stay at zero new dependencies), the\nDictionary-from-pairs constructor, and HashSet<string>.TryGetValue (the\neager intern table is a Dictionary now; the comment that argued HashSet\nwas the honest structure records why it changed). The two polyfills are\nunconditional because neither netstandard has the types; a net5.0+ target\nis what would force an #if.\n\nTests multi-target net8.0;net48 on Windows only (OS-conditioned csproj,\nso Linux CI runs exactly what it ran — verified against all three\nworkflows). Two portable shims replace .NET-5+/6+ APIs the suite used:\nWithTimeout for Task.WaitAsync(TimeSpan), ByReference for\nReferenceEqualityComparer. The full suite has run green on the Framework\nCLR itself: 1,423 on net48 against the netstandard2.0 build, ExcelDataReader's\nown Framework assembly included — verified by the owner on Windows.\nBoth nupkgs verified to carry lib/netstandard2.0 and lib/netstandard2.1.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-05T17:36:21Z",
          "tree_id": "67b64bc3184652344901c8a243f8ce1256d858b9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16fef6aa0fb6978c79559b92910716e6ad44c995"
        },
        "date": 1788630324107,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 2125127.5167410714,
            "unit": "ns",
            "range": "± 18823.35954023003"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 30534963.395833332,
            "unit": "ns",
            "range": "± 402105.91553664906"
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
          "id": "e1d21c2d706b054d252bbea3a996b97f6f7bfc4b",
          "message": "Hygiene: .gitattributes and editorconfig trim — line endings become a decision\n\nThe repo had no .gitattributes, so line endings were whatever tool\ntouched a file last — four .linq files sat CRLF in the index beside an\nLF corpus, and the vocabulary respell nearly flipped three of them\nwholesale (caught in review; a 10-line diff had become 366). One rule\nnow: the index holds LF (* text=auto); .linq and .sln check out CRLF\nbecause their native editors are Windows-bound; spreadsheets and images\nare explicitly binary. This commit carries the one-time renormalization\nso the flip lives here, deliberately, and nowhere else.\n\n.editorconfig gains trailing-whitespace trimming, with markdown exempt\n(a trailing double-space is a hard line break).\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-06T00:49:13Z",
          "tree_id": "ce25b3593543c4145d69070ff009c9f67ff1aa09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/e1d21c2d706b054d252bbea3a996b97f6f7bfc4b"
        },
        "date": 1788712513379,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 1645033.1440805288,
            "unit": "ns",
            "range": "± 7200.203583126293"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 23618007.395833332,
            "unit": "ns",
            "range": "± 94086.33046336622"
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
          "id": "bf1fc2851544d9f87ffb3b76df4fa0b714780243",
          "message": "Merge experiment/typed-spaces: the projection model\n\nThe deepest cut since wave 2: what was called Shape is a projection; the\nspace and its subspaces are the shapes. Seven phases plus pre-merge\ncleanup (docs/design/projection-model-spec.md, judgment record in §10):\nthe modifier doubling killed (43 -> 6 irreducible), IShape -> IProjection\nwith namespace Unrect.Projections, the capability stack (IFormulaSpace,\nCapability<T>() transport, the slicing law, shared-formula\nreconstruction), OrBlank and the eachRow slot, the CaptionMap bind, the\nscoped entry Over<TSpace>() and MapWorkbook, phase-7 acceptance with the\nfourteen recorded refusals, and the three-sweep cleanup.\n\nSuite 1,709 green (net8.0 and net48); both library TFMs 0 warnings.\nBreaking: the rename, the Table family, the SpreadsheetSpace shell\nretirement, and the entries.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-07T23:11:42Z",
          "tree_id": "d091a9f7437fb1ec8d2b35119ab628b2a54047a3",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bf1fc2851544d9f87ffb3b76df4fa0b714780243"
        },
        "date": 1788823321797,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 2138345.096153846,
            "unit": "ns",
            "range": "± 9565.467778923798"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 29505537.737980768,
            "unit": "ns",
            "range": "± 72729.32272478625"
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
          "id": "4a44b787ac6b8b6fa5c487623e316bfe0e364292",
          "message": "Presence semantics and three hygiene fixes: the zero disambiguated\n\nThe algebra's highest-priority open problem (v0.4 §7/§14.1): geometric\nzero carried four meanings. The internal Presence { Read, Empty,\nAbsorbed } now rides beside Consumed on the engine's results — stamped\nat tolerance boundaries (Absorbed), settled-at-zero extents (Empty, read\noff the SETTLED extent so evaluation order and doors agree by\nconstruction), joined through layouts (Read if any child Read, else\nEmpty), defaulting Read. The internal epsilon (NothingProjection) makes\nthe flow-unit law stateable: VerticalFlow is a monoid at L3 for\nsuccessful readings, boundaries pinned.\n\nThe law, earned the hard way: presence explains a stop; the extent\ndecides one. The first implementation let presence decide the repeat\nguard; the law-tests caught a real L1/L2 change on\nVerticalRepeat(item.Optional().Sized(...)) — an Absorbed boundary under\na declared area legitimately consumes it and always kept the run going.\nThe guard is byte-identical to before; presence powers only the new\nteaching Info when a run ends at an absorbed item (D2), and the caught\nshape is pinned as the compatibility-law section of PresenceLawTests.\n\nDesign record: docs/design/presence-and-unit-spec.md (DECIDED, with the\nD5 amendment trail).\n\nAlso, with pins: ChoiceProjection.Summarise renders nested aggregates\ndepth-indented with one location per line; MinOffset() is one canonical\ninstance (HasDeclaredOffset correct by construction); the content-\nmatching rule reduced to one implementation (CellMatching primitives,\nTableView's dictionary keyed on TextComparer; CaptionComparer's\ndeliberate divergence untouched). spike/Phase1Probe removed (a husk).\n\nSuite 1,820 -> 1,862 green; both library TFMs 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-09T15:34:18Z",
          "tree_id": "883452644caa55f06a0f9c2d530ee9c0cece2b92",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4a44b787ac6b8b6fa5c487623e316bfe0e364292"
        },
        "date": 1788969081340,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 1861402.737132353,
            "unit": "ns",
            "range": "± 31618.795488495063"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 23843223.37748016,
            "unit": "ns",
            "range": "± 1082196.3606784747"
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
          "id": "d06708df60e381758b11dde07836c674565409b6",
          "message": "Merge experiment/record-primitive: streaming composites over a bound, the band tiler, Table composed from primitives\n\nThe labels-as-context primitives (ColumnLabels, WithColumnLabels, Record),\nSkipToFirstNonBlankCell, and the uniform offset law; the engine streaming a\ndiscovered bound through a composite (TailSpace, a bound-aware Exceeds, lazy\nflow and repeat slices); VerticalBands/HorizontalBands, the tiler — a\nrepeating fixed-dimension space, distinct from the pattern repeat;\nTable(headerRows, eachRow) composed over the tiler under a UnitProjection,\nwith AsScaffolding and a mark-driven path fold giving it the leaf's own\ndiagnostics; ColumnLabels self-contained, its header parse the one home in\nLabelMap.FromHeader; and the repeat returned to a pure walk, onBlank living\non the tiler alone.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-14T17:43:35Z",
          "tree_id": "1c80a0bed7aeb311bb6e1bdb0933ac5a05562434",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/d06708df60e381758b11dde07836c674565409b6"
        },
        "date": 1789408533815,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 2557286.6434151786,
            "unit": "ns",
            "range": "± 14088.61522364146"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 33521302.49107143,
            "unit": "ns",
            "range": "± 461600.3244350894"
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
          "id": "33f1afa3496afb1fabd2e0242197d1c5d9889568",
          "message": "Merge experiment/point-and-line: the Point substrate — one vocabulary over any ISpace, planes and points as the locators, the kinds in Unrect.Spreadsheets\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-16T04:05:17Z",
          "tree_id": "09d4c757d0f1502df340fb2a2481c31f78e8d8b2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/33f1afa3496afb1fabd2e0242197d1c5d9889568"
        },
        "date": 1789531989174,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 8485389.928385416,
            "unit": "ns",
            "range": "± 16614.502041611344"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 92061469.97619049,
            "unit": "ns",
            "range": "± 204849.93047221177"
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
          "id": "0f34f159151554afcc0555f7afc83fdd486b8c06",
          "message": "Merge experiment/point-follow-ups: Extents into Plane, Unrect.Interactive, and the typed-predicate lift\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-17T02:37:19Z",
          "tree_id": "cfa1d003f75555e9d6f330d316cdd949a1da2b25",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/0f34f159151554afcc0555f7afc83fdd486b8c06"
        },
        "date": 1789613137454,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 8166368.403846154,
            "unit": "ns",
            "range": "± 65465.02702973536"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 89782155.28888889,
            "unit": "ns",
            "range": "± 539252.2341567563"
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
          "id": "717d1ea3b61c226c608194aae5ff909fd4603455",
          "message": "Packaging: Unrect.Interactive ships as its own package\n\nThe project was already packable — id, description, tags, and a\ndependency on exactly Unrect and Unrect.Spreadsheets at the same MinVer\nversion — but the release workflow packed and asserted two packages, and\nthe README and build props named two. Now three.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T01:12:25Z",
          "tree_id": "f01ec5a705245aededd07d5455e7b03ad4f8880d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/717d1ea3b61c226c608194aae5ff909fd4603455"
        },
        "date": 1789796482548,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 116212048.5,
            "unit": "ns",
            "range": "± 137268.33082089172"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 9239102954.8,
            "unit": "ns",
            "range": "± 24398811.47840713"
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
          "id": "4818c36f9e9b0c2b75a4093dbbce6981329fd14b",
          "message": "Merge feature/scaffold-shapes: ScaffoldRecord and ScaffoldClass, on a sheet and as leaves\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T15:35:04Z",
          "tree_id": "d48076b68bb0a93dc6ab6797b10f0ead882156a7",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4818c36f9e9b0c2b75a4093dbbce6981329fd14b"
        },
        "date": 1789847938092,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 115258624.21666668,
            "unit": "ns",
            "range": "± 111906.28997707219"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 9365126489.833334,
            "unit": "ns",
            "range": "± 4762654.579264571"
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
          "id": "bd027f2136cee944da88c1eae17a06b14a05579d",
          "message": "Merge feature/bind-from-row: Table<T> fills a member from the row - Column(member, row => ...)\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T19:49:16Z",
          "tree_id": "88c877501aeddcefb436f136842b9904cda55a09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bd027f2136cee944da88c1eae17a06b14a05579d"
        },
        "date": 1789863952856,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 117070061.0923077,
            "unit": "ns",
            "range": "± 377127.5198632739"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 9523220806.692308,
            "unit": "ns",
            "range": "± 4243863.842889527"
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
          "id": "96b946d5920955eb81be3eab46697a3e4639e2e4",
          "message": "Merge feature/header-bands: headers of several rows, columns addressed by path, flat binding over bands\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T03:17:51Z",
          "tree_id": "33ccb96a2f1d1cb7795dde522c6c586ee8ce479a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/96b946d5920955eb81be3eab46697a3e4639e2e4"
        },
        "date": 1789888951494,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 152201452.90384614,
            "unit": "ns",
            "range": "± 334458.09245212463"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 12276220090.846153,
            "unit": "ns",
            "range": "± 11804635.15069315"
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
          "id": "191ad7e666be18cd9916eb0132108e83e54bd6db",
          "message": "Open questions: IsText is the wrong shape - what it is for, the better shape, and the larger question of facets, recorded and not implemented\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T04:23:54Z",
          "tree_id": "5e6c0e37d52a9f501293d06ad42b1a125ed81250",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/191ad7e666be18cd9916eb0132108e83e54bd6db"
        },
        "date": 1789931368250,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 154532086.26666668,
            "unit": "ns",
            "range": "± 2495632.4700189037"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 11879440145.846153,
            "unit": "ns",
            "range": "± 12815763.767206723"
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
        "date": 1788472137171,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 1950861.7396763393,
            "unit": "ns",
            "range": "± 24004.420781140696"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 1886742.1881009615,
            "unit": "ns",
            "range": "± 5664.678268842075"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 237276.48885672432,
            "unit": "ns",
            "range": "± 1186.7636271297197"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 242700.2598031851,
            "unit": "ns",
            "range": "± 1055.8134218455762"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 1306204.122829861,
            "unit": "ns",
            "range": "± 26989.450298770662"
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
          "id": "f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb",
          "message": "Fix the 2-core CI flake: a blocked-borrower proof needs a started borrower\n\nAReachWaitsForAWarmerRatherThanStartingASecondOpenOfTheSameFile failed on\nthe GitHub runner (ef348dd) on \"the wait is counted\": WarmWaitMilliseconds\nwas 0, and 0 was the honest count. The pool's warmers ride Task.Run and\nthe gated arrangement BLOCKS them inside their opens, one pool thread\neach — on a two-core runner that is the entire starting thread pool, so\nthe test's own Task.Run borrower never started until thread injection got\naround to it. Both blocked-ness assertions passed vacuously (not finished\nbecause not scheduled), and by the time the reach ran, the warm reader was\nparked and there was nothing left to wait for.\n\nReproduced under taskset -c 0,1: three failures in four runs before the\nfix, none in six Debug runs plus a Release run after. The fix is\nOnItsOwnThread (TaskCreationOptions.LongRunning) at the four sites that\nassert a borrower is blocked — a dedicated thread starts unconditionally,\nso \"started, and still not finished\" really does mean \"parked inside\nBorrow\". The three sibling sites could only pass vacuously, never fail,\nbut their proofs were the same lie under starvation. The burst tests\nalready stood on structural evidence (SpinUntil on OpensStarted) and are\nuntouched.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T22:37:18Z",
          "tree_id": "c37ffff8e7e618f8d8cdb3778c429c1bd5259fc9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb"
        },
        "date": 1788475703306,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 1623925.0089285714,
            "unit": "ns",
            "range": "± 45717.13458106314"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 1680035.4471354166,
            "unit": "ns",
            "range": "± 21543.86047248015"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 176359.091796875,
            "unit": "ns",
            "range": "± 312.3754806789942"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 180796.62502034506,
            "unit": "ns",
            "range": "± 82.20140545618344"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 1047398.3604166667,
            "unit": "ns",
            "range": "± 6975.263972322352"
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
          "id": "10027e9f1d263aac70041f0f7166b186324129e8",
          "message": "Both doors measure a sheet that will not say how big it is\n\nSpreadsheetSpace.Create sized its grid from reader.RowCount/FieldCount and\nsilently yielded an empty space when the reader would not give them — the\none outcome an adapter must not have, and a divergence from the streaming\ndoor, which has measured such sheets since Part 2 step 7. The fill is now\ntwo named siblings behind one dichotomy: ReadDeclared (the original loop,\nunchanged) and ReadMeasured (rows collected at their own width, the widest\nrow wins, absent trailing cells Blank — the same answer Workbook.Measure\ngives). The guard is rowCount <= 0 alone, deliberately mirroring the\nstreaming door so the two can never disagree about the same file.\n\nThe recorded cause was wrong, and is corrected everywhere it appeared: a\nmissing dimension element does not trigger this — ExcelDataReader derives\nboth counts from a pre-scan of the cells on every format it handles. The\nreachable trigger is a sheet with NO valued cell (rows of formatted-but-\nvalueless cells, a pre-formatted export region). Pinned by the committed\nTestData/no-extent.xlsx (dimensionless AND valueless, with the survey's\nRowsMeasured == 4 doubling as the fixture's own guard against a\nregeneration that quietly stops reaching the path) and a both-doors\nidentity test.\n\nRides along, both owner decisions from this session's discussion:\n- MaxReaders: spec §14 Q2 DECIDED — 3 stays and stops being provisional,\n  because no number is right: reader demand is the declaration's monotone-\n  cursor count, unbounded in principle, data-independent in practice, and\n  the ceiling fails gently (Reopens is the counted, named signal to raise\n  it). Sizing guidance added to docs/streaming.md; per-reader economics\n  (~5s CPU per open, position must be walked, reader-per-row is O(n^2))\n  recorded in the spec.\n- Table's header-derived width: spec §14 Q1 DEFERRED, superseding the\n  2026-09-03 yes — the step-8 interleave delivered the lazy win with\n  today's denotation intact, so the K-1 campaign votes before the\n  denotation change is paid for.\n\nSuite 1,382 -> 1,387; gates green in Debug and 2-core Release.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T14:29:21Z",
          "tree_id": "fc431b0954d2e3a5115a177bd1a21d63c169ffae",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/10027e9f1d263aac70041f0f7166b186324129e8"
        },
        "date": 1788533064546,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 1708442.9326822916,
            "unit": "ns",
            "range": "± 18005.96369333704"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 1736661.9540364584,
            "unit": "ns",
            "range": "± 31479.55508073869"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 155710.11560058594,
            "unit": "ns",
            "range": "± 485.1248766857986"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 164527.29262288412,
            "unit": "ns",
            "range": "± 803.1168430168726"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 1077766.309765625,
            "unit": "ns",
            "range": "± 17590.462648114484"
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
          "id": "c01531cec6968e544acc578291244292172a00a5",
          "message": "Docs: Part 3 deferred on principle, and .Sized's composite role stated honestly\n\nSpec §13 gains the Part 3 row (bound-aware composite placement): the\nengine's remaining greed sorted into one necessary force (Repeat items —\nthe item's existence is the question), one free force (post-Project\nconsumption, amortised by the root's accounting), and one debt (composite\nchild placement, whose questions have lazy answers nobody asks for).\nDeferred until the first tall sized composite pays the debt — sized\ncomposites in the corpus are short header bands, where settling eagerly\ncosts nothing. The K-1 campaign is the likely judge; the census pin is the\ntripwire.\n\ndocs/streaming.md stops saying \"put the .Sized on the leaf\" as if it were\na law: a sized composite is a legitimate spelling with no leaf equivalent\n— a composite has no intrinsic extent, and the declared band is what\nscopes its internal seeks and settles its consumption.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T15:37:53Z",
          "tree_id": "6188ce68af3130bfba604f38845b0c515958cb34",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/c01531cec6968e544acc578291244292172a00a5"
        },
        "date": 1788537630056,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 2112074.611328125,
            "unit": "ns",
            "range": "± 7949.774288629191"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 2091353.7274639423,
            "unit": "ns",
            "range": "± 6484.971585135688"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 224746.10305175782,
            "unit": "ns",
            "range": "± 472.09203368017995"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 232742.75307992788,
            "unit": "ns",
            "range": "± 333.0013156522098"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 1359558.9286458334,
            "unit": "ns",
            "range": "± 5238.055516854218"
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
          "id": "2d73985e95c70f51a2b26d7dc98c3936f1f52d5d",
          "message": "Retention: the live-set floor for the interning change, with the target on the chart\n\nAn eighth CI leg that is not a BenchmarkDotNet family: interning reduces\nRETAINED bytes, not allocations (a duplicate string is allocated by the\nreader before the adapter sees it and dies young after dedup), so the\nAllocated column cannot see it — and retention is deterministic, so it\nneeds no statistical engine. A one-shot job measures live bytes with the\nresult held, emits the same JSON document the rig already stores, and\nrides the same workflow and dashboard as everything else.\n\nBuilding it surfaced two facts worth more than the plumbing:\n\n- The eager door's duplication depends on how the file spells its text.\n  Shared-string cells come back already deduped (the reader returns its\n  table's own instance); inline strings and formula-result cells\n  materialise fresh per cell. A real Excel export is both (the local K-1:\n  9,049 text cells, 2,876 values, 4,016 instances — the formula results\n  are the duplicated half). The family brackets it, and the shared-string\n  row is the priced TARGET: the same cells read 112.0 MB duplicated vs\n  58.2 MB deduped, so ~48% is what a complete eager interner is worth on\n  this shape — short of that is unfinished, not failed.\n- The first fixture boxed decimals a real read never produces (16 MB of\n  boxes in a retained-bytes measurement); the retention fixtures now\n  yield doubles like a reader does. StreamingSpaces is deliberately\n  untouched — changing it would re-baseline that family's history.\n\nScenarios exercise the real seams the interning change will live in: the\neager rows go through SpreadsheetSpace.Create over generated workbooks\n(RetentionWorkbooks: a minimal hand-rolled OOXML writer, no new package;\nthe one deliberate exception to the no-workbooks rule, recorded in\ndocs/benchmarking.md), the streaming rows through the store's chunk fill.\nFloor: eager space held 106.8 MB, results held 82.1 MB both doors\n(byte-identical — streaming's promise stated in the metric), controls\nbyte-identical to their duplicated twins by fixed-width padding. Leg\nruns ~65s, the shortest in the matrix.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T16:47:38Z",
          "tree_id": "0c756ae6dd2d4f17cd84e585c99d7d3ae08fd409",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/2d73985e95c70f51a2b26d7dc98c3936f1f52d5d"
        },
        "date": 1788542161407,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 2217115.175,
            "unit": "ns",
            "range": "± 25141.445577277365"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 2290203.323660714,
            "unit": "ns",
            "range": "± 16535.262209747372"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 243071.77057291666,
            "unit": "ns",
            "range": "± 1951.4722571712225"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 242121.45724051338,
            "unit": "ns",
            "range": "± 368.22230251635904"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 1412781.0311197916,
            "unit": "ns",
            "range": "± 14171.81162389612"
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
          "id": "eddc5d17c38c715f41cd95d041452deb66f8354c",
          "message": "Interning: equal text shares one instance through both doors\n\nAdapter-level string interning — a find-my-twin table at each door's\nadapt seam. The eager door threads a per-Create-call HashSet through both\nfill paths (one instance per distinct value across every sheet of the\ncall); the streaming door hangs one capped ConcurrentDictionary on the\nWorkbook, plumbed into every store's chunk fill, so a chase reader's\nre-parse dedupes against the first parse. Strings only: every other kind\nis inline in the 24-byte struct or unreachable from the spreadsheet door.\n\nThe win is retention, not allocation — the duplicate is allocated by the\nreader before the adapter sees it and dies in gen0 after dedup, which is\nwhy the Retention leg is the judge and MemoryDiagnoser is blind to it.\nMeasured against the committed floor: eager space held 106.8 -> 55.5 MB,\nlanding on the priced shared-string target to the byte; held results\n82.1 -> 30.8 MB, byte-identical across doors; all three unique controls\nflat to the byte; wall time noise on the 1M-row parse.\n\nThe cap (WorkbookOptions.MaxInternedStrings, default 65,536; 0 = off)\nand the 256-char length guard bound what the book-lifetime table can\npin. Documented as a two-way knob: a full table costs its entries for\nthe book's life — some 40 MB at the default cap — so it turns DOWN, to\n0, for known-unique text, and the docs say so at every site (the rig is\nstructurally blind to the table's own live set: readings are taken with\nthe book closed). Workbook.InterningStatistics reports hits, distinct,\nand estimated bytes (64-bit layout, exact for it; Hits counts fills, so\na reloaded chunk counts again — read against ChunkReloads).\n\nExcelDataReader fact on the record: shared-string cells arrive\npre-deduped from the reader's own SST; the duplication this kills comes\nfrom inline-string cells, formula-result cells, and .xls. Pinned by 36\ntests including a cross-door sharing differential, mutation-checked both\ndirections, and a WeakReference proof that Dispose releases the strings.\nSuite 1,423.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T19:19:56Z",
          "tree_id": "fea5150f427d5b65cf652662879afb3b470547f2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/eddc5d17c38c715f41cd95d041452deb66f8354c"
        },
        "date": 1788556079733,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 2074217.2555338542,
            "unit": "ns",
            "range": "± 6507.291379831539"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 2119863.5440104166,
            "unit": "ns",
            "range": "± 28715.507525721954"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 225359.43318058894,
            "unit": "ns",
            "range": "± 392.96887111131565"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 229448.58201090494,
            "unit": "ns",
            "range": "± 317.95219073421714"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 1348781.5697916667,
            "unit": "ns",
            "range": "± 6559.3319355792655"
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
          "id": "16fef6aa0fb6978c79559b92910716e6ad44c995",
          "message": "netstandard2.0: the packages install on .NET Framework, one source, no forks\n\nAll four libraries multi-target netstandard2.0;netstandard2.1. The rule\nthat made it clean: no #if anywhere — netstandard2.1-only constructs are\nremoved rather than branched on, so both targets compile one source and\ncannot drift.\n\nThe structural change is de-DIMing the incremental calculus. .NET\nFramework's runtime cannot dispatch a default interface member, so the\ndefinitional folds move from DIM bodies to the static Unrect.Core.Scans\n(Fold/FoldSize/FoldArea) and internal ColumnAccumulators.Fold, with every\nimplementation delegating in one line. The guarantee shifts and every doc\nthat claimed it says so honestly: \"eager and lazy cannot disagree by\nconstruction\" is now \"cannot disagree, pinned by the fold-identity suite\"\n— IncrementalStrategyTests asserts SelectRows == a hand-written fold for\nevery factory, and a new incremental strategy carries the obligation to\njoin that theory data. Corollary recorded in capability-seam-notes: the\nDIM \"safety net\" for evolving ISpace is withdrawn — a member added to a\npublished Core interface is now a breaking change with no escape hatch;\ntype-testing is the whole capability recipe.\n\nThe rest the compiler found: System.HashCode (Core hand-rolls its combine\nrather than take Microsoft.Bcl.HashCode — the bundling would have\nsuppressed the dependency from the nuspec and thrown FileNotFoundException\non Framework; both packages stay at zero new dependencies), the\nDictionary-from-pairs constructor, and HashSet<string>.TryGetValue (the\neager intern table is a Dictionary now; the comment that argued HashSet\nwas the honest structure records why it changed). The two polyfills are\nunconditional because neither netstandard has the types; a net5.0+ target\nis what would force an #if.\n\nTests multi-target net8.0;net48 on Windows only (OS-conditioned csproj,\nso Linux CI runs exactly what it ran — verified against all three\nworkflows). Two portable shims replace .NET-5+/6+ APIs the suite used:\nWithTimeout for Task.WaitAsync(TimeSpan), ByReference for\nReferenceEqualityComparer. The full suite has run green on the Framework\nCLR itself: 1,423 on net48 against the netstandard2.0 build, ExcelDataReader's\nown Framework assembly included — verified by the owner on Windows.\nBoth nupkgs verified to carry lib/netstandard2.0 and lib/netstandard2.1.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-05T17:36:21Z",
          "tree_id": "67b64bc3184652344901c8a243f8ce1256d858b9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16fef6aa0fb6978c79559b92910716e6ad44c995"
        },
        "date": 1788630324294,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 2186359.9229910714,
            "unit": "ns",
            "range": "± 16496.45064413659"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 2122686.300520833,
            "unit": "ns",
            "range": "± 21185.247325819124"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 240043.63980538506,
            "unit": "ns",
            "range": "± 367.39789146177696"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 233625.96407376803,
            "unit": "ns",
            "range": "± 161.17507731802604"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 1360673.8890625,
            "unit": "ns",
            "range": "± 9610.826032943814"
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
          "id": "e1d21c2d706b054d252bbea3a996b97f6f7bfc4b",
          "message": "Hygiene: .gitattributes and editorconfig trim — line endings become a decision\n\nThe repo had no .gitattributes, so line endings were whatever tool\ntouched a file last — four .linq files sat CRLF in the index beside an\nLF corpus, and the vocabulary respell nearly flipped three of them\nwholesale (caught in review; a 10-line diff had become 366). One rule\nnow: the index holds LF (* text=auto); .linq and .sln check out CRLF\nbecause their native editors are Windows-bound; spreadsheets and images\nare explicitly binary. This commit carries the one-time renormalization\nso the flip lives here, deliberately, and nowhere else.\n\n.editorconfig gains trailing-whitespace trimming, with markdown exempt\n(a trailing double-space is a hard line break).\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-06T00:49:13Z",
          "tree_id": "ce25b3593543c4145d69070ff009c9f67ff1aa09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/e1d21c2d706b054d252bbea3a996b97f6f7bfc4b"
        },
        "date": 1788712513575,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 1192346.6865234375,
            "unit": "ns",
            "range": "± 19445.737897213225"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 1207929.2671875,
            "unit": "ns",
            "range": "± 11632.079205711485"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 115825.8216389974,
            "unit": "ns",
            "range": "± 977.5888180862589"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 120641.95217285157,
            "unit": "ns",
            "range": "± 896.7486944812345"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 749985.125,
            "unit": "ns",
            "range": "± 5613.0140376645995"
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
          "id": "bf1fc2851544d9f87ffb3b76df4fa0b714780243",
          "message": "Merge experiment/typed-spaces: the projection model\n\nThe deepest cut since wave 2: what was called Shape is a projection; the\nspace and its subspaces are the shapes. Seven phases plus pre-merge\ncleanup (docs/design/projection-model-spec.md, judgment record in §10):\nthe modifier doubling killed (43 -> 6 irreducible), IShape -> IProjection\nwith namespace Unrect.Projections, the capability stack (IFormulaSpace,\nCapability<T>() transport, the slicing law, shared-formula\nreconstruction), OrBlank and the eachRow slot, the CaptionMap bind, the\nscoped entry Over<TSpace>() and MapWorkbook, phase-7 acceptance with the\nfourteen recorded refusals, and the three-sweep cleanup.\n\nSuite 1,709 green (net8.0 and net48); both library TFMs 0 warnings.\nBreaking: the rename, the Table family, the SpreadsheetSpace shell\nretirement, and the entries.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-07T23:11:42Z",
          "tree_id": "d091a9f7437fb1ec8d2b35119ab628b2a54047a3",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bf1fc2851544d9f87ffb3b76df4fa0b714780243"
        },
        "date": 1788823322031,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 2156971.361458333,
            "unit": "ns",
            "range": "± 18599.455684236797"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 2186070.6593191964,
            "unit": "ns",
            "range": "± 8044.407904217717"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 219638.26276506696,
            "unit": "ns",
            "range": "± 945.0311714160009"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 228970.8541608538,
            "unit": "ns",
            "range": "± 468.60569718027045"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 1414693.4454427084,
            "unit": "ns",
            "range": "± 6814.626160085645"
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
          "id": "4a44b787ac6b8b6fa5c487623e316bfe0e364292",
          "message": "Presence semantics and three hygiene fixes: the zero disambiguated\n\nThe algebra's highest-priority open problem (v0.4 §7/§14.1): geometric\nzero carried four meanings. The internal Presence { Read, Empty,\nAbsorbed } now rides beside Consumed on the engine's results — stamped\nat tolerance boundaries (Absorbed), settled-at-zero extents (Empty, read\noff the SETTLED extent so evaluation order and doors agree by\nconstruction), joined through layouts (Read if any child Read, else\nEmpty), defaulting Read. The internal epsilon (NothingProjection) makes\nthe flow-unit law stateable: VerticalFlow is a monoid at L3 for\nsuccessful readings, boundaries pinned.\n\nThe law, earned the hard way: presence explains a stop; the extent\ndecides one. The first implementation let presence decide the repeat\nguard; the law-tests caught a real L1/L2 change on\nVerticalRepeat(item.Optional().Sized(...)) — an Absorbed boundary under\na declared area legitimately consumes it and always kept the run going.\nThe guard is byte-identical to before; presence powers only the new\nteaching Info when a run ends at an absorbed item (D2), and the caught\nshape is pinned as the compatibility-law section of PresenceLawTests.\n\nDesign record: docs/design/presence-and-unit-spec.md (DECIDED, with the\nD5 amendment trail).\n\nAlso, with pins: ChoiceProjection.Summarise renders nested aggregates\ndepth-indented with one location per line; MinOffset() is one canonical\ninstance (HasDeclaredOffset correct by construction); the content-\nmatching rule reduced to one implementation (CellMatching primitives,\nTableView's dictionary keyed on TextComparer; CaptionComparer's\ndeliberate divergence untouched). spike/Phase1Probe removed (a husk).\n\nSuite 1,820 -> 1,862 green; both library TFMs 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-09T15:34:18Z",
          "tree_id": "883452644caa55f06a0f9c2d530ee9c0cece2b92",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4a44b787ac6b8b6fa5c487623e316bfe0e364292"
        },
        "date": 1788969081602,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 1660452.76796875,
            "unit": "ns",
            "range": "± 19675.039485394806"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 1648292.9038783482,
            "unit": "ns",
            "range": "± 6450.332905234764"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 169045.57221330915,
            "unit": "ns",
            "range": "± 186.6057645751899"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 173485.0171875,
            "unit": "ns",
            "range": "± 295.8321209592785"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 1030434.1111979167,
            "unit": "ns",
            "range": "± 5802.708672528698"
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
          "id": "d06708df60e381758b11dde07836c674565409b6",
          "message": "Merge experiment/record-primitive: streaming composites over a bound, the band tiler, Table composed from primitives\n\nThe labels-as-context primitives (ColumnLabels, WithColumnLabels, Record),\nSkipToFirstNonBlankCell, and the uniform offset law; the engine streaming a\ndiscovered bound through a composite (TailSpace, a bound-aware Exceeds, lazy\nflow and repeat slices); VerticalBands/HorizontalBands, the tiler — a\nrepeating fixed-dimension space, distinct from the pattern repeat;\nTable(headerRows, eachRow) composed over the tiler under a UnitProjection,\nwith AsScaffolding and a mark-driven path fold giving it the leaf's own\ndiagnostics; ColumnLabels self-contained, its header parse the one home in\nLabelMap.FromHeader; and the repeat returned to a pure walk, onBlank living\non the tiler alone.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-14T17:43:35Z",
          "tree_id": "1c80a0bed7aeb311bb6e1bdb0933ac5a05562434",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/d06708df60e381758b11dde07836c674565409b6"
        },
        "date": 1789408534082,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 2549537.36328125,
            "unit": "ns",
            "range": "± 17619.043133098698"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 2585586.4598958334,
            "unit": "ns",
            "range": "± 14425.05337765218"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 218129.9633225661,
            "unit": "ns",
            "range": "± 797.8863449510229"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 218197.00510817306,
            "unit": "ns",
            "range": "± 784.7233444139493"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 1575267.3927283655,
            "unit": "ns",
            "range": "± 6796.253199799842"
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
          "id": "33f1afa3496afb1fabd2e0242197d1c5d9889568",
          "message": "Merge experiment/point-and-line: the Point substrate — one vocabulary over any ISpace, planes and points as the locators, the kinds in Unrect.Spreadsheets\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-16T04:05:17Z",
          "tree_id": "09d4c757d0f1502df340fb2a2481c31f78e8d8b2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/33f1afa3496afb1fabd2e0242197d1c5d9889568"
        },
        "date": 1789531989443,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 8508544.07700893,
            "unit": "ns",
            "range": "± 86211.54134426336"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 8331926.257291666,
            "unit": "ns",
            "range": "± 79393.39920301481"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 283661.8287635216,
            "unit": "ns",
            "range": "± 230.85219911376404"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 295320.85114397324,
            "unit": "ns",
            "range": "± 442.1355088608685"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 4889317.585416666,
            "unit": "ns",
            "range": "± 56796.707069100725"
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
          "id": "0f34f159151554afcc0555f7afc83fdd486b8c06",
          "message": "Merge experiment/point-follow-ups: Extents into Plane, Unrect.Interactive, and the typed-predicate lift\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-17T02:37:19Z",
          "tree_id": "cfa1d003f75555e9d6f330d316cdd949a1da2b25",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/0f34f159151554afcc0555f7afc83fdd486b8c06"
        },
        "date": 1789613137724,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 8171592.963068182,
            "unit": "ns",
            "range": "± 199843.2886273306"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 8019353.739182692,
            "unit": "ns",
            "range": "± 55278.556566821295"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 283853.12451171875,
            "unit": "ns",
            "range": "± 665.1097954365555"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 287848.8308105469,
            "unit": "ns",
            "range": "± 272.83275955972186"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 4683315.604166667,
            "unit": "ns",
            "range": "± 44321.228664936716"
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
          "id": "717d1ea3b61c226c608194aae5ff909fd4603455",
          "message": "Packaging: Unrect.Interactive ships as its own package\n\nThe project was already packable — id, description, tags, and a\ndependency on exactly Unrect and Unrect.Spreadsheets at the same MinVer\nversion — but the release workflow packed and asserted two packages, and\nthe README and build props named two. Now three.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T01:12:25Z",
          "tree_id": "f01ec5a705245aededd07d5455e7b03ad4f8880d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/717d1ea3b61c226c608194aae5ff909fd4603455"
        },
        "date": 1789796482770,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 89323994.17857145,
            "unit": "ns",
            "range": "± 286312.4819760478"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 89827888.84444445,
            "unit": "ns",
            "range": "± 690035.4335062739"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 324964891.54347825,
            "unit": "ns",
            "range": "± 7967684.860130264"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 314306191.25,
            "unit": "ns",
            "range": "± 2368106.4231014685"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 148041254.84615386,
            "unit": "ns",
            "range": "± 457844.7615440392"
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
          "id": "4818c36f9e9b0c2b75a4093dbbce6981329fd14b",
          "message": "Merge feature/scaffold-shapes: ScaffoldRecord and ScaffoldClass, on a sheet and as leaves\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T15:35:04Z",
          "tree_id": "d48076b68bb0a93dc6ab6797b10f0ead882156a7",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4818c36f9e9b0c2b75a4093dbbce6981329fd14b"
        },
        "date": 1789847938330,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 145622646.6607143,
            "unit": "ns",
            "range": "± 1307713.3880597858"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 144131154.98333332,
            "unit": "ns",
            "range": "± 1281384.0080392864"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 528294622.7692308,
            "unit": "ns",
            "range": "± 611360.8242499069"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 534378959.6,
            "unit": "ns",
            "range": "± 799308.9299136384"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 240781639.97777784,
            "unit": "ns",
            "range": "± 2221404.820021421"
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
          "id": "bd027f2136cee944da88c1eae17a06b14a05579d",
          "message": "Merge feature/bind-from-row: Table<T> fills a member from the row - Column(member, row => ...)\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T19:49:16Z",
          "tree_id": "88c877501aeddcefb436f136842b9904cda55a09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bd027f2136cee944da88c1eae17a06b14a05579d"
        },
        "date": 1789863953143,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 118568784.01428573,
            "unit": "ns",
            "range": "± 349883.6779404285"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 118485487.42857143,
            "unit": "ns",
            "range": "± 594692.1573589843"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 423285629.26666665,
            "unit": "ns",
            "range": "± 1444190.8959945343"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 421721409.3076923,
            "unit": "ns",
            "range": "± 1350519.046738536"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 202874950.7777778,
            "unit": "ns",
            "range": "± 1160829.416830108"
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
          "id": "96b946d5920955eb81be3eab46697a3e4639e2e4",
          "message": "Merge feature/header-bands: headers of several rows, columns addressed by path, flat binding over bands\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T03:17:51Z",
          "tree_id": "33ccb96a2f1d1cb7795dde522c6c586ee8ce479a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/96b946d5920955eb81be3eab46697a3e4639e2e4"
        },
        "date": 1789888951767,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 92129533.78205127,
            "unit": "ns",
            "range": "± 1079313.978980849"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 96182646.57272726,
            "unit": "ns",
            "range": "± 2330734.017427612"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 315985454.5833333,
            "unit": "ns",
            "range": "± 1437840.013324893"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 316211073.7083333,
            "unit": "ns",
            "range": "± 1323286.9891843956"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 155759975.8382353,
            "unit": "ns",
            "range": "± 2974207.4292387324"
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
          "id": "191ad7e666be18cd9916eb0132108e83e54bd6db",
          "message": "Open questions: IsText is the wrong shape - what it is for, the better shape, and the larger question of facets, recorded and not implemented\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T04:23:54Z",
          "tree_id": "5e6c0e37d52a9f501293d06ad42b1a125ed81250",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/191ad7e666be18cd9916eb0132108e83e54bd6db"
        },
        "date": 1789931368529,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 148604022.32692307,
            "unit": "ns",
            "range": "± 354112.2009679392"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 149872491.91666666,
            "unit": "ns",
            "range": "± 402298.242070345"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 531595370.64285713,
            "unit": "ns",
            "range": "± 421318.3052236811"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 531505086.6666667,
            "unit": "ns",
            "range": "± 574513.5966691012"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 239792310.51111105,
            "unit": "ns",
            "range": "± 247177.11608290343"
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
        "date": 1788472137496,
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
          "id": "f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb",
          "message": "Fix the 2-core CI flake: a blocked-borrower proof needs a started borrower\n\nAReachWaitsForAWarmerRatherThanStartingASecondOpenOfTheSameFile failed on\nthe GitHub runner (ef348dd) on \"the wait is counted\": WarmWaitMilliseconds\nwas 0, and 0 was the honest count. The pool's warmers ride Task.Run and\nthe gated arrangement BLOCKS them inside their opens, one pool thread\neach — on a two-core runner that is the entire starting thread pool, so\nthe test's own Task.Run borrower never started until thread injection got\naround to it. Both blocked-ness assertions passed vacuously (not finished\nbecause not scheduled), and by the time the reach ran, the warm reader was\nparked and there was nothing left to wait for.\n\nReproduced under taskset -c 0,1: three failures in four runs before the\nfix, none in six Debug runs plus a Release run after. The fix is\nOnItsOwnThread (TaskCreationOptions.LongRunning) at the four sites that\nassert a borrower is blocked — a dedicated thread starts unconditionally,\nso \"started, and still not finished\" really does mean \"parked inside\nBorrow\". The three sibling sites could only pass vacuously, never fail,\nbut their proofs were the same lie under starvation. The burst tests\nalready stood on structural evidence (SpinUntil on OpensStarted) and are\nuntouched.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T22:37:18Z",
          "tree_id": "c37ffff8e7e618f8d8cdb3778c429c1bd5259fc9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb"
        },
        "date": 1788475703619,
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
          "id": "10027e9f1d263aac70041f0f7166b186324129e8",
          "message": "Both doors measure a sheet that will not say how big it is\n\nSpreadsheetSpace.Create sized its grid from reader.RowCount/FieldCount and\nsilently yielded an empty space when the reader would not give them — the\none outcome an adapter must not have, and a divergence from the streaming\ndoor, which has measured such sheets since Part 2 step 7. The fill is now\ntwo named siblings behind one dichotomy: ReadDeclared (the original loop,\nunchanged) and ReadMeasured (rows collected at their own width, the widest\nrow wins, absent trailing cells Blank — the same answer Workbook.Measure\ngives). The guard is rowCount <= 0 alone, deliberately mirroring the\nstreaming door so the two can never disagree about the same file.\n\nThe recorded cause was wrong, and is corrected everywhere it appeared: a\nmissing dimension element does not trigger this — ExcelDataReader derives\nboth counts from a pre-scan of the cells on every format it handles. The\nreachable trigger is a sheet with NO valued cell (rows of formatted-but-\nvalueless cells, a pre-formatted export region). Pinned by the committed\nTestData/no-extent.xlsx (dimensionless AND valueless, with the survey's\nRowsMeasured == 4 doubling as the fixture's own guard against a\nregeneration that quietly stops reaching the path) and a both-doors\nidentity test.\n\nRides along, both owner decisions from this session's discussion:\n- MaxReaders: spec §14 Q2 DECIDED — 3 stays and stops being provisional,\n  because no number is right: reader demand is the declaration's monotone-\n  cursor count, unbounded in principle, data-independent in practice, and\n  the ceiling fails gently (Reopens is the counted, named signal to raise\n  it). Sizing guidance added to docs/streaming.md; per-reader economics\n  (~5s CPU per open, position must be walked, reader-per-row is O(n^2))\n  recorded in the spec.\n- Table's header-derived width: spec §14 Q1 DEFERRED, superseding the\n  2026-09-03 yes — the step-8 interleave delivered the lazy win with\n  today's denotation intact, so the K-1 campaign votes before the\n  denotation change is paid for.\n\nSuite 1,382 -> 1,387; gates green in Debug and 2-core Release.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T14:29:21Z",
          "tree_id": "fc431b0954d2e3a5115a177bd1a21d63c169ffae",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/10027e9f1d263aac70041f0f7166b186324129e8"
        },
        "date": 1788533064955,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 3040321,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 812320,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 3008,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 1344937,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 1408,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 588,
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
          "id": "c01531cec6968e544acc578291244292172a00a5",
          "message": "Docs: Part 3 deferred on principle, and .Sized's composite role stated honestly\n\nSpec §13 gains the Part 3 row (bound-aware composite placement): the\nengine's remaining greed sorted into one necessary force (Repeat items —\nthe item's existence is the question), one free force (post-Project\nconsumption, amortised by the root's accounting), and one debt (composite\nchild placement, whose questions have lazy answers nobody asks for).\nDeferred until the first tall sized composite pays the debt — sized\ncomposites in the corpus are short header bands, where settling eagerly\ncosts nothing. The K-1 campaign is the likely judge; the census pin is the\ntripwire.\n\ndocs/streaming.md stops saying \"put the .Sized on the leaf\" as if it were\na law: a sized composite is a legitimate spelling with no leaf equivalent\n— a composite has no intrinsic extent, and the declared band is what\nscopes its internal seeks and settles its consumption.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T15:37:53Z",
          "tree_id": "6188ce68af3130bfba604f38845b0c515958cb34",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/c01531cec6968e544acc578291244292172a00a5"
        },
        "date": 1788537630494,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 3040321,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 812320,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 3008,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 1344937,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 1408,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 588,
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
          "id": "2d73985e95c70f51a2b26d7dc98c3936f1f52d5d",
          "message": "Retention: the live-set floor for the interning change, with the target on the chart\n\nAn eighth CI leg that is not a BenchmarkDotNet family: interning reduces\nRETAINED bytes, not allocations (a duplicate string is allocated by the\nreader before the adapter sees it and dies young after dedup), so the\nAllocated column cannot see it — and retention is deterministic, so it\nneeds no statistical engine. A one-shot job measures live bytes with the\nresult held, emits the same JSON document the rig already stores, and\nrides the same workflow and dashboard as everything else.\n\nBuilding it surfaced two facts worth more than the plumbing:\n\n- The eager door's duplication depends on how the file spells its text.\n  Shared-string cells come back already deduped (the reader returns its\n  table's own instance); inline strings and formula-result cells\n  materialise fresh per cell. A real Excel export is both (the local K-1:\n  9,049 text cells, 2,876 values, 4,016 instances — the formula results\n  are the duplicated half). The family brackets it, and the shared-string\n  row is the priced TARGET: the same cells read 112.0 MB duplicated vs\n  58.2 MB deduped, so ~48% is what a complete eager interner is worth on\n  this shape — short of that is unfinished, not failed.\n- The first fixture boxed decimals a real read never produces (16 MB of\n  boxes in a retained-bytes measurement); the retention fixtures now\n  yield doubles like a reader does. StreamingSpaces is deliberately\n  untouched — changing it would re-baseline that family's history.\n\nScenarios exercise the real seams the interning change will live in: the\neager rows go through SpreadsheetSpace.Create over generated workbooks\n(RetentionWorkbooks: a minimal hand-rolled OOXML writer, no new package;\nthe one deliberate exception to the no-workbooks rule, recorded in\ndocs/benchmarking.md), the streaming rows through the store's chunk fill.\nFloor: eager space held 106.8 MB, results held 82.1 MB both doors\n(byte-identical — streaming's promise stated in the metric), controls\nbyte-identical to their duplicated twins by fixed-width padding. Leg\nruns ~65s, the shortest in the matrix.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T16:47:38Z",
          "tree_id": "0c756ae6dd2d4f17cd84e585c99d7d3ae08fd409",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/2d73985e95c70f51a2b26d7dc98c3936f1f52d5d"
        },
        "date": 1788542161879,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 3040321,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 812320,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 3008,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 1344937,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 1408,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 588,
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
          "id": "eddc5d17c38c715f41cd95d041452deb66f8354c",
          "message": "Interning: equal text shares one instance through both doors\n\nAdapter-level string interning — a find-my-twin table at each door's\nadapt seam. The eager door threads a per-Create-call HashSet through both\nfill paths (one instance per distinct value across every sheet of the\ncall); the streaming door hangs one capped ConcurrentDictionary on the\nWorkbook, plumbed into every store's chunk fill, so a chase reader's\nre-parse dedupes against the first parse. Strings only: every other kind\nis inline in the 24-byte struct or unreachable from the spreadsheet door.\n\nThe win is retention, not allocation — the duplicate is allocated by the\nreader before the adapter sees it and dies in gen0 after dedup, which is\nwhy the Retention leg is the judge and MemoryDiagnoser is blind to it.\nMeasured against the committed floor: eager space held 106.8 -> 55.5 MB,\nlanding on the priced shared-string target to the byte; held results\n82.1 -> 30.8 MB, byte-identical across doors; all three unique controls\nflat to the byte; wall time noise on the 1M-row parse.\n\nThe cap (WorkbookOptions.MaxInternedStrings, default 65,536; 0 = off)\nand the 256-char length guard bound what the book-lifetime table can\npin. Documented as a two-way knob: a full table costs its entries for\nthe book's life — some 40 MB at the default cap — so it turns DOWN, to\n0, for known-unique text, and the docs say so at every site (the rig is\nstructurally blind to the table's own live set: readings are taken with\nthe book closed). Workbook.InterningStatistics reports hits, distinct,\nand estimated bytes (64-bit layout, exact for it; Hits counts fills, so\na reloaded chunk counts again — read against ChunkReloads).\n\nExcelDataReader fact on the record: shared-string cells arrive\npre-deduped from the reader's own SST; the duplication this kills comes\nfrom inline-string cells, formula-result cells, and .xls. Pinned by 36\ntests including a cross-door sharing differential, mutation-checked both\ndirections, and a WeakReference proof that Dispose releases the strings.\nSuite 1,423.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T19:19:56Z",
          "tree_id": "fea5150f427d5b65cf652662879afb3b470547f2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/eddc5d17c38c715f41cd95d041452deb66f8354c"
        },
        "date": 1788556080090,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 3040321,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 812320,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 3008,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 1344937,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 1408,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 588,
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
          "id": "16fef6aa0fb6978c79559b92910716e6ad44c995",
          "message": "netstandard2.0: the packages install on .NET Framework, one source, no forks\n\nAll four libraries multi-target netstandard2.0;netstandard2.1. The rule\nthat made it clean: no #if anywhere — netstandard2.1-only constructs are\nremoved rather than branched on, so both targets compile one source and\ncannot drift.\n\nThe structural change is de-DIMing the incremental calculus. .NET\nFramework's runtime cannot dispatch a default interface member, so the\ndefinitional folds move from DIM bodies to the static Unrect.Core.Scans\n(Fold/FoldSize/FoldArea) and internal ColumnAccumulators.Fold, with every\nimplementation delegating in one line. The guarantee shifts and every doc\nthat claimed it says so honestly: \"eager and lazy cannot disagree by\nconstruction\" is now \"cannot disagree, pinned by the fold-identity suite\"\n— IncrementalStrategyTests asserts SelectRows == a hand-written fold for\nevery factory, and a new incremental strategy carries the obligation to\njoin that theory data. Corollary recorded in capability-seam-notes: the\nDIM \"safety net\" for evolving ISpace is withdrawn — a member added to a\npublished Core interface is now a breaking change with no escape hatch;\ntype-testing is the whole capability recipe.\n\nThe rest the compiler found: System.HashCode (Core hand-rolls its combine\nrather than take Microsoft.Bcl.HashCode — the bundling would have\nsuppressed the dependency from the nuspec and thrown FileNotFoundException\non Framework; both packages stay at zero new dependencies), the\nDictionary-from-pairs constructor, and HashSet<string>.TryGetValue (the\neager intern table is a Dictionary now; the comment that argued HashSet\nwas the honest structure records why it changed). The two polyfills are\nunconditional because neither netstandard has the types; a net5.0+ target\nis what would force an #if.\n\nTests multi-target net8.0;net48 on Windows only (OS-conditioned csproj,\nso Linux CI runs exactly what it ran — verified against all three\nworkflows). Two portable shims replace .NET-5+/6+ APIs the suite used:\nWithTimeout for Task.WaitAsync(TimeSpan), ByReference for\nReferenceEqualityComparer. The full suite has run green on the Framework\nCLR itself: 1,423 on net48 against the netstandard2.0 build, ExcelDataReader's\nown Framework assembly included — verified by the owner on Windows.\nBoth nupkgs verified to carry lib/netstandard2.0 and lib/netstandard2.1.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-05T17:36:21Z",
          "tree_id": "67b64bc3184652344901c8a243f8ce1256d858b9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16fef6aa0fb6978c79559b92910716e6ad44c995"
        },
        "date": 1788630324660,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 3040321,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 812320,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 3008,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 1344937,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 1408,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 588,
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
          "id": "e1d21c2d706b054d252bbea3a996b97f6f7bfc4b",
          "message": "Hygiene: .gitattributes and editorconfig trim — line endings become a decision\n\nThe repo had no .gitattributes, so line endings were whatever tool\ntouched a file last — four .linq files sat CRLF in the index beside an\nLF corpus, and the vocabulary respell nearly flipped three of them\nwholesale (caught in review; a 10-line diff had become 366). One rule\nnow: the index holds LF (* text=auto); .linq and .sln check out CRLF\nbecause their native editors are Windows-bound; spreadsheets and images\nare explicitly binary. This commit carries the one-time renormalization\nso the flip lives here, deliberately, and nowhere else.\n\n.editorconfig gains trailing-whitespace trimming, with markdown exempt\n(a trailing double-space is a hard line break).\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-06T00:49:13Z",
          "tree_id": "ce25b3593543c4145d69070ff009c9f67ff1aa09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/e1d21c2d706b054d252bbea3a996b97f6f7bfc4b"
        },
        "date": 1788712513948,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 3040321,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 812320,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 3008,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 1344937,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 1408,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 588,
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
          "id": "bf1fc2851544d9f87ffb3b76df4fa0b714780243",
          "message": "Merge experiment/typed-spaces: the projection model\n\nThe deepest cut since wave 2: what was called Shape is a projection; the\nspace and its subspaces are the shapes. Seven phases plus pre-merge\ncleanup (docs/design/projection-model-spec.md, judgment record in §10):\nthe modifier doubling killed (43 -> 6 irreducible), IShape -> IProjection\nwith namespace Unrect.Projections, the capability stack (IFormulaSpace,\nCapability<T>() transport, the slicing law, shared-formula\nreconstruction), OrBlank and the eachRow slot, the CaptionMap bind, the\nscoped entry Over<TSpace>() and MapWorkbook, phase-7 acceptance with the\nfourteen recorded refusals, and the three-sweep cleanup.\n\nSuite 1,709 green (net8.0 and net48); both library TFMs 0 warnings.\nBreaking: the rename, the Table family, the SpreadsheetSpace shell\nretirement, and the entries.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-07T23:11:42Z",
          "tree_id": "d091a9f7437fb1ec8d2b35119ab628b2a54047a3",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bf1fc2851544d9f87ffb3b76df4fa0b714780243"
        },
        "date": 1788823322490,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 3040321,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 812320,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 3008,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 1344936,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 1408,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 588,
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
          "id": "4a44b787ac6b8b6fa5c487623e316bfe0e364292",
          "message": "Presence semantics and three hygiene fixes: the zero disambiguated\n\nThe algebra's highest-priority open problem (v0.4 §7/§14.1): geometric\nzero carried four meanings. The internal Presence { Read, Empty,\nAbsorbed } now rides beside Consumed on the engine's results — stamped\nat tolerance boundaries (Absorbed), settled-at-zero extents (Empty, read\noff the SETTLED extent so evaluation order and doors agree by\nconstruction), joined through layouts (Read if any child Read, else\nEmpty), defaulting Read. The internal epsilon (NothingProjection) makes\nthe flow-unit law stateable: VerticalFlow is a monoid at L3 for\nsuccessful readings, boundaries pinned.\n\nThe law, earned the hard way: presence explains a stop; the extent\ndecides one. The first implementation let presence decide the repeat\nguard; the law-tests caught a real L1/L2 change on\nVerticalRepeat(item.Optional().Sized(...)) — an Absorbed boundary under\na declared area legitimately consumes it and always kept the run going.\nThe guard is byte-identical to before; presence powers only the new\nteaching Info when a run ends at an absorbed item (D2), and the caught\nshape is pinned as the compatibility-law section of PresenceLawTests.\n\nDesign record: docs/design/presence-and-unit-spec.md (DECIDED, with the\nD5 amendment trail).\n\nAlso, with pins: ChoiceProjection.Summarise renders nested aggregates\ndepth-indented with one location per line; MinOffset() is one canonical\ninstance (HasDeclaredOffset correct by construction); the content-\nmatching rule reduced to one implementation (CellMatching primitives,\nTableView's dictionary keyed on TextComparer; CaptionComparer's\ndeliberate divergence untouched). spike/Phase1Probe removed (a husk).\n\nSuite 1,820 -> 1,862 green; both library TFMs 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-09T15:34:18Z",
          "tree_id": "883452644caa55f06a0f9c2d530ee9c0cece2b92",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4a44b787ac6b8b6fa5c487623e316bfe0e364292"
        },
        "date": 1788969082139,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 3040321,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 812320,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 3008,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 1344936,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 1408,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 588,
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
          "id": "d06708df60e381758b11dde07836c674565409b6",
          "message": "Merge experiment/record-primitive: streaming composites over a bound, the band tiler, Table composed from primitives\n\nThe labels-as-context primitives (ColumnLabels, WithColumnLabels, Record),\nSkipToFirstNonBlankCell, and the uniform offset law; the engine streaming a\ndiscovered bound through a composite (TailSpace, a bound-aware Exceeds, lazy\nflow and repeat slices); VerticalBands/HorizontalBands, the tiler — a\nrepeating fixed-dimension space, distinct from the pattern repeat;\nTable(headerRows, eachRow) composed over the tiler under a UnitProjection,\nwith AsScaffolding and a mark-driven path fold giving it the leaf's own\ndiagnostics; ColumnLabels self-contained, its header parse the one home in\nLabelMap.FromHeader; and the repeat returned to a pure walk, onBlank living\non the tiler alone.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-14T17:43:35Z",
          "tree_id": "1c80a0bed7aeb311bb6e1bdb0933ac5a05562434",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/d06708df60e381758b11dde07836c674565409b6"
        },
        "date": 1789408534696,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 3280353,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 916352,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 3408,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 1696929,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 1536,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 631,
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
          "id": "33f1afa3496afb1fabd2e0242197d1c5d9889568",
          "message": "Merge experiment/point-and-line: the Point substrate — one vocabulary over any ISpace, planes and points as the locators, the kinds in Unrect.Spreadsheets\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-16T04:05:17Z",
          "tree_id": "09d4c757d0f1502df340fb2a2481c31f78e8d8b2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/33f1afa3496afb1fabd2e0242197d1c5d9889568"
        },
        "date": 1789531989998,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 2160337,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 720337,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 3296,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 1248889,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 1152,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 652,
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
          "id": "0f34f159151554afcc0555f7afc83fdd486b8c06",
          "message": "Merge experiment/point-follow-ups: Extents into Plane, Unrect.Interactive, and the typed-predicate lift\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-17T02:37:19Z",
          "tree_id": "cfa1d003f75555e9d6f330d316cdd949a1da2b25",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/0f34f159151554afcc0555f7afc83fdd486b8c06"
        },
        "date": 1789613138262,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 2160337,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 720336,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 3296,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 1248888,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 1152,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 646,
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
          "id": "717d1ea3b61c226c608194aae5ff909fd4603455",
          "message": "Packaging: Unrect.Interactive ships as its own package\n\nThe project was already packable — id, description, tags, and a\ndependency on exactly Unrect and Unrect.Spreadsheets at the same MinVer\nversion — but the release workflow packed and asserted two packages, and\nthe README and build props named two. Now three.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T01:12:25Z",
          "tree_id": "f01ec5a705245aededd07d5455e7b03ad4f8880d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/717d1ea3b61c226c608194aae5ff909fd4603455"
        },
        "date": 1789796483212,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 7594774,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 3155320152,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 1583104,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 4500750,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 398053,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 6293287,
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
          "id": "4818c36f9e9b0c2b75a4093dbbce6981329fd14b",
          "message": "Merge feature/scaffold-shapes: ScaffoldRecord and ScaffoldClass, on a sheet and as leaves\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T15:35:04Z",
          "tree_id": "d48076b68bb0a93dc6ab6797b10f0ead882156a7",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4818c36f9e9b0c2b75a4093dbbce6981329fd14b"
        },
        "date": 1789847938822,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 7594774,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 3155312416,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 1583104,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 4500750,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 398176,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 6293287,
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
          "id": "bd027f2136cee944da88c1eae17a06b14a05579d",
          "message": "Merge feature/bind-from-row: Table<T> fills a member from the row - Column(member, row => ...)\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T19:49:16Z",
          "tree_id": "88c877501aeddcefb436f136842b9904cda55a09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bd027f2136cee944da88c1eae17a06b14a05579d"
        },
        "date": 1789863953671,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 7954846,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 3155560464,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 1583400,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 4644822,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 398320,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 6293359,
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
          "id": "96b946d5920955eb81be3eab46697a3e4639e2e4",
          "message": "Merge feature/header-bands: headers of several rows, columns addressed by path, flat binding over bands\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T03:17:51Z",
          "tree_id": "33ccb96a2f1d1cb7795dde522c6c586ee8ce479a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/96b946d5920955eb81be3eab46697a3e4639e2e4"
        },
        "date": 1789888952322,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 7954846,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 3155550968,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 1583400,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 4644822,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 398197,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 6293359,
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
          "id": "191ad7e666be18cd9916eb0132108e83e54bd6db",
          "message": "Open questions: IsText is the wrong shape - what it is for, the better shape, and the larger question of facets, recorded and not implemented\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T04:23:54Z",
          "tree_id": "5e6c0e37d52a9f501293d06ad42b1a125ed81250",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/191ad7e666be18cd9916eb0132108e83e54bd6db"
        },
        "date": 1789931369114,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Engine.VerticalFlow_ManyChildren",
            "value": 7954846,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Flow_Nested",
            "value": 3155495048,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Overlay_AnchoredChildren",
            "value": 1583400,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Repeat_SeparatedBlocks",
            "value": 4644819,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Under_CaptionedSection",
            "value": 398136,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Engine.Range_ReadAllCells",
            "value": 6293359,
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
        "date": 1788472137654,
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
          "id": "f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb",
          "message": "Fix the 2-core CI flake: a blocked-borrower proof needs a started borrower\n\nAReachWaitsForAWarmerRatherThanStartingASecondOpenOfTheSameFile failed on\nthe GitHub runner (ef348dd) on \"the wait is counted\": WarmWaitMilliseconds\nwas 0, and 0 was the honest count. The pool's warmers ride Task.Run and\nthe gated arrangement BLOCKS them inside their opens, one pool thread\neach — on a two-core runner that is the entire starting thread pool, so\nthe test's own Task.Run borrower never started until thread injection got\naround to it. Both blocked-ness assertions passed vacuously (not finished\nbecause not scheduled), and by the time the reach ran, the warm reader was\nparked and there was nothing left to wait for.\n\nReproduced under taskset -c 0,1: three failures in four runs before the\nfix, none in six Debug runs plus a Release run after. The fix is\nOnItsOwnThread (TaskCreationOptions.LongRunning) at the four sites that\nassert a borrower is blocked — a dedicated thread starts unconditionally,\nso \"started, and still not finished\" really does mean \"parked inside\nBorrow\". The three sibling sites could only pass vacuously, never fail,\nbut their proofs were the same lie under starvation. The burst tests\nalready stood on structural evidence (SpinUntil on OpensStarted) and are\nuntouched.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T22:37:18Z",
          "tree_id": "c37ffff8e7e618f8d8cdb3778c429c1bd5259fc9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb"
        },
        "date": 1788475703772,
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
          "id": "10027e9f1d263aac70041f0f7166b186324129e8",
          "message": "Both doors measure a sheet that will not say how big it is\n\nSpreadsheetSpace.Create sized its grid from reader.RowCount/FieldCount and\nsilently yielded an empty space when the reader would not give them — the\none outcome an adapter must not have, and a divergence from the streaming\ndoor, which has measured such sheets since Part 2 step 7. The fill is now\ntwo named siblings behind one dichotomy: ReadDeclared (the original loop,\nunchanged) and ReadMeasured (rows collected at their own width, the widest\nrow wins, absent trailing cells Blank — the same answer Workbook.Measure\ngives). The guard is rowCount <= 0 alone, deliberately mirroring the\nstreaming door so the two can never disagree about the same file.\n\nThe recorded cause was wrong, and is corrected everywhere it appeared: a\nmissing dimension element does not trigger this — ExcelDataReader derives\nboth counts from a pre-scan of the cells on every format it handles. The\nreachable trigger is a sheet with NO valued cell (rows of formatted-but-\nvalueless cells, a pre-formatted export region). Pinned by the committed\nTestData/no-extent.xlsx (dimensionless AND valueless, with the survey's\nRowsMeasured == 4 doubling as the fixture's own guard against a\nregeneration that quietly stops reaching the path) and a both-doors\nidentity test.\n\nRides along, both owner decisions from this session's discussion:\n- MaxReaders: spec §14 Q2 DECIDED — 3 stays and stops being provisional,\n  because no number is right: reader demand is the declaration's monotone-\n  cursor count, unbounded in principle, data-independent in practice, and\n  the ceiling fails gently (Reopens is the counted, named signal to raise\n  it). Sizing guidance added to docs/streaming.md; per-reader economics\n  (~5s CPU per open, position must be walked, reader-per-row is O(n^2))\n  recorded in the spec.\n- Table's header-derived width: spec §14 Q1 DEFERRED, superseding the\n  2026-09-03 yes — the step-8 interleave delivered the lazy win with\n  today's denotation intact, so the K-1 campaign votes before the\n  denotation change is paid for.\n\nSuite 1,382 -> 1,387; gates green in Debug and 2-core Release.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T14:29:21Z",
          "tree_id": "fc431b0954d2e3a5115a177bd1a21d63c169ffae",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/10027e9f1d263aac70041f0f7166b186324129e8"
        },
        "date": 1788533065165,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 489,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 491,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 496,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 499,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 2715,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 670,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 497,
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
          "id": "c01531cec6968e544acc578291244292172a00a5",
          "message": "Docs: Part 3 deferred on principle, and .Sized's composite role stated honestly\n\nSpec §13 gains the Part 3 row (bound-aware composite placement): the\nengine's remaining greed sorted into one necessary force (Repeat items —\nthe item's existence is the question), one free force (post-Project\nconsumption, amortised by the root's accounting), and one debt (composite\nchild placement, whose questions have lazy answers nobody asks for).\nDeferred until the first tall sized composite pays the debt — sized\ncomposites in the corpus are short header bands, where settling eagerly\ncosts nothing. The K-1 campaign is the likely judge; the census pin is the\ntripwire.\n\ndocs/streaming.md stops saying \"put the .Sized on the leaf\" as if it were\na law: a sized composite is a legitimate spelling with no leaf equivalent\n— a composite has no intrinsic extent, and the declared band is what\nscopes its internal seeks and settles its consumption.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T15:37:53Z",
          "tree_id": "6188ce68af3130bfba604f38845b0c515958cb34",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/c01531cec6968e544acc578291244292172a00a5"
        },
        "date": 1788537630718,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 489,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 489,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 496,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 499,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 2715,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 670,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 497,
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
          "id": "2d73985e95c70f51a2b26d7dc98c3936f1f52d5d",
          "message": "Retention: the live-set floor for the interning change, with the target on the chart\n\nAn eighth CI leg that is not a BenchmarkDotNet family: interning reduces\nRETAINED bytes, not allocations (a duplicate string is allocated by the\nreader before the adapter sees it and dies young after dedup), so the\nAllocated column cannot see it — and retention is deterministic, so it\nneeds no statistical engine. A one-shot job measures live bytes with the\nresult held, emits the same JSON document the rig already stores, and\nrides the same workflow and dashboard as everything else.\n\nBuilding it surfaced two facts worth more than the plumbing:\n\n- The eager door's duplication depends on how the file spells its text.\n  Shared-string cells come back already deduped (the reader returns its\n  table's own instance); inline strings and formula-result cells\n  materialise fresh per cell. A real Excel export is both (the local K-1:\n  9,049 text cells, 2,876 values, 4,016 instances — the formula results\n  are the duplicated half). The family brackets it, and the shared-string\n  row is the priced TARGET: the same cells read 112.0 MB duplicated vs\n  58.2 MB deduped, so ~48% is what a complete eager interner is worth on\n  this shape — short of that is unfinished, not failed.\n- The first fixture boxed decimals a real read never produces (16 MB of\n  boxes in a retained-bytes measurement); the retention fixtures now\n  yield doubles like a reader does. StreamingSpaces is deliberately\n  untouched — changing it would re-baseline that family's history.\n\nScenarios exercise the real seams the interning change will live in: the\neager rows go through SpreadsheetSpace.Create over generated workbooks\n(RetentionWorkbooks: a minimal hand-rolled OOXML writer, no new package;\nthe one deliberate exception to the no-workbooks rule, recorded in\ndocs/benchmarking.md), the streaming rows through the store's chunk fill.\nFloor: eager space held 106.8 MB, results held 82.1 MB both doors\n(byte-identical — streaming's promise stated in the metric), controls\nbyte-identical to their duplicated twins by fixed-width padding. Leg\nruns ~65s, the shortest in the matrix.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T16:47:38Z",
          "tree_id": "0c756ae6dd2d4f17cd84e585c99d7d3ae08fd409",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/2d73985e95c70f51a2b26d7dc98c3936f1f52d5d"
        },
        "date": 1788542162114,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 489,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 489,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 496,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 499,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 2715,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 667,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 497,
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
          "id": "eddc5d17c38c715f41cd95d041452deb66f8354c",
          "message": "Interning: equal text shares one instance through both doors\n\nAdapter-level string interning — a find-my-twin table at each door's\nadapt seam. The eager door threads a per-Create-call HashSet through both\nfill paths (one instance per distinct value across every sheet of the\ncall); the streaming door hangs one capped ConcurrentDictionary on the\nWorkbook, plumbed into every store's chunk fill, so a chase reader's\nre-parse dedupes against the first parse. Strings only: every other kind\nis inline in the 24-byte struct or unreachable from the spreadsheet door.\n\nThe win is retention, not allocation — the duplicate is allocated by the\nreader before the adapter sees it and dies in gen0 after dedup, which is\nwhy the Retention leg is the judge and MemoryDiagnoser is blind to it.\nMeasured against the committed floor: eager space held 106.8 -> 55.5 MB,\nlanding on the priced shared-string target to the byte; held results\n82.1 -> 30.8 MB, byte-identical across doors; all three unique controls\nflat to the byte; wall time noise on the 1M-row parse.\n\nThe cap (WorkbookOptions.MaxInternedStrings, default 65,536; 0 = off)\nand the 256-char length guard bound what the book-lifetime table can\npin. Documented as a two-way knob: a full table costs its entries for\nthe book's life — some 40 MB at the default cap — so it turns DOWN, to\n0, for known-unique text, and the docs say so at every site (the rig is\nstructurally blind to the table's own live set: readings are taken with\nthe book closed). Workbook.InterningStatistics reports hits, distinct,\nand estimated bytes (64-bit layout, exact for it; Hits counts fills, so\na reloaded chunk counts again — read against ChunkReloads).\n\nExcelDataReader fact on the record: shared-string cells arrive\npre-deduped from the reader's own SST; the duplication this kills comes\nfrom inline-string cells, formula-result cells, and .xls. Pinned by 36\ntests including a cross-door sharing differential, mutation-checked both\ndirections, and a WeakReference proof that Dispose releases the strings.\nSuite 1,423.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T19:19:56Z",
          "tree_id": "fea5150f427d5b65cf652662879afb3b470547f2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/eddc5d17c38c715f41cd95d041452deb66f8354c"
        },
        "date": 1788556080273,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 489,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 489,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 496,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 499,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 2715,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 667,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 497,
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
          "id": "16fef6aa0fb6978c79559b92910716e6ad44c995",
          "message": "netstandard2.0: the packages install on .NET Framework, one source, no forks\n\nAll four libraries multi-target netstandard2.0;netstandard2.1. The rule\nthat made it clean: no #if anywhere — netstandard2.1-only constructs are\nremoved rather than branched on, so both targets compile one source and\ncannot drift.\n\nThe structural change is de-DIMing the incremental calculus. .NET\nFramework's runtime cannot dispatch a default interface member, so the\ndefinitional folds move from DIM bodies to the static Unrect.Core.Scans\n(Fold/FoldSize/FoldArea) and internal ColumnAccumulators.Fold, with every\nimplementation delegating in one line. The guarantee shifts and every doc\nthat claimed it says so honestly: \"eager and lazy cannot disagree by\nconstruction\" is now \"cannot disagree, pinned by the fold-identity suite\"\n— IncrementalStrategyTests asserts SelectRows == a hand-written fold for\nevery factory, and a new incremental strategy carries the obligation to\njoin that theory data. Corollary recorded in capability-seam-notes: the\nDIM \"safety net\" for evolving ISpace is withdrawn — a member added to a\npublished Core interface is now a breaking change with no escape hatch;\ntype-testing is the whole capability recipe.\n\nThe rest the compiler found: System.HashCode (Core hand-rolls its combine\nrather than take Microsoft.Bcl.HashCode — the bundling would have\nsuppressed the dependency from the nuspec and thrown FileNotFoundException\non Framework; both packages stay at zero new dependencies), the\nDictionary-from-pairs constructor, and HashSet<string>.TryGetValue (the\neager intern table is a Dictionary now; the comment that argued HashSet\nwas the honest structure records why it changed). The two polyfills are\nunconditional because neither netstandard has the types; a net5.0+ target\nis what would force an #if.\n\nTests multi-target net8.0;net48 on Windows only (OS-conditioned csproj,\nso Linux CI runs exactly what it ran — verified against all three\nworkflows). Two portable shims replace .NET-5+/6+ APIs the suite used:\nWithTimeout for Task.WaitAsync(TimeSpan), ByReference for\nReferenceEqualityComparer. The full suite has run green on the Framework\nCLR itself: 1,423 on net48 against the netstandard2.0 build, ExcelDataReader's\nown Framework assembly included — verified by the owner on Windows.\nBoth nupkgs verified to carry lib/netstandard2.0 and lib/netstandard2.1.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-05T17:36:21Z",
          "tree_id": "67b64bc3184652344901c8a243f8ce1256d858b9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16fef6aa0fb6978c79559b92910716e6ad44c995"
        },
        "date": 1788630324834,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 489,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 489,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 496,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 499,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 2715,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 667,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 497,
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
          "id": "e1d21c2d706b054d252bbea3a996b97f6f7bfc4b",
          "message": "Hygiene: .gitattributes and editorconfig trim — line endings become a decision\n\nThe repo had no .gitattributes, so line endings were whatever tool\ntouched a file last — four .linq files sat CRLF in the index beside an\nLF corpus, and the vocabulary respell nearly flipped three of them\nwholesale (caught in review; a 10-line diff had become 366). One rule\nnow: the index holds LF (* text=auto); .linq and .sln check out CRLF\nbecause their native editors are Windows-bound; spreadsheets and images\nare explicitly binary. This commit carries the one-time renormalization\nso the flip lives here, deliberately, and nowhere else.\n\n.editorconfig gains trailing-whitespace trimming, with markdown exempt\n(a trailing double-space is a hard line break).\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-06T00:49:13Z",
          "tree_id": "ce25b3593543c4145d69070ff009c9f67ff1aa09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/e1d21c2d706b054d252bbea3a996b97f6f7bfc4b"
        },
        "date": 1788712514136,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 489,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 489,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 496,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 499,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 2715,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 667,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 497,
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
          "id": "bf1fc2851544d9f87ffb3b76df4fa0b714780243",
          "message": "Merge experiment/typed-spaces: the projection model\n\nThe deepest cut since wave 2: what was called Shape is a projection; the\nspace and its subspaces are the shapes. Seven phases plus pre-merge\ncleanup (docs/design/projection-model-spec.md, judgment record in §10):\nthe modifier doubling killed (43 -> 6 irreducible), IShape -> IProjection\nwith namespace Unrect.Projections, the capability stack (IFormulaSpace,\nCapability<T>() transport, the slicing law, shared-formula\nreconstruction), OrBlank and the eachRow slot, the CaptionMap bind, the\nscoped entry Over<TSpace>() and MapWorkbook, phase-7 acceptance with the\nfourteen recorded refusals, and the three-sweep cleanup.\n\nSuite 1,709 green (net8.0 and net48); both library TFMs 0 warnings.\nBreaking: the rename, the Table family, the SpreadsheetSpace shell\nretirement, and the entries.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-07T23:11:42Z",
          "tree_id": "d091a9f7437fb1ec8d2b35119ab628b2a54047a3",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bf1fc2851544d9f87ffb3b76df4fa0b714780243"
        },
        "date": 1788823322708,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 489,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 489,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 496,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 499,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 2715,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 667,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 497,
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
          "id": "4a44b787ac6b8b6fa5c487623e316bfe0e364292",
          "message": "Presence semantics and three hygiene fixes: the zero disambiguated\n\nThe algebra's highest-priority open problem (v0.4 §7/§14.1): geometric\nzero carried four meanings. The internal Presence { Read, Empty,\nAbsorbed } now rides beside Consumed on the engine's results — stamped\nat tolerance boundaries (Absorbed), settled-at-zero extents (Empty, read\noff the SETTLED extent so evaluation order and doors agree by\nconstruction), joined through layouts (Read if any child Read, else\nEmpty), defaulting Read. The internal epsilon (NothingProjection) makes\nthe flow-unit law stateable: VerticalFlow is a monoid at L3 for\nsuccessful readings, boundaries pinned.\n\nThe law, earned the hard way: presence explains a stop; the extent\ndecides one. The first implementation let presence decide the repeat\nguard; the law-tests caught a real L1/L2 change on\nVerticalRepeat(item.Optional().Sized(...)) — an Absorbed boundary under\na declared area legitimately consumes it and always kept the run going.\nThe guard is byte-identical to before; presence powers only the new\nteaching Info when a run ends at an absorbed item (D2), and the caught\nshape is pinned as the compatibility-law section of PresenceLawTests.\n\nDesign record: docs/design/presence-and-unit-spec.md (DECIDED, with the\nD5 amendment trail).\n\nAlso, with pins: ChoiceProjection.Summarise renders nested aggregates\ndepth-indented with one location per line; MinOffset() is one canonical\ninstance (HasDeclaredOffset correct by construction); the content-\nmatching rule reduced to one implementation (CellMatching primitives,\nTableView's dictionary keyed on TextComparer; CaptionComparer's\ndeliberate divergence untouched). spike/Phase1Probe removed (a husk).\n\nSuite 1,820 -> 1,862 green; both library TFMs 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-09T15:34:18Z",
          "tree_id": "883452644caa55f06a0f9c2d530ee9c0cece2b92",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4a44b787ac6b8b6fa5c487623e316bfe0e364292"
        },
        "date": 1788969082406,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 489,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 489,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 496,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 499,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 2715,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 667,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 497,
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
          "id": "d06708df60e381758b11dde07836c674565409b6",
          "message": "Merge experiment/record-primitive: streaming composites over a bound, the band tiler, Table composed from primitives\n\nThe labels-as-context primitives (ColumnLabels, WithColumnLabels, Record),\nSkipToFirstNonBlankCell, and the uniform offset law; the engine streaming a\ndiscovered bound through a composite (TailSpace, a bound-aware Exceeds, lazy\nflow and repeat slices); VerticalBands/HorizontalBands, the tiler — a\nrepeating fixed-dimension space, distinct from the pattern repeat;\nTable(headerRows, eachRow) composed over the tiler under a UnitProjection,\nwith AsScaffolding and a mark-driven path fold giving it the leaf's own\ndiagnostics; ColumnLabels self-contained, its header parse the one home in\nLabelMap.FromHeader; and the repeat returned to a pure walk, onBlank living\non the tiler alone.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-14T17:43:35Z",
          "tree_id": "1c80a0bed7aeb311bb6e1bdb0933ac5a05562434",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/d06708df60e381758b11dde07836c674565409b6"
        },
        "date": 1789408534966,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 521,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 521,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 528,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 531,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 3027,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 715,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 529,
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
          "id": "33f1afa3496afb1fabd2e0242197d1c5d9889568",
          "message": "Merge experiment/point-and-line: the Point substrate — one vocabulary over any ISpace, planes and points as the locators, the kinds in Unrect.Spreadsheets\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-16T04:05:17Z",
          "tree_id": "09d4c757d0f1502df340fb2a2481c31f78e8d8b2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/33f1afa3496afb1fabd2e0242197d1c5d9889568"
        },
        "date": 1789531990272,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 553,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 553,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 457,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 462,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 3358,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 670,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 457,
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
          "id": "0f34f159151554afcc0555f7afc83fdd486b8c06",
          "message": "Merge experiment/point-follow-ups: Extents into Plane, Unrect.Interactive, and the typed-predicate lift\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-17T02:37:19Z",
          "tree_id": "cfa1d003f75555e9d6f330d316cdd949a1da2b25",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/0f34f159151554afcc0555f7afc83fdd486b8c06"
        },
        "date": 1789613138523,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 553,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 553,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 456,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 462,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 3358,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 670,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 457,
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
          "id": "717d1ea3b61c226c608194aae5ff909fd4603455",
          "message": "Packaging: Unrect.Interactive ships as its own package\n\nThe project was already packable — id, description, tags, and a\ndependency on exactly Unrect and Unrect.Spreadsheets at the same MinVer\nversion — but the release workflow packed and asserted two packages, and\nthe README and build props named two. Now three.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T01:12:25Z",
          "tree_id": "f01ec5a705245aededd07d5455e7b03ad4f8880d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/717d1ea3b61c226c608194aae5ff909fd4603455"
        },
        "date": 1789796483424,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 6293191,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 6293191,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 788880,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 6293976,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 25173496,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 12586440,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 3147534,
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
          "id": "4818c36f9e9b0c2b75a4093dbbce6981329fd14b",
          "message": "Merge feature/scaffold-shapes: ScaffoldRecord and ScaffoldClass, on a sheet and as leaves\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T15:35:04Z",
          "tree_id": "d48076b68bb0a93dc6ab6797b10f0ead882156a7",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4818c36f9e9b0c2b75a4093dbbce6981329fd14b"
        },
        "date": 1789847939070,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 6293191,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 6293191,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 788880,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 6293976,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 25173496,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 12586440,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 3147534,
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
          "id": "bd027f2136cee944da88c1eae17a06b14a05579d",
          "message": "Merge feature/bind-from-row: Table<T> fills a member from the row - Column(member, row => ...)\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T19:49:16Z",
          "tree_id": "88c877501aeddcefb436f136842b9904cda55a09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bd027f2136cee944da88c1eae17a06b14a05579d"
        },
        "date": 1789863953960,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 6293263,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 6293263,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 788880,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 6293976,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 25173496,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 12586512,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 3147534,
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
          "id": "96b946d5920955eb81be3eab46697a3e4639e2e4",
          "message": "Merge feature/header-bands: headers of several rows, columns addressed by path, flat binding over bands\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T03:17:51Z",
          "tree_id": "33ccb96a2f1d1cb7795dde522c6c586ee8ce479a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/96b946d5920955eb81be3eab46697a3e4639e2e4"
        },
        "date": 1789888952605,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_FullHeight",
            "value": 6293263,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.RowsWhileAnyValue_Sparse",
            "value": 6293263,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt10Percent",
            "value": 788880,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_HitAt90Percent",
            "value": 6293976,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Seek_MissWholeGrid",
            "value": 25173496,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.Until_BoundResolution",
            "value": 12586512,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Strategies.BlankRows_Skip",
            "value": 3147534,
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
        "date": 1788472137825,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2481820,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 24802350,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 10641414,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 106402270,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 6800902,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 68002054,
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
          "id": "f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb",
          "message": "Fix the 2-core CI flake: a blocked-borrower proof needs a started borrower\n\nAReachWaitsForAWarmerRatherThanStartingASecondOpenOfTheSameFile failed on\nthe GitHub runner (ef348dd) on \"the wait is counted\": WarmWaitMilliseconds\nwas 0, and 0 was the honest count. The pool's warmers ride Task.Run and\nthe gated arrangement BLOCKS them inside their opens, one pool thread\neach — on a two-core runner that is the entire starting thread pool, so\nthe test's own Task.Run borrower never started until thread injection got\naround to it. Both blocked-ness assertions passed vacuously (not finished\nbecause not scheduled), and by the time the reach ran, the warm reader was\nparked and there was nothing left to wait for.\n\nReproduced under taskset -c 0,1: three failures in four runs before the\nfix, none in six Debug runs plus a Release run after. The fix is\nOnItsOwnThread (TaskCreationOptions.LongRunning) at the four sites that\nassert a borrower is blocked — a dedicated thread starts unconditionally,\nso \"started, and still not finished\" really does mean \"parked inside\nBorrow\". The three sibling sites could only pass vacuously, never fail,\nbut their proofs were the same lie under starvation. The burst tests\nalready stood on structural evidence (SpinUntil on OpensStarted) and are\nuntouched.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T22:37:18Z",
          "tree_id": "c37ffff8e7e618f8d8cdb3778c429c1bd5259fc9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb"
        },
        "date": 1788475703936,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2481812,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 24802506,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 10641414,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 106401962,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 6800902,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 68003019,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 13107,
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
          "id": "10027e9f1d263aac70041f0f7166b186324129e8",
          "message": "Both doors measure a sheet that will not say how big it is\n\nSpreadsheetSpace.Create sized its grid from reader.RowCount/FieldCount and\nsilently yielded an empty space when the reader would not give them — the\none outcome an adapter must not have, and a divergence from the streaming\ndoor, which has measured such sheets since Part 2 step 7. The fill is now\ntwo named siblings behind one dichotomy: ReadDeclared (the original loop,\nunchanged) and ReadMeasured (rows collected at their own width, the widest\nrow wins, absent trailing cells Blank — the same answer Workbook.Measure\ngives). The guard is rowCount <= 0 alone, deliberately mirroring the\nstreaming door so the two can never disagree about the same file.\n\nThe recorded cause was wrong, and is corrected everywhere it appeared: a\nmissing dimension element does not trigger this — ExcelDataReader derives\nboth counts from a pre-scan of the cells on every format it handles. The\nreachable trigger is a sheet with NO valued cell (rows of formatted-but-\nvalueless cells, a pre-formatted export region). Pinned by the committed\nTestData/no-extent.xlsx (dimensionless AND valueless, with the survey's\nRowsMeasured == 4 doubling as the fixture's own guard against a\nregeneration that quietly stops reaching the path) and a both-doors\nidentity test.\n\nRides along, both owner decisions from this session's discussion:\n- MaxReaders: spec §14 Q2 DECIDED — 3 stays and stops being provisional,\n  because no number is right: reader demand is the declaration's monotone-\n  cursor count, unbounded in principle, data-independent in practice, and\n  the ceiling fails gently (Reopens is the counted, named signal to raise\n  it). Sizing guidance added to docs/streaming.md; per-reader economics\n  (~5s CPU per open, position must be walked, reader-per-row is O(n^2))\n  recorded in the spec.\n- Table's header-derived width: spec §14 Q1 DEFERRED, superseding the\n  2026-09-03 yes — the step-8 interleave delivered the lazy win with\n  today's denotation intact, so the K-1 campaign votes before the\n  denotation change is paid for.\n\nSuite 1,382 -> 1,387; gates green in Debug and 2-core Release.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T14:29:21Z",
          "tree_id": "fc431b0954d2e3a5115a177bd1a21d63c169ffae",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/10027e9f1d263aac70041f0f7166b186324129e8"
        },
        "date": 1788533065370,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2766565,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 26596772,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 10824144,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 107699583,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 6983664,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 69299758,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 13091,
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
          "id": "c01531cec6968e544acc578291244292172a00a5",
          "message": "Docs: Part 3 deferred on principle, and .Sized's composite role stated honestly\n\nSpec §13 gains the Part 3 row (bound-aware composite placement): the\nengine's remaining greed sorted into one necessary force (Repeat items —\nthe item's existence is the question), one free force (post-Project\nconsumption, amortised by the root's accounting), and one debt (composite\nchild placement, whose questions have lazy answers nobody asks for).\nDeferred until the first tall sized composite pays the debt — sized\ncomposites in the corpus are short header bands, where settling eagerly\ncosts nothing. The K-1 campaign is the likely judge; the census pin is the\ntripwire.\n\ndocs/streaming.md stops saying \"put the .Sized on the leaf\" as if it were\na law: a sized composite is a legitimate spelling with no leaf equivalent\n— a composite has no intrinsic extent, and the declared band is what\nscopes its internal seeks and settles its consumption.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T15:37:53Z",
          "tree_id": "6188ce68af3130bfba604f38845b0c515958cb34",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/c01531cec6968e544acc578291244292172a00a5"
        },
        "date": 1788537630955,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2766551,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 26596772,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 10824105,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 107699504,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 6983642,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 69299360,
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
          "id": "2d73985e95c70f51a2b26d7dc98c3936f1f52d5d",
          "message": "Retention: the live-set floor for the interning change, with the target on the chart\n\nAn eighth CI leg that is not a BenchmarkDotNet family: interning reduces\nRETAINED bytes, not allocations (a duplicate string is allocated by the\nreader before the adapter sees it and dies young after dedup), so the\nAllocated column cannot see it — and retention is deterministic, so it\nneeds no statistical engine. A one-shot job measures live bytes with the\nresult held, emits the same JSON document the rig already stores, and\nrides the same workflow and dashboard as everything else.\n\nBuilding it surfaced two facts worth more than the plumbing:\n\n- The eager door's duplication depends on how the file spells its text.\n  Shared-string cells come back already deduped (the reader returns its\n  table's own instance); inline strings and formula-result cells\n  materialise fresh per cell. A real Excel export is both (the local K-1:\n  9,049 text cells, 2,876 values, 4,016 instances — the formula results\n  are the duplicated half). The family brackets it, and the shared-string\n  row is the priced TARGET: the same cells read 112.0 MB duplicated vs\n  58.2 MB deduped, so ~48% is what a complete eager interner is worth on\n  this shape — short of that is unfinished, not failed.\n- The first fixture boxed decimals a real read never produces (16 MB of\n  boxes in a retained-bytes measurement); the retention fixtures now\n  yield doubles like a reader does. StreamingSpaces is deliberately\n  untouched — changing it would re-baseline that family's history.\n\nScenarios exercise the real seams the interning change will live in: the\neager rows go through SpreadsheetSpace.Create over generated workbooks\n(RetentionWorkbooks: a minimal hand-rolled OOXML writer, no new package;\nthe one deliberate exception to the no-workbooks rule, recorded in\ndocs/benchmarking.md), the streaming rows through the store's chunk fill.\nFloor: eager space held 106.8 MB, results held 82.1 MB both doors\n(byte-identical — streaming's promise stated in the metric), controls\nbyte-identical to their duplicated twins by fixed-width padding. Leg\nruns ~65s, the shortest in the matrix.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T16:47:38Z",
          "tree_id": "0c756ae6dd2d4f17cd84e585c99d7d3ae08fd409",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/2d73985e95c70f51a2b26d7dc98c3936f1f52d5d"
        },
        "date": 1788542162352,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2766569,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 26596776,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 10824099,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 107699502,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 6983584,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 69298967,
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
          "id": "eddc5d17c38c715f41cd95d041452deb66f8354c",
          "message": "Interning: equal text shares one instance through both doors\n\nAdapter-level string interning — a find-my-twin table at each door's\nadapt seam. The eager door threads a per-Create-call HashSet through both\nfill paths (one instance per distinct value across every sheet of the\ncall); the streaming door hangs one capped ConcurrentDictionary on the\nWorkbook, plumbed into every store's chunk fill, so a chase reader's\nre-parse dedupes against the first parse. Strings only: every other kind\nis inline in the 24-byte struct or unreachable from the spreadsheet door.\n\nThe win is retention, not allocation — the duplicate is allocated by the\nreader before the adapter sees it and dies in gen0 after dedup, which is\nwhy the Retention leg is the judge and MemoryDiagnoser is blind to it.\nMeasured against the committed floor: eager space held 106.8 -> 55.5 MB,\nlanding on the priced shared-string target to the byte; held results\n82.1 -> 30.8 MB, byte-identical across doors; all three unique controls\nflat to the byte; wall time noise on the 1M-row parse.\n\nThe cap (WorkbookOptions.MaxInternedStrings, default 65,536; 0 = off)\nand the 256-char length guard bound what the book-lifetime table can\npin. Documented as a two-way knob: a full table costs its entries for\nthe book's life — some 40 MB at the default cap — so it turns DOWN, to\n0, for known-unique text, and the docs say so at every site (the rig is\nstructurally blind to the table's own live set: readings are taken with\nthe book closed). Workbook.InterningStatistics reports hits, distinct,\nand estimated bytes (64-bit layout, exact for it; Hits counts fills, so\na reloaded chunk counts again — read against ChunkReloads).\n\nExcelDataReader fact on the record: shared-string cells arrive\npre-deduped from the reader's own SST; the duplication this kills comes\nfrom inline-string cells, formula-result cells, and .xls. Pinned by 36\ntests including a cross-door sharing differential, mutation-checked both\ndirections, and a WeakReference proof that Dispose releases the strings.\nSuite 1,423.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T19:19:56Z",
          "tree_id": "fea5150f427d5b65cf652662879afb3b470547f2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/eddc5d17c38c715f41cd95d041452deb66f8354c"
        },
        "date": 1788556080452,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2766567,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 26596773,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 10824144,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 107699583,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 6983664,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 69299736,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 13091,
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
          "id": "16fef6aa0fb6978c79559b92910716e6ad44c995",
          "message": "netstandard2.0: the packages install on .NET Framework, one source, no forks\n\nAll four libraries multi-target netstandard2.0;netstandard2.1. The rule\nthat made it clean: no #if anywhere — netstandard2.1-only constructs are\nremoved rather than branched on, so both targets compile one source and\ncannot drift.\n\nThe structural change is de-DIMing the incremental calculus. .NET\nFramework's runtime cannot dispatch a default interface member, so the\ndefinitional folds move from DIM bodies to the static Unrect.Core.Scans\n(Fold/FoldSize/FoldArea) and internal ColumnAccumulators.Fold, with every\nimplementation delegating in one line. The guarantee shifts and every doc\nthat claimed it says so honestly: \"eager and lazy cannot disagree by\nconstruction\" is now \"cannot disagree, pinned by the fold-identity suite\"\n— IncrementalStrategyTests asserts SelectRows == a hand-written fold for\nevery factory, and a new incremental strategy carries the obligation to\njoin that theory data. Corollary recorded in capability-seam-notes: the\nDIM \"safety net\" for evolving ISpace is withdrawn — a member added to a\npublished Core interface is now a breaking change with no escape hatch;\ntype-testing is the whole capability recipe.\n\nThe rest the compiler found: System.HashCode (Core hand-rolls its combine\nrather than take Microsoft.Bcl.HashCode — the bundling would have\nsuppressed the dependency from the nuspec and thrown FileNotFoundException\non Framework; both packages stay at zero new dependencies), the\nDictionary-from-pairs constructor, and HashSet<string>.TryGetValue (the\neager intern table is a Dictionary now; the comment that argued HashSet\nwas the honest structure records why it changed). The two polyfills are\nunconditional because neither netstandard has the types; a net5.0+ target\nis what would force an #if.\n\nTests multi-target net8.0;net48 on Windows only (OS-conditioned csproj,\nso Linux CI runs exactly what it ran — verified against all three\nworkflows). Two portable shims replace .NET-5+/6+ APIs the suite used:\nWithTimeout for Task.WaitAsync(TimeSpan), ByReference for\nReferenceEqualityComparer. The full suite has run green on the Framework\nCLR itself: 1,423 on net48 against the netstandard2.0 build, ExcelDataReader's\nown Framework assembly included — verified by the owner on Windows.\nBoth nupkgs verified to carry lib/netstandard2.0 and lib/netstandard2.1.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-05T17:36:21Z",
          "tree_id": "67b64bc3184652344901c8a243f8ce1256d858b9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16fef6aa0fb6978c79559b92910716e6ad44c995"
        },
        "date": 1788630325015,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2766565,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 26596770,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 10824144,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 107699581,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 6983663,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 69300605,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 13091,
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
          "id": "e1d21c2d706b054d252bbea3a996b97f6f7bfc4b",
          "message": "Hygiene: .gitattributes and editorconfig trim — line endings become a decision\n\nThe repo had no .gitattributes, so line endings were whatever tool\ntouched a file last — four .linq files sat CRLF in the index beside an\nLF corpus, and the vocabulary respell nearly flipped three of them\nwholesale (caught in review; a 10-line diff had become 366). One rule\nnow: the index holds LF (* text=auto); .linq and .sln check out CRLF\nbecause their native editors are Windows-bound; spreadsheets and images\nare explicitly binary. This commit carries the one-time renormalization\nso the flip lives here, deliberately, and nowhere else.\n\n.editorconfig gains trailing-whitespace trimming, with markdown exempt\n(a trailing double-space is a hard line break).\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-06T00:49:13Z",
          "tree_id": "ce25b3593543c4145d69070ff009c9f67ff1aa09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/e1d21c2d706b054d252bbea3a996b97f6f7bfc4b"
        },
        "date": 1788712514322,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2766581,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 26596772,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 10824105,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 107699582,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 6983661,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 69299738,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ShapeConstruction",
            "value": 13091,
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
          "id": "bf1fc2851544d9f87ffb3b76df4fa0b714780243",
          "message": "Merge experiment/typed-spaces: the projection model\n\nThe deepest cut since wave 2: what was called Shape is a projection; the\nspace and its subspaces are the shapes. Seven phases plus pre-merge\ncleanup (docs/design/projection-model-spec.md, judgment record in §10):\nthe modifier doubling killed (43 -> 6 irreducible), IShape -> IProjection\nwith namespace Unrect.Projections, the capability stack (IFormulaSpace,\nCapability<T>() transport, the slicing law, shared-formula\nreconstruction), OrBlank and the eachRow slot, the CaptionMap bind, the\nscoped entry Over<TSpace>() and MapWorkbook, phase-7 acceptance with the\nfourteen recorded refusals, and the three-sweep cleanup.\n\nSuite 1,709 green (net8.0 and net48); both library TFMs 0 warnings.\nBreaking: the rename, the Table family, the SpreadsheetSpace shell\nretirement, and the entries.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-07T23:11:42Z",
          "tree_id": "d091a9f7437fb1ec8d2b35119ab628b2a54047a3",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bf1fc2851544d9f87ffb3b76df4fa0b714780243"
        },
        "date": 1788823322924,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2766635,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 26596892,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 10824171,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 107699579,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 6983654,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 69298953,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 13074,
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
          "id": "4a44b787ac6b8b6fa5c487623e316bfe0e364292",
          "message": "Presence semantics and three hygiene fixes: the zero disambiguated\n\nThe algebra's highest-priority open problem (v0.4 §7/§14.1): geometric\nzero carried four meanings. The internal Presence { Read, Empty,\nAbsorbed } now rides beside Consumed on the engine's results — stamped\nat tolerance boundaries (Absorbed), settled-at-zero extents (Empty, read\noff the SETTLED extent so evaluation order and doors agree by\nconstruction), joined through layouts (Read if any child Read, else\nEmpty), defaulting Read. The internal epsilon (NothingProjection) makes\nthe flow-unit law stateable: VerticalFlow is a monoid at L3 for\nsuccessful readings, boundaries pinned.\n\nThe law, earned the hard way: presence explains a stop; the extent\ndecides one. The first implementation let presence decide the repeat\nguard; the law-tests caught a real L1/L2 change on\nVerticalRepeat(item.Optional().Sized(...)) — an Absorbed boundary under\na declared area legitimately consumes it and always kept the run going.\nThe guard is byte-identical to before; presence powers only the new\nteaching Info when a run ends at an absorbed item (D2), and the caught\nshape is pinned as the compatibility-law section of PresenceLawTests.\n\nDesign record: docs/design/presence-and-unit-spec.md (DECIDED, with the\nD5 amendment trail).\n\nAlso, with pins: ChoiceProjection.Summarise renders nested aggregates\ndepth-indented with one location per line; MinOffset() is one canonical\ninstance (HasDeclaredOffset correct by construction); the content-\nmatching rule reduced to one implementation (CellMatching primitives,\nTableView's dictionary keyed on TextComparer; CaptionComparer's\ndeliberate divergence untouched). spike/Phase1Probe removed (a husk).\n\nSuite 1,820 -> 1,862 green; both library TFMs 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-09T15:34:18Z",
          "tree_id": "883452644caa55f06a0f9c2d530ee9c0cece2b92",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4a44b787ac6b8b6fa5c487623e316bfe0e364292"
        },
        "date": 1788969082665,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2766634,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 26596826,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 10824206,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 107699655,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 6983734,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 69299634,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 13091,
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
          "id": "d06708df60e381758b11dde07836c674565409b6",
          "message": "Merge experiment/record-primitive: streaming composites over a bound, the band tiler, Table composed from primitives\n\nThe labels-as-context primitives (ColumnLabels, WithColumnLabels, Record),\nSkipToFirstNonBlankCell, and the uniform offset law; the engine streaming a\ndiscovered bound through a composite (TailSpace, a bound-aware Exceeds, lazy\nflow and repeat slices); VerticalBands/HorizontalBands, the tiler — a\nrepeating fixed-dimension space, distinct from the pattern repeat;\nTable(headerRows, eachRow) composed over the tiler under a UnitProjection,\nwith AsScaffolding and a mark-driven path fold giving it the leaf's own\ndiagnostics; ColumnLabels self-contained, its header parse the one home in\nLabelMap.FromHeader; and the repeat returned to a pure walk, onBlank living\non the tiler alone.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-14T17:43:35Z",
          "tree_id": "1c80a0bed7aeb311bb6e1bdb0933ac5a05562434",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/d06708df60e381758b11dde07836c674565409b6"
        },
        "date": 1789408535238,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 3166886,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 30597088,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 10904447,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 108499896,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 7063995,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 70099944,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 13075,
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
          "id": "33f1afa3496afb1fabd2e0242197d1c5d9889568",
          "message": "Merge experiment/point-and-line: the Point substrate — one vocabulary over any ISpace, planes and points as the locators, the kinds in Unrect.Spreadsheets\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-16T04:05:17Z",
          "tree_id": "09d4c757d0f1502df340fb2a2481c31f78e8d8b2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/33f1afa3496afb1fabd2e0242197d1c5d9889568"
        },
        "date": 1789531990544,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2286924,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 21797112,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 27628038,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 275703549,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 5224000,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 51699707,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 13587,
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
          "id": "0f34f159151554afcc0555f7afc83fdd486b8c06",
          "message": "Merge experiment/point-follow-ups: Extents into Plane, Unrect.Interactive, and the typed-predicate lift\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-17T02:37:19Z",
          "tree_id": "cfa1d003f75555e9d6f330d316cdd949a1da2b25",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/0f34f159151554afcc0555f7afc83fdd486b8c06"
        },
        "date": 1789613138810,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2286910,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 21797121,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 27628015,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 275703317,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 5223944,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 51699206,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 13570,
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
          "id": "717d1ea3b61c226c608194aae5ff909fd4603455",
          "message": "Packaging: Unrect.Interactive ships as its own package\n\nThe project was already packable — id, description, tags, and a\ndependency on exactly Unrect and Unrect.Spreadsheets at the same MinVer\nversion — but the release workflow packed and asserted two packages, and\nthe README and build props named two. Now three.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T01:12:25Z",
          "tree_id": "f01ec5a705245aededd07d5455e7b03ad4f8880d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/717d1ea3b61c226c608194aae5ff909fd4603455"
        },
        "date": 1789796483649,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2709882,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 25495247,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 96301282,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 956077408,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 5749014,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 55894328,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 14074,
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
          "id": "4818c36f9e9b0c2b75a4093dbbce6981329fd14b",
          "message": "Merge feature/scaffold-shapes: ScaffoldRecord and ScaffoldClass, on a sheet and as leaves\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T15:35:04Z",
          "tree_id": "d48076b68bb0a93dc6ab6797b10f0ead882156a7",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4818c36f9e9b0c2b75a4093dbbce6981329fd14b"
        },
        "date": 1789847939315,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2709882,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 25495197,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 96301313,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 956077408,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 5749014,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 55894235,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 14074,
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
          "id": "bd027f2136cee944da88c1eae17a06b14a05579d",
          "message": "Merge feature/bind-from-row: Table<T> fills a member from the row - Column(member, row => ...)\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T19:49:16Z",
          "tree_id": "88c877501aeddcefb436f136842b9904cda55a09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bd027f2136cee944da88c1eae17a06b14a05579d"
        },
        "date": 1789863954246,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2709996,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 25497559,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 96301569,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 956077664,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 5749150,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 55894949,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 17692,
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
          "id": "96b946d5920955eb81be3eab46697a3e4639e2e4",
          "message": "Merge feature/header-bands: headers of several rows, columns addressed by path, flat binding over bands\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T03:17:51Z",
          "tree_id": "33ccb96a2f1d1cb7795dde522c6c586ee8ce479a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/96b946d5920955eb81be3eab46697a3e4639e2e4"
        },
        "date": 1789888952879,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_10k",
            "value": 2712459,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Lambda_100k",
            "value": 25498416,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_10k",
            "value": 96306993,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_100k",
            "value": 956083088,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_10k",
            "value": 5750687,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Dictionary_100k",
            "value": 55896488,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Tables.Bound_ProjectionConstruction",
            "value": 19754,
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
        "date": 1788472137982,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56000435,
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
          "id": "f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb",
          "message": "Fix the 2-core CI flake: a blocked-borrower proof needs a started borrower\n\nAReachWaitsForAWarmerRatherThanStartingASecondOpenOfTheSameFile failed on\nthe GitHub runner (ef348dd) on \"the wait is counted\": WarmWaitMilliseconds\nwas 0, and 0 was the honest count. The pool's warmers ride Task.Run and\nthe gated arrangement BLOCKS them inside their opens, one pool thread\neach — on a two-core runner that is the entire starting thread pool, so\nthe test's own Task.Run borrower never started until thread injection got\naround to it. Both blocked-ness assertions passed vacuously (not finished\nbecause not scheduled), and by the time the reach ran, the warm reader was\nparked and there was nothing left to wait for.\n\nReproduced under taskset -c 0,1: three failures in four runs before the\nfix, none in six Debug runs plus a Release run after. The fix is\nOnItsOwnThread (TaskCreationOptions.LongRunning) at the four sites that\nassert a borrower is blocked — a dedicated thread starts unconditionally,\nso \"started, and still not finished\" really does mean \"parked inside\nBorrow\". The three sibling sites could only pass vacuously, never fail,\nbut their proofs were the same lie under starvation. The burst tests\nalready stood on structural evidence (SpinUntil on OpensStarted) and are\nuntouched.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T22:37:18Z",
          "tree_id": "c37ffff8e7e618f8d8cdb3778c429c1bd5259fc9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb"
        },
        "date": 1788475704094,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56000401,
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
          "id": "10027e9f1d263aac70041f0f7166b186324129e8",
          "message": "Both doors measure a sheet that will not say how big it is\n\nSpreadsheetSpace.Create sized its grid from reader.RowCount/FieldCount and\nsilently yielded an empty space when the reader would not give them — the\none outcome an adapter must not have, and a divergence from the streaming\ndoor, which has measured such sheets since Part 2 step 7. The fill is now\ntwo named siblings behind one dichotomy: ReadDeclared (the original loop,\nunchanged) and ReadMeasured (rows collected at their own width, the widest\nrow wins, absent trailing cells Blank — the same answer Workbook.Measure\ngives). The guard is rowCount <= 0 alone, deliberately mirroring the\nstreaming door so the two can never disagree about the same file.\n\nThe recorded cause was wrong, and is corrected everywhere it appeared: a\nmissing dimension element does not trigger this — ExcelDataReader derives\nboth counts from a pre-scan of the cells on every format it handles. The\nreachable trigger is a sheet with NO valued cell (rows of formatted-but-\nvalueless cells, a pre-formatted export region). Pinned by the committed\nTestData/no-extent.xlsx (dimensionless AND valueless, with the survey's\nRowsMeasured == 4 doubling as the fixture's own guard against a\nregeneration that quietly stops reaching the path) and a both-doors\nidentity test.\n\nRides along, both owner decisions from this session's discussion:\n- MaxReaders: spec §14 Q2 DECIDED — 3 stays and stops being provisional,\n  because no number is right: reader demand is the declaration's monotone-\n  cursor count, unbounded in principle, data-independent in practice, and\n  the ceiling fails gently (Reopens is the counted, named signal to raise\n  it). Sizing guidance added to docs/streaming.md; per-reader economics\n  (~5s CPU per open, position must be walked, reader-per-row is O(n^2))\n  recorded in the spec.\n- Table's header-derived width: spec §14 Q1 DEFERRED, superseding the\n  2026-09-03 yes — the step-8 interleave delivered the lazy win with\n  today's denotation intact, so the K-1 campaign votes before the\n  denotation change is paid for.\n\nSuite 1,382 -> 1,387; gates green in Debug and 2-core Release.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T14:29:21Z",
          "tree_id": "fc431b0954d2e3a5115a177bd1a21d63c169ffae",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/10027e9f1d263aac70041f0f7166b186324129e8"
        },
        "date": 1788533065567,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56000399,
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
          "id": "c01531cec6968e544acc578291244292172a00a5",
          "message": "Docs: Part 3 deferred on principle, and .Sized's composite role stated honestly\n\nSpec §13 gains the Part 3 row (bound-aware composite placement): the\nengine's remaining greed sorted into one necessary force (Repeat items —\nthe item's existence is the question), one free force (post-Project\nconsumption, amortised by the root's accounting), and one debt (composite\nchild placement, whose questions have lazy answers nobody asks for).\nDeferred until the first tall sized composite pays the debt — sized\ncomposites in the corpus are short header bands, where settling eagerly\ncosts nothing. The K-1 campaign is the likely judge; the census pin is the\ntripwire.\n\ndocs/streaming.md stops saying \"put the .Sized on the leaf\" as if it were\na law: a sized composite is a legitimate spelling with no leaf equivalent\n— a composite has no intrinsic extent, and the declared band is what\nscopes its internal seeks and settles its consumption.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T15:37:53Z",
          "tree_id": "6188ce68af3130bfba604f38845b0c515958cb34",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/c01531cec6968e544acc578291244292172a00a5"
        },
        "date": 1788537631185,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56000435,
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
          "id": "2d73985e95c70f51a2b26d7dc98c3936f1f52d5d",
          "message": "Retention: the live-set floor for the interning change, with the target on the chart\n\nAn eighth CI leg that is not a BenchmarkDotNet family: interning reduces\nRETAINED bytes, not allocations (a duplicate string is allocated by the\nreader before the adapter sees it and dies young after dedup), so the\nAllocated column cannot see it — and retention is deterministic, so it\nneeds no statistical engine. A one-shot job measures live bytes with the\nresult held, emits the same JSON document the rig already stores, and\nrides the same workflow and dashboard as everything else.\n\nBuilding it surfaced two facts worth more than the plumbing:\n\n- The eager door's duplication depends on how the file spells its text.\n  Shared-string cells come back already deduped (the reader returns its\n  table's own instance); inline strings and formula-result cells\n  materialise fresh per cell. A real Excel export is both (the local K-1:\n  9,049 text cells, 2,876 values, 4,016 instances — the formula results\n  are the duplicated half). The family brackets it, and the shared-string\n  row is the priced TARGET: the same cells read 112.0 MB duplicated vs\n  58.2 MB deduped, so ~48% is what a complete eager interner is worth on\n  this shape — short of that is unfinished, not failed.\n- The first fixture boxed decimals a real read never produces (16 MB of\n  boxes in a retained-bytes measurement); the retention fixtures now\n  yield doubles like a reader does. StreamingSpaces is deliberately\n  untouched — changing it would re-baseline that family's history.\n\nScenarios exercise the real seams the interning change will live in: the\neager rows go through SpreadsheetSpace.Create over generated workbooks\n(RetentionWorkbooks: a minimal hand-rolled OOXML writer, no new package;\nthe one deliberate exception to the no-workbooks rule, recorded in\ndocs/benchmarking.md), the streaming rows through the store's chunk fill.\nFloor: eager space held 106.8 MB, results held 82.1 MB both doors\n(byte-identical — streaming's promise stated in the metric), controls\nbyte-identical to their duplicated twins by fixed-width padding. Leg\nruns ~65s, the shortest in the matrix.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T16:47:38Z",
          "tree_id": "0c756ae6dd2d4f17cd84e585c99d7d3ae08fd409",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/2d73985e95c70f51a2b26d7dc98c3936f1f52d5d"
        },
        "date": 1788542162720,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56000404,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 30400166,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 12,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetString",
            "value": 1,
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
            "value": 0,
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
          "id": "eddc5d17c38c715f41cd95d041452deb66f8354c",
          "message": "Interning: equal text shares one instance through both doors\n\nAdapter-level string interning — a find-my-twin table at each door's\nadapt seam. The eager door threads a per-Create-call HashSet through both\nfill paths (one instance per distinct value across every sheet of the\ncall); the streaming door hangs one capped ConcurrentDictionary on the\nWorkbook, plumbed into every store's chunk fill, so a chase reader's\nre-parse dedupes against the first parse. Strings only: every other kind\nis inline in the 24-byte struct or unreachable from the spreadsheet door.\n\nThe win is retention, not allocation — the duplicate is allocated by the\nreader before the adapter sees it and dies in gen0 after dedup, which is\nwhy the Retention leg is the judge and MemoryDiagnoser is blind to it.\nMeasured against the committed floor: eager space held 106.8 -> 55.5 MB,\nlanding on the priced shared-string target to the byte; held results\n82.1 -> 30.8 MB, byte-identical across doors; all three unique controls\nflat to the byte; wall time noise on the 1M-row parse.\n\nThe cap (WorkbookOptions.MaxInternedStrings, default 65,536; 0 = off)\nand the 256-char length guard bound what the book-lifetime table can\npin. Documented as a two-way knob: a full table costs its entries for\nthe book's life — some 40 MB at the default cap — so it turns DOWN, to\n0, for known-unique text, and the docs say so at every site (the rig is\nstructurally blind to the table's own live set: readings are taken with\nthe book closed). Workbook.InterningStatistics reports hits, distinct,\nand estimated bytes (64-bit layout, exact for it; Hits counts fills, so\na reloaded chunk counts again — read against ChunkReloads).\n\nExcelDataReader fact on the record: shared-string cells arrive\npre-deduped from the reader's own SST; the duplication this kills comes\nfrom inline-string cells, formula-result cells, and .xls. Pinned by 36\ntests including a cross-door sharing differential, mutation-checked both\ndirections, and a WeakReference proof that Dispose releases the strings.\nSuite 1,423.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T19:19:56Z",
          "tree_id": "fea5150f427d5b65cf652662879afb3b470547f2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/eddc5d17c38c715f41cd95d041452deb66f8354c"
        },
        "date": 1788556080631,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56000399,
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
          "id": "16fef6aa0fb6978c79559b92910716e6ad44c995",
          "message": "netstandard2.0: the packages install on .NET Framework, one source, no forks\n\nAll four libraries multi-target netstandard2.0;netstandard2.1. The rule\nthat made it clean: no #if anywhere — netstandard2.1-only constructs are\nremoved rather than branched on, so both targets compile one source and\ncannot drift.\n\nThe structural change is de-DIMing the incremental calculus. .NET\nFramework's runtime cannot dispatch a default interface member, so the\ndefinitional folds move from DIM bodies to the static Unrect.Core.Scans\n(Fold/FoldSize/FoldArea) and internal ColumnAccumulators.Fold, with every\nimplementation delegating in one line. The guarantee shifts and every doc\nthat claimed it says so honestly: \"eager and lazy cannot disagree by\nconstruction\" is now \"cannot disagree, pinned by the fold-identity suite\"\n— IncrementalStrategyTests asserts SelectRows == a hand-written fold for\nevery factory, and a new incremental strategy carries the obligation to\njoin that theory data. Corollary recorded in capability-seam-notes: the\nDIM \"safety net\" for evolving ISpace is withdrawn — a member added to a\npublished Core interface is now a breaking change with no escape hatch;\ntype-testing is the whole capability recipe.\n\nThe rest the compiler found: System.HashCode (Core hand-rolls its combine\nrather than take Microsoft.Bcl.HashCode — the bundling would have\nsuppressed the dependency from the nuspec and thrown FileNotFoundException\non Framework; both packages stay at zero new dependencies), the\nDictionary-from-pairs constructor, and HashSet<string>.TryGetValue (the\neager intern table is a Dictionary now; the comment that argued HashSet\nwas the honest structure records why it changed). The two polyfills are\nunconditional because neither netstandard has the types; a net5.0+ target\nis what would force an #if.\n\nTests multi-target net8.0;net48 on Windows only (OS-conditioned csproj,\nso Linux CI runs exactly what it ran — verified against all three\nworkflows). Two portable shims replace .NET-5+/6+ APIs the suite used:\nWithTimeout for Task.WaitAsync(TimeSpan), ByReference for\nReferenceEqualityComparer. The full suite has run green on the Framework\nCLR itself: 1,423 on net48 against the netstandard2.0 build, ExcelDataReader's\nown Framework assembly included — verified by the owner on Windows.\nBoth nupkgs verified to carry lib/netstandard2.0 and lib/netstandard2.1.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-05T17:36:21Z",
          "tree_id": "67b64bc3184652344901c8a243f8ce1256d858b9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16fef6aa0fb6978c79559b92910716e6ad44c995"
        },
        "date": 1788630325202,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56000399,
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
          "id": "e1d21c2d706b054d252bbea3a996b97f6f7bfc4b",
          "message": "Hygiene: .gitattributes and editorconfig trim — line endings become a decision\n\nThe repo had no .gitattributes, so line endings were whatever tool\ntouched a file last — four .linq files sat CRLF in the index beside an\nLF corpus, and the vocabulary respell nearly flipped three of them\nwholesale (caught in review; a 10-line diff had become 366). One rule\nnow: the index holds LF (* text=auto); .linq and .sln check out CRLF\nbecause their native editors are Windows-bound; spreadsheets and images\nare explicitly binary. This commit carries the one-time renormalization\nso the flip lives here, deliberately, and nowhere else.\n\n.editorconfig gains trailing-whitespace trimming, with markdown exempt\n(a trailing double-space is a hard line break).\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-06T00:49:13Z",
          "tree_id": "ce25b3593543c4145d69070ff009c9f67ff1aa09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/e1d21c2d706b054d252bbea3a996b97f6f7bfc4b"
        },
        "date": 1788712514525,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56000399,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 30400167,
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
          "id": "bf1fc2851544d9f87ffb3b76df4fa0b714780243",
          "message": "Merge experiment/typed-spaces: the projection model\n\nThe deepest cut since wave 2: what was called Shape is a projection; the\nspace and its subspaces are the shapes. Seven phases plus pre-merge\ncleanup (docs/design/projection-model-spec.md, judgment record in §10):\nthe modifier doubling killed (43 -> 6 irreducible), IShape -> IProjection\nwith namespace Unrect.Projections, the capability stack (IFormulaSpace,\nCapability<T>() transport, the slicing law, shared-formula\nreconstruction), OrBlank and the eachRow slot, the CaptionMap bind, the\nscoped entry Over<TSpace>() and MapWorkbook, phase-7 acceptance with the\nfourteen recorded refusals, and the three-sweep cleanup.\n\nSuite 1,709 green (net8.0 and net48); both library TFMs 0 warnings.\nBreaking: the rename, the Table family, the SpreadsheetSpace shell\nretirement, and the entries.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-07T23:11:42Z",
          "tree_id": "d091a9f7437fb1ec8d2b35119ab628b2a54047a3",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bf1fc2851544d9f87ffb3b76df4fa0b714780243"
        },
        "date": 1788823323150,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56000416,
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
          "id": "4a44b787ac6b8b6fa5c487623e316bfe0e364292",
          "message": "Presence semantics and three hygiene fixes: the zero disambiguated\n\nThe algebra's highest-priority open problem (v0.4 §7/§14.1): geometric\nzero carried four meanings. The internal Presence { Read, Empty,\nAbsorbed } now rides beside Consumed on the engine's results — stamped\nat tolerance boundaries (Absorbed), settled-at-zero extents (Empty, read\noff the SETTLED extent so evaluation order and doors agree by\nconstruction), joined through layouts (Read if any child Read, else\nEmpty), defaulting Read. The internal epsilon (NothingProjection) makes\nthe flow-unit law stateable: VerticalFlow is a monoid at L3 for\nsuccessful readings, boundaries pinned.\n\nThe law, earned the hard way: presence explains a stop; the extent\ndecides one. The first implementation let presence decide the repeat\nguard; the law-tests caught a real L1/L2 change on\nVerticalRepeat(item.Optional().Sized(...)) — an Absorbed boundary under\na declared area legitimately consumes it and always kept the run going.\nThe guard is byte-identical to before; presence powers only the new\nteaching Info when a run ends at an absorbed item (D2), and the caught\nshape is pinned as the compatibility-law section of PresenceLawTests.\n\nDesign record: docs/design/presence-and-unit-spec.md (DECIDED, with the\nD5 amendment trail).\n\nAlso, with pins: ChoiceProjection.Summarise renders nested aggregates\ndepth-indented with one location per line; MinOffset() is one canonical\ninstance (HasDeclaredOffset correct by construction); the content-\nmatching rule reduced to one implementation (CellMatching primitives,\nTableView's dictionary keyed on TextComparer; CaptionComparer's\ndeliberate divergence untouched). spike/Phase1Probe removed (a husk).\n\nSuite 1,820 -> 1,862 green; both library TFMs 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-09T15:34:18Z",
          "tree_id": "883452644caa55f06a0f9c2d530ee9c0cece2b92",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4a44b787ac6b8b6fa5c487623e316bfe0e364292"
        },
        "date": 1788969082919,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56000401,
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
          "id": "d06708df60e381758b11dde07836c674565409b6",
          "message": "Merge experiment/record-primitive: streaming composites over a bound, the band tiler, Table composed from primitives\n\nThe labels-as-context primitives (ColumnLabels, WithColumnLabels, Record),\nSkipToFirstNonBlankCell, and the uniform offset law; the engine streaming a\ndiscovered bound through a composite (TailSpace, a bound-aware Exceeds, lazy\nflow and repeat slices); VerticalBands/HorizontalBands, the tiler — a\nrepeating fixed-dimension space, distinct from the pattern repeat;\nTable(headerRows, eachRow) composed over the tiler under a UnitProjection,\nwith AsScaffolding and a mark-driven path fold giving it the leaf's own\ndiagnostics; ColumnLabels self-contained, its header parse the one home in\nLabelMap.FromHeader; and the repeat returned to a pure walk, onBlank living\non the tiler alone.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-14T17:43:35Z",
          "tree_id": "1c80a0bed7aeb311bb6e1bdb0933ac5a05562434",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/d06708df60e381758b11dde07836c674565409b6"
        },
        "date": 1789408535523,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56000415,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 30400166,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Sweep_GetDecimal",
            "value": 12,
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
            "value": 0,
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
          "id": "33f1afa3496afb1fabd2e0242197d1c5d9889568",
          "message": "Merge experiment/point-and-line: the Point substrate — one vocabulary over any ISpace, planes and points as the locators, the kinds in Unrect.Spreadsheets\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-16T04:05:17Z",
          "tree_id": "09d4c757d0f1502df340fb2a2481c31f78e8d8b2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/33f1afa3496afb1fabd2e0242197d1c5d9889568"
        },
        "date": 1789531990847,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 30400222,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsBlank_Million",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsText_Million",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.AsText_Million_Text",
            "value": 6,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Decimal_Million",
            "value": 40000023,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Point_Mint_Million",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Slice_Million",
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
          "distinct": true,
          "id": "0f34f159151554afcc0555f7afc83fdd486b8c06",
          "message": "Merge experiment/point-follow-ups: Extents into Plane, Unrect.Interactive, and the typed-predicate lift\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-17T02:37:19Z",
          "tree_id": "cfa1d003f75555e9d6f330d316cdd949a1da2b25",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/0f34f159151554afcc0555f7afc83fdd486b8c06"
        },
        "date": 1789613139071,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 30400200,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsBlank_Million",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsText_Million",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.AsText_Million_Text",
            "value": 6,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Decimal_Million",
            "value": 40000023,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Point_Mint_Million",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Predicate_Million",
            "value": 6,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.TypedPredicate_Million",
            "value": 46,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Slice_Million",
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
          "id": "717d1ea3b61c226c608194aae5ff909fd4603455",
          "message": "Packaging: Unrect.Interactive ships as its own package\n\nThe project was already packable — id, description, tags, and a\ndependency on exactly Unrect and Unrect.Spreadsheets at the same MinVer\nversion — but the release workflow packed and asserted two packages, and\nthe README and build props named two. Now three.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T01:12:25Z",
          "tree_id": "f01ec5a705245aededd07d5455e7b03ad4f8880d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/717d1ea3b61c226c608194aae5ff909fd4603455"
        },
        "date": 1789796484057,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 30400222,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsBlank_Million",
            "value": 3,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsText_Million",
            "value": 3,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.AsText_Million_Text",
            "value": 6,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Decimal_Million",
            "value": 40000023,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Point_Mint_Million",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Predicate_Million",
            "value": 3,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.TypedPredicate_Million",
            "value": 53,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Slice_Million",
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
          "id": "4818c36f9e9b0c2b75a4093dbbce6981329fd14b",
          "message": "Merge feature/scaffold-shapes: ScaffoldRecord and ScaffoldClass, on a sheet and as leaves\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T15:35:04Z",
          "tree_id": "d48076b68bb0a93dc6ab6797b10f0ead882156a7",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4818c36f9e9b0c2b75a4093dbbce6981329fd14b"
        },
        "date": 1789847939562,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 30400222,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsBlank_Million",
            "value": 3,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsText_Million",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.AsText_Million_Text",
            "value": 6,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Decimal_Million",
            "value": 40000023,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Point_Mint_Million",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Predicate_Million",
            "value": 3,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.TypedPredicate_Million",
            "value": 53,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Slice_Million",
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
          "id": "bd027f2136cee944da88c1eae17a06b14a05579d",
          "message": "Merge feature/bind-from-row: Table<T> fills a member from the row - Column(member, row => ...)\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T19:49:16Z",
          "tree_id": "88c877501aeddcefb436f136842b9904cda55a09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bd027f2136cee944da88c1eae17a06b14a05579d"
        },
        "date": 1789863954519,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 30400222,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsBlank_Million",
            "value": 3,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsText_Million",
            "value": 3,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.AsText_Million_Text",
            "value": 6,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Decimal_Million",
            "value": 40000023,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Point_Mint_Million",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Predicate_Million",
            "value": 3,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.TypedPredicate_Million",
            "value": 53,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Slice_Million",
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
          "id": "96b946d5920955eb81be3eab46697a3e4639e2e4",
          "message": "Merge feature/header-bands: headers of several rows, columns addressed by path, flat binding over bands\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T03:17:51Z",
          "tree_id": "33ccb96a2f1d1cb7795dde522c6c586ee8ce479a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/96b946d5920955eb81be3eab46697a3e4639e2e4"
        },
        "date": 1789888953153,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Values.Create_FromInts",
            "value": 56,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Create_FromObjects",
            "value": 30400222,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsBlank_Million",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.IsText_Million",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.AsText_Million_Text",
            "value": 3,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Decimal_Million",
            "value": 40000012,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Point_Mint_Million",
            "value": 1,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Predicate_Million",
            "value": 3,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.TypedPredicate_Million",
            "value": 46,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Values.Slice_Million",
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
        "date": 1788472138140,
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
          "id": "f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb",
          "message": "Fix the 2-core CI flake: a blocked-borrower proof needs a started borrower\n\nAReachWaitsForAWarmerRatherThanStartingASecondOpenOfTheSameFile failed on\nthe GitHub runner (ef348dd) on \"the wait is counted\": WarmWaitMilliseconds\nwas 0, and 0 was the honest count. The pool's warmers ride Task.Run and\nthe gated arrangement BLOCKS them inside their opens, one pool thread\neach — on a two-core runner that is the entire starting thread pool, so\nthe test's own Task.Run borrower never started until thread injection got\naround to it. Both blocked-ness assertions passed vacuously (not finished\nbecause not scheduled), and by the time the reach ran, the warm reader was\nparked and there was nothing left to wait for.\n\nReproduced under taskset -c 0,1: three failures in four runs before the\nfix, none in six Debug runs plus a Release run after. The fix is\nOnItsOwnThread (TaskCreationOptions.LongRunning) at the four sites that\nassert a borrower is blocked — a dedicated thread starts unconditionally,\nso \"started, and still not finished\" really does mean \"parked inside\nBorrow\". The three sibling sites could only pass vacuously, never fail,\nbut their proofs were the same lie under starvation. The burst tests\nalready stood on structural evidence (SpinUntil on OpensStarted) and are\nuntouched.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T22:37:18Z",
          "tree_id": "c37ffff8e7e618f8d8cdb3778c429c1bd5259fc9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb"
        },
        "date": 1788475704255,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 3874497,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 38635751,
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
          "id": "10027e9f1d263aac70041f0f7166b186324129e8",
          "message": "Both doors measure a sheet that will not say how big it is\n\nSpreadsheetSpace.Create sized its grid from reader.RowCount/FieldCount and\nsilently yielded an empty space when the reader would not give them — the\none outcome an adapter must not have, and a divergence from the streaming\ndoor, which has measured such sheets since Part 2 step 7. The fill is now\ntwo named siblings behind one dichotomy: ReadDeclared (the original loop,\nunchanged) and ReadMeasured (rows collected at their own width, the widest\nrow wins, absent trailing cells Blank — the same answer Workbook.Measure\ngives). The guard is rowCount <= 0 alone, deliberately mirroring the\nstreaming door so the two can never disagree about the same file.\n\nThe recorded cause was wrong, and is corrected everywhere it appeared: a\nmissing dimension element does not trigger this — ExcelDataReader derives\nboth counts from a pre-scan of the cells on every format it handles. The\nreachable trigger is a sheet with NO valued cell (rows of formatted-but-\nvalueless cells, a pre-formatted export region). Pinned by the committed\nTestData/no-extent.xlsx (dimensionless AND valueless, with the survey's\nRowsMeasured == 4 doubling as the fixture's own guard against a\nregeneration that quietly stops reaching the path) and a both-doors\nidentity test.\n\nRides along, both owner decisions from this session's discussion:\n- MaxReaders: spec §14 Q2 DECIDED — 3 stays and stops being provisional,\n  because no number is right: reader demand is the declaration's monotone-\n  cursor count, unbounded in principle, data-independent in practice, and\n  the ceiling fails gently (Reopens is the counted, named signal to raise\n  it). Sizing guidance added to docs/streaming.md; per-reader economics\n  (~5s CPU per open, position must be walked, reader-per-row is O(n^2))\n  recorded in the spec.\n- Table's header-derived width: spec §14 Q1 DEFERRED, superseding the\n  2026-09-03 yes — the step-8 interleave delivered the lazy win with\n  today's denotation intact, so the K-1 campaign votes before the\n  denotation change is paid for.\n\nSuite 1,382 -> 1,387; gates green in Debug and 2-core Release.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T14:29:21Z",
          "tree_id": "fc431b0954d2e3a5115a177bd1a21d63c169ffae",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/10027e9f1d263aac70041f0f7166b186324129e8"
        },
        "date": 1788533065771,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 3988969,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 39726191,
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
          "id": "c01531cec6968e544acc578291244292172a00a5",
          "message": "Docs: Part 3 deferred on principle, and .Sized's composite role stated honestly\n\nSpec §13 gains the Part 3 row (bound-aware composite placement): the\nengine's remaining greed sorted into one necessary force (Repeat items —\nthe item's existence is the question), one free force (post-Project\nconsumption, amortised by the root's accounting), and one debt (composite\nchild placement, whose questions have lazy answers nobody asks for).\nDeferred until the first tall sized composite pays the debt — sized\ncomposites in the corpus are short header bands, where settling eagerly\ncosts nothing. The K-1 campaign is the likely judge; the census pin is the\ntripwire.\n\ndocs/streaming.md stops saying \"put the .Sized on the leaf\" as if it were\na law: a sized composite is a legitimate spelling with no leaf equivalent\n— a composite has no intrinsic extent, and the declared band is what\nscopes its internal seeks and settles its consumption.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T15:37:53Z",
          "tree_id": "6188ce68af3130bfba604f38845b0c515958cb34",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/c01531cec6968e544acc578291244292172a00a5"
        },
        "date": 1788537631393,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 3988971,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 39726191,
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
          "id": "2d73985e95c70f51a2b26d7dc98c3936f1f52d5d",
          "message": "Retention: the live-set floor for the interning change, with the target on the chart\n\nAn eighth CI leg that is not a BenchmarkDotNet family: interning reduces\nRETAINED bytes, not allocations (a duplicate string is allocated by the\nreader before the adapter sees it and dies young after dedup), so the\nAllocated column cannot see it — and retention is deterministic, so it\nneeds no statistical engine. A one-shot job measures live bytes with the\nresult held, emits the same JSON document the rig already stores, and\nrides the same workflow and dashboard as everything else.\n\nBuilding it surfaced two facts worth more than the plumbing:\n\n- The eager door's duplication depends on how the file spells its text.\n  Shared-string cells come back already deduped (the reader returns its\n  table's own instance); inline strings and formula-result cells\n  materialise fresh per cell. A real Excel export is both (the local K-1:\n  9,049 text cells, 2,876 values, 4,016 instances — the formula results\n  are the duplicated half). The family brackets it, and the shared-string\n  row is the priced TARGET: the same cells read 112.0 MB duplicated vs\n  58.2 MB deduped, so ~48% is what a complete eager interner is worth on\n  this shape — short of that is unfinished, not failed.\n- The first fixture boxed decimals a real read never produces (16 MB of\n  boxes in a retained-bytes measurement); the retention fixtures now\n  yield doubles like a reader does. StreamingSpaces is deliberately\n  untouched — changing it would re-baseline that family's history.\n\nScenarios exercise the real seams the interning change will live in: the\neager rows go through SpreadsheetSpace.Create over generated workbooks\n(RetentionWorkbooks: a minimal hand-rolled OOXML writer, no new package;\nthe one deliberate exception to the no-workbooks rule, recorded in\ndocs/benchmarking.md), the streaming rows through the store's chunk fill.\nFloor: eager space held 106.8 MB, results held 82.1 MB both doors\n(byte-identical — streaming's promise stated in the metric), controls\nbyte-identical to their duplicated twins by fixed-width padding. Leg\nruns ~65s, the shortest in the matrix.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T16:47:38Z",
          "tree_id": "0c756ae6dd2d4f17cd84e585c99d7d3ae08fd409",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/2d73985e95c70f51a2b26d7dc98c3936f1f52d5d"
        },
        "date": 1788542163248,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 3988971,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 39726191,
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
          "id": "eddc5d17c38c715f41cd95d041452deb66f8354c",
          "message": "Interning: equal text shares one instance through both doors\n\nAdapter-level string interning — a find-my-twin table at each door's\nadapt seam. The eager door threads a per-Create-call HashSet through both\nfill paths (one instance per distinct value across every sheet of the\ncall); the streaming door hangs one capped ConcurrentDictionary on the\nWorkbook, plumbed into every store's chunk fill, so a chase reader's\nre-parse dedupes against the first parse. Strings only: every other kind\nis inline in the 24-byte struct or unreachable from the spreadsheet door.\n\nThe win is retention, not allocation — the duplicate is allocated by the\nreader before the adapter sees it and dies in gen0 after dedup, which is\nwhy the Retention leg is the judge and MemoryDiagnoser is blind to it.\nMeasured against the committed floor: eager space held 106.8 -> 55.5 MB,\nlanding on the priced shared-string target to the byte; held results\n82.1 -> 30.8 MB, byte-identical across doors; all three unique controls\nflat to the byte; wall time noise on the 1M-row parse.\n\nThe cap (WorkbookOptions.MaxInternedStrings, default 65,536; 0 = off)\nand the 256-char length guard bound what the book-lifetime table can\npin. Documented as a two-way knob: a full table costs its entries for\nthe book's life — some 40 MB at the default cap — so it turns DOWN, to\n0, for known-unique text, and the docs say so at every site (the rig is\nstructurally blind to the table's own live set: readings are taken with\nthe book closed). Workbook.InterningStatistics reports hits, distinct,\nand estimated bytes (64-bit layout, exact for it; Hits counts fills, so\na reloaded chunk counts again — read against ChunkReloads).\n\nExcelDataReader fact on the record: shared-string cells arrive\npre-deduped from the reader's own SST; the duplication this kills comes\nfrom inline-string cells, formula-result cells, and .xls. Pinned by 36\ntests including a cross-door sharing differential, mutation-checked both\ndirections, and a WeakReference proof that Dispose releases the strings.\nSuite 1,423.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T19:19:56Z",
          "tree_id": "fea5150f427d5b65cf652662879afb3b470547f2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/eddc5d17c38c715f41cd95d041452deb66f8354c"
        },
        "date": 1788556080807,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 3988971,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 39726191,
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
          "id": "16fef6aa0fb6978c79559b92910716e6ad44c995",
          "message": "netstandard2.0: the packages install on .NET Framework, one source, no forks\n\nAll four libraries multi-target netstandard2.0;netstandard2.1. The rule\nthat made it clean: no #if anywhere — netstandard2.1-only constructs are\nremoved rather than branched on, so both targets compile one source and\ncannot drift.\n\nThe structural change is de-DIMing the incremental calculus. .NET\nFramework's runtime cannot dispatch a default interface member, so the\ndefinitional folds move from DIM bodies to the static Unrect.Core.Scans\n(Fold/FoldSize/FoldArea) and internal ColumnAccumulators.Fold, with every\nimplementation delegating in one line. The guarantee shifts and every doc\nthat claimed it says so honestly: \"eager and lazy cannot disagree by\nconstruction\" is now \"cannot disagree, pinned by the fold-identity suite\"\n— IncrementalStrategyTests asserts SelectRows == a hand-written fold for\nevery factory, and a new incremental strategy carries the obligation to\njoin that theory data. Corollary recorded in capability-seam-notes: the\nDIM \"safety net\" for evolving ISpace is withdrawn — a member added to a\npublished Core interface is now a breaking change with no escape hatch;\ntype-testing is the whole capability recipe.\n\nThe rest the compiler found: System.HashCode (Core hand-rolls its combine\nrather than take Microsoft.Bcl.HashCode — the bundling would have\nsuppressed the dependency from the nuspec and thrown FileNotFoundException\non Framework; both packages stay at zero new dependencies), the\nDictionary-from-pairs constructor, and HashSet<string>.TryGetValue (the\neager intern table is a Dictionary now; the comment that argued HashSet\nwas the honest structure records why it changed). The two polyfills are\nunconditional because neither netstandard has the types; a net5.0+ target\nis what would force an #if.\n\nTests multi-target net8.0;net48 on Windows only (OS-conditioned csproj,\nso Linux CI runs exactly what it ran — verified against all three\nworkflows). Two portable shims replace .NET-5+/6+ APIs the suite used:\nWithTimeout for Task.WaitAsync(TimeSpan), ByReference for\nReferenceEqualityComparer. The full suite has run green on the Framework\nCLR itself: 1,423 on net48 against the netstandard2.0 build, ExcelDataReader's\nown Framework assembly included — verified by the owner on Windows.\nBoth nupkgs verified to carry lib/netstandard2.0 and lib/netstandard2.1.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-05T17:36:21Z",
          "tree_id": "67b64bc3184652344901c8a243f8ce1256d858b9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16fef6aa0fb6978c79559b92910716e6ad44c995"
        },
        "date": 1788630325403,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 3988971,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 39726191,
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
          "id": "e1d21c2d706b054d252bbea3a996b97f6f7bfc4b",
          "message": "Hygiene: .gitattributes and editorconfig trim — line endings become a decision\n\nThe repo had no .gitattributes, so line endings were whatever tool\ntouched a file last — four .linq files sat CRLF in the index beside an\nLF corpus, and the vocabulary respell nearly flipped three of them\nwholesale (caught in review; a 10-line diff had become 366). One rule\nnow: the index holds LF (* text=auto); .linq and .sln check out CRLF\nbecause their native editors are Windows-bound; spreadsheets and images\nare explicitly binary. This commit carries the one-time renormalization\nso the flip lives here, deliberately, and nowhere else.\n\n.editorconfig gains trailing-whitespace trimming, with markdown exempt\n(a trailing double-space is a hard line break).\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-06T00:49:13Z",
          "tree_id": "ce25b3593543c4145d69070ff009c9f67ff1aa09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/e1d21c2d706b054d252bbea3a996b97f6f7bfc4b"
        },
        "date": 1788712514730,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 3988969,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 39726191,
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
          "id": "bf1fc2851544d9f87ffb3b76df4fa0b714780243",
          "message": "Merge experiment/typed-spaces: the projection model\n\nThe deepest cut since wave 2: what was called Shape is a projection; the\nspace and its subspaces are the shapes. Seven phases plus pre-merge\ncleanup (docs/design/projection-model-spec.md, judgment record in §10):\nthe modifier doubling killed (43 -> 6 irreducible), IShape -> IProjection\nwith namespace Unrect.Projections, the capability stack (IFormulaSpace,\nCapability<T>() transport, the slicing law, shared-formula\nreconstruction), OrBlank and the eachRow slot, the CaptionMap bind, the\nscoped entry Over<TSpace>() and MapWorkbook, phase-7 acceptance with the\nfourteen recorded refusals, and the three-sweep cleanup.\n\nSuite 1,709 green (net8.0 and net48); both library TFMs 0 warnings.\nBreaking: the rename, the Table family, the SpreadsheetSpace shell\nretirement, and the entries.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-07T23:11:42Z",
          "tree_id": "d091a9f7437fb1ec8d2b35119ab628b2a54047a3",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bf1fc2851544d9f87ffb3b76df4fa0b714780243"
        },
        "date": 1788823323385,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 4046643,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 40302219,
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
          "id": "4a44b787ac6b8b6fa5c487623e316bfe0e364292",
          "message": "Presence semantics and three hygiene fixes: the zero disambiguated\n\nThe algebra's highest-priority open problem (v0.4 §7/§14.1): geometric\nzero carried four meanings. The internal Presence { Read, Empty,\nAbsorbed } now rides beside Consumed on the engine's results — stamped\nat tolerance boundaries (Absorbed), settled-at-zero extents (Empty, read\noff the SETTLED extent so evaluation order and doors agree by\nconstruction), joined through layouts (Read if any child Read, else\nEmpty), defaulting Read. The internal epsilon (NothingProjection) makes\nthe flow-unit law stateable: VerticalFlow is a monoid at L3 for\nsuccessful readings, boundaries pinned.\n\nThe law, earned the hard way: presence explains a stop; the extent\ndecides one. The first implementation let presence decide the repeat\nguard; the law-tests caught a real L1/L2 change on\nVerticalRepeat(item.Optional().Sized(...)) — an Absorbed boundary under\na declared area legitimately consumes it and always kept the run going.\nThe guard is byte-identical to before; presence powers only the new\nteaching Info when a run ends at an absorbed item (D2), and the caught\nshape is pinned as the compatibility-law section of PresenceLawTests.\n\nDesign record: docs/design/presence-and-unit-spec.md (DECIDED, with the\nD5 amendment trail).\n\nAlso, with pins: ChoiceProjection.Summarise renders nested aggregates\ndepth-indented with one location per line; MinOffset() is one canonical\ninstance (HasDeclaredOffset correct by construction); the content-\nmatching rule reduced to one implementation (CellMatching primitives,\nTableView's dictionary keyed on TextComparer; CaptionComparer's\ndeliberate divergence untouched). spike/Phase1Probe removed (a husk).\n\nSuite 1,820 -> 1,862 green; both library TFMs 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-09T15:34:18Z",
          "tree_id": "883452644caa55f06a0f9c2d530ee9c0cece2b92",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4a44b787ac6b8b6fa5c487623e316bfe0e364292"
        },
        "date": 1788969083186,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 4046643,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 40302087,
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
          "id": "d06708df60e381758b11dde07836c674565409b6",
          "message": "Merge experiment/record-primitive: streaming composites over a bound, the band tiler, Table composed from primitives\n\nThe labels-as-context primitives (ColumnLabels, WithColumnLabels, Record),\nSkipToFirstNonBlankCell, and the uniform offset law; the engine streaming a\ndiscovered bound through a composite (TailSpace, a bound-aware Exceeds, lazy\nflow and repeat slices); VerticalBands/HorizontalBands, the tiler — a\nrepeating fixed-dimension space, distinct from the pattern repeat;\nTable(headerRows, eachRow) composed over the tiler under a UnitProjection,\nwith AsScaffolding and a mark-driven path fold giving it the leaf's own\ndiagnostics; ColumnLabels self-contained, its header parse the one home in\nLabelMap.FromHeader; and the repeat returned to a pure walk, onBlank living\non the tiler alone.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-14T17:43:35Z",
          "tree_id": "1c80a0bed7aeb311bb6e1bdb0933ac5a05562434",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/d06708df60e381758b11dde07836c674565409b6"
        },
        "date": 1789408535794,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 4402763,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 43855116,
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
          "id": "33f1afa3496afb1fabd2e0242197d1c5d9889568",
          "message": "Merge experiment/point-and-line: the Point substrate — one vocabulary over any ISpace, planes and points as the locators, the kinds in Unrect.Spreadsheets\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-16T04:05:17Z",
          "tree_id": "09d4c757d0f1502df340fb2a2481c31f78e8d8b2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/33f1afa3496afb1fabd2e0242197d1c5d9889568"
        },
        "date": 1789531991285,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 10415964,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 103983616,
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
          "id": "0f34f159151554afcc0555f7afc83fdd486b8c06",
          "message": "Merge experiment/point-follow-ups: Extents into Plane, Unrect.Interactive, and the typed-predicate lift\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-17T02:37:19Z",
          "tree_id": "cfa1d003f75555e9d6f330d316cdd949a1da2b25",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/0f34f159151554afcc0555f7afc83fdd486b8c06"
        },
        "date": 1789613139334,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 10415964,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 103983616,
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
          "id": "717d1ea3b61c226c608194aae5ff909fd4603455",
          "message": "Packaging: Unrect.Interactive ships as its own package\n\nThe project was already packable — id, description, tags, and a\ndependency on exactly Unrect and Unrect.Spreadsheets at the same MinVer\nversion — but the release workflow packed and asserted two packages, and\nthe README and build props named two. Now three.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T01:12:25Z",
          "tree_id": "f01ec5a705245aededd07d5455e7b03ad4f8880d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/717d1ea3b61c226c608194aae5ff909fd4603455"
        },
        "date": 1789796484525,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 35154589,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 348219544,
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
          "id": "4818c36f9e9b0c2b75a4093dbbce6981329fd14b",
          "message": "Merge feature/scaffold-shapes: ScaffoldRecord and ScaffoldClass, on a sheet and as leaves\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T15:35:04Z",
          "tree_id": "d48076b68bb0a93dc6ab6797b10f0ead882156a7",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4818c36f9e9b0c2b75a4093dbbce6981329fd14b"
        },
        "date": 1789847939804,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 35154491,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 348219544,
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
          "id": "bd027f2136cee944da88c1eae17a06b14a05579d",
          "message": "Merge feature/bind-from-row: Table<T> fills a member from the row - Column(member, row => ...)\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T19:49:16Z",
          "tree_id": "88c877501aeddcefb436f136842b9904cda55a09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bd027f2136cee944da88c1eae17a06b14a05579d"
        },
        "date": 1789863954796,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 35321651,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 349884304,
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
          "id": "96b946d5920955eb81be3eab46697a3e4639e2e4",
          "message": "Merge feature/header-bands: headers of several rows, columns addressed by path, flat binding over bands\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T03:17:51Z",
          "tree_id": "33ccb96a2f1d1cb7795dde522c6c586ee8ce479a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/96b946d5920955eb81be3eab46697a3e4639e2e4"
        },
        "date": 1789888953428,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_400Investors",
            "value": 38303112,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.EndToEnd.Document_4000Investors",
            "value": 379649728,
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
        "date": 1788472138305,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 3874499,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 3875513,
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
          "id": "f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb",
          "message": "Fix the 2-core CI flake: a blocked-borrower proof needs a started borrower\n\nAReachWaitsForAWarmerRatherThanStartingASecondOpenOfTheSameFile failed on\nthe GitHub runner (ef348dd) on \"the wait is counted\": WarmWaitMilliseconds\nwas 0, and 0 was the honest count. The pool's warmers ride Task.Run and\nthe gated arrangement BLOCKS them inside their opens, one pool thread\neach — on a two-core runner that is the entire starting thread pool, so\nthe test's own Task.Run borrower never started until thread injection got\naround to it. Both blocked-ness assertions passed vacuously (not finished\nbecause not scheduled), and by the time the reach ran, the warm reader was\nparked and there was nothing left to wait for.\n\nReproduced under taskset -c 0,1: three failures in four runs before the\nfix, none in six Debug runs plus a Release run after. The fix is\nOnItsOwnThread (TaskCreationOptions.LongRunning) at the four sites that\nassert a borrower is blocked — a dedicated thread starts unconditionally,\nso \"started, and still not finished\" really does mean \"parked inside\nBorrow\". The three sibling sites could only pass vacuously, never fail,\nbut their proofs were the same lie under starvation. The burst tests\nalready stood on structural evidence (SpinUntil on OpensStarted) and are\nuntouched.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T22:37:18Z",
          "tree_id": "c37ffff8e7e618f8d8cdb3778c429c1bd5259fc9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb"
        },
        "date": 1788475704417,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 3874497,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 3875513,
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
          "id": "10027e9f1d263aac70041f0f7166b186324129e8",
          "message": "Both doors measure a sheet that will not say how big it is\n\nSpreadsheetSpace.Create sized its grid from reader.RowCount/FieldCount and\nsilently yielded an empty space when the reader would not give them — the\none outcome an adapter must not have, and a divergence from the streaming\ndoor, which has measured such sheets since Part 2 step 7. The fill is now\ntwo named siblings behind one dichotomy: ReadDeclared (the original loop,\nunchanged) and ReadMeasured (rows collected at their own width, the widest\nrow wins, absent trailing cells Blank — the same answer Workbook.Measure\ngives). The guard is rowCount <= 0 alone, deliberately mirroring the\nstreaming door so the two can never disagree about the same file.\n\nThe recorded cause was wrong, and is corrected everywhere it appeared: a\nmissing dimension element does not trigger this — ExcelDataReader derives\nboth counts from a pre-scan of the cells on every format it handles. The\nreachable trigger is a sheet with NO valued cell (rows of formatted-but-\nvalueless cells, a pre-formatted export region). Pinned by the committed\nTestData/no-extent.xlsx (dimensionless AND valueless, with the survey's\nRowsMeasured == 4 doubling as the fixture's own guard against a\nregeneration that quietly stops reaching the path) and a both-doors\nidentity test.\n\nRides along, both owner decisions from this session's discussion:\n- MaxReaders: spec §14 Q2 DECIDED — 3 stays and stops being provisional,\n  because no number is right: reader demand is the declaration's monotone-\n  cursor count, unbounded in principle, data-independent in practice, and\n  the ceiling fails gently (Reopens is the counted, named signal to raise\n  it). Sizing guidance added to docs/streaming.md; per-reader economics\n  (~5s CPU per open, position must be walked, reader-per-row is O(n^2))\n  recorded in the spec.\n- Table's header-derived width: spec §14 Q1 DEFERRED, superseding the\n  2026-09-03 yes — the step-8 interleave delivered the lazy win with\n  today's denotation intact, so the K-1 campaign votes before the\n  denotation change is paid for.\n\nSuite 1,382 -> 1,387; gates green in Debug and 2-core Release.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T14:29:21Z",
          "tree_id": "fc431b0954d2e3a5115a177bd1a21d63c169ffae",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/10027e9f1d263aac70041f0f7166b186324129e8"
        },
        "date": 1788533065973,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 3988969,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 3989985,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 5872,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 4384,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 2218833,
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
          "id": "c01531cec6968e544acc578291244292172a00a5",
          "message": "Docs: Part 3 deferred on principle, and .Sized's composite role stated honestly\n\nSpec §13 gains the Part 3 row (bound-aware composite placement): the\nengine's remaining greed sorted into one necessary force (Repeat items —\nthe item's existence is the question), one free force (post-Project\nconsumption, amortised by the root's accounting), and one debt (composite\nchild placement, whose questions have lazy answers nobody asks for).\nDeferred until the first tall sized composite pays the debt — sized\ncomposites in the corpus are short header bands, where settling eagerly\ncosts nothing. The K-1 campaign is the likely judge; the census pin is the\ntripwire.\n\ndocs/streaming.md stops saying \"put the .Sized on the leaf\" as if it were\na law: a sized composite is a legitimate spelling with no leaf equivalent\n— a composite has no intrinsic extent, and the declared band is what\nscopes its internal seeks and settles its consumption.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T15:37:53Z",
          "tree_id": "6188ce68af3130bfba604f38845b0c515958cb34",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/c01531cec6968e544acc578291244292172a00a5"
        },
        "date": 1788537631609,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 3988971,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 3989987,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 5872,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 4384,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 2218833,
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
          "id": "2d73985e95c70f51a2b26d7dc98c3936f1f52d5d",
          "message": "Retention: the live-set floor for the interning change, with the target on the chart\n\nAn eighth CI leg that is not a BenchmarkDotNet family: interning reduces\nRETAINED bytes, not allocations (a duplicate string is allocated by the\nreader before the adapter sees it and dies young after dedup), so the\nAllocated column cannot see it — and retention is deterministic, so it\nneeds no statistical engine. A one-shot job measures live bytes with the\nresult held, emits the same JSON document the rig already stores, and\nrides the same workflow and dashboard as everything else.\n\nBuilding it surfaced two facts worth more than the plumbing:\n\n- The eager door's duplication depends on how the file spells its text.\n  Shared-string cells come back already deduped (the reader returns its\n  table's own instance); inline strings and formula-result cells\n  materialise fresh per cell. A real Excel export is both (the local K-1:\n  9,049 text cells, 2,876 values, 4,016 instances — the formula results\n  are the duplicated half). The family brackets it, and the shared-string\n  row is the priced TARGET: the same cells read 112.0 MB duplicated vs\n  58.2 MB deduped, so ~48% is what a complete eager interner is worth on\n  this shape — short of that is unfinished, not failed.\n- The first fixture boxed decimals a real read never produces (16 MB of\n  boxes in a retained-bytes measurement); the retention fixtures now\n  yield doubles like a reader does. StreamingSpaces is deliberately\n  untouched — changing it would re-baseline that family's history.\n\nScenarios exercise the real seams the interning change will live in: the\neager rows go through SpreadsheetSpace.Create over generated workbooks\n(RetentionWorkbooks: a minimal hand-rolled OOXML writer, no new package;\nthe one deliberate exception to the no-workbooks rule, recorded in\ndocs/benchmarking.md), the streaming rows through the store's chunk fill.\nFloor: eager space held 106.8 MB, results held 82.1 MB both doors\n(byte-identical — streaming's promise stated in the metric), controls\nbyte-identical to their duplicated twins by fixed-width padding. Leg\nruns ~65s, the shortest in the matrix.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T16:47:38Z",
          "tree_id": "0c756ae6dd2d4f17cd84e585c99d7d3ae08fd409",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/2d73985e95c70f51a2b26d7dc98c3936f1f52d5d"
        },
        "date": 1788542163597,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 3988971,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 3989987,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 5872,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 4384,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 2218833,
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
          "id": "eddc5d17c38c715f41cd95d041452deb66f8354c",
          "message": "Interning: equal text shares one instance through both doors\n\nAdapter-level string interning — a find-my-twin table at each door's\nadapt seam. The eager door threads a per-Create-call HashSet through both\nfill paths (one instance per distinct value across every sheet of the\ncall); the streaming door hangs one capped ConcurrentDictionary on the\nWorkbook, plumbed into every store's chunk fill, so a chase reader's\nre-parse dedupes against the first parse. Strings only: every other kind\nis inline in the 24-byte struct or unreachable from the spreadsheet door.\n\nThe win is retention, not allocation — the duplicate is allocated by the\nreader before the adapter sees it and dies in gen0 after dedup, which is\nwhy the Retention leg is the judge and MemoryDiagnoser is blind to it.\nMeasured against the committed floor: eager space held 106.8 -> 55.5 MB,\nlanding on the priced shared-string target to the byte; held results\n82.1 -> 30.8 MB, byte-identical across doors; all three unique controls\nflat to the byte; wall time noise on the 1M-row parse.\n\nThe cap (WorkbookOptions.MaxInternedStrings, default 65,536; 0 = off)\nand the 256-char length guard bound what the book-lifetime table can\npin. Documented as a two-way knob: a full table costs its entries for\nthe book's life — some 40 MB at the default cap — so it turns DOWN, to\n0, for known-unique text, and the docs say so at every site (the rig is\nstructurally blind to the table's own live set: readings are taken with\nthe book closed). Workbook.InterningStatistics reports hits, distinct,\nand estimated bytes (64-bit layout, exact for it; Hits counts fills, so\na reloaded chunk counts again — read against ChunkReloads).\n\nExcelDataReader fact on the record: shared-string cells arrive\npre-deduped from the reader's own SST; the duplication this kills comes\nfrom inline-string cells, formula-result cells, and .xls. Pinned by 36\ntests including a cross-door sharing differential, mutation-checked both\ndirections, and a WeakReference proof that Dispose releases the strings.\nSuite 1,423.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T19:19:56Z",
          "tree_id": "fea5150f427d5b65cf652662879afb3b470547f2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/eddc5d17c38c715f41cd95d041452deb66f8354c"
        },
        "date": 1788556081024,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 3988971,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 3989987,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 5872,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 4384,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 2218833,
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
          "id": "16fef6aa0fb6978c79559b92910716e6ad44c995",
          "message": "netstandard2.0: the packages install on .NET Framework, one source, no forks\n\nAll four libraries multi-target netstandard2.0;netstandard2.1. The rule\nthat made it clean: no #if anywhere — netstandard2.1-only constructs are\nremoved rather than branched on, so both targets compile one source and\ncannot drift.\n\nThe structural change is de-DIMing the incremental calculus. .NET\nFramework's runtime cannot dispatch a default interface member, so the\ndefinitional folds move from DIM bodies to the static Unrect.Core.Scans\n(Fold/FoldSize/FoldArea) and internal ColumnAccumulators.Fold, with every\nimplementation delegating in one line. The guarantee shifts and every doc\nthat claimed it says so honestly: \"eager and lazy cannot disagree by\nconstruction\" is now \"cannot disagree, pinned by the fold-identity suite\"\n— IncrementalStrategyTests asserts SelectRows == a hand-written fold for\nevery factory, and a new incremental strategy carries the obligation to\njoin that theory data. Corollary recorded in capability-seam-notes: the\nDIM \"safety net\" for evolving ISpace is withdrawn — a member added to a\npublished Core interface is now a breaking change with no escape hatch;\ntype-testing is the whole capability recipe.\n\nThe rest the compiler found: System.HashCode (Core hand-rolls its combine\nrather than take Microsoft.Bcl.HashCode — the bundling would have\nsuppressed the dependency from the nuspec and thrown FileNotFoundException\non Framework; both packages stay at zero new dependencies), the\nDictionary-from-pairs constructor, and HashSet<string>.TryGetValue (the\neager intern table is a Dictionary now; the comment that argued HashSet\nwas the honest structure records why it changed). The two polyfills are\nunconditional because neither netstandard has the types; a net5.0+ target\nis what would force an #if.\n\nTests multi-target net8.0;net48 on Windows only (OS-conditioned csproj,\nso Linux CI runs exactly what it ran — verified against all three\nworkflows). Two portable shims replace .NET-5+/6+ APIs the suite used:\nWithTimeout for Task.WaitAsync(TimeSpan), ByReference for\nReferenceEqualityComparer. The full suite has run green on the Framework\nCLR itself: 1,423 on net48 against the netstandard2.0 build, ExcelDataReader's\nown Framework assembly included — verified by the owner on Windows.\nBoth nupkgs verified to carry lib/netstandard2.0 and lib/netstandard2.1.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-05T17:36:21Z",
          "tree_id": "67b64bc3184652344901c8a243f8ce1256d858b9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16fef6aa0fb6978c79559b92910716e6ad44c995"
        },
        "date": 1788630325591,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 3988971,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 3989987,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 5872,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 4384,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 2218833,
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
          "id": "e1d21c2d706b054d252bbea3a996b97f6f7bfc4b",
          "message": "Hygiene: .gitattributes and editorconfig trim — line endings become a decision\n\nThe repo had no .gitattributes, so line endings were whatever tool\ntouched a file last — four .linq files sat CRLF in the index beside an\nLF corpus, and the vocabulary respell nearly flipped three of them\nwholesale (caught in review; a 10-line diff had become 366). One rule\nnow: the index holds LF (* text=auto); .linq and .sln check out CRLF\nbecause their native editors are Windows-bound; spreadsheets and images\nare explicitly binary. This commit carries the one-time renormalization\nso the flip lives here, deliberately, and nowhere else.\n\n.editorconfig gains trailing-whitespace trimming, with markdown exempt\n(a trailing double-space is a hard line break).\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-06T00:49:13Z",
          "tree_id": "ce25b3593543c4145d69070ff009c9f67ff1aa09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/e1d21c2d706b054d252bbea3a996b97f6f7bfc4b"
        },
        "date": 1788712514921,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 3988969,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 3989985,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 5872,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 4384,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ShapeException_Render",
            "value": 2218833,
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
          "id": "bf1fc2851544d9f87ffb3b76df4fa0b714780243",
          "message": "Merge experiment/typed-spaces: the projection model\n\nThe deepest cut since wave 2: what was called Shape is a projection; the\nspace and its subspaces are the shapes. Seven phases plus pre-merge\ncleanup (docs/design/projection-model-spec.md, judgment record in §10):\nthe modifier doubling killed (43 -> 6 irreducible), IShape -> IProjection\nwith namespace Unrect.Projections, the capability stack (IFormulaSpace,\nCapability<T>() transport, the slicing law, shared-formula\nreconstruction), OrBlank and the eachRow slot, the CaptionMap bind, the\nscoped entry Over<TSpace>() and MapWorkbook, phase-7 acceptance with the\nfourteen recorded refusals, and the three-sweep cleanup.\n\nSuite 1,709 green (net8.0 and net48); both library TFMs 0 warnings.\nBreaking: the rename, the Table family, the SpreadsheetSpace shell\nretirement, and the entries.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-07T23:11:42Z",
          "tree_id": "d091a9f7437fb1ec8d2b35119ab628b2a54047a3",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bf1fc2851544d9f87ffb3b76df4fa0b714780243"
        },
        "date": 1788823323618,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 4046643,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 4047667,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 5656,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 4384,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 2247705,
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
          "id": "4a44b787ac6b8b6fa5c487623e316bfe0e364292",
          "message": "Presence semantics and three hygiene fixes: the zero disambiguated\n\nThe algebra's highest-priority open problem (v0.4 §7/§14.1): geometric\nzero carried four meanings. The internal Presence { Read, Empty,\nAbsorbed } now rides beside Consumed on the engine's results — stamped\nat tolerance boundaries (Absorbed), settled-at-zero extents (Empty, read\noff the SETTLED extent so evaluation order and doors agree by\nconstruction), joined through layouts (Read if any child Read, else\nEmpty), defaulting Read. The internal epsilon (NothingProjection) makes\nthe flow-unit law stateable: VerticalFlow is a monoid at L3 for\nsuccessful readings, boundaries pinned.\n\nThe law, earned the hard way: presence explains a stop; the extent\ndecides one. The first implementation let presence decide the repeat\nguard; the law-tests caught a real L1/L2 change on\nVerticalRepeat(item.Optional().Sized(...)) — an Absorbed boundary under\na declared area legitimately consumes it and always kept the run going.\nThe guard is byte-identical to before; presence powers only the new\nteaching Info when a run ends at an absorbed item (D2), and the caught\nshape is pinned as the compatibility-law section of PresenceLawTests.\n\nDesign record: docs/design/presence-and-unit-spec.md (DECIDED, with the\nD5 amendment trail).\n\nAlso, with pins: ChoiceProjection.Summarise renders nested aggregates\ndepth-indented with one location per line; MinOffset() is one canonical\ninstance (HasDeclaredOffset correct by construction); the content-\nmatching rule reduced to one implementation (CellMatching primitives,\nTableView's dictionary keyed on TextComparer; CaptionComparer's\ndeliberate divergence untouched). spike/Phase1Probe removed (a husk).\n\nSuite 1,820 -> 1,862 green; both library TFMs 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-09T15:34:18Z",
          "tree_id": "883452644caa55f06a0f9c2d530ee9c0cece2b92",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4a44b787ac6b8b6fa5c487623e316bfe0e364292"
        },
        "date": 1788969083440,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 4046641,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 4047665,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 5872,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 4384,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 2247705,
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
          "id": "d06708df60e381758b11dde07836c674565409b6",
          "message": "Merge experiment/record-primitive: streaming composites over a bound, the band tiler, Table composed from primitives\n\nThe labels-as-context primitives (ColumnLabels, WithColumnLabels, Record),\nSkipToFirstNonBlankCell, and the uniform offset law; the engine streaming a\ndiscovered bound through a composite (TailSpace, a bound-aware Exceeds, lazy\nflow and repeat slices); VerticalBands/HorizontalBands, the tiler — a\nrepeating fixed-dimension space, distinct from the pattern repeat;\nTable(headerRows, eachRow) composed over the tiler under a UnitProjection,\nwith AsScaffolding and a mark-driven path fold giving it the leaf's own\ndiagnostics; ColumnLabels self-contained, its header parse the one home in\nLabelMap.FromHeader; and the repeat returned to a pure walk, onBlank living\non the tiler alone.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-14T17:43:35Z",
          "tree_id": "1c80a0bed7aeb311bb6e1bdb0933ac5a05562434",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/d06708df60e381758b11dde07836c674565409b6"
        },
        "date": 1789408536065,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 4402763,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 4404059,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 6728,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 5120,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 2428921,
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
          "id": "33f1afa3496afb1fabd2e0242197d1c5d9889568",
          "message": "Merge experiment/point-and-line: the Point substrate — one vocabulary over any ISpace, planes and points as the locators, the kinds in Unrect.Spreadsheets\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-16T04:05:17Z",
          "tree_id": "09d4c757d0f1502df340fb2a2481c31f78e8d8b2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/33f1afa3496afb1fabd2e0242197d1c5d9889568"
        },
        "date": 1789531991721,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 10415964,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 10417148,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 6136,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 5664,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 5770406,
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
          "id": "0f34f159151554afcc0555f7afc83fdd486b8c06",
          "message": "Merge experiment/point-follow-ups: Extents into Plane, Unrect.Interactive, and the typed-predicate lift\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-17T02:37:19Z",
          "tree_id": "cfa1d003f75555e9d6f330d316cdd949a1da2b25",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/0f34f159151554afcc0555f7afc83fdd486b8c06"
        },
        "date": 1789613139610,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 10415964,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 10417148,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 6136,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 5664,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 5770406,
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
          "id": "717d1ea3b61c226c608194aae5ff909fd4603455",
          "message": "Packaging: Unrect.Interactive ships as its own package\n\nThe project was already packable — id, description, tags, and a\ndependency on exactly Unrect and Unrect.Spreadsheets at the same MinVer\nversion — but the release workflow packed and asserted two packages, and\nthe README and build props named two. Now three.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T01:12:25Z",
          "tree_id": "f01ec5a705245aededd07d5455e7b03ad4f8880d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/717d1ea3b61c226c608194aae5ff909fd4603455"
        },
        "date": 1789796485400,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 35154579,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 35155763,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 1634544,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 1976256,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 20195536,
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
          "id": "4818c36f9e9b0c2b75a4093dbbce6981329fd14b",
          "message": "Merge feature/scaffold-shapes: ScaffoldRecord and ScaffoldClass, on a sheet and as leaves\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T15:35:04Z",
          "tree_id": "d48076b68bb0a93dc6ab6797b10f0ead882156a7",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4818c36f9e9b0c2b75a4093dbbce6981329fd14b"
        },
        "date": 1789847940050,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 35154528,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 35155712,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 1635128,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 1976624,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 20195597,
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
          "id": "bd027f2136cee944da88c1eae17a06b14a05579d",
          "message": "Merge feature/bind-from-row: Table<T> fills a member from the row - Column(member, row => ...)\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T19:49:16Z",
          "tree_id": "88c877501aeddcefb436f136842b9904cda55a09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bd027f2136cee944da88c1eae17a06b14a05579d"
        },
        "date": 1789863955075,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 35321786,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 35322872,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 1635272,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 1976696,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 20279485,
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
          "id": "96b946d5920955eb81be3eab46697a3e4639e2e4",
          "message": "Merge feature/header-bands: headers of several rows, columns addressed by path, flat binding over bands\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T03:17:51Z",
          "tree_id": "33ccb96a2f1d1cb7795dde522c6c586ee8ce479a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/96b946d5920955eb81be3eab46697a3e4639e2e4"
        },
        "date": 1789888953697,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_Plain",
            "value": 38303163,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Map_WithDiagnostics",
            "value": 38304394,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Choice_FirstAlternativeLoses",
            "value": 1634904,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.Optional_AbsorbsFailure",
            "value": 1976328,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Diagnostics.ProjectionException_Render",
            "value": 21772848,
            "unit": "bytes"
          }
        ]
      }
    ],
    "Streaming Benchmarks": [
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
        "date": 1788472137332,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 285846820.78571427,
            "unit": "ns",
            "range": "± 3230105.045182043"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 537893711.1428572,
            "unit": "ns",
            "range": "± 5165152.032152316"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 323716538.43333334,
            "unit": "ns",
            "range": "± 5085490.236258125"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 24952580.339583334,
            "unit": "ns",
            "range": "± 140229.79545139958"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 99724561.68888889,
            "unit": "ns",
            "range": "± 962300.7082999676"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 51122441.93333333,
            "unit": "ns",
            "range": "± 1509860.8392303558"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 29359144.879464287,
            "unit": "ns",
            "range": "± 114449.57846926326"
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
          "id": "f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb",
          "message": "Fix the 2-core CI flake: a blocked-borrower proof needs a started borrower\n\nAReachWaitsForAWarmerRatherThanStartingASecondOpenOfTheSameFile failed on\nthe GitHub runner (ef348dd) on \"the wait is counted\": WarmWaitMilliseconds\nwas 0, and 0 was the honest count. The pool's warmers ride Task.Run and\nthe gated arrangement BLOCKS them inside their opens, one pool thread\neach — on a two-core runner that is the entire starting thread pool, so\nthe test's own Task.Run borrower never started until thread injection got\naround to it. Both blocked-ness assertions passed vacuously (not finished\nbecause not scheduled), and by the time the reach ran, the warm reader was\nparked and there was nothing left to wait for.\n\nReproduced under taskset -c 0,1: three failures in four runs before the\nfix, none in six Debug runs plus a Release run after. The fix is\nOnItsOwnThread (TaskCreationOptions.LongRunning) at the four sites that\nassert a borrower is blocked — a dedicated thread starts unconditionally,\nso \"started, and still not finished\" really does mean \"parked inside\nBorrow\". The three sibling sites could only pass vacuously, never fail,\nbut their proofs were the same lie under starvation. The burst tests\nalready stood on structural evidence (SpinUntil on OpensStarted) and are\nuntouched.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T22:37:18Z",
          "tree_id": "c37ffff8e7e618f8d8cdb3778c429c1bd5259fc9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb"
        },
        "date": 1788475703459,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 213776955.51111114,
            "unit": "ns",
            "range": "± 2217010.837508953"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 423109910.3333333,
            "unit": "ns",
            "range": "± 4091406.9118655203"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 261941149,
            "unit": "ns",
            "range": "± 4353117.845638205"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 19117479.34151786,
            "unit": "ns",
            "range": "± 170132.06134401675"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 75441393.6095238,
            "unit": "ns",
            "range": "± 430666.31091986864"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 39187873.507692315,
            "unit": "ns",
            "range": "± 429904.13092018984"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 24757876.40401786,
            "unit": "ns",
            "range": "± 192957.53647964072"
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
          "id": "10027e9f1d263aac70041f0f7166b186324129e8",
          "message": "Both doors measure a sheet that will not say how big it is\n\nSpreadsheetSpace.Create sized its grid from reader.RowCount/FieldCount and\nsilently yielded an empty space when the reader would not give them — the\none outcome an adapter must not have, and a divergence from the streaming\ndoor, which has measured such sheets since Part 2 step 7. The fill is now\ntwo named siblings behind one dichotomy: ReadDeclared (the original loop,\nunchanged) and ReadMeasured (rows collected at their own width, the widest\nrow wins, absent trailing cells Blank — the same answer Workbook.Measure\ngives). The guard is rowCount <= 0 alone, deliberately mirroring the\nstreaming door so the two can never disagree about the same file.\n\nThe recorded cause was wrong, and is corrected everywhere it appeared: a\nmissing dimension element does not trigger this — ExcelDataReader derives\nboth counts from a pre-scan of the cells on every format it handles. The\nreachable trigger is a sheet with NO valued cell (rows of formatted-but-\nvalueless cells, a pre-formatted export region). Pinned by the committed\nTestData/no-extent.xlsx (dimensionless AND valueless, with the survey's\nRowsMeasured == 4 doubling as the fixture's own guard against a\nregeneration that quietly stops reaching the path) and a both-doors\nidentity test.\n\nRides along, both owner decisions from this session's discussion:\n- MaxReaders: spec §14 Q2 DECIDED — 3 stays and stops being provisional,\n  because no number is right: reader demand is the declaration's monotone-\n  cursor count, unbounded in principle, data-independent in practice, and\n  the ceiling fails gently (Reopens is the counted, named signal to raise\n  it). Sizing guidance added to docs/streaming.md; per-reader economics\n  (~5s CPU per open, position must be walked, reader-per-row is O(n^2))\n  recorded in the spec.\n- Table's header-derived width: spec §14 Q1 DEFERRED, superseding the\n  2026-09-03 yes — the step-8 interleave delivered the lazy win with\n  today's denotation intact, so the K-1 campaign votes before the\n  denotation change is paid for.\n\nSuite 1,382 -> 1,387; gates green in Debug and 2-core Release.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T14:29:21Z",
          "tree_id": "fc431b0954d2e3a5115a177bd1a21d63c169ffae",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/10027e9f1d263aac70041f0f7166b186324129e8"
        },
        "date": 1788533064745,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 190833901.73809522,
            "unit": "ns",
            "range": "± 2061518.3291903043"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 396593282.6666667,
            "unit": "ns",
            "range": "± 1490566.6728120754"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 235223520.7101449,
            "unit": "ns",
            "range": "± 5895234.854217428"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 25392001.747916665,
            "unit": "ns",
            "range": "± 247090.39390361233"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 97489311.16,
            "unit": "ns",
            "range": "± 1042721.6018692746"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 40451267.86666667,
            "unit": "ns",
            "range": "± 266183.22004805406"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 29483898.285714287,
            "unit": "ns",
            "range": "± 173900.40256954954"
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
          "id": "c01531cec6968e544acc578291244292172a00a5",
          "message": "Docs: Part 3 deferred on principle, and .Sized's composite role stated honestly\n\nSpec §13 gains the Part 3 row (bound-aware composite placement): the\nengine's remaining greed sorted into one necessary force (Repeat items —\nthe item's existence is the question), one free force (post-Project\nconsumption, amortised by the root's accounting), and one debt (composite\nchild placement, whose questions have lazy answers nobody asks for).\nDeferred until the first tall sized composite pays the debt — sized\ncomposites in the corpus are short header bands, where settling eagerly\ncosts nothing. The K-1 campaign is the likely judge; the census pin is the\ntripwire.\n\ndocs/streaming.md stops saying \"put the .Sized on the leaf\" as if it were\na law: a sized composite is a legitimate spelling with no leaf equivalent\n— a composite has no intrinsic extent, and the declared band is what\nscopes its internal seeks and settles its consumption.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T15:37:53Z",
          "tree_id": "6188ce68af3130bfba604f38845b0c515958cb34",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/c01531cec6968e544acc578291244292172a00a5"
        },
        "date": 1788537630263,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 157307666.66666666,
            "unit": "ns",
            "range": "± 872347.6608323057"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 331965883.85714287,
            "unit": "ns",
            "range": "± 1681294.1872474132"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 183899048.45238096,
            "unit": "ns",
            "range": "± 592650.2735428825"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 18953871.879166666,
            "unit": "ns",
            "range": "± 73474.78996801563"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 74378363.84693877,
            "unit": "ns",
            "range": "± 221477.2232040874"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 34912638.75072464,
            "unit": "ns",
            "range": "± 865010.2956682411"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 22131395.792410713,
            "unit": "ns",
            "range": "± 150486.9562344988"
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
          "id": "2d73985e95c70f51a2b26d7dc98c3936f1f52d5d",
          "message": "Retention: the live-set floor for the interning change, with the target on the chart\n\nAn eighth CI leg that is not a BenchmarkDotNet family: interning reduces\nRETAINED bytes, not allocations (a duplicate string is allocated by the\nreader before the adapter sees it and dies young after dedup), so the\nAllocated column cannot see it — and retention is deterministic, so it\nneeds no statistical engine. A one-shot job measures live bytes with the\nresult held, emits the same JSON document the rig already stores, and\nrides the same workflow and dashboard as everything else.\n\nBuilding it surfaced two facts worth more than the plumbing:\n\n- The eager door's duplication depends on how the file spells its text.\n  Shared-string cells come back already deduped (the reader returns its\n  table's own instance); inline strings and formula-result cells\n  materialise fresh per cell. A real Excel export is both (the local K-1:\n  9,049 text cells, 2,876 values, 4,016 instances — the formula results\n  are the duplicated half). The family brackets it, and the shared-string\n  row is the priced TARGET: the same cells read 112.0 MB duplicated vs\n  58.2 MB deduped, so ~48% is what a complete eager interner is worth on\n  this shape — short of that is unfinished, not failed.\n- The first fixture boxed decimals a real read never produces (16 MB of\n  boxes in a retained-bytes measurement); the retention fixtures now\n  yield doubles like a reader does. StreamingSpaces is deliberately\n  untouched — changing it would re-baseline that family's history.\n\nScenarios exercise the real seams the interning change will live in: the\neager rows go through SpreadsheetSpace.Create over generated workbooks\n(RetentionWorkbooks: a minimal hand-rolled OOXML writer, no new package;\nthe one deliberate exception to the no-workbooks rule, recorded in\ndocs/benchmarking.md), the streaming rows through the store's chunk fill.\nFloor: eager space held 106.8 MB, results held 82.1 MB both doors\n(byte-identical — streaming's promise stated in the metric), controls\nbyte-identical to their duplicated twins by fixed-width padding. Leg\nruns ~65s, the shortest in the matrix.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T16:47:38Z",
          "tree_id": "0c756ae6dd2d4f17cd84e585c99d7d3ae08fd409",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/2d73985e95c70f51a2b26d7dc98c3936f1f52d5d"
        },
        "date": 1788542161644,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 217110489.04761907,
            "unit": "ns",
            "range": "± 3356800.5113136987"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 333108877.9166667,
            "unit": "ns",
            "range": "± 4625929.291573266"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 227017454.46666664,
            "unit": "ns",
            "range": "± 6002438.2770817755"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 17457840.012946427,
            "unit": "ns",
            "range": "± 555924.8715912908"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 61695795.08928572,
            "unit": "ns",
            "range": "± 932767.2474546522"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 40324197.650000006,
            "unit": "ns",
            "range": "± 920505.8925675354"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 25603540.457291666,
            "unit": "ns",
            "range": "± 453060.36343471956"
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
          "id": "eddc5d17c38c715f41cd95d041452deb66f8354c",
          "message": "Interning: equal text shares one instance through both doors\n\nAdapter-level string interning — a find-my-twin table at each door's\nadapt seam. The eager door threads a per-Create-call HashSet through both\nfill paths (one instance per distinct value across every sheet of the\ncall); the streaming door hangs one capped ConcurrentDictionary on the\nWorkbook, plumbed into every store's chunk fill, so a chase reader's\nre-parse dedupes against the first parse. Strings only: every other kind\nis inline in the 24-byte struct or unreachable from the spreadsheet door.\n\nThe win is retention, not allocation — the duplicate is allocated by the\nreader before the adapter sees it and dies in gen0 after dedup, which is\nwhy the Retention leg is the judge and MemoryDiagnoser is blind to it.\nMeasured against the committed floor: eager space held 106.8 -> 55.5 MB,\nlanding on the priced shared-string target to the byte; held results\n82.1 -> 30.8 MB, byte-identical across doors; all three unique controls\nflat to the byte; wall time noise on the 1M-row parse.\n\nThe cap (WorkbookOptions.MaxInternedStrings, default 65,536; 0 = off)\nand the 256-char length guard bound what the book-lifetime table can\npin. Documented as a two-way knob: a full table costs its entries for\nthe book's life — some 40 MB at the default cap — so it turns DOWN, to\n0, for known-unique text, and the docs say so at every site (the rig is\nstructurally blind to the table's own live set: readings are taken with\nthe book closed). Workbook.InterningStatistics reports hits, distinct,\nand estimated bytes (64-bit layout, exact for it; Hits counts fills, so\na reloaded chunk counts again — read against ChunkReloads).\n\nExcelDataReader fact on the record: shared-string cells arrive\npre-deduped from the reader's own SST; the duplication this kills comes\nfrom inline-string cells, formula-result cells, and .xls. Pinned by 36\ntests including a cross-door sharing differential, mutation-checked both\ndirections, and a WeakReference proof that Dispose releases the strings.\nSuite 1,423.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T19:19:56Z",
          "tree_id": "fea5150f427d5b65cf652662879afb3b470547f2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/eddc5d17c38c715f41cd95d041452deb66f8354c"
        },
        "date": 1788556079915,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 190235245.47619042,
            "unit": "ns",
            "range": "± 3321098.113671084"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 480989039.06666666,
            "unit": "ns",
            "range": "± 2318323.441470195"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 222380676.09523812,
            "unit": "ns",
            "range": "± 1468841.9164388566"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 49865826.468,
            "unit": "ns",
            "range": "± 1306128.9483046003"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 145703872.7638889,
            "unit": "ns",
            "range": "± 2942412.0648582065"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 48597693.23030303,
            "unit": "ns",
            "range": "± 276577.49275127955"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 29713619.883333333,
            "unit": "ns",
            "range": "± 157313.6106998786"
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
          "id": "16fef6aa0fb6978c79559b92910716e6ad44c995",
          "message": "netstandard2.0: the packages install on .NET Framework, one source, no forks\n\nAll four libraries multi-target netstandard2.0;netstandard2.1. The rule\nthat made it clean: no #if anywhere — netstandard2.1-only constructs are\nremoved rather than branched on, so both targets compile one source and\ncannot drift.\n\nThe structural change is de-DIMing the incremental calculus. .NET\nFramework's runtime cannot dispatch a default interface member, so the\ndefinitional folds move from DIM bodies to the static Unrect.Core.Scans\n(Fold/FoldSize/FoldArea) and internal ColumnAccumulators.Fold, with every\nimplementation delegating in one line. The guarantee shifts and every doc\nthat claimed it says so honestly: \"eager and lazy cannot disagree by\nconstruction\" is now \"cannot disagree, pinned by the fold-identity suite\"\n— IncrementalStrategyTests asserts SelectRows == a hand-written fold for\nevery factory, and a new incremental strategy carries the obligation to\njoin that theory data. Corollary recorded in capability-seam-notes: the\nDIM \"safety net\" for evolving ISpace is withdrawn — a member added to a\npublished Core interface is now a breaking change with no escape hatch;\ntype-testing is the whole capability recipe.\n\nThe rest the compiler found: System.HashCode (Core hand-rolls its combine\nrather than take Microsoft.Bcl.HashCode — the bundling would have\nsuppressed the dependency from the nuspec and thrown FileNotFoundException\non Framework; both packages stay at zero new dependencies), the\nDictionary-from-pairs constructor, and HashSet<string>.TryGetValue (the\neager intern table is a Dictionary now; the comment that argued HashSet\nwas the honest structure records why it changed). The two polyfills are\nunconditional because neither netstandard has the types; a net5.0+ target\nis what would force an #if.\n\nTests multi-target net8.0;net48 on Windows only (OS-conditioned csproj,\nso Linux CI runs exactly what it ran — verified against all three\nworkflows). Two portable shims replace .NET-5+/6+ APIs the suite used:\nWithTimeout for Task.WaitAsync(TimeSpan), ByReference for\nReferenceEqualityComparer. The full suite has run green on the Framework\nCLR itself: 1,423 on net48 against the netstandard2.0 build, ExcelDataReader's\nown Framework assembly included — verified by the owner on Windows.\nBoth nupkgs verified to carry lib/netstandard2.0 and lib/netstandard2.1.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-05T17:36:21Z",
          "tree_id": "67b64bc3184652344901c8a243f8ce1256d858b9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16fef6aa0fb6978c79559b92910716e6ad44c995"
        },
        "date": 1788630324467,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 207226431.02020204,
            "unit": "ns",
            "range": "± 6436823.008883498"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 394482194.7164179,
            "unit": "ns",
            "range": "± 18553578.533091925"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 214136858.20772952,
            "unit": "ns",
            "range": "± 10259830.9482446"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 43371447.79296875,
            "unit": "ns",
            "range": "± 2493487.8979083304"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 103083811.71428572,
            "unit": "ns",
            "range": "± 2924820.7519416776"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 32979033.161458332,
            "unit": "ns",
            "range": "± 265796.6169957925"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 24246960.40625,
            "unit": "ns",
            "range": "± 99379.58201128425"
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
          "id": "e1d21c2d706b054d252bbea3a996b97f6f7bfc4b",
          "message": "Hygiene: .gitattributes and editorconfig trim — line endings become a decision\n\nThe repo had no .gitattributes, so line endings were whatever tool\ntouched a file last — four .linq files sat CRLF in the index beside an\nLF corpus, and the vocabulary respell nearly flipped three of them\nwholesale (caught in review; a 10-line diff had become 366). One rule\nnow: the index holds LF (* text=auto); .linq and .sln check out CRLF\nbecause their native editors are Windows-bound; spreadsheets and images\nare explicitly binary. This commit carries the one-time renormalization\nso the flip lives here, deliberately, and nowhere else.\n\n.editorconfig gains trailing-whitespace trimming, with markdown exempt\n(a trailing double-space is a hard line break).\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-06T00:49:13Z",
          "tree_id": "ce25b3593543c4145d69070ff009c9f67ff1aa09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/e1d21c2d706b054d252bbea3a996b97f6f7bfc4b"
        },
        "date": 1788712513762,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 193964550.57142857,
            "unit": "ns",
            "range": "± 1823184.1439342052"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 480308408.84615386,
            "unit": "ns",
            "range": "± 2087732.7158037513"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 226143548.25641027,
            "unit": "ns",
            "range": "± 1885443.1904751938"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 47783746.53939393,
            "unit": "ns",
            "range": "± 540542.9649677888"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 140741647.21666667,
            "unit": "ns",
            "range": "± 644019.6196123197"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 43716550.50555555,
            "unit": "ns",
            "range": "± 259372.55665886353"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 32281304.279166665,
            "unit": "ns",
            "range": "± 97576.45365571059"
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
          "id": "bf1fc2851544d9f87ffb3b76df4fa0b714780243",
          "message": "Merge experiment/typed-spaces: the projection model\n\nThe deepest cut since wave 2: what was called Shape is a projection; the\nspace and its subspaces are the shapes. Seven phases plus pre-merge\ncleanup (docs/design/projection-model-spec.md, judgment record in §10):\nthe modifier doubling killed (43 -> 6 irreducible), IShape -> IProjection\nwith namespace Unrect.Projections, the capability stack (IFormulaSpace,\nCapability<T>() transport, the slicing law, shared-formula\nreconstruction), OrBlank and the eachRow slot, the CaptionMap bind, the\nscoped entry Over<TSpace>() and MapWorkbook, phase-7 acceptance with the\nfourteen recorded refusals, and the three-sweep cleanup.\n\nSuite 1,709 green (net8.0 and net48); both library TFMs 0 warnings.\nBreaking: the rename, the Table family, the SpreadsheetSpace shell\nretirement, and the entries.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-07T23:11:42Z",
          "tree_id": "d091a9f7437fb1ec8d2b35119ab628b2a54047a3",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bf1fc2851544d9f87ffb3b76df4fa0b714780243"
        },
        "date": 1788823322255,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 192820076.5,
            "unit": "ns",
            "range": "± 3731391.0285080313"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 487208980.2,
            "unit": "ns",
            "range": "± 8920836.053261818"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 224636354.06666666,
            "unit": "ns",
            "range": "± 2495595.559326621"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 47066246.5090909,
            "unit": "ns",
            "range": "± 619979.78705694"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 147015886.89772728,
            "unit": "ns",
            "range": "± 3525943.694857812"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 54077176.44666665,
            "unit": "ns",
            "range": "± 919457.2724962889"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 29628847.777083334,
            "unit": "ns",
            "range": "± 162355.24621556172"
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
          "id": "4a44b787ac6b8b6fa5c487623e316bfe0e364292",
          "message": "Presence semantics and three hygiene fixes: the zero disambiguated\n\nThe algebra's highest-priority open problem (v0.4 §7/§14.1): geometric\nzero carried four meanings. The internal Presence { Read, Empty,\nAbsorbed } now rides beside Consumed on the engine's results — stamped\nat tolerance boundaries (Absorbed), settled-at-zero extents (Empty, read\noff the SETTLED extent so evaluation order and doors agree by\nconstruction), joined through layouts (Read if any child Read, else\nEmpty), defaulting Read. The internal epsilon (NothingProjection) makes\nthe flow-unit law stateable: VerticalFlow is a monoid at L3 for\nsuccessful readings, boundaries pinned.\n\nThe law, earned the hard way: presence explains a stop; the extent\ndecides one. The first implementation let presence decide the repeat\nguard; the law-tests caught a real L1/L2 change on\nVerticalRepeat(item.Optional().Sized(...)) — an Absorbed boundary under\na declared area legitimately consumes it and always kept the run going.\nThe guard is byte-identical to before; presence powers only the new\nteaching Info when a run ends at an absorbed item (D2), and the caught\nshape is pinned as the compatibility-law section of PresenceLawTests.\n\nDesign record: docs/design/presence-and-unit-spec.md (DECIDED, with the\nD5 amendment trail).\n\nAlso, with pins: ChoiceProjection.Summarise renders nested aggregates\ndepth-indented with one location per line; MinOffset() is one canonical\ninstance (HasDeclaredOffset correct by construction); the content-\nmatching rule reduced to one implementation (CellMatching primitives,\nTableView's dictionary keyed on TextComparer; CaptionComparer's\ndeliberate divergence untouched). spike/Phase1Probe removed (a husk).\n\nSuite 1,820 -> 1,862 green; both library TFMs 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-09T15:34:18Z",
          "tree_id": "883452644caa55f06a0f9c2d530ee9c0cece2b92",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4a44b787ac6b8b6fa5c487623e316bfe0e364292"
        },
        "date": 1788969081862,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 192030600.33333334,
            "unit": "ns",
            "range": "± 3528392.372001955"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 471817003.73333335,
            "unit": "ns",
            "range": "± 6036952.9888978815"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 220306560.2,
            "unit": "ns",
            "range": "± 1338058.0195536688"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 46143849.55244755,
            "unit": "ns",
            "range": "± 262706.45358144457"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 139632413.0357143,
            "unit": "ns",
            "range": "± 595770.8072643069"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 53068574.741666675,
            "unit": "ns",
            "range": "± 167240.47539898677"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 29488005.035416666,
            "unit": "ns",
            "range": "± 206801.27075713742"
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
          "id": "d06708df60e381758b11dde07836c674565409b6",
          "message": "Merge experiment/record-primitive: streaming composites over a bound, the band tiler, Table composed from primitives\n\nThe labels-as-context primitives (ColumnLabels, WithColumnLabels, Record),\nSkipToFirstNonBlankCell, and the uniform offset law; the engine streaming a\ndiscovered bound through a composite (TailSpace, a bound-aware Exceeds, lazy\nflow and repeat slices); VerticalBands/HorizontalBands, the tiler — a\nrepeating fixed-dimension space, distinct from the pattern repeat;\nTable(headerRows, eachRow) composed over the tiler under a UnitProjection,\nwith AsScaffolding and a mark-driven path fold giving it the leaf's own\ndiagnostics; ColumnLabels self-contained, its header parse the one home in\nLabelMap.FromHeader; and the repeat returned to a pure walk, onBlank living\non the tiler alone.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-14T17:43:35Z",
          "tree_id": "1c80a0bed7aeb311bb6e1bdb0933ac5a05562434",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/d06708df60e381758b11dde07836c674565409b6"
        },
        "date": 1789408534394,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 114736629.73750003,
            "unit": "ns",
            "range": "± 2125166.243762924"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 306009523.2941176,
            "unit": "ns",
            "range": "± 4029717.2855553795"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 130115246.78571428,
            "unit": "ns",
            "range": "± 2077392.9884077613"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 32011456.026785713,
            "unit": "ns",
            "range": "± 210361.69569539026"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 88124196.88690475,
            "unit": "ns",
            "range": "± 2448625.4662407245"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 30419158.47767857,
            "unit": "ns",
            "range": "± 173980.75329019254"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 20660196.16294643,
            "unit": "ns",
            "range": "± 248972.99196458972"
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
          "id": "33f1afa3496afb1fabd2e0242197d1c5d9889568",
          "message": "Merge experiment/point-and-line: the Point substrate — one vocabulary over any ISpace, planes and points as the locators, the kinds in Unrect.Spreadsheets\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-16T04:05:17Z",
          "tree_id": "09d4c757d0f1502df340fb2a2481c31f78e8d8b2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/33f1afa3496afb1fabd2e0242197d1c5d9889568"
        },
        "date": 1789531989721,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 729807003.8461539,
            "unit": "ns",
            "range": "± 2145901.390714589"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 1076501039.4,
            "unit": "ns",
            "range": "± 13032350.365095206"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 777271187.6428572,
            "unit": "ns",
            "range": "± 5997873.0897587305"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 1373404.1389973958,
            "unit": "ns",
            "range": "± 588.6222516130623"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 1376850.4829799107,
            "unit": "ns",
            "range": "± 3148.3702542888645"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 47962908.69930069,
            "unit": "ns",
            "range": "± 100471.65663237499"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 37497726.44761905,
            "unit": "ns",
            "range": "± 184464.82905850536"
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
          "id": "0f34f159151554afcc0555f7afc83fdd486b8c06",
          "message": "Merge experiment/point-follow-ups: Extents into Plane, Unrect.Interactive, and the typed-predicate lift\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-17T02:37:19Z",
          "tree_id": "cfa1d003f75555e9d6f330d316cdd949a1da2b25",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/0f34f159151554afcc0555f7afc83fdd486b8c06"
        },
        "date": 1789613137992,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 739704924.2142857,
            "unit": "ns",
            "range": "± 6518791.67728592"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 1095946026.8,
            "unit": "ns",
            "range": "± 4591618.158988945"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 755771192.2,
            "unit": "ns",
            "range": "± 6272156.666278698"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 1377317.4193793403,
            "unit": "ns",
            "range": "± 29051.11855482037"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 1389958.7611607143,
            "unit": "ns",
            "range": "± 18580.594138355358"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 52103410.88333333,
            "unit": "ns",
            "range": "± 53613.81044532332"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 31636339.704166666,
            "unit": "ns",
            "range": "± 511176.1344200175"
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
          "id": "717d1ea3b61c226c608194aae5ff909fd4603455",
          "message": "Packaging: Unrect.Interactive ships as its own package\n\nThe project was already packable — id, description, tags, and a\ndependency on exactly Unrect and Unrect.Spreadsheets at the same MinVer\nversion — but the release workflow packed and asserted two packages, and\nthe README and build props named two. Now three.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T01:12:25Z",
          "tree_id": "f01ec5a705245aededd07d5455e7b03ad4f8880d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/717d1ea3b61c226c608194aae5ff909fd4603455"
        },
        "date": 1789796482986,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 2268128862.4666667,
            "unit": "ns",
            "range": "± 9038251.378558323"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Streamed",
            "value": 2689056179.5,
            "unit": "ns",
            "range": "± 12532343.383890819"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_SecondPass",
            "value": 5266507460.066667,
            "unit": "ns",
            "range": "± 13281734.48412057"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Eager",
            "value": 8719577.143028846,
            "unit": "ns",
            "range": "± 16821.026329551158"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Streamed",
            "value": 68593957.13157895,
            "unit": "ns",
            "range": "± 1452072.6443263185"
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
          "id": "4818c36f9e9b0c2b75a4093dbbce6981329fd14b",
          "message": "Merge feature/scaffold-shapes: ScaffoldRecord and ScaffoldClass, on a sheet and as leaves\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T15:35:04Z",
          "tree_id": "d48076b68bb0a93dc6ab6797b10f0ead882156a7",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4818c36f9e9b0c2b75a4093dbbce6981329fd14b"
        },
        "date": 1789847938577,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 1820696186.6,
            "unit": "ns",
            "range": "± 17951668.3034554"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Streamed",
            "value": 2289227155.4,
            "unit": "ns",
            "range": "± 31353057.97682698"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_SecondPass",
            "value": 4268425632.0666666,
            "unit": "ns",
            "range": "± 53449745.107524656"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Eager",
            "value": 7161667.491629465,
            "unit": "ns",
            "range": "± 33163.85506342373"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Streamed",
            "value": 61372118.787037045,
            "unit": "ns",
            "range": "± 800663.331979662"
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
          "id": "bd027f2136cee944da88c1eae17a06b14a05579d",
          "message": "Merge feature/bind-from-row: Table<T> fills a member from the row - Column(member, row => ...)\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T19:49:16Z",
          "tree_id": "88c877501aeddcefb436f136842b9904cda55a09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bd027f2136cee944da88c1eae17a06b14a05579d"
        },
        "date": 1789863953396,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 2233092883.076923,
            "unit": "ns",
            "range": "± 8858173.210820694"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Streamed",
            "value": 2736775561.733333,
            "unit": "ns",
            "range": "± 29969590.3467561"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_SecondPass",
            "value": 5266282446.466666,
            "unit": "ns",
            "range": "± 23347528.906260584"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Eager",
            "value": 7755677.802083333,
            "unit": "ns",
            "range": "± 20252.10801191638"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Streamed",
            "value": 64144715.34615385,
            "unit": "ns",
            "range": "± 541614.265609542"
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
          "id": "96b946d5920955eb81be3eab46697a3e4639e2e4",
          "message": "Merge feature/header-bands: headers of several rows, columns addressed by path, flat binding over bands\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T03:17:51Z",
          "tree_id": "33ccb96a2f1d1cb7795dde522c6c586ee8ce479a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/96b946d5920955eb81be3eab46697a3e4639e2e4"
        },
        "date": 1789888952048,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 1260586946.642857,
            "unit": "ns",
            "range": "± 14696977.886648165"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Streamed",
            "value": 1485401883.0714285,
            "unit": "ns",
            "range": "± 14055348.589717047"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_SecondPass",
            "value": 2897566708.928571,
            "unit": "ns",
            "range": "± 13979794.303066658"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Eager",
            "value": 4989601.866629465,
            "unit": "ns",
            "range": "± 47047.19900624588"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Streamed",
            "value": 44766546.04999999,
            "unit": "ns",
            "range": "± 824917.7972452802"
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
          "id": "191ad7e666be18cd9916eb0132108e83e54bd6db",
          "message": "Open questions: IsText is the wrong shape - what it is for, the better shape, and the larger question of facets, recorded and not implemented\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T04:23:54Z",
          "tree_id": "5e6c0e37d52a9f501293d06ad42b1a125ed81250",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/191ad7e666be18cd9916eb0132108e83e54bd6db"
        },
        "date": 1789931368824,
        "tool": "benchmarkdotnet",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 1941694692.6,
            "unit": "ns",
            "range": "± 17933035.992527544"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Streamed",
            "value": 2558587017.2,
            "unit": "ns",
            "range": "± 26254819.909180608"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_SecondPass",
            "value": 5076269816.933333,
            "unit": "ns",
            "range": "± 53871436.994820766"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Eager",
            "value": 6207803.4390625,
            "unit": "ns",
            "range": "± 40658.983501661656"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Streamed",
            "value": 45139464.45370371,
            "unit": "ns",
            "range": "± 1438314.5646714568"
          }
        ]
      }
    ],
    "Streaming Memory": [
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
        "date": 1788472138463,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 312001032,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 548490608,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 312001032,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 18507047,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 92483787,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 15261490,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 15260639,
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
          "id": "f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb",
          "message": "Fix the 2-core CI flake: a blocked-borrower proof needs a started borrower\n\nAReachWaitsForAWarmerRatherThanStartingASecondOpenOfTheSameFile failed on\nthe GitHub runner (ef348dd) on \"the wait is counted\": WarmWaitMilliseconds\nwas 0, and 0 was the honest count. The pool's warmers ride Task.Run and\nthe gated arrangement BLOCKS them inside their opens, one pool thread\neach — on a two-core runner that is the entire starting thread pool, so\nthe test's own Task.Run borrower never started until thread injection got\naround to it. Both blocked-ness assertions passed vacuously (not finished\nbecause not scheduled), and by the time the reach ran, the warm reader was\nparked and there was nothing left to wait for.\n\nReproduced under taskset -c 0,1: three failures in four runs before the\nfix, none in six Debug runs plus a Release run after. The fix is\nOnItsOwnThread (TaskCreationOptions.LongRunning) at the four sites that\nassert a borrower is blocked — a dedicated thread starts unconditionally,\nso \"started, and still not finished\" really does mean \"parked inside\nBorrow\". The three sibling sites could only pass vacuously, never fail,\nbut their proofs were the same lie under starvation. The burst tests\nalready stood on structural evidence (SpinUntil on OpensStarted) and are\nuntouched.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-03T22:37:18Z",
          "tree_id": "c37ffff8e7e618f8d8cdb3778c429c1bd5259fc9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/f9d4b35d017794b434bd9f3a3ecf31dc81ff83bb"
        },
        "date": 1788475704575,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 312000805,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 548490608,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 312001052,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 18507047,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 92483769,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 15261473,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 15260639,
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
          "id": "10027e9f1d263aac70041f0f7166b186324129e8",
          "message": "Both doors measure a sheet that will not say how big it is\n\nSpreadsheetSpace.Create sized its grid from reader.RowCount/FieldCount and\nsilently yielded an empty space when the reader would not give them — the\none outcome an adapter must not have, and a divergence from the streaming\ndoor, which has measured such sheets since Part 2 step 7. The fill is now\ntwo named siblings behind one dichotomy: ReadDeclared (the original loop,\nunchanged) and ReadMeasured (rows collected at their own width, the widest\nrow wins, absent trailing cells Blank — the same answer Workbook.Measure\ngives). The guard is rowCount <= 0 alone, deliberately mirroring the\nstreaming door so the two can never disagree about the same file.\n\nThe recorded cause was wrong, and is corrected everywhere it appeared: a\nmissing dimension element does not trigger this — ExcelDataReader derives\nboth counts from a pre-scan of the cells on every format it handles. The\nreachable trigger is a sheet with NO valued cell (rows of formatted-but-\nvalueless cells, a pre-formatted export region). Pinned by the committed\nTestData/no-extent.xlsx (dimensionless AND valueless, with the survey's\nRowsMeasured == 4 doubling as the fixture's own guard against a\nregeneration that quietly stops reaching the path) and a both-doors\nidentity test.\n\nRides along, both owner decisions from this session's discussion:\n- MaxReaders: spec §14 Q2 DECIDED — 3 stays and stops being provisional,\n  because no number is right: reader demand is the declaration's monotone-\n  cursor count, unbounded in principle, data-independent in practice, and\n  the ceiling fails gently (Reopens is the counted, named signal to raise\n  it). Sizing guidance added to docs/streaming.md; per-reader economics\n  (~5s CPU per open, position must be walked, reader-per-row is O(n^2))\n  recorded in the spec.\n- Table's header-derived width: spec §14 Q1 DEFERRED, superseding the\n  2026-09-03 yes — the step-8 interleave delivered the lazy win with\n  today's denotation intact, so the K-1 campaign votes before the\n  denotation change is paid for.\n\nSuite 1,382 -> 1,387; gates green in Debug and 2-core Release.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T14:29:21Z",
          "tree_id": "fc431b0954d2e3a5115a177bd1a21d63c169ffae",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/10027e9f1d263aac70041f0f7166b186324129e8"
        },
        "date": 1788533066184,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 312195485,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 430579936,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 312195485,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 18512527,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 92523275,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 15257673,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 15256839,
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
          "id": "c01531cec6968e544acc578291244292172a00a5",
          "message": "Docs: Part 3 deferred on principle, and .Sized's composite role stated honestly\n\nSpec §13 gains the Part 3 row (bound-aware composite placement): the\nengine's remaining greed sorted into one necessary force (Repeat items —\nthe item's existence is the question), one free force (post-Project\nconsumption, amortised by the root's accounting), and one debt (composite\nchild placement, whose questions have lazy answers nobody asks for).\nDeferred until the first tall sized composite pays the debt — sized\ncomposites in the corpus are short header bands, where settling eagerly\ncosts nothing. The K-1 campaign is the likely judge; the census pin is the\ntripwire.\n\ndocs/streaming.md stops saying \"put the .Sized on the leaf\" as if it were\na law: a sized composite is a legitimate spelling with no leaf equivalent\n— a composite has no intrinsic extent, and the declared band is what\nscopes its internal seeks and settles its consumption.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T15:37:53Z",
          "tree_id": "6188ce68af3130bfba604f38845b0c515958cb34",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/c01531cec6968e544acc578291244292172a00a5"
        },
        "date": 1788537631823,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 312195424,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 430579568,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 312195485,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 18512527,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 92523233,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 15257665,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 15256839,
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
          "id": "2d73985e95c70f51a2b26d7dc98c3936f1f52d5d",
          "message": "Retention: the live-set floor for the interning change, with the target on the chart\n\nAn eighth CI leg that is not a BenchmarkDotNet family: interning reduces\nRETAINED bytes, not allocations (a duplicate string is allocated by the\nreader before the adapter sees it and dies young after dedup), so the\nAllocated column cannot see it — and retention is deterministic, so it\nneeds no statistical engine. A one-shot job measures live bytes with the\nresult held, emits the same JSON document the rig already stores, and\nrides the same workflow and dashboard as everything else.\n\nBuilding it surfaced two facts worth more than the plumbing:\n\n- The eager door's duplication depends on how the file spells its text.\n  Shared-string cells come back already deduped (the reader returns its\n  table's own instance); inline strings and formula-result cells\n  materialise fresh per cell. A real Excel export is both (the local K-1:\n  9,049 text cells, 2,876 values, 4,016 instances — the formula results\n  are the duplicated half). The family brackets it, and the shared-string\n  row is the priced TARGET: the same cells read 112.0 MB duplicated vs\n  58.2 MB deduped, so ~48% is what a complete eager interner is worth on\n  this shape — short of that is unfinished, not failed.\n- The first fixture boxed decimals a real read never produces (16 MB of\n  boxes in a retained-bytes measurement); the retention fixtures now\n  yield doubles like a reader does. StreamingSpaces is deliberately\n  untouched — changing it would re-baseline that family's history.\n\nScenarios exercise the real seams the interning change will live in: the\neager rows go through SpreadsheetSpace.Create over generated workbooks\n(RetentionWorkbooks: a minimal hand-rolled OOXML writer, no new package;\nthe one deliberate exception to the no-workbooks rule, recorded in\ndocs/benchmarking.md), the streaming rows through the store's chunk fill.\nFloor: eager space held 106.8 MB, results held 82.1 MB both doors\n(byte-identical — streaming's promise stated in the metric), controls\nbyte-identical to their duplicated twins by fixed-width padding. Leg\nruns ~65s, the shortest in the matrix.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T16:47:38Z",
          "tree_id": "0c756ae6dd2d4f17cd84e585c99d7d3ae08fd409",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/2d73985e95c70f51a2b26d7dc98c3936f1f52d5d"
        },
        "date": 1788542163822,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 312195485,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 430579936,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 312195485,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 18512527,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 92523220,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 15257673,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 15256839,
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
          "id": "eddc5d17c38c715f41cd95d041452deb66f8354c",
          "message": "Interning: equal text shares one instance through both doors\n\nAdapter-level string interning — a find-my-twin table at each door's\nadapt seam. The eager door threads a per-Create-call HashSet through both\nfill paths (one instance per distinct value across every sheet of the\ncall); the streaming door hangs one capped ConcurrentDictionary on the\nWorkbook, plumbed into every store's chunk fill, so a chase reader's\nre-parse dedupes against the first parse. Strings only: every other kind\nis inline in the 24-byte struct or unreachable from the spreadsheet door.\n\nThe win is retention, not allocation — the duplicate is allocated by the\nreader before the adapter sees it and dies in gen0 after dedup, which is\nwhy the Retention leg is the judge and MemoryDiagnoser is blind to it.\nMeasured against the committed floor: eager space held 106.8 -> 55.5 MB,\nlanding on the priced shared-string target to the byte; held results\n82.1 -> 30.8 MB, byte-identical across doors; all three unique controls\nflat to the byte; wall time noise on the 1M-row parse.\n\nThe cap (WorkbookOptions.MaxInternedStrings, default 65,536; 0 = off)\nand the 256-char length guard bound what the book-lifetime table can\npin. Documented as a two-way knob: a full table costs its entries for\nthe book's life — some 40 MB at the default cap — so it turns DOWN, to\n0, for known-unique text, and the docs say so at every site (the rig is\nstructurally blind to the table's own live set: readings are taken with\nthe book closed). Workbook.InterningStatistics reports hits, distinct,\nand estimated bytes (64-bit layout, exact for it; Hits counts fills, so\na reloaded chunk counts again — read against ChunkReloads).\n\nExcelDataReader fact on the record: shared-string cells arrive\npre-deduped from the reader's own SST; the duplication this kills comes\nfrom inline-string cells, formula-result cells, and .xls. Pinned by 36\ntests including a cross-door sharing differential, mutation-checked both\ndirections, and a WeakReference proof that Dispose releases the strings.\nSuite 1,423.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T19:19:56Z",
          "tree_id": "fea5150f427d5b65cf652662879afb3b470547f2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/eddc5d17c38c715f41cd95d041452deb66f8354c"
        },
        "date": 1788556081525,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 312195485,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 440547416,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 312195485,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 23538202,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 97548936,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 15531691,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 15530847,
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
          "id": "16fef6aa0fb6978c79559b92910716e6ad44c995",
          "message": "netstandard2.0: the packages install on .NET Framework, one source, no forks\n\nAll four libraries multi-target netstandard2.0;netstandard2.1. The rule\nthat made it clean: no #if anywhere — netstandard2.1-only constructs are\nremoved rather than branched on, so both targets compile one source and\ncannot drift.\n\nThe structural change is de-DIMing the incremental calculus. .NET\nFramework's runtime cannot dispatch a default interface member, so the\ndefinitional folds move from DIM bodies to the static Unrect.Core.Scans\n(Fold/FoldSize/FoldArea) and internal ColumnAccumulators.Fold, with every\nimplementation delegating in one line. The guarantee shifts and every doc\nthat claimed it says so honestly: \"eager and lazy cannot disagree by\nconstruction\" is now \"cannot disagree, pinned by the fold-identity suite\"\n— IncrementalStrategyTests asserts SelectRows == a hand-written fold for\nevery factory, and a new incremental strategy carries the obligation to\njoin that theory data. Corollary recorded in capability-seam-notes: the\nDIM \"safety net\" for evolving ISpace is withdrawn — a member added to a\npublished Core interface is now a breaking change with no escape hatch;\ntype-testing is the whole capability recipe.\n\nThe rest the compiler found: System.HashCode (Core hand-rolls its combine\nrather than take Microsoft.Bcl.HashCode — the bundling would have\nsuppressed the dependency from the nuspec and thrown FileNotFoundException\non Framework; both packages stay at zero new dependencies), the\nDictionary-from-pairs constructor, and HashSet<string>.TryGetValue (the\neager intern table is a Dictionary now; the comment that argued HashSet\nwas the honest structure records why it changed). The two polyfills are\nunconditional because neither netstandard has the types; a net5.0+ target\nis what would force an #if.\n\nTests multi-target net8.0;net48 on Windows only (OS-conditioned csproj,\nso Linux CI runs exactly what it ran — verified against all three\nworkflows). Two portable shims replace .NET-5+/6+ APIs the suite used:\nWithTimeout for Task.WaitAsync(TimeSpan), ByReference for\nReferenceEqualityComparer. The full suite has run green on the Framework\nCLR itself: 1,423 on net48 against the netstandard2.0 build, ExcelDataReader's\nown Framework assembly included — verified by the owner on Windows.\nBoth nupkgs verified to carry lib/netstandard2.0 and lib/netstandard2.1.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-05T17:36:21Z",
          "tree_id": "67b64bc3184652344901c8a243f8ce1256d858b9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16fef6aa0fb6978c79559b92910716e6ad44c995"
        },
        "date": 1788630325771,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 312195485,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 440547416,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 312195485,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 23538174,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 97548899,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 15531670,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 15530847,
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
          "id": "e1d21c2d706b054d252bbea3a996b97f6f7bfc4b",
          "message": "Hygiene: .gitattributes and editorconfig trim — line endings become a decision\n\nThe repo had no .gitattributes, so line endings were whatever tool\ntouched a file last — four .linq files sat CRLF in the index beside an\nLF corpus, and the vocabulary respell nearly flipped three of them\nwholesale (caught in review; a 10-line diff had become 366). One rule\nnow: the index holds LF (* text=auto); .linq and .sln check out CRLF\nbecause their native editors are Windows-bound; spreadsheets and images\nare explicitly binary. This commit carries the one-time renormalization\nso the flip lives here, deliberately, and nowhere else.\n\n.editorconfig gains trailing-whitespace trimming, with markdown exempt\n(a trailing double-space is a hard line break).\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-06T00:49:13Z",
          "tree_id": "ce25b3593543c4145d69070ff009c9f67ff1aa09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/e1d21c2d706b054d252bbea3a996b97f6f7bfc4b"
        },
        "date": 1788712515134,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 312195485,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 440547128,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 312195485,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 23538195,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 97548936,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 15531685,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 15530870,
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
          "id": "bf1fc2851544d9f87ffb3b76df4fa0b714780243",
          "message": "Merge experiment/typed-spaces: the projection model\n\nThe deepest cut since wave 2: what was called Shape is a projection; the\nspace and its subspaces are the shapes. Seven phases plus pre-merge\ncleanup (docs/design/projection-model-spec.md, judgment record in §10):\nthe modifier doubling killed (43 -> 6 irreducible), IShape -> IProjection\nwith namespace Unrect.Projections, the capability stack (IFormulaSpace,\nCapability<T>() transport, the slicing law, shared-formula\nreconstruction), OrBlank and the eachRow slot, the CaptionMap bind, the\nscoped entry Over<TSpace>() and MapWorkbook, phase-7 acceptance with the\nfourteen recorded refusals, and the three-sweep cleanup.\n\nSuite 1,709 green (net8.0 and net48); both library TFMs 0 warnings.\nBreaking: the rename, the Table family, the SpreadsheetSpace shell\nretirement, and the entries.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-07T23:11:42Z",
          "tree_id": "d091a9f7437fb1ec8d2b35119ab628b2a54047a3",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bf1fc2851544d9f87ffb3b76df4fa0b714780243"
        },
        "date": 1788823323846,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 312195557,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 440547200,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 312195557,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 23538195,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 97548936,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 15531698,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 15530858,
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
          "id": "4a44b787ac6b8b6fa5c487623e316bfe0e364292",
          "message": "Presence semantics and three hygiene fixes: the zero disambiguated\n\nThe algebra's highest-priority open problem (v0.4 §7/§14.1): geometric\nzero carried four meanings. The internal Presence { Read, Empty,\nAbsorbed } now rides beside Consumed on the engine's results — stamped\nat tolerance boundaries (Absorbed), settled-at-zero extents (Empty, read\noff the SETTLED extent so evaluation order and doors agree by\nconstruction), joined through layouts (Read if any child Read, else\nEmpty), defaulting Read. The internal epsilon (NothingProjection) makes\nthe flow-unit law stateable: VerticalFlow is a monoid at L3 for\nsuccessful readings, boundaries pinned.\n\nThe law, earned the hard way: presence explains a stop; the extent\ndecides one. The first implementation let presence decide the repeat\nguard; the law-tests caught a real L1/L2 change on\nVerticalRepeat(item.Optional().Sized(...)) — an Absorbed boundary under\na declared area legitimately consumes it and always kept the run going.\nThe guard is byte-identical to before; presence powers only the new\nteaching Info when a run ends at an absorbed item (D2), and the caught\nshape is pinned as the compatibility-law section of PresenceLawTests.\n\nDesign record: docs/design/presence-and-unit-spec.md (DECIDED, with the\nD5 amendment trail).\n\nAlso, with pins: ChoiceProjection.Summarise renders nested aggregates\ndepth-indented with one location per line; MinOffset() is one canonical\ninstance (HasDeclaredOffset correct by construction); the content-\nmatching rule reduced to one implementation (CellMatching primitives,\nTableView's dictionary keyed on TextComparer; CaptionComparer's\ndeliberate divergence untouched). spike/Phase1Probe removed (a husk).\n\nSuite 1,820 -> 1,862 green; both library TFMs 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-09T15:34:18Z",
          "tree_id": "883452644caa55f06a0f9c2d530ee9c0cece2b92",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4a44b787ac6b8b6fa5c487623e316bfe0e364292"
        },
        "date": 1788969083693,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 312195557,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 440547200,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 312195557,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 23538195,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 97548936,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 15531698,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 15530847,
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
          "id": "d06708df60e381758b11dde07836c674565409b6",
          "message": "Merge experiment/record-primitive: streaming composites over a bound, the band tiler, Table composed from primitives\n\nThe labels-as-context primitives (ColumnLabels, WithColumnLabels, Record),\nSkipToFirstNonBlankCell, and the uniform offset law; the engine streaming a\ndiscovered bound through a composite (TailSpace, a bound-aware Exceeds, lazy\nflow and repeat slices); VerticalBands/HorizontalBands, the tiler — a\nrepeating fixed-dimension space, distinct from the pattern repeat;\nTable(headerRows, eachRow) composed over the tiler under a UnitProjection,\nwith AsScaffolding and a mark-driven path fold giving it the leaf's own\ndiagnostics; ColumnLabels self-contained, its header parse the one home in\nLabelMap.FromHeader; and the repeat returned to a pure walk, onBlank living\non the tiler alone.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-14T17:43:35Z",
          "tree_id": "1c80a0bed7aeb311bb6e1bdb0933ac5a05562434",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/d06708df60e381758b11dde07836c674565409b6"
        },
        "date": 1789408536339,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 314195715,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 442547088,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 314195752,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 23538206,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 97548907,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 15531647,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 15530847,
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
          "id": "33f1afa3496afb1fabd2e0242197d1c5d9889568",
          "message": "Merge experiment/point-and-line: the Point substrate — one vocabulary over any ISpace, planes and points as the locators, the kinds in Unrect.Spreadsheets\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-16T04:05:17Z",
          "tree_id": "09d4c757d0f1502df340fb2a2481c31f78e8d8b2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/33f1afa3496afb1fabd2e0242197d1c5d9889568"
        },
        "date": 1789531992073,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 832198832,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 960549976,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 832198832,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 2345,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 2345,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 15531683,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 15530869,
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
          "id": "0f34f159151554afcc0555f7afc83fdd486b8c06",
          "message": "Merge experiment/point-follow-ups: Extents into Plane, Unrect.Interactive, and the typed-predicate lift\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-17T02:37:19Z",
          "tree_id": "cfa1d003f75555e9d6f330d316cdd949a1da2b25",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/0f34f159151554afcc0555f7afc83fdd486b8c06"
        },
        "date": 1789613139888,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 832198832,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Windowed",
            "value": 960549976,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Resident",
            "value": 832198832,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowFits",
            "value": 2345,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_WindowTooSmall",
            "value": 2345,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_OneReader",
            "value": 15531690,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Adversarial_Pooled",
            "value": 15530862,
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
          "id": "717d1ea3b61c226c608194aae5ff909fd4603455",
          "message": "Packaging: Unrect.Interactive ships as its own package\n\nThe project was already packable — id, description, tags, and a\ndependency on exactly Unrect and Unrect.Spreadsheets at the same MinVer\nversion — but the release workflow packed and asserted two packages, and\nthe README and build props named two. Now three.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T01:12:25Z",
          "tree_id": "f01ec5a705245aededd07d5455e7b03ad4f8880d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/717d1ea3b61c226c608194aae5ff909fd4603455"
        },
        "date": 1789796485934,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 2972530312,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Streamed",
            "value": 3178724872,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_SecondPass",
            "value": 6347476152,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Eager",
            "value": 3147446,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Streamed",
            "value": 28557811,
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
          "id": "4818c36f9e9b0c2b75a4093dbbce6981329fd14b",
          "message": "Merge feature/scaffold-shapes: ScaffoldRecord and ScaffoldClass, on a sheet and as leaves\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T15:35:04Z",
          "tree_id": "d48076b68bb0a93dc6ab6797b10f0ead882156a7",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4818c36f9e9b0c2b75a4093dbbce6981329fd14b"
        },
        "date": 1789847940295,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 2972530312,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Streamed",
            "value": 3178724928,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_SecondPass",
            "value": 6347476136,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Eager",
            "value": 3147663,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Streamed",
            "value": 28557378,
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
          "id": "bd027f2136cee944da88c1eae17a06b14a05579d",
          "message": "Merge feature/bind-from-row: Table<T> fills a member from the row - Column(member, row => ...)\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T19:49:16Z",
          "tree_id": "88c877501aeddcefb436f136842b9904cda55a09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bd027f2136cee944da88c1eae17a06b14a05579d"
        },
        "date": 1789863955366,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 2972530616,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Streamed",
            "value": 3178725264,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_SecondPass",
            "value": 6347476808,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Eager",
            "value": 3147518,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Streamed",
            "value": 28557523,
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
          "id": "96b946d5920955eb81be3eab46697a3e4639e2e4",
          "message": "Merge feature/header-bands: headers of several rows, columns addressed by path, flat binding over bands\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T03:17:51Z",
          "tree_id": "33ccb96a2f1d1cb7795dde522c6c586ee8ce479a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/96b946d5920955eb81be3eab46697a3e4639e2e4"
        },
        "date": 1789888953965,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Eager",
            "value": 2972537744,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_Streamed",
            "value": 3178732400,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Monotone_SecondPass",
            "value": 6347491072,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Eager",
            "value": 3147687,
            "unit": "bytes"
          },
          {
            "name": "Unrect.Benchmarks.Streaming.Band_Streamed",
            "value": 28557495,
            "unit": "bytes"
          }
        ]
      }
    ],
    "Retention": [
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
          "id": "2d73985e95c70f51a2b26d7dc98c3936f1f52d5d",
          "message": "Retention: the live-set floor for the interning change, with the target on the chart\n\nAn eighth CI leg that is not a BenchmarkDotNet family: interning reduces\nRETAINED bytes, not allocations (a duplicate string is allocated by the\nreader before the adapter sees it and dies young after dedup), so the\nAllocated column cannot see it — and retention is deterministic, so it\nneeds no statistical engine. A one-shot job measures live bytes with the\nresult held, emits the same JSON document the rig already stores, and\nrides the same workflow and dashboard as everything else.\n\nBuilding it surfaced two facts worth more than the plumbing:\n\n- The eager door's duplication depends on how the file spells its text.\n  Shared-string cells come back already deduped (the reader returns its\n  table's own instance); inline strings and formula-result cells\n  materialise fresh per cell. A real Excel export is both (the local K-1:\n  9,049 text cells, 2,876 values, 4,016 instances — the formula results\n  are the duplicated half). The family brackets it, and the shared-string\n  row is the priced TARGET: the same cells read 112.0 MB duplicated vs\n  58.2 MB deduped, so ~48% is what a complete eager interner is worth on\n  this shape — short of that is unfinished, not failed.\n- The first fixture boxed decimals a real read never produces (16 MB of\n  boxes in a retained-bytes measurement); the retention fixtures now\n  yield doubles like a reader does. StreamingSpaces is deliberately\n  untouched — changing it would re-baseline that family's history.\n\nScenarios exercise the real seams the interning change will live in: the\neager rows go through SpreadsheetSpace.Create over generated workbooks\n(RetentionWorkbooks: a minimal hand-rolled OOXML writer, no new package;\nthe one deliberate exception to the no-workbooks rule, recorded in\ndocs/benchmarking.md), the streaming rows through the store's chunk fill.\nFloor: eager space held 106.8 MB, results held 82.1 MB both doors\n(byte-identical — streaming's promise stated in the metric), controls\nbyte-identical to their duplicated twins by fixed-width padding. Leg\nruns ~65s, the shortest in the matrix.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T16:47:38Z",
          "tree_id": "0c756ae6dd2d4f17cd84e585c99d7d3ae08fd409",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/2d73985e95c70f51a2b26d7dc98c3936f1f52d5d"
        },
        "date": 1788542164058,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld",
            "value": 112000168,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · SpreadsheetSpace.Create over a real .xlsx (inline strings); grid held"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Unique",
            "value": 112000168,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same file and reader, every text distinct"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Shared",
            "value": 58223080,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL/TARGET — the same values shared-string encoded, which the reader already dedups"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_ResultHeld",
            "value": 86096872,
            "range": "± 536 bytes",
            "unit": "bytes",
            "extra": "median of 3 · TableRows over the eager grid; result held, grid released"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld",
            "value": 86096872,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · TableRows through a window; result held, workbook closed"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld_Unique",
            "value": 86096872,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same projection, every text distinct"
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
          "id": "eddc5d17c38c715f41cd95d041452deb66f8354c",
          "message": "Interning: equal text shares one instance through both doors\n\nAdapter-level string interning — a find-my-twin table at each door's\nadapt seam. The eager door threads a per-Create-call HashSet through both\nfill paths (one instance per distinct value across every sheet of the\ncall); the streaming door hangs one capped ConcurrentDictionary on the\nWorkbook, plumbed into every store's chunk fill, so a chase reader's\nre-parse dedupes against the first parse. Strings only: every other kind\nis inline in the 24-byte struct or unreachable from the spreadsheet door.\n\nThe win is retention, not allocation — the duplicate is allocated by the\nreader before the adapter sees it and dies in gen0 after dedup, which is\nwhy the Retention leg is the judge and MemoryDiagnoser is blind to it.\nMeasured against the committed floor: eager space held 106.8 -> 55.5 MB,\nlanding on the priced shared-string target to the byte; held results\n82.1 -> 30.8 MB, byte-identical across doors; all three unique controls\nflat to the byte; wall time noise on the 1M-row parse.\n\nThe cap (WorkbookOptions.MaxInternedStrings, default 65,536; 0 = off)\nand the 256-char length guard bound what the book-lifetime table can\npin. Documented as a two-way knob: a full table costs its entries for\nthe book's life — some 40 MB at the default cap — so it turns DOWN, to\n0, for known-unique text, and the docs say so at every site (the rig is\nstructurally blind to the table's own live set: readings are taken with\nthe book closed). Workbook.InterningStatistics reports hits, distinct,\nand estimated bytes (64-bit layout, exact for it; Hits counts fills, so\na reloaded chunk counts again — read against ChunkReloads).\n\nExcelDataReader fact on the record: shared-string cells arrive\npre-deduped from the reader's own SST; the duplication this kills comes\nfrom inline-string cells, formula-result cells, and .xls. Pinned by 36\ntests including a cross-door sharing differential, mutation-checked both\ndirections, and a WeakReference proof that Dispose releases the strings.\nSuite 1,423.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-04T19:19:56Z",
          "tree_id": "fea5150f427d5b65cf652662879afb3b470547f2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/eddc5d17c38c715f41cd95d041452deb66f8354c"
        },
        "date": 1788556081699,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld",
            "value": 58223080,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · SpreadsheetSpace.Create over a real .xlsx (inline strings); grid held"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Unique",
            "value": 112000168,
            "range": "± 6,144 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same file and reader, every text distinct"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Shared",
            "value": 58222544,
            "range": "± 536 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL/TARGET — the same values shared-string encoded, which the reader already dedups"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · TableRows over the eager grid; result held, grid released"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · TableRows through a window; result held, workbook closed"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld_Unique",
            "value": 86096872,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same projection, every text distinct"
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
          "id": "16fef6aa0fb6978c79559b92910716e6ad44c995",
          "message": "netstandard2.0: the packages install on .NET Framework, one source, no forks\n\nAll four libraries multi-target netstandard2.0;netstandard2.1. The rule\nthat made it clean: no #if anywhere — netstandard2.1-only constructs are\nremoved rather than branched on, so both targets compile one source and\ncannot drift.\n\nThe structural change is de-DIMing the incremental calculus. .NET\nFramework's runtime cannot dispatch a default interface member, so the\ndefinitional folds move from DIM bodies to the static Unrect.Core.Scans\n(Fold/FoldSize/FoldArea) and internal ColumnAccumulators.Fold, with every\nimplementation delegating in one line. The guarantee shifts and every doc\nthat claimed it says so honestly: \"eager and lazy cannot disagree by\nconstruction\" is now \"cannot disagree, pinned by the fold-identity suite\"\n— IncrementalStrategyTests asserts SelectRows == a hand-written fold for\nevery factory, and a new incremental strategy carries the obligation to\njoin that theory data. Corollary recorded in capability-seam-notes: the\nDIM \"safety net\" for evolving ISpace is withdrawn — a member added to a\npublished Core interface is now a breaking change with no escape hatch;\ntype-testing is the whole capability recipe.\n\nThe rest the compiler found: System.HashCode (Core hand-rolls its combine\nrather than take Microsoft.Bcl.HashCode — the bundling would have\nsuppressed the dependency from the nuspec and thrown FileNotFoundException\non Framework; both packages stay at zero new dependencies), the\nDictionary-from-pairs constructor, and HashSet<string>.TryGetValue (the\neager intern table is a Dictionary now; the comment that argued HashSet\nwas the honest structure records why it changed). The two polyfills are\nunconditional because neither netstandard has the types; a net5.0+ target\nis what would force an #if.\n\nTests multi-target net8.0;net48 on Windows only (OS-conditioned csproj,\nso Linux CI runs exactly what it ran — verified against all three\nworkflows). Two portable shims replace .NET-5+/6+ APIs the suite used:\nWithTimeout for Task.WaitAsync(TimeSpan), ByReference for\nReferenceEqualityComparer. The full suite has run green on the Framework\nCLR itself: 1,423 on net48 against the netstandard2.0 build, ExcelDataReader's\nown Framework assembly included — verified by the owner on Windows.\nBoth nupkgs verified to carry lib/netstandard2.0 and lib/netstandard2.1.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-05T17:36:21Z",
          "tree_id": "67b64bc3184652344901c8a243f8ce1256d858b9",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/16fef6aa0fb6978c79559b92910716e6ad44c995"
        },
        "date": 1788630325961,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld",
            "value": 58223080,
            "range": "± 6,144 bytes",
            "unit": "bytes",
            "extra": "median of 3 · SpreadsheetSpace.Create over a real .xlsx (inline strings); grid held"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Unique",
            "value": 112000168,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same file and reader, every text distinct"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Shared",
            "value": 58223080,
            "range": "± 536 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL/TARGET — the same values shared-string encoded, which the reader already dedups"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · TableRows over the eager grid; result held, grid released"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · TableRows through a window; result held, workbook closed"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld_Unique",
            "value": 86096872,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same projection, every text distinct"
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
          "id": "e1d21c2d706b054d252bbea3a996b97f6f7bfc4b",
          "message": "Hygiene: .gitattributes and editorconfig trim — line endings become a decision\n\nThe repo had no .gitattributes, so line endings were whatever tool\ntouched a file last — four .linq files sat CRLF in the index beside an\nLF corpus, and the vocabulary respell nearly flipped three of them\nwholesale (caught in review; a 10-line diff had become 366). One rule\nnow: the index holds LF (* text=auto); .linq and .sln check out CRLF\nbecause their native editors are Windows-bound; spreadsheets and images\nare explicitly binary. This commit carries the one-time renormalization\nso the flip lives here, deliberately, and nowhere else.\n\n.editorconfig gains trailing-whitespace trimming, with markdown exempt\n(a trailing double-space is a hard line break).\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-06T00:49:13Z",
          "tree_id": "ce25b3593543c4145d69070ff009c9f67ff1aa09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/e1d21c2d706b054d252bbea3a996b97f6f7bfc4b"
        },
        "date": 1788712515323,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld",
            "value": 58223080,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · SpreadsheetSpace.Create over a real .xlsx (inline strings); grid held"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Unique",
            "value": 112000168,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same file and reader, every text distinct"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Shared",
            "value": 58223080,
            "range": "± 536 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL/TARGET — the same values shared-string encoded, which the reader already dedups"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · TableRows over the eager grid; result held, grid released"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · TableRows through a window; result held, workbook closed"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld_Unique",
            "value": 86096872,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same projection, every text distinct"
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
          "id": "bf1fc2851544d9f87ffb3b76df4fa0b714780243",
          "message": "Merge experiment/typed-spaces: the projection model\n\nThe deepest cut since wave 2: what was called Shape is a projection; the\nspace and its subspaces are the shapes. Seven phases plus pre-merge\ncleanup (docs/design/projection-model-spec.md, judgment record in §10):\nthe modifier doubling killed (43 -> 6 irreducible), IShape -> IProjection\nwith namespace Unrect.Projections, the capability stack (IFormulaSpace,\nCapability<T>() transport, the slicing law, shared-formula\nreconstruction), OrBlank and the eachRow slot, the CaptionMap bind, the\nscoped entry Over<TSpace>() and MapWorkbook, phase-7 acceptance with the\nfourteen recorded refusals, and the three-sweep cleanup.\n\nSuite 1,709 green (net8.0 and net48); both library TFMs 0 warnings.\nBreaking: the rename, the Table family, the SpreadsheetSpace shell\nretirement, and the entries.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-07T23:11:42Z",
          "tree_id": "d091a9f7437fb1ec8d2b35119ab628b2a54047a3",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bf1fc2851544d9f87ffb3b76df4fa0b714780243"
        },
        "date": 1788823324124,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld",
            "value": 58223056,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · SpreadsheetSpace.Create over a real .xlsx (inline strings); grid held"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Unique",
            "value": 112000144,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same file and reader, every text distinct"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Shared",
            "value": 58223056,
            "range": "± 536 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL/TARGET — the same values shared-string encoded, which the reader already dedups"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table over the eager grid; result held, grid released"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table through a window; result held, workbook closed"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld_Unique",
            "value": 86096872,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same projection, every text distinct"
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
          "id": "4a44b787ac6b8b6fa5c487623e316bfe0e364292",
          "message": "Presence semantics and three hygiene fixes: the zero disambiguated\n\nThe algebra's highest-priority open problem (v0.4 §7/§14.1): geometric\nzero carried four meanings. The internal Presence { Read, Empty,\nAbsorbed } now rides beside Consumed on the engine's results — stamped\nat tolerance boundaries (Absorbed), settled-at-zero extents (Empty, read\noff the SETTLED extent so evaluation order and doors agree by\nconstruction), joined through layouts (Read if any child Read, else\nEmpty), defaulting Read. The internal epsilon (NothingProjection) makes\nthe flow-unit law stateable: VerticalFlow is a monoid at L3 for\nsuccessful readings, boundaries pinned.\n\nThe law, earned the hard way: presence explains a stop; the extent\ndecides one. The first implementation let presence decide the repeat\nguard; the law-tests caught a real L1/L2 change on\nVerticalRepeat(item.Optional().Sized(...)) — an Absorbed boundary under\na declared area legitimately consumes it and always kept the run going.\nThe guard is byte-identical to before; presence powers only the new\nteaching Info when a run ends at an absorbed item (D2), and the caught\nshape is pinned as the compatibility-law section of PresenceLawTests.\n\nDesign record: docs/design/presence-and-unit-spec.md (DECIDED, with the\nD5 amendment trail).\n\nAlso, with pins: ChoiceProjection.Summarise renders nested aggregates\ndepth-indented with one location per line; MinOffset() is one canonical\ninstance (HasDeclaredOffset correct by construction); the content-\nmatching rule reduced to one implementation (CellMatching primitives,\nTableView's dictionary keyed on TextComparer; CaptionComparer's\ndeliberate divergence untouched). spike/Phase1Probe removed (a husk).\n\nSuite 1,820 -> 1,862 green; both library TFMs 0 warnings.\n\nCo-Authored-By: Claude Fable 5 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_016BvUBicaVLLYkdp7iqFZNo",
          "timestamp": "2026-09-09T15:34:18Z",
          "tree_id": "883452644caa55f06a0f9c2d530ee9c0cece2b92",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4a44b787ac6b8b6fa5c487623e316bfe0e364292"
        },
        "date": 1788969083961,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld",
            "value": 58223056,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · SpreadsheetSpace.Create over a real .xlsx (inline strings); grid held"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Unique",
            "value": 112000144,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same file and reader, every text distinct"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Shared",
            "value": 58223056,
            "range": "± 536 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL/TARGET — the same values shared-string encoded, which the reader already dedups"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table over the eager grid; result held, grid released"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table through a window; result held, workbook closed"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld_Unique",
            "value": 86096872,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same projection, every text distinct"
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
          "id": "d06708df60e381758b11dde07836c674565409b6",
          "message": "Merge experiment/record-primitive: streaming composites over a bound, the band tiler, Table composed from primitives\n\nThe labels-as-context primitives (ColumnLabels, WithColumnLabels, Record),\nSkipToFirstNonBlankCell, and the uniform offset law; the engine streaming a\ndiscovered bound through a composite (TailSpace, a bound-aware Exceeds, lazy\nflow and repeat slices); VerticalBands/HorizontalBands, the tiler — a\nrepeating fixed-dimension space, distinct from the pattern repeat;\nTable(headerRows, eachRow) composed over the tiler under a UnitProjection,\nwith AsScaffolding and a mark-driven path fold giving it the leaf's own\ndiagnostics; ColumnLabels self-contained, its header parse the one home in\nLabelMap.FromHeader; and the repeat returned to a pure walk, onBlank living\non the tiler alone.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-14T17:43:35Z",
          "tree_id": "1c80a0bed7aeb311bb6e1bdb0933ac5a05562434",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/d06708df60e381758b11dde07836c674565409b6"
        },
        "date": 1789408536601,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld",
            "value": 58223056,
            "range": "± 6,144 bytes",
            "unit": "bytes",
            "extra": "median of 3 · SpreadsheetSpace.Create over a real .xlsx (inline strings); grid held"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Unique",
            "value": 112000144,
            "range": "± 536 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same file and reader, every text distinct"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Shared",
            "value": 58223056,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL/TARGET — the same values shared-string encoded, which the reader already dedups"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table over the eager grid; result held, grid released"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table through a window; result held, workbook closed"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld_Unique",
            "value": 86096872,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same projection, every text distinct"
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
          "id": "33f1afa3496afb1fabd2e0242197d1c5d9889568",
          "message": "Merge experiment/point-and-line: the Point substrate — one vocabulary over any ISpace, planes and points as the locators, the kinds in Unrect.Spreadsheets\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-16T04:05:17Z",
          "tree_id": "09d4c757d0f1502df340fb2a2481c31f78e8d8b2",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/33f1afa3496afb1fabd2e0242197d1c5d9889568"
        },
        "date": 1789531992337,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld",
            "value": 58223048,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · SpreadsheetSpace.Create over a real .xlsx (inline strings); grid held"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Unique",
            "value": 112000136,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same file and reader, every text distinct"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Shared",
            "value": 58223048,
            "range": "± 5,608 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL/TARGET — the same values shared-string encoded, which the reader already dedups"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table over the eager grid; result held, grid released"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table through a window; result held, workbook closed"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld_Unique",
            "value": 86096872,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same projection, every text distinct"
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
          "id": "0f34f159151554afcc0555f7afc83fdd486b8c06",
          "message": "Merge experiment/point-follow-ups: Extents into Plane, Unrect.Interactive, and the typed-predicate lift\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01Wk5ZXojzv4hibCXDkjRu3g",
          "timestamp": "2026-09-17T02:37:19Z",
          "tree_id": "cfa1d003f75555e9d6f330d316cdd949a1da2b25",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/0f34f159151554afcc0555f7afc83fdd486b8c06"
        },
        "date": 1789613140162,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld",
            "value": 58223048,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · SpreadsheetSpace.Create over a real .xlsx (inline strings); grid held"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Unique",
            "value": 112000136,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same file and reader, every text distinct"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Shared",
            "value": 58223048,
            "range": "± 536 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL/TARGET — the same values shared-string encoded, which the reader already dedups"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table over the eager grid; result held, grid released"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table through a window; result held, workbook closed"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld_Unique",
            "value": 86096872,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same projection, every text distinct"
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
          "id": "717d1ea3b61c226c608194aae5ff909fd4603455",
          "message": "Packaging: Unrect.Interactive ships as its own package\n\nThe project was already packable — id, description, tags, and a\ndependency on exactly Unrect and Unrect.Spreadsheets at the same MinVer\nversion — but the release workflow packed and asserted two packages, and\nthe README and build props named two. Now three.\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T01:12:25Z",
          "tree_id": "f01ec5a705245aededd07d5455e7b03ad4f8880d",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/717d1ea3b61c226c608194aae5ff909fd4603455"
        },
        "date": 1789796486568,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld",
            "value": 58223048,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · SpreadsheetSpace.Create over a real .xlsx (inline strings); grid held"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Unique",
            "value": 112000136,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same file and reader, every text distinct"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Shared",
            "value": 58223048,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL/TARGET — the same values shared-string encoded, which the reader already dedups"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table over the eager grid; result held, grid released"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table through a window; result held, workbook closed"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld_Unique",
            "value": 86096872,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same projection, every text distinct"
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
          "id": "4818c36f9e9b0c2b75a4093dbbce6981329fd14b",
          "message": "Merge feature/scaffold-shapes: ScaffoldRecord and ScaffoldClass, on a sheet and as leaves\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T15:35:04Z",
          "tree_id": "d48076b68bb0a93dc6ab6797b10f0ead882156a7",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/4818c36f9e9b0c2b75a4093dbbce6981329fd14b"
        },
        "date": 1789847940537,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld",
            "value": 58223048,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · SpreadsheetSpace.Create over a real .xlsx (inline strings); grid held"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Unique",
            "value": 112000136,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same file and reader, every text distinct"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Shared",
            "value": 58223048,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL/TARGET — the same values shared-string encoded, which the reader already dedups"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table over the eager grid; result held, grid released"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table through a window; result held, workbook closed"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld_Unique",
            "value": 86096872,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same projection, every text distinct"
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
          "id": "bd027f2136cee944da88c1eae17a06b14a05579d",
          "message": "Merge feature/bind-from-row: Table<T> fills a member from the row - Column(member, row => ...)\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>",
          "timestamp": "2026-09-19T19:49:16Z",
          "tree_id": "88c877501aeddcefb436f136842b9904cda55a09",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/bd027f2136cee944da88c1eae17a06b14a05579d"
        },
        "date": 1789863955648,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld",
            "value": 58223048,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · SpreadsheetSpace.Create over a real .xlsx (inline strings); grid held"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Unique",
            "value": 112000136,
            "range": "± 536 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same file and reader, every text distinct"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Shared",
            "value": 58223048,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL/TARGET — the same values shared-string encoded, which the reader already dedups"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table over the eager grid; result held, grid released"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table through a window; result held, workbook closed"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld_Unique",
            "value": 86096872,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same projection, every text distinct"
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
          "id": "96b946d5920955eb81be3eab46697a3e4639e2e4",
          "message": "Merge feature/header-bands: headers of several rows, columns addressed by path, flat binding over bands\n\nCo-Authored-By: Claude Fable 5.1 <noreply@anthropic.com>\nClaude-Session: https://claude.ai/code/session_01UNe2iWN7QtdKfZXqYPymGt",
          "timestamp": "2026-09-20T03:17:51Z",
          "tree_id": "33ccb96a2f1d1cb7795dde522c6c586ee8ce479a",
          "url": "https://github.com/jasonmcboyd/Unrect/commit/96b946d5920955eb81be3eab46697a3e4639e2e4"
        },
        "date": 1789888954229,
        "tool": "customSmallerIsBetter",
        "benches": [
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld",
            "value": 58223048,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · SpreadsheetSpace.Create over a real .xlsx (inline strings); grid held"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Unique",
            "value": 112000136,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same file and reader, every text distinct"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_SpaceHeld_Shared",
            "value": 58223048,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL/TARGET — the same values shared-string encoded, which the reader already dedups"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Eager_ResultHeld",
            "value": 32319248,
            "range": "± 536 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table over the eager grid; result held, grid released"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld",
            "value": 32319784,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · Table through a window; result held, workbook closed"
          },
          {
            "name": "Unrect.Benchmarks.Retention.Streaming_ResultHeld_Unique",
            "value": 86096872,
            "range": "± 0 bytes",
            "unit": "bytes",
            "extra": "median of 3 · CONTROL — the same projection, every text distinct"
          }
        ]
      }
    ]
  }
}