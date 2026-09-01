<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Array.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Array.dll</Reference>
  <Reference Relative="..\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Excel.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Excel.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Excel</Namespace>
  <Namespace>Unrect.Shapes</Namespace>
  <Namespace>static Unrect.Shapes.Shape</Namespace>
</Query>

var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\investor-irr.xlsx");

// What this demonstrates: ONE shape declared once and PLACED TWICE, and .Until — the dual of
// .After. The sheet carries the same per-investor blocks twice, under two captions:
//
//   Cash Flows Using Transfer Date        <- first series
//     ... three investor blocks ...
//   Cash Flows using inception date       <- second series
//     ... the same three blocks, different dates ...
//
// Without a way to say where the first series ENDS, its repeat runs into the second caption
// and fails from inside an item. .Until bounds it by content, and — because the bound is
// consumed in full — the next child's own seek finds that caption at distance zero.
var reportHeader = Column(4, c => new
{
	Title = c[0].GetString(),
	Fund = c[1].GetString(),
	ReportDate = c[2].GetDateTime(),
	ReportId = c[3].GetString(),
});

var summary = TableRows(r => new
{
	Investor = r["Investors"].GetString(),
	Contribution = r["Contribution ITD"].GetDecimal(),
	Distribution = r["Distribution ITD"].GetDecimal(),
	ManagementFee = r["Management Fee ITD"].GetDecimal(),
	EndBalance = r["End Balance"].GetDecimal(),
	Irr = r["IRR"].GetDouble(),
});

var investorBlock = TableRows(r => new
{
	Investor = r["Investor Name"].GetString(),
	Date = r["Date"].GetDateTime(),
	Transaction = r["Transaction"].GetString(),
	Irr = r["IRR"].GetDouble(),
});

// Declared once; the two placements below differ only in where they start and stop.
var irrDetails = Repeat(investorBlock, separatedBy: BlankRows());

var byTransferDate = irrDetails
	.After(Then(SeekRowContaining("Cash Flows Using Transfer Date"), SkipRows(1)))
	.Until(RowContaining("Cash Flows using inception date"));

var byInception = irrDetails
	.After(Then(SeekRowContaining("Cash Flows using inception date"), SkipRows(1)));

var report = VerticalFlow(v => new
{
	ReportHeader = v.Next(reportHeader),
	Summary = v.Next(summary),
	ByTransferDate = v.Next(byTransferDate),
	ByInception = v.Next(byInception),
});

var mapped = report.MapWithDiagnostics(SpreadsheetSpace.Create(path, "IRR"));
var result = mapped.Value;

// Nothing left undescribed: the two bounded series between them account for the whole sheet.
mapped.Diagnostics.Select(d => d.ToString()).Dump("diagnostics");

// Cross-region correlation is post-parse validation, not decomposition.
new
{
	SummaryInvestors = result.Summary.Count,
	TransferDateBlocks = result.ByTransferDate.Count,
	InceptionBlocks = result.ByInception.Count,
	SeriesAgreeWithSummary =
		result.ByTransferDate.Count == result.Summary.Count &&
		result.ByInception.Count == result.Summary.Count,
}.Dump("validation");

result.Dump();
