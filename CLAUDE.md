# Unrect

## Status

This project is experimental, but both the substrate (CellValue, spaces, strategies) and the projection layer (`Unrect.Projections`) now have deliberate, review-hardened semantics pinned by `src/Unrect.Tests` (xUnit, 2,073 tests). Run `dotnet test src/Unrect.sln`; keep it green. Gate builds with `dotnet build src/Unrect.sln -v q --no-incremental` — incremental builds silently skip analyzer diagnostics (xUnit analyzers etc.), so a plain build can report 0 warnings while warnings exist.

## Problem Domain

Unrect addresses a real-world problem common in financial institutions: hierarchical data stored in flat 2D structures (primarily Excel spreadsheets). These are not simple tabular datasets — they contain nested, heterogeneous regions that need to be parsed into structured objects.

A typical example is an IRR report with:
- A header section (title, date, generic report info)
- Repeating client blocks, one per client
- Within each client block, sub-sections like capital calls, fundings, etc.

Row-oriented parsers are the wrong tool for this. They devolve into stateful cursor logic and fragile index tracking. Unrect takes a different approach: declarative 2D spatial decomposition. You describe the shape of the data once, and the framework handles decomposing the grid along that shape and projecting each part to a typed object.

## Design Philosophy: Declarative, Not Imperative

This is the core design commitment of the project, and it constrains every API decision.

A user of Unrect **declares the shape** of the data — "a header, then N repeating client blocks, each containing a capital-calls section sized by this predicate" — as a static description built from shapes and strategies. The framework interprets that description to perform the decomposition. The user never writes traversal logic: no "current row" state, no manual index arithmetic, no loops that walk the grid deciding what comes next. (`LayoutCursor` is a cursor in name only: `Next` declares the next child in flow order and exposes no position to compute with.)

The distinction matters because the imperative alternative is the failure mode this project exists to escape. Row-oriented parsers force the shape of the data to live implicitly in control flow, where it is fragile and unreadable. In Unrect, the shape is a first-class value: a composition of shapes and strategies that can be inspected, reused, and reasoned about independently of any particular spreadsheet.

Practical implications for API design:

- **Projections describe; they do not execute.** Constructing a projection should be side-effect free. Decomposition happens when the description is applied to a space (`Map`/`Apply`), not while the description is being assembled.
- **Strategies are the escape hatch for dynamism — but they are still declarative.** Variable-sized regions are handled by declaring *how* a boundary is determined (a predicate, a size rule), not by the user imperatively scanning for it. If a use case seems to require the user to write cursor logic, the right fix is a new strategy or combinator, not an imperative API.
- **Mapping is projection, not parsing.** By the time map functions run, decomposition is complete. Map functions read from an extent handed to them; they never influence or perform boundary decisions. One deliberate exception: the cursor-lambda form (`VerticalFlow(v => ...)`) interleaves projection with the flow's decomposition, which makes value-dependent shape choice *expressible* — allowed because the API cannot prevent it, discouraged in the docs, and nothing is added to encourage it.
- **Evaluate new features against this test:** does it let the user *say what the data looks like*, or does it make them *say how to walk it*? The former belongs in Unrect; the latter is a design smell. A second lens on the same question: could a writer execute this declaration — could it produce the file as well as read it? Declarations run backward; opaque code does not.
- **Explicit dimensions are the exception, not the rule.** Almost every boundary should be discovered by a strategy (predicate-based sizes, skip-while offsets). Hard-coded offsets/sizes are acceptable only for structurally fixed regions — e.g., a report header whose shape is part of the format's definition. A hard-coded count that merely *happens* to match today's file (a gap of 2 blank rows, a table 4 columns wide) is a fragility bug waiting for the next export.

## Architecture

### Core Metaphor

A "space" is a 2D rectangular grid of values. Spaces can be subdivided into subspaces (via offset and area), and a declared shape decomposes a space into a hierarchy of subspaces, projecting each to a typed value as it goes. Strategies determine how to compute boundaries (sizes, offsets, row/column counts) dynamically.

### Project Structure

| Project | Purpose |
|---|---|
| **Unrect.Core** | **The charter: Core contains exactly the types its contracts speak — nothing else.** Every type here appears in a contract signature or in a contract's documented behaviour: `CellValue` (and `CellKind`/`CellError` through it) via `ISpace`'s indexer; `Size`/`Offset`/`Area` via the strategy signatures; `OutOfBoundsException` via the throw `ISpace` mandates. `ISpace`, the strategy interfaces and `IRowLandmark`/`IColumnLandmark` are those contracts. A type that merely *uses* the contracts belongs above them — which is why `GridSpace` (an implementation of `ISpace`) and `SpaceExtensions` (sugar over one of its methods) moved out on 2026-09-03. Before adding a type here, name the contract it appears in; if you cannot, it goes in `Unrect`. |
| **Unrect** | The projection layer (`Unrect.Projections`): the `Projection` vocabulary, `IProjection<T>`, `ProjectionEngine`, the composites and primitives, the cell views (`CellStrip`/`CellBlock`/`TableView`), and diagnostics — plus, in the root `Unrect` namespace, `GridSpace` (a `CellValue[,]` viewed as an `ISpace`, entered by constructor for cells already canonical or by `Create<T>(values, map)` and the primitive overloads with blank predicates, which is where blankness is decided); and the shared `Orientation` enum with the `CallerArgumentExpressionAttribute` polyfill (netstandard2.1 has no built-in one; it backs use-site name capture) |
| **Unrect.Strategies** | Strategy implementations for computing sizes, offsets, rows, and columns — plus `SpaceExtensions` (declared in the root `Unrect` namespace, sugar over `ISpace.GetSubspace`) |
| **Unrect.Spreadsheets** | `SpreadsheetSpace` — reads spreadsheet files (`.xls`/`.xlsx` via ExcelDataReader) and adapts cells to `CellValue` eagerly, the whole sheet at once. `Workbook.Open(path)` is the streaming door onto the same files: `Sheet(name)` vends a lent `ISpace` view backed by a windowed chunk store and a lead/chase reader pool, so a shape reads a sheet a window at a time instead of the whole grid. Named for the family, not the vendor: further formats belong here rather than in a second package. Foldered `Streaming/` + `Formulas/` since the 2026-09-08 cleanup — the tree-wide convention: **libraries folder with flat namespaces** (a folder is navigation, never an API boundary); **tests folder = namespace**. |
| **Unrect.Benchmarks** | The continuous-benchmark suite (BenchmarkDotNet, 41 benchmarks in seven one-class families including `Streaming`; not packable), plus `Retention` — an eighth CI leg that is not a BenchmarkDotNet family at all: a deterministic one-shot measurement of LIVE bytes with a result held, which is the question `MemoryDiagnoser`'s `Allocated` column cannot answer (a duplicate string is allocated by the reader before the adapter sees it). Its eager rows are the rig's one exception to the no-workbooks rule — they generate a real `.xlsx` into the temp directory at setup and read it back through `SpreadsheetSpace.Create`, because the adapter seam they measure is inside it. Conventions and the change-judging workflow: `docs/benchmarking.md`. |

All four library projects multi-target `netstandard2.0;netstandard2.1` (since 2026-09-05), so the packages install on .NET Framework 4.6.1+ as well as .NET Core/5+. `Unrect.Tests` is `net8.0` everywhere and `net8.0;net48` on Windows — the net48 leg cannot run on Linux, so an OS condition in its csproj keeps Linux CI at net8.0; a net48 portability regression therefore surfaces only on Windows (`dotnet test src/Unrect.Tests/Unrect.Tests.csproj -f net48`). `Unrect.Benchmarks` stays net8.0.

**No `#if` forks — one source, both targets.** netstandard2.1-only constructs are removed rather than branched on: no default interface members (the incremental calculus's definitional folds live on the static `Unrect.Core.Scans` — `Fold`/`FoldSize`/`FoldArea` — and internal `ColumnAccumulators.Fold`, each implementation delegating in one line), no `System.HashCode` (Core hand-rolls its combine to keep the package dependency-free), no `HashSet<T>.TryGetValue`, no `Dictionary<K,V>(IEnumerable<KeyValuePair<K,V>>)`. The two polyfills (`CallerArgumentExpressionAttribute`, `IsExternalInit`) are unconditional because *neither* netstandard has the type; a net5.0+ target is what would force an `#if`. Consequence: DIMs are no longer a seam option — a DIM on a published Core interface would compile everywhere and fail at run time on .NET Framework, so adding a member to a published Core interface is a breaking change with no escape hatch; type-testing is the whole capability recipe. The "eager and lazy cannot disagree by construction" guarantee is now carried by the fold-identity suite instead (`IncrementalStrategyTests` pins `SelectRows == a hand-written fold` for every factory; a new incremental strategy carries the obligation to join that theory data).

### Data Flow

```
Excel file / 2D array
    -> adapter normalizes to CellValue   ("lexing": backend values -> canonical vocabulary)
    -> ISpace                            (uniform grid of CellValue)
    -> Projection + Strategies           (declarative description)
    -> shape.Map(space)                  (hierarchical decomposition and projection, fused)
    -> typed objects
```

### Key Abstractions

- **`CellValue` / `CellKind`** — The canonical cell vocabulary (Blank, Text, Number, Temporal, Boolean, Error). One `Number` kind with granular checked accessors (`GetDouble`/`GetDecimal`/`GetInt`); numbers created from `decimal`/`int`/`long` retain an exact decimal alongside the double. Blankness is decided at adaptation time (e.g., `GridSpace.Create(nums, isBlank: v => v == 0)`); `CellValue` is a 24-byte readonly struct whose `default` IS `Blank` (adopted 2026-09-03, judged by the benchmark rig: creation allocations −42% to −61%, double/string/date/bool cells allocate zero heap); strategies just test `IsBlank`/`HasValue`.
- **`ISpace`** — A 2D rectangular grid of `CellValue` with subspace slicing. Non-generic since the wave-1 canonical-model refactor.
- **Strategies** — Pluggable functions that determine spatial boundaries:
  - `ISizeStrategy` — computes a `Size` from available space
  - `IOffsetStrategy` / `IAreaStrategy` — adapted from `ISizeStrategy`
  - `IRowStrategy` / `IColumnStrategy` — predicate-based row/column selection
  - Blankness conveniences: `OffsetStrategies.SkipBlankRows()`/`SkipBlankColumns()`, `SizeStrategies.RowsWhileAnyValue()`, `RowStrategies.TakeRowsWhileAnyValue()`, `ColumnStrategies.TakeColumnsWhileAnyValue()`
  - Explicit counts: `RowStrategies.TakeRows(n)` / `ColumnStrategies.TakeColumns(n)` — these throw `OutOfBoundsException` rather than clamp, consistent with `ExplicitArea`
  - `SizeStrategies.RowsWhileAny(predicate)` / `RowsWhileAnyValue()` + `.ToAreaStrategy()` (width = full available width, height = leading rows in which at least one cell satisfies the predicate) — the preferred way to size a data region ("rows while any cell has a value") instead of explicit bounds or `MaxArea`
  - `OffsetStrategies.SkipRowsWhileAll(predicate)` / `SkipRowsWhileAny(predicate)` (via internal `RowOffsetSizeStrategy`, width always 0) declare a vertical offset such as "skip however many leading rows are entirely blank" — the declarative replacement for hard-coding gap heights; `SkipBlankRows()` is the zero-argument form. These do their primary work as the shape layer's defaults (e.g. `Table`'s placement); the family's direct usage is pinned in `src/Unrect.Tests/Strategies/OffsetStrategyTests.cs` (`SkipBlankRows` and `SkipRowsWhileAny` directly; `SkipRowsWhileAll` only through its zero-argument form)
- **Projections (`Unrect.Projections`) — the user-facing API, and the only one.** A projection fuses declaration and reading: `projection.Map(space)` decomposes and projects in one call, and the projection is a reusable value safe to apply to many spaces at once. Vocabulary via `using static Unrect.Projections.Projection`:
  - **Leaves** — `Cell`/`Row`/`Column`/`Range`, the six typed cell leaves `Text()`/`Decimal()`/`Integer()`/`Double()`/`Date()`/`Boolean()`, `Caption(text)`, `Field`/`Fields`. `Range` is the rectangular-region leaf, read through a `CellBlock`; the view keeps its name, only the factory is `Range`. `Caption` is an anchor row *declared*: it finds the row, asserts the text, consumes it at full width, and yields the file's own spelling. `Fields(Field("EIN"), …)` is the labelled-pair card — two columns by one row per field, extent from the child count, anchored on its own first label, keyed by the declared labels.
  - **The leaf firewall.** The typed leaves are closed over `CellValue`'s canonical accessor set and mirror it 1:1; there is no `Long()`, `Money()` or `Enum<T>()`, now or later, because each would be a new failure vocabulary and a new writer obligation, and the document has six kinds, two of which no leaf reads (`Blank` and `Error` are conditions, not values a leaf projects). A conversion beyond that set is `Select` territory. Nothing is added to Core to serve a leaf, and adding an accessor to Core does not add one (`GetDate` is a transformation of `GetDateTime`, so it has no leaf).
  - **Kind vs conversion.** A kind failure speaks the document's vocabulary — `expected Number at B4, found Text` — never the reader's; a `Number` that will not fit the CLR type asked for is reported as a conversion on a number that is really there — `the Number at B4 (1.5) is not a whole number`. One template (`CellReading`), shared by the leaves and the table binder, so a `Decimal()` leaf and a `decimal` column cannot describe the same cell differently.
  - **Layouts** — `VerticalFlow(v => ...)`, `HorizontalFlow(h => ...)`, `Overlay(o => ...)`. Each takes one `Layout<T>` lambda that declares its children by calling `v.Next(shape)` in order and builds the result where the parts are read; there is no arity. A flow divides its extent into bands, each child starting where the last left off; an overlay hands every child the whole extent to place itself in, so children may overlap. There is no applicative (tuple) spelling — it was removed once the lambda form proved out.
  - **Tables — one `Table` family, five rungs (phase 5 of the projection model, 2026-09-07; `TableRows*` is gone, no aliases).** `Table<T>()` binds captions to a type's members by name (case- and whitespace-insensitive, via the public `CaptionComparer`), the member's own type choosing the kind and accessor; `Table<T>(bind => bind.Column(t => t.Date, "Transaction Date").Ignore(t => t.X))` adjusts what reflection writes; `Table(headerRows: 1, eachRow: captions => ...)` is **the bind** — the header is read once per application of the table (a table inside a repeat binds per occurrence, each with its own header) and its `CaptionMap` handed to a lambda that *returns the projection for one record* (`Overlay(o => new Row(o.Next(Decimal().Right(captions["Amount"]))))`), so caption positions are absolute, a reordered export needs no change, and a bound row's demand (a `Formula()` inside it) flows out through the lambda's return type; `Table(headerRows:, eachRow:)` is the plain row slot (a projection applied to each 1×W band through the engine — a failure inside record 1 cites `Table[1] -> 'allocation' -> Decimal?#3 @ J5`; note `Row(project)` as an eachRow measures ITSELF, width 0 on a sparse sheet — use `Range(WholeExtent(), ...)` or an `Overlay`); `Table()` hands back `IReadOnlyDictionary<string, CellValue>` per row for exploring an unfamiliar sheet; `Table(row => ...)`/`Table(view => ...)` are the lambda escape hatches. Binding for the first two rungs resolves once at construction — a bad member type is an error then, not per file — and is strict one way: every member must find a column, while a column no member claims is fine. A nullable member tolerates a *blank* and still fails on the wrong kind; an annotated `string?` is blank-tolerant the same way `Nullable<T>` is. **Rung 1 is sugar only conceptually**: implemented directly rather than desugared to the bind, so its diagnostics keep their subject (`column 'Amount': ...`), their path (the table's), and their aggregation (every unbound member in one message). One wart, compiler-caught: a lambda body touching nothing distinctive is ambiguous between the two rung-5 lambdas (`Table(t => t)`); type the parameter — `Table((TableView t) => t)`. And `Table()` shadows any member named `Table` (a class member beats `using static`).
  - **Repetition and alternation** — `VerticalRepeat(item, separatedBy:, atLeast:)`/`HorizontalRepeat(...)` (both axes marked since 2026-09-05 — the algebra never encodes one substrate's dominant axis as "normal"), `Choice`, `.Else`, `.Optional`. `VerticalBands(rows, each, onBlank:)`/`HorizontalBands(columns, ...)` is the tiler, not a repeat: a repeat repeats a *pattern* (each occurrence's extent discovered from the item, run ends where the pattern stops matching), a tiler repeats a *fixed-dimension space* (the extent cut into bands of a declared stride, nothing searched, the walk ends when a whole band is no longer left).
  - **Modifiers** — `.Named`, the placement family `.On(m)`/`.Below(m)`/`.RightOf(m)`/`.OffsetBy(strategy)`/`.AfterBlankRows`/`.Down`/`.Right` (law: silence is adjacency — every placement operator is a declared exception naming its kind of reason: relation to a landmark, filler to absorb, fixed distance, or `.OffsetBy`, the strategy-level door and the marked crossing from the cell-model surface into the interval-model strategy calculus; second law since 2026-09-09: a declared placement REFUSES a second declaration at construction — anchors/`.OffsetBy`/`.Sized` replace only a shape's own default, and a second `.Until` directly on an `.Until` refuses likewise — a contradiction has no quiet meaning, record in `docs/design/modifier-congruence-survey.md` §5), `.Sized`, `.Padded`, `.Until(landmark, orEnd:)`, `.Under(captions)`. `.Under` is sugar for a vertical flow — `x.Under(a, b)` is `VerticalFlow(v => { v.Next(a); v.Next(b); return v.Next(x); })` — so every caption is a real child with its own path segment, and it describes itself as `Under`.
  - **Matchers and their lifts** — one family for "a row that matches": `RowWhere`/`RowWithCell`/`RowContaining` and the column twins. A matcher only locates and reports absence; a *modifier* decides what absence means and where the projection lands: `.On(m)` owns the matched row/column (axis-agnostic — occupancy is directionless), `.Below(m)`/`.RightOf(m)` start exactly one beyond it (direction in the word because adjacency has one), and `.Until(m, orEnd:)` bounds by it. `OffsetStrategies.To`/`Past` remain public strategy calculus (and `Caption`/the binder use them internally) but are no longer re-exported on `Projection` — the 2026-09-05 renovation replaced the `After(To/Past(...))` spelling, which itself replaced the twelve `Seek*` factories. **The naming law:** a bare `Where`/`While` takes a space predicate `(space, index)`; a cell predicate is always marked (`WithCell`, `WhileAll`, `WhileAny`); `Containing` is whole-cell, trimmed, case-insensitive.
  - **Naming** — a child's path segment is the first of: its own `.Named`; the bare identifier it was written as at the capture site (`v.Next(transactions)` reads as `'transactions'`, inferred by `CallerArgumentExpression`); otherwise its kind and 1-based position, as `Cell#2`. `VerticalRepeat(item)`/`HorizontalRepeat(item)` capture the same way, so a hoisted item labels every occurrence — `VerticalRepeat(investorDetail)` renders `VerticalRepeat[2] -> 'investorDetail'`, where the index stays on the repeat's own segment and the label lands on the item's. A repeat's item has no ordinal to fall back on, so an inline one keeps its description. So hoist shapes into well-named locals and let the use site name them; a helper must not `.Named` what it returns, or every use site is called the same thing.
  - **`.Until(landmark)`** ends a shape's extent just before a content landmark (`RowContaining`, `RowWithCell`, `RowWhere`, and the column twins), consuming the bound in full so the next sibling starts *at* the landmark. A missing landmark is a loud, absorbable failure unless `orEnd: true`, which runs to the end of the space and records an `Info`.

  Placement is applied by `ProjectionEngine` alone, exactly once, at every level including the root. Failures throw `ProjectionException` with the declaration path and an A1 cell location. A layout composite is opaque to tooling — its children exist only while its lambda runs. `docs/vocabulary.md` is the complete operator survey, grouped by role with the cross-cutting laws, and the first place to check the current surface.
- **Streaming (`Workbook`, in `Unrect.Spreadsheets`) — the second door onto a spreadsheet, for files too big to hold entirely in memory or many files run through one declaration.** `Workbook.Open(path)` owns the file handles and reader pool; `book.Sheet(name)` vends a lent `ISpace` — a value, not a handle, invalidated only by the workbook's own `Dispose`. The cost model is declaration-shaped, not a flat tax: since Part 2 (lazy extents) a monotone walk costs eager's wall time at a fraction of the live memory (the 1M-row `Table` parse: ~13s both doors, 1.6 MB peak resident vs ~214 MB — `RowsMaterialised` exactly 1,000,001, one interleaved pass), but a declaration that reaches backwards or sweeps a band wider than its window can be arbitrarily slower — eager stays the simple default for small files. **Sizing law:** `WorkbookOptions.WindowRows` must be at least as tall as the tallest extent open at once (a walk needs one chunk; a `HorizontalFlow`/`Overlay` over a band needs the whole band) — undersizing it is not degradation, it is collapse. `Workbook.Statistics(sheet)`/`ReaderStatistics` report the vocabulary to act on: `ChunkReloads`/`WindowOverruns` say the window is too small, `Reopens` says too few readers are open. **IO errors are faults, never tolerance** — a disk failure or a read after `Dispose` is classified a fault at every site that can absorb a foreign exception, so `.Optional()`/`.Else()`/`Choice` can never report a broken file as an absent section. User guide: `docs/streaming.md`.

## Known Bugs

- **Elapsed-time cells throw.** A cell with an elapsed-time number format (`[h]:mm:ss` — built-in `numFmt` id 46, id 79, or any custom bracketed `[h]`/`[m]`/`[s]` format) throws `"TimeSpan cell values are not yet supported"` from inside `SpreadsheetSpace.Create` — *before any shape runs*, so the failure carries no declaration path and no A1 location. Excel has no duration type; these are an ordinary number of days (may exceed 1, may be negative) and should lex as `Number` (days). Any timesheet / SLA / duration column in a real workbook hits this. (See the Duration-kind question below.)

(The original historical list — `RegionBuilder1` double-offset, inverted `TakeColumnsWhileAny`, `SpreadsheetValueBase` equality contract — was resolved by the 2026-08-31 wave-1 refactor; subspace resolution and bounds checking now live in `ProjectionEngine`, the one code path that resolves a placement.)

## Design Direction

The forward design: a canonical cell-value model that de-generifies the core (spaces as
"lexers" adapting backends into one value vocabulary, blankness decided at adaptation time),
a document-level shape vocabulary (`Table`, `Repeat`/separator, `Choice`) built on the
strategy calculus, capability seams for backend extras (formatting, native types) under the
rule that nothing in Core may require a capability, and an observability roadmap (named
regions, decomposition trace, dry-run renderer, unconsumed-space warnings). New API work is
checked against these principles. What is built and what remains is in **Where Work Left
Off**.

## Open Design Questions

- **Strategy layering** — `IAreaStrategy` and `IOffsetStrategy` are thin wrappers around `ISizeStrategy`. Whether this indirection earns its keep or should be collapsed is an open question. Adjacent and DECIDED (owner, 2026-09-02, pre-publish): `Area` stays a distinct `Size` wrapper and keeps both its `Size` property and the `Width`/`Height` passthroughs — the two-spellings wart is confined to engine plumbing that library consumers never operate at, so it does not justify surgery on `ISpace`/`Placement`/the strategy interfaces. Revisit only if the strategy-layering question itself is ever taken up.
- ~~**`SpaceExtensions.GetSubspace(space, offset)` throws `ArgumentOutOfRangeException`**~~ — **resolved 2026-09-03.** Both forms now throw `OutOfBoundsException`, checked before the subtraction that used to produce a negative extent and let `Size` report it as an argument error. `ISpace`'s indexer doc states the contract for every implementation: running off the edge of a space is a bounds condition a declaration may recover from, never an argument bug. Fixed while moving the type out of Core under the charter; it wants a test pin now that it is behaviour rather than an accident.
- **`OutOfBoundsException` carries no diagnostics** — no requested-vs-available extents, no location. The projection layer wraps everything in `ProjectionException` with path + A1 location, so the bare type now surfaces only from strategies and from direct `ISpace` slicing.
- **Does a `Duration` kind join the kind set?** ExcelDataReader yields `TimeSpan` for `[h]:mm`-formatted (elapsed-time) cells; the adapter currently throws rather than guessing (see Known Bugs). ODS/Apple Numbers have a first-class duration; Excel/Google Sheets/Gnumeric/CSV do not (elapsed time is a number format over a number) — 2 of 6 vendors, so by the "reliably made" rule it does not earn a kind, and the correct canonical lex is `Number` in days. Decide when a real file forces it; the throw is the interim.
- ~~`ProjectionContext.Root(ISpace)` discards its space argument~~ (null-check only; `Locate` derived availability from the space passed at failure time instead) — **resolved in the wrap-up round following this one:** `Root` now owns its space, closing the question before the decomposition trace is built. The `Locate`-derivation note is left here only as history of the pre-fix behavior.
- ~~A blank band is a separator, never a terminator~~ and ~~`Repeat` cannot stop gracefully before trailing content~~ — **both resolved by `.Until`.** A blank band still means "separator, never terminator", deliberately and unchanged; what was missing was a way to say where a repeat ends, and that is now `VerticalRepeat(item, separatedBy: BlankRows()).Until(RowContaining("..."))`. The bound is consumed in full, so the shape after it anchors on the landmark at distance zero. `examples/investor-irr.xlsx` is the worked case: two caption-separated series parsed by one `Repeat` declared once and placed twice.
- ~~`Cells` may want to be `Range`~~ — **DECIDED (owner, 2026-09-01): renamed.** `Cells` read as "some cells" where the shape is a rectangular region. All three factory overloads and their descriptions are now `Range`/`Range(w, h)`; the `CellBlock` view is unchanged, and `Range` does not collide with `System.Range` or `Enumerable.Range` (nothing imports the latter statically).
- **Use-site name capture cannot reach `Choice`.** `Next` and `VerticalRepeat`/`HorizontalRepeat` both capture their argument's text, but `Choice(params IProjection<T>[] alternatives)` cannot: `CallerArgumentExpression` targets one parameter, and a `params` array collapses every alternative into it, so there is no per-argument text to capture. Alternatives therefore still render by description unless explicitly `.Named`. Fixing it would mean giving up `params` for fixed arities — the arity explosion this vocabulary just finished removing — so it stays. (`Else(fallback)` does capture, since phase A.)
- ~~Two parallel mapping APIs~~ — resolved in wave 2 and closed by the retirement: `RegionMapper`/`RegionMapperFactory`/`IRegionMapper` went in wave 2, `RegionExtensions.Map` with the region stack; `IProjection<T>` is the only mapping API.
- ~~Arity explosion~~ — closed for good: a layout composite takes one cursor lambda (`VerticalFlow(v => ...)`), so there is no arity anywhere; the applicative `StackShape`/tuple machinery and `Region1/2/3` are all deleted.

(Resolved in the 2026-08-31 session: `uint` vs `int` — codebase is all-`int`; row/column composition asymmetry — both halves public since the wave-1 mirror collapse; `TakeRows(n)`/`TakeColumns(n)` factories added.)

Recorded for later by the wrap-up (Copse-cadence) review of 2026-09-02 — none blocking, each with its reason on record:

- **`CellMatching` may belong in Core, public, beside `CellValue`** — it is policy over the canonical vocabulary; publishing it would let consumers write predicates under the exact rules `RowContaining` uses and would remove one `InternalsVisibleTo` reason. An API expansion deserving its own decision.
- **Where does the dry-run renderer live?** `IOpaqueComposite` is internal; a renderer outside `Unrect` would read `Children.Count == 0` on every layout composite and render exactly the lie the marker exists to prevent. Decide before wave-3 tooling starts.
- **`OutOfBoundsException` diagnostics** (above) and a public `Description` on `AnchorNotFoundException` should be solved together — the latter would remove the last `InternalsVisibleTo` reason but is subsumed by the former.
- ~~`Unrect.Excel` depends on `Unrect.Array`~~ — **resolved.** There is one in-memory space: `GridSpace` (in Core at the time, in `Unrect` since the 2026-09-03 charter), which `Unrect.Spreadsheets` builds directly. The pre-publish amendment folded the `Create` overloads into it, deleted `ArraySpace` (a delegation shell that did not earn a type) and deleted the `Unrect.Array` project — which also ends that namespace's shadowing of `System.Array`.
- **`ProjectionContext` does three jobs** (tree position, sheet position, diagnostics/naming) at ~300 lines; split the rendering half into a `PathRenderer` if the decomposition trace pushes it much past 450.
- ~~`StrategyTests.cs` (~950 lines) should split along its 18 section headers~~ — **resolved 2026-09-08** in the pre-merge cleanup: split verbatim along its own headers into `Strategies/{SizeStrategyTests, OffsetStrategyTests, RowAndColumnStrategyTests, LandmarkAndLiftTests}.cs` (landmarks and lifts kept together — CLAUDE.md frames them as one family), verified by line-multiset diff (zero lines lost).
- ~~`EnforceCodeStyleInBuild`~~ — **flipped 2026-09-08** in the pre-merge cleanup, deliberately, with the discovery that the flip alone is inert: IDE0005 needs `dotnet_diagnostic.IDE0005.severity = warning` in .editorconfig as its partner (both now in place, proven load-bearing). The triage found 19 unused usings and zero of anything else; IDE0051/52/35/79 probed clean; IDE0060 deliberately NOT enabled (CellReading's ignored delegate-shape parameters are load-bearing).

## Where Work Left Off

The library is mature and review-hardened: a canonical `CellValue` model (Core), the fused
projection layer (`Unrect.Projections` — typed leaves, the layouts `VerticalFlow`/`HorizontalFlow`/
`Overlay`, the five-rung `Table` family, `Repeat`/`Choice`, matchers, `Caption`, `Fields`, tolerance
boundaries `.Else`/`.Optional`), and two doors onto a spreadsheet: the eager `SpreadsheetSpace` and the
streaming `Workbook` (windowed store + lead/chase reader pool + lazy extents — Parts 1 and 2 shipped).
Capabilities (the `IFormulaSpace`/`Formula()` stack, opt-in via `CreateWithFormulas`) and Entry C — the
file-scoped `ProjectionBuilders<TSpace>` vocabulary with a pipeline-only placement calculus (all postfix
geometry retired; geometry is the pipeline, so erasure is unspellable at compile time) plus the
`UNR001`–`UNR003` analyzers — are all shipped. Adapter-level string interning
(`WorkbookOptions.MaxInternedStrings`) trades a book-lifetime table for retention. `docs/vocabulary.md`
is the operator survey; `docs/streaming.md` the streaming guide. Published through **v0.3.0-alpha.1**;
the placement renovation is complete but unreleased (no v0.4.0 bump yet). Suite ~2,170 green on
`net8.0`, both library TFMs 0 warnings, `net48` verified on Windows.

Per-corner design rationale that isn't in code lives in git history — the design-spec pile (waves,
projection-model, placement-renovation, streaming, presence, congruence, and the labeled-axes
step-chain) was gutted 2026-09-12 to stop stale specs contradicting current work. The surviving design
docs are the only live ones: `composing-primitives.md`, `static-boundary-spec.md`,
`placement-spelling-rounds.md`, `modifier-congruence-survey.md`.

**Current work — branch `experiment/record-primitive`** (the record / labeled-axes campaign). Shipped
this session: the labels-as-context primitives (`ColumnLabels`/`WithColumnLabels`/`Record`), the
`SkipToFirstNonBlankCell` offset strategy, the `Table onBlank` blank-row strategy
(`Stop`/`Skip`/`Fault`/`Tolerate`/`blankRecord`), the **uniform offset law** — a declared pipeline
offset *replaces* a shape's own default (there is no offset-vs-movement distinction; one `Steps.Offset`
rule, composing only onto what the pipeline itself declared) — and, for one rung,
`docs/design/composing-primitives.md`'s live goal itself: **`Table(headerRows, eachRow)` composed from
primitives and streaming.** GAP A closed a second time, differently than first planned: not a repeat
re-hosting its own bound, but the bound living on the COMPOSITE's placement while the engine's
placement seams (`ProjectionEngine.Exceeds`, `FlowState.Next`, `RepeatProjection.ItemExtent`) became
bound-aware, so a discovered extent now streams through a flow/overlay/repeat instead of forcing at
first-child placement. The tiler (`VerticalBands`/`HorizontalBands` — cut a real fixed-stride band,
search for nothing) carries `Table`'s body. `UnitProjection` (a generic named-wrapper-over-a-composed-
body primitive) plus `AsScaffolding` and the collapsing path fold in `ProjectionContext` close GAP B:
the composed rung's failure paths are byte-identical to the leaf's. Remaining: retiring the other four
rungs (`Table<T>()`, the `LabelMap` bind, `Table()`, the `Table(row => ...)`/`Table(view => ...)`
escape hatches) onto the same `UnitProjection` treatment, one at a time; and the deferred lazy seam —
a strategy in `Unrect.Strategies` that itself reads `ISpace.Area` inside a child's DECLARED area still
forces (a bare `VerticalRepeat(Record(record))` inside a bound still forces at the first occurrence).

**Roadmap:** the decomposition trace, the dry-run renderer, `IFormattingSpace`, streaming Part 3, and
the RECORD CAMPAIGN (`placement-spelling-rounds.md`).

## Test Fixture Policy

`examples/scrubbed-k1.xlsx` is a scrubbed real fund K-1 workbook (63x2772, 169 sections)
used as a LOCAL-ONLY acceptance target — it is gitignored and must never be committed,
nor copied into `src/Unrect.Tests/TestData/`. The working practice: when the K-1 file
exposes a corner case (error cells, whitespace-only cells, repeating numbered groups,
multi-row headers...), distill it into a small synthetic workbook that IS committed and
tested. Automated tests must never depend on the scrubbed file's presence.

## Example Usage

- `linqpad/simple-report.linq` — parses `examples/simple-report.xlsx`: a header flow of typed leaves (the hard-coded `Column(4, …)` height dissolved into the child count) over `Table<Transaction>` with the two demonstration caption overrides; table defaults absorb the blank gap and header row. Zero accessor calls.
- `linqpad/investors-by-deal.linq` — parses `examples/investors-by-deal.xlsx`: one deal-block `VerticalFlow` (`Text()` over `Table<DealTransaction>()`), applied with `VerticalRepeat(deal, separatedBy: BlankRows())`. **All six captions bind with nothing declared** — the script that shows the caption comparer earning its keep.
- `linqpad/investor-summary.linq` — the reference report (`examples/investor-summary.xlsx`), and **deliberately the corpus's one worked example of the lambda table form**: its two tables keep their `Table(r => …)` spelling so the escape hatch appears somewhere, and its discovered `Column(c => …)` header is left alone because a discovery is never traded for a child count: discovered header height, summary table, and a nested `VerticalRepeat` of per-investor blocks (`atLeast: 1`), plus the post-parse correlation check (summary rows == detail blocks).
- `linqpad/investor-irr.linq` — `examples/investor-irr.xlsx`: the `Heading`/`.Of`/`.Until` demonstration (migrated to the pipeline canon in phase 4). Two heading-separated series of the same per-investor blocks, parsed by ONE `VerticalRepeat` declared once and placed twice — the first `Until(RowContaining(Inception)).Heading("IRR Details").Heading("Cash Flows Using Transfer Date").Of(irrDetails)`, the second `Heading(Inception).Of(irrDetails)` with the shared literal as a `const`. All three heading rows are consumed and attributed. Consumes the whole sheet, no diagnostics.
- `linqpad/array.linq` — shapes over an in-memory 2D integer array with `GridSpace.Create(nums, isBlank: v => v == 0)`: `VerticalRepeat(block, separatedBy: BlankRows())` over a `VerticalFlow` of `Row` then `Range`; the example that shows the vocabulary is not Excel-specific.
- `linqpad/edge-cases.linq` — `examples/edge-cases.xlsx` (the first distilled corner-case fixture): the `Error` kind end-to-end, whitespace-vs-empty-vs-absent blankness under default and strict `isBlank`, how blankness changes discovered extents, and a section printing the typed leaves' kind-vs-conversion diagnostic sentences.
- `linqpad/scrubbed-k1.linq` — parses the LOCAL-ONLY `examples/scrubbed-k1.xlsx` (gitignored; script fails without it): an `Overlay` header that digests itself into `{Entity, AtaxColumn, Columns}` by resolving fund columns from content (the entity card declared via `Fields`), one `section` shape placed twice via `Heading(...).Of(section)` (K-1 lines, and portfolio income with `.Optional()`), and a fund-centric pivot validated by `AllAllocationsSumToFederal`. The unconsumed-space `Info` doubles as the campaign burn-down.
