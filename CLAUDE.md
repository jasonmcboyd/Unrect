# Unrect

## Status

This project is experimental. The design is still fluid and no architectural decisions are set in stone. There are no unit tests yet — intentionally, since the abstractions are still being explored.

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
| **Unrect.Core** | Core abstractions: `ISpace<T>`, `IRegion<T>`, strategy interfaces, primitives (`Size`, `Offset`, `Area`) |
| **Unrect** | Region implementations (`Region`, `Region1/2/3`, `SuperRegion`), builders, mappers, and factory methods |
| **Unrect.Strategies** | Strategy implementations for computing sizes, offsets, rows, and columns |
| **Unrect.Array** | `ArraySpace<T>` — adapts a 2D array as `ISpace<T>` |
| **Unrect.Excel** | `SpreadsheetSpace` — reads Excel files via ExcelDataReader, exposes as `ISpace<SpreadsheetValueBase>` |

All projects target .NET Standard 2.1.

### Data Flow

```
Excel file / 2D array
    -> ISpace<T>               (uniform grid abstraction)
    -> RegionBuilder + Strategies  (declarative shape description)
    -> Region tree              (hierarchical decomposition)
    -> Map functions            (extract to typed objects)
```

### Key Abstractions

- **`ISpace<T>`** — A 2D rectangular grid of `T` values with subspace slicing.
- **`IRegion<T>`** — A node in the region tree. Holds an `ISpace<T>` and can yield subregions.
- **Strategies** — Pluggable functions that determine spatial boundaries:
  - `ISizeStrategy<T>` — computes a `Size` from available space
  - `IOffsetStrategy<T>` / `IAreaStrategy<T>` — adapted from `ISizeStrategy`
  - `IRowStrategy<T>` / `IColumnStrategy<T>` — predicate-based row/column selection
- **Builders** — Compose strategies to construct region trees (`RegionBuilder`, `StackRegionBuilder2/3`, `SuperStackRegionBuilder`)
- **Mappers** — Transform region trees into result objects

### Generic Parameter Convention

The generic parameter `TSpace` (used throughout the codebase) represents the **element type** of the space, not the space itself. `ISpace<int>` is "a space of ints." This follows the mental model of "what the space is composed of." There is an open question about whether this naming creates confusion given standard C# conventions where `TFoo` means "this type is a Foo."

## Known Bugs

- **`RegionBuilder1.Build()` applies offset twice** — After `space = space.GetSubspace(offset)`, the offset is consumed, but it's applied again in the subsequent `space.GetSubspace(offset, area)` call. The bounds check also uses the offset against the already-adjusted space. (`RegionBuilder.cs`)
- **`SpreadsheetValueBase` overrides `Equals` without `GetHashCode`** — Violates the .NET contract; will cause incorrect behavior in hash-based collections.
- **`StringSpreadsheetValue.GetValueType()` returns `typeof(double)`** — Copy-paste bug; the `_ValueType` field is correctly `typeof(string)` but the method ignores it. (Note: this file may be excluded from compilation in favor of the generic `SpreadsheetValue<T>`.)

## Known Incomplete Work

`SuperStackRegionBuilder.Build()` is implemented and verified end-to-end (contrary to earlier notes here): it repeatedly invokes a block-builder factory, applying each block's own offset/area strategies and advancing past each built block until the space is exhausted, yielding a `SuperRegion` with an `ImmutableArray<TSubregion>`. The canonical test case is `examples/investors-by-deal.xlsx` (repeating deal blocks — deal code row, column-header row, N transaction rows — separated by a blank row, block lengths varying per deal), parsed by `linqpad/investors-by-deal.linq`: block offset = `SkipRowsWhileAll(blank)`, block area = `WhileAny(has value)`. Caveats: there is no `RegionBuilderFactory` convenience method for it yet (users `new` it with an explicit `Region3<...>` type argument, which is verbose), and trailing all-blank rows after the last block would build a degenerate empty block and throw from the inner stack builder's bounds check — not reachable via `SpreadsheetSpace` (ExcelDataReader trims trailing blanks) but untested for other spaces.

`WhileAnySizeStrategy.GetSize()` is now implemented: width = full available width, height = leading rows in which at least one cell satisfies the predicate (delegates to `TakeToAnyRowStrategy`). Exposed via `SizeStrategies.WhileAny(predicate)` + `.ToAreaStrategy()`. This is the preferred way to size a data region ("rows while any cell has a value") instead of explicit bounds or `MaxArea`.

`OffsetStrategies.SkipRowsWhileAll(predicate)` / `SkipRowsWhileAny(predicate)` are also implemented (via internal `RowOffsetSizeStrategy`, width always 0): they declare a vertical offset such as "skip however many leading rows are entirely blank" — the declarative replacement for hard-coding gap heights. See `linqpad/simple-report.linq` for the canonical usage of all of these against `examples/simple-report.xlsx`.

## Dead Code

The old `SpreadsheetValue` interface and its per-type struct implementations (`DateTimeSpreadsheetValue`, `DoubleSpreadsheetValue`, `IntSpreadsheetValue`, `StringSpreadsheetValue`) are still in the source tree but excluded from compilation. They were superseded by the `SpreadsheetValueBase` / `SpreadsheetValue<T>` class hierarchy.

## Design Direction (proposed, not yet implemented)

See `docs/design/canonical-model-and-shapes.md` for the agreed forward design: a
canonical cell value model that de-generifies the core (spaces as "lexers" adapting
backends into one value vocabulary, blankness on the `ISpace` contract), a
document-level shape vocabulary (`Table`, `Repeat`/separator, `Choice`) built on the
strategy calculus, capability seams for backend extras (formatting, native types)
under the rule that nothing in Core may require a capability, and an observability
roadmap (named regions, decomposition trace, dry-run renderer, unconsumed-space
warnings). New API work should be checked against that document.

## Open Design Questions

- **Arity explosion** — `Region1`, `Region2`, `Region3` each require dedicated builder, mapper, and factory overloads. This doesn't scale well. Real-world reports may need more than 3 fixed subregions. A compositional approach (e.g., binary nesting or a different encoding) could eliminate this, but the right answer isn't clear yet.
- **Strategy layering** — `IAreaStrategy` and `IOffsetStrategy` are thin wrappers around `ISizeStrategy`. Whether this indirection earns its keep or should be collapsed is an open question.
- **`uint` vs `int` for dimensions** — `uint` is used throughout for sizes and indices, which causes constant casting friction. Decision: switch to `int`. The unsigned guarantee isn't worth the ergonomic cost, especially given that Excel's upper bound is ~1M rows, well within `int` range.
- **Filename typo** — `SupterStackRegionBuilder.cs` should be `SuperStackRegionBuilder.cs`.
- **Leaf builders ignore their own strategies at the top level** — `RegionBuilder<TSpace>.Build(space)` just wraps the space; a builder's offset/area strategies are applied by its *parent* (`StackRegionBuilderBase.GetSubregionSpaces` / `RegionBuilder1`). Calling `Build` directly on a leaf builder with strategies silently produces the whole space. Should the top-level `Build` apply the builder's own strategies, or should this be an error?
- **Row/column composition is asymmetrically public** — rows-then-columns is public (`ColumnStrategies.TakeColumnsWhile*` extension methods on `IRowStrategy` return `IAreaStrategy`), but columns-then-rows is `internal` (the `TakeRowsWhile*` extensions on `IColumnStrategy` in `RowStrategies.cs`). The internal half should probably become public for symmetry. A `TakeRows(int count)` / `TakeColumns(int count)` factory would also help: "exactly 1 row" currently requires the awkward `TakeRowsWhile((s, r) => r < 1)`.

## Where Work Left Off

The most recent commits added Excel file parsing support:
- `e485815` — "First pass at creating a Space for Excel spreadsheets"
- `f129f48` — "Successfully parsing spreadsheet"

The Excel data source works, and the core use case is now proven end-to-end: `linqpad/investors-by-deal.linq` parses a repeating-block report (N deal blocks of varying length) with no explicit bounds except the structural deal-code cell. Likely next steps are ergonomics-driven: a `RegionBuilderFactory` convenience for `SuperStackRegionBuilder`, `TakeRows(int)`/`TakeColumns(int)` factories, and the open questions around arity explosion and fusing mapping into the declaration.

## Example Usage

- `linqpad/simple-report.linq` — parses `examples/simple-report.xlsx` (vertical header block, blank gap, column headers, data table). Demonstrates strategy-driven boundaries: `SkipRowsWhileAll` for the gap, `TakeColumnsWhileAny` for header width, `WhileAny` for data height. Only the report header uses explicit bounds.
- `linqpad/investors-by-deal.linq` — parses `examples/investors-by-deal.xlsx`, the repeating-block report. Demonstrates `SuperStackRegionBuilder`: one declared deal block applied N times, block lengths discovered per block.
- `linqpad/array.linq` — region tree from a 2D integer array; exercises `Vertical`/`Horizontal` stacking, explicit offsets/sizes, and the `Map` API.
