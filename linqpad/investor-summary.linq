<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.Engine.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.Engine.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Spreadsheets</Namespace>
  <Namespace>Unrect.Projections</Namespace>
  <Namespace>static Unrect.Projections.ProjectionBuilders&lt;Unrect.Spreadsheets.ISheetCells&gt;</Namespace>
  <Namespace>static Unrect.Spreadsheets.SheetProjectionBuilders&lt;Unrect.Spreadsheets.ISheetCells&gt;</Namespace>
</Query>

// The space is named once, in the query's namespace imports: the canonical vocabulary as
// `using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>`, and the
// sheet's own readings as `using static Unrect.Spreadsheets.SheetProjectionBuilders<...>`. The cell
// reads below — .Text(), .Date(), .Decimal() on a point — come with the same package
// (Unrect.Spreadsheets), and for the same reason: a kind is a sheet's claim about its own cells.
var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\investor-summary.xlsx");

// Per-investor detail block: a name cell over a transaction table.
// The tables below deliberately keep their lambda form: the corpus needs one worked example of the
// escape hatch that survives for a row no record type describes — the view hands back the cell as a
// place, and the reading is written at it.
var investorName = Text();

var detailTransactions = Table(r => new
{
	Date = r["Date"].Date(),
	Type = r["Transaction Type"].Text(),
	Amount = r["Amount"].Decimal(),
});

var investorDetail = VerticalFlow(v =>
{
	var investor = v.Next(investorName);
	var transactions = v.Next(detailTransactions);

	return v.Build(read => new
	{
		Investor = read.Of(investor),
		Transactions = read.Of(transactions),
	});
});

var reportHeader = Column(c => new
{
	Title = c[0].Text(),
	ReportDate = c[1].Date(),
	ReportId = c[2].Text(),
});

var summary = Table(r => new
{
	Investor = r["Investor"].Text(),
	Contributions = r["Contributions"].Decimal(),
	Distributions = r["Distributions"].Decimal(),
	Net = r["Net"].Decimal(),
});

// Position first: the blank gap in front of the section is declared where the reader meets it,
// ahead of the section itself. The same declaration as the postfix .AfterBlankRows() it replaces
// — the pipeline is a spelling, not a semantics.
var details = AfterBlankRows().VerticalRepeat(investorDetail, separatedBy: BlankRows(), atLeast: 1);

// The report. Column(c => ...) discovers the header height; the gap before the summary is the
// table's own default offset; the gap before the details section is that AfterBlankRows entry;
// the gaps between detail blocks are the repeat's separator.
var report = VerticalFlow(v =>
{
	var header = v.Next(reportHeader);
	var summaryRows = v.Next(summary);
	var detailBlocks = v.Next(details);

	return v.Build(read => new
	{
		ReportHeader = read.Of(header),
		Summary = read.Of(summaryRows),
		Details = read.Of(detailBlocks),
	});
});

var result = report.Map(SpreadsheetSpace.Create(path, "Summary"));

// Cross-region correlations are post-parse validation, not decomposition.
(result.Summary.Count == result.Details.Count).Dump("summary rows == detail blocks");
result.Dump();
