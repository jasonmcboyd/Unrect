# Design: Canonical Value Model, Shape Vocabulary, and Observability

**Status:** Wave 1 implemented and review-hardened (2026-08-31): `CellValue`/`CellKind`
in Core, full de-generification, `ArraySpace` mapping adapter, Excel adapter slimmed,
`Repeat`/`RepeatHorizontal` factories (an early piece of §4's vocabulary), centralized
subspace resolution with safe termination. Waves 2+ remain proposed.
(Original design session 2026-08-31.)
**Drives:** the developer-ergonomics overhaul and the de-generification of the core

This document captures a connected set of design decisions arrived at while reviewing
the ergonomics of the current API. None of it is implemented yet. It builds on the
Design Philosophy in `CLAUDE.md` (declarative, not imperative; explicit dimensions are
the exception) and does not revisit it.

---

## 1. The framing: Unrect is a parser combinator library for 2D data

The problems Unrect faces — describing structure declaratively, repetition,
alternatives, error reporting — are the problems text parsing solved decades ago with
parser combinators. We should borrow deliberately rather than reinvent:

| Parser world | Unrect |
|---|---|
| Token stream | `ISpace` of canonical cell values |
| Lexer / tokenizer | Space adapters (Excel, ODS, CSV, 2D array) |
| Combinators (`seq`, `many`, `sepBy`) | Shape vocabulary (`Vertical`, `Repeat`, separator-aware repeat) |
| `choice` / alternation | "Try shape A, else shape B" — needed for per-vendor format variants |
| Label operator (`<?>`) | Named regions for error messages |
| Parse trace / debug mode | Decomposition trace |
| "Expected EOF" | Unconsumed-space warnings |

The lexer analogy is load-bearing: combinators never touch raw source; a tokenizer
fixes the vocabulary first. Likewise, shapes should never touch backend-native values;
adapters normalize into a canonical cell vocabulary at the boundary.

## 2. Canonical value model (de-generifying the core)

### Decision

Replace the open generic `TSpace` element type with a single canonical cell value type
owned by Core. `ISpace` becomes non-generic. Builders, strategies, and mappers all
speak canonical values; the pervasive `<SpreadsheetValueBase>` noise does not get
hidden — it ceases to exist, because the "token type" is fixed by the framework.

`SpreadsheetValueBase` (currently in `Unrect.Excel`) is the embryo of this type: it
already has the kind classification, `HasValue`, and typed accessors. The move is a
*promotion* into Core (with a rename — it is no longer spreadsheet-specific), not an
invention. Backend-native value handling becomes an internal detail of each adapter.

### The governing principle

> **The canonical model captures only distinctions the source formats reliably make;
> anything finer is a consumer-side conversion requested through an accessor.
> Adapters must never guess.**

This principle answers every "should the model have X" question, present and future.

### Canonical kinds

Small and closed: **Blank, Text, Number, Temporal, Boolean** (plus, likely, **Error**
for Excel `#DIV/0!`-style cells — to be confirmed when the adapter is built).

- **One `Number`, granular accessors.** An xlsx cell holding `3` records no int-ness
  or decimal-ness; granular numeric kinds would be fiction invented by the adapter.
  Accessors stay granular and checked: `GetDecimal()`, `GetDouble()`, `GetInt()`.
  Classification lives in the model; *interpretation* lives in the Map — only the map
  function knows a number is money.
  - *Fidelity note:* xlsx stores numbers as decimal strings in XML; the double is
    manufactured by ExcelDataReader. Our report producers use `decimal` exclusively.
    Internal representation of `Number` may hold "decimal if it arrived that way or
    fits, double otherwise" so a raw-string-reading adapter can deliver perfect money
    fidelity through the same type. Implementation detail; deferred.
- **One `Temporal` kind.** Excel dates are numbers wearing a display format — no
  timezone, no offset; even date-vs-datetime is formatting, not value. Accessors:
  `GetDate()` (truncating), `GetDateTime()`. `DateTimeOffset` cannot come out of an
  xlsx honestly; attaching a timezone is the consumer's job (only they know the
  report's provenance). `DateTime2` etc. are storage details of other systems, not
  semantic kinds. Backends that truly record richer temporals use the payload slot
  (§3) — the kind set does not grow on speculation.

### Blankness is part of the `ISpace` contract

"Blank" is decided **where data enters the system**, exactly once — the same move a
lexer makes when deciding what counts as whitespace:

- Every space answers "is this cell blank" (canonical kind Blank).
- Adapters decide what maps to Blank and may accept overrides at construction:
  the Excel adapter defaults sanely (null; possibly empty string / `"N/A"` as
  configurable); an array adapter takes it explicitly, e.g.
  `ArraySpace.Create(nums, isBlank: v => v == 0)` ("in this grid, zero means empty").
- Strategies then need no blankness predicates at all: `SkipBlankRows()` takes zero
  arguments. The `v => !v.HasValue` lambda currently repeated at every call site
  disappears.

## 3. Extensibility without boiling the ocean

The goal is parsing vendor spreadsheets; genericity must never make that harder.
Extensibility is designed as two *seams*, both purely additive, with one enforceable
rule.

### Richer values: carry, don't classify

A canonical value may carry an optional **native payload** alongside its canonical
representation. A backend with true `DateTimeOffset` (or arbitrary-precision decimal,
etc.) stores it in the payload; `GetDateTime()` still works everywhere; a consumer who
knows their backend calls `TryGetNative<T>()` at the map site. No model explosion, no
negotiation — you just try, locally, where interpretation already lives. Adapters with
nothing extra store nothing extra.

### Richer spaces: capability interfaces, probed — and declared by shapes

Formatting inspection, formula-vs-cached-value views, merged-cell info: these are
*space* capabilities, not value properties. Mechanism:

- **Overlay spaces**: the same grid coordinates viewed through another lens (style
  view, formula view) — exposed via optional interfaces on the space
  (`space is IStyleOverlay`-style probing; standard .NET idiom, no registry).
  Decomposition runs on the value view; capability-aware predicates consult overlays
  at the same coordinates. (The positional predicate forms
  `TakeRowsWhile((space, row) => ...)` can already consult external data via closure —
  the door exists; overlays make it principled.)
- **Shapes declare their requirements.** Because shapes are inspectable values, a
  shape using a bold-row predicate carries "requires: formatting inspection" in its
  description. Pre-run validation intersects the shape's declared requirements with
  the space's implemented interfaces and fails immediately with a message like
  *"this shape requires formatting inspection; ArraySpace does not provide it"* —
  instead of a failure three strategies deep. An imperative parser structurally cannot
  do this.

### The rule that keeps v1 small

> **Nothing in Core may require a capability.** Every shape built from core vocabulary
> must decompose any conforming space. Capabilities only unlock extra predicates and
> accessors for consumers who know their backend.

Under this rule, v1 ships exactly what vendor-spreadsheet parsing needs (the kinds,
blankness on the contract — no payloads, no overlays), and both seams open later
without breaking anything. Excel is not the ceiling; it is the first adapter — the
canonical kinds match it because it is the *poorest* common format, and
poorest-common-denominator is exactly what a canonical model should be.

## 4. Shape vocabulary (the ergonomics layer)

The current API speaks geometry (offsets, areas, rows-while); users think in document
vocabulary (gap, table, header block, repeated section). Every ergonomics complaint is
a translation cost between the two. Direction:

- A standard library of document shapes built on the strategy calculus — the strategy
  layer remains the extension point, and most users should never see it.
- Candidate vocabulary: `Cells(w, h)` / `Cell()` for structural blocks; `Table()`;
  `Repeat(shape)` and separator-aware repeat (`SeparatedBy(block, blankRow)` — the
  `sepBy` of this domain; investors-by-deal is literally this); `Choice(a, b)` for
  vendor variants.
- Target reading for the two existing examples:
  ```
  Vertical(Cells(1, 4), Table())            // simple-report
  Repeat(Vertical(Cell(), Table()))         // investors-by-deal
  ```
- **Gaps stay unopinionated.** Whether a blank band is meaningful is document
  semantics best left to the user: jump past it (offset) when unimportant; declare it
  as a region and simply not project it when it matters. The framework already offers
  both spellings; it should not pick one.

### Tables

Eventually want *really good* table support. Three mapping tiers (prior art:
CsvHelper — by index, by name, auto-map):

1. **By index** — always available; headers not required.
2. **By name** — when column headers exist; also the robustness story for the
   50-similar-reports scenario (column *order* changes stop mattering).
3. **Runtime-inferred** — properties inferred from header names at runtime
   (dynamic/dictionary-shaped rows); "you live with the consequences." Right for
   LINQPad / polyglot-notebook exploration, wrong for production; the docs say so
   out loud.

## 5. Observability

Because the framework does the traversal, it can narrate it — a structural advantage
over imperative parsers. Roadmap:

- **Named regions** (the parser label operator): `.Named("column headers")`, so
  failures read *"in 'deal block'[2] → 'column headers': expected ≥ 1 row, found blank
  at row 14"* instead of a bare `OutOfBoundsException`. Cheap; transforms debugging;
  `Choice` is nearly useless without it.
- **Decomposition trace**: debug mode logging every strategy decision (which strategy,
  on what subspace, what it returned).
- **Dry run + diagnostic renderer**: `Build` already *is* decomposition without
  mapping; add a renderer that dumps the region tree with coordinates, ideally
  overlaid on cell data — see your shape land on the sheet before writing a map.
- **Unconsumed-space warnings** ("expected EOF"): rows remaining after the last
  declared region mean the shape drifted from the file; today that is silent.
- **Pre-run validation** = capability check (§3) + dry run + these warnings; little
  else is checkable ahead of data by nature.

## 6. Sequencing

1. **Wave 1 — canonical model:** promote/rename the value type into Core,
   de-generify `ISpace`/builders/strategies, blankness on the contract, adapt
   `SpreadsheetSpace` and `ArraySpace`. (This alone deletes most of the generic noise
   and every blankness lambda.)
2. **Wave 2 — shape vocabulary:** document-level combinators over the strategy
   calculus; named regions; separator-aware repeat; basic table (index mapping).
3. **Wave 3 — observability:** trace, dry-run renderer, unconsumed-space warnings.
4. **Later waves:** map fusion into declarations (entangled with the Region1/2/3
   arity question — solving it likely dissolves arity from the user's view),
   name-based and inferred table mapping, `Choice`, capability seams as demanded.

## Open questions

- ~~Name for the canonical value type~~ — resolved: `CellValue`, with `CellKind`.
- ~~`Number` internal representation~~ — resolved: always stores the double; retains
  the exact `decimal` when constructed from `decimal`/`int`/`long`; `GetDecimal()`
  prefers the exact value. Equality compares on the double representation.
- Does `Error` join the kind set? (Decide when the Excel adapter meets `#DIV/0!`.)
- Does a `Duration` kind join the kind set? ExcelDataReader yields `TimeSpan` for
  `[h]:mm`-formatted cells; the Excel adapter currently throws for them rather than
  guessing (per §2's principle). Decide when a real file needs durations.
- `CellValue` equality is double-based by design (`Of(1m) == Of(1.0)`), while
  `GetDecimal()` may return different exact values for equal cells — documented on
  `Equals`. Revisit if exact-decimal matching is ever needed.
- `CellValue` memory layout: each instance carries all payload slots (~72 bytes/cell)
  and `SpreadsheetSpace` materializes whole sheets eagerly. Fine at example scale;
  revisit before million-row workloads (the `Blank` singleton already covers the
  dominant sparse case).
- Exact shape of the capability-declaration API on shapes.
- Whether `Table()` yields a composite region (headers + body) or a mapped result
  directly — interacts with map fusion.
