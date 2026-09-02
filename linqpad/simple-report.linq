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

var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\simple-report.xlsx");

// The report definition: shape and projection fused, independent of any file. Each part is
// hoisted into a local, and the local's name is what diagnostics call it — no .Named needed.
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
var transactions = TableRows<Transaction>(bind => bind
	.Column(t => t.Date, "Transaction Date")
	.Column(t => t.Type, "Transaction Type"));

// One lambda declares the children in flow order and builds the result from what they read.
var report = VerticalFlow(v => new
{
	ReportHeader = v.Next(reportHeader),
	Transactions = v.Next(transactions),
});

report.Map(SpreadsheetSpace.Create(path, "Report")).Dump();

record Transaction(string Client, DateTime Date, string Type, decimal Amount);
