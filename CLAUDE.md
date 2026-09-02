# Unrect

## Status

This project is experimental, but both the substrate (CellValue, spaces, strategies) and the shape layer (`Unrect.Shapes`) now have deliberate, review-hardened semantics pinned by `src/Unrect.Tests` (xUnit, 906 tests). Run `dotnet test src/Unrect.sln`; keep it green. Gate builds with `dotnet build src/Unrect.sln -v q --no-incremental` — incremental builds silently skip analyzer diagnostics (xUnit analyzers etc.), so a plain build can report 0 warnings while warnings exist.

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

- **Shapes describe; they do not execute.** Constructing a shape should be side-effect free. Decomposition happens when the description is applied to a space (`Map`/`Apply`), not while the description is being assembled.
- **Strategies are the escape hatch for dynamism — but they are still declarative.** Variable-sized regions are handled by declaring *how* a boundary is determined (a predicate, a size rule), not by the user imperatively scanning for it. If a use case seems to require the user to write cursor logic, the right fix is a new strategy or combinator, not an imperative API.
- **Mapping is projection, not parsing.** By the time map functions run, decomposition is complete. Map functions read from an extent handed to them; they never influence or perform boundary decisions. One deliberate exception: the cursor-lambda form (`VerticalFlow(v => ...)`) interleaves projection with the flow's decomposition, which makes value-dependent shape choice *expressible* — allowed because the API cannot prevent it, discouraged in the docs, and nothing is added to encourage it (see `docs/design/combined-select-experiment.md` §7).
- **Evaluate new features against this test:** does it let the user *say what the data looks like*, or does it make them *say how to walk it*? The former belongs in Unrect; the latter is a design smell. A second lens on the same question: could a writer execute this declaration — could it produce the file as well as read it? Declarations run backward; opaque code does not.
- **Explicit dimensions are the exception, not the rule.** Almost every boundary should be discovered by a strategy (predicate-based sizes, skip-while offsets). Hard-coded offsets/sizes are acceptable only for structurally fixed regions — e.g., a report header whose shape is part of the format's definition. A hard-coded count that merely *happens* to match today's file (a gap of 2 blank rows, a table 4 columns wide) is a fragility bug waiting for the next export.

## Architecture

### Core Metaphor

A "space" is a 2D rectangular grid of values. Spaces can be subdivided into subspaces (via offset and area), and a declared shape decomposes a space into a hierarchy of subspaces, projecting each to a typed value as it goes. Strategies determine how to compute boundaries (sizes, offsets, row/column counts) dynamically.

### Project Structure

| Project | Purpose |
|---|---|
| **Unrect.Core** | Canonical value model (`CellValue`, `CellKind`, `CellError`), core abstractions (`ISpace`, strategy interfaces, `IRowLandmark`/`IColumnLandmark`), primitives (`Size`, `Offset`, `Area`), and the in-memory adapter `GridSpace` — a `CellValue[,]` viewed as an `ISpace`, entered either by constructor (cells already canonical) or by `Create<T>(values, map)` and the primitive overloads with blank predicates (a plain array, lexed here, blankness decided here). An adapter needing no third-party reader lives in Core; only a vendor-backed one earns a package. |
| **Unrect** | The shape layer (`Unrect.Shapes`): the `Shape` vocabulary, `IShape<T>`, `ShapeEngine`, the composites and primitives, the cell views (`CellStrip`/`CellBlock`/`TableView`), and diagnostics — plus the shared `Orientation` enum and the `CallerArgumentExpressionAttribute` polyfill (netstandard2.1 has no built-in one; it backs use-site name capture) |
| **Unrect.Strategies** | Strategy implementations for computing sizes, offsets, rows, and columns |
| **Unrect.Spreadsheets** | `SpreadsheetSpace` — reads spreadsheet files (`.xls`/`.xlsx` via ExcelDataReader) and adapts cells to `CellValue`. Named for the family, not the vendor: further formats belong here rather than in a second package. |

All library projects target .NET Standard 2.1 (`Unrect.Tests` is `net8.0`).

### Data Flow

```
Excel file / 2D array
    -> adapter normalizes to CellValue   ("lexing": backend values -> canonical vocabulary)
    -> ISpace                            (uniform grid of CellValue)
    -> Shape + Strategies                (declarative shape description)
    -> shape.Map(space)                  (hierarchical decomposition and projection, fused)
    -> typed objects
```

### Key Abstractions

- **`CellValue` / `CellKind`** — The canonical cell vocabulary (Blank, Text, Number, Temporal, Boolean, Error). One `Number` kind with granular checked accessors (`GetDouble`/`GetDecimal`/`GetInt`); numbers created from `decimal`/`int`/`long` retain an exact decimal alongside the double. Blankness is decided at adaptation time (e.g., `GridSpace.Create(nums, isBlank: v => v == 0)`); `Blank` is a singleton kind, so strategies just test `IsBlank`/`HasValue`.
- **`ISpace`** — A 2D rectangular grid of `CellValue` with subspace slicing. Non-generic since the wave-1 canonical-model refactor (see `docs/design/canonical-model-and-shapes.md`).
- **Strategies** — Pluggable functions that determine spatial boundaries:
  - `ISizeStrategy` — computes a `Size` from available space
  - `IOffsetStrategy` / `IAreaStrategy` — adapted from `ISizeStrategy`
  - `IRowStrategy` / `IColumnStrategy` — predicate-based row/column selection
  - Blankness conveniences: `OffsetStrategies.SkipBlankRows()`/`SkipBlankColumns()`, `SizeStrategies.RowsWhileAnyValue()`, `RowStrategies.TakeRowsWhileAnyValue()`, `ColumnStrategies.TakeColumnsWhileAnyValue()`
  - Explicit counts: `RowStrategies.TakeRows(n)` / `ColumnStrategies.TakeColumns(n)` — these throw `OutOfBoundsException` rather than clamp, consistent with `ExplicitArea`
  - `SizeStrategies.RowsWhileAny(predicate)` / `RowsWhileAnyValue()` + `.ToAreaStrategy()` (width = full available width, height = leading rows in which at least one cell satisfies the predicate) — the preferred way to size a data region ("rows while any cell has a value") instead of explicit bounds or `MaxArea`
  - `OffsetStrategies.SkipRowsWhileAll(predicate)` / `SkipRowsWhileAny(predicate)` (via internal `RowOffsetSizeStrategy`, width always 0) declare a vertical offset such as "skip however many leading rows are entirely blank" — the declarative replacement for hard-coding gap heights; `SkipBlankRows()` is the zero-argument form. These do their primary work as the shape layer's defaults (e.g. `Table`'s placement); direct usage is pinned in `src/Unrect.Tests/StrategyTests.cs`
- **Shapes (`Unrect.Shapes`) — the user-facing API, and the only one.** A shape fuses declaration and projection: `shape.Map(space)` decomposes and projects in one call, and the shape is a reusable value safe to apply to many spaces at once. Vocabulary via `using static Unrect.Shapes.Shape`:
  - **Leaves** — `Cell`/`Row`/`Column`/`Range`, the six typed cell leaves `Text()`/`Decimal()`/`Integer()`/`Double()`/`Date()`/`Boolean()`, `Caption(text)`, `Field`/`Fields`. `Range` is the rectangular-region leaf, read through a `CellBlock`; the view keeps its name, only the factory is `Range`. `Caption` is an anchor row *declared*: it finds the row, asserts the text, consumes it at full width, and yields the file's own spelling. `Fields(Field("EIN"), …)` is the labelled-pair card — two columns by one row per field, extent from the child count, anchored on its own first label, keyed by the declared labels.
  - **The leaf firewall.** The typed leaves are closed over `CellValue`'s canonical accessor set and mirror it 1:1; there is no `Long()`, `Money()` or `Enum<T>()`, now or later, because each would be a new failure vocabulary and a new writer obligation, and the document has six kinds, two of which no leaf reads (`Blank` and `Error` are conditions, not values a leaf projects). A conversion beyond that set is `Select` territory. Nothing is added to Core to serve a leaf, and adding an accessor to Core does not add one (`GetDate` is a transformation of `GetDateTime`, so it has no leaf).
  - **Kind vs conversion.** A kind failure speaks the document's vocabulary — `expected Number at B4, found Text` — never the reader's; a `Number` that will not fit the CLR type asked for is reported as a conversion on a number that is really there — `the Number at B4 (1.5) is not a whole number`. One template (`CellReading`), shared by the leaves and the table binder, so a `Decimal()` leaf and a `decimal` column cannot describe the same cell differently.
  - **Layouts** — `VerticalFlow(v => ...)`, `HorizontalFlow(h => ...)`, `Overlay(o => ...)`. Each takes one `Layout<T>` lambda that declares its children by calling `v.Next(shape)` in order and builds the result where the parts are read; there is no arity. A flow divides its extent into bands, each child starting where the last left off; an overlay hands every child the whole extent to place itself in, so children may overlap. There is no applicative (tuple) spelling — it was removed once the lambda form proved out.
  - **Tables — a ladder of three.** `TableRows<T>()` binds captions to a type's members by name (case- and whitespace-insensitive, via the public `CaptionComparer`), with the member's own type choosing the kind and accessor; `TableRows<T>(bind => bind.Column(t => t.Date, "Transaction Date").Ignore(t => t.X))` adds per-member overrides and the per-member opt-out from strictness; `TableRows()` hands back `IReadOnlyDictionary<string, CellValue>` per row for exploring an unfamiliar sheet. `Table`/`TableRows(lambda)` survive as the escape hatch for a column whose kind varies. Binding is resolved once at construction — a bad member type is an error then, not per file — and is strict one way: every member must find a column, while a column no member claims is fine. A nullable member tolerates a *blank* and still fails on the wrong kind; an annotated `string?` member is blank-tolerant the same way `Nullable<T>` is, not just the value types.
  - **Repetition and alternation** — `Repeat(item, separatedBy:, atLeast:)`, `Choice`, `.Else`, `.Optional`.
  - **Modifiers** — `.Named`, `.After`/`.AfterBlankRows`/`.Down`/`.Right`, `.Sized`, `.Padded`, `.Until(landmark, orEnd:)`, `.Under(captions)`. `.Under` is sugar for a vertical flow — `x.Under(a, b)` is `VerticalFlow(v => { v.Next(a); v.Next(b); return v.Next(x); })` — so every caption is a real child with its own path segment, and it describes itself as `Under`.
  - **Matchers and their lifts** — one family for "a row that matches": `RowWhere`/`RowWithCell`/`RowContaining` and the column twins. A matcher only locates and reports absence; a *lift* decides what absence means. `To(m)` lands a shape ON the match (it owns that row), `Past(m)` one after, and `.Until(m, orEnd:)` bounds by it. There are no `Seek*` factories — `To`/`Past` replaced them, and `Past(m)` is what `Then(Seek…, SkipRows(1))` used to spell without the hard-coded 1. **The naming law:** a bare `Where`/`While` takes a space predicate `(space, index)`; a cell predicate is always marked (`WithCell`, `WhileAll`, `WhileAny`); `Containing` is whole-cell, trimmed, case-insensitive.
  - **Naming** — a child's path segment is the first of: its own `.Named`; the bare identifier it was written as at the capture site (`v.Next(transactions)` reads as `'transactions'`, inferred by `CallerArgumentExpression`); otherwise its kind and 1-based position, as `Cell#2`. `Repeat(item)`/`RepeatHorizontal(item)` capture the same way, so a hoisted item labels every occurrence — `Repeat(investorDetail)` renders `Repeat[2] -> 'investorDetail'`, where the index stays on the repeat's own segment and the label lands on the item's. A repeat's item has no ordinal to fall back on, so an inline one keeps its description. So hoist shapes into well-named locals and let the use site name them; a helper must not `.Named` what it returns, or every use site is called the same thing.
  - **`.Until(landmark)`** ends a shape's extent just before a content landmark (`RowContaining`, `RowWithCell`, `RowWhere`, and the column twins), consuming the bound in full so the next sibling starts *at* the landmark. A missing landmark is a loud, absorbable failure unless `orEnd: true`, which runs to the end of the space and records an `Info`.

  Placement is applied by `ShapeEngine` alone, exactly once, at every level including the root. Failures throw `ShapeException` with the declaration path and an A1 cell location. A layout composite is opaque to tooling — its children exist only while its lambda runs. `docs/vocabulary.md` is the complete operator survey, grouped by role with the cross-cutting laws, and the first place to check the current surface — it supersedes `wave2-shapes-spec.md` as the everyday reference now that spec's layout vocabulary (`StackShape`, the tuple factories, `Cells`) is gone; for the semantics behind each corner, see `docs/design/flow-vocabulary-spec.md` (layouts, naming), `docs/design/matcher-and-caption-spec.md` (matchers, `Caption`), `docs/design/typed-leaves-and-tables-spec.md` (typed leaves, tables, `Fields`), and `docs/design/diagnostics-and-choice-spec.md` (`Choice`, tolerance boundaries).

## Known Bugs

None currently known. (The historical list — `RegionBuilder1` double-offset, inverted `TakeColumnsWhileAny`, `SpreadsheetValueBase` equality contract — was fully resolved by the 2026-08-31 session: the wave-1 refactor, its code review, and the review-fix pass. Subspace resolution and bounds checking now live in `ShapeEngine`, the one code path that resolves a placement.)

## Design Direction

See `docs/design/canonical-model-and-shapes.md` for the agreed forward design: a
canonical cell value model that de-generifies the core (spaces as "lexers" adapting
backends into one value vocabulary, blankness decided at adaptation time), a
document-level shape vocabulary (`Table`, `Repeat`/separator, `Choice`) built on the
strategy calculus, capability seams for backend extras (formatting, native types)
under the rule that nothing in Core may require a capability, and an observability
roadmap (named regions, decomposition trace, dry-run renderer, unconsumed-space
warnings). New API work should be checked against that document.

**Wave 1 (canonical model) is implemented**: `CellValue`/`CellKind` live in Core, the
`TSpace` generic is gone from the entire surface, `GridSpace` is the in-memory adapter,
and `Unrect.Spreadsheets` (then named `Unrect.Excel`) is a thin adapter (`SpreadsheetValueBase` and friends are deleted).
**Wave 2 (fused shape vocabulary) is implemented** per `docs/design/wave2-shapes-spec.md`,
and **wave 3 part 1 (diagnostics, tolerance boundaries, `Choice`) is implemented** per
`docs/design/diagnostics-and-choice-spec.md`. Remaining from the roadmap: the decomposition
trace, dry-run renderer, and capability seams.

## Open Design Questions

- **Strategy layering** — `IAreaStrategy` and `IOffsetStrategy` are thin wrappers around `ISizeStrategy`. Whether this indirection earns its keep or should be collapsed is an open question. Adjacent and DECIDED (owner, 2026-09-02, pre-publish): `Area` stays a distinct `Size` wrapper and keeps both its `Size` property and the `Width`/`Height` passthroughs — the two-spellings wart is confined to engine plumbing that library consumers never operate at, so it does not justify surgery on `ISpace`/`Placement`/the strategy interfaces. Revisit only if the strategy-layering question itself is ever taken up.
- **`SpaceExtensions.GetSubspace(space, offset)` throws `ArgumentOutOfRangeException`** (from `Size`'s negative-length check) for an oversized offset, while the two-argument form correctly throws `OutOfBoundsException`. Unreachable through shapes (`ShapeEngine.TryPlace` rejects an oversized offset first) but publicly reachable on `ISpace` and inconsistent; deliberately not pinned by tests.
- **`OutOfBoundsException` carries no diagnostics** — no requested-vs-available extents, no location. The shape layer wraps everything in `ShapeException` with path + A1 location, so the bare type now surfaces only from strategies and from direct `ISpace` slicing.
- ~~`ShapeContext.Root(ISpace)` discards its space argument~~ (null-check only; `Locate` derived availability from the space passed at failure time instead) — **resolved in the wrap-up round following this one:** `Root` now owns its space, closing the question before the decomposition trace is built. The `Locate`-derivation note is left here only as history of the pre-fix behavior.
- ~~A blank band is a separator, never a terminator~~ and ~~`Repeat` cannot stop gracefully before trailing content~~ — **both resolved by `.Until`.** A blank band still means "separator, never terminator", deliberately and unchanged; what was missing was a way to say where a repeat ends, and that is now `Repeat(item, separatedBy: BlankRows()).Until(RowContaining("..."))`. The bound is consumed in full, so the shape after it anchors on the landmark at distance zero. `examples/investor-irr.xlsx` is the worked case: two caption-separated series parsed by one `Repeat` declared once and placed twice.
- ~~`Cells` may want to be `Range`~~ — **DECIDED (owner, 2026-09-01): renamed.** `Cells` read as "some cells" where the shape is a rectangular region. All three factory overloads and their descriptions are now `Range`/`Range(w, h)`; the `CellBlock` view is unchanged, and `Range` does not collide with `System.Range` or `Enumerable.Range` (nothing imports the latter statically).
- **Use-site name capture cannot reach `Choice`.** `Next` and `Repeat`/`RepeatHorizontal` both capture their argument's text, but `Choice(params IShape<T>[] alternatives)` cannot: `CallerArgumentExpression` targets one parameter, and a `params` array collapses every alternative into it, so there is no per-argument text to capture. Alternatives therefore still render by description unless explicitly `.Named`. Fixing it would mean giving up `params` for fixed arities — the arity explosion this vocabulary just finished removing — so it stays. (`Else(fallback)` does capture, since phase A.)
- ~~Two parallel mapping APIs~~ — resolved in wave 2 and closed by the retirement: `RegionMapper`/`RegionMapperFactory`/`IRegionMapper` went in wave 2, `RegionExtensions.Map` with the region stack; `IShape<T>` is the only mapping API.
- ~~Arity explosion~~ — closed for good: a layout composite takes one cursor lambda (`VerticalFlow(v => ...)`), so there is no arity anywhere; the applicative `StackShape`/tuple machinery and `Region1/2/3` are all deleted.

(Resolved in the 2026-08-31 session: `uint` vs `int` — codebase is all-`int`; row/column composition asymmetry — both halves public since the wave-1 mirror collapse; `TakeRows(n)`/`TakeColumns(n)` factories added.)

Recorded for later by the wrap-up (Copse-cadence) review of 2026-09-02 — none blocking, each with its reason on record:

- **`CellMatching` may belong in Core, public, beside `CellValue`** — it is policy over the canonical vocabulary; publishing it would let consumers write predicates under the exact rules `RowContaining` uses and would remove one `InternalsVisibleTo` reason. An API expansion deserving its own decision.
- **Where does the dry-run renderer live?** `IOpaqueComposite` is internal; a renderer outside `Unrect` would read `Children.Count == 0` on every layout composite and render exactly the lie the marker exists to prevent. Decide before wave-3 tooling starts.
- **`OutOfBoundsException` diagnostics** (above) and a public `Description` on `AnchorNotFoundException` should be solved together — the latter would remove the last `InternalsVisibleTo` reason but is subsumed by the former.
- ~~`Unrect.Excel` depends on `Unrect.Array`~~ — **resolved.** There is one in-memory space: `GridSpace` in Core, which `Unrect.Spreadsheets` builds directly. The pre-publish amendment folded the `Create` overloads into it, deleted `ArraySpace` (a delegation shell that did not earn a type) and deleted the `Unrect.Array` project — which also ends that namespace's shadowing of `System.Array`.
- **`ShapeContext` does three jobs** (tree position, sheet position, diagnostics/naming) at ~300 lines; split the rendering half into a `PathRenderer` if the decomposition trace pushes it much past 450.
- **`StrategyTests.cs` (~950 lines) should split along its 18 section headers**; the shape suites split at ~500 and it never did.
- **`EnforceCodeStyleInBuild`** would make unused usings (IDE0005) fail the gate — the wrap-up round proved `TreatWarningsAsErrors` alone cannot catch them. Flip it deliberately, with time to triage whatever other IDE rules it surfaces.

## Where Work Left Off

The 2026-08-31 session completed waves 1 AND 2 of the design doc: wave 1 (canonical `CellValue` model, full de-generification, adapter-owned blankness, review-hardened, 182-test suite) and wave 2 (the fused shape layer in `Unrect.Shapes` per `docs/design/wave2-shapes-spec.md` — applicative shape+projection fusion, `Table` with by-name access, `Repeat` with `sepBy` separators, named shapes, `ShapeException` diagnostics with paths and A1 locations). All four LINQPad scripts use the appropriate API and all three example workbooks parse end-to-end. Wave 3 observability has since shipped in part: `Choice`, tolerance boundaries (`.Else`/`.Optional`), and unconsumed-space warnings landed per `docs/design/diagnostics-and-choice-spec.md`. What remains from the roadmap is the decomposition trace, the dry-run renderer, and capability seams (see spec §7).

On branch `experiment/combined-select` the shape layer reached its final vocabulary. The cursor-lambda experiment (`docs/design/combined-select-experiment.md`) was judged and **adopted**, then generalised by `docs/design/flow-vocabulary-spec.md`: stack became flow (`VerticalFlow`/`HorizontalFlow`), `Overlay` joined the same `LayoutCursor` grammar, the applicative tuple spelling and its whole supporting cast were deleted (`StackShape`, the 14 tuple factories, the 7 overlay arities, the 7 tuple `Select` combines, and the untyped `ApplyUntyped`/`ProjectUntyped` path), children gained names from their use sites, and `.Until` gave declarations a content terminator. One spelling of a layout composite now exists.

Earlier on the same branch the region/builder substrate was **retired**: `Region`/`Region1/2/3`/`SuperRegion`, every `*RegionBuilder*`, `RegionExtensions`, `SubspaceResolver`, and Core's `IRegion`/`IRegionBuilder` are deleted, along with `BuilderTests` and `RepeatTests` (the shape layer had already superseded their coverage). The `Unrect` project is now the shape layer plus `Orientation`; `linqpad/array.linq` was converted to the shape API.

Once the final vocabulary was in place, an **invertibility audit** (`docs/design/invertibility-audit.md`) applied a writer lens to the whole surface — could a declaration run backward and produce the file it reads, the way a parser combinator's grammar can generate as well as recognize? — sorting every operator into inverts-as-is, discovery-strategy (correctly one-way), document knowledge trapped in a lambda (the findings), or correctly-one-way-for-other-reasons, and ranking the trapped-knowledge findings by remediation payoff. Three remediation phases followed, each closing a batch of findings:
- **Phase A** (the audit's mechanical items, no dedicated spec): filled the row/column mirror holes, re-exported the `.Sized` vocabulary onto `Shape`, and finished the naming ladder — including an attempt to capture a name at the root (`Map`/`Apply`) that was tried and reverted, because it collides with method-group application such as `spaces.Select(report.Map)`.
- **Phase B** (`docs/design/matcher-and-caption-spec.md`) unified the three separate row/column matching vocabularies into one family (`RowWhere`/`RowWithCell`/`RowContaining` and their column twins) with `To`/`Past` lifts replacing all twelve `Seek*` factories outright, and added the `Caption` leaf and `.Under` so an anchor row becomes declared content instead of an offset side effect.
- **Phase C** (`docs/design/typed-leaves-and-tables-spec.md`) added the six typed cell leaves behind the accessor firewall, the three-rung table ladder (`TableRows()` dictionaries, `TableRows<T>()` bound by caption, and the lambda escape hatch), and `Fields` for labelled-pair blocks; `docs/vocabulary.md` was written as the resulting cross-cutting survey of the whole algebra.

## Test Fixture Policy

`examples/scrubbed-k1.xlsx` is a scrubbed real fund K-1 workbook (63x2772, 169 sections)
used as a LOCAL-ONLY acceptance target — it is gitignored and must never be committed,
nor copied into `src/Unrect.Tests/TestData/`. The working practice: when the K-1 file
exposes a corner case (error cells, whitespace-only cells, repeating numbered groups,
multi-row headers...), distill it into a small synthetic workbook that IS committed and
tested. Automated tests must never depend on the scrubbed file's presence.

## Example Usage

- `linqpad/simple-report.linq` — parses `examples/simple-report.xlsx`: a header flow of typed leaves (the hard-coded `Column(4, …)` height dissolved into the child count) over `TableRows<Transaction>` with the two demonstration caption overrides; table defaults absorb the blank gap and header row. Zero accessor calls.
- `linqpad/investors-by-deal.linq` — parses `examples/investors-by-deal.xlsx`: one deal-block `VerticalFlow` (`Text()` over `TableRows<DealTransaction>()`), applied with `Repeat(deal, separatedBy: BlankRows())`. **All six captions bind with nothing declared** — the script that shows the caption comparer earning its keep.
- `linqpad/investor-summary.linq` — the reference report (`examples/investor-summary.xlsx`), and **deliberately the corpus's one worked example of the lambda table form**: its two tables keep their `TableRows(r => …)` spelling so the escape hatch appears somewhere, and its discovered `Column(c => …)` header is left alone because a discovery is never traded for a child count: discovered header height, summary table, and a nested `Repeat` of per-investor blocks (`atLeast: 1`), plus the post-parse correlation check (summary rows == detail blocks).
- `linqpad/investor-irr.linq` — `examples/investor-irr.xlsx`: the `.Under`/`.Until` demonstration. Two caption-separated series of the same per-investor blocks, parsed by ONE `Repeat` declared once and placed twice — the first `.Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date")).Until(RowContaining(Inception))`, the second `.Under(Caption(Inception))` with the shared literal as a `const`. All three caption rows are nodes. Consumes the whole sheet, no diagnostics.
- `linqpad/array.linq` — shapes over an in-memory 2D integer array with `GridSpace.Create(nums, isBlank: v => v == 0)`: `Repeat(block, separatedBy: BlankRows())` over a `VerticalFlow` of `Row` then `Range`; the example that shows the vocabulary is not Excel-specific.
- `linqpad/edge-cases.linq` — `examples/edge-cases.xlsx` (the first distilled corner-case fixture): the `Error` kind end-to-end, whitespace-vs-empty-vs-absent blankness under default and strict `isBlank`, how blankness changes discovered extents, and a section printing the typed leaves' kind-vs-conversion diagnostic sentences.
- `linqpad/scrubbed-k1.linq` — parses the LOCAL-ONLY `examples/scrubbed-k1.xlsx` (gitignored; script fails without it): an `Overlay` header that digests itself into `{Entity, AtaxColumn, Columns}` by resolving fund columns from content (the entity card declared via `Fields`), one `section` shape placed twice under `.Under(Caption(...))` (K-1 lines, and portfolio income with `.Optional()`), and a fund-centric pivot validated by `AllAllocationsSumToFederal`. The unconsumed-space `Info` doubles as the campaign burn-down.
