# Audit: Invertibility and Algebra of the Shape Vocabulary

**Status:** DESIGN ANALYSIS (2026-09-01, branch `experiment/combined-select`, at `4237ee8`).
No production code was changed and no writer was implemented. Nothing here is a decision;
everything here is evidence and a proposal.

**The charge, in the owner's words:** *"I think that will help us make sure we get the algebra
right."* The audit is not a proposal to build a writer. A writer is a **lens**: it is the
sharpest available test of whether a declaration is really a declaration.

---

## 0. The lens, and the law it implies

CLAUDE.md's philosophy section now carries the test:

> Could a writer execute this declaration — could it produce the file as well as read it?
> Declarations run backward; opaque code does not.

Applied to every element of the public vocabulary, the test sorts into four classes:

| Class | Meaning |
|---|---|
| **(a) Data, inverts as-is** | Geometry composites, literal landmarks, explicit sizes, names. The writer executes the same description forwards. |
| **(b) Discovery strategy** | Does not invert, and does not need to: it *dissolves* on write, because the writer knows the extent the reader had to discover. Not a defect. |
| **(c) Document knowledge trapped in code** | An opaque lambda doing something that is really a declarable fact. **These are the findings.** |
| **(d) Correctly one-way** | Consumer-side reshaping — business pivots, validations, formatting. The boundary test: does it *describe the document* or *consume it*? |

### 0.1 What the writer law would be (the organizing fiction)

Suppose `Write<T>(IShape<T> shape, T value) : ISpace`. The law is **not** an isomorphism:

```
read ∘ write = identity          (a shape reads back exactly what it wrote)
write ∘ read ≠ identity          (a file is not recovered cell-for-cell)
```

The asymmetry is exactly class (b). A discovery strategy is a *many-to-one* map from files to
values, so the writer must pick a **canonical representative** of the fibre:

| Declaration | What read accepts | What write emits (canonical representative) |
|---|---|---|
| `BlankRows()` / `AfterBlankRows()` | a gap of any height, including 0 | one blank row |
| `separatedBy: BlankRows()` | gaps of differing heights between items | one blank row between items |
| discovered extents (`Row`, `Column`, `Range`, `Table`) | any trailing blank margin | the exact extent of the data written |
| `Repeat(item, atLeast: n)` | n or more occurrences | exactly `value.Count` occurrences |
| `Choice(a, b, c)` | whichever alternative matches | the first alternative |
| `Optional` / `Else` | present or absent | present (the tolerated branch is never written) |
| `.Until(L, orEnd: true)` | bounded, or run to the end | bounded (the landmark is emitted by whoever owns it) |
| `MaxArea` / `MaxOffset` | whatever is available | the caller's declared frame |

That table is the whole of class (b), and it is healthy: **read is a relation, write is a
section of it.** A vocabulary in which every element inverted exactly would be a vocabulary that
could not tolerate a real spreadsheet.

Two places where the law is *not merely lossy but unsatisfiable* today, and they are the
findings:

1. **A cell whose kind and meaning live in a lambda.** `Cell(c => c.GetDecimal())` — the writer
   has a `decimal` and no instruction to put it anywhere. `read ∘ write` cannot even be attempted.
2. **A landmark that is referenced but never declared.** `.After(SeekRowContaining("Cash Flows
   using inception date"))` asserts that a row with that text exists; **no shape declares that
   row as content**, so the writer emits a file without it, and reading that file fails. This is
   the only place in the vocabulary where `read ∘ write = identity` is *provably false* rather
   than merely unimplemented.

A third, structural, one:

3. **`Overlay` makes write partial.** A flow partitions its extent, so writing is a disjoint sum
   and always total. An overlay hands every child the same extent; two children may describe the
   same cell, so write is only defined when they agree. (Read is happily total there — children
   *read* rather than paint. The asymmetry is real and is the price of the shape being useful.)

---

## 1. Classification of the public vocabulary

`IShape<T>` factories and modifiers (`Unrect.Shapes.Shape`, `ShapeExtensions`), the strategy and
landmark vocabularies (`Unrect.Strategies`), and the views.

### 1.1 Leaves

| Element | Class | Notes |
|---|---|---|
| `Cell(project)` | **a** placement / **c** projection | 1×1 extent inverts; the lambda does not. **Finding C1.** |
| `Row(width, project)`, `Column(height, project)`, `Range(w, h, project)` | **a** / **c** | Explicit extents invert. Projections do not. |
| `Row(project)`, `Column(project)`, `Range(project)` | **b** / **c** | Discovered extents dissolve on write; projections do not. |
| `Row(IColumnStrategy, …)`, `Column(IRowStrategy, …)`, `Range(IAreaStrategy, …)` | **b** (literal strategies) / **c** (lambda strategies) | `TakeRows(1)` inverts; `TakeColumnsWhile((s, c) => true)` is an opaque spelling of "all columns" — **Finding C5.** |
| `Table(project)` / `Table(headerRows, project)` | **b** extent, **c** header | Extent dissolves. The header row's *captions* are not in the declaration at all — they are string literals inside the projection. **Finding C2.** |
| `TableRows(project)` | **b** / **c** | Same, per row. |
| *(missing)* `TableColumns` | — | No transposed twin. **Finding C3.** |

### 1.2 Layouts, repetition, alternation

| Element | Class | Notes |
|---|---|---|
| `VerticalFlow` / `HorizontalFlow` | **a** | The canonical invertible composite: bands in declaration order. Write = concatenate. |
| `Overlay` | **a**, with a consistency condition | Write is partial (§0.1.3). |
| `Repeat` / `RepeatHorizontal` | **a** | Write = emit `list.Count` items with the separator between. `atLeast` is a read-side assertion; it dissolves on write. |
| `separatedBy:` | **b** | Canonical representative: one blank row (§0.1). |
| `Choice` | **a**, with a canonical branch | Write picks alternative 1. |
| `.Else(shape)` / `.Else(value)` / `.Optional()` | **b** / **d** | Tolerance dissolves on write: the good branch is always written. The *fallback value* is consumer-side. |

### 1.3 Modifiers

| Element | Class | Notes |
|---|---|---|
| `.Named(name)` | **a** | Pure metadata; survives inversion untouched. |
| `.Down(n)` / `.Right(n)` / `SkipRows` / `SkipColumns` | **a** | Explicit gap; write emits blanks. |
| `.AfterBlankRows()` / `.AfterBlankColumns()` / `BlankRows()` / `BlankColumns()` | **b** | Canonical: one blank row/column. |
| `.After(SeekRowContaining("X"))` | **c** | The *text* is data; the *row it names* is declared nowhere. **Finding C4.** |
| `.After(SeekRow(predicate))` / `SeekRowWhere(anyCell)` | **c**, irreducibly | A lambda locator cannot describe itself either to a writer or to a diagnostic. |
| `.After(FromRight(n))` / `FromBottom(n)` | **a** | Right-aligned placement; write emits it at that alignment. |
| `Then(a, b, …)` | **a** if its parts are | Composition inherits the class of its arguments. |
| `.Sized(ExplicitArea(w, h))` | **a** | |
| `.Sized(RowsWhileAnyValue()…)` | **b** | |
| `.Sized(SelectArea(lambda))` | **c** | Opaque size rule. |
| `.Padded(…)` | **a** | Write emits the inset as blanks. |
| `.Until(RowContaining("X"))` | **c** | Same as `.After(seek)`: the bound is data, the landmark row is undeclared. **Finding C4.** |
| `.Until(…, orEnd: true)` | **b** | Canonical: bounded. |
| `.Select(f)` | **d** unless `f` is a bijection | Projection into consumer types. Correctly one-way; a writer would need `IShape<T>` to be an *iso-arrow*, which is a much larger design (see §5.1). |

### 1.4 Application and views

| Element | Class | Notes |
|---|---|---|
| `.Map` / `.Apply` / `.MapWithDiagnostics` | **d** | The consumer-side entry points. `MapWithDiagnostics`'s unconsumed-space `Info` is, notably, an approximation of "the writer and the reader disagree about the file" computed without a writer. |
| `CellStrip` / `CellBlock` indexing | **c** where positional constants encode field identity (`c[0]`, `b[0, r]`); **d** where genuinely bulk | See **Finding C6**. |
| `TableRow[string]`, `TryGet`, `AddressOf` | **c** as used (captions in lambdas) | See **Finding C2**. |
| `CellValue.Get*/Try*` | **c** | The kind is a declarable fact. See **Finding C1**. |
| `ShapeLocation`, `ShapeDiagnostic`, `ShapeException` | **d** | Reader-side reporting. |

---

## 2. Findings (class c), ranked by declaration code they would clean up

Evidence is the seven `linqpad/*.linq` scripts (the honest sample; `import-k1.cs` is local-only
reference material and is not quoted). Counted across them: **61** `Get*`/`Try*` accessor calls,
**27** by-name table bindings, **10** literal anchor strings, of which **3** are duplicated
between an anchor and a bound.

### C1. A cell's kind and direction are declarable; today they are a lambda body

**Evidence:** every script. `Cell(c => c.GetDecimal())`, `Cell(v => v.GetString())`,
`r["Amount"].GetDecimal()`, `c[2].GetDateTime()`. 61 sites.

**The declarable fact:** *this cell holds a decimal* (or text, or a date). A kind is not a
computation; it is part of the format's definition, exactly like an extent. The lambda is
carrying one bit of vocabulary and 100% of the opacity.

**Sketched vocabulary** — typed leaves that *are* the binding, with `Cell(lambda)` retained as
the escape hatch:

```csharp
public static IShape<string>   Text     { get; }   // Cell.Text
public static IShape<decimal>  Decimal  { get; }
public static IShape<int>      Int      { get; }
public static IShape<double>   Double   { get; }
public static IShape<DateTime> Date     { get; }
public static IShape<bool>     Flag     { get; }
public static IShape<T?>       Optional…            // via the existing .Optional()
```

so `v.Next(Decimal)` replaces `v.Next(Cell(c => c.GetDecimal()))`.

**What inverts afterwards:** the writer formats the value into the cell by kind. Also — and this
is the immediate read-side payoff — **the kind becomes visible before projection runs**, so a
dry-run renderer can say "expected Number at B14" without a file, and a `Choice` can discriminate
alternatives on declared kinds rather than by trying and failing.

**Which script would use it:** all seven. `investor-irr.linq`, `simple-report.linq`, and
`investor-summary.linq` become almost entirely binding declarations.

### C2. A table's column captions are data; today they are string literals inside a lambda

**Evidence:** `simple-report.linq` (4), `investors-by-deal.linq` (6), `investor-summary.linq` (7),
`investor-irr.linq` (10). 27 sites of the shape:

```csharp
var transactions = TableRows(r => new {
  Client = r["Client"].GetString(),
  Date   = r["Transaction Date"].GetDateTime(),
  ...
});
```

**The declarable fact:** the table has a header row containing exactly these captions, in some
order, each bound to a member of a known type. The triple `(caption, kind, member)` is the entire
content of every one of those lines — and it is invertible **by construction**: the writer emits
the header row from the captions and each body cell by kind.

**Sketched vocabulary** (this is the deferred "table projection by header caption into
properties" item of `flow-vocabulary-spec.md` §10, and the audit's evidence for prioritising it):

```csharp
var transactions = TableRows<Transaction>(t => t
  .Column("Client",           r => r.Client)
  .Column("Transaction Date", r => r.Date)
  .Column("Amount",           r => r.Amount));
```

or, keeping the applicative flavour already in the codebase, a per-column cursor mirroring
`LayoutCursor`:

```csharp
var transactions = TableRows(r => new Transaction(
  Client: r.Next(Text.From("Client")),
  Date:   r.Next(Date.From("Transaction Date")),
  Amount: r.Next(Decimal.From("Amount"))));
```

The second spelling composes with C1 and needs no expression trees; the first needs an expression
tree (or a setter delegate pair) to invert, which is the trade to decide.

**What inverts afterwards:** the whole table — header and body — from a `List<T>`. This is the
single largest block of currently-opaque declaration text in the corpus.

### C3. `Table` has no transposed twin, and the K-1 header band is one

**Evidence:** `scrubbed-k1.linq`. Three parallel full-width rows (`captionRow`, `fundNameRow`,
`ownershipRow`) are read as raw `CellValue[]`, then correlated by index:

```csharp
int Find(CellValue[] row, string caption) => Array.FindIndex(row, …);
var label = Find(fundNames, "Fund Short Name");
var columns = … fundNames.Select((v, i) => …).Where(x => x.Index > label && x.Value.HasValue)
                .Select(x => (Code: …, Percent: ownership[x.Index].GetDouble(), Column: x.Index));
```

**The declarable fact:** *this band is a table whose records are columns.* Each fund is a column;
the leading labels ("Fund Short Name", the ownership caption) are a **header column**, not a
header row. `TableView` already does exactly this job on the other axis, including by-name
lookup, ambiguity errors, and per-cell addressing — the vocabulary simply has no way to say it
sideways. `Row`/`Column`, `VerticalFlow`/`HorizontalFlow`, `Repeat`/`RepeatHorizontal`,
`Until`/`UntilColumn`, and both landmark trios are all mirrored; `Table` is the one member of the
leaf family that is not.

**Sketched vocabulary:**

```csharp
public static IShape<T> TableColumns<T>(Func<TableView, T> project);        // header COLUMN
public static IShape<IReadOnlyList<T>> TableColumnRecords<T>(Func<TableColumn, T> project);
```

reusing `TableView` with an `Orientation`, so `view["Fund Short Name"]` resolves along the header
column and `TableColumn` is `TableRow`'s mirror.

**Then the general case, which the campaign will need next:** the K-1 body sections are a
**cross-tab** — line items down (keyed by the ATAX code column) and funds across (keyed by the
header band far above). A row-keyed table and a column-keyed table sharing one body:

```csharp
var matrix = Matrix(
  rowKeys:    ColumnContaining("ATAX"),      // or a declared key column
  columnKeys: fundHeader,                     // a shape yielding the column axis
  cell:       Decimal);
// view[rowKey, columnKey]
```

The hard part is real and should be named: **the column axis is discovered in one region and
consumed in another**, which today is only possible because the projection carries `head.Columns`
down by hand. That is value-dependence — the one deliberate exception in the philosophy section —
doing structural work. A `Matrix`/`Keyed` composite is how that becomes a declaration again.

**What inverts afterwards:** a cross-tab is the *most* invertible shape in the vocabulary once
declared — the writer emits both key axes and fills the intersections. It would also let the K-1
sections stop being `CellValue[][]` and become keyed values, deleting most of the pivot code in
`scrubbed-k1.linq`'s flow lambda.

### C4. Content anchors are referenced but never declared

**Evidence:** `investor-irr.linq` — the string `"Cash Flows using inception date"` appears twice,
once as a bound and once as an anchor:

```csharp
var byTransferDate = irrDetails.After(…).Until(RowContaining("Cash Flows using inception date"));
var byInception    = irrDetails.After(Then(SeekRowContaining("Cash Flows using inception date"), SkipRows(1)));
```

and `scrubbed-k1.linq` — a helper exists *solely to re-read the row it just sought*:

```csharp
IShape<CellValue[]> FullRow(string anchor) => Range(…).After(SeekRowContaining(anchor));
var captionRow = FullRow("ATAX");
var fundNameRow = FullRow("Fund Short Name");
var ownershipRow = FullRow("Fund Short Name").Down(4);   // "Fund Short Name" twice
```

**The declarable fact:** *there is a row here whose content is "X"*. Today the declaration only
ever says "somewhere ahead of me is such a row, and I start/stop relative to it". Nothing owns
that row. Consequences, all present in the corpus:

- the same literal is written twice, in two different vocabularies (`SeekRowContaining` /
  `RowContaining`), with nothing tying them together;
- the caption row is consumed by an arithmetic `SkipRows(1)` — a hard-coded count that CLAUDE.md's
  own rule calls "a fragility bug waiting for the next export";
- `read ∘ write` is unsatisfiable: the writer emits no caption, and the result does not read back.

**Sketched vocabulary — a literal leaf, plus anchoring on a matcher:**

```csharp
public static IShape<string> Label(string text);        // matches (trimmed, ignoring case) and emits
public static IShape<Unit>   Caption(string text);      // same, discarded result
```

turning the anchor-and-skip idiom into flow membership:

```csharp
var series = VerticalFlow(v => {
  v.Next(Caption("Cash Flows using inception date"));
  return v.Next(irrDetails);
});
```

The caption is now *declared content* rather than a search key; the literal appears once; the
`SkipRows(1)` disappears (the caption's own extent is what advances the cursor); and a failure
blames `'Caption'` with the text it expected. The bound on the preceding series can then be
expressed against the same matcher value rather than a second literal.

**Which script would use it:** `investor-irr.linq` (removes one duplicate literal and one
`SkipRows`), `scrubbed-k1.linq` (removes the `FullRow` helper's reason to exist and one duplicate
literal), and every future caption-to-caption K-1 section.

### C5. "All columns" and "the whole extent" have no spelling, so users write `(s, c) => true`

**Evidence:** `scrubbed-k1.linq`:

```csharp
Range(RowStrategies.TakeRows(1).TakeColumnsWhile((s, c) => true), b => b.Row(0).ToArray())
```

**The declarable fact:** *this row spans the full available width* — needed because `Row(project)`
discovers its width with `TakeColumnsWhileAnyValue()`, which stops at the first blank column, and
a caption band has gaps. The lambda is an opaque spelling of a constant.

**Sketched vocabulary:** `ColumnStrategies.AllColumns()` / `RowStrategies.AllRows()`, and a
`Row(project)` overload (or a `.FullWidth()` modifier) that uses them; equivalently re-export
`MaxArea()` on `Shape` (see §5.8 — the `.Sized` argument vocabulary is not re-exported at all,
while the `.After` argument vocabulary is).

**What inverts afterwards:** a full-width row is a frame declaration, not a discovery — the writer
emits the caller's width.

### C6. Positional field access is a labelled record in disguise

**Evidence:** `simple-report.linq`, `investor-irr.linq`, `investor-summary.linq`:

```csharp
var reportHeader = Column(4, c => new {
  Title = c[0].GetString(), SubTitle = c[1].GetString(),
  ReportDate = c[2].GetDateTime(), ReportId = c[3].GetString() });
```

and `scrubbed-k1.linq`'s entity card, which is the labelled-pair version of the same thing:

```csharp
var entity = Range(2, 5, b => Enumerable.Range(0, 5)
    .ToDictionary(r => b[0, r].GetString().TrimEnd(':'), r => b[1, r].ToString()))
  .After(Then(SeekColumnContaining("EIN:"), SeekRowContaining("EIN:")));
```

**The declarable facts:** (i) a run of cells bound to named fields with kinds — which is
`VerticalFlow` over C1's typed leaves, already expressible, just verbose; and (ii) *a two-column
block of `label: value` pairs whose labels end in ':'*, which is not expressible at all. Note the
hard-coded `2, 5` — a width and a height that merely happen to match today's file, i.e. precisely
the fragility CLAUDE.md warns about; the honest declaration is "rows while column A carries a
label".

**Sketched vocabulary:**

```csharp
public static IShape<FieldView> Fields(string labelSuffix = ":");   // label column + value column,
                                                                    // extent discovered from the labels
// then declared bindings on top:  f.Get("EIN", Text), f.Get("Tax Year", Int)
```

**What inverts afterwards:** the writer emits the label column (labels + suffix) and the value
column by kind — the entity card round-trips. As a bonus the `2, 5` becomes discovered, and
`TrimEnd(':')` becomes a declared label format rather than string surgery in a projection.

### C7. Coercion facts inside projections

**Evidence:** `scrubbed-k1.linq`:

```csharp
string Code(CellValue v) => v.TryGetString() ?? v.TryGetInt()?.ToString() ?? "";
```

**The declarable fact:** *this column's cells are text or number, and both mean a code.* A small
union-kind binding (`Text.Or(Int)`, or `Code` as a declared kind) carries it. Lowest value of the
seven — one site — but it is the same shape of fact as C1 and would fall out of the same
mechanism. Left here so the list is complete rather than tidy.

---

## 3. Class (d): what is correctly one-way, and why the boundary is physical

The corpus draws the line cleanly, and the flow-lambda style is what makes it visible: **above the
`return`, the document; the `return` and after it, the consumer.**

| Site | Why it is (d) |
|---|---|
| `AllAllocationsSumToFederal` (`scrubbed-k1.linq`) | An assertion *about* the data's meaning. A writer would have to invent numbers satisfying it. |
| `SeriesAgreeWithSummary`, `Summary.Count == Details.Count` (`investor-irr`, `investor-summary`) | Cross-region correlation; post-parse validation, as both scripts' comments already say. |
| The fund-centric pivot in `scrubbed-k1.linq`'s flow lambda | A business reshaping: sparse per-fund line items. Describes the *import target*, not the sheet. Note that C3 would shrink it, not remove it: the pivot stays (d), but its *input* stops being raw arrays. |
| `.Where(i => i.Amount is decimal a && a != 0m)` | A consumer filter; the file contains the zeros. |
| `edge-cases.linq` in its entirety | A probe of adapter semantics, not a report declaration. |
| `.Select(f)` in general | Projection into consumer types; invertible only if `f` is a bijection, which the API cannot and should not require. |

**The boundary test, restated as a rule of thumb for future review:** if removing the code would
change *which cells are read*, it is decomposition and belongs in class (a)/(b)/(c). If removing it
would change only *what the caller gets back*, it is (d).

One caution, because the flow lambda makes it easy to blur: the K-1 header's
`Overlay(o => { … resolve columns … })` is doing (c) work (C3) inside a (d)-looking lambda. The
"header digests itself" idiom is excellent for keeping raw rows from travelling — but it is also
where structural facts hide. The reviewer's question for any layout lambda: *does this compute a
position?* If yes, it is decomposition wearing a projection's coat.

---

## 4. Algebra: pairs, duals, and laws

Findings here are independent of invertibility except where noted.

### 4.1 Duals: `After` ↔ `Until`, and the three vocabularies for "a row that matches"

`.After` says where a shape starts by content; `.Until` says where it ends. The duality is real and
the spec states it. But the two sides take **different argument types**, and the reason is
mechanical rather than semantic:

| | Argument | Not-found behaviour | Lives in |
|---|---|---|---|
| `.After(…)` | `IOffsetStrategy` (a *movement*) | throws (`AnchorNotFoundException`) — load-bearing for `Repeat`'s stop | `OffsetStrategies` |
| `.Until(…)` | `IRowLandmark` (a *locator*) | returns `null`, so `orEnd` can be implemented without exceptions | `RowLandmarks` |
| `separatedBy:` | `IOffsetStrategy` (a *movement*) | not-found ends the repetition | `OffsetStrategies` |

So the vocabulary contains **three ways to say "the first row containing 'Total'"**:

```csharp
OffsetStrategies.SeekRowContaining("Total")   // lifted to an offset, throwing
RowLandmarks.RowContaining("Total")           // a nullable locator
RowStrategies.TakeRowsTo(predicate)           // lifted to a size
```

`flow-vocabulary-spec.md` §5.3 already noticed half of this and shared the *predicate helpers*
(`CellMatching`) so the two cannot drift on what "containing" means. The audit's recommendation
goes one step further: **one matcher concept, three lifts.**

```csharp
public interface IRowMatch { string Description { get; } int? Find(ISpace space); }   // = today's IRowLandmark

// lifts, all in one place:
IOffsetStrategy To(IRowMatch match);          // strict: today's SeekRow*  (throws)
IOffsetStrategy Past(IRowMatch match);        // To + the matched row's height — replaces Then(Seek…, SkipRows(1))
IRowStrategy    RowsBefore(IRowMatch match);  // today's TakeRowsTo
// .Until(match, orEnd:) stays as it is
```

Why this is worth doing beyond tidiness:

- **It removes the `SkipRows(1)` idiom** (`Past`), which is a hard-coded count standing in for
  "the caption row's own height" — the same fragility C4 attacks from the other side.
- **It makes `.After` and `.Until` take the same argument**, so the duality is visible in the
  signature and not just in the prose.
- **It makes anchors describable.** A matcher built from a literal can say what it is; a
  `Func<ISpace,int,bool>` cannot. Today `SeekRow(predicate)` fails with "no matching row", which is
  the least useful sentence in the library. A matcher type gives the obvious place to hang a
  `.Describing("the fund header")`.
- **It is the type C4's `Label`/`Caption` leaf wants**, so the two findings land together: the same
  matcher can *declare* the row and *anchor* on it.

**Naming defect found while tabulating this — the two trios' names cross:**

| Predicate shape | Seek (offset) | Landmark |
|---|---|---|
| `Func<ISpace, int, bool>` | `SeekRow` | `RowWhere` |
| `Func<CellValue, bool>` | `SeekRowWhere` | `RowWithCell` |
| `string` literal | `SeekRowContaining` | `RowContaining` |

`Where` means *space-predicate* on one side and *cell-predicate* on the other. Anyone who learns
one trio will mis-write the other, and the compiler will often let them (both lambdas are
inferable in some positions). Rename in whichever direction the matcher unification settles;
`RowWhere`/`RowWithCell` is the better pair of names, so `SeekRow`→`SeekRowWhere`,
`SeekRowWhere`→`SeekRowWithCell` is the smaller lie to fix.

**Should separators and landmarks unify?** No — and the reason is worth recording. A separator is
an *advance* ("get me over the gap to the next item"); a landmark is a *stop* ("everything before
this"). They answer different questions and `Repeat` depends on the difference: a separator that
cannot advance ends the repetition quietly, while a bound that cannot be found is loud drift. Keep
the roles distinct; unify only the **matcher** underneath, so that
`Repeat(item, separatedBy: Past(caption))` and `Repeat(item).Until(caption)` can share the word
`caption`.

**One asymmetry left standing, deliberately:** `.Until` has `orEnd:`; `.After` has no `orStart:`.
Correct as it stands — a missing *start* is how a repeat learns it is finished — but it should be
documented as a decision rather than left to be discovered, because the natural reading of a
dual pair is that both ends have the same options.

### 4.2 Rows ↔ columns: the mirror table

| Row-axis item | Column-axis twin | Status |
|---|---|---|
| `Row(project)` / `Row(w, …)` / `Row(IColumnStrategy, …)` | `Column(project)` / `Column(h, …)` / `Column(IRowStrategy, …)` | ✅ complete |
| `VerticalFlow` | `HorizontalFlow` | ✅ |
| `Repeat` | `RepeatHorizontal` | ✅ (naming, §4.6) |
| `.Until` | `.UntilColumn` | ✅ (naming, §4.6) |
| `.Down(n)` | `.Right(n)` | ✅ |
| `.AfterBlankRows()` | `.AfterBlankColumns()` | ✅ |
| `BlankRows()` | `BlankColumns()` | ✅ |
| `SkipRows(n)` | `SkipColumns(n)` | ✅ |
| `FromBottom(h)` | `FromRight(w)` | ✅ (naming, §4.6) |
| `SeekRow` / `SeekRowWhere` / `SeekRowContaining` | `SeekColumn` / `SeekColumnWhere` / `SeekColumnContaining` | ✅ |
| `RowWhere` / `RowWithCell` / `RowContaining` | `ColumnWhere` / `ColumnWithCell` / `ColumnContaining` | ✅ |
| `SkipRowsWhileAll/Any` | `SkipColumnsWhileAll/Any` | ✅ |
| `RowStrategies.TakeRowsWhile(pred)` / `TakeRows(n)` / `TakeRowsWhileAll/Any/AnyValue` | `ColumnStrategies.TakeColumns*` | ✅ |
| `RowStrategies.TakeRowsWhile(int column, Func<CellValue,int,bool>)` | *(none)* | ❌ **hole** |
| `RowStrategies.TakeRowsTo(pred)` | *(none)* | ❌ **hole** |
| `RowStrategies.TakeRowsToValue(column, value)` | *(none)* | ❌ **hole** |
| `SizeStrategies.RowsWhileAny(pred)` / `RowsWhileAnyValue()` | *(none)* | ❌ **hole** — and this is the one the shape layer's own defaults use |
| `Table` / `TableRows` | *(none)* | ❌ **hole** — Finding C3, the largest |
| `CellStrip` (orientation-parametric) | — | ✅ one type, both axes |
| `CellBlock.Row(i)` | `CellBlock.Column(i)` | ✅ |
| `TableRow` (by-name access) | *(none)* | ❌ follows from C3 |

Five holes, four of them cheap. Note also `CellValue.GetDate()` has no `TryGetDate()` twin
(`TryGetDateTime` exists) — trivial, but it is the kind of gap that makes a user reach for
exceptions.

**The deeper question the table raises: should the mirror be generated rather than written?** An
involution `Transposed()` — a decorator over `ISpace` swapping the two indices — would collapse
half of this table by law:

```
Transposed(Transposed(x)) = x
HorizontalFlow(f)         = Transposed(VerticalFlow(Transposed ∘ f))
Column                    = Transposed(Row)
TableColumns              = Transposed(Table)
```

`ISpace` is three members, so the decorator is ~15 lines. **Do not do it without solving
locations:** every `ShapeLocation` computed inside a transposed space would report row and column
swapped, and A1 addressing is one of the library's best features. Recorded here as a structural
observation and a warning, not a recommendation — the honest cost of the mirror is that it must
be hand-written, and the audit's advice is to *finish* it (the five holes) rather than to
mechanise it.

### 4.3 Flow ↔ Overlay: the layout lattice

The three layout composites are exactly the three interesting settings of one variable — what the
cursor does between children:

| Composite | Advance along X | Advance along Y | Consumed |
|---|---|---|---|
| `VerticalFlow` | no | yes | Σ heights × max width |
| `HorizontalFlow` | yes | no | max height × Σ widths |
| `Overlay` | no | no | bounding box of children's advances |
| *(absent)* | yes | yes | — a diagonal flow: no motivating document |
| *(absent)* | structured on both axes | | **a Grid with declared tracks** — deferred in `panel-and-anchoring-spec.md`, still unmotivated by a file, but see below |

`FlowState` and `OverlayState` differ only in that arithmetic, which is why the shared
`LayoutState` base is the right factoring and why the family feels complete. Two observations:

- **Nothing is expressible in a flow that is awkward in an overlay, or the reverse** — the two are
  genuinely dual (partition vs. share) and each has the natural spelling for its job. The corpus
  bears this out: `scrubbed-k1.linq`'s header is an overlay because its parts share rows; every
  other composite in every script is a flow.
- **What *is* awkward in both is the cross-tab** (C3): two key axes with a shared body. It is not a
  Grid-with-tracks (that is about *declared sizes*); it is about *declared keys*. Worth saying
  plainly so the deferred Grid item is not mistaken for the answer to the K-1 campaign — they are
  different features and the campaign needs the keyed one.

### 4.4 Modifier composition: the laws

Two mechanisms are in play, and the distinction explains every result in the table:

- **Placement modifiers** clone the shape and change its `Placement`. They attach to *the shape
  object they are called on*.
- **Wrapper modifiers** construct a new shape *around* the receiver, with `Placement.Default`.

| Modifier | Mechanism | Law |
|---|---|---|
| `.Named(n)` | clone (`ShapeBase.WithName`) | **replaces**; last wins |
| `.After(o)` | clone (`WithOffset`) | **replaces**, including a default (how `Table` is told not to skip blanks) |
| `.Down` / `.Right` / `.AfterBlankRows` / `.AfterBlankColumns` | clone (`Move`) | **compose**: `Then(existing, new)` when an offset was declared, else take it |
| `.Sized(a)` | clone (`WithArea`) | **replaces**; extents do not stack |
| `.Padded(…)` | wrapper | **nests**: `.Padded(1).Padded(1)` insets 2 |
| `.Select(f)` | wrapper | **composes** as functions |
| `.Else` / `.Optional` | wrapper | **nests**: an inner boundary catches first |
| `.Until(L)` | wrapper, **except** on an `UntilShape` where it clones | **replaces when adjacent, nests when separated** — the one non-uniform rule |

The `.Until` rule deserves its own line because it is the vocabulary's only
context-sensitive modifier: `x.Until(A).Until(B)` ends at B (replace, "a shape has one end"), while
`x.Until(A).Select(f).Until(B)` puts both in force (nest). Both behaviours are defensible and both
are documented — but the *same two calls* mean different things depending on what sits between
them, which is exactly the kind of rule a reader will get wrong once. Options: (i) leave it and
keep the doc; (ii) make `Until` always nest and let the "one end" case fall out (B is searched
inside A's bound, which for a *later* landmark means B is not found → `orEnd` or a loud failure —
arguably a *better* error than silently replacing); (iii) make `Until` always replace by walking
transparent wrappers. The audit prefers (ii) on the grounds that "later replaces earlier" is a
special case of `Sized`'s rule that `Until` does not actually need, but this is a judgement call
for the owner, and (i) costs nothing.

**Order sensitivity** — pairs whose two orders differ, all of them meaningfully:

| Pair | `f(g(x))` | `g(f(x))` | Notes |
|---|---|---|---|
| `.After(seek)` / `.Until(L)` | `x.After(s).Until(L)`: landmark measured from the flow cursor, seek anchors inside the bound | `x.Until(L).After(s)`: shift first, then bound from the shifted origin | Documented; first is recommended |
| `.Sized(a)` / `.Until(L)` | `x.Sized(a).Until(L)`: bound wins, and `a` taller than the bound is a loud contradiction | `x.Until(L).Sized(a)`: `a` wins; the landmark is searched inside it | "The modifier written last is what the parent sees" |
| `.After(seek)` / `.Else(y)` | `x.After(s).Else(y)`: the missing anchor **is** absorbed | `x.Else(y).After(s)`: it is **not** — the boundary's own placement resolves first | The most consequential order in the library; `Repeat`'s stop condition depends on it |
| `.Sized(a)` / `.Padded(p)` | `x.Sized(a).Padded(p)`: pad wraps a shape that declares `a`; the pad derives its own extent from the parent | `x.Padded(p).Sized(a)`: the pad has extent `a` and the inner shape gets `a` inset by `p` | Follows mechanically from the two mechanisms; worth an example in the docs |
| `.Optional()` / `.Select(f)` | changes the result type (`T?` vs `TResult?`) and *what* is defaulted | | Type system enforces it; no surprise |

**Undefined or surprising interactions found:** none that are outright undefined — every pair above
resolves deterministically. The two to watch are `.Until`'s context sensitivity (above) and
`.Sized` on a wrapper, where "replaces whatever it is applied to" means the *wrapper's* area, not
the inner shape's — correct, but the word "replaces" in the XML docs can be read as reaching
inward. Suggest the docs say "declares the extent **of this shape object**", once, on `Sized`,
`After`, and `Named` together.

### 4.5 Transparency: uniform, with one thing to note

`IsTransparent => Name is null` on **all four** wrappers — `MapShape` (`Select`), `PadShape`
(`Padded`), `UntilShape` (`Until`), `BoundaryShape` (`Else`/`Optional`). Every non-wrapper is
opaque. **No exceptions found**; the rule is genuinely uniform, and the engine honours it in one
place (`ShapeEngine.TryPlace`: `Advance` vs `Descend`), so paths and coordinates cannot drift apart.

Two notes rather than defects:

- `Optional()` builds **two** shapes (a `Select` to `T?`, then the boundary), both transparent when
  unnamed. Naming an `Optional` names the boundary; the inner `Select` stays transparent. Correct,
  and invisible — recorded only so a future reader of a path is not surprised by the arity.
- Transparency is what makes use-site name inference reach through wrappers
  (`ShapeContext.Through`), so the two features are coupled: **any future wrapper must decide
  transparency, or names will stop at it.** Worth stating as a rule for new shapes.

### 4.6 Naming conventions for axis pairs — three conventions in one vocabulary

| Convention | Members |
|---|---|
| Both sides qualified | `VerticalFlow`/`HorizontalFlow`, `Row`/`Column`, `BlankRows`/`BlankColumns`, `SkipRows`/`SkipColumns`, `SeekRow*`/`SeekColumn*`, `RowWhere`/`ColumnWhere` |
| Row side unqualified, column side qualified | `Repeat`/`RepeatHorizontal`, `.Until`/`.UntilColumn` |
| Named by edge, not axis | `FromBottom`/`FromRight` |

The second convention is a deliberate decision (the row form is the common one and should not have
to be disambiguated — `flow-vocabulary-spec.md` §5.4), and the third is fine on its own terms. The
observation for the record: a reader cannot predict which convention a new axis pair will follow.
If a `TableColumns` lands (C3), it joins convention 1 and the split widens. Cheapest fix is not
renaming anything but *writing the convention down*: "the axis pair is qualified on both sides
unless one side is overwhelmingly the common case, in which case the common side is bare."

### 4.7 Identity, units, and the overloading of "consumed nothing"

**Is there a no-op shape?** No. `Range(0, 0, _ => x)` is the nearest thing, and it is a trap
rather than a unit, because zero consumption is a *signal*:

| Zero consumption means | Where |
|---|---|
| the repetition is over | `RepeatShape.TryCollect` — a zero-consuming item stops the repeat |
| a boundary absorbed a failure | `BoundaryShape` — "an absorbed shape consumes nothing" |
| this region is legitimately empty | an empty `Repeat`, a zero-row `Until` bound |
| explain the next sibling's failure | `FlowState.FollowsAnEmptySibling` — the sibling note |

**Four meanings, one measurement.** This is the sharpest algebraic irregularity in the library, and
it is what makes `Repeat(x.Optional())` a documented trap. It is also, notably, a *writer* problem:
given a zero-consumption region the writer cannot tell "absent" from "empty", so `read ∘ write`
would silently drop empty sections.

The fix is invasive and should not be undertaken casually: `ShapeResult<T>` carries `Consumed`, and
would need to carry *why* — a `Presence { Read, Empty, Absorbed }` or simply a `Matched` flag —
with `RepeatShape`, `FlowState`, and the boundary shapes reading it instead of comparing sizes to
zero. The payoff is that `Repeat(x.Optional())` becomes expressible, a genuine empty section stops
ending a repetition, and a unit shape (`Nothing`) becomes definable, making `VerticalFlow` a proper
monoid rather than a semigroup. **Recommend: do not build it now; do record it as the prerequisite
for any writer work, and stop adding features that overload zero further.**

**Consistency of "declares nothing" errors:** uniform and good. A flow with zero `Next` calls and
an overlay with zero `Next` calls both throw with their own noun (`LayoutState.DeclaredNothing`),
both non-absorbable, both projection faults. `Choice` requires ≥2 alternatives at the factory.
`Repeat(atLeast: 0)` yielding an empty list is *not* an error — and that is the right call, because
it is a statement about **data**, not about the **declaration**; the two categories should not be
conflated. The safety net is `MapWithDiagnostics`'s unconsumed-space `Info`, which does fire for a
repeat that found nothing. Consistent.

### 4.8 The naming ladder: coverage

| Argument position | Capturable? | Status |
|---|---|---|
| `LayoutCursor.Next(shape)` | yes | ✅ implemented |
| `Repeat(item)` / `RepeatHorizontal(item)` | yes | ✅ implemented |
| `Choice(params …)` | **no** — `params` collapses the arguments | documented constraint; `.Named` is the mechanism |
| `Else(fallback)` | yes | ❌ **not done** — noted as out of scope in the spec addendum |
| `shape.Map(space)` / `Apply` / `MapWithDiagnostics` | technically yes | ⛔ **attempted and reverted — see below** |

The last one looked like a real hole: the **root** shape is the one node in every path that renders
by description (`VerticalFlow`) rather than by a name the user wrote, even though `report.Map(space)`
has the identifier sitting right there. `CallerArgumentExpression` on an extension method's receiver
does work — verified on this machine (SDK 8.0.4xx, net8.0 consumer): `report.Map(1)` supplies
`"report"`, and `new object().Map(1)` supplies `"new object()"`, falling to rung 3 as it should.

**It was implemented, and then reverted. The root does not participate in the ladder, by decision.**
Two reasons, the first decisive:

1. **It breaks the method-group idiom.** The capture needs an optional parameter, and a method group
   cannot be converted to a delegate that omits one: `spaces.Select(report.Map)` and
   `Func<ISpace, T> f = report.Map` stop compiling (`CS0123`). That idiom — one shape applied over
   many workbooks — is the founding use case of the library, recorded in `CLAUDE.md`'s own
   description of what a shape is. No overload arrangement rescues both: adding a capture-free
   overload beside the capturing one makes every *direct* call bind to the capture-free one, which
   is exactly the case the capture existed for.
2. **`Map` is the one capture point users wrap.** `Next` and `Repeat` are written inside a
   declaration, where the argument text is the user's own. `Map` is routinely called from a helper
   (`T Parse<T>(IShape<T> shape, ISpace space)`), and `CallerArgumentExpression` captures at the
   immediate call site — so the root would render as the *helper's parameter name*. Our own test
   suite demonstrated this immediately: two assertion helpers turned every path in two files into
   `'shape' -> …`.

`.Named` remains the way to name a root, and it is the right one: a root worth naming is worth
naming explicitly. **Recorded as answered so it is not re-proposed.**

Also worth a look while there: a `Choice` alternative could be named by a cursor form
(`Choice(c => { c.Or(vendorA); c.Or(vendorB); })`) matching `LayoutCursor`, which would restore
capture without fixed arities. Not recommended on its own — but if `Choice` ever grows for another
reason, that is the shape to grow into.

### 4.9 Two smaller irregularities

**The `.Sized` argument vocabulary is not re-exported.** `Shape`'s own doc comment says
`using static Unrect.Shapes.Shape;` is "the only import a shape declaration needs", and for
`.After` that is true — `BlankRows`, `SkipRows`, `Seek*`, `From*`, and all six landmarks are
re-exported. For `.Sized` nothing is: `scrubbed-k1.linq` needs
`using static Unrect.Strategies.SizeStrategies;` **and** a qualified `RowStrategies.…`. The dual of
"where does it start" is fully served; the dual of "how big is it" is not. Re-export at least
`RowsWhileAnyValue()`, `ExplicitArea`, `MaxArea`, and the row/column take-strategies (with the
`AllColumns` addition from C5).

**`Choice` and `.Else` are structurally identical and semantically different.** `Choice(a, b)`
records `Info` per non-matching alternative; `a.Else(b)` records `Warning`. The rationale is
recorded (`diagnostics-and-choice-spec.md` §4: alternation is expected, tolerance is exercised) and
is right — but the *structure* does not carry it, only the spelling does, and `Else` is the only
one of the two that can name its arguments (§4.8). Nothing to change today; flagged so that if a
future pass unifies them, the severity distinction is understood to be the load-bearing part.

---

## 5. What a writer would additionally need (recorded, not proposed)

For completeness, because it bounds how far the lens can be pushed:

1. **`IShape<T>` is a reader arrow.** `Select(Func<T, TResult>)` is one-directional by type. A real
   writer needs either a separate `IWriter<T>` built from the same declaration data, or an
   iso-arrow (`Func<T,U>` + `Func<U,T>`) at every `Select` — which would infect the entire
   vocabulary for a feature nobody has asked for. **The audit's position: never make `IShape<T>`
   invertible. Use invertibility as a design test, and if a writer is ever wanted, build it from
   the declared parts (C1–C4) and refuse to write a shape whose projection is opaque.** That
   refusal is itself a useful tool: a "can this declaration be written?" checker is a lint for
   opacity, buildable with no writer at all.
2. **Presence, not size.** §4.7 — the writer cannot distinguish absent from empty until zero
   consumption stops carrying four meanings.
3. **Ownership of landmark rows.** C4 — until a caption is declared content, no writer can produce
   a readable file.

---

## 6. Recommended order of attack

Ordered by (value to real declarations) ÷ (design risk), with the dependencies respected.

| # | Work | Class | Why here |
|---|---|---|---|
| **1** | **Fill the mirror holes** (§4.2): `TakeColumnsTo`, `TakeColumnsToValue`, `TakeColumnsWhile(row, …)`, `SizeStrategies.ColumnsWhileAny(Value)`, `ColumnStrategies.AllColumns()`/`RowStrategies.AllRows()` (C5), `CellValue.TryGetDate()` | mechanical | Zero design risk; removes one opaque lambda from the corpus immediately; the audit is the record of *why*. |
| **2** | **Re-export the `.Sized` vocabulary on `Shape`** (§4.9) | mechanical | Makes the single-import claim true; one line per factory. |
| **3** | **Complete the naming ladder** (§4.8): capture on `Map`/`Apply`/`MapWithDiagnostics` (verified feasible) and on `Else(fallback)` | mechanical | Every path gains a user-written root segment. |
| **4** ✅ | **Unify the matcher** (§4.1) — **done** (`matcher-and-caption-spec.md` phase B). `IRowLandmark`/`IColumnLandmark` kept their names and became the one family; `To`/`Past` landed; the twelve seek factories are deleted, which removed the `Where` crossing rather than renaming it. The size lift (`RowsBefore`) is deferred with its reasons recorded (that spec §1.6). | small design | Prerequisite for 5; kills the `SkipRows(1)` idiom; gives anchors a description. |
| **5** ✅ | **`Caption` leaf + `.Under`** (C4) — **done** (same spec). Both scripts respelled; the K-1 burn-down stayed at 92 of 2772 exactly as predicted, because the caption rows were always *consumed* — what changed is that they are now *described*. | small design | Makes anchor rows *declared content*; removes duplicate literals in two scripts; the first place `read ∘ write` becomes satisfiable. |
| **6** | **Declared table columns** (C2) | medium design | The largest single cleanup in the corpus (27 sites). Already deferred-and-planned; the audit's evidence says promote it. Decide expression-trees vs. a column cursor. |
| **7** | **Typed cell leaves** (C1) | medium design | 61 sites; falls out of the same binding notion as 6 and should share its spelling. Big diagnostics win independent of writers. |
| **8** | **`Fields` / labelled-pair block** (C6) | medium | Built on 7. Also removes a hard-coded `2, 5` that CLAUDE.md's own rule condemns. |
| **9** | **`TableColumns`** (C3, first half) | medium | Closes the last mirror hole; makes the K-1 fund band a declaration. |
| **10** | **`Matrix` / keyed cross-tab** (C3, second half) | large | The campaign unlock. Needs 6/7's binding notion and a decision about how one region's discovered axis reaches another. |
| **11** | **Revisit "consumed nothing"** (§4.7) | large, invasive | Only if a writer, or `Repeat(x.Optional())`, is actually wanted. Record now; do not start. |
| — | **`.Until` context sensitivity** (§4.4), the axis-naming convention (§4.6), `Choice` vs `Else` severity (§4.9) | owner judgement | Cost nothing to leave; should be *decided* rather than defaulted. |

Items 1–3 are a single afternoon and touch no semantics. Items 4–5 are the invertibility payoff.
Items 6–7 are the declaration-code payoff. Items 9–10 are the K-1 campaign.

---

## 7. Summary of findings

**Class (c) findings, ranked:**

1. **C1** — cell kind trapped in accessor lambdas (61 sites, all 7 scripts).
2. **C2** — table column captions trapped in projection lambdas (27 sites, 4 scripts).
3. **C3** — no transposed table, and no keyed cross-tab (the K-1 header band and every K-1 section).
4. **C4** — content anchors referenced but never declared (10 literals, 3 duplicated; the only
   provable failure of `read ∘ write`).
5. **C6** — positional/labelled field access, including a hard-coded `2, 5` entity card.
6. **C5** — `(s, c) => true` as the spelling of "all columns".
7. **C7** — coercion facts (text-or-number codes) inside a projection.

**Algebra irregularities, ranked:**

1. **Zero consumption carries four meanings** (§4.7) — the deepest one.
2. **Three vocabularies for "a row that matches"**, with `.After` and `.Until` taking different
   argument types and the two trios' names crossing (§4.1).
3. **Five row/column mirror holes**, the largest being `Table` (§4.2).
4. **`.Until` is the only context-sensitive modifier** — replaces when adjacent, nests when
   separated (§4.4).
5. ~~The naming ladder does not reach the root shape~~ — **answered (§4.8): attempted, reverted.** Root capture is incompatible with method-group application (`spaces.Select(report.Map)`) and would report a wrapping helper's parameter name rather than the user's. `.Named` is the mechanism for a root.
6. **The `.Sized` argument vocabulary is not re-exported** while `.After`'s is (§4.9).
7. **Three naming conventions for axis pairs**, unwritten (§4.6).
8. **`Choice` and `Else` differ in severity and in nameability, not in structure** (§4.9).

**What the audit did *not* find**, stated because a negative result is worth as much here:
transparency is uniform across all four wrappers; the flow/overlay/`LayoutState` family is complete
for the documents in hand; "declares nothing" is an error consistently in both layouts; the
modifier laws are deterministic in every pair examined; and the (d) boundary — document above the
`return`, consumer at it — holds in all seven scripts.
