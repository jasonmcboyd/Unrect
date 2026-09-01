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

var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\investors-by-deal.xlsx");

// One deal block: a deal-code cell over a table. Block extents are derived from what the
// block's children consume, so blocks may differ in length.
var deal =
	Vertical(
		Cell(v => v.GetString()).Named("deal code"),
		TableRows(r => new
		{
			AccountKey = r["Account Key"].GetString(),
			FundCode = r["Fund Code"].GetString(),
			Name = r["Name"].GetString(),
			Type = r["Transaction Type"].GetString(),
			Amount = r["Amount"].GetDecimal(),
			TransferDate = r["Transfer Date"].GetDateTime(),
		}).Named("transactions"))
	.Select((code, txns) => new { DealCode = code, Transactions = txns })
	.Named("deal block");

// The report: that block, repeated, blank-row separated.
var deals = Repeat(deal, separatedBy: BlankRows());

deals.Map(SpreadsheetSpace.Create(path, "Investors")).Dump();
