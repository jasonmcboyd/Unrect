# The push interpreter: what each machine does with a span

**Status.** Accepted by the owner, 2026-09-18, after two revisions in review (§5.1 added; §8
reframed around one door). **Phase 3 landed the same day** on `experiment/engine-split` (commits
3a–3c): every one of the twenty-two shipped nodes builds its machine through `Build`, beside
`Project`, and a root-level switch (`UNRECT_PUSH=1`, or `ProjectionEngine.UsePush()`) runs any
application on the push interpreter. Sixty-three equivalence theories
(`src/Unrect.Tests/Machines/PushEquivalenceTests.cs`) observe a declaration through both engines
and compare at L3. The whole suite under push passes 2,625 of 2,695; the 70 that do not all test
the pull engine's own mechanics — rows touched, high-water marks, deferred scans, sweep
announcements, window overruns, read ordering — and one matcher rescan (below); phase 4 sorts
them into "retires with pull" and "must pass". Built differently from the text below:

- `Settlement<T>` carries `Size Consumed`, both dimensions, not an along-axis count (§2.3): a parent
  across the machine's axis needs the other one, and a derived width is the node's, not the placement's.
- `Axes` is a flags enum — `None`, `Vertical`, `Horizontal`, `Either` — rather than a nullable
  orientation (§3); a wrapper announces its inner's axis, or `Either` when the inner announces `None`.
- `ProjectorScope<TSpace>` is generic over the space, and in this phase wraps the pull engine's
  `ProjectionContext` for tree position and diagnostics, so the views keep taking a context until
  phase 5; its members are internal until the node classes go public.
- The placement machine (`ChildProjector`) streams the placements the vocabulary itself builds —
  explicit offsets and sizes, row landmarks, skip-while rows, the first non-blank cell, chained
  offsets, rows-while-any, the interleaved block, rows-then-columns — and holds any other child
  whole, placing it at `Close` with the pull engine's own placement (`EagerPlacement`) and
  re-driving it along its own axis. Under a column-span driver only explicit offsets and sizes,
  column predicates, column landmarks and the first non-blank cell stream.
- The table rungs — `Table(view => …)`, `Table(row => …)`, `Table()` and the bind rung — are
  one-span machines over their held region (§6.2, §6.5 traced them streaming); a streaming bind
  rung is a later optimisation, not a contract change. A repeat whose separator has no per-span
  form is held the same way.
- Every leaf and view is one machine, `SpanCountProjector`: take the spans, then read the region
  they cover as `Project` always has. Under a declared area it takes them all, so a leaf forced
  wider than its kind still says so.
- A landmark is handed the region searched so far, as it is handed the extent today, which rescans
  earlier spans on each new one (`TypedPredicateLoweringTests` sees the predicate called once more);
  bounded by the seek's length, and the one place a per-span form of `IRowLandmark` would pay.

**Phase 4, step 1 landed 2026-09-18:** the harness proper. `[PullOnlyFact]`/`[PullOnlyTheory]` skip a
test of the pull interpreter's own mechanics under `UNRECT_PUSH=1`, with the reason in the skip
message; all 44 methods that failed under push were of that kind and are marked; `PushMatcherRegionTests`
is the one push-side twin so far. Both runs are green (pull 2,697; push 2,624 passed, 44 skipped), and
CLAUDE.md's gate runs the suite both ways. Steps still open: the buffer manager and a non-retaining
source (§2.4, §8), `PathRenderer` and the scope's own diagnostics (§7), `Unrect.Engine`, the dry-run
cost report (§3).

**Phase 4, steps 2–5 landed 2026-09-18** (commits `278f393`, `427c3b7`, `916640a`, `dfccfe0`); phase 4
is complete and both runs are green (pull 2,712; push 2,639 passed, 44 skipped). Built differently from
the text below:

- **The buffer manager asks, it is not told** (§2.2, §2.4). No hold is opened when a child starts.
  Instead every placement machine registers with the session while open, and after each row the
  session asks each one the oldest row it may still read — `IRetaining.RetainFrom` — and releases
  everything before the minimum. What a machine holds is its node's `Retains` (internal on
  `DefinitionNode`: one span for a cell, the header rows for column labels, the stride for bands, the
  extent for a block, a choice or a fallback), or, for a repeat, the attempt in progress through
  `IHolding.HeldFrom`: a committed occurrence is final, so a walk over a thousand occurrences retains
  one. A machine still resolving an offset or a width, or held whole, retains from where it began.
  The cap is `WorkbookOptions.BufferRows`; exceeding it is a fault naming the *innermost* holder of the
  oldest row, since an enclosing boundary holds whatever its child holds.
- **The source contract is `IRowFeed`** in `Unrect`: `Advance`/`Loaded`/`Retained`/`Cap`/`Release`. A
  space that implements it is driven one loaded row at a time; any other space is a retaining source
  and is driven over its whole extent as before. `StreamedSheet` (`Workbook.Stream(name)`) is the
  workbook's feed — a fresh cursor per call, rows in a deque, a cell of a released row a located
  `CellReadException` that says to read it inside the projection, a cell of a disposed book an
  `ObjectDisposedException`. `MapWorkbook` streams under push and keeps the windowed `Sheet` under pull;
  `Workbook.Sheet` itself is untouched until phase 5.
- **A collector streams under a declared rule.** `Range(…)` and `Table(view => …)` announce no axis,
  but under a size rule the engine can run per span (the discovered block, rows-while-any, an explicit
  size) they need not bound themselves, so they are driven by the rule and read the region whole at
  close — the fold the pull placement ran, one row at a time. Only a collector left to bound itself, or
  one under a rule with no per-span form, is held. A width that does not fit is reported once the rows
  have settled, so a declared 3x3 over a 2x2 space still says "2x2 available".
- **`PathRenderer` lives in `Unrect`, not the engine** (§7): every node's machine names it, so it is
  declaration-side by the same test as `Spans` and `PlacementRules` (a repeat steps its own separator
  rule). It is a static class over the data face; `ProjectionContext` keeps only the chain walk.
- **`Unrect.Engine` holds** `PushSession`/`SessionScope` (the driver, the feed loop, the buffer
  manager), `ChildProjector` (the placement machine), `EagerPlacement` (the held-child fallback) and
  `ProjectionMapping` (`Map`/`Apply`/`MapWithDiagnostics`, namespace `Unrect.Projections`). A node's
  machine holds an `IChildHandle<TSpace, T>`, the typed handle `ProjectorScope.Start` returns, never the
  engine's class. Packaging follows the graph: the engine project produces the "Unrect" package and
  bundles `Unrect`, `Core`, `Strategies` and the analyzers (the reverse of engine-split §10 decision 11,
  which would have been a reference cycle); the `Unrect` project carries the restore identity
  `Unrect.Definitions` so the two never collide, and the doc ratchet names it explicitly.
- **The cost report is `CostReport.Of(definition, driver)`** in `Unrect`, over the data face: a line
  per node — path-segment name, streams or holds and why, `Reach`, `Axis` — with a held node's children
  reported under the axis it is re-driven along. It and `ChildProjector` ask one function,
  `PlacementRules.Streams`, so they cannot disagree. `Orientation` is public now that a driver is the
  subject of a public signature.

**Phase 5 landed 2026-09-18** (commits `d16e39c`, `7628060`, `b62ddf2`, `7969754`, `e1120ff`): the
demolition. Map runs the push session only; a repeat whose separator has no per-span form
gathers and walks at close rather than falling back; `Project` left the contract and every node,
with a collector's read becoming `Collect` and the bind rung the labelled composite with a late
body; `ProjectionEngine`, `ProjectionResult`, `Bound`, the layout states, `ISweepAware`, `IBound`
and the plane's lazy bottom edge are gone; `Workbook.Sheet` is one forward pass over its own
cursor, and the window, chunk store, reader pool and their counters are gone with the 65 tests of
them. Two more engine corrections came out of the store's demolition: a collector under a declared
rule is driven along whichever axis the rule runs (`DefinitionNode.Collects`), and it retains every
span it collected. Two tightenings followed the same day: retention folds up the tree (each handle answers for its
subtree; the session asks the root; no registry), and `ProjectionContext` merged into
`ProjectorScope` — a scope is one immutable object holding its `TreePosition`, the diagnostics, the
labels and the engine's services, and the views take it. Then §4 as the owner drew it (`2ba3f0d`): a strategy builds its
own machine. Every Core strategy contract is one member, `Begin(Orientation)` (or `Begin()` for a
row or column rule), handing back an `IOffsetScan`/`ISizeScan`/`IRowScan`/`IColumnScan`; a scan says
whether it streams, and one that does not takes every span and settles at the end, which is what the
engine holds a child for. The whole-region answers are the folds in `Scans`, kept as extension
methods so the strategy suites keep their spelling; `PlacementRules` no longer recognises a strategy
by its type, and the three incremental interfaces, the area scan and the rule classes are gone. Then
`EagerPlacement` too: a held child replays its buffered spans through the one `StreamingPlacement`
at close and drives its machine along its own axis, so §4's placement machine is the only one. What survives of the pull era: nothing that runs. Suite:
2,340 tests, 18 analyzer tests.

The rulings in §0 were settled with the owner in the session that produced this document; the
decisions in §11 were accepted as recommended.

`engine-split.md` fixed the shape: a definition builds its own machine (`Build`), the machine is
`bool Next(span)` and `Close()`, buffering is announced upward, and `Unrect.Engine` holds only the
driver. This document fixes the behaviour: what every shipped node's machine does with a span, when
it answers false, what it does at `Close`, and what it announces. Vocabulary, one verb corrected in review: **builders** make **definitions**; a definition
**builds** its **projector** when the engine asks (`Build`), and the engine **drives** it; a driven
projector yields **the projection**. A **span** is what the driver offers: a `Plane<TSpace>` one row tall under a row-major
driver, one column wide under a column-major one.

---

## 0. Rulings (settled)

1. **Consumption is final, except under a boundary that can absorb.** A `true` from `Next` means
   the row is taken. A failed seek — an anchor that never appears — is a *failure*, absorbable and
   propagating, never "the successor's rows". Rows go to a successor in exactly three places, all
   declared: a repeat absorbing its item's failure to find the next occurrence; `Else`/`Optional`
   absorbing a failure and consuming nothing; `Choice` abandoning a losing alternative. Fault stays
   what it is today: broken code, never absorbed.
2. **A hold is the engine's savepoint, not the machine's buffer.** When a parent starts a child that
   announced it may settle short, the engine records the current row index as that child's hold and
   retains rows from the oldest open hold. Holds nest with `Start`/`Close`. A machine never keeps a
   row; it announces before it starts and reports how many rows it settled on when it closes.
3. **The parent replays, locally.** When a child settles short, the parent that fed it asks the
   scope for the shortfall from the buffer and feeds those rows to the successor before continuing.
   The root's state is never unwound and the driver never rewinds.
4. **A machine is driven along its own axis only.** The engine compares a node's announced axis to
   the driver's. Same axis: spans pass straight through. Different, or none: the engine holds the
   node's extent until its placement closes it, then re-drives the node span by span from the
   buffer, along the node's axis. A held node is the same machine, fed later. No node branches on
   the driver's axis.
5. **Two kinds of buffering.** *Declared*: a static function of the definition and the driver's
   axis, readable from the tree with no file (§3). *Discovered*: the amount every hold retains,
   and, for one shipped node, the kind — the bind rung, whose row projection exists only once a
   header has been read. One mechanism serves both; the difference is what a dry run can say.
6. **Choice is sequential.** Running alternatives concurrently does not remove the hold (the
   driver would run ahead for the slower ones and the winner's refusal point would still need
   replay), so a choice holds, tries alternatives in declaration order, and rolls back per attempt
   — today's semantics exactly.
7. **A hold is also the diagnostics mark.** Today's `Diagnostics.Mark()`/`Rollback(mark)` at every
   boundary is the same savepoint over a different stream. Under push they are one object: rolling
   a hold back rolls back the diagnostics recorded inside it.

---

## 1. The driver

A **session** owns one application: the root projector, the buffer, the diagnostics collector, and
the row source. `Map(space)` is a session fed from the space's rows and closed. A consumer never
holds a projector.

```
start root projector (recursively starts the tree; every announcement joins upward)
for each span from the source, in order:
    offered := root.Next(span)
    if not offered: the root is finished; stop feeding      (trailing rows → unconsumed-space report)
    drop every buffered row older than the oldest open hold
settlement := root.Close()
```

Inside the tree the same loop is the parent's `Next`: offer the span to the current child; on
`false`, close the child, take its settlement, move to the successor and re-offer the same span; on a
short settlement, replay the shortfall from the buffer into the successor first (§2.3). A parent
answers `false` when it has no successor left. `Close` is the only way to a value: a child that
ended by refusing is still closed by its parent, and a child that was never offered a span is closed
on nothing and answers for itself (a leaf fails "expected a cell here"; an `Optional` yields its
default).

**The root.** Its extent is the whole space, so its spans are full-width rows. Its settlement is
what `MapWithDiagnostics` reports as consumed; the rows it refused, or was never offered because it
finished, are the unconsumed space.

**Span, not row.** Every span is a `Plane<TSpace>` over the buffer-backed space, with the sheet's own
origin. A parent cuts a child's slice of it with `Slice`, which is arithmetic; a leaf mints a
`Point` from it; a strategy reads it. Nothing in Core is touched, and nothing is added.

---

## 2. Holds

### 2.1 Announcing

```csharp
/// <summary>How far back the engine may need to reach for this machine: none, a fixed number of
/// spans, or as far as its extent runs. Joins upward by maximum.</summary>
public readonly struct Reach
{
  public static Reach None { get; }            // what I accept, I keep
  public static Reach Spans(int count);        // a pad's bottom, a fixed-size item
  public static Reach Extent { get; }          // up to my own extent, however long that turns out
  public static Reach Max(Reach a, Reach b);
}
```

`Reach` is a property of the *definition*, computed at `Start` from the children's and the node's own
rule, and it is the child's promise: `None` means the parent need never open a hold for it. It is
`Extent` rather than `Unbounded` because no machine ever reaches past the extent its placement gave
it; "unbounded" was the wrong word for "as long as my parent keeps feeding me".

### 2.2 Opening and releasing

The scope opens a hold when a parent starts a child whose reach is not `None`, and releases it when
that child closes. That is the whole protocol; a node's code never touches it. Two consequences the
document leans on:

- Holds nest. An inner hold never reaches below an outer one, and releasing the inner keeps the
  outer's rows. A repeat's item inside an `Optional` inside a repeat is three nested holds and needs
  no special case.
- A repeat releases per committed occurrence, so in steady state it retains one gap plus one seek.
  Only at the end does it retain the tail it hands back.

### 2.3 Settling short

```csharp
public readonly struct Settlement<TResult>
{
  public TResult Value { get; }
  public int Settled { get; }        // spans kept, along the machine's own axis
  public Presence Presence { get; }  // Read / Empty / Absorbed, as today
}
```

A machine that took *n* spans and settles on *k* < *n* has handed *n − k* back. The three places
that do so and what they settle on:

| Boundary | Settles on | Then |
|---|---|---|
| Repeat, item failed to place | rows through the last committed occurrence | the repeat refuses; its parent replays the tail to the successor |
| `Else`/`Optional`, inner failed | 0 | fallback projection replayed from the hold, or the value with `Absorbed` |
| `Choice`, alternative failed | 0 for that attempt | next alternative replayed from the hold; all failed → failure |

Everything else settles on exactly what it took. A repeat's own reach is its item's, so a shortfall
propagates through it: reach joins upward for exactly this reason.

### 2.4 The buffer

One buffer per session, retaining from the oldest open hold. It grows to what the holds require;
with no open hold it retains nothing and the source is a forward pass. One optional cap, the honest
successor of `WorkbookOptions.WindowRows`: exceeding it is a failure naming the node and the axis
that asked, never a collapse. Statistics report rows read, the high-water mark, and holds opened —
the vocabulary to act on, replacing `ChunkReloads`/`WindowOverruns`/`Reopens`.

---

## 3. Axes

A node announces one thing about orientation, statically: the axis it streams along, or none.

| Announces | Nodes |
|---|---|
| Vertical | `VerticalFlow`, `VerticalRepeat`, `VerticalBands`, `Labelled` (top header), `ColumnLabels`, `Record`, `Row(…)` strip, `Table(row => …)`, `Table()`, the bind rung, `Caption`, `Field`, `Fields` |
| Horizontal | `HorizontalFlow`, `HorizontalRepeat`, `HorizontalBands`, `Column(…)` strip, a header-side table when it exists |
| Either | `Point`, `Read` (one cell is one span either way), `Nothing`, the wrappers (`Select`, `Pad`, `Bounded`, `Fallback`, `Unit`, `WithLabels`: their child's), `Choice` (its alternatives' join), `Overlay` (its children's join) |
| None | `Range(…)` block, `Table(view => …)`, a pivot when it exists |

**Same axis: stream. Different or none: hold and re-drive.** The engine's placement machine (§4)
runs along the driver's axis in every case, so it knows where a held node's extent ends; at that
point the hold closes and the node is driven span by span from the held region along its own axis.
`Column(c => …)` under a row-major driver is the small case: a one-wide band held until its height
settles, then fed once as a single column span.

**Overlay streams; a horizontal flow under a row-major driver does not.** The difference is
dependence. An overlay's children are independent — each is placed from the overlay's origin and
reads what it reads — so every span is offered to every open child, each with its own placement
machine, and the overlay refuses when all have closed. A flow's children are dependent — the second
starts where the first's settlement ends, and that is known only at the first's `Close` — so a flow
across the driver's axis holds. A tall horizontal flow of declared-width children could be streamed
by slicing each span across concurrent children; recorded as an optimisation, not built.

**The declared cost table** (the dry-run deliverable of phase 4's tooling face): reach and axis per
node for a given driver, a pure function of the definition tree and one enum. For the row-major
driver being built: a top-header table streams; a horizontal flow of discovered widths holds its
band; a column landmark (`RightOf(ColumnContaining(…))`, `UntilColumn`) holds its enclosing extent;
a block lambda holds its extent; alternation and repetition hold as §2 says. A column-major driver
would invert exactly which shapes hold, with no change to any node.

---

## 4. Placement as a machine

Today `ProjectionEngine.TryPlace` resolves an offset and an area eagerly, over a plane; the lazy
bound (`IBound`, `Bound`, the incremental scans) exists so that a discovered height need not be
measured before it is consumed. Under push, placement *is* a machine the engine wraps around every
child it starts, driven along the driver's axis, in two phases:

| Phase | Rule | Behaviour per span |
|---|---|---|
| Offset, along | `Explicit(n)` / `SkipWhile(p)` / `To(landmark)` / `Past(landmark)` | skip *n* spans / skip while the span satisfies *p* / skip until a span matches, then start on it (or after it). A seek that reaches the end of the parent's feed without a match is a failure, absorbable, blamed on the child |
| Offset, across | explicit *n* / a column landmark | slice each span from column *n* / **hold**: the column is found only over the whole extent |
| Size, along | `Explicit(h)` / `While(p)` / derived | take *h* spans / take while *p* holds, refuse the first that does not / the child's own refusal decides |
| Size, across | explicit *w* / full available / content-discovered | slice each span to *w* / to its width / **hold** |

**What this replaces.** `IBound`, `Bound`, `Plane._bound`, `HasRow`-versus-`Area` forcing: a child
that consumes rows one at a time never asks whether there is another; the parent either offers one
or does not. `ProjectionResult.Consumed` (a `Size`): the along dimension is `Settlement.Settled`,
and the across dimension is the placed width, which the placement machine knows. `Presence` stays,
same three values, same meaning.

**The strategy calculus becomes single-form.** Today a strategy is read two ways — eagerly over a
plane, and incrementally as a fold (`IIncrementalRowStrategy`, `Scans.Fold*`), with the
fold-identity suite pinning that they agree. Under push the per-span form is the only form the engine
runs, because a held region is re-driven per span too; the eager reading is never needed by the
engine. So `engine-split.md` §5.4 is refined rather than contradicted: the *duality* dies, and it is
the eager half that goes. The incremental half is renamed to the plain names and becomes the
calculus. The strategies' own orientation follows §3: a rule along its own axis, re-driven when the
driver's differs. `Unrect.Interactive`, which reads a sheet directly, is unaffected.

---

## 5. The contract, refined

```csharp
public interface IProjector<TSpace, TResult> where TSpace : class, ISpace
{
  bool Next(Plane<TSpace> span);      // true: taken. false: not mine, and I am finished.
  Settlement<TResult> Close();         // the value, the spans kept, the presence
}

public interface IProjectionDefinition<TSpace, TResult> : IProjectionDefinition
{
  Reach Reach { get; }                 // computed at construction from the children; joins upward
  Orientation? Axis { get; }           // the axis this node streams along, or null for none
  IProjector<TSpace, TResult> Build(ProjectorScope scope);
  IProjectionDefinition<TSpace, TResult> With(Annotations annotations);
}
```

`Reach` and `Axis` are on the definition, not the projector, because the dry run reads them without
starting anything, and because a parent decides whether to open a hold *before* it starts the child.
Both are computed once at construction and are as immutable as the rest of the node.

`ProjectorScope` gains what a parent needs and nothing a child does:

```csharp
public abstract class ProjectorScope
{
  // as engine-split.md §1.4: Enter, Occurrence, Ordinal, Failure, Reading, Report, WithLabels, TryLabels

  /// <summary>Starts a child under this scope: opens its hold if its reach asks for one, and hands
  /// back a driver-side handle the parent feeds. The parent never sees the hold.</summary>
  public abstract IProjector<TSpace, T> Start<TSpace, T>(Child edge, IProjectionDefinition<TSpace, T> definition);

  /// <summary>The spans a just-closed child took and did not keep, oldest first, for the parent to
  /// feed to the successor. Empty when the child settled on everything it took.</summary>
  public abstract IEnumerable<Plane<TSpace>> Shortfall<TSpace>(IProjector<TSpace, ?> closed);
}
```

The handle `Start` returns is the placement machine wrapped around the child's own; the parent feeds
it and never distinguishes the two. `Shortfall` is how replay stays local (§0.3).

### 5.1 Contract violations are faults

The node set is open, so a machine can be wrong, and the engine validates the protocol at the one
seam it already occupies: the handle between parent and child, which counts what it offered. A
violation is a **fault** — blamed on the node, with its path, never absorbable — in the same
vocabulary as a null reference or an index overrun. The wrapper checks:

| Violation | Fault message shape |
|---|---|
| settled more than offered, or negative | `'transactions' (Table) settled on 7 spans but was offered 5` |
| settled short under `Reach.None` | `… settled 2 spans short but announced no reach; a definition that may hand rows back must announce it` |
| settled short by more than `Reach.Spans(n)` | `… settled 4 spans short but announced a reach of 2` |
| `true` after `false` | `… took a span after refusing one; a machine that refused is finished` |
| `Next` after `Close`, or `Close` twice | `… was fed after it was closed` / `… was closed twice` |

What the wrapper does not check, because it cannot: whether the value is right, or whether the
`Presence` is honest. A wrong one of those is a wrong result, which is the node's own truth to get
right, not a protocol error. The checks cost a counter and a state flag per open child.

---

## 6. The traces

For each node: what `Next` does with a span, when it answers false, what `Close` does, and its
`Reach`. Axis per §3. "Placement" means the engine's machine of §4, which runs before the node's
`Next` sees anything and slices the span to the child's width.

### 6.1 Leaves — one span

| Node | `Next` | `false` when | `Close` | Reach |
|---|---|---|---|---|
| `Point` | takes the 1×1 span | it already has one | the point | None |
| `Read` (`AsText`, `Text`, `Decimal`, …) | takes the 1×1 span | it already has one | blank and `BlankIsNull` → default; else `Read(cell)` or the located `CellProblem` | None |
| `Caption` | takes the one-row span after placement seeks the row; verifies a cell matches | it already has one | the matched cell's text | None |
| `Field` | takes the 2×1 span after placement anchors on the label; verifies `[0,0]` | it already has one | the point at `[1,0]` | None |
| `Nothing` | never takes a span | always | default, settled 0, `Empty` | None |

A leaf declared 1×1 whose placement offers it more than one span is a placement contradiction that
cannot arise: the size rule is `Explicit(1)`. The "must be exactly one cell" failure survives only for
the across dimension, when a caller replaced the placement.

### 6.2 Views — a lambda over cells

| Node | Axis | `Next` | `Close` | Reach |
|---|---|---|---|---|
| `Row(r => …)` strip | V | takes the one span | lambda over the `CellStrip` | None |
| `Column(c => …)` strip | H | takes the one (column) span — held under row-major until the height settles | lambda over the strip | None (the hold is the engine's, from the axis) |
| `Range(b => …)` block | none | held; then takes the re-driven spans of its extent | lambda over the `CellBlock` | Extent |
| `Table(view => …)` | none | as `Range`; header rows counted off the top | lambda over the `TableView` | Extent |
| `Table(row => …)`, `Table()` | V | header spans → `LabelMap`; each body span → the row lambda / the dictionary, blank policy per span | the list | None |
| `Record` | V | takes the one row span | lambda over the `TableRow`, ordinal from the scope | None |
| `ColumnLabels` | V | takes the header span(s) | `LabelMap.FromHeader` | None |

The view lambdas keep raising exactly the failures they raise today; `CellStrip`/`CellBlock`/
`TableRow`/`TableView` take a `ProjectorScope`. The row rungs stream because a `TableRow` is one span;
the view rung does not because a `TableView` is random access by contract.

### 6.3 Layouts

**`VerticalFlow`** (along a row-major driver). Children in declaration order, one open at a time.
`Next`: offer the span to the open child; on `false`, close it into its slot, start the next child
(§5 `Start`, replay any shortfall into it, §2.3), re-offer. `false` when the last child has refused.
`Close`: close the open child, then close each never-started child on nothing; run the combiner over
the slots. The sibling note (a child failing on the very span its predecessor refused having settled
on nothing) is raised where it is today, by the flow, because the flow is the one that knows both
facts. Reach: max over children. Presence: `Read` if any child read, else `Empty`.

**`HorizontalFlow`** (across a row-major driver): held to its extent, then the same machine driven
along columns. Under a column-major driver it is the streaming one and `VerticalFlow` is held.

**`Overlay`**: every span to every open child, each behind its own placement machine; a child that
refuses is closed into its slot; `false` when all have closed. `Close`: close the rest on nothing;
combiner. Settled: the furthest any child reached. Reach: max over children.

**`Fields`**: a `VerticalFlow` of `Field` leaves, placed by the first label; nothing of its own.

**`Labelled`** (the composed table's header-then-body): a flow of two — the header definition,
then a `WithLabels` over the body with the header's `LabelMap` — with `WithLabels` started only
once the header has closed. Reach: the body's.

**`WithLabels`**: slices each span to the labelled width, pushes the label scope, forwards. Reach:
its body's.

### 6.4 Repetition and tiling

**`VerticalRepeat`**. State: the committed occurrences, and the open item if any. `Next`:

1. No open item: if a separator is declared and an occurrence has been committed, the separator's
   offset rule consumes this span if it is one of the gap (a blank row); a gap that reaches the end
   of the feed is not counted. Otherwise start an item (hold opened by the scope, since an item's
   placement may seek), and fall through.
2. Open item: offer the span. `true`: done. `false`: close it. Settled on nothing, or advanced
   nowhere: the productivity guard — the run ends, the repeat refuses this span and everything
   after. Settled on something: commit the occurrence (release), then re-offer the span to a fresh
   item as in 1.
3. An item whose *placement* fails (its seek ran out of feed, its explicit area did not fit) ends the
   run: the diagnostics recorded inside the hold roll back, the repeat settles on the rows through
   the last committed occurrence, and refuses. A failure *inside* an item propagates, as today.

`Close`: an open item is closed; if it commits, it counts. Fewer than `atLeast` → failure. Zero
occurrences → `Empty`. The "ended by tolerance" Info is reported when the item that ended the run
was `Absorbed`. Reach: the item's, joined with the separator's (`None`). `HorizontalRepeat`: the
same machine, held under row-major.

**`VerticalBands(stride, each, onBlank)`**. `Next`: accumulate `stride` spans into the current band.
On a complete band: if every cell is blank and a policy is declared, apply it (`Stop` → refuse this
and every later span; `Skip`/`Tolerate` → count the band, report if asked; `Fault` → fault); else
start the band's child, feed it the band's spans, close it, take its value. A band that does not
complete because the feed ended is not a band: the spans are settled by the tiler (they were cut out
of the extent) but no child runs. `false` after `Stop`. `Close`: the list; `Empty` if none. Reach:
`Spans(stride − 1)` joined with the child's — the tiler must see a whole band before it can say
it is blank. `HorizontalBands`: held under row-major.

### 6.5 The bind rung — `Table(headerRows, eachRow: captions => …)`

`Next`: the header spans → `LabelMap` (as `ColumnLabels`); on the last header span, call the bind
with the map to obtain the row definition — the one moment a shipped node builds a child from data,
which is what its `Opacity` says — and from then on behave as `VerticalBands(1, row, onBlank)` under
a `WithLabels` scope. A bind that returns null is a fault. Reach: `None` until the header is read,
then the row definition's; announced late, which the scope permits for exactly this node. Under a
column-major driver: held, then re-driven along rows, identically. The reflective `Table<T>` is this
rung and traces nothing of its own.

### 6.6 Alternation

**`Choice`**. Hold opened by the scope. `Next`: offer the span to the current alternative. `false` →
it closed; if it closed *successfully*, the choice settles on what it settled on and refuses from
here. A failure (non-fault) from the alternative: roll the hold back (rows and diagnostics), record
the Info line, start the next alternative, replay the held spans into it, then offer the current
span. No alternative left → the summarising failure, as today. Reach: `Extent` (it may replay
everything it was offered), joined with the alternatives'.

**`Else(fallback)` / `Optional` / `Else(value)`** — `Fallback`. Hold opened. `Next`: forward to the
inner. Inner fails (non-fault): roll back, record the Warning carrying the failure's own path, then
either start the fallback and replay into it, or, with no fallback, settle on 0 with `Absorbed` and
refuse. A fallback that fails too carries the note about what it stood in for. Reach: `Extent`.
Note the declared-area case: a boundary under `Sized(…)` or a padding still consumes the extent the
placement claimed — the placement machine took those spans, not the boundary — so `Absorbed` travels
with a non-zero settlement exactly as it does today.

### 6.7 Wrappers

| Node | `Next` | `Close` | Reach |
|---|---|---|---|
| `Select` | forward | inner's value through the selector; a `CellReadException` rethrown as located | inner's |
| `Pad` | skip `Top` spans; slice `Left`/`Right` off each; forward; after the inner refuses, take `Bottom` more | inner's | `Spans(Bottom)` joined with inner's: the pad must be able to tell the last `Bottom` spans from the inner's before the extent ends |
| `Bounded` (`Until`) | a span matching the landmark → refuse it and stop; else forward; after the inner refuses, keep taking spans up to the landmark (a bound is consumed in full) | landmark never seen: failure, or with `orEnd` the Info and run to the end | inner's. `UntilColumn`: held (axis) |
| `Unit` | forward to the body | body's value | body's |
| `WithLabels` | §6.3 | | |

### 6.8 What every trace shares

The lambda-calling nodes (`Select`, strips, blocks, `Record`, the layout combiner, the two table
rungs) catch `CellReadException` at one seam and rethrow through `ProjectorScope.Reading`, as
`ProjectionContext.Reading` does today. Every failure a node raises names the span or point it was
handed, so `ProjectionLocation` reads off the plane as it does now. Fault classification (`IsFault`)
is the driver's, unchanged, applied where a machine's `Next` or `Close` throws a foreign exception.

---

## 7. Diagnostics and paths

Tree position: `ProjectorScope.Enter(child)` at `Start`, `Occurrence(i)` per repeat item or band, as
`engine-split.md` §5.2. The use site rides on the `Child` edge, so `ProjectionContext.Pending` and
`WithUseSite` die here: `Start(edge, definition)` has the site in hand. `PathRenderer` renders the
same segments from the same facts; the diagnostics suite pins the output byte for byte, and the
two-engines harness of phase 4 runs it against both.

Rollback: a hold's diagnostics mark (§0.7). `Report` inside a hold is provisional until the hold is
released; a rolled-back hold discards them, and the boundary then records the one line that survives
(the Info per losing alternative, the Warning per absorbed failure, the "ended by tolerance" Info).

---

## 8. One door: a source of spans

Random access is not something a space provides; it is something a definition asks for, and the
mechanism is already in §2: a node that announces `Reach.Extent` has its whole extent held, and a
held region is a plane, which is random access. `Range(b => …)` and `Table(view => …)` announce it
for their lambdas. Announced at the root, it holds the whole sheet — which is what "load it eagerly"
always was, seen from the other side. So there are not two doors, an eager one and a streaming one.
There is one: **a source of spans**, and the declaration says how much of it to hold.

- **`Workbook.Sheet(name)`** narrows from vending a random-access `ISheetCells` over a window, pool
  and rewinds (`engine-split.md` §5.3's open question) to vending a *source* for `Map`: one forward
  pass, one buffer sized by the declaration's holds. `MapWorkbook(path, sheet)` is the
  one-call spelling.
- **`SpreadsheetSpace.Create`** and **`SheetGrid.Of`** stay as sources that happen to *retain*
  everything — the former also the one that carries formulas, which a stream cannot — but they are
  not "the random-access answer". No source is; the question is the definition's, at whatever depth
  it asks it, up to and including the root.
- **Points that escape** — `Point()` and `Fields()` hand back addresses a consumer reads after `Map`
  returns — are valid exactly when the source retains. Under a retaining source, always. Under a
  streamed source, a point whose row has left the buffer throws a located `CellReadException` saying
  so, rather than the buffer pinning rows for the life of the result. A declaration that wants the
  value across that line reads it inside (`AsText()`, a kinded leaf, or `Select` on the point) —
  which is what the vocabulary already recommends.

Nothing new is needed for any of this: no node, no space type, no modifier. The cost of random
access sits on the declaration, where the dry run reads it, instead of on the door.

---

## 9. What phase 3 builds first

The machines land beside `Project`, dead under test until phase 4, in this order, each a build-green
step: (1) `Reach`, `Settlement`, `Axis`, the contract; (2) the leaves and `Record`/`ColumnLabels`;
(3) `VerticalFlow`, `Select`, `Unit`, `WithLabels`, `Labelled`; (4) the placement machine and the
single-form strategies, with the fold-identity suite retargeted at "per-span equals eager" until the
eager half is deleted; (5) `VerticalBands` and the bind rung; (6) `VerticalRepeat`; (7) `Fallback`,
`Choice`, `Bounded`, `Pad`; (8) `Overlay`; (9) hold-and-re-drive for the across-axis nodes, which is
what makes `HorizontalFlow`, `HorizontalRepeat`, `HorizontalBands`, `Column`, `Range` and
`Table(view => …)` run at all. The pull engine runs the whole time.

---

## 10. What this deletes (confirming `engine-split.md` §5.4)

`IBound`, `Bound`, `Plane._bound`/`Bounded`, `Presence`'s inference from a size, `ProjectionResult`,
`AppliedResult`, `ProjectionEngine.TryPlace`/`Bind`/`Announce`, `ISweepAware`, the layout states, the
store's window, pool, leases, rewinds and their statistics, `ProjectionContext` entire, and — refined
by §4 — the *eager* half of the strategy calculus and the fold-identity suite, with the per-span
half renamed as the calculus.

---

## 11. Decisions (accepted as recommended, 2026-09-18)

| # | Decision | Recommendation |
|---|---|---|
| 1 | `Settlement` carries `Presence` | Yes: the sibling note, the repeat's "ended by tolerance" and the unconsumed-space report all read it today; nothing replaces it more simply |
| 2 | The strategy calculus | Single-form, per-span, oriented (§4): the incremental half survives renamed, the eager half and the duality die. This *refines* `engine-split.md` §5.4, which said delete the incremental half |
| 3 | `Overlay` streams concurrently; a horizontal flow under row-major holds | Yes, on dependence (§3); the declared-width slicing optimisation is recorded, not built |
| 4 | Points that escape a streamed source throw after eviction | Yes (§8); valid forever under a retaining source; the alternative pins rows for the result's lifetime, which is the window's cost model back again |
| 5 | One door | `Workbook.Sheet` narrows to a source for `Map`; the eager spaces are retaining sources, not a second kind of input; random access is a definition's `Extent`, at any depth (§8, owner's framing) |
| 6 | The buffer cap | One optional `WorkbookOptions` value, a failure naming the node when exceeded; no default cap |
| 7 | `Reach.Extent` in place of `Unbounded` | Yes: no machine reaches past what its placement gave it |
| 8 | `Reach` and `Axis` on the definition, not the projector | Yes: the dry run and the parent both need them before anything starts |
| 9 | The bind rung announces late | Accepted as the one exception, mirrored by its `Opacity` |
| 10 | Column-major driver | Deferred, as the roadmap has it; §3's table is what makes it a bounded piece of work later |
