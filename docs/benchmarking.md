# Benchmarking Conventions

The continuous-benchmark rig (modeled on Copse's): `src/Unrect.Benchmarks` runs 41
benchmarks in seven BenchmarkDotNet families — `Values`, `Strategies`, `Engine`, `Tables`,
`Diagnostics`, `EndToEnd`, and `Streaming` (`docs/design/streaming-spec.md` §12) — plus an
eighth leg, `Retention`, which is not a BenchmarkDotNet family at all and measures a live
set rather than a duration (its own section below). One CI matrix leg per family,
publishing trend lines to the gh-pages dashboard and (optionally) Bencher. This file
records the conventions that keep the numbers honest.

## The rules

- **One benchmark class per family, and the family is the class's FIRST
  `[BenchmarkCategory]` value.** Load-bearing twice: the workflow matrix partitions legs
  by category, and the publish job takes `head -n 1` of the family's export — a family
  split across two classes would silently publish half its rows. (`Retention` is the one
  leg with no benchmark class. It is exempt by construction, and the publish job stores it
  from its own document rather than from a BenchmarkDotNet export.)
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
  file either. **`Retention` is the one deliberate exception**, and only on its eager half: the
  change it judges lives inside `SpreadsheetSpace.Create`, which no synthetic space passes
  through, so those rows generate a real `.xlsx` into the temp directory at setup (never
  committed, cached by shape) and read it back through the actual door. Its streaming half
  keeps the rule, over the same cells, so the two doors stay comparable.
- **Per-CPU testbeds**: each leg records its runner's CPU model, and Bencher files
  results per model so thresholds learn each machine's population separately. Cross-family
  absolute comparisons are meaningless by construction; don't make them.

## The retention family: a different instrument, on the same rails

`Retention` answers the one question `MemoryDiagnoser` cannot: **how many bytes stay LIVE
when a result is held.** Allocation and retention are different quantities, and a duplicate
string is exactly the case that separates them — the duplicate is allocated by the reader
before the adapter ever sees it, so a change that dedups at adaptation time removes nothing
from `Allocated` and a great deal from the live set. A family with no retention row would
report such a change as having done nothing at all.

**It is not a BenchmarkDotNet family, on purpose.** Measuring a live set means holding
exactly one result and collecting everything else, which is the opposite of what a benchmark
engine does — it runs an operation thousands of times and keeps none of the results. And the
number is *deterministic*: same input, same bytes. There is no distribution to estimate, so
warmup, unrolling and outlier detection have nothing to earn. A custom `IDiagnoser` emitting a
`Retained` metric was the alternative and was rejected twice over: it would have to fight the
engine into one un-warmed invocation to mean anything, and neither consumer of BenchmarkDotNet's
JSON reads a custom metric (the gh-pages action reads `Mean`; the workflow's own `jq` reads
`Memory.BytesAllocatedPerOperation`).

**The conventions.**

- **One-shot, deterministic, medians not statistics.** Each scenario is built once and
  discarded (warming the code paths, and letting the fixture's transients settle), then built
  again per reading. The reading is `GC.GetTotalMemory(forceFullCollection: true)` with the
  result held, minus the same measurement taken with nothing held. Every collection is forced,
  blocking and **compacting** — a heap size over a fragmented heap counts bytes no object is
  using. Three readings, median published, spread reported. The rig's ~1 ms noise floor does
  not apply; the floor here is that **the spread must be zero or near it**. The synthetic rows
  read exactly 0 across three readings; the rows that parse a workbook read within 0.01%
  (8 KB on 112 MB), which is the reader's own internal state and not the measurement. A
  retention row whose readings disagree by more than that is a broken protocol, not a noisy
  machine.
- **Hold via the return value and `GC.KeepAlive`, and release everything else.** A scenario
  builds whatever it needs internally and returns *only* the object whose retention is the
  question. The grid under `Eager_ResultHeld` and the reader pool under
  `Streaming_ResultHeld` are gone by the time the reading is taken, deliberately: "result
  held, source released" is the shape of the question a caller asks.
- **Both doors reach their real adapter.** The change this family judges lives in the
  adapters — `SheetStore`'s chunk fill for streaming, `SpreadsheetSpace.Create`'s fill for
  eager — so the fixtures have to arrive through them. Streaming does: every `IRowSource`
  passes the store's fill, so a synthetic source exercises the real seam in milliseconds.
  Eager does not, and cannot be faked: a locally-built `GridSpace` bypasses the eager adapter
  entirely and would read **flat** under the very change it is the floor for. So the eager
  rows generate a genuine `.xlsx` into the temp directory at setup and read it back through
  `SpreadsheetSpace.Create`. **A floor that cannot move is not a floor.**
- **Every duplicated row has a `_Unique` control.** The fixture flavours hold the same number
  of strings, of the same lengths, and differ *only* in how many distinct values those strings
  spell (indices are formatted to a fixed six digits so the lengths cannot drift). Today each
  pair must therefore read identically — and it does, to the byte. A dedup change must move
  the duplicated rows and leave the controls flat. **That contrast is what distinguishes the
  mechanism from general drift**, and it is why the controls are published rather than checked
  once and thrown away.
- **The eager door's duplication depends on how the file spells its text, so the family
  brackets both** rather than picking one and hoping. Measured against ExcelDataReader 3.7:
  a *shared-string* cell (`t="s"`) gets the table's own instance back, so equal cells already
  share and there is nothing left to intern; an *inline-string* cell (`t="inlineStr"`) gets a
  fresh instance every time. A real Excel export is **both** — the local scrubbed K-1 holds
  9,049 text cells over 2,876 distinct values in 4,016 instances, because its 8,572
  shared-string cells collapse to the table's 2,873 entries while its 3,731 formula-result
  cells (`t="str"`) materialise fresh per cell exactly as an inline string does. The eager
  door's duplication is therefore real but *partial*, and its size is a property of how
  formula-heavy the sheet is. `Eager_SpaceHeld` is the inline case (the most an interner can
  ever remove) and `Eager_SpaceHeld_Shared` is the other end — which makes that row both a
  control that must stay flat and **the target, priced on the same cells and charted beside
  the floor it is the destination for**.
- **Check the fixture, not just the bytes** — the retention reading of the rig's
  check-outputs-not-timings rule. The warm build asserts the row count and the distinct-value
  count, so a fixture that quietly stopped repeating cannot make a dedup change look like a
  no-op with a perfectly plausible number. The *instance* count (250k today, one per equal
  string) is **printed and never asserted**: driving it down to the distinct-value count is the
  change this family exists to judge, and an assertion on it would fail the day it landed.
- **What it exists to judge**: adapter-level value interning — repeated strings sharing one
  instance. `Eager_SpaceHeld`, `Eager_ResultHeld` and `Streaming_ResultHeld` are the "before"
  floor; the other three rows are the controls that say whether a movement was the mechanism.
- **What it does not measure, said out loud**: the reader's own shared-string table, which the
  eager door drops when `Create` returns and the streaming door never builds. On a real
  text-heavy sheet *held open* that table is itself a large retained object which no window
  bounds. These are the grid's and the projection's retention, not the process's. The
  generated workbooks are also the rig's one deliberate exception to "no files" — they are
  written to the temp directory at setup, cached by shape, and never committed.

**How it rides the same rails.** It is a matrix leg like every other family, it records its
runner's CPU model like every other leg, and it emits the same
`{name, unit: "bytes", value}` document every family's *memory* rows are already stored and
charted as — so nothing in the ingest pipeline changed to carry it. On the dashboard it is its
own suite, `Retention`, and its own metric, **Retained**, kept apart from the `… Memory`
allocation suites because a ~100 MB live set and a per-operation allocation are not comparable
numbers. Its Bencher measure is `retention` with a *percentage* model (10%), not the families'
`t_test`: a statistical test over a zero-variance population is meaningless where "more than
10% above baseline" is exactly the question.

## Running locally

```
cd src/Unrect.Benchmarks
dotnet run -c Release -- --allCategories Values --job short
dotnet run -c Release -- --retention                 # the whole retention family, ~65s
dotnet run -c Release -- --retention --repeats 1     # faster, no spread to check
dotnet run -c Release -- --retention --rows 6000     # a fast probe; prints but REFUSES to write
```

Local runs use ShortRun (fast, indicative); CI uses Job.Default (slow, publishable). Do
not paste local numbers into discussions as if they were CI numbers. The retention family is
the exception that proves the rule: it is deterministic, so a local reading and a CI reading of
the same commit differ only by runtime version — but it is still a *different* machine's
runtime, so trend lines stay CI's.

## How a change gets judged (the representation-decision workflow)

1. Master's trend line is the baseline — every push to master re-measures.
2. Put the candidate change on a branch; run the *Continuous Benchmarking* workflow
   against that branch via workflow_dispatch. Bencher files the results under the branch
   name, forked from master's population, and answers branch-vs-master per benchmark.
   The gh-pages dashboard stays master-only by design.
3. Decide on the comparison, merge, and the trend line absorbs the new normal.

A retention change is judged the same way with one addition: **read the `_Unique` controls
first.** A duplicated row that fell while its control fell with it is drift, a fixture change
or a runtime change — not the mechanism. The claim is only supported when the duplicated rows
move and the controls do not. For a change judged this way, a local
`dotnet run -c Release -- --retention` on each side is a legitimate first read, because the
number is deterministic; CI's run is what goes on the trend line.

## Curiosities on record

- `Map_WithDiagnostics / Map_Plain ≈ 0.98` at rig-build time: the diagnostics channel is
  free on a clean parse.
- `ShapeException_Render` measures a realistic failing parse (header + summary + first
  series parse before the failure), not isolated render cost — read it against
  `Map_Plain`.
- `Values.Create_FromInts` allocating ~96 MB/op (class-`CellValue` era) is the number the
  representation work targets; its trend line is the decision's receipt.
- **The retention floor, recorded the day it was measured** (2026-09-04, local, .NET 8.0.419,
  250k x 8 fixture, before any interning work):

  | row | bytes | MB | instances / values |
  |---|---|---|---|
  | `Eager_SpaceHeld` | 112,000,168 | 106.8 | 249,999 / 5,000 |
  | `Eager_SpaceHeld_Unique` (control) | 112,000,168 | 106.8 | 249,999 / 249,999 |
  | `Eager_SpaceHeld_Shared` (control + target) | 58,223,080 | 55.5 | 5,000 / 5,000 |
  | `Eager_ResultHeld` | 86,096,872 | 82.1 | 249,999 / 5,000 |
  | `Streaming_ResultHeld` | 86,096,872 | 82.1 | 249,999 / 5,000 |
  | `Streaming_ResultHeld_Unique` (control) | 86,096,872 | 82.1 | 249,999 / 249,999 |

  Four things worth keeping. **A `_Unique` control matches its twin to the byte**, which is the
  fixture's fairness proved rather than asserted: the duplication is real and today costs
  exactly what uniqueness costs, which is what "nothing is shared" means. **The target is on
  the chart** — `Eager_SpaceHeld_Shared` says the reader's own dedup takes the same cells from
  112.0 MB to 58.2 MB, so a 48% cut is what a complete eager interner is worth on this shape,
  and anything short of it is unfinished rather than failed. **The two doors' result rows are
  byte-identical**, which is streaming's promise stated in the metric: the same result to the
  byte, arrived at without ever materialising the grid the eager door retains. And **the eager
  grid splits about half and half** — 48 MB of `CellValue` cells against 64 MB of strings — so
  string dedup is the largest single lever this shape offers.
- **The same floor with interning in** (2026-09-04, local, .NET 8.0.419, same machine and
  fixture — **local-run figures, pending the next CI point**):

  | row | before | after | MB | change |
  |---|---|---|---|---|
  | `Eager_SpaceHeld` | 112,000,168 | 58,223,080 | 55.5 | −48% |
  | `Eager_SpaceHeld_Unique` (control) | 112,000,168 | 112,000,168 | 106.8 | flat |
  | `Eager_SpaceHeld_Shared` (control + target) | 58,223,080 | 58,223,080 | 55.5 | flat |
  | `Eager_ResultHeld` | 86,096,872 | 32,319,784 | 30.8 | −62% |
  | `Streaming_ResultHeld` | 86,096,872 | 32,319,784 | 30.8 | −62% |
  | `Streaming_ResultHeld_Unique` (control) | 86,096,872 | 86,096,872 | 82.1 | flat |

  Read the controls first, as the workflow says, and all three are flat to the byte — so the
  movement is the mechanism and not drift. `Eager_SpaceHeld` landed on
  `Eager_SpaceHeld_Shared` exactly: the target was the reader's own dedup, and the number is
  the target's, not near it. The two result rows stayed byte-identical to each other while
  both fell by 62%, which is the doors' equivalence surviving the change rather than being
  restated after it.
- **`Streaming`'s honesty caveat, read every time the family's numbers come up:** its
  fixture is a synthetic `IRowSource`, so an "open" there is free. The adversarial
  benchmarks (`Adversarial_OneReader` vs `Adversarial_Pooled`) measure only the
  *repositioning* half of the reader pool's value, never the ExcelDataReader open
  (~5s on the 1M-row probe workbook, §1.1 of the streaming spec) that the pool exists to
  overlap — that half is deliberately measured nowhere in CI.
