<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Array.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Array.dll</Reference>
  <Reference Relative="..\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Excel.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Excel.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>Unrect</Namespace>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Excel</Namespace>
  <Namespace>Unrect.Strategies</Namespace>
  <Namespace>static Unrect.RegionBuilderFactory</Namespace>
  <Namespace>static Unrect.Strategies.SizeStrategies</Namespace>
</Query>

var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\investors-by-deal.xlsx");

var space = SpreadsheetSpace.Create(path, "Investors");

// One deal block, declared once:
//   offset — skip the blank separator row(s) between blocks
//   area   — rows while any cell has a value (the whole block, whatever its length)
// Inside the block, a vertical stack:
//   1. deal code — the single cell in column A (structural, explicit)
//   2. headers   — 1 row, as many columns as have values
//   3. data      — rows while any cell has a value (the rest of the block)
var blockBuilder =
	Vertical(
		OffsetStrategies.SkipBlankRows(),
		RowsWhileAnyValue().ToAreaStrategy(),
		Builder(0, 0, 1, 1),
		Builder(RowStrategies.TakeRows(1).TakeColumnsWhileAnyValue()),
		Builder(RowsWhileAnyValue().ToAreaStrategy()));

// The report: that block, repeated until the space is exhausted.
var deals = Repeat(blockBuilder).Build(space).Subregions
	.Select(block => block.Map((dealCode, headers, txns, _) => new
	{
		DealCode = dealCode.Space[0, 0].GetString(),
		Transactions = txns.Rows()
			.Select(r => new
			{
				AccountKey = r[0].GetString(),
				FundCode = r[1].GetString(),
				Name = r[2].GetString(),
				Type = r[3].GetString(),
				Amount = r[4].GetDecimal(),
				TransferDate = r[5].GetDateTime(),
			})
			.ToArray(),
	}))
	.ToArray();

deals.Dump();
