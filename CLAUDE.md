# Unrect

## Status

This project is experimental. The design is still fluid above the canonical model, but the bottom layer (CellValue, spaces, strategies, builders) has deliberate, review-hardened semantics pinned by `src/Unrect.Tests` (xUnit, 182 tests). Run `dotnet test src/Unrect.sln`; keep it green. Layers still being explored (shape vocabulary, mapping) intentionally have no tests yet.

## Problem Domain

Unrect addresses a real-world problem common in financial institutions: hierarchical data stored in flat 2D structures (primarily Excel spreadsheets). These are not simple tabular datasets — they contain nested, heterogeneous regions that need to be parsed into structured objects.

A typical example is an IRR report with:
- A header section (title, date, generic report info)
- Repeating client blocks, one per client
- Within each client block, sub-sections like capital calls, fundings, etc.

Row-oriented parsers are the wrong tool for this. They devolve into stateful cursor logic and fragile index tracking. Unrect takes a different approach: declarative 2D spatial decomposition. You describe the shape of the data once, and the framework handles decomposition into a typed region tree that can be mapped to objects.

## Design Philosophy: Declarative, Not Imperative

This is the core design commitment of the project, and it constrains every API decision.

A user of Unrect **declares the shape** of the data — "a header, then N repeating client blocks, each containing a capital-calls section sized by this predicate" — as a static description built from builders and strategies. The framework interprets that description to perform the decomposition. The user never writes traversal logic: no cursors, no "current row" state, no manual index arithmetic, no loops that walk the grid deciding what comes next.

The distinction matters because the imperative alternative is the failure mode this project exists to escape. Row-oriented parsers force the shape of the data to live implicitly in control flow, where it is fragile and unreadable. In Unrect, the shape is a first-class value: a composition of strategies and builders that can be inspected, reused, and reasoned about independently of any particular spreadsheet.

Practical implications for API design:

- **Builders describe; they do not execute.** Constructing a builder should be side-effect free. Decomposition happens when the description is applied to a space (e.g., `Build`), not while the description is being assembled.
- **Strategies are the escape hatch for dynamism — but they are still declarative.** Variable-sized regions are handled by declaring *how* a boundary is determined (a predicate, a size rule), not by the user imperatively scanning for it. If a use case seems to require the user to write cursor logic, the right fix is a new strategy or combinator, not an imperative API.
- **Mapping is projection, not parsing.** By the time map functions run, decomposition is complete. Map functions read from a region handed to them; they never influence or perform boundary decisions.
- **Evaluate new features against this test:** does it let the user *say what the data looks like*, or does it make them *say how to walk it*? The former belongs in Unrect; the latter is a design smell.
- **Explicit dimensions are the exception, not the rule.** Almost every boundary should be discovered by a strategy (predicate-based sizes, skip-while offsets). Hard-coded offsets/sizes are acceptable only for structurally fixed regions — e.g., a report header whose shape is part of the format's definition. A hard-coded count that merely *happens* to match today's file (a gap of 2 blank rows, a table 4 columns wide) is a fragility bug waiting for the next export.

## Architecture

### Core Metaphor

A "space" is a 2D rectangular grid of values. Spaces can be subdivided into subspaces (via offset and area), and those subspaces are organized into a hierarchical tree of "regions." Strategies determine how to compute boundaries (sizes, offsets, row/column counts) dynamically.

### Project Structure

| Project | Purpose |
|---|---|
| **Unrect.Core** | Canonical value model (`CellValue`, `CellKind`), core abstractions (`ISpace`, `IRegion`, strategy interfaces), primitives (`Size`, `Offset`, `Area`) |
| **Unrect** | Region implementations (`Region`, `Region1/2/3`, `SuperRegion`), builders, mappers, and factory methods |
| **Unrect.Strategies** | Strategy implementations for computing sizes, offsets, rows, and columns |
| **Unrect.Array** | `ArraySpace` — adapts 2D arrays as `ISpace` via `Create<T>(values, map)` and primitive overloads with blank predicates |
| **Unrect.Excel** | `SpreadsheetSpace` — reads Excel files via ExcelDataReader, adapts cells to `CellValue` |

All projects target .NET Standard 2.1.

### Data Flow

```
Excel file / 2D array
    -> adapter normalizes to CellValue   ("lexing": backend values -> canonical vocabulary)
    -> ISpace                            (uniform grid of CellValue)
    -> RegionBuilder + Strategies        (declarative shape description)
    -> Region tree                       (hierarchical decomposition)
    -> Map functions                     (extract to typed objects)
```

### Key Abstractions

- **`CellValue` / `CellKind`** — The canonical cell vocabulary (Blank, Text, Number, Temporal, Boolean). One `Number` kind with granular checked accessors (`GetDouble`/`GetDecimal`/`GetInt`); numbers created from `decimal`/`int`/`long` retain an exact decimal alongside the double. Blankness is decided at adaptation time (e.g., `ArraySpace.Create(nums, isBlank: v => v == 0)`); `Blank` is a singleton kind, so strategies just test `IsBlank`/`HasValue`.
- **`ISpace`** — A 2D rectangular grid of `CellValue` with subspace slicing. Non-generic since the wave-1 canonical-model refactor (see `docs/design/canonical-model-and-shapes.md`).
- **`IRegion`** — A node in the region tree. Holds an `ISpace` and can yield subregions.
- **Strategies** — Pluggable functions that determine spatial boundaries:
  - `ISizeStrategy` — computes a `Size` from available space
  - `IOffsetStrategy` / `IAreaStrategy` — adapted from `ISizeStrategy`
  - `IRowStrategy` / `IColumnStrategy` — predicate-based row/column selection
  - Blankness conveniences: `OffsetStrategies.SkipBlankRows()`/`SkipBlankColumns()`, `SizeStrategies.RowsWhileAnyValue()`, `RowStrategies.TakeRowsWhileAnyValue()`, `ColumnStrategies.TakeColumnsWhileAnyValue()`
  - Explicit counts: `RowStrategies.TakeRows(n)` / `ColumnStrategies.TakeColumns(n)` — these throw `OutOfBoundsException` rather than clamp, consistent with `ExplicitArea`
- **Builders** — Compose strategies to construct region trees (`RegionBuilder`, `StackRegionBuilder2/3`, `SuperStackRegionBuilder`)
- **Mappers** — Transform region trees into result objects

## Known Bugs

None currently known. (The historical list — `RegionBuilder1` double-offset, inverted `TakeColumnsWhileAny`, `SpreadsheetValueBase` equality contract — was fully resolved by the 2026-08-31 session: the wave-1 refactor, its code review, and the review-fix pass. Subspace resolution and bounds checking are now centralized in `SubspaceResolver`.)

## Known Incomplete Work

`SuperStackRegionBuilder` is implemented and hardened: reached via `RegionBuilderFactory.Repeat(blockBuilder)` (and `RepeatHorizontal`), it takes the block builder directly (builders are immutable descriptions — no factory `Func<>` needed), applies each block's own offset/area strategies, and terminates safely on trailing blank bands, zero-area blocks, and zero-advance shapes. The canonical test case is `examples/investors-by-deal.xlsx` (repeating deal blocks — deal code row, column-header row, N transaction rows — separated by a blank row, block lengths varying per deal), parsed by `linqpad/investors-by-deal.linq`: block offset = `SkipBlankRows()`, block area = `RowsWhileAnyValue()`.

`RowsWhileAnySizeStrategy` (width = full available width, height = leading rows in which at least one cell satisfies the predicate) is exposed via `SizeStrategies.RowsWhileAny(predicate)` / `RowsWhileAnyValue()` + `.ToAreaStrategy()`. This is the preferred way to size a data region ("rows while any cell has a value") instead of explicit bounds or `MaxArea`.

`OffsetStrategies.SkipRowsWhileAll(predicate)` / `SkipRowsWhileAny(predicate)` are also implemented (via internal `RowOffsetSizeStrategy`, width always 0): they declare a vertical offset such as "skip however many leading rows are entirely blank" — the declarative replacement for hard-coding gap heights. `SkipBlankRows()` is the zero-argument form. See `linqpad/simple-report.linq` for the canonical usage of all of these against `examples/simple-report.xlsx`.

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
`TSpace` generic is gone from the entire surface, `ArraySpace` is a mapping adapter,
and `Unrect.Excel` is a thin adapter (`SpreadsheetValueBase` and friends are deleted).
Waves 2+ (shape vocabulary, observability, map fusion) are not started.

## Open Design Questions

- **Arity explosion** — `Region1`, `Region2`, `Region3` each require dedicated builder, mapper, and factory overloads. This doesn't scale well. Real-world reports may need more than 3 fixed subregions. A compositional approach (e.g., binary nesting or a different encoding) could eliminate this, but the right answer isn't clear yet.
- **Strategy layering** — `IAreaStrategy` and `IOffsetStrategy` are thin wrappers around `ISizeStrategy`. Whether this indirection earns its keep or should be collapsed is an open question.
- **Leaf builders ignore their own strategies at the top level** — `RegionBuilder.Build(space)` just wraps the space; a builder's offset/area strategies are applied by its *parent* (`StackRegionBuilderBase.GetSubregionSpaces` / `RegionBuilder1`). Calling `Build` directly on a leaf builder with strategies silently produces the whole space. The QA pass sharpened this: `Builder(offset, area, subregionBuilder)` reads as "position the subregion here" but the strategies belong to the outer builder, so a top-level `Builder(1, 1, 2, 2, Builder()).Build(space)` silently hands the subregion the entire space (correct spelling: `Builder(Builder(1, 1, 2, 2))`). Should the top-level `Build` apply the builder's own strategies, or throw when called directly with non-default strategies? Two tests pin the current behavior (`Builder1_AppliesItsSubregionOffsetAndAreaExactlyOnce`, `Builder1_OwnStrategiesPositionItWithinItsParent`) and must be updated when this is decided.
- **`SpaceExtensions.GetSubspace(space, offset)` throws `ArgumentOutOfRangeException`** (from `Size`'s negative-length check) for an oversized offset, while the two-argument form correctly throws `OutOfBoundsException`. Unreachable through builders (`SubspaceResolver` pre-checks) but publicly reachable and inconsistent; deliberately not pinned by tests.
- **`OutOfBoundsException` carries no diagnostics** — no requested-vs-available extents, no location. Cheap win to fold into the wave-3 named-regions/error-message work.
- **Two parallel mapping APIs** — `RegionExtensions.Map` (tested, used by all examples) and the `IRegionMapper`-returning `RegionMapperFactory` overloads (untested, unused). Decide which survives as part of the map-fusion design; the loser should be deleted.

(Resolved this session: `uint` vs `int` — codebase is all-`int`; the `SupterStackRegionBuilder.cs` filename typo; row/column composition asymmetry — both halves public since the wave-1 mirror collapse; `TakeRows(n)`/`TakeColumns(n)` factories added.)

## Where Work Left Off

The most recent commits added Excel file parsing support:
- `e485815` — "First pass at creating a Space for Excel spreadsheets"
- `f129f48` — "Successfully parsing spreadsheet"

The 2026-08-31 session completed wave 1 of the design doc end-to-end: canonical `CellValue` model, full de-generification, adapter-owned blankness, `Repeat` factory, a code-review pass whose 21 findings were all fixed (including two latent `SuperStackRegionBuilder` termination bugs and null-hole hardening in `SpreadsheetSpace`), and a root `.editorconfig`. All three LINQPad scripts use the current API. Next up per the design doc: wave 2 (document-level shape vocabulary — `Table`, separator-aware repeat, `Choice`, named regions) and wave 3 (observability), with map fusion and the arity question after that.

## Example Usage

- `linqpad/simple-report.linq` — parses `examples/simple-report.xlsx` (vertical header block, blank gap, column headers, data table). Demonstrates strategy-driven boundaries: `SkipRowsWhileAll` for the gap, `TakeColumnsWhileAny` for header width, `WhileAny` for data height. Only the report header uses explicit bounds.
- `linqpad/investors-by-deal.linq` — parses `examples/investors-by-deal.xlsx`, the repeating-block report. Demonstrates `SuperStackRegionBuilder`: one declared deal block applied N times, block lengths discovered per block.
- `linqpad/array.linq` — region tree from a 2D integer array; exercises `Vertical`/`Horizontal` stacking, explicit offsets/sizes, and the `Map` API.
