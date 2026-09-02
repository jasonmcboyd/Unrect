# Spec: Typed Leaves, Typed Tables, and the Labelled-Pair Block

**Status:** IMPLEMENTED (2026-09-02, branch `experiment/combined-select`). All eight steps of §9 are done and the suite is green. Every §7 failure text and every §11 script expectation was reproduced, including the flat K-1 burn-down (92 of 2772), the entity card's keys and its 2x5 footprint at J2, and the accessor counts 53 → 22. The new test suites (§10) are QA's.

This is **phase C** of the invertibility-audit remediation: audit items **6** (declared table
columns, finding **C2**), **7** (typed cell leaves, finding **C1**), and **8** (the labelled-pair
block, finding **C6**), plus the one-site coercion fact (**C7**) which falls out of the same
mechanism.

Extends `matcher-and-caption-spec.md` (phase B: the matcher family, the `Caption` leaf, the
removal-order discipline, the capture cautions), `flow-vocabulary-spec.md` (the layout grammar,
use-site naming, message templates), `wave2-shapes-spec.md` (engine rules, file layout, test
style), and `diagnostics-and-choice-spec.md` (severity rationale). All of their conventions apply.

Everything in §§1–6 is owner-settled. Where a detail had to be decided to make the spec mechanical
it is marked **[decided here]**; §13 lists every one of them in one place, and §14 lists what is
deliberately deferred with its names reserved.

**Nothing is deleted in this phase.** It is additive plus script respells: `Cell(lambda)`,
`Row`/`Column`/`Range`, `Table`/`TableRows(lambda)`, `TableView`/`TableRow` and every accessor on
`CellValue` survive untouched and stay supported. Every claim about the workbooks in §11 was
checked against the files themselves (a direct read of the sheet XML, listed per script); claims
about diagnostics figures are reasoned from the arithmetic and are marked as such.

---

## 0. What this pass does

| # | Change | Kind |
|---|---|---|
| 1 | Six typed cell leaves — `Text()`, `Decimal()`, `Integer()`, `Double()`, `Date()`, `Boolean()` | addition |
| 2 | **The firewall rule**: the leaf family is closed over `CellValue`'s canonical accessor set and never leads it (§2) | rule |
| 3 | Kind-vs-conversion diagnostic language, shared by leaves and tables (§1.4) | rule |
| 4 | `TableRows<T>()` — captions bound to a consumer type's members by name, kinds inferred from member types | addition |
| 5 | `TableRows<T>(bind => …)` — per-column overrides and per-member `Ignore`, via expression-tree selectors | addition |
| 6 | `TableRows()` — the dictionary form: `IReadOnlyList<IReadOnlyDictionary<string, CellValue>>` | addition |
| 7 | `Fields(params Field…)` / `Field(label)` — the labelled-pair block, extent from child count, anchored by its first label | addition |
| 8 | `CaptionComparer` — the binding comparer (case- and whitespace-insensitive), public because we hand back dictionaries built with it | addition |
| 9 | Five scripts respelled; two workbook-mirroring synthetics added to `ShapeExampleTests` | scripts |

After this pass, a cell's **kind** and a column's **caption** are declaration data rather than
lambda bodies, and the corpus's `CellValue` accessor calls drop from **53 to 22** — of which 7 are a
deliberately-preserved lambda-form example, 7 wait for the cross-tab (phase D), 3 are the adapter
probe that exists to show accessors, and 2 are the substrate script. By-name table bindings drop
from **27 to 7** (all in the one preserved example).

---

## 1. Typed cell leaves

### 1.1 Surface

```csharp
// Shape.cs, beside Cell
public static IShape<string>   Text();
public static IShape<decimal>  Decimal();
public static IShape<int>      Integer();
public static IShape<double>   Double();
public static IShape<DateTime> Date();
public static IShape<bool>     Boolean();
```

**Methods, not properties. [decided here]** Every other member of the vocabulary is a factory call
(`BlankRows()`, `WholeExtent()`, `AllColumns()`), the audit's property sketch would be the only
exception, and a property that returns a fresh shape on every read reads as a constant while
behaving as a constructor. `v.Next(Decimal())` costs two characters and keeps one rule.

**No `Cell` suffix, and named for what they yield. [settled by the brief]** `Decimal()` reads as
the *value the declaration promises*, which is the thing a reader of a declaration wants to know.
`Text()` is the one name taken from the kind rather than the CLR type — `String()` reads worse and
collides harder (§1.5) — and there the kind and the yield agree anyway.

### 1.2 Semantics

| Aspect | Rule |
|---|---|
| **Placement** | `Placement.Of(ExplicitArea(1, 1))` — exactly `Cell`'s |
| **Extent** | one cell; any other extent is a failure, with `Cell`'s message and this leaf's noun |
| **Kind** | the leaf asserts its `CellKind` before projecting; a mismatch is a failure (§1.4) |
| **Projection** | the corresponding canonical accessor, and nothing else |
| **Consumed** | 1×1 |
| **Description** | the factory name: `Text`, `Decimal`, `Integer`, `Double`, `Date`, `Boolean` |
| **Transparency** | opaque; no children |
| **Failures** | absorbable (`isProjectionFault: false`) — a kind disagreement is a disagreement about the data, exactly what `Optional`/`Else` exist for |

The kind/accessor table, which is the whole of the mapping:

| Leaf | Yields | Asserts `CellKind` | Accessor |
|---|---|---|---|
| `Text()` | `string` | `Text` | `GetString()` |
| `Decimal()` | `decimal` | `Number` | `GetDecimal()` |
| `Integer()` | `int` | `Number` | `GetInt()` |
| `Double()` | `double` | `Number` | `GetDouble()` |
| `Date()` | `DateTime` | `Temporal` | `GetDateTime()` |
| `Boolean()` | `bool` | `Boolean` | `GetBoolean()` |

**`Date()` yields the cell's `DateTime` verbatim, not its date part. [decided here]** `GetDate()`
truncates, and a leaf that silently discards the time of day would be the only member of the
vocabulary that hands back less than the cell holds. Truncation is consumer-side —
`Date().Select(d => d.Date)` — and `GetDate()`/`TryGetDate()` remain substrate conveniences. This
is also why the firewall's "1:1 with the accessor set" is stated over *kinds and conversions*
rather than over method names: `GetDate` is a transformation of `GetDateTime`, not a distinct
reading of the cell.

**Four of the six cannot fail once the kind is right** (`Text`, `Double`, `Date`, `Boolean`);
`Decimal` and `Integer` have a second, *conversion* step that can, and it speaks a different
sentence (§1.4).

### 1.3 Implementation

One internal generic leaf, six factories. `src/Unrect/Shapes/Primitives/TypedCellShape.cs`:

```csharp
/// <summary>Reads a cell whose kind has already been asserted; false means the value is of the
/// right kind but does not fit the CLR type asked for, and <paramref name="conversion"/> says so.</summary>
internal delegate bool CellReader<T>(CellValue cell, string at, out T value, out string? conversion);

internal sealed class TypedCellShape<T> : ShapeBase<T>
{
  public TypedCellShape(CellKind kind, string description, CellReader<T> read, Placement placement)
    : base(placement)
  {
    Kind = kind;
    Description = description;
    Read = read;
  }

  private CellKind Kind { get; }
  private CellReader<T> Read { get; }

  public override string Description { get; }

  public override ShapeResult<T> Project(ISpace extent, ShapeContext context)
  {
    var size = extent.Area.Size;

    if (size.Width != 1 || size.Height != 1)
      throw context.Failure($"a {Description} must be exactly one cell; this one is {size.Width}x{size.Height}", extent);

    var cell = extent[0, 0];
    var at = context.Locate(extent).A1;

    if (cell.Kind != Kind)
      throw context.Failure(CellReading.WrongKind(Kind, cell, at), extent);

    if (!Read(cell, at, out var value, out var conversion))
      throw context.Failure(conversion!, extent);

    return new ShapeResult<T>(value, size);
  }
}
```

`src/Unrect/Shapes/CellReading.cs` — internal, and **the single definition of what a kind failure
and a conversion failure say**, shared by the leaves and by the table binder (§4.9). That sharing
is the point: one vocabulary, two callers, so a `Decimal()` leaf and a `decimal` column cannot
describe the same cell differently.

```csharp
internal static class CellReading
{
  public static string WrongKind(CellKind expected, CellValue found, string at)
    => $"expected {expected} at {at}, found {Found(found)}";

  // An error cell says which error it is, through Core's own rendering — nothing is duplicated
  // here and nothing is added there.
  private static string Found(CellValue cell)
    => cell.Kind == CellKind.Error ? cell.ToString() : cell.Kind.ToString();

  public static bool ReadDecimal(CellValue cell, string at, out decimal value, out string? conversion) { … }
  public static bool ReadInteger(CellValue cell, string at, out int value, out string? conversion) { … }
  …
}
```

Numbers inside messages are formatted with `CultureInfo.InvariantCulture` **[decided here]** — a
failure message is a diagnostic artefact that gets pasted into an issue, not display output.
(`CellValue`'s own accessor messages interpolate culture-sensitively; that is a pre-existing
inconsistency in Core, recorded here and deliberately not fixed by this phase — see §14.)

The factories:

```csharp
/// <summary>One cell holding text.</summary>
public static IShape<string> Text()
  => new TypedCellShape<string>(CellKind.Text, "Text", CellReading.ReadString, Placement.Of(ExplicitArea(1, 1)));
…
```

### 1.4 Diagnostic language: kinds and conversions are two different sentences

**This is a rule of the vocabulary, not an implementation detail.**

> **A kind failure speaks kinds. A conversion failure speaks conversions.**
>
> The document has five kinds and one of them is `Number`. It does not have decimals, integers or
> doubles — those are the reader's business. So a cell of the wrong kind is reported in the
> document's vocabulary (`expected Number at B4, found Text`), *never* in the reader's
> (`expected Decimal…`), and a `Number` that will not fit the CLR type the declaration asked for is
> reported as what it is: a conversion, on a number that is really there.

The wave-1 truth that there is exactly one `Number` kind must stay visible in every message. A user
who sees `expected Number at B4, found Text` learns something true about the sheet; a user who sees
`expected Decimal` learns something false about spreadsheets.

| Situation | Message |
|---|---|
| `Decimal()` over a text cell | `expected Number at B4, found Text` |
| `Decimal()` over a blank cell | `expected Number at B4, found Blank` |
| `Text()` over an error cell | `expected Text at B4, found Error(#DIV/0!)` |
| `Integer()` over `1.5` | `the Number at B4 (1.5) is not a whole number` |
| `Integer()` over `5000000000` | `the Number at B4 (5000000000) is outside the range of a 32-bit integer` |
| `Decimal()` over `1E+30` | `the Number at B4 (1E+30) is not representable as a decimal` |

Notes, each load-bearing:

- **Blank is a kind**, so a blank cell is reported as a kind failure and reads correctly:
  `found Blank`. Nothing special-cases it. **[decided here]**
- **Error cells render through `CellValue.ToString()`**, which already produces `Error(#DIV/0!)` —
  Core's canonical spelling of an error, reused rather than copied. **[decided here]** Nothing is
  added to Core and the private display table is not duplicated.
- **Kind and conversion messages carry no remedial advice.** The rule this establishes
  **[decided here]**: *a declaration failure explains how to fix the declaration* (the unbound-member
  message in §7 lists the captions and shows the two spellings that fix it); *a per-cell failure
  states the fact and stops*. Per-cell messages appear once per bad cell in a 2,772-row sheet, and
  advice repeated 2,772 times is noise.
- **`at {A1}` is in the message even though the location line also carries it.** The template is
  shared with the table binder (§4.9), where the shape's own location is the *table* or the *row*
  and the cell address is the only thing that identifies the failure. One template, told the same
  way in both places, is worth one redundant address in the leaf case. **[decided here]**

### 1.5 Collision check — measured

The short names are used under `using static Unrect.Shapes.Shape;` beside `using System;`, so
`Decimal`, `Double` and `Boolean` sit next to the framework types of the same name. Compiled on
this machine (SDK 8.0.419, net8.0 consumer, `ImplicitUsings` off, the LINQPad default import set
plus `Unrect.Core`/`Unrect.Shapes`):

| Spelling | Result |
|---|---|
| `Text()`, `Decimal()`, `Integer()`, `Double()`, `Date()`, `Boolean()` as calls | ✅ compiles; the method group wins the invocation |
| `Decimal money = 1.5m;` — the framework type in **type position** | ✅ compiles; namespace-or-type lookup ignores methods |
| `Decimal.ToDouble(x)`, `Double.IsNaN(x)`, `Boolean.TrueString` — a static member off the **framework type name** | ❌ `CS0119: 'Shape.Decimal()' is a method, which is not valid in the given context` |
| `decimal.Parse(…)`, `double.IsNaN(…)`, `bool.TrueString` — the **keyword aliases** | ✅ compiles |
| `Text.StringBuilder` (the `System.Text` namespace by its short name) | ❌ `CS0246` — **and identically ❌ without our import**, so `Text()` costs nothing here |
| `TableRows<T>()` / `TableRows<T>(bind => …)` / `TableRows(r => …)` / `TableRows()` all in scope together | ✅ compiles; no `CS0121` |

**Verdict: safe, with one documented consequence.** In a file that statically imports `Shape`, a
static member must be reached through the C# keyword (`decimal.Parse`, `double.IsNaN`,
`bool.TryParse`) rather than through the framework type name. That is the spelling everyone writes
anyway; the corpus was grepped and **contains no `Decimal.`, `Double.`, `Boolean.` or `String.`
member access at all** (the single hit is a string literal in `CellValueTests`), across all 23
files that import `Shape` statically and all 7 scripts. The XML doc on each leaf records the
consequence, and §10 pins it with a test.

`Integer` and `Date` have no rival in `System`, in `Unrect.Core`, in `Unrect.Shapes`, or in the
LINQPad default import set. `Field` (§6) is the same pattern as `Decimal` — a method and a type of
the same name — and behaves the same way; `Field` has no static members, so the one broken spelling
does not exist for it.

### 1.6 `Cell(lambda)` survives, and the flow-of-leaves idiom is documentation

`Cell(project)` is unchanged and stays the escape hatch: a column whose kind varies, a value that
needs `TryGet*`, an error cell read on purpose. The typed leaves are the *declared* spelling of the
common case, not a replacement for the general one.

**The flow-of-typed-leaves idiom is documentation, not API. [settled by the brief]** There is no
`Fields`-like factory for a run of positional cells, because a vertical flow already is one:

```csharp
// before — the count 4 is a fact about today's file, and every kind is in a lambda body
var reportHeader = Column(4, c => new {
  Title = c[0].GetString(), SubTitle = c[1].GetString(),
  ReportDate = c[2].GetDateTime(), ReportId = c[3].GetString() });

// after — the count dissolves into the child count, and every kind is declared
var reportHeader = VerticalFlow(v => new {
  Title      = v.Next(Text()),
  SubTitle   = v.Next(Text()),
  ReportDate = v.Next(Date()),
  ReportId   = v.Next(Text()) });
```

Both consume 1 column × 4 rows, so nothing above or below moves.

**The rule that governs when to use it [decided here]:** *the flow-of-leaves replaces an
explicit-count strip (`Column(4, …)`, `Row(6, …)`), never a discovered one (`Column(project)`).*
Replacing a discovery with a child count would trade class (b) for class (a) and lose the drift
tolerance CLAUDE.md asks for; replacing a hard-coded count with a child count loses nothing and
gains a kind per field. This is why `investor-summary.linq`'s discovered header is left alone in
§11.3 while `simple-report`'s and `investor-irr`'s hard-coded ones are converted.

---

## 2. The firewall rule

The leaf family is **closed over `CellValue`'s canonical accessor set**. It mirrors that surface
1:1 and never leads it.

```
CellValue (Unrect.Core)          Shape leaves (Unrect.Shapes)
  GetString                        Text()
  GetDecimal                       Decimal()
  GetInt                           Integer()
  GetDouble                        Double()
  GetDateTime  (GetDate)           Date()
  GetBoolean                       Boolean()
  GetError                         — (§14: reserved, deliberately absent)
```

**The rule, in three parts:**

1. **No leaf may exist without a canonical accessor behind it.** There is no `Long()`, no
   `Single()`, no `Byte()`, no `Guid()`, no `Money()`, no `Percent()`, no `Enum<T>()` — not now and
   not later. A CLR conversion beyond the accessor set is `Select` territory:
   `Integer().Select(i => (long)i)` is correct, one-way, and honest about which side of the
   audit's (d) boundary it sits on.
2. **Nothing is added to Core to serve a leaf.** The dependency points **down**: leaves call
   accessors, and `Unrect.Core` does not know the shape layer exists. If a leaf seems to need a new
   accessor, the question to answer first is whether the *document* has that kind — and it has
   five, fixed in wave 1.
3. **Adding an accessor to Core does not automatically add a leaf.** `GetDate` is the standing
   example: it is a transformation of `GetDateTime`, not a different reading of the cell, so it has
   no leaf (§1.2).

**Why this is a firewall and not a preference.** The owner pressed on it twice, and the pressure is
right — every candidate leaf looks harmless on its own:

- **Each leaf is a new failure vocabulary.** `Long()` would need its own conversion sentence, its
  own tests, its own row in the table binder's kind map, and its own answer to "what does a
  `long` column do with `1.5`". Six leaves is a table a reader can hold in their head; sixteen is a
  reference manual.
- **Each leaf is a writer obligation.** The whole reason C1 is a finding is that a declaration must
  be executable backwards. A `Money()` leaf would have to decide what a writer emits — a number? a
  formatted string? a currency-styled cell? — and that is a *capability* question (formatting),
  which by the standing rule nothing in Core may require.
- **The document does not know about `long`.** A spreadsheet number is a `Number`. A leaf named for
  a CLR width would be describing the consumer, not the sheet, and would fail the audit's boundary
  test: *does it describe the document, or consume it?*

The same rule governs the table binder's supported member types (§4.2): the closed set there is the
closed set here, plus `Nullable<>` for blank tolerance and `CellValue` for "no accessor at all".

---

## 3. Three matching rules, and why they are three

Phase B established one content-matching rule and one implementation of it. This phase adds a
**binding comparer**, which is a different kind of question, and a **label rule**, which is a
narrow variant of content matching. Three rules is exactly two more than anyone wants, so each one
is named, scoped, and justified here — and **they are deliberately not unified**.

| Rule | Implementation | Compares | Applies to |
|---|---|---|---|
| **Content matching** | `CellMatching.TextEquals` (whole-cell, `Trim()`, `OrdinalIgnoreCase`) | a **cell** to a **literal the declaration wrote** | `RowContaining` / `ColumnContaining`; `Caption`; `.Until` bounds; `TableView`/`TableRow`'s by-name cell access (`r["Amount"]`), which asks the same question through its trimmed `OrdinalIgnoreCase` dictionary |
| **Label matching** | `CellMatching.LabelEquals` — content matching after a trailing run of `':'` and whitespace is removed **from both sides** | a **label cell** to a **label literal** | `Field(label)` only (§6), including the landmarks `Fields` anchors on |
| **Binding comparer** | `CaptionComparer` — ignores **all** whitespace and case | a **caption** to a **member name**, and dictionary keys to lookups | `TableRows<T>` caption↔member binding (inferred *and* overridden captions); the keys and lookups of the dictionaries returned by `TableRows()` and `Fields` |

**Why the binding comparer is not the content rule. [settled by the brief; stated here because it
will be asked again]** The content rule bridges *a cell and a literal*: both are text a human wrote
into a document, and the only noise between them is presentation whitespace and case. The binding
comparer bridges *two identifier spaces*: `"Contribution ITD"` is a caption, `ContributionItd` is a
C# identifier, and identifiers cannot contain spaces. Whitespace-insensitivity is meaningful there
and meaningless — indeed harmful — in a content matcher, where `RowContaining("Net Income")`
matching a cell reading `"NetIncome"` would be a false anchor of exactly the kind `TextEquals`'s
whole-cell rule exists to prevent. **Do not unify them.** If a future pass is tempted, the test in
§10 named `TheBindingComparerDoesNotLeakIntoContentMatching` is the pin.

**Why label matching is separate from content matching, and narrow. [decided here]** A trailing
colon is *presentation of a label*, not part of the label: the same export writes `EIN:` this year
and `EIN` next year, and a declaration that breaks on that is the fragility CLAUDE.md condemns. So
`Field("EIN")` matches a cell reading `EIN`, `EIN:`, or `EIN :`. The rule is deliberately confined
to `Field` — the one place where the matched text is known to be a label — and deliberately covers
**`':'` only**: every character we agree to ignore is a character a label may no longer contain, and
the corpus has colons and nothing else. It is implemented beside `TextEquals` in `CellMatching`, so
there remains exactly one file that decides what matching means.

```csharp
// Unrect.Strategies/CellMatching.cs, beside TextEquals
public static Func<CellValue, bool> LabelEquals(string label)
{
  var needle = TrimLabel(label);

  return cell => cell.TryGetString() is string value
    && TrimLabel(value).Equals(needle, StringComparison.OrdinalIgnoreCase);
}

// Strips a trailing RUN of colons and whitespace, not just one of each — a single
// TrimEnd(':').Trim() pass leaves "EIN:" behind on input like "EIN: :"; the loop keeps
// peeling until nothing more can go, so "EIN: :" reduces to "EIN".
private static string TrimLabel(string text)
{
  var trimmed = text.Trim();

  while (trimmed.Length > 0 && (trimmed[trimmed.Length - 1] == ':' || char.IsWhiteSpace(trimmed[trimmed.Length - 1])))
    trimmed = trimmed.Substring(0, trimmed.Length - 1);

  return trimmed;
}
```

**`CaptionComparer` is public. [decided here]** It is the only comparer in the library a consumer
can observe: `TableRows()` and `Fields` hand back dictionaries built with it, and a consumer who
copies one (`ToDictionary(…)`) or builds a lookup beside one would silently get the default
comparer and a different answer. Publishing it is how the returned dictionary's contract becomes
statable.

```csharp
// src/Unrect/Shapes/CaptionComparer.cs
/// <summary>How a column caption is matched to a member name: ignoring case, and ignoring
/// whitespace entirely, because a caption may contain spaces and an identifier may not.
/// This is NOT how cell content is matched — see the matchers — and the two must not be merged.</summary>
public sealed class CaptionComparer : IEqualityComparer<string>
{
  public static CaptionComparer Default { get; } = new CaptionComparer();
  public bool Equals(string? x, string? y);      // whitespace stripped, then OrdinalIgnoreCase
  public int GetHashCode(string value);
}
```

Whitespace means `char.IsWhiteSpace`, stripped everywhere in the string, not just at the ends.
Nothing else is stripped: punctuation, parentheses and `%` all count, so `"Net (USD)"` needs an
override rather than binding to `NetUsd`. **[decided here]** — widening this is a one-line change
the day a real file argues for it, and every character we strip is a character two captions can
collide on.

---

## 4. `TableRows<T>` — captions bound to a type

### 4.1 Surface

```csharp
// Shape.cs, beside the existing TableRows
public static IShape<IReadOnlyList<T>> TableRows<T>();
public static IShape<IReadOnlyList<T>> TableRows<T>(Func<TableBinding<T>, TableBinding<T>> bind);
```

```csharp
// the corpus's two spellings
var summary      = TableRows<SummaryRow>();
var transactions = TableRows<Transaction>(bind => bind
  .Column(t => t.Date, "Transaction Date")
  .Column(t => t.Type, "Transaction Type"));
```

`TableBinding<T>` is public, sealed, and **immutable**: every method returns a new instance, so a
binding handed to two factories cannot be changed by either. It is constructed only by
`TableRows<T>`; its constructor is internal.

```csharp
// src/Unrect/Shapes/TableBinding.cs
public sealed class TableBinding<T>
{
  internal TableBinding();

  /// <summary>Binds one member to a caption the comparer would not have found.</summary>
  public TableBinding<T> Column<TMember>(Expression<Func<T, TMember>> member, string caption);

  /// <summary>Declares that one member is not read from the table — the opt-out from strictness,
  /// per member and by name, so a member added later is still loud.</summary>
  public TableBinding<T> Ignore<TMember>(Expression<Func<T, TMember>> member);
}
```

**No `headerRows` parameter. [decided here]** Both new forms are caption-driven, and
`headerRows: 0` means "no captions"; an overload that could only be passed `1` is a parameter that
can only be wrong. `Table(0, …)` and the lambda `TableRows` remain for headerless tables.

**Overload resolution is safe — measured.** `TableRows<T>()`, `TableRows<T>(bind)`,
`TableRows<T>(Func<TableRow,T>)`, `TableRows<T>(int, Func<TableRow,T>)` and `TableRows()` compile
together, and `TableRows<Transaction>(bind => bind.Column(t => t.Date, "…"))` binds to the binding
overload with no `CS0121` (SDK 8.0.419, C# 12). §10 keeps that as a compile-level test so a future
overload cannot break it silently.

One measured incompleteness (QA, 2026-09-02): an untyped `null` argument — `TableRows<int>(null!)`
— IS ambiguous between the projection and binding overloads and needs a cast. Any lambda still
resolves without help; real code passing an untyped null is pathological, but the "safe" claim
above applies to lambdas and typed arguments only. One test carries the cast with a comment.

**Description: `TableRows<Transaction>`. [decided here]** The type argument is part of what the
user typed, and a path segment reading `TableRows` for four different tables in one report would be
ungreppable. Rendered with `typeof(T).Name`.

### 4.2 Supported member types — the closed set

| Member type | Kind asserted | Read as | Blank cell |
|---|---|---|---|
| `string` | `Text` | `GetString()` | failure |
| `decimal` | `Number` | `GetDecimal()` | failure |
| `double` | `Number` | `GetDouble()` | failure |
| `int` | `Number` | `GetInt()` | failure |
| `DateTime` | `Temporal` | `GetDateTime()` | failure |
| `bool` | `Boolean` | `GetBoolean()` | failure |
| `decimal?` `double?` `int?` `DateTime?` `bool?` | as above | as above | `null` |
| `string?` (annotated nullable — §4.7) | `Text` | `GetString()` | `null` |
| `CellValue` | *(none)* | the cell itself | the cell itself |

This is §2's closed set, and nothing else. Anything else is a **construction-time** error (§7, C1):

```
Transaction.Quantity is a long, and no cell accessor yields long.
Supported: string, decimal, double, int, DateTime, bool, CellValue, and the nullable forms.
Read it as int or decimal and convert in Select.
```

**`CellValue` is supported, and it is the one addition to the brief's list. [decided here]**
Without it, a single odd column — the K-1's ATAX code, which is text in some rows and a number in
others (finding **C7**) — forces the whole table back to the lambda form. That cliff is worse than
the surface: `CellValue` is not a conversion at all, so it does not breach the firewall; it is the
in-table spelling of `Cell(c => c)`, kind-agnostic and blank-visible by construction. It is also
the smallest possible answer to C7, which the audit said "would fall out of the same mechanism". If
the owner would rather not have it, delete the row: nothing else in this spec depends on it.

**`Nullable<T>` means blank-tolerant, not kind-tolerant. [decided here]** A `decimal?` column
yields `null` for a blank cell and still **fails loudly** on a text cell. Tolerating a blank is a
statement about the *data* ("this figure is sometimes not reported"); tolerating a wrong kind is a
statement about the *format*, and no real format has one.

### 4.3 How `T` is constructed

Resolved once, at shape construction, in this order **[decided here]**:

1. **A single public constructor with parameters, and no public parameterless constructor** →
   *constructor binding*. Every parameter must bind (or be ignored with a default). This is the
   record-with-primary-constructor case, and it is the one the sketches use.
2. **A public parameterless constructor** → *property binding*. Every public property with an
   accessible setter (`set` **or** `init`) must bind or be ignored; read-only properties are
   invisible to the binder and are never required.
3. **Anything else** → construction-time error (§7, C2/C3): no public constructor, or several
   parameterized ones and no parameterless one.

Rule 1 is checked first so that a positional record binds through its constructor even though its
properties look settable; rule 2 catches the ordinary DTO and the
`record X { public int A { get; init; } }` form. A type with *both* a parameterless and a
parameterized constructor takes rule 2 — the property path can always express what the constructor
path can, and the reverse is not true.

On the constructor path, a member selector (`bind.Column(t => t.Date, …)`) names a **property**,
which is resolved to the constructor parameter of the same name under the binding comparer
(positional records name them identically; hand-written constructors differ only in case). A
selector that matches no parameter is a construction-time error naming it.

Materialization is compiled once, at construction, with expression trees:

```csharp
// constructor path
var values = Expression.Parameter(typeof(object?[]), "values");
var arguments = parameters.Select((parameter, index) => parameter.Ignored
  ? (Expression)Expression.Constant(parameter.DefaultValue, parameter.Type)
  : Expression.Convert(Expression.ArrayIndex(values, Expression.Constant(index)), parameter.Type));

_materialize = Expression.Lambda<Func<object?[], T>>(Expression.New(constructor, arguments), values).Compile();

// property path: Expression.MemberInit(Expression.New(parameterless), Expression.Bind(property, …))
```

**Verified for netstandard2.1 and no new dependency:** `System.Linq.Expressions` —
`Expression<TDelegate>`, `Expression.MemberInit`, `LambdaExpression.Compile()` — is in-box in
netstandard2.1; so is `System.Reflection`'s `GetCustomAttributesData()`, which §4.7 needs. Nothing
is added to any `.csproj`. On a runtime without codegen (full AOT), `Compile()` falls back to the
expression interpreter: slower, still correct. `Expression.Bind` accepts an `init`-only property
(the `modreq` is a compile-time signal; the setter is an ordinary setter in metadata) — pinned by a
test in §10, because it is the one mechanical claim here that could rot.

### 4.4 When binding happens

| Resolved | When | Failure mode |
|---|---|---|
| `T`'s constructible shape, member set, member types, materializer | **shape construction** | `ArgumentException` from the factory (§7, C1–C9) |
| The declared overrides and ignores | **shape construction** | as above |
| caption → column index, per table | **`Map`, once per table application** | `ShapeException` (§7, M1–M2) |
| cell → value | **`Map`, once per row per bound member** | `ShapeException` (§7, M3) |

**No reflection runs per `Map`, and none per row.** The shape holds an immutable plan and a
compiled delegate; a `Map` builds one caption→index array from the `TableView`'s captions and then
reads rows through it. Shapes stay immutable and concurrently reusable, exactly as wave 2 requires:
one `TableRows<Transaction>()` applied to two hundred workbooks at once holds no per-call state.

### 4.5 Strictness

**Every required member must find a caption, and a member that finds none is loud.** Required means
"every constructor parameter" or "every settable public property", minus the ignored ones. The
check runs once per table at `Map` time (the captions are data) and reports **all** unbound members
in one failure, with the table's captions listed and both fixes shown (§7, M1).

**Strictness is one-directional and deliberately so. [decided here]** A caption that no member
claims is **not** a failure: real reports carry columns a given consumer does not want, and a
declaration that broke on an added column would be unusable. (An `Info` diagnostic naming unread
captions is a natural extension and is deferred — §14.)

**The opt-out is `Ignore`, per member, by name. [decided here]** Not a `nonStrict` flag:

```csharp
var rows = TableRows<Transaction>(bind => bind.Ignore(t => t.ImportedAt));
```

This is the same shape of decision as `.Until(…, orEnd: true)` and `.Optional()` — tolerance
declared at the one place it is acceptable, in terms of the thing being tolerated. A flag would
tolerate *the next* member somebody adds too, silently, which is exactly the failure mode
strictness exists to prevent.

`Ignore` on the property path leaves the property at its default. `Ignore` on a constructor
parameter requires the parameter to **have a default value**, which is then used; a parameter
without one cannot be skipped, and that is a construction-time error (§7, C6). **[decided here]**

### 4.6 Overrides

`bind.Column(member, caption)` binds one member to a caption the comparer would not have found.
Both the inferred and the overridden caption are resolved through `CaptionComparer` (§3), so an
override is a *different caption*, not a *different rule* — `Column(t => t.Date, "Transaction Date")`
still matches a header reading `"Transaction  Date"`.

Member selectors are `Expression<Func<T, TMember>>` and must be a **direct property access on the
lambda parameter**: `t => t.Date`, optionally wrapped in the compiler's boxing `Convert`. Anything
else — `t => t.Date.Year`, `t => t.Lines[0]`, a field, a method call — is a construction-time error
(§7, C4). Fields are excluded **[decided here]**: the constructor path resolves parameters by
property name, records and DTOs use properties, and admitting fields would double the resolution
rules for no motivating type.

### 4.7 Nullable reference types

`Nullable<T>` is a CLR type and reflection sees it. `string?` is not — it is `string` plus
`[Nullable]`/`[NullableContext]` metadata the consumer's compiler emitted. The binder **reads that
metadata**, with these rules **[decided here]**:

1. Look for `System.Runtime.CompilerServices.NullableAttribute` on the property (or the constructor
   parameter) itself; its single argument is a `byte` (or a `byte[]`, in which case take `[0]`).
2. Otherwise, the nearest `NullableContextAttribute` walking outward: declaring type, then its
   enclosing types, then the module.
3. Otherwise, oblivious.

`2` means annotated-nullable ⇒ **blank-tolerant**. `1` (not-null) and `0` (oblivious) ⇒ **strict**.

Both attributes are generated per-assembly and are `internal` to the consumer's assembly, so they
must be matched **by full name through `GetCustomAttributesData()`**, never by type. Our supported
member types are all non-generic, so only the single-`byte` form can occur; the `byte[]` case is
handled defensively and is not exercised by any supported type.

**Why not the simpler rules.** "`string` is always blank-tolerant" is silent, and a `null` Client
travelling into a consumer is precisely the kind of quiet wrongness this library exists to prevent.
"`string` is always strict" leaves a genuinely optional text column with no spelling at all. Reading
the annotation makes the declared type mean what it says — which is the whole thesis of this phase —
and its degradation path (no metadata ⇒ strict) is the loud one.

### 4.8 Defaults, and what is unchanged

`TableRows<T>` uses **exactly** the existing `TablePlacement()`: past leading blank rows, then rows
and columns while they carry values, one header row. Header discovery, extent discovery, blank-gap
absorption, `.After`/`.Sized`/`.Until` composition, `Repeat` interaction — all identical to
`TableRows(lambda)`, because it is the same `TableShape` with a different projection. **[settled by
the brief]** A table with no body rows yields an empty list, as today.

### 4.9 Reading a cell

Per bound member, per row: assert the kind, then convert — through the **same `CellReading` helpers
the leaves use** (§1.3), with the cell's own A1 from `TableRow.AddressOf(column)`, and the problem
prefixed by the column so the failing cell is identifiable when the failing shape is the table:

```
column 'Amount': expected Number at D11, found Text
column 'Amount': the Number at D11 (1.5) is not a whole number
```

A `CellValue` member skips both steps. A nullable member checks `IsBlank` first and yields `null`.

---

## 5. `TableRows()` — the dictionary form

```csharp
public static IShape<IReadOnlyList<IReadOnlyDictionary<string, CellValue>>> TableRows();
```

| Aspect | Rule |
|---|---|
| **Keys** | the header captions, trimmed as `TableView.ColumnNames` already trims them |
| **Comparer** | `CaptionComparer.Default`, so `row["contribution itd"]` and `row["ContributionITD"]` both find `"Contribution ITD"` |
| **Values** | `CellValue` — **never strings**. Kinds and blankness survive; this is an exploratory reader, not a stringifier |
| **Placement / extent / header** | identical to `TableRows<T>` and `TableRows(lambda)` |
| **Description** | `TableRows` |
| **Duplicate captions under the comparer** | loud failure naming both cells (§7, D3) |
| **A column with no caption** | loud failure naming the cell (§7, D2) |

**Duplicates are strict, and the message explains the comparer. [settled by the brief; message
decided here]** Two columns that collide only under the comparer (`"Net Amount"` and `"NetAmount"`)
would otherwise produce a dictionary that silently drops a column, and a reader staring at two
visibly different captions needs to be told *why* they collide. A lenient variant (last wins, first
wins, a list per key) is deferred until a real file demands it — §14.

**An uncaptioned column is a failure here and harmless in `TableRows<T>`. [decided here]** The
dictionary form promises one entry per column and cannot keep that promise without a key; the typed
form only promises that every *member* found a column, so an unnamed column is simply one nothing
binds to. Both messages say which cell they mean.

**The exploratory idiom is documentation:** open an unfamiliar sheet with `TableRows()`, look at the
kinds, then graduate to `TableRows<T>()` once the columns are known. That sentence belongs in the
XML doc; there is no API for it.

---

## 6. `Fields` and `Field` — the labelled-pair block

Audit **C6**: `Range(2, 5, b => … b[0, r].GetString().TrimEnd(':') …)` — a width, a height, a
string surgery, and five labels that appear nowhere in the declaration.

### 6.1 Surface

```csharp
public static Field Field(string label);
public static IShape<IReadOnlyDictionary<string, CellValue>> Fields(params Field[] fields);

// src/Unrect/Shapes/Field.cs
/// <summary>One labelled pair in a Fields block: a label cell, and the value cell beside it.</summary>
public sealed class Field
{
  internal Field(string label);
  public string Label { get; }
}
```

```csharp
// the K-1 entity card, entire
var entity = Fields(
  Field("EIN"),
  Field("Entity Type"),
  Field("Deal Type"),
  Field("State Sourced Income"),
  Field("Underlying CFC(s)/PFIC(s)"));
```

**`Field` is a declaration value rather than a shape. [decided here]** `Fields` needs each field's
label to key the result, and a `params IShape<CellValue>[]` could not supply it without
type-testing the elements — which would make `.Under`'s openness (any string shape may sit there)
impossible to imitate honestly. A small value type also leaves room for the deferred extensions
(§14) without changing `Fields`' signature.

### 6.2 Geometry, exactly

| Aspect | Rule |
|---|---|
| **One field** | a **2 wide × 1 tall** extent: the label cell, and the value cell **immediately to its right**. No gap, no spanning, no wider value region |
| **The block** | a vertical flow of the fields in declaration order: **2 wide × n tall**, where n is the number of `Field`s |
| **The extent** | comes from the child count. There is no `2, 5` to get wrong, and adding a sixth field is one line |
| **Placement** | anchored on the **first field's label**, on both axes (§6.3) |
| **Result** | `IReadOnlyDictionary<string, CellValue>`, keyed by the **declared labels verbatim**, built with `CaptionComparer.Default` |
| **Values** | the value cell as a `CellValue` — blank values are `Blank`, not failures. The labels are the structure; the values are data |
| **Description** | `Fields`; each child renders `Field("EIN")` |
| **Transparency** | opaque, like every cursor composite, with an `IOpaqueComposite.Reason` |

`Fields` is built on the existing `FlowShape<T>` with a cursor lambda — the same desugaring
`.Under` uses, and for the same reason: **nodes get the algebra free.** Each field is a real child
with its own path segment, `.Optional()`/`.Else` absorb a missing block, `.After` replaces the
anchor, `.Named` names the block, and the consumed footprint is the flow's ordinary arithmetic.
`declared: null` is passed at the `Next` call site — mandatory, per the phase-B hazard: without it
every field would be labelled with this helper's own loop variable.

**A missing label row is an ordinary loud failure** raised from inside the field, absorbable by
`Optional`/`Else` on the block (§7, F2). There is no per-field tolerance: `Field` is not a shape,
so there is nothing to call `.Optional()` on. That is a real limitation and it is recorded in §14.

### 6.3 Anchoring: `Fields` finds its own block

**`Fields` anchors on its first field's label, on both axes. [decided here — beyond the brief]**

The alternative is `Placement.Default` plus an explicit
`.After(Then(To(ColumnContaining("EIN:")), To(RowContaining("EIN:"))))` — which is what the K-1
script writes today, and which would leave the *same literal in two vocabularies*: the exact defect
C4 attacked and phase B removed. Self-anchoring is the `Caption` precedent applied to a block: a
caption declares its row and finds it; a field block declares its labels and finds the first one.

```csharp
private static Placement FieldsPlacement(string label)
  => new Placement(
       OffsetStrategies.Then(
         OffsetStrategies.To(new PredicateColumnLandmark(
           CellMatching.AnyCellInColumn(CellMatching.LabelEquals(label)), $"no column with the label '{label}'")),
         OffsetStrategies.To(new PredicateRowLandmark(
           CellMatching.AnyCellInRow(CellMatching.LabelEquals(label)), $"no row with the label '{label}'"))),
       null);
```

Both landmark classes are already `internal` in `Unrect.Strategies` and already visible to `Unrect`
through the existing `InternalsVisibleTo`, so **no public landmark surface is added**; the
descriptions are the phase-B negative-noun form, so a miss reads
`no column with the label 'EIN' exists in the available space`.

Column first, then row — the same order the K-1 script uses, and the one that works when the label
column is far to the right of a wide sheet. The anchor is an ordinary placement, so `.After(…)`
**replaces** it (the escape hatch when a sheet holds two blocks with the same first label) and
`.Down(n)`/`.Right(n)` compose onto it, per the standing laws.

**The consequence to know:** like `Caption`, `Fields` searches from the cursor and can therefore
jump. Inside a `Repeat`, the same recipe as phase B §3.4 applies — the anchor is *inside* the flow,
so a repeat over field blocks wants the matcher hoisted onto the item as well. Recorded in the XML
doc; no file needs it yet.

### 6.4 What this replaces

```csharp
// before: a width, a height, five undeclared labels, and string surgery in a projection
var entity = Range(2, 5, b => Enumerable.Range(0, 5)
    .ToDictionary(r => b[0, r].GetString().TrimEnd(':'), r => b[1, r].ToString()))
  .After(Then(To(ColumnContaining("EIN:")), To(RowContaining("EIN:"))));

// after: five labels, and nothing else
var entity = Fields(Field("EIN"), Field("Entity Type"), Field("Deal Type"),
                    Field("State Sourced Income"), Field("Underlying CFC(s)/PFIC(s)"));
```

`TrimEnd(':')` dies because the *label rule* absorbs the colon (§3), so the keys are the labels the
declaration wrote: `EIN`, `Entity Type`, … — **identical to today's output** (§11.5). The values
improve from `CellValue.ToString()` diagnostic renderings (`"Text(US Flow-Through)"` — a latent
defect in the current script) to `CellValue`s.

---

## 7. Failure catalogue

Message texts are normative. Subject, path and location come from the existing
`ShapeException`/`ShapeContext` machinery and are not restated per row.

### Leaves (`ShapeException`, absorbable)

| # | When | Problem text |
|---|---|---|
| L1 | extent is not 1×1 | `a Decimal must be exactly one cell; this one is 2x1` |
| L2 | wrong kind | `expected Number at B4, found Text` |
| L3 | blank cell | `expected Number at B4, found Blank` |
| L4 | error cell | `expected Number at B4, found Error(#DIV/0!)` |
| L5 | `Integer()`, not whole | `the Number at B4 (1.5) is not a whole number` |
| L6 | `Integer()`, out of range | `the Number at B4 (5000000000) is outside the range of a 32-bit integer` |
| L7 | `Decimal()`, not representable | `the Number at B4 (1E+30) is not representable as a decimal` |

### The dictionary form (`ShapeException`, absorbable)

| # | When | Problem text |
|---|---|---|
| D1 | empty extent with a header declared | *(existing)* `a header row was declared but the table's extent is empty` |
| D2 | a column has no caption | `the column at C7 has no caption; every column needs one to be read by name` |
| D3 | two captions collide under the comparer | `the columns at B7 ('Net Amount') and E7 ('NetAmount') carry the same caption; captions are matched ignoring case and whitespace` |

### `TableRows<T>` — construction time (`ArgumentException` from the factory)

`paramName` names the offending argument as precisely as possible and is omitted when the fault is
in `T` itself. **[decided here; corrected post-implementation]** The implementation is more precise
than this section's original `nameof(bind)` rule and QA pinned the actual values: C4 → `member`,
C5 → `member`, C7 → `binding`, C8 → `caption`; C1/C6 omit it. The precision is deliberate — keep it.

Two further post-implementation notes (QA, 2026-09-02): F1's "a Field must be two cells wide and
one row tall" text is unreachable through the public surface (Field is a descriptor, not a shape;
sizing the block yields the engine's placement failure blamed at `Field("…")#1`, which is what is
pinned) — the text stays as defense-in-depth. And M1's guidance example must name an UNBOUND
member (fixed in the same round; the original implementation named the first member of `T`).

| # | When | Message |
|---|---|---|
| C1 | unsupported member type | `Transaction.Quantity is a long, and no cell accessor yields long. Supported: string, decimal, double, int, DateTime, bool, CellValue, and the nullable forms. Read it as int or decimal and convert in Select.` |
| C2 | several parameterized constructors, no parameterless one | `Transaction cannot be constructed: it has 3 public constructors and no parameterless one. Give it one constructor, or a parameterless constructor and settable properties.` |
| C3 | no public constructor, abstract, or an interface | `Transaction cannot be constructed: it has no public constructor.` |
| C4 | selector is not a direct property access | `Column(t => t.Date.Year) does not select a property of Transaction; select a property directly.` |
| C5 | the same member bound twice | `Transaction.Date is bound twice.` |
| C6 | `Ignore` on a constructor parameter with no default | `Transaction.Note cannot be ignored: the constructor parameter has no default value.` |
| C7 | a member both bound and ignored | `Transaction.Date is both bound and ignored.` |
| C8 | null/empty caption or null selector | `A column caption cannot be empty or whitespace.` |
| C9 | nothing to bind | `Transaction has no properties to bind.` |

### `TableRows<T>` — map time (`ShapeException`, absorbable)

| # | When | Problem text |
|---|---|---|
| M1 | required members unbound | `no column binds Transaction.Date or Transaction.Type; the table's captions are 'Client', 'Transaction Date', 'Transaction Type', 'Amount'. Bind one with Column(t => t.Date, "…") or drop it with Ignore(t => t.Date)` |
| M2 | a member matches two columns | `Transaction.Amount matches the columns at D7 ('Amount') and F7 ('amount'); captions are matched ignoring case and whitespace` |
| M3 | a cell is the wrong kind or will not convert | `column 'Amount': expected Number at D11, found Text` / `column 'Amount': the Number at D11 (1.5) is not a whole number` |

M1 lists **every** unbound member in one failure, joined with commas and a final "or"; its location
is the table's origin. M3's location is the table's origin too — the cell is named in the message,
which is why §1.4 puts `at {A1}` in the shared template.

### `Fields` (`ShapeException`, absorbable)

| # | When | Problem text |
|---|---|---|
| F1 | field extent is not 2×1 | `a Field must be two cells wide and one row tall; this one is 1x1` |
| F2 | the label is not there | `expected a label reading 'EIN' here, but this cell reads 'Deal Type'` / `…, but this cell is blank` / `…, but this cell holds a Number` |
| F3 | the anchor is missing | *(existing engine text)* `no column with the label 'EIN' exists in the available space` |
| F4 | two fields with the same label (construction) | `ArgumentException`: quotes the DUPLICATE's verbatim label (e.g. `'ein:'`), matching rules stated per relation — see the §7 corrections note; the review-fix round split this into two guards (same-cell match via `LabelEquals`, key collision via `CaptionComparer`), each with honest message text |
| F5 | empty/whitespace/null label (construction) | `ArgumentException`: `A field label cannot be empty or whitespace.` — mirroring `Caption`'s guard |
| F6 | `Fields()` with no fields (construction) | `ArgumentException`: `A Fields block must declare at least one field.` — mirroring `Under`'s guard |

---

## 8. Files

```
src/Unrect/Shapes/Shape.cs                              six leaves; TableRows<T> x2; TableRows(); Field; Fields
src/Unrect/Shapes/Primitives/TypedCellShape.cs          new — one internal leaf, six factories drive it
src/Unrect/Shapes/Primitives/FieldShape.cs              new — the 2x1 labelled pair
src/Unrect/Shapes/CellReading.cs                        new — kind and conversion message templates, shared
src/Unrect/Shapes/CaptionComparer.cs                    new — public binding comparer
src/Unrect/Shapes/TableBinding.cs                       new — public immutable binder (Column / Ignore)
src/Unrect/Shapes/Field.cs                              new — public declaration value
src/Unrect/Shapes/Binding/RowBinding.cs                 new — internal plan: members, kinds, materializer
src/Unrect/Shapes/Binding/MemberPlan.cs                 new — internal
src/Unrect/Shapes/Binding/NullableAnnotations.cs        new — internal, reads NullableAttribute by name
src/Unrect.Strategies/CellMatching.cs                   LabelEquals beside TextEquals
src/Unrect.Strategies/Unrect.Strategies.csproj          InternalsVisibleTo comment names the landmark classes

src/Unrect.Tests/Shapes/TypedLeafTests.cs               new
src/Unrect.Tests/Shapes/TypedTableTests.cs              new
src/Unrect.Tests/Shapes/DictionaryTableTests.cs         new
src/Unrect.Tests/Shapes/FieldsTests.cs                  new
src/Unrect.Tests/Shapes/CaptionComparerTests.cs         new
src/Unrect.Tests/Shapes/ShapeExampleTests.cs            respelled; two new workbook mirrors
src/Unrect.Tests/Shapes/ShapeReExportTests.cs           the single-import declaration grows the new vocabulary
src/Unrect.Tests/Shapes/ShapeInspectionTests.cs         descriptions of the new shapes
src/Unrect.Tests/StrategyTests.cs                       LabelEquals rules
linqpad/simple-report.linq                              typed leaves + TableRows<Transaction>
linqpad/investor-irr.linq                               typed leaves + TableRows<SummaryRow>/<CashFlow>
linqpad/investors-by-deal.linq                          Text() + TableRows<DealTransaction>() (no overrides)
linqpad/investor-summary.linq                           Text() only — keeps the lambda-form example
linqpad/scrubbed-k1.linq                                Fields
linqpad/edge-cases.linq                                 a typed-leaf diagnostics section
CLAUDE.md, docs/design/invertibility-audit.md           status notes (step 7)
```

No new dependencies. netstandard2.1, nullable enabled, `LangVersion=Latest`. The public surface
grows by 6 leaf factories, 3 table factories, `Field`/`Fields`, and the three public types
`TableBinding<T>`, `Field`, `CaptionComparer`. **Nothing is removed.**

---

## 9. Addition order — every step green

`dotnet build src/Unrect.sln -v q --no-incremental` clean and `dotnet test src/Unrect.sln` passing
after each step. This phase deletes nothing, so the phase-B rule ("every deletion is preceded by
the migration of what it pinned") has no work to do; the ordering is instead about **never
respelling onto a factory that does not exist** and **never changing a script before the tests that
mirror it**.

| Step | Work | Why here |
|---|---|---|
| **0** | *(additive)* `CellReading`, `TypedCellShape`, the six factories, `TypedLeafTests`, the collision test, the `ShapeReExportTests` line. | Everything else quotes these messages. Independent of all the rest. |
| **1** | *(additive)* `CaptionComparer` + `CaptionComparerTests`; `CellMatching.LabelEquals` + its `StrategyTests` cases. | Two of the three matching rules, pinned before anything depends on them. |
| **2** | *(additive)* `TableRows()` dictionary form + `DictionaryTableTests`. | Needs step 1; the smallest consumer of the comparer, and it proves the duplicate and uncaptioned policies before the typed form leans on them. |
| **3** | *(additive)* `TableBinding<T>`, `RowBinding`, `NullableAnnotations`, `TableRows<T>` x2 + `TypedTableTests`. | The biggest piece; needs steps 0 (cell reading) and 1 (comparer). |
| **4** | *(additive)* `Field`, `FieldShape`, `Fields` + `FieldsTests`. | Needs step 1 (`LabelEquals`, the comparer for keys). Independent of 2–3 and may be done in parallel. |
| **5** | *(tests)* The two workbook-mirroring synthetics in `ShapeExampleTests` (a typed table over the simple-report grid; a field block over a K-1-shaped header band), asserting the **same values and the same consumed extents** as the existing spellings. | The regression pins for step 6, written before the scripts move. |
| **6** | *(scripts)* The six script respells of §11, and the `ShapeExampleTests` end-to-end assertions respelled. Check every expectation in §11. | The evidence of success, and the last thing that can move. |
| **7** | *(docs)* `CLAUDE.md` (vocabulary, status, the C1/C2/C6/C7 open questions), a status note on `invertibility-audit.md` §§2/6 marking items 6–8 done, and the status line at the head of this spec. | |

Steps 0 and 1 must precede everything; 2, 3 and 4 are independent of each other after 1.

---

## 10. Test outline (house style, synthetic grids)

### TypedLeafTests — new
- **each leaf reads its kind**: six one-cell grids, six values, six `Description` strings, consumed
  1×1 in each;
- **kind failures speak kinds**: `Decimal()` over text → `expected Number at A1, found Text`; over
  blank → `found Blank`; over an error cell → `found Error(#DIV/0!)`; `Text()` over a number →
  `expected Text at A1, found Number`. Subject and path pinned, and the **word `Decimal` never
  appears in a kind message** (the pin for §1.4);
- **conversion failures speak conversions**: `Integer()` over `1.5`, over `5e9`, `Decimal()` over
  `1e30` — the three texts, each naming the cell and the value;
- **`Date()` does not truncate**: a cell holding `2026-03-04 13:45` yields exactly that;
- **extent guard**: a leaf forced to `.Sized(WholeExtent())` over a 2×1 grid →
  `a Decimal must be exactly one cell; this one is 2x1`;
- **absorbable**: `Decimal().Optional()` over a text cell yields `default` and records exactly one
  `Warning` naming the leaf and its A1; `Choice(Decimal().Select(…), Text().Select(…))` picks the
  arm whose kind matches and records one `Info` for the other — *the payoff the audit predicted:
  alternatives discriminate on declared kinds*;
- **composition**: a leaf in a `VerticalFlow` gets its use-site label (`v.Next(amount)` →
  `'amount'`), and the four-leaf header flow consumes 1×4 — **identical to `Column(4, …)`**, pinned
  side by side on one grid (the §1.6 claim);
- **the collision pin**: one test file that imports `Shape` statically, declares
  `Decimal money = 1m; Double ratio = 0.5; Boolean flag = true;`, calls `decimal.Parse`,
  `double.IsNaN`, `bool.TryParse`, and calls all six leaves — it exists to fail at *compile* time if
  a future name breaks the corpus (§1.5).

### CaptionComparerTests — new
- case, leading/trailing/interior whitespace, and combinations: `"Contribution ITD"` ≡
  `ContributionItd`; `"IRR"` ≡ `Irr`; `"End Balance"` ≡ `EndBalance`;
- **not** equal: `"Net (USD)"` vs `NetUsd`; `"Net Amount"` vs `NetIncome`;
- hash agreement for every equal pair;
- **`TheBindingComparerDoesNotLeakIntoContentMatching`**: `RowContaining("Net Income")` does **not**
  match a cell reading `"NetIncome"`, and `Caption("Net Income")` does not either — the §3 pin.

### StrategyTests — `LabelEquals`
- `EIN` matches `EIN`, `EIN:`, `ein :`, `  EIN:  `; does not match `EINS`, `MY EIN`, a number, or
  blank; a colon *inside* the label (`Note: see below`) is preserved and only the trailing run is
  dropped.

### DictionaryTableTests — new
- keys are the captions; values are `CellValue`s with kinds intact (a date column stays `Temporal`,
  a blank cell is `Blank`, an error cell is `Error`) — **nothing is stringified**;
- lookups go through the comparer (`row["transactiondate"]` finds `"Transaction Date"`);
- the dictionary is read-only and its comparer is `CaptionComparer.Default`;
- **duplicate captions** under the comparer → D3, naming both cells, including the case where the
  two captions differ textually;
- **an uncaptioned column** → D2, naming the cell;
- defaults: leading blank rows skipped, extent discovered, empty body yields an empty list.

### TypedTableTests — new. The binding matrix is the spine of this file.
- **binds free**: a record whose property names differ from the captions only by case and spaces
  (`ContributionItd` ↔ `"Contribution ITD"`, `Irr` ↔ `"IRR"`, `InvestorName` ↔ `"Investor Name"`);
- **every supported type**, one column each, values and kinds asserted; plus a `CellValue` column
  holding text in one row and a number in the next (the C7 case);
- **nullable**: `decimal?` yields `null` for a blank and a value otherwise; `decimal` over a blank
  fails with L3's text prefixed by the column; `decimal?` over **text** still fails (blank-tolerant,
  not kind-tolerant); `string?` (annotated) tolerates blank while `string` does not — with the
  record declared in a `#nullable enable` file, plus one type in a `#nullable disable` region
  asserting the oblivious ⇒ strict rule;
- **construction**: a positional record; a record with `init` properties; a plain class with
  setters; and the three failures C2/C3/C9;
- **overrides**: `Column(t => t.Date, "Transaction Date")`; an override that still goes through the
  comparer (`"Transaction  Date"` in the header); C4/C5/C7/C8;
- **Ignore**: on a property (left at default); on a constructor parameter **with** a default (used)
  and **without** one (C6);
- **unsupported type** → C1, with `long`, `float` and a custom class, each naming the member and
  listing the supported set;
- **strictness**: M1 lists *all* unbound members and the captions, in one failure; an **extra**
  caption nothing binds to is **not** a failure; M2 for a member matching two columns;
- **per-cell**: M3 for a kind failure and for a conversion failure, each carrying the cell's A1 and
  the column caption, with the table's path;
- **immutability and concurrency**: one `TableRows<T>` mapped from 32 threads over 32 grids yields
  32 correct results; a `TableBinding<T>` method returns a new instance and the original is
  unchanged; **binding resolves once** — two `Map` calls over the same shape, asserted through
  behaviour rather than instrumentation;
- **defaults are the table's**: the same grid read by `TableRows<T>()` and `TableRows(lambda)`
  consumes the same extent and lands in the same place;
- **`init`-only binding works** — the mechanical claim of §4.3, pinned on a record with
  `{ get; init; }` members;
- **the overload probe**: all five `TableRows` spellings called in one file (a compile-time pin
  against `CS0121`).

### FieldsTests — new
- the block reads n rows × 2 columns and yields one entry per field, keyed by the declared labels,
  looked up through the comparer;
- **labels absorb the colon**: `Field("EIN")` matches a cell reading `"EIN:"`, and the key is
  `"EIN"`;
- values keep their kinds; a blank value cell is `Blank`, not a failure;
- **the anchor**: a block placed in the middle of a wide grid is found on both axes; `.After(…)`
  replaces the anchor; `.Down(1)` composes onto it; F3 when the label is nowhere;
- **F2**: a missing middle row fails with the label it expected and what the cell actually reads,
  and `Optional()` on the block absorbs it with one `Warning`;
- **F1** when the extent is forced to something other than 2×1;
- guards F4/F5/F6, and the `params` array is copied (mutating the caller's array afterwards does
  not change the shape);
- **inspection and naming**: `Description == "Fields"`, opaque with a `Reason`, each child renders
  `Field("EIN")#1`, a failure path reads `… -> 'entity' -> Field("Deal Type")#3`, and **no child is
  ever labelled with this helper's own identifiers** (the phase-B leak pin);
- **the K-1 mirror**: a synthetic band whose label column sits well to the right, with two labels
  ending in `':'` and three not — the exact shape of the real card — asserting the five keys and
  that the block consumed 2×5.

### ShapeExampleTests
- `SimpleReport_*` respelled: the header as a four-leaf flow and the table as
  `TableRows<Transaction>` with two overrides, asserting the **same values and the same extents**
  the current tests assert;
- `InvestorsByDeal_*` respelled onto `TableRows<DealTransaction>()` with **no overrides**;
- `InvestorIrr_*` respelled; `Assert.Empty(diagnostics)` must still hold;
- `InvestorSummary_*` **unchanged** apart from `investorName` — the discovered-header test keeps
  pinning a discovered header (§1.6);
- **new:** one synthetic grid read twice, by `TableRows(lambda)` and by `TableRows<T>()`, asserting
  identical values and identical consumed extents — the regression pin that the typed form changed
  the projection and nothing else.

### MethodGroupTests
Unchanged, and deliberately checked: **no member added by this phase takes an optional or
compiler-supplied parameter.** The leaves take none, `TableRows<T>()` takes none, `TableRows()`
takes none, `Fields` is `params` (which could not carry a `CallerArgumentExpression` anyway), and
`Field` takes one required string. `Map`/`Apply`/`MapWithDiagnostics` are untouched, so
`spaces.Select(report.Map)` keeps compiling.

---

## 11. Script expectations

Counted with `grep -o "\.\(Get\|TryGet\)\(String\|Int\|Double\|Decimal\|DateTime\|Date\|Boolean\|Error\)()"`
across `linqpad/*.linq` on 2026-09-02: **53** `CellValue` accessor calls and **27** by-name table
bindings. (The audit's "61" counted `Get*`/`Try*` more loosely, including `GetSubspace` and
`Path.GetDirectoryName`; the 27 agrees exactly.)

| Script | accessors before → after | by-name before → after | What changes |
|---|---|---|---|
| `simple-report.linq` | 8 → **0** | 4 → **0** | typed-leaf header; `TableRows<Transaction>` + 2 overrides |
| `investor-irr.linq` | 14 → **0** | 10 → **0** | typed-leaf header; `TableRows<SummaryRow>` + 1 override; `TableRows<CashFlow>` with none |
| `investors-by-deal.linq` | 7 → **0** | 6 → **0** | `Text()`; `TableRows<DealTransaction>()` with **no overrides** |
| `investor-summary.linq` | 11 → **10** | 7 → **7** | `Text()` only — the corpus keeps one worked lambda-form example |
| `scrubbed-k1.linq` | 8 → **7** | 2 → **0** | `Fields` for the entity card; the rest waits for the cross-tab |
| `edge-cases.linq` | 3 → **3** | 0 → 0 | unchanged, plus a new section showing kind-vs-conversion messages |
| `array.linq` | 2 → 2 | 0 → 0 | untouched (substrate) |
| **total** | **53 → 22** | **27 → 7** | |

Of the 22 remaining: 7 are `investor-summary`'s deliberately-preserved lambda tables, 7 are the K-1
cross-tab awaiting phase D, 3 are the adapter probe whose subject *is* accessors, 2 are the
substrate script, and 3 are `scrubbed-k1`'s `Code`/`Find` helpers (C7 and C3, both phase D).

**A note on LINQPad and named types.** `TableRows<T>` needs a named type, and a
`<Query Kind="Statements">` body is wrapped in a method. At respell time, first try declaring the
records after the statements (recent LINQPad versions accept trailing type declarations); if the
installed version rejects it, convert those three scripts to `<Query Kind="Program">` with
`void Main() { … }` and the records below. Mechanical either way, and it affects only
`simple-report`, `investor-irr` and `investors-by-deal`. **[decided here — verify at respell]**

### 11.1 `simple-report.linq`

Captions read from the workbook: `Client`, `Transaction Date`, `Transaction Type`, `Amount`.

```csharp
record Transaction(string Client, DateTime Date, string Type, decimal Amount);

var reportHeader = VerticalFlow(v => new {
  Title      = v.Next(Text()),
  SubTitle   = v.Next(Text()),
  ReportDate = v.Next(Date()),
  ReportId   = v.Next(Text()) });

var transactions = TableRows<Transaction>(bind => bind
  .Column(t => t.Date, "Transaction Date")
  .Column(t => t.Type, "Transaction Type"));
```

**Binding:** `Client` ✅ free, `Amount` ✅ free, `Date` and `Type` need overrides — *because the
consumer type chose shorter names*. Naming them `TransactionDate`/`TransactionType` would bind free;
the overrides are kept precisely so the corpus demonstrates the override spelling.

**Known-good:** the A1:A4 header (`Capital Activity Report`, `Q2 2026 - All Clients`, a date,
`RPT-00042`) and 8 transaction rows from A8:D16. The header flow consumes 1×4, exactly as
`Column(4, …)` did, so the table below still starts from the same cursor and
`SimpleReport_LandsOnTheExtentsTheSpecRecords` must pass unchanged. The result element type changes
from an anonymous type to `Transaction`.

### 11.2 `investor-irr.linq`

Summary captions: `Investors`, `Contribution ITD`, `Distribution ITD`, `Management Fee ITD`,
`End Balance`, `IRR`. Detail captions: `Investor Name`, `Date`, `Transaction`, `IRR`.

```csharp
record SummaryRow(string Investor, decimal ContributionItd, decimal DistributionItd,
                  decimal ManagementFeeItd, decimal EndBalance, double Irr);
record CashFlow(string InvestorName, DateTime Date, string Transaction, double Irr);

var summary       = TableRows<SummaryRow>(bind => bind.Column(r => r.Investor, "Investors"));
var investorBlock = TableRows<CashFlow>();
```

**Binding matrix, checked against the workbook:**

| Member | Caption | Free under the comparer? |
|---|---|---|
| `ContributionItd` | `Contribution ITD` | ✅ (`ContributionITD` ≡ `ContributionItd`) |
| `DistributionItd` | `Distribution ITD` | ✅ |
| `ManagementFeeItd` | `Management Fee ITD` | ✅ |
| `EndBalance` | `End Balance` | ✅ |
| `Irr` (summary) | `IRR` | ✅ |
| `Investor` | `Investors` | ❌ **one override** — the caption is plural |
| `InvestorName` | `Investor Name` | ✅ |
| `Date`, `Transaction` | `Date`, `Transaction` | ✅ |
| `Irr` (detail) | `IRR` | ✅ |

**Nine of ten bind free; the one override is a genuine plural/singular difference rather than a
comparer weakness.**

**Known-good:** three summary rows; three transfer-date blocks of 3/2/4 rows; three inception blocks
of 3/2/4; `SeriesAgreeWithSummary` true; and — the load-bearing one —
**`Assert.Empty(diagnostics)`**: the whole 6×45 sheet stays described. Reasoned: the header flow
consumes 1×4 where `Column(4, …)` consumed 1×4, and both `TableRows` forms are the same `TableShape`
with the same placement and a different projection, so **no extent anywhere in this script moves**.
The `Caption`/`Under`/`Until` composition from phase B is untouched.

### 11.3 `investor-summary.linq`

**Changes: one line.** `var investorName = Cell(c => c.GetString());` becomes
`var investorName = Text();`.

Everything else is left alone **on purpose**:

- the header is `Column(c => …)`, a **discovered** extent — §1.6's rule says a discovery is not
  traded for a child count, and `InvestorSummary_DiscoversItsHeaderHeight` goes on pinning it;
- the two tables keep their lambdas, so the corpus retains one worked example of the escape hatch
  this phase explicitly preserves. `edge-cases` cannot play that role (it has no table), and a
  vocabulary whose fallback spelling appears in no script is a fallback nobody will find.

**Known-good:** unchanged in every particular — three summary rows, three detail blocks of 3/2/4,
`summary rows == detail blocks` true, whole sheet consumed.

### 11.4 `investors-by-deal.linq`

Captions: `Account Key`, `Fund Code`, `Name`, `Transaction Type`, `Amount`, `Transfer Date`.

```csharp
record DealTransaction(string AccountKey, string FundCode, string Name,
                       string TransactionType, decimal Amount, DateTime TransferDate);

var dealCode     = Text();
var transactions = TableRows<DealTransaction>();
```

**All six bind free — zero overrides.** This is the script that shows the comparer earning its keep,
and it is why the comparer is whitespace-insensitive rather than exact.

**Known-good:** three deal blocks (`ATLAS-2024`, `HELIOS-2025`, `KESTREL-2025`) of 3/5/2
transaction rows, discovered per block; `InvestorsByDeal_DiscoversABlockLengthPerBlock` unchanged.
The `Repeat`'s separator, the per-block extent discovery and the flow arithmetic are untouched:
only the projection changes.

### 11.5 `scrubbed-k1.linq` (LOCAL-ONLY fixture; never committed)

```csharp
var entity = Fields(
  Field("EIN"), Field("Entity Type"), Field("Deal Type"),
  Field("State Sourced Income"), Field("Underlying CFC(s)/PFIC(s)"));
```

Checked against the local workbook (a direct read of the sheet XML, not a run):

- the label column is **J** and the five labels sit at **J2:J6**, with their values at **K2:K6**;
  **J7 is blank**, so the block's height was always "as many rows as have labels" and the
  hard-coded `5` was a coincidence of this export;
- **two labels carry a colon** (`EIN:`, `Entity Type:`) and **three do not** — which is exactly why
  the current script needs `TrimEnd(':')`, and why the label rule (§3) is scoped to `Field`;
- **`EIN` is unique within the anchor's search area.** The header overlay is bounded by
  `.Sized(RowsWhileAnyValue())`, which stops at the first entirely blank row — row 14 — so the seek
  scans rows 1–13, and in that band only J2 matches `EIN` or `EIN:` under the label rule. The
  self-anchor therefore lands on the same cell that
  `Then(To(ColumnContaining("EIN:")), To(RowContaining("EIN:")))` lands on today. It is also **more**
  drift-tolerant: an export that drops the colon breaks today's script and not the new one.

**Known-good, all of which must hold unchanged:**

| | expected after |
|---|---|
| diagnostics | `Info: the shape consumed 92 of 2772 rows; rows 93+ were not described` at **A93** |
| `k1Lines` rows / `portfolio` rows | 44 / 31 |
| coded rows after the `HasValue` filter | 74 |
| funds / Σ pct / federal line items / `AllAllocationsSumToFederal` | 15 / 0.9999999997 / 8 / true |

**Reasoned, not measured:** `Fields` consumes 2 columns × 5 rows starting at J2 — the identical
footprint to `Range(2, 5, …)` anchored at J2. The entity block is one child of an `Overlay`, whose
consumed extent is the bounding box of its children; the three full-width `FullRow` children reach
column BI and row 11, so the entity child was never the maximum on either axis. **Nothing in the
overlay's footprint moves, so the burn-down stays flat at 92 — for the same reason phase B's did.**

**Two deltas that are improvements, and must be stated so they are not read as regressions:**

1. The entity dictionary's **values** change from `CellValue.ToString()` strings
   (`"Text(US Flow-Through)"` — a diagnostic rendering that leaked into data, and a latent defect)
   to `CellValue`s. To keep the `Dump` byte-identical, add
   `.Select(f => f.ToDictionary(e => e.Key, e => e.Value.ToString()))` — but the honest reading is
   that the old output was wrong.
2. The **keys** are `EIN`, `Entity Type`, `Deal Type`, `State Sourced Income`,
   `Underlying CFC(s)/PFIC(s)` — **exactly today's `TrimEnd(':')` keys**, now produced by the
   vocabulary instead of by string surgery.

The `FullRow` helper, `Code`, `Find`, the fund-column resolution and the pivot all stay: they are
the cross-tab (**C3**) and belong to the next phase. This script's accessor count therefore drops by
only one — and that is the honest signal that C3, not C1, is what the K-1 campaign is waiting on.

### 11.6 `edge-cases.linq`

**Judgement: the dictionary form is not its spelling. [decided here]** `edge-cases.xlsx` has no
header row and no table — row 1 is one cell of each kind, row 2 is five error cells — so
`TableRows()` has nothing to key on. More to the point, the script exists to *display* kinds and
blankness, and typed leaves exist to *assert* them; replacing the accessors would hide the very
thing the probe is for. Its three accessor calls stay.

**One section is added** (section 5), because this is the corpus's natural home for the new
diagnostics and it costs four lines:

```csharp
// 5. Typed leaves speak the document's vocabulary: kinds for a kind mismatch, conversions for a
// number that will not fit. Note that the error cell is reported as an Error, never as "blank".
//   Decimal() over the #VALUE! cell  -> expected Number at A1, found Error(#VALUE!)
//   Integer() over the 3.14 cell     -> the Number at A1 (3.14) is not a whole number
```

---

## 12. Out of scope, and one rejection

**Out of scope, stated so it is not inferred from what is here:**

- **The cross-tab / `Matrix`** (audit C3, second half) — the K-1's real structure: line items keyed
  down the ATAX column, funds keyed across the header band, amounts at the intersections. It needs
  the binding notion this phase establishes *and* an answer to "one region discovers an axis that
  another region consumes". Its own sketch comes next; nothing here forecloses it, and `Fields` and
  `TableRows<T>` are the two halves of the key vocabulary it will reuse.
- **`TableColumns` / the transposed table** (audit C3, first half).
- **Multi-row header bands** — `Table` still validates `headerRows` as 0 or 1.
- **Any leaf beyond the accessor set** — §2, permanently.
- **A typed `Field(label, leaf)`** — §14, deferred with its reasons.

**Rejected: attribute-based caption mapping.** `[Column("Transaction Date")] public DateTime Date`
is the spelling everyone reaches for, and it is on the wrong side of the audit's (d) boundary:

- **It puts document knowledge on the consumer's type.** `Transaction` describes the *import
  target*; the caption `"Transaction Date"` describes *one vendor's spreadsheet*. Attributes weld
  them together, and the moment a second vendor emits the same data under different captions the
  type has to fork.
- **It takes the declaration out of the declaration.** A shape is a value: built at runtime,
  composed, inspected, reused, and — the whole point of the audit — potentially executed backwards.
  An attribute is compile-time metadata on somebody else's type, invisible at the site that says
  what the report looks like, and unavailable to a `Choice` between two layouts.
- **It cannot be overridden per use.** The bind lambda is per-shape, so two shapes over the same
  type can disagree about captions — which is exactly the "one shape declared once, placed twice"
  idiom the corpus already relies on.

Recorded here so the question is answered rather than re-opened.

---

## 13. Decisions taken here, beyond the brief

1. **The leaves are methods, not properties** (§1.1).
2. **`Date()` yields `GetDateTime()` verbatim**, not the truncated date, and the firewall mirror is
   stated over kinds and conversions rather than method names (§1.2).
3. **Blank is reported as a kind** (`found Blank`), with no special case (§1.4).
4. **Error cells render through `CellValue.ToString()`** — `found Error(#DIV/0!)` — so nothing is
   duplicated from Core and nothing is added to it (§1.4).
5. **Per-cell messages carry no remedial advice; declaration failures do** (§1.4).
6. **`at {A1}` stays in the kind/conversion template even when the location line repeats it**,
   because leaves and table columns share one template (§1.4).
7. **Numbers in messages are formatted invariantly** (§1.3); Core's culture-sensitive accessor
   messages are recorded as a pre-existing inconsistency, not fixed here.
8. **The flow-of-leaves idiom replaces an explicit-count strip, never a discovered one** — which is
   why `investor-summary`'s header is left alone (§1.6, §11.3).
9. **A third matching rule is added, narrowly**: `CellMatching.LabelEquals`, colon-only, used by
   `Field` alone; the three rules are tabulated rather than unified (§3).
10. **`CaptionComparer` is public**, because we hand back dictionaries built with it (§3).
11. **The comparer strips whitespace and case only** — not punctuation (§3).
12. **`CellValue` is a supported member type** in `TableRows<T>` — the one addition to the brief's
    closed set, argued from the all-or-nothing cliff and from C7 (§4.2).
13. **`Nullable<>` means blank-tolerant, not kind-tolerant** (§4.2).
14. **Construction resolution order**: sole parameterized constructor with no parameterless one →
    constructor binding; else parameterless + settable/`init` properties; else a loud error (§4.3).
15. **Nullable *reference* annotations are read from metadata by name**, with oblivious ⇒ strict
    (§4.7).
16. **Strictness is one-directional**: an unclaimed caption is not a failure (§4.5).
17. **The opt-out is `Ignore(t => t.X)`, not a flag**; on a constructor parameter it requires a
    default value (§4.5).
18. **Member selectors are properties only**, direct access only (§4.6).
19. **No `headerRows` overload** for the two new table forms (§4.1).
20. **`Description` carries the type argument**: `TableRows<Transaction>` (§4.1).
21. **`Field` is a declaration value, not a shape**, so `Fields` can key its result (§6.1).
22. **`Fields` self-anchors on its first label, on both axes** — the `Caption` precedent, and the
    thing that removes the duplicated anchor literal from the K-1 script (§6.3). Beyond the brief,
    and the single most reversible decision here: dropping it costs one `.After(…)` per use site.
23. **An uncaptioned column fails in the dictionary form and is harmless in the typed form** (§5).
24. **Construction-time binding errors are `ArgumentException`**, with `paramName` only when the
    fault is in the bind lambda (§7).
25. **`edge-cases.linq` keeps its accessors** and gains a diagnostics section instead (§11.6).
26. **Scripts adopting `TableRows<T>` may need `Kind="Program"`** — try trailing type declarations
    first (§11).

---

## 14. Deferred, with names reserved

- **`Field<T>(string label, IShape<T> value)`** — the typed labelled pair
  (`v.Next(Field("EIN", Text()))` inside a `VerticalFlow`). It composes cleanly — a horizontal flow
  of a label cell and a leaf — but it has nowhere to go *inside* `Fields`, whose result is one
  dictionary of `CellValue`; as a standalone it would be a second `Field` overload returning a
  different kind of thing. No file needs it (the K-1 card is heterogeneous and is dumped, so the
  dictionary is exactly right). Reserved together with a public cell-level **`Label(text)`** leaf,
  which is what it would be built from.
- **Per-field tolerance** — `Field(label).Optional()` needs `Field` to be a shape (§6.2).
- **A wider or offset value region** in a field (`Field(label, width)`), and a label column that is
  not adjacent to its values.
- **Trailing punctuation beyond `':'`** in the label rule (§3).
- **Punctuation-insensitive caption binding** (`"Net (USD)"` ↔ `NetUsd`) (§3).
- **An `Info` naming captions no member bound** — the reverse-strictness observability item; the
  risk is noise on wide sheets, so it waits for the decomposition trace to give it a home (§4.5).
- **A lenient duplicate-caption policy** for the dictionary form (§5).
- **`Error()`** as a leaf, and `Cell(c => c.TryGetError())` until then. An error cell is a failure
  in every declaration the corpus contains; the day one is *expected*, the leaf is one line and the
  firewall permits it (`GetError` is in the accessor set).
- **Blank-tolerant leaves** (`Text().OrBlank()`), so a leaf can yield `null` for a blank without
  `Optional()`'s `Warning`. The table binder gets this for free through `Nullable<>`; the leaf
  spelling waits for a file.
- **A `Repeat`-friendly `Fields` anchor recipe** — the same shape as phase B §3.4, unexercised.
- **Culture-invariant messages in `CellValue`** — Core's own accessor messages interpolate numbers
  culture-sensitively (§1.3). A one-line fix in Core, out of scope here because this phase adds
  nothing to Core.
