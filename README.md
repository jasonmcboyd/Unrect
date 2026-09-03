# Unrect

Financial and operational reports live in spreadsheets, and spreadsheets are flat 2D
grids carrying hierarchical, heterogeneous data: a header, a summary table, N repeating
client blocks each with its own sub-sections. Row-oriented parsers handle this badly —
they devolve into stateful cursor logic and index arithmetic that breaks the moment a
row shifts. Unrect takes a different approach: you **declare the shape** of the data —
a header, a table bound to a record type, a repeating series bounded by a caption — and
the framework decomposes the grid and projects it into typed objects. You never write a
loop that walks the grid deciding what comes next.

## Install

```
dotnet add package Unrect
dotnet add package Unrect.Spreadsheets
```

`Unrect` is the engine — the shape vocabulary, the layout composites, the strategies
that decide boundaries — and works directly over any 2D grid you can adapt to `ISpace`.
`Unrect.Spreadsheets` adds the adapters that read spreadsheet files — `.xls`/`.xlsx` today — straight into that grid;
add it when your data lives in a workbook rather than an array you built yourself.
The same package also has a streaming door, `Workbook`, for files too large to read
whole — see [Large files](#large-files) below.

`GridSpace` ships in the `Unrect` package for exactly that case:
`GridSpace.Create(values, isBlank: ...)` turns a plain 2D array into a space, deciding
there and then what counts as empty. It needs nothing else installed, which makes it the
way to build test fixtures and scripted data without a workbook in sight.

## Show me the code

A report with a typed header, a summary table, and the same repeating per-investor
block appearing twice under two different captions:

```
Fund IRR Report
Example Fund I                          2026-06-30
--------------------------------------------------
Investors | Contribution ITD | ... | Irr
--------------------------------------------------
Cash Flows Using Transfer Date
  [investor block]  [investor block]  ...
Cash Flows using inception date
  [investor block]  [investor block]  ...
```

```csharp
using Unrect.Spreadsheets;
using static Unrect.Shapes.Shape;

var header = VerticalFlow(v => new
{
    Title = v.Next(Text()),
    Fund = v.Next(Text()),
    ReportDate = v.Next(Date()),
});

// Captions bind to record properties by name (case- and whitespace-insensitive).
var summary = TableRows<SummaryRow>(bind => bind.Column(r => r.Investor, "Investors"));

var investorBlock = TableRows<CashFlow>();

// Declared once, placed twice — .Until bounds the first series so it stops at the
// second caption instead of trying to parse it as another investor block.
var series = Repeat(investorBlock, separatedBy: BlankRows());
const string Inception = "Cash Flows using inception date";

var byTransferDate = series
    .Under(Caption("Cash Flows Using Transfer Date"))
    .Until(RowContaining(Inception));

var byInception = series.Under(Caption(Inception));

var report = VerticalFlow(v => new
{
    Header = v.Next(header),
    Summary = v.Next(summary),
    ByTransferDate = v.Next(byTransferDate),
    ByInception = v.Next(byInception),
});

var result = report.Map(SpreadsheetSpace.Create("irr-report.xlsx", "IRR"));

record SummaryRow(string Investor, decimal ContributionItd, decimal DistributionItd,
                   decimal ManagementFeeItd, decimal EndBalance, double Irr);
record CashFlow(string InvestorName, DateTime Date, string Transaction, double Irr);
```

## The ideas

- **Shapes are reusable, immutable values.** Declare `report` once, apply it to as many
  workbooks as you have — `workbooks.Select(report.Map)`.
- **Diagnostics carry a declaration path and an A1 cell location** — a failure tells you
  which shape it came from and exactly where on the sheet it happened.
- **Names are inferred from your own identifiers.** The local you assign a shape to
  (`series`, `byTransferDate`) is what shows up in its diagnostics — no separate naming
  step.
- **Tolerance is declared per shape, never ambient.** `.Optional()` and `.Else()` mark
  exactly where a missing or malformed region is acceptable; nothing is silently lenient
  everywhere.
- **Content anchors survive layout drift.** `To`, `Past`, `Caption`, and `.Until` find
  their place by what a row or column *says*, not by a hard-coded offset that breaks the
  next time someone inserts a row.

## Large files

`SpreadsheetSpace.Create` reads a sheet whole, which is the simple default and the right
choice for anything that fits comfortably in memory. For a file too big for that, or the
same declaration applied to many files in sequence, `Workbook` reads a window at a time
instead of the whole grid:

```csharp
var report = VerticalFlow(v => ...);              // one declaration, reused

foreach (var path in monthlyCloseOfFunds)
{
  using var book = Workbook.Open(path);
  Publish(report.Map(book.Sheet("Detail")));       // bounded memory per iteration
}
```

Same shapes, same results — the two paths differ only in the shape of their cost. A
monotone read through `Workbook` costs about 35% more wall time for about 2.7× less live
memory than `SpreadsheetSpace.Create`; a declaration that reaches backwards or sweeps a
band wider than its window can cost more than that, which `book.Statistics("Detail")`
will tell you. Shapes are immutable and workbooks are independent, so
`Parallel.ForEach(monthlyCloseOfFunds, path => { using var book = ...; })` needs nothing
added. The full guide, including the sizing law and the statistics to act on: `docs/streaming.md`.

## Learn more

- `docs/vocabulary.md` — the full operator survey, grouped by role.
- `docs/streaming.md` — the `Workbook` guide: when to reach for it, the lifecycle rules,
  the sizing law, and the statistics vocabulary.
- `docs/design/` — the specs behind the vocabulary (layout, matching, tables, diagnostics,
  streaming).
- `linqpad/` — worked examples against the workbooks in `examples/`, including the report
  above (`linqpad/investor-irr.linq`).

## License

MIT — see [LICENSE](LICENSE).
