<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Array\bin\Debug\netstandard2.1\Unrect.Array.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Array\bin\Debug\netstandard2.1\Unrect.Array.dll</Reference>
  <Reference Relative="..\src\Unrect.Array\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Array\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>Unrect.Array</Namespace>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Shapes</Namespace>
  <Namespace>static Unrect.Shapes.Shape</Namespace>
</Query>

// Shapes over an in-memory array. The adapter decides blankness where data enters
// the system — in this grid, zero means empty — and everything above it is the same
// vocabulary the spreadsheet scripts use.
var nums = new[,]
{
	{ 1,  2,  3,  4 },
	{ 5,  6,  7,  8 },
	{ 0,  0,  0,  0 },
	{ 9,  10, 11, 12 },
	{ 13, 14, 15, 16 },
	{ 17, 18, 19, 20 },
	{ 0,  0,  0,  0 },
	{ 21, 22, 23, 24 },
	{ 25, 26, 27, 28 },
};

var space = ArraySpace.Create(nums, isBlank: v => v == 0);

// Repeating blocks of varying height, separated by blank rows — the same shape idea as
// investors-by-deal, in miniature. Nothing here counts rows: Row takes one, Cells discovers
// the rest of the block by running out of values at the separator, and the separator itself
// is what carries the repeat across the gap to the next block.
var block =
	Vertical(
		Row(r => r.Select(v => v.GetInt()).ToArray())
			.Named("first row"),
		Cells(b => b.Rows.Select(r => r.Select(v => v.GetInt()).ToArray()).ToArray())
			.Named("rest"))
	.Select((firstRow, rest) => new { FirstRow = firstRow, Rest = rest })
	.Named("block");

Repeat(block, separatedBy: BlankRows()).Map(space).Dump();
