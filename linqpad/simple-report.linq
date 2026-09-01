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

var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\simple-report.xlsx");

// The report definition: shape and projection fused, independent of any file. Each part is
// hoisted into a local, and the local's name is what diagnostics call it — no .Named needed.
//   reportHeader — A1:A4, structurally fixed by the format (Column(c => ...) would discover it)
//   transactions — defaults do the rest: skip the blank gap, one header row, rows while any value
var reportHeader = Column(4, c => new
{
	Title = c[0].GetString(),
	SubTitle = c[1].GetString(),
	ReportDate = c[2].GetDateTime(),
	ReportId = c[3].GetString(),
});

var transactions = TableRows(r => new
{
	Client = r["Client"].GetString(),
	Date = r["Transaction Date"].GetDateTime(),
	Type = r["Transaction Type"].GetString(),
	Amount = r["Amount"].GetDecimal(),
});

// One lambda declares the children in flow order and builds the result from what they read.
var report = VerticalFlow(v => new
{
	ReportHeader = v.Next(reportHeader),
	Transactions = v.Next(transactions),
});

report.Map(SpreadsheetSpace.Create(path, "Report")).Dump();
