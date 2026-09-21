<Query Kind="Program">
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Engine.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Engine.dll</Reference>
  <Reference Relative="..\src\Unrect.Interactive\bin\Debug\netstandard2.1\Unrect.Interactive.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Interactive\bin\Debug\netstandard2.1\Unrect.Interactive.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>static Unrect.Projections.ProjectionBuilders&lt;Unrect.Spreadsheets.ICellSpace&gt;</Namespace>
  <Namespace>static Unrect.Spreadsheets.SheetProjectionBuilders&lt;Unrect.Spreadsheets.ICellSpace&gt;</Namespace>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Interactive</Namespace>
  <Namespace>Unrect.Projections</Namespace>
  <Namespace>Unrect.Spreadsheets</Namespace>
</Query>

void Main()
{
	var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\multi-header-table.xlsx");
	
	var space = SpreadsheetSpace.CreateWithFormulas(path, "Sheet1");


	var report =
		Table<Transaction>(2)
		.Map(space)
		.Dump();
	
	
//
//	space.ScaffoldRecord("Transaction", headerRows: 2).Dump();
//	"".Dump();
//	space.ScaffoldClass("Transaction", headerRows: 2).Dump();

	
}

// You can define other methods, fields, classes and namespaces here
public sealed class Transaction
{
	public int FromId { get; init; }
	public string FromCode { get; init; } = "";
	public int ToId { get; init; }
	public string ToCode { get; init; } = "";
}