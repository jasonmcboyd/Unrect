# The value facet — call-site sketches

A design record for a live arc (branch `feature/value-facet`). It is deleted when the arc lands and
CLAUDE.md carries what stands. **Status:** steps 1 to 3 of the order below are built (the bare point, with
`CanonicalReads` in Core; `IValueSpace<TValue>`; `CellValue`; `ICellSpace : IValueSpace<CellValue>` with the
reads derived in `PointReads`; `ISpace` the text facet alone, held text and its matchers in the
value vocabulary, `Caption`/`Field`/`Heading` matching what a cell says); steps 4 and 5 are not. The broad strokes are the owner's
(2026-09-21/23); the details are there to be quibbled with.

## The model

A point is an ADDRESS. What is at it is seen through facets, and a space says which it has by the
interfaces it implements:

| Facet | Interface | Required | Shape |
|---|---|---|---|
| text representation | `ISpace` | of every space | is there anything; what it says |
| value | `IValueSpace<TValue>` | optional | ONE of several kinds — the alternatives are mutually exclusive |
| formula | `IFormulaSpace` | optional | a second string, or none |
| style | `IStyleSpace` | optional | a record: font AND fill AND … |

The text facet is the universal one because a space can have it and nothing else (a plain CSV: every
field is text), and nothing has a typed value without also being able to say it. In a sheet the
text is rendered FROM the value; in a CSV with inference the value is inferred FROM the text. Either
way they are two facets. Blank means "says nothing" — a blank cell may still have a formula or a
fill (the local K-1: 192 cells with a formula and no value, 32,989 styled empty cells).

## 1. `ISpace` — the text facet, and nothing about kind

```csharp
public interface ISpace
{
    Area Area { get; }
    bool IsBlank(int column, int row);      // each space decides what blank means; the adapter is told the rule
    string? AsText(int column, int row);    // null exactly where IsBlank
}
```

Three members. `TryGetTextAt` — "is this text the cell's OWN" — is a question about the value's
kind, and leaves with `IsText`. A text-only space never has to answer it.

## 2. `IValueSpace<TValue>` — the value facet

```csharp
public interface IValueSpace<out TValue> : ISpace     // today's IValueSpace<T>, renamed
{
    TValue ValueAt(int column, int row);
}

public interface ICellSpace : IValueSpace<CellValue> { }   // an implementer writes ValueAt. That is all.
```

`GridSpace<int>` is an `IValueSpace<int>`, as now. A spreadsheet's value is a sum type, and because
its alternatives partition the cell, a tag on it — and a switch over the tag — is legitimate, where
a switch over a POINT never was:

```csharp
public readonly struct CellValue : IEquatable<CellValue>      // today's Cell, designed on purpose
{
    public CellKind Kind { get; }                    // Blank | Text | Number | Date | Boolean | Error

    public bool TryGetText(out string text);
    public bool TryGetNumber(out double number);     // a workbook's numbers are doubles; nothing else is kept
    public bool TryGetDate(out DateTime date);
    public bool TryGetBoolean(out bool flag);
    public bool TryGetError(out CellError error);

    public static CellValue Blank { get; }
    public static CellValue Of(string text);         // Of(double), Of(DateTime), Of(bool), OfError(...)
}
```

At a call site nothing a script writes today changes — the reads are derived once, over the value:

```csharp
r["Amount"].Decimal()                                // = Value().TryGetNumber, then the conversion
RowsWhileAny(p => p.IsDouble())
p.Value().Kind switch { CellKind.Text => …, CellKind.Number => …, _ => … }     // NEW, and honest
```

`TryGetDoubleAt`/`TryGetDateTimeAt`/`TryGetBooleanAt`/`TryGetErrorAt` leave the space's contract:
they are what `CellSpaceBase` derives from its private `CellAt` today, made the rule. Every failure
sentence stays as it is (`expected Number at B4, found Text` is worded from `Kind`, as now).

A wrinkle to know: C# does not infer a type argument from a constraint, so there is no single
generic `p.Value()`. The grid keeps `Value<T>(this Point<IValueSpace<T>>)`; the cell space declares
its own `Value<TSpace>(this Point<TSpace>) where TSpace : class, ICellSpace`. They cannot collide —
a `Point<ICellSpace>` is not a `Point<IValueSpace<CellValue>>`.

## 3. `Point` — a bare address

```csharp
public readonly struct Point<TSpace>      // Space, Column, Row, equality, Erased(). No questions.
```

Every question is an extension, in one place per facet, spelled one way:

```csharp
p.IsBlank()   p.HasValue()   p.AsText()                       // over ISpace        (Core)
p.Value()  p.IsText()  p.Text()  p.IsDouble()  p.Double()  …   // over ICellSpace    (the cell package)
p.HasFormula()  p.Formula()                                   // over IFormulaSpace
p.Font()  p.Fill()                                            // over IStyleSpace
```

Measured: about 175 property reads become method calls (`.IsBlank` 53, `.IsText` 44, `.HasValue`
79 — some of those last are `Nullable<T>`'s). Mechanical.

## 4. Matching: what a cell SAYS is generic; what it HOLDS is the value space's

```csharp
RowSaying("Total")        // ProjectionBuilders<TSpace>      — any space; exists today
RowContaining("Total")    // the value space's vocabulary    — held text: a numeric 2024 is not "2024"
```

A spreadsheet file imports both vocabularies, so it has both names, as it does now. A text-only
space has `Saying` alone, where the two mean the same thing. The same split for
`ColumnContaining` and `TakeRows/ColumnsToText`.

**The behaviour change to weigh.** `Caption`, `Field`/`Fields` and `Heading` are core leaves and
match held text today. Staying in the core they match what a cell SAYS — `Heading("2024")` also
finds a numeric 2024 — which is the rule already made for table headers on 2026-09-20 (a label is
what a header cell says, whatever its kind). In use: `Heading` 119, `Caption` 77, `Field` 55,
`RowContaining` 176, `ColumnContaining` 46. Of the 36 files that use `…Containing`, 11 import no
spreadsheet vocabulary and would move to `…Saying`.

## 5. Fixtures and the blankness rule

```csharp
SheetGrid.Of(new object?[,]
{
    { "Client", "Amount" },
    { "Acme",   12.5 },
    { "Bad",    CellError.DivisionByZero },          // an error without naming the value type
});

SpreadsheetSpace.Create(path, "Sheet", isBlank: text => string.IsNullOrWhiteSpace(text));   // the default
SpreadsheetSpace.Create(path, "Sheet", isBlank: _ => false);                                // strict — spelled as today
CsvSpace.Open(path, isBlank: text => text.Length == 0 || text == "<NULL>");                 // if it ever exists
```

The rule is a `Func<string, bool>` over the TEXT a cell holds: every rule anyone has written is one
(`_ => false` seven times, one test blanking "x", and the built-in whitespace rule). In a sheet it is
asked of text cells only — a number, a date, a boolean and an error are never blank — so nothing is
rendered at load to ask it.

## What this unwinds from alpha.9

`TryGetTextAt` off `ISpace`; `Point.IsText`/`TryGetText` off the struct; the core `Text()` leaf and
`p.Text()` back to the cell vocabulary; `CellProblem` back up out of Core (no Core contract speaks
it any more — the charter's own test); `GridSpace.Create(..., isText:)` loses the parameter (5
uses). What stands: `CellProblem` as a struct, the store-faithful reads, the derived `Is…` forms,
`TryGetFormulaAt`, the released-row fault.

## Going with it

The kept exact decimal (`Cell.Of(1.50m)` saying "1.50") — no workbook produces one; a number is a
double and says "1.5". The test-only `SpaceQuestions.KindAt`/`Describe` — the value has a real
`Kind` again.

## Quibble list

- Names: `CellValue` (or keep `Cell`)? `CellKind.Temporal` → `Date`? `IValueSpace` vs `ICellSpace`
  — is the cell space just `IValueSpace<CellValue>` with no name of its own?
- `IsBlank(c, r)`/`AsText(c, r)` break the `…At` convention `ValueAt` follows. `IsBlankAt`/`AsTextAt`?
- `HasValue()` on a point: at the `ISpace` level "value" is now another facet's word. Keep
  `IsBlank()` alone?
- Where the `ISpace` extensions live. Core has a precedent (`Scans`), and `Unrect.Strategies` needs them.
- A date is a double with a date format, and the partition is our lexer's: the number read refuses
  a date. Deliberate; say so in `CellKind`'s own doc.
- A second backend with the same kinds needs `CellValue` and its reads outside the Excel package
  (`Unrect.Cells`). Not before one is real.

## Order, when it is built

1. `Point` bare; the `ISpace` questions as extensions (mechanical, no behaviour change).
2. `CellValue` designed; `IValueSpace<TValue>`; `ICellSpace : IValueSpace<CellValue>`; reads derived.
3. `TryGetTextAt` off `ISpace`; `Text()` and the held-text matchers to the value vocabulary;
   `Caption`/`Field`/`Heading` match what a cell says — the one behaviour change, its own commit.
4. The blankness rule over text; `CellError` in a literal fixture.
5. Docs, scripts, release notes.
