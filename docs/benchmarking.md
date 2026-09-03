# Benchmarking Conventions

The continuous-benchmark rig (modeled on Copse's): `src/Unrect.Benchmarks` runs 41
benchmarks in seven families — `Values`, `Strategies`, `Engine`, `Tables`, `Diagnostics`,
`EndToEnd`, and `Streaming` (`docs/design/streaming-spec.md` §12) — one CI matrix leg per
family, publishing trend lines to the gh-pages dashboard and (optionally) Bencher. This
file records the conventions that keep the numbers honest.

## The rules

- **One benchmark class per family, and the family is the class's FIRST
  `[BenchmarkCategory]` value.** Load-bearing twice: the workflow matrix partitions legs
  by category, and the publish job takes `head -n 1` of the family's export — a family
  split across two classes would silently publish half its rows.
- **Benchmarks that must be COMPARED to each other live in the same family** and
  therefore the same runner. Shared runners are a CPU lottery (~±30% between models);
  same-run ratios are the only trustworthy comparisons. `Map_Plain` vs
  `Map_WithDiagnostics` is the canonical pair.
- **Every row must clear the ~1 ms noise floor**, or a regression can never surface
  above runner variance. When a "realistic" size measures in microseconds, retier the
  fixture and say so at the constant — do not document an exception.
- **Check fixture OUTPUTS, not just timings, when adding benchmarks.** Both fidelity
  bugs found while building the rig (a sparse fixture whose all-blank rows truncated
  scans; a kind-cycle resonance blanking two columns in every row) produced plausible
  timings of the wrong thing.
- **Fixtures are GridSpace-built synthetics** (`CanonicalSpaces`, `IrrReport`) — CI
  runners get no workbooks. The 1M-row xlsx load measurements live outside the rig as
  scratch probes; the rig measures the layers we control. `Streaming`'s fixture keeps the
  same rule a different way: a synthetic `IRowSource` (`StreamingSpaces`) stands in for
  ExcelDataReader, so the family measures the window and the reader pool without a real
  file either.
- **Per-CPU testbeds**: each leg records its runner's CPU model, and Bencher files
  results per model so thresholds learn each machine's population separately. Cross-family
  absolute comparisons are meaningless by construction; don't make them.

## Running locally

```
cd src/Unrect.Benchmarks
dotnet run -c Release -- --allCategories Values --job short
```

Local runs use ShortRun (fast, indicative); CI uses Job.Default (slow, publishable). Do
not paste local numbers into discussions as if they were CI numbers.

## How a change gets judged (the representation-decision workflow)

1. Master's trend line is the baseline — every push to master re-measures.
2. Put the candidate change on a branch; run the *Continuous Benchmarking* workflow
   against that branch via workflow_dispatch. Bencher files the results under the branch
   name, forked from master's population, and answers branch-vs-master per benchmark.
   The gh-pages dashboard stays master-only by design.
3. Decide on the comparison, merge, and the trend line absorbs the new normal.

## Curiosities on record

- `Map_WithDiagnostics / Map_Plain ≈ 0.98` at rig-build time: the diagnostics channel is
  free on a clean parse.
- `ShapeException_Render` measures a realistic failing parse (header + summary + first
  series parse before the failure), not isolated render cost — read it against
  `Map_Plain`.
- `Values.Create_FromInts` allocating ~96 MB/op (class-`CellValue` era) is the number the
  representation work targets; its trend line is the decision's receipt.
- **`Streaming`'s honesty caveat, read every time the family's numbers come up:** its
  fixture is a synthetic `IRowSource`, so an "open" there is free. The adversarial
  benchmarks (`Adversarial_OneReader` vs `Adversarial_Pooled`) measure only the
  *repositioning* half of the reader pool's value, never the ExcelDataReader open
  (~5s on the 1M-row probe workbook, §1.1 of the streaming spec) that the pool exists to
  overlap — that half is deliberately measured nowhere in CI.
