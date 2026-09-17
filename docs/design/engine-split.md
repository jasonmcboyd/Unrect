# The engine split: a definition builds its own state machine

**Status.** Phase 1 of §9 landed on `experiment/engine-split` on 2026-09-17, in six commits (1a
the tooling face, 1b `Annotations`, 1c `Child`/`UseSite`, 1d the modifier collapse, 1e the slot
layout form, 1f the rename and `ReadDefinition`); phases 2–5 are not started. Three things were built
differently from the text below and are recorded in CLAUDE.md's "Where Work Left Off": `Reading`
reads a slot through the method `read.Of(slot)` rather than an indexer (a C# indexer cannot be
generic); the node classes stay internal for now; `ProjectionContext.Pending` survives until the
engine takes a use site per `Apply`. The document was written against `0f34f15` and records the
owner's decisions of 2026-09-16/17 and the three rulings that followed the first cut. It is
preparation for the push interpreter and comes *before* that design session; the per-kind machine
trace belongs to that document, not this one.

The one sentence: `IProjection<TSpace, TResult>.Project(Plane<TSpace>, ProjectionContext)` makes every
node both the declaration and its own pull interpreter. Replace `Project` with `Start` — *hand back
the state machine that reads you* — and the node becomes what CLAUDE.md already says it is, a
description, while the engine becomes what it should have been: a driver that pushes rows at a tree of
machines the definitions built.

**Three rulings that fix the shape.**

1. **Push is decided, not compared.** The pull interpreter is going away. Nothing here is designed to
   preserve it, and nothing is named for it.
2. **No visitor, no central walk.** Each definition builds its own machine. The **node set is open** —
   a third party may author a definition class with a machine of its own — and the **interpreter set
   is closed**: a new interpreter (the writer) costs a method on the contract, not a rewrite of every
   node. That is the expression-problem trade, chosen deliberately. A walk over `Children` still
   exists for *tooling* (the dry-run renderer), but it reads only the data face and interprets
   nothing.
3. **The machine contract is declaration-side.** `IProjector`, the span, the reach announcement and
   `ProjectorScope` live in `Unrect`, beside the nodes, because a definition names them. `Unrect.Engine`
   keeps only what drives: the driver, the row sources, the buffer manager, the diagnostics collector
   and renderer, and `Map`/`Apply`/`MapWithDiagnostics`.

Vocabulary, so the document can be terse: **builders** make **definitions**; the engine **starts**
**projectors** from definitions; a driven projector yields **the projection**.

---

## 1. The contract

### 1.1 The data face — unchanged from the first cut

```csharp
namespace Unrect.Projections
{
  /// <summary>A declared region and the reading of it, as data. Readable without a space, and without an engine.</summary>
  public interface IProjectionDefinition
  {
    string? Name { get; }
    string Description { get; }
    Placement Placement { get; }
    IReadOnlyList<Child> Children { get; }

    /// <summary>The unit label <c>.AsUnit</c> gave this node, or null.</summary>
    string? UnitName { get; }

    /// <summary>Marked a composition's internal plumbing by <c>.AsScaffolding</c>.</summary>
    bool IsScaffolding { get; }

    /// <summary>
    /// True for a node the declaration did not write as a level of its own — Select, Padded, Until,
    /// the tolerance boundary. A path renderer skips it unless it is named or a unit; nothing else
    /// reads it. (Today's <c>IsTransparent</c>, with the naming rule moved to the renderer.)
    /// </summary>
    bool IsWrapper { get; }

    /// <summary>
    /// Null when <see cref="Children"/> is the whole truth. A sentence when it is not — the one
    /// shipped node whose children are manufactured from data (<c>Table(headerRows, eachRow:
    /// captions =&gt; …)</c>), and whatever a third party authors that cannot enumerate itself.
    /// Replaces the internal <c>IOpaqueComposite</c>, which a renderer outside this assembly could
    /// not see and so read an empty child list as "leaf".
    /// </summary>
    string? Opacity { get; }
  }

  /// <summary>One edge of a composite: the child, and how the declaration wrote it.</summary>
  public readonly struct Child
  {
    public IProjectionDefinition Definition { get; }
    public UseSite Site { get; }        // the identifier written, and the 1-based position
  }
}
```

This face is the whole of what the dry-run renderer needs, and it needs no interpreter contract at
all — which settles CLAUDE.md's open question "where does the dry-run renderer live?": anywhere,
including outside the package.

`Annotations` is the one record every node carries — `(string? Name, string? UnitName, bool
IsScaffolding, Placement Placement)` — replacing four `MemberwiseClone` sites on `ProjectionBase`
with one.

### 1.2 The typed handle: `Start`

```csharp
public interface IProjectionDefinition<TSpace, TResult> : IProjectionDefinition
  where TSpace : class, ISpace
{
  /// <summary>
  /// The machine that reads this definition, for ONE application. A composite starts its children
  /// and wires them into its own machine; a leaf returns a machine over its own read. Called once
  /// per application, so a definition stays a reusable value applied to many spaces at once — all
  /// per-run state lives in the projector, none in the definition.
  /// </summary>
  IProjector<TSpace, TResult> Start(ProjectorScope scope);

  /// <summary>This node with different annotations — what <c>.Named</c> and its siblings build.</summary>
  IProjectionDefinition<TSpace, TResult> With(Annotations annotations);
}
```

There is no central interpreter object and no visitor: "turning a definition into machines" is `Start`,
recursive, and the engine holds only the root it started.

### 1.3 The machine: `IProjector`

A first sketch — enough to fix the shape. The per-kind trace (what a flow's machine does with a row a
child refused, how a repeat re-offers, how a bind's header row reaches the lambda) is the push design
document's subject.

```csharp
namespace Unrect.Projections
{
  /// <summary>One definition, mid-application: a state machine fed rows in order.</summary>
  public interface IProjector<TSpace, TResult>
    where TSpace : class, ISpace
  {
    /// <summary>
    /// Offers the next span. True: consumed — keep feeding me. False: <b>not mine, and I am
    /// finished</b> — the span is untouched and the caller re-offers it to my successor. A projector
    /// that has answered false answers false to everything after it.
    /// </summary>
    bool Next(Plane<TSpace> span);

    /// <summary>
    /// Nothing more is coming. Finalises and yields the value, or throws because what was declared
    /// is incomplete. The only way to get a value: a projector that ended by refusing a span is
    /// still closed by its parent.
    /// </summary>
    TResult Close();
  }
}
```

**The span is `Plane<TSpace>`, and no new type is needed.** A pushed row is a plane of height 1 whose
origin is the sheet's own; a buffered region is a plane of height *n* over the same buffer. Everything
a leaf, a view or a strategy already knows how to read is already written against a plane, so the push
engine's currency is the substrate's currency. Nothing is added to `Unrect.Core` by this design.

**Buffering is announced, never guessed.** A machine that cannot decide on the row in front of it —
`Until` holding rows against a landmark, `Choice` offering the same rows to each alternative, a
placement searching for its anchor — says at `Start` how far back it may have to see. Reaches join
upward, and the engine sizes **one** buffer from the root's reach:

```csharp
/// <summary>How much history a projector may need before it can decide. Joins by maximum.</summary>
public readonly struct Reach
{
  public static Reach None { get; }
  public static Reach Rows(int rows);
  public static Reach Unbounded { get; }      // honest, and expensive; a declaration that asks says so
  public static Reach Max(Reach first, Reach second);
}
```

This is `ISweepAware` inverted, and it is less machinery, not more: today a space is *told* which band
a placement opened and must keep up; under push the shapes *tell the engine* what to hold, once, before
a row is read.

### 1.4 What a machine asks of the engine: `ProjectorScope`

```csharp
/// <summary>
/// What a projector needs of the run it belongs to. Abstract class, not interface, and
/// private-protected: only the engine derives one, so it can grow a member without breaking anyone
/// — the same reason Plane and Point are structs (no default interface members on either target).
/// </summary>
public abstract class ProjectorScope
{
  private protected ProjectorScope() { }

  // Tree position, for diagnostics
  public abstract ProjectorScope Enter(Child child);
  public abstract ProjectorScope Occurrence(int index);
  public abstract int Ordinal { get; }

  // Buffering
  public abstract void Announce(Reach reach);

  // Failure and diagnostics — the path and the A1 location are the scope's to supply
  public abstract ProjectionException Failure<TSpace>(string problem, Plane<TSpace> at) where TSpace : class, ISpace;
  public abstract ProjectionException Reading<TSpace>(CellReadException failure, Plane<TSpace> at) where TSpace : class, ISpace;
  public abstract void Report<TSpace>(DiagnosticSeverity severity, string message, Plane<TSpace> at) where TSpace : class, ISpace;

  // The ambient label stack
  public abstract ProjectorScope WithLabels(LabelAxis axis, LabelMap labels, Offset captureOrigin);
  public abstract bool TryLabels(LabelAxis axis, out LabelMap labels, out Offset captureOrigin);
}
```

It subsumes the `IProjectionScope` seam the first cut invented for the views: `CellStrip`, `CellBlock`,
`TableRow` and `TableView` take a `ProjectorScope` and keep raising exactly the failures they raise
today.

### 1.5 The trade, stated

| | Open | Closed |
|---|---|---|
| Node kinds | **open** — a third party authors a definition class and its machine, and the vocabulary's factories are not privileged | |
| Interpreters | | **closed** — a second interpreter is a second method on `IProjectionDefinition<,>` |

The known, chosen cost: **a writer costs a second method.** "Declarations run backward — could this
declaration produce the file as well as read it?" is CLAUDE.md's own design test, so a writer is a
plausible future interpreter, and adding `IEmitter<TSpace, TResult> StartWriting(…)` to the contract
would break every definition authored outside this tree. Accepted, and recorded here so the decision
is not rediscovered as a surprise: the node set was opened on purpose, and this is what it costs.
Mitigations available when the day comes, in preference order: ship the writer as a `Children`-walk over
the data face for the shipped nodes only (third-party nodes render as opaque); or add the writer on a
*separate optional interface* a definition may implement (`IWritableDefinition<,>`), type-tested — the
same capability-by-type-test recipe the tree already uses for `ISweepAware` and the formula spaces.

---

## 2. A backend's leaf

With an open node set, `Unrect.Spreadsheets` may simply author `KindedCellProjection`'s successor as
its own definition class with its own machine. **It should not**, and the recommendation is unchanged
from the first cut: one node in `Unrect`, carrying the kind as data and the read as a function.

```csharp
namespace Unrect.Projections
{
  /// <summary>One cell read as a named kind. The read is a function; the kind is data.</summary>
  public delegate bool CellRead<TSpace, TValue>(Point<TSpace> cell, out TValue value, out CellProblem? problem)
    where TSpace : class, ISpace;

  public sealed class ReadDefinition<TSpace, TResult> : DefinitionNode<TSpace, TResult>
    where TSpace : class, ISpace
  {
    /// <summary>What the declaration asserts the cell is — "Decimal", "Date". Rendered in a path, and what a writer would emit.</summary>
    public string Kind { get; }

    public CellRead<TSpace, TResult> Read { get; }

    /// <summary>Whether a blank cell reads as null rather than failing — what <c>OrBlank</c> declares.</summary>
    public bool BlankIsNull { get; }
  }
}
```

Why, given the freedom not to: the machine for "one cell, one read" is **identical for every backend**
— assert a 1×1 span, honour `BlankIsNull`, call the read, turn a `CellProblem` into a located failure —
so a backend that authored its own would be copying a state machine in order to vary a delegate and a
string. Keeping it central also keeps the guarantee CLAUDE.md pins: a `Decimal()` leaf and a bound
`decimal` column describe a bad cell identically, because one machine raises both.

**The law this draws.** *A leaf may carry a read; a machine is what carries a walk.* A function from
one cell to one value declares no children and consumes nothing but its own span, so it is describable
data with a callback in it.

Two consequences: `OrBlank` stops being a virtual `Tolerating` dispatch through the base class and
becomes a pattern match on `ReadDefinition`/`TextDefinition` inside `Unrect` (deleting the cast in
`KindedLeaves.cs`), and the `Unrect` → `Unrect.Spreadsheets` `InternalsVisibleTo` grant loses its main
reason to exist.

---

## 3. The nodes that ship

Twenty-three, from twenty-two classes today (`LayoutProjection` is abstract; `Heading` was never a node
and still is not — it is a flow of captions above a section). This is **the set this package ships**,
not a closed set: a third party adds a twenty-fourth without asking anyone.

| Today | Node | Notes |
|---|---|---|
| `PointProjection` | `PointDefinition` | |
| `TextProjection` | `TextDefinition` | carries `BlankIsNull` |
| `KindedCellProjection` (Spreadsheets) | `ReadDefinition` | moves into `Unrect`; §2 |
| `CaptionProjection` | `CaptionDefinition` | |
| `FieldProjection` | `FieldDefinition` | |
| `NothingProjection` | `NothingDefinition` | |
| `StripProjection` | `StripDefinition` | lambda over `CellStrip` |
| `BlockProjection` | `BlockDefinition` | lambda over `CellBlock` |
| `RecordProjection` | `RecordDefinition` | lambda over `TableRow` |
| `TableProjection` | `TableViewDefinition` | lambda over `TableView`; `Table()` and `Table(view => …)` |
| `ColumnLabelsProjection` | `ColumnLabelsDefinition` | |
| `WithLabelsProjection` | `LabelledDefinition` | **takes the header's *definition*, not a `LabelMap` value** — §4.3 |
| `BandsProjection` | `BandsDefinition` | |
| `RepeatProjection` | `RepeatDefinition` | |
| — (today inside `TableProjection`'s lambda) | `BindDefinition` | `Table(headerRows, eachRow: captions => …)`; the one shipped node with `Opacity` |
| `ChoiceProjection` | `ChoiceDefinition` | its machine is the buffer's first customer |
| `FlowProjection` | `FlowDefinition` | §4 |
| `OverlayProjection` | `OverlayDefinition` | §4 |
| `MapProjection` | `SelectDefinition` | |
| `PadProjection` | `PadDefinition` | |
| `UntilProjection` | `BoundedDefinition` | announces a reach |
| `BoundaryProjection` | `FallbackDefinition` | |
| `UnitProjection` | `UnitDefinition` | |

Node classes are public and sealed with internal constructors: public because a third party's machine
may compose them and a renderer reads them; internal constructors because the vocabulary's factories
remain the only way to build the shipped ones.

---

## 4. Modifiers as nodes

Five abstract members leave `ProjectionBase`: `Inset`, `BoundedBy`, `WithHeadings`, `Otherwise`,
`Tolerating`. What remains is `DefinitionNode<TSpace, TResult>`: the annotations, `Clone()`,
`Description`, `Children`, and the abstract `Start`. A base class for *construction*, not behaviour.

| Modifier | Becomes | Why |
|---|---|---|
| `.Named(n)` | annotation field | already a clone-with-field |
| `.AsUnit(n)` | annotation fields (`UnitName`; `IsUnitBoundary` derived from it) | ditto |
| `.AsScaffolding()` | annotation field | ditto |
| `.On/.Below/.Sized/…` (`WithPlacement`) | annotation field (`Placement`) | ditto |
| `.Padded(…)` | `PadDefinition` | already a wrapper node |
| `.Until(l)` | `BoundedDefinition` | already a wrapper node |
| `.Else(f)` / `.Optional()` | `FallbackDefinition` | already a wrapper node |
| `.Select(f)` | `SelectDefinition` | already a wrapper node |
| `Heading(t).Of(x)` | `FlowDefinition` of captions + section | already composed, not a node |
| `.OrBlank()` | a copy of the leaf with `BlankIsNull: true` | §2 |

The five abstract members existed for one reason: the modifier surface is generic in the *receiver's
own type* (`TProjection Padded<TProjection>(this TProjection …)`), so the extension cannot name `TSpace`
or `TResult` and cannot construct a typed wrapper — virtual dispatch supplied the types.

That generality no longer earns its keep. It dates from the typed-spaces world where a receiver could
carry a demand its interface did not spell; with one invariant `IProjectionDefinition<TSpace, TResult>`
the types are in the receiver. Today's form already fails on a concrete-class receiver — `Wrapped<>`
throws "Hold the projection as `IProjection<TSpace, T>` rather than as its own class."

```csharp
// Before
public static TProjection Padded<TProjection>(this TProjection projection, int all)
  where TProjection : class, IProjection;

// After — inference gives back exactly the receiver's constructed type
public static IProjectionDefinition<TSpace, TResult> Padded<TSpace, TResult>(
  this IProjectionDefinition<TSpace, TResult> definition, int all)
  where TSpace : class, ISpace
  => new PadDefinition<TSpace, TResult>(definition, all, all, all, all, Annotations.None);
```

Every call site in the tree, the tests and `linqpad/` compiles unchanged. Deleted with the change:
`ProjectionExtensions.Base`, `Cloned`, `Wrapped`, `Leaf`, `NotOurs`, and the `MemberwiseClone` casts
they protect.

**Path rendering.** `IsTransparent` was a node-computed mixture of a structural fact and a naming rule
(`Name is null && !IsUnitBoundary`). Split it: the node publishes the structural fact (`IsWrapper`), the
renderer applies the rule, because it is the renderer's rule:

```csharp
private static bool Skipped(IProjectionDefinition node)
  => node.IsWrapper && node.Name is null && node.UnitName is null;
```

`ProjectionContext.Collapse`, `Chain`, `Segment`, `SegmentName`, `ApplyKindSuffix` and `PathNode`
become `PathRenderer` in the engine; `IsScaffolding` and `UnitName` are read off the public face, so
the `projection is ProjectionBase node && node.IsUnitScaffolding` cast disappears. Rendered output is
unchanged, which the diagnostics suite pins byte-for-byte.

---

## 5. The engine

### 5.1 What `Unrect.Engine` contains

| Piece | Note |
|---|---|
| **The driver** | reads spans from a row source in order, pushes them at the root projector, re-offers a refused span to the successor, calls `Close`, and classifies faults (`IsFault` moves here unchanged). Shaped as a **session** the engine owns — it starts the projector, owns the buffer, and exposes `Push(span)`/`Close()` — so `Map(space)` is a session fed from a row source and closed. A consumer never holds a projector (single-use, and the reach obligation would leak outward); a consumer who wants to drive the feed — a database cursor, a socket, rows arriving over time, partial results, cancellation between rows — holds the session. That entry point is deferred to the push design document's second cut; the first proves the machines over a file. |
| **Row sources** | `IRowSource<TCell>`, `IRowCursor<TCell>` — generic over the cell type, from `Unrect.Spreadsheets/Streaming/` |
| **The buffer manager** | the single buffer, sized from the root's announced `Reach`; vends the buffer-backed space a held region is read through |
| **`ProjectorScope`'s implementation** | tree position, diagnostics entry points, label stack |
| **`PathRenderer`, `DiagnosticCollector`, `MapResult<T>`, unconsumed-space reporting** | per-run, mutable |
| **`Map`, `Apply`, `MapWithDiagnostics`** | extensions on `IProjectionDefinition<TSpace, TResult>` |

**Namespaces do not follow the assembly.** `Map`/`Apply`/`MapWithDiagnostics` and `MapResult` keep the
namespace `Unrect.Projections`; the driver, the sources and the buffer manager take a new
`Unrect.Engine`. A consumer file adds a *reference*, never a `using` (§8), and the analyzers' metadata
lookups keep resolving (§7.4).

### 5.2 `ProjectionContext`'s four jobs, re-expressed

| Job | Where it lands |
|---|---|
| Tree position (`Descend`, `Blaming`, `WithIndex`, `WithOrdinal`, `Index`, `Ordinal`) | `ProjectorScope.Enter`/`Occurrence`/`Ordinal`, implemented in the engine |
| Diagnostics and path rendering (`Failure`, `Report`, `Reading`, `Chain`, `Collapse`, `Describe`) | `ProjectorScope`'s three reporting members; `PathRenderer` does the work |
| Label scope (`PushLabels`, `NearestLabels`) | `ProjectorScope.WithLabels`/`TryLabels`; `LabelAxis` and `ILabelSource` stay in `Unrect` (a node field, and `LabelMap` implements it) |
| Use-site naming (`UseSite`, `WithUseSite`, `Pending`) | **`Unrect`, on the `Child` edge** — see below |

**Use-site naming is a declaration-time fact.** `v.Next(transactions)` captures `"transactions"` with
`CallerArgumentExpression`; today that capture happens while the layout lambda *runs*, which is why it
is threaded through the context as a `Pending` slot claimed on `Descend`. Under the layout form of §6
the lambda runs once at declaration time, so the capture is stored on the `Child` edge (§1.1) and
`Pending`/`WithUseSite`/`SiteOf` are deleted. `UseSite` becomes public: it is declaration data now.
`RepeatDefinition.ItemSite` and `Table`'s `declared` argument already work this way and stop being
special.

### 5.3 What `Unrect.Spreadsheets` keeps

`Cell`/`CellKind`/`CellError`, `ISheetCells`/`SheetCellsBase`/`SheetGrid`, the eager
`SpreadsheetGridSpace` and its formulas, the kinded vocabulary, the reflective binder, `Workbook`/
`WorkbookOptions`, one `SpreadsheetRowSource : IRowSource<Cell>`, and
`SpreadsheetProjectionExtensions.MapWorkbook`. It gains a reference to `Unrect.Engine` for the source
and for `MapWorkbook`'s `Map`.

Stated plainly: a consumer of `Unrect.Spreadsheets` still gets the engine transitively, so "declare
here, run there" is not enforced by the package graph. Enforcing it would mean a fifth project; revisit
if a declaration-only dependency ever matters.

**Open for the push design doc, not settled here:** `Workbook.Sheet` vends a random-access
`ISheetCells` today, which is exactly what a push engine cannot promise. Either the streaming door
narrows to "drive a declaration over a file" (`MapWorkbook`, which is already the common case), or the
buffer-backed space is vended under the same name with its honest limits. The eager door
(`SpreadsheetSpace.Create`) is unaffected either way and remains the random-access answer.

### 5.4 What the push engine **deletes** — `IBound` and the pull machinery

With push decided, this list is not a migration plan but a demolition order. All of it stays exactly
where it is, untouched, until the push engine is green (§9, phase 5), and then goes:

| Gone | Because |
|---|---|
| `Unrect.Core/IBound.cs`, `Plane`'s `_bound` field, `Plane.Bound`, `Plane.Bounded` | a discovered bottom edge is what "ask whether there is another row" *was*; under push the driver either offers a row or does not |
| `Unrect.Core/Scans.cs`, `IRowScan`, `IAreaScan`, `IIncrementalRowStrategy`, `IIncrementalSizeStrategy`, `IIncrementalAreaStrategy` | the eager/incremental duality exists so one strategy can be read two ways; a machine reads rows one way |
| `Unrect/Projections/Bound.cs`, `Presence.cs`, `ProjectionResult.cs` (`Consumed`) | consumption is the machine's answer to `Next`, not a size reported afterwards |
| `Streaming/SheetStore.cs`, `ReaderPool.cs`, `ReaderLease.cs`, `ReaderPoolStatistics.cs`, `WindowedSpace.cs`, rewinds and reopens | one forward pass and one announced buffer replace a window, a pool and the arithmetic that sized them |
| `ISweepAware` | inverted into `Reach` (§1.3) |

Until then: `IBound` stays in Core (it is in `Plane`'s signature, and moving it would need `Unrect.Core`
to reference `Unrect.Engine` — a cycle), and nothing in the incremental calculus is touched. **Do not
refactor what is scheduled for deletion.**

---

## 6. The layout form

The decision that reshapes the vocabulary, and it must be taken before the push design session rather
than during it.

### 6.1 What today's form costs

```csharp
var report = VerticalFlow(v => new Report(
    Title: v.Next(Text()),
    Rows:  v.Next(transactions)));
```

The lambda is the only record of what the flow contains, and it runs *during* interpretation, handing
each child's **value** back before the next child is declared. So: `Children` is empty and
`IOpaqueComposite` exists to say so, blocking the dry-run renderer; use-site capture is an
interpretation-time event; and — decisively — **a machine cannot be built from it at all**, because
`v.Next(a)` must return an `A` before the rows that produce it have arrived.

### 6.2 The candidates

| Form | Push | Dry-run | Name capture | `linqpad/` |
|---|---|---|---|---|
| **A. Cursor (today)** | impossible | impossible | interpretation-time | unchanged |
| **B. Run the lambda twice** (stubs, then real) | still needs values mid-lambda on the real pass | partial, and a lie when the passes disagree | unchanged | unchanged |
| **C. Applicative / tuple builder** `VerticalFlow(l => l.Next(a).Next(b), (a, b) => …)` | works | works | works | rewrite; nested tuples past arity 3 |
| **D. Slots + build** | works | works | declaration-time | rewrite, mechanical |

B makes a declaration's construction side-effecting, which is the opposite of the charter, and does not
even solve push. C re-opens the arity explosion the tree closed for good and loses the parameter names
that make today's form readable.

### 6.3 Recommended: **D, slots and a build**

```csharp
var report = VerticalFlow(l =>
{
    var title = l.Next(Text());          // Slot<string>          — a handle, not a value
    var rows  = l.Next(transactions);    // Slot<IReadOnlyList<Transaction>>

    return l.Build(read => new Report(
        Title: read[title],
        Rows:  read[rows]));
});
```

- The lambda runs **once, at declaration time**. `l.Next` records a child and hands back a typed slot
  token; `l.Build` hands back the node. `Children` is complete and ordered; `Opacity` is null.
- `read` is a per-application values bag, so the definition stays a reusable, thread-safe value applied
  to many spaces at once — the guarantee a captured mutable box would break.
- **The combiner is a pure function of the children's values**, which is exactly and only what a
  composite's machine needs: fill slots as each child closes, call the combiner at `Close()`.
- Name capture returns to the declaration: `[CallerArgumentExpression]` fires where the child is
  *recorded*, so the `UseSite` lands on the `Child` edge.
- Value-dependent shape choice becomes unspellable — nothing in the lambda has a value to branch on.
  CLAUDE.md already discourages it; this leaves one sanctioned, bounded place where shape depends on
  data: the bind rung, which stays a node with a non-null `Opacity`.

```csharp
public readonly ref struct LayoutCursor<TSpace>
  where TSpace : class, ISpace
{
  /// <summary>Declares the next child and hands back the slot its value will arrive in.</summary>
  public Slot<T> Next<T>(
    IProjectionDefinition<TSpace, T> definition,
    [CallerArgumentExpression("definition")] string? declared = null);

  /// <summary>Closes the layout: <paramref name="combine"/> builds the result from what the children read.</summary>
  public Layout<TSpace, TResult> Build<TResult>(Func<Reading, TResult> combine);
}

/// <summary>A typed slot in one layout — an index, not a box.</summary>
public readonly struct Slot<T> { }

/// <summary>What the children read, in one application.</summary>
public readonly struct Reading { public T this[Slot<T>] { get; } }
```

`LayoutCursor<TSpace>` keeps its name (the analyzer resolves it by metadata name) and its `ref
struct`-ness. `Layout<TSpace, TResult>` changes from a delegate to the closed layout the cursor
returns, so a lambda that forgets to `Build` does not compile — where today a lambda that never calls
`Next` is a runtime fault (`DeclaredNothing`). One diagnostic disappears into the type system.

**What this does to the tree's own compositions.** `WithHeadings` translates mechanically.
`UnderColumnLabels` does not: it builds `WithColumnLabels(columns, body)` from `columns`, *a value read
at interpretation time*. That is the one place the library does what it tells users not to, and the
slot form forces the honest fix — `LabelledDefinition` takes the header's **definition**, and its
machine reads the header, pushes the label scope and feeds the body. One opaque composite fewer, the
same behaviour, and a table becomes fully readable by tooling.

**Migration.** One rung only: phase 1 changes `VerticalFlow`/`HorizontalFlow`/`Overlay` to the slot
form, rewrites the five `linqpad/*.linq` files and the layout tests, and keeps no compatibility
spelling. The vocabulary is pre-1.0, the change is mechanical, and two spellings of a flow is the "two
parallel mapping APIs" problem the tree has already closed twice.

---

## 7. The move list

`Unrect` means "stays in the `Unrect` assembly". "Phase" refers to §9.

### 7.1 `src/Unrect`

| File | To | Action |
|---|---|---|
| `Projections/IProjection.cs` | Unrect | **rewrite** → `IProjectionDefinition`, `IProjectionDefinition<,>`, `Child`, `Annotations` (phase 1); `Start` added (phase 3), `Project` removed (phase 5) |
| *(new)* `Projections/IProjector.cs`, `ProjectorScope.cs`, `Reach.cs` | Unrect | **add** (phase 3) — the machine contract, declaration-side |
| `Projections/ProjectionBase.cs` | Unrect | **rewrite** → `DefinitionNode<TSpace, TResult>`; five abstract members deleted (phase 1) |
| `Projections/LayoutCursor.cs` | Unrect | **rewrite** — slots and `Build` (phase 1) |
| `Projections/Composites/*` (11 node classes) | Unrect | **rewrite** as data nodes with a `Start` (phases 1, 3) |
| `Projections/Primitives/*` (10 files) | Unrect | ditto |
| `Projections/Composites/LayoutProjection.cs` | — | **delete** — folds into `FlowDefinition`/`OverlayDefinition` |
| `Projections/Composites/IOpaqueComposite.cs` | — | **delete** — replaced by `Opacity` |
| `Projections/Composites/LayoutState.cs`, `FlowState.cs`, `OverlayState.cs` | — | **delete** (phase 5) — the flow's machine replaces them |
| `Projections/Landmark.cs` | Unrect | stay; made public (declaration data over public Core contracts) |
| `Projections/Placement.cs`, `Field.cs`, `Orientation.cs`, `BlankRowStrategy.cs`, `CellReadException.cs`, `TypedPredicates.cs`, `Demanding*.cs`, `Binding/*`, `Pipeline/*`, `ProjectionBuilders*.cs` | Unrect | stay |
| `Projections/Views/*` (5 files) | Unrect | stay; constructors take a `ProjectorScope` |
| `Projections/Diagnostics/ProjectionException.cs`, `ProjectionDiagnostic.cs`, `ProjectionLocation.cs`, `DiagnosticSeverity.cs` | Unrect | stay — the views and `CellReadException` throw and render them |
| `Projections/Diagnostics/DiagnosticCollector.cs`, `MapResult.cs`, `ProjectionExtensions.UnconsumedSpace.cs` | **Engine** | move (phase 4) |
| `Projections/ProjectionContext.cs` | **split** | `UseSite`, `LabelAxis`, `ILabelSource` stay in Unrect; the rest becomes the engine's `ProjectorScope` implementation + `PathRenderer` (phase 4); the file dies with `Project` (phase 5) |
| `Projections/ProjectionExtensions.cs` | **split** | modifiers stay, rewritten (§4, phase 1); `Map`/`Apply`/`MapWithDiagnostics` → Engine (phase 4) |
| `Projections/ProjectionEngine.cs`, `Bound.cs`, `Presence.cs`, `ProjectionResult.cs` | — | **delete** (phase 5); `IsFault` is the one part that moves to the driver |
| `ISweepAware.cs` | — | **delete** (phase 5) — inverted into `Reach` |
| `CallerArgumentExpressionAttribute.cs`, `GridSpace.cs`, `ValueReads.cs` | Unrect | stay |

### 7.2 `src/Unrect.Spreadsheets` and `src/Unrect.Core`

| File | To | Action |
|---|---|---|
| `Streaming/IRowSource.cs` | **Engine** | move + generalise → `IRowSource<TCell>`, `IRowCursor<TCell>`; public (a second implementation now lives in another assembly) |
| `Streaming/SheetStore.cs`, `ReaderPool.cs`, `ReaderLease.cs`, `ReaderPoolStatistics.cs`, `WindowedSpace.cs` | — | **delete** (phase 5); the buffer manager replaces them |
| `Streaming/StreamingStatistics.cs` | **Engine** | move + re-cut around buffer reach and rows read |
| `Streaming/Workbook.cs`, `WorkbookOptions.cs`, `SpreadsheetRowSource.cs` | Spreadsheets | stay (§5.3's open question applies to `Workbook.Sheet`) |
| `KindedCellProjection.cs` | — | **delete**; its data becomes `ReadDefinition` in `Unrect` |
| `SpreadsheetProjections*.cs`, `SheetProjectionBuilders.cs`, `SpreadsheetTerminals.cs`, `KindedLeaves.cs`, `RowBinding.cs`, `MemberPlan.cs`, `TableBinding.cs`, `CellReading.cs`, `SpreadsheetProjectionExtensions.cs` | Spreadsheets | stay |
| `Unrect.Core/IBound.cs`, `Scans.cs`, `IRowScan.cs`, `IAreaScan.cs`, `IIncremental*.cs` | Core | **untouched until phase 5, then deleted** (§5.4) |

### 7.3 `InternalsVisibleTo`

| Grant | After |
|---|---|
| `Unrect.Core` → `Unrect`, `Unrect.Spreadsheets`, `Unrect.Tests` | keep while the bound layer lives; the `Plane.Bounded` reason dies with it |
| `Unrect.Strategies` → `Unrect` | keep, **add `Unrect.Engine`** (`AnchorNotFoundException.Description` is rendered by the diagnostics renderer) |
| `Unrect` → `Unrect.Spreadsheets` | **narrows to the pipeline-stage internals**; the `ProjectionBase` constructor and the `Tolerating` seam are gone |
| `Unrect` → `Unrect.Engine` | **new**, one grant: `UseSite`'s constructor, `Steps`, `BlankRowStrategy`'s flags, `LabelMap.Bound`/`AddressOf`, the view constructors. *Not* for node internals — a definition's machine is reached through `Start`, and an engine reaching past it is a review failure |
| `Unrect` → `Unrect.Tests`, `Unrect.Engine` → `Unrect.Tests`/`Unrect.Benchmarks`, `Unrect.Spreadsheets` → `Unrect.Tests`/`Unrect.Benchmarks` | keep / add |

### 7.4 Analyzers

`UnrectSymbols.TryLoad` resolves types by **namespace-qualified metadata name across all referenced
assemblies**, so moving a type between assemblies costs nothing as long as the namespace is kept —
which §5.1 does deliberately. One rename breaks it:

- ``"Unrect.Projections.IProjection`2"`` → ``"Unrect.Projections.IProjectionDefinition`2"``. Probe the
  new name first and fall back to the old for one release, so the analyzer and library packages need
  not be upgraded in lockstep.
- `ISpace`, ``ProjectionBuilders`1``, ``PlacementStage`1``, ``LayoutCursor`1`` and the seven phantoms
  are unchanged. `UNR002`/`UNR003` are unaffected in substance; `ScopedFactories` needs no edit.

### 7.5 Tests, benchmarks, interactive, packaging

- **One test project.** `Unrect.Tests` keeps its 2,470 tests and its folder convention: nearly every
  engine test asserts behaviour *through* `Map`, and the shared fixtures (`CountingSpace`,
  `WatermarkSpace`, `ProjectionTestSpaces`, `Observations`) serve both halves. Add an `Engine/` folder
  for tests that reach engine internals and move `Streaming/` under it. Add **one** architecture test
  to replace the boundary a second project would have enforced:
  `typeof(IProjectionDefinition).Assembly.GetReferencedAssemblies()` contains no `Unrect.Engine`.
  The differential suite (`ForceEager`) dies with the incremental calculus; its replacement is the
  two-engines-side-by-side harness of phase 4.
- **Benchmarks**: add a direct `ProjectReference` to `Unrect.Engine` (as for Core and Strategies —
  `Unrect` bundles it with `PrivateAssets="all"`, so it does not flow). Every family re-baselines when
  push lands; that re-baseline is the push arc's headline number, not this one's.
- **`Unrect.Interactive`**: unchanged. It never maps; it reads a sheet and emits source text.
- **Packaging**: `Unrect.Engine` rides inside the **`Unrect`** package, exactly as `Unrect.Core`,
  `Unrect.Strategies` and the two analyzer assemblies do — `ProjectReference … PrivateAssets="all"`
  plus the existing `BundleReferencedProjects` target. No new package id, no fifth version number.
  `Unrect.Spreadsheets` references `Unrect.Engine` with `PrivateAssets="all"`, the pattern it already
  uses for `Unrect.Core`.

---

## 8. A consumer file after

`linqpad/simple-report.linq`, imports and the map call, as they would read:

```xml
<Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">…</Reference>
<Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.Engine.dll">…</Reference>   <!-- new -->
<Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.Core.dll">…</Reference>
<Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.Strategies.dll">…</Reference>
<Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll">…</Reference>
<Namespace>Unrect.Core</Namespace>
<Namespace>Unrect.Spreadsheets</Namespace>
<Namespace>Unrect.Projections</Namespace>                                       <!-- unchanged: Map lives here -->
<Namespace>static Unrect.Projections.ProjectionBuilders&lt;Unrect.Spreadsheets.ISheetCells&gt;</Namespace>
<Namespace>static Unrect.Spreadsheets.SheetProjectionBuilders&lt;Unrect.Spreadsheets.ISheetCells&gt;</Namespace>
```

```csharp
var reportHeader = VerticalFlow(l =>
{
    var title      = l.Next(Text());
    var subTitle   = l.Next(Text());
    var reportDate = l.Next(Date());
    var reportId   = l.Next(Text());

    return l.Build(read => new
    {
        Title      = read[title],
        SubTitle   = read[subTitle],
        ReportDate = read[reportDate],
        ReportId   = read[reportId],
    });
});

var transactions = Table<Transaction>(bind => bind
    .Column(t => t.Date, "Transaction Date")
    .Column(t => t.Type, "Transaction Type"));

var report = VerticalFlow(l =>
{
    var header = l.Next(reportHeader);
    var rows   = l.Next(transactions);

    return l.Build(read => new { ReportHeader = read[header], Transactions = read[rows] });
});

report.Map(SpreadsheetSpace.Create(path, "Report")).Dump();     // unchanged
```

The whole cost to a consumer: **one assembly reference, and the layout lambdas gain a `Build`.** No
`using` changes, no prefix, no type argument, `Map` spelled as today.

---

## 9. Phases

Each phase builds green (`dotnet build src/Unrect.sln -v q --no-incremental`, `dotnet test
src/Unrect.sln`); WIP commits inside a phase are fine.

**Phase 1 — the vocabulary work push needs, with `Project` left alone.** The data-face rename
(`IProjection` → `IProjectionDefinition`, `*Projection` → `*Definition`, `ProjectionBase` →
`DefinitionNode`, `Child`/`UseSite`/`Annotations`, `IsWrapper` + `Opacity`), modifiers as nodes (§4),
and the slot layout form (§6). All three are changes the push engine requires and the pull engine can
still run, so the suite stays green throughout and every one of them is independently reviewable.
Update `UnrectSymbols` with the dual probe. Rewrite the five `linqpad/*.linq` files.

**Phase 2 — the push design document.** The per-kind machine trace: what each of the twenty-three
nodes' machines do with a span, how a refusal is re-offered, how placement resolution becomes a
machine, how a bind's header row reaches its lambda, what `Reach` each node announces, and what
replaces `Consumed`/`Presence` in a world where consumption is the answer to `Next`. This document
fixes the shape; that one fixes the behaviour.

**Phase 3 — the contract and the machines, beside `Project`.** Add `IProjector`, `ProjectorScope`,
`Reach` and `Start` to `Unrect`. Implement `Start` kind by kind, keeping `Project` on every node until
the last kind has a machine. The tree runs on the pull engine the whole time; the machines are dead
code under test until phase 4.

**Phase 4 — the driver, and two engines under one suite.** Create `Unrect.Engine`: the driver, the row
sources, the buffer manager, the `ProjectorScope` implementation, `PathRenderer`, the collector, and
`Map`/`Apply`/`MapWithDiagnostics` over push. Run the suite against both engines (a harness switch,
the successor to `ForceEager`) until push is green — values, extents, diagnostics and rendered paths
alike.

**Phase 5 — the demolition, which is the dividend.** Delete `Project` from the contract and the nodes,
the pull engine (`ProjectionEngine`, `ProjectionContext`, `ProjectionResult`, `Presence`, `Bound`), the
bound layer in Core (`IBound`, `Plane._bound`, `Scans`, `IRowScan`/`IAreaScan`, the three
`IIncremental*` interfaces), `ISweepAware`, the layout states, and the store's pull half (window, pool,
leases, rewinds). **`Unrect` now compiles with no reference to `Unrect.Engine`** — that compile is the
split's acceptance test, and it is only achievable here.

**Why no "walk in place" phase first, and why the assembly appears late.** The pull engine *cannot* be
moved out of `Unrect` while `Project` lives on the nodes: `Project` names `ProjectionContext`, so an
engine assembly owning the context would have to be referenced by `Unrect` — the boundary inverted. The
fusion is exactly what prevents the split, which is why the split completes only when `Project` dies.
Everything before phase 5 is therefore preparation inside one assembly, and that is the tree forcing
the order, not a preference.

---

## 10. Decisions for the owner

| # | Decision | Recommendation |
|---|---|---|
| 1 | **The name.** `IProjectionDefinition` vs `IProjectionSpec`, `IShape`, `IDeclaration` | **`IProjectionDefinition`** (the owner's lean). "Projection" becomes what a driven projector yields. `IShape` is a returning ghost; `IDeclaration` overreaches — a declaration is the whole file. |
| 2 | **`IProjector`'s shape** | `bool Next(Plane<TSpace> span)` + `TResult Close()`, with false meaning "not mine, and I am finished — re-offer it". `Close` is the only way to a value. Refine in phase 2, not here. |
| 3 | **Where the machine contract lives** | **`Unrect`, beside the nodes** — a definition names `IProjector`/`ProjectorScope`/`Reach`, so they are declaration-side by the same test Core's charter uses. **Nothing new goes in Core.** |
| 4 | **The span type** | **`Plane<TSpace>`, no new type.** A pushed row is a plane of height 1; a buffered region is a plane of height *n*. Leaves, views and strategies already read planes, so the engine's currency is the substrate's. |
| 5 | **`ProjectorScope`: class or interface** | **Abstract class with a `private protected` constructor.** Only the engine derives one, and without default interface members on either target an interface could never grow a member — the same argument that made `Plane` and `Point` structs. |
| 6 | **A backend's leaf** | **One `ReadDefinition<TSpace, TResult>` in `Unrect`**, carrying `Kind` (data) and `Read` (a function of one cell) — the open node set permits a backend to author its own, but the machine for "one cell, one read" is identical everywhere, and one machine is what keeps a leaf and a bound column describing a bad cell identically. |
| 7 | **The layout form** | **Slots + `Build`** (§6.3), with the slot type named `Slot<T>`. One rung; no compatibility spelling. |
| 8 | **The bound layer and the incremental calculus** | **Untouched until phase 5, then deleted outright** (§5.4). Do not refactor what is scheduled for demolition; do not move `IBound` out of Core in the meantime (it would need a cycle). |
| 9 | **A writer costs a second method** | **Accepted, on the record.** The node set is open, so adding `StartWriting` to the contract would break third-party definitions. When the writer arrives: ship it as a `Children`-walk over the data face, or as an optional interface a definition may implement and the writer type-tests. |
| 10 | **Test projects** | **One.** Add an `Engine/` folder and the one architecture test of phase 5. |
| 11 | **Packaging and namespaces** | **`Unrect.Engine` rides inside the `Unrect` package**; everything a consumer names keeps the namespace `Unrect.Projections`, including `Map`. Costs a consumer one reference and no `using`, and keeps the analyzers resolving. |
| 12 | **`Unrect.Spreadsheets` → `Unrect.Engine`** | **Accept it.** The row source and `MapWorkbook` need the engine. A declaration-only graph would need a fifth project; defer until someone asks. `Workbook.Sheet`'s future is phase 2's question. |
| 13 | **Modifier surface** | **Collapse to two-type-parameter extensions** (§4), deleting `Base`/`Cloned`/`Wrapped`/`Leaf`. Source-compatible for every call site in the tree. |
