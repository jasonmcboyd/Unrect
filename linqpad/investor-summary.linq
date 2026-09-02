<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Spreadsheets</Namespace>
  <Namespace>Unrect.Shapes</Namespace>
  <Namespace>static Unrect.Shapes.Shape</Namespace>
</Query>

var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\investor-summary.xlsx");

// Per-investor detail block: a name cell over a transaction table.
// The tables below deliberately keep their lambda form: the corpus needs one worked example of
// the escape hatch that survives for columns whose kind varies or whose value needs a Try*.
var investorName = Text();

var detailTransactions = TableRows(r => new
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

var summary = TableRows(r => new
{
	Investor = r["Investor"].GetString(),
	Contributions = r["Contributions"].GetDecimal(),
	Distributions = r["Distributions"].GetDecimal(),
	Net = r["Net"].GetDecimal(),
});

var details = Repeat(investorDetail, separatedBy: BlankRows(), atLeast: 1).AfterBlankRows();

// The report. Column(c => ...) discovers the header height; the gap before the summary is the
// table's own default offset; the gap before the details section is the repeat's offset; the
// gaps between detail blocks are the repeat's separator.
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
