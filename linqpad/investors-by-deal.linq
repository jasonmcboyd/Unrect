<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Engine.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Engine.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Spreadsheets</Namespace>
  <Namespace>Unrect.Projections</Namespace>
  <Namespace>static Unrect.Projections.ProjectionBuilders&lt;Unrect.Spreadsheets.ISpreadsheetSpace&gt;</Namespace>
  <Namespace>static Unrect.Spreadsheets.SpreadsheetProjectionBuilders&lt;Unrect.Spreadsheets.ISpreadsheetSpace&gt;</Namespace>
</Query>

// The space is named once, in the query's namespace imports: the canonical vocabulary as
// `using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>`, and the
// sheet's own readings — Text() and the Table<T> rungs used below — as
// `using static Unrect.Spreadsheets.SpreadsheetProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>`.
var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\investors-by-deal.xlsx");

// One deal block: a deal-code cell over a table. Block extents are derived from what the
// block's children consume, so blocks may differ in length.
var dealCode = Text();

// Every caption binds free — this is the comparer earning its keep, and why it ignores whitespace
// rather than demanding an exact match.
var transactions = Table<DealTransaction>();

var deal = VerticalFlow(v => new
{
	DealCode = v.Next(dealCode),
	Transactions = v.Next(transactions),
});

// The report: that block, repeated, blank-row separated.
var deals = VerticalRepeat(deal);

var space = SpreadsheetSpace.CreateWithFormulas(path, "Investors");

deals.Map(space).Dump();

record DealTransaction(string AccountKey, string FundCode, string Name,
					   string TransactionType, decimal Amount, DateTime TransferDate);
