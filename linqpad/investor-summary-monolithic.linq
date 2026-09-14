<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>static Unrect.Projections.ProjectionBuilders&lt;Unrect.Core.ICellValues&gt;</Namespace>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Projections</Namespace>
  <Namespace>Unrect.Spreadsheets</Namespace>
</Query>

// The MONOLITHIC counterpart to investor-summary.linq: the same report parsed as one inline
// VerticalFlow rather than hoisted into named sub-projections. Kept side-by-side to compare the
// two structures — identical output, different decomposition.
//
// The space is named once, in the query's namespace imports:
// `using static Unrect.Projections.ProjectionBuilders<Unrect.Core.ICellValues>`.
var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\investor-summary.xlsx");

// The tables keep the lambda form but use the compute-legal binder (r.Text/r.Decimal/r.Date): each
// column is resolved from its caption and read in one call, and any failure carries the cell's A1
// location. (The decomposed investor-summary.linq keeps the raw r["..."].Get* escape hatch, so the
// two scripts read the same report through the two accessor styles.)
//
// Column(c => ...) discovers the header height; the gap before the summary is the table's own default
// offset; the gap before the details section is the AfterBlankRows() entry; the gaps between detail
// blocks are the repeat's separator.
var report =
	VerticalFlow(v => new
	{
		ReportHeader = v.Next(
			Column(c => new
			{
				Title      = c.Text(0),
				ReportDate = c.Date(1),
				ReportId   = c.Text(2),
			})),
		Summary = v.Next(
			Table((TableRow r) => new
			{
				Investor      = r.Text("Investor"),
				Contributions = r.Decimal("Contributions"),
				Distributions = r.Decimal("Distributions"),
				Net           = r.Decimal("Net"),
			})),
		Details = v.Next(
			AfterBlankRows()
			.VerticalRepeat(
				VerticalFlow(d => new
				{
					Investor     = d.Next(Text()),
					Transactions = d.Next(
						Table((TableRow r) => new
						{
							Date   = r.Date("Date"),
							Type   = r.Text("Transaction Type"),
							Amount = r.Decimal("Amount"),
						})),
				}),
				separatedBy: BlankRows(),
				atLeast: 1)),
	});

var result = report.Map(SpreadsheetSpace.Create(path, "Summary"));

// Cross-region correlations are post-parse validation, not decomposition.
(result.Summary.Count == result.Details.Count).Dump("summary rows == detail blocks");
result.Dump();
