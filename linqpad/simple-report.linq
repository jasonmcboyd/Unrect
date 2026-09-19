<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Engine.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Engine.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>static Unrect.Projections.ProjectionBuilders&lt;Unrect.Spreadsheets.ISpreadsheetSpace&gt;</Namespace>
  <Namespace>static Unrect.Spreadsheets.SpreadsheetProjectionBuilders&lt;Unrect.Spreadsheets.ISpreadsheetSpace&gt;</Namespace>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Projections</Namespace>
  <Namespace>Unrect.Spreadsheets</Namespace>
</Query>

// The space this file is written over is named ONCE — in two imports that say the same name:
// `using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>` is the
// vocabulary every space has, and `using static Unrect.Spreadsheets.SpreadsheetProjectionBuilders<...>`
// adds the readings only a sheet can promise: Text(), Date(), Decimal(), and the Table<T> rungs
// that assert a kind per member. Everything below is spelled with no prefix and no type argument.
//
// ISpreadsheetSpace is the FULL space — values, formulas, and what a cell looks like — and it is
// the one to start a script in: `row["Amount"].Font()` and `Formula()` compile only over it, and
// the space is fixed by these two imports, not by how the workbook is opened below. (The narrower
// pair, ISheetCells with SheetProjectionBuilders, reads values alone — cheaper, and the only one
// an .xls or a streamed Workbook can answer. Never both sheet classes in one file.)
var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\simple-report.xlsx");

// The report definition: the region and the reading of it, fused into one value independent of
// any file. Each part is hoisted into a local, and the local's name is what diagnostics call it
// — no .Named needed.
//
// The header was Column(4, c => ...): a hard-coded height and four accessor calls. As a flow of
// typed leaves the 4 dissolves into the child count and every field states its kind. It consumes
// 1x4 either way, so nothing below it moves.
var reportHeader = VerticalFlow(v => new
{
	Title = v.Next(Text()),
	SubTitle = v.Next(Text()),
	ReportDate = v.Next(Date()),
	ReportId = v.Next(Text()),
});

// Captions bind to members by name, ignoring case and whitespace: Client and Amount need nothing
// said. Date and Type need a caption only because this type chose shorter names than the sheet —
// naming them TransactionDate/TransactionType would bind free.
var transactions = Table<Transaction>(bind => bind
	.Column(t => t.Date, "Transaction Date")
	.Column(t => t.Type, "Transaction Type"));

// One lambda declares the children in flow order, once, at declaration; Build combines what they read.
var report = VerticalFlow(v => new
{
	ReportHeader = v.Next(reportHeader),
	Transactions = v.Next(transactions),
});

report.Map(SpreadsheetSpace.CreateWithFormulas(path, "Report")).Dump();

record Transaction(string Client, DateTime Date, string Type, decimal Amount);
