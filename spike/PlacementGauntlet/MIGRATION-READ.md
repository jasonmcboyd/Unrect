# The migration read (placement gauntlet, scenario 7)

One script, both spellings, side by side and **unannotated** — the cold read the verdict turns on.
No commentary here on purpose: the question is whether a reader who knows today's vocabulary can
read tomorrow's unaided. The judgment belongs in `docs/design/placement-gauntlet-spec.md` §6.

Both columns are compiled and executed in `Scenario1_Examples.cs` and asserted to read the same
values from `examples/investor-irr.xlsx`.

---

# The geography law

> An operator sits on the side of its subject where its referent sits on the sheet. What is above
> spells **before** the subject — anchors, offsets, a section's captions. What is below spells
> **after** it — a bound's landmark.

So `Under` becomes an entry and `.Until` stays postfix. The three acceptance reads below are
compiled and executed in `ScenarioG_Geography.cs`; each is verified at L2 (value and geometry) and
at L3 (diagnostics verbatim, and a provoked failure's path, subject and location).

### Read 1 — the bounded series

```csharp
// today
irrDetails
    .Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date"))
    .Until(RowContaining(Inception));

// under the geography law
Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date"))
    .Of(irrDetails)
    .Until(RowContaining(Inception));
```

### Read 2 — the unbounded series

```csharp
// today
irrDetails.Under(Caption(Inception));

// under the geography law
Under(Caption(Inception)).Of(irrDetails);
```

### Read 3 — a K-1 section: anchor prefix, content, bound postfix

```csharp
// today
VerticalFlow(v => new KSection(
        Caption: v.Next(Caption("K-1 Lines 1-21")),
        Lines:   v.Next(kLines)))
    .On(RowContaining("K-1 Lines 1-21"))
    .Until(RowContaining("Portfolio Income"));

// under the geography law
On(RowContaining("K-1 Lines 1-21"))
    .VerticalFlow(v => new KSection(
        Caption: v.Next(Caption("K-1 Lines 1-21")),
        Lines:   v.Next(kLines)))
    .Until(RowContaining("Portfolio Income"));
```

### The three words together, and the repeat-stop recipe

```csharp
// today
kLines.Under(Caption("K-1 Lines 1-21")).On(RowContaining("K-1 Lines 1-21")).Until(RowContaining("Portfolio Income"));
lines.Under(regionName).On(regionMark);

// under the geography law — every word in sheet order
On(RowContaining("K-1 Lines 1-21")).Under(Caption("K-1 Lines 1-21")).Of(kLines).Until(RowContaining("Portfolio Income"));
On(regionMark).Under(regionName).Of(lines);
```

---

## `linqpad/investor-irr.linq`

> **Superseded below the geography law** — this section is the first trial's record, when `.Until`
> was a pipeline stage. Read 1 above is the current spelling of the same declaration.

### Today

```csharp
var reportHeader = VerticalFlow(v => new
{
    Title      = v.Next(Text()),
    Fund       = v.Next(Text()),
    ReportDate = v.Next(Date()),
    ReportId   = v.Next(Text()),
});

var summary = Table<SummaryRow>(bind => bind.Column(r => r.Investor, "Investors"));

var investorBlock = Table<CashFlow>();

const string Inception = "Cash Flows using inception date";

var irrDetails = VerticalRepeat(investorBlock, separatedBy: BlankRows());

var byTransferDate = irrDetails
    .Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date"))
    .Until(RowContaining(Inception));

var byInception = irrDetails.Under(Caption(Inception));

var report = VerticalFlow(v => new
{
    ReportHeader   = v.Next(reportHeader),
    Summary        = v.Next(summary),
    ByTransferDate = v.Next(byTransferDate),
    ByInception    = v.Next(byInception),
});

var mapped = report.MapWithDiagnostics(SpreadsheetSpace.Create(path, "IRR"));
```

### Under the inverted pipeline

```csharp
var reportHeader = VerticalFlow(v => new
{
    Title      = v.Next(Text()),
    Fund       = v.Next(Text()),
    ReportDate = v.Next(Date()),
    ReportId   = v.Next(Text()),
});

var summary = Table<SummaryRow>(bind => bind.Column(r => r.Investor, "Investors"));

var investorBlock = Table<CashFlow>();

const string Inception = "Cash Flows using inception date";

var irrDetails = VerticalRepeat(investorBlock, separatedBy: BlankRows());

var byTransferDate = Until(RowContaining(Inception))
    .Of(irrDetails.Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date")));

var byInception = irrDetails.Under(Caption(Inception));

var report = VerticalFlow(v => new
{
    ReportHeader   = v.Next(reportHeader),
    Summary        = v.Next(summary),
    ByTransferDate = v.Next(byTransferDate),
    ByInception    = v.Next(byInception),
});

var mapped = report.MapWithDiagnostics(SpreadsheetSpace.Create(path, "IRR"));
```

---

## `linqpad/investor-summary.linq`, the details section

### Today

```csharp
var details = VerticalRepeat(investorDetail, separatedBy: BlankRows(), atLeast: 1).AfterBlankRows();

var report = VerticalFlow(v => new
{
    ReportHeader = v.Next(reportHeader),
    Summary      = v.Next(summary),
    Details      = v.Next(details),
});
```

### Under the inverted pipeline

```csharp
var details = AfterBlankRows().VerticalRepeat(investorDetail, separatedBy: BlankRows(), atLeast: 1);

var report = VerticalFlow(v => new
{
    ReportHeader = v.Next(reportHeader),
    Summary      = v.Next(summary),
    Details      = v.Next(details),
});
```

---

## A K-1-style section (`Scenario5_NestedSection.cs`)

> The `.Until` stage in the right-hand column is superseded; read 3 above is the current spelling.

### Today

```csharp
var kLines = Table(headerRows: 1, eachRow: captions => Overlay(o => new KLine(
    Code:   o.Next(Text().Right(captions["Line"])),
    Amount: o.Next(Decimal().Right(captions["Amount"])))));

var lines = VerticalFlow(v => new KSection(
        Caption: v.Next(Caption("K-1 Lines 1-21")),
        Lines:   v.Next(kLines)))
    .On(RowContaining("K-1 Lines 1-21"))
    .Until(RowContaining("Portfolio Income"));

var portfolio = VerticalFlow(v => new KSection(
        Caption: v.Next(Caption("Portfolio Income")),
        Lines:   v.Next(kLines)))
    .On(RowContaining("Portfolio Income"))
    .Until(RowContaining("Totals"));
```

### Under the inverted pipeline

```csharp
var kLines = Table(headerRows: 1, eachRow: captions => Overlay(o => new KLine(
    Code:   o.Next(Right(captions["Line"]).Text()),
    Amount: o.Next(Right(captions["Amount"]).Decimal()))));

var lines = On(RowContaining("K-1 Lines 1-21")).Until(RowContaining("Portfolio Income"))
    .VerticalFlow(v => new KSection(
        Caption: v.Next(Caption("K-1 Lines 1-21")),
        Lines:   v.Next(kLines)));

var portfolio = On(RowContaining("Portfolio Income")).Until(RowContaining("Totals"))
    .VerticalFlow(v => new KSection(
        Caption: v.Next(Caption("Portfolio Income")),
        Lines:   v.Next(kLines)));
```

---

## The scoped ledger (`Scenario3_ScopedSketch.cs`)

### Today

```csharp
var q = Projection.Over<ISpreadsheetSpace>();

var line = q.Overlay(o => new AuditedLine(
    Item:    o.Next(Text()),
    Qty:     o.Next(Integer().Right(1)),
    Total:   o.Next(Double().Right(3)),
    Formula: o.Next(Formula().Right(3))));

var ledger = q.VerticalFlow(v => new AuditedLedger(
    Lines:        v.Next(q.Table(headerRows: 1, eachRow: line)),
    TotalFormula: v.Next(Formula().On(RowContaining("Total")).Right(3))));
```

### Under the inverted pipeline

```csharp
var p = Place.Over<ISpreadsheetSpace>();

var line = p.Offset().Overlay(o => new AuditedLine(
    Item:    o.Next(Text()),
    Qty:     o.Next(Right(1).Integer()),
    Total:   o.Next(Right(3).Double()),
    Formula: o.Next(Right(3).Of(Formula()))));

var ledger = p.Offset().VerticalFlow(v => new AuditedLedger(
    Lines:        v.Next(p.Offset().Table(headerRows: 1, eachRow: line)),
    TotalFormula: v.Next(On(RowContaining("Total")).Right(3).Of(Formula()))));
```

---

# Entry C — the zero-prefix file

> `using static ProjectionBuilders<ISpreadsheetSpace>;` — a closed generic static class imported
> once, closing `TSpace` at the top of the file, where C# already puts file-level bindings.

The reads below are **whole files**, because that is the unit Entry C scopes. They are compiled in
`ScenarioC_Audited.cs` and `ScenarioC_Plain.cs`, and differentially verified against today's
spellings in `ScenarioC_Differential.cs` at L2 (value and geometry) and L3 (diagnostics verbatim,
plus a provoked failure's path, subject and location). All agree.

**Read the using block first — it is the whole design.** Then read down and count the prefixes.

### Read 1 — the audited `investor-irr`, entire

Audited: every summary row carries the formula behind its amount as well as the amount, so the
declaration demands `ISpreadsheetSpace`. The demand is answered once, in line 6. The file is quoted
entire, less the two helper-trap members shown under boundary (c) below.

```csharp
using System;

using Unrect.Projections;
using Unrect.Spreadsheets;

using static PlacementGauntlet.Staged.ProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;
using static Unrect.Spreadsheets.SpreadsheetProjections;

namespace PlacementGauntlet
{
  public static class ScenarioCAudited
  {
    public const string Inception = "Cash Flows using inception date";

    public static IProjection<ISpreadsheetSpace, AuditedIrrReport> Report { get; } = Declare();

    private static IProjection<ISpreadsheetSpace, AuditedIrrReport> Declare()
    {
      var reportHeader = VerticalFlow(v => new IrrHeader(
        Title:      v.Next(Text()),
        Fund:       v.Next(Text()),
        ReportDate: v.Next(Date()),
        ReportId:   v.Next(Text())));

      Func<CaptionMap, IProjection<ISpreadsheetSpace, AuditedSummaryRow>> auditedRow = captions => Overlay(o => new AuditedSummaryRow(
        Investor:      o.Next(Right(captions["Investors"]).Text()),
        EndBalance:    o.Next(Right(captions["End Balance"]).Decimal()),
        AmountFormula: o.Next(Right(captions["End Balance"]).Of(Formula()))));

      var summary = Table(headerRows: 1, eachRow: auditedRow);

      var investorBlock = Table<CashFlow>();
      var irrDetails    = VerticalRepeat(investorBlock, separatedBy: BlankRows());

      var byTransferDate = Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date"))
        .Of(irrDetails)
        .Until(RowContaining(Inception));

      var byInception = Under(Caption(Inception)).Of(irrDetails);

      return VerticalFlow(v => new AuditedIrrReport(
        Header:         v.Next(reportHeader),
        Summary:        v.Next(summary),
        ByTransferDate: v.Next(byTransferDate),
        ByInception:    v.Next(byInception)));
    }
  }
}
```

The same document today (Entry B), for the count:

```csharp
var q = Projection.Over<ISpreadsheetSpace>();

var reportHeader = q.VerticalFlow(v => new IrrHeader(…));

Func<CaptionMap, IProjection<ISpreadsheetSpace, AuditedSummaryRow>> auditedRow = captions => q.Overlay(o => new AuditedSummaryRow(
    Investor:      o.Next(Text().Right(captions["Investors"])),
    EndBalance:    o.Next(Decimal().Right(captions["End Balance"])),
    AmountFormula: o.Next(Formula().Right(captions["End Balance"]))));

var summary = q.Table(headerRows: 1, eachRow: auditedRow);
…
var today = q.VerticalFlow(v => new AuditedIrrReport(…));
```

**The import blocks are the same size.** Entry B's file needs
`using Unrect.Projections; using Unrect.Spreadsheets; using static Unrect.Projections.Projection;
using static Unrect.Spreadsheets.SpreadsheetProjections;` — four lines, exactly what read 1 carries.
Entry C spends the same line differently: `Projection` becomes
`ProjectionBuilders<ISpreadsheetSpace>`, and what that buys is the deletion of the `var q = …` line
and of four `q.` prefixes in this declaration alone. **Entry C costs nothing at the top of the file;
it is not an extra import, it is a different one.**

### Read 2 — the plain twin, and read 3 — the K-1 pair: one file, one line shorter

The dichotomy theorem's corollary is that the plain vocabulary is Entry C at its floor. Measured:
the same file scoped to `ISpace` loses exactly one using line, and nothing else changes. (Quoted
less the differential's failure probe and the helper-trap twin, both shown below.)

```csharp
using System;

using Unrect.Core;
using Unrect.Projections;

using static PlacementGauntlet.Staged.ProjectionBuilders<Unrect.Core.ISpace>;

namespace PlacementGauntlet
{
  public static class ScenarioCPlain
  {
    public const string KLines    = "K-1 Lines 1-21";
    public const string Portfolio = "Portfolio Income";
    public const string Totals    = "Totals";

    public static IProjection<ISpace, PlainIrrReport> Report { get; } = DeclareReport();
    public static IProjection<ISpace, K1Report>       K1     { get; } = DeclareK1();

    private static IProjection<ISpace, PlainIrrReport> DeclareReport()
    {
      var reportHeader = VerticalFlow(v => new IrrHeader(
        Title:      v.Next(Text()),
        Fund:       v.Next(Text()),
        ReportDate: v.Next(Date()),
        ReportId:   v.Next(Text())));

      Func<CaptionMap, IProjection<ISpace, PlainSummaryRow>> plainRow = captions => Overlay(o => new PlainSummaryRow(
        Investor:   o.Next(Right(captions["Investors"]).Text()),
        EndBalance: o.Next(Right(captions["End Balance"]).Decimal())));

      var summary = Table(headerRows: 1, eachRow: plainRow);

      var investorBlock = Table<CashFlow>();
      var irrDetails    = VerticalRepeat(investorBlock, separatedBy: BlankRows());

      var byTransferDate = Under(Caption("IRR Details"), Caption("Cash Flows Using Transfer Date"))
        .Of(irrDetails)
        .Until(RowContaining(ScenarioCAudited.Inception));

      var byInception = Under(Caption(ScenarioCAudited.Inception)).Of(irrDetails);

      return VerticalFlow(v => new PlainIrrReport(
        Header:         v.Next(reportHeader),
        Summary:        v.Next(summary),
        ByTransferDate: v.Next(byTransferDate),
        ByInception:    v.Next(byInception)));
    }

    // Read 3 — the K-1 pair. Anchor prefix, content, bound postfix; every word in sheet order.
    private static IProjection<ISpace, K1Report> DeclareK1()
    {
      Func<CaptionMap, IProjection<ISpace, KLine>> kLine = captions => Overlay(o => new KLine(
        Code:   o.Next(Right(captions["Line"]).Text()),
        Amount: o.Next(Right(captions["Amount"]).Decimal())));

      var kLines = Table(headerRows: 1, eachRow: kLine);

      var lines = On(RowContaining(KLines))
        .VerticalFlow(v => new KSection(
          Caption: v.Next(Caption(KLines)),
          Lines:   v.Next(kLines)))
        .Until(RowContaining(Portfolio));

      var portfolio = On(RowContaining(Portfolio))
        .VerticalFlow(v => new KSection(
          Caption: v.Next(Caption(Portfolio)),
          Lines:   v.Next(kLines)))
        .Until(RowContaining(Totals));

      var title = Text();

      return VerticalFlow(v => new K1Report(
        Title:     v.Next(title),
        Lines:     v.Next(lines),
        Portfolio: v.Next(portfolio)));
    }
  }
}
```

The K-1 sections are in the **plain** file deliberately: they read text and decimals, and §5.5's
guidance is to scope a file to what its declarations READ, not to what it parses.

### What the using block actually costs

`using System;` is free in any project with `ImplicitUsings` (the spike turns it off, so it is
written). What remains is the honest floor:

| Line | Why it is there | Removable? |
|---|---|---|
| `using Unrect.Projections;` | the POSTFIX half — `.Until`, `.Named`, `.Optional`, `.OrBlank`, `.Select` | **No.** CS1106: extension methods cannot live in a generic static class |
| `using Unrect.Spreadsheets;` | names the space, opens the door | only by fully qualifying it in line 3 |
| `using static …ProjectionBuilders<ISpreadsheetSpace>;` | the whole prefix vocabulary, space answered | it is the design |
| `using static …SpreadsheetProjections;` | `Formula()`, `RowWithFormula()` | only if the file reads no capability |

Four lines for a spreadsheet declaration that reads a capability, three for a plain one (read 2
drops the last row and swaps `Unrect.Spreadsheets` for `Unrect.Core`). **"Zero prefixes" is not "one
import"** — but it is not *more* imports either, since Entry A and Entry B need the same four. The
split is not arbitrary: under the geography law, what spells *before* the subject is imported from
the closed class, and what spells *after* it arrives as an extension.

### The four boundaries, probed

Verbatim compiler output, collected by two builds (`MustNotCompileEntryC.cs` explains why two:
a declaration error suppresses body binding for the whole compilation).

**(a) Two closed imports in one file** — refused, but *not* as §5.5 predicted:

```
CS0121: The call is ambiguous between the following methods or properties:
        'ProjectionBuilders<TSpace>.Text()' and 'ProjectionBuilders<TSpace>.Text()'
```

Three corrections to the stub: it is **CS0121, not CS0104** (members of a closed static class are
methods, and method groups merge rather than collide); it fires **per call, not per file** (a file
importing two scopes and naming nothing they share compiles clean); and **the message cannot be
read** — Roslyn renders both candidates with the type *parameter's* name, so the reader is told a
call is ambiguous between a method and itself. The refusal is real; the diagnostic is unusable.

**(b) Entry C beside `using static Projection`** — refused, and here the message is fine, because
the two candidates have different type names:

```
CS0121: The call is ambiguous between the following methods or properties:
        'Unrect.Projections.Projection.Text()' and 'PlacementGauntlet.Staged.ProjectionBuilders<TSpace>.Text()'
```

This is the completeness obligation, enforced: the closed class must re-export the *whole*
vocabulary, because a file cannot supplement it. Full qualification still works —
`Unrect.Projections.Projection.Text()` compiles — which is the deliberately-effortful fallback the
dichotomy theorem asks for.

**(c) The helper trap** — compiles, over-demanding, exactly as §5.5 said. The same body in two
files:

```csharp
// identical source in both files
Under(Caption(caption)).Of(Table<Line>())

// inferred in the audited file:   IProjection<ISpreadsheetSpace, IReadOnlyList<Line>>
// inferred in the plain file:     IProjection<ISpace,            IReadOnlyList<Line>>
```

Nothing warns, and nothing should: the file said what it was for. What the arm adds is where the
consequence lands — applying the over-demanding helper to a space that cannot answer:

```
CS0411: The type arguments for method
        'ProjectionExtensions.Map<TSpace, TResult>(IProjection<TSpace, TResult>, TSpace)'
        cannot be inferred from the usage. Try specifying the type arguments explicitly.
```

**The worst message in the taxonomy** — it names neither space. Stating the type arguments turns it
into the sentence a reader needs (`CS1503: cannot convert from 'ISpace' to 'ISpreadsheetSpace'`),
which is the fix nobody knows to try. So the trap is silent where the mistake is made *and*
inarticulate where it is caught.

**(d) The partial-class edge** — compiles. One class, two files, two scopes; the same body in each
part infers a different demand:

```csharp
// ScenarioC_PartialAudited.cs  ->  IProjection<ISpreadsheetSpace, Line>
// ScenarioC_PartialPlain.cs    ->  IProjection<ISpace,            Line>
Overlay(o => new Line(Fund: o.Next(Text()), Amount: o.Next(Right(1).Decimal())))
```

A `using static` binds a **file**, not a type, so a partial class is no counter-example to the
dichotomy theorem — it is two files, and the theorem is about files. What it costs is the theorem's
own remedy: a reader who opens the *type* sees two demands on one class and no using block to
explain either.

### The split-type trick, measured

`Table.Of<Person>()` binds — nested types do come through a `using static` of a closed generic. So
does `Table<Person>()`. **Entry C dissolves the problem the trick was invented for**: the
all-or-none inference wall is about a *method's* type arguments, and Entry C's space is a *class's*,
already closed by the using directive. Stating the result states one argument, not two.

Adopting it anyway costs the whole family — a class cannot hold a nested type and a method of one
name (`CS0102`), so `Table(headerRows:, eachRow:)`, `Table()` and the two lambda rungs would be
re-spelled to split a type argument that did not need splitting. Its one advantage is the collision
message, which is genuinely better:

```
CS0104: 'Table' is an ambiguous reference between
        'SplitRungs<Unrect.Core.ISpace>.Table' and 'SplitRungs<Unrect.Spreadsheets.ISpreadsheetSpace>.Table'
```

Both spellings are built (`ProjectionBuilders<TSpace>`, `SplitRungs<TSpace>`) and read the same
document in `ScenarioC.SplitTypeTrick`.
