<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Array\bin\Debug\netstandard2.1\Unrect.Array.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Array\bin\Debug\netstandard2.1\Unrect.Array.dll</Reference>
  <Reference Relative="..\src\Unrect.Array\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Array\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>Unrect</Namespace>
  <Namespace>Unrect.Array</Namespace>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Strategies</Namespace>
  <Namespace>static Unrect.RegionBuilderFactory</Namespace>
  <Namespace>static Unrect.Strategies.SizeStrategies</Namespace>
</Query>

// The array adapter: blankness is decided where data enters the system —
// in this grid, zero means empty.
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

// Repeating blocks of varying height, separated by blank rows —
// same shape idea as investors-by-deal, in miniature.
var block =
	Vertical(
		OffsetStrategies.SkipBlankRows(),
		RowsWhileAnyValue().ToAreaStrategy(),
		Builder(RowStrategies.TakeRows(1).TakeColumnsWhileAnyValue()),
		Builder(RowsWhileAnyValue().ToAreaStrategy()));

var blocks = Repeat(block).Build(space).Subregions
	.Select(b => b.Map((firstRow, rest, _) => new
	{
		FirstRow = firstRow.RowOrderEnumerable().Select(v => v.GetInt()).ToArray(),
		Rest = rest.Rows().Select(r => r.Select(v => v.GetInt()).ToArray()).ToArray(),
	}))
	.ToArray();

blocks.Dump();
