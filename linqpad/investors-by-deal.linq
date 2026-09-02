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
var dealCode = Text();

// Every caption binds free — this is the comparer earning its keep, and why it ignores whitespace
// rather than demanding an exact match.
var transactions = TableRows<DealTransaction>();

var deal = VerticalFlow(v => new
{
	DealCode = v.Next(dealCode),
	Transactions = v.Next(transactions),
});

// The report: that block, repeated, blank-row separated.
var deals = Repeat(deal, separatedBy: BlankRows());

deals.Map(SpreadsheetSpace.Create(path, "Investors")).Dump();

record DealTransaction(string AccountKey, string FundCode, string Name,
					   string TransactionType, decimal Amount, DateTime TransferDate);
