<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.Engine.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.Engine.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Spreadsheets</Namespace>
  <Namespace>Unrect.Projections</Namespace>
  <Namespace>static Unrect.Projections.ProjectionBuilders&lt;Unrect.Spreadsheets.ISheetCells&gt;</Namespace>
  <Namespace>static Unrect.Spreadsheets.SheetProjectionBuilders&lt;Unrect.Spreadsheets.ISheetCells&gt;</Namespace>
</Query>

// The space this file is written over is named ONCE — in two imports that say the same name:
// `using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>` is the
// vocabulary every space has, and `using static Unrect.Spreadsheets.SheetProjectionBuilders<...>`
// adds the readings only a sheet can promise: Text(), Date(), Decimal(), and the Table<T> rungs
// that assert a kind per member. Everything below is spelled with no prefix and no type argument.
// A file that read formulas would name ISpreadsheetSpace in both lines and take its second from
// SpreadsheetProjectionBuilders instead — never both sheet classes at once — and nothing else in
// it would change.
var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\simple-report.xlsx");

// The report definition: the region and the reading of it, fused into one value independent of
// any file. Each part is hoisted into a local, and the local's name is what diagnostics call it
// — no .Named needed.
//
// The header was Column(4, c => ...): a hard-coded height and four accessor calls. As a flow of
// typed leaves the 4 dissolves into the child count and every field states its kind. It consumes
// 1x4 either way, so nothing below it moves.
var reportHeader = VerticalFlow(v =>
{
	var title = v.Next(Text());
	var subTitle = v.Next(Text());
	var reportDate = v.Next(Date());
	var reportId = v.Next(Text());

	return v.Build(read => new
	{
		Title = read.Of(title),
		SubTitle = read.Of(subTitle),
		ReportDate = read.Of(reportDate),
		ReportId = read.Of(reportId),
	});
});

// Captions bind to members by name, ignoring case and whitespace: Client and Amount need nothing
// said. Date and Type need a caption only because this type chose shorter names than the sheet —
// naming them TransactionDate/TransactionType would bind free.
var transactions = Table<Transaction>(bind => bind
	.Column(t => t.Date, "Transaction Date")
	.Column(t => t.Type, "Transaction Type"));

// One lambda declares the children in flow order, once, at declaration; Build combines what they read.
var report = VerticalFlow(v =>
{
	var header = v.Next(reportHeader);
	var rows = v.Next(transactions);

	return v.Build(read => new
	{
		ReportHeader = read.Of(header),
		Transactions = read.Of(rows),
	});
});

report.Map(SpreadsheetSpace.Create(path, "Report")).Dump();

record Transaction(string Client, DateTime Date, string Type, decimal Amount);
