<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Spreadsheets</Namespace>
  <Namespace>Unrect.Projections</Namespace>
  <Namespace>static Unrect.Projections.ProjectionBuilders&lt;Unrect.Core.ISpace&gt;</Namespace>
</Query>

// The space is named once, in the query's namespace imports:
// `using static Unrect.Projections.ProjectionBuilders<Unrect.Core.ISpace>`.
var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\investor-summary.xlsx");

// Per-investor detail block: a name cell over a transaction table.
// The tables below deliberately keep their lambda form: the corpus needs one worked example of
// the escape hatch that survives for columns whose kind varies or whose value needs a Try*.
var investorName = Text();

var detailTransactions = Table(r => new
{
	Date = r["Date"].GetDateTime(),
	Type = r["Transaction Type"].GetString(),
	Amount = r["Amount"].GetDecimal(),
});

var investorDetail = VerticalFlow(v => new
{
	Investor = v.Next(investorName),
	Transactions = v.Next(detailTransactions),
});

var reportHeader = Column(c => new
{
	Title = c[0].GetString(),
	ReportDate = c[1].GetDateTime(),
	ReportId = c[2].GetString(),
});

var summary = Table(r => new
{
	Investor = r["Investor"].GetString(),
	Contributions = r["Contributions"].GetDecimal(),
	Distributions = r["Distributions"].GetDecimal(),
	Net = r["Net"].GetDecimal(),
});

// Position first: the blank gap in front of the section is declared where the reader meets it,
// ahead of the section itself. The same declaration as the postfix .AfterBlankRows() it replaces
// — the pipeline is a spelling, not a semantics.
var details = AfterBlankRows().VerticalRepeat(investorDetail, separatedBy: BlankRows(), atLeast: 1);

// The report. Column(c => ...) discovers the header height; the gap before the summary is the
// table's own default offset; the gap before the details section is that AfterBlankRows entry;
// the gaps between detail blocks are the repeat's separator.
var report = VerticalFlow(v => new
{
	ReportHeader = v.Next(reportHeader),
	Summary = v.Next(summary),
	Details = v.Next(details),
});

var result = report.Map(SpreadsheetSpace.Create(path, "Summary"));

// Cross-region correlations are post-parse validation, not decomposition.
(result.Summary.Count == result.Details.Count).Dump("summary rows == detail blocks");
result.Dump();
