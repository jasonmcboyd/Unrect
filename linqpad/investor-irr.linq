<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Engine.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Engine.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>static Unrect.Projections.ProjectionBuilders&lt;Unrect.Spreadsheets.ISheetCells&gt;</Namespace>
  <Namespace>static Unrect.Spreadsheets.SheetProjectionBuilders&lt;Unrect.Spreadsheets.ISheetCells&gt;</Namespace>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Projections</Namespace>
  <Namespace>Unrect.Spreadsheets</Namespace>
</Query>

var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\investor-irr.xlsx");

// The space is named once, in the query's namespace imports: the canonical vocabulary as
// `using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>`, and the
// sheet's own readings — Text(), Date() and the Table<T> rungs — as
// `using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>`.
//
// What this demonstrates: ONE projection declared once and PLACED TWICE, and Until — the dual of
// On. The sheet carries the same per-investor blocks twice, under two headings:
//
//   Cash Flows Using Transfer Date        <- first series
//     ... three investor blocks ...
//   Cash Flows using inception date       <- second series
//     ... the same three blocks, different dates ...
//
// Without a way to say where the first series ENDS, its repeat runs into the second heading
// and fails from inside an item. Until bounds it by content, and — because the bound is
// consumed in full — the next child's own seek finds that row at distance zero.
var reportHeader = VerticalFlow(v => new
	{
		Title = v.Next(Text()),
		Fund = v.Next(Text()),
		ReportDate = v.Next(Date()),
		ReportId = v.Next(Text()),
	});

// Five of six captions bind with nothing said: the comparer ignores case and whitespace, so
// "Contribution ITD" fills ContributionItd. Only Investors needs a caption, and only because the
// sheet's heading is plural where the row is singular.
var summary = Table<SummaryRow>(bind => bind.Column(r => r.Investor, "Investors"));

// All four bind free.
var investorBlock = Table<CashFlow>();

// The row that both ends the first series and begins the second. One literal, so the bound and
// the heading cannot drift apart — both go through the same matching rule.
const string Inception = "Cash Flows using inception date";

// Declared once; the two placements below differ only in what announces them and where they stop.
var irrDetails = VerticalRepeat(investorBlock, separatedBy: BlankRows());

// Heading is what a section announces itself by: it finds the row, asserts the text and consumes
// it at full width — structure, not a value, so nothing is built here only to be discarded, and a
// missing heading still fails in the caption's own words. Chained headings read in document order,
// both above the one section; left to right is the sheet top to bottom — where the series stops,
// what announces it, then what it reads, which is the hoisted declaration placed with .Of.
var byTransferDate = Until(RowContaining(Inception))
	.Heading("IRR Details")
	.Heading("Cash Flows Using Transfer Date")
	.Of(irrDetails);

var byInception = Heading(Inception).Of(irrDetails);

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

record SummaryRow(string Investor, decimal ContributionItd, decimal DistributionItd,
				  decimal ManagementFeeItd, decimal EndBalance, double Irr);

record CashFlow(string InvestorName, DateTime Date, string Transaction, double Irr);
