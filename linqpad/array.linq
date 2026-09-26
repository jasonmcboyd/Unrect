<Query Kind="Statements">
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Engine.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Engine.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>static Unrect.Projections.ProjectionBuilders&lt;Unrect.Core.IValueSpace&lt;int&gt;&gt;</Namespace>
  <Namespace>Unrect</Namespace>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Projections</Namespace>
</Query>

// Projections over an in-memory array. The adapter decides blankness where data enters
// the system — in this grid, zero means empty — and everything above it is the same
// vocabulary the spreadsheet scripts use — down to the header. The query's namespace imports name
// this file's space once, `using static Unrect.Projections.ProjectionBuilders<Unrect.Core.IValueSpace<int>>`,
// which is the same line the spreadsheet scripts carry with ICellSpace in it: one vocabulary, each
// file naming the space it is written over. What differs is only what a cell can be asked — a point
// over a grid of values answers Value(), a point over a sheet answers Decimal() — because the
// reading a space can promise is the space's own.
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

var space = GridSpace.Create(nums, isBlank: v => v == 0);

// Repeating blocks of varying height, separated by blank rows — the same shape idea as
// investors-by-deal, in miniature. Nothing here counts rows: firstRow takes one, rest discovers
// the remainder of the block by running out of values at the separator, and the separator itself
// is what carries the repeat across the gap to the next block.
var firstRow = Row(r => r.Select(p => p.Value()).ToArray());

var rest = Range(b => b.Rows.Select(r => r.Select(p => p.Value()).ToArray()).ToArray());

var block = VerticalFlow(v => new
{
	FirstRow = v.Next(firstRow),
	Rest = v.Next(rest),
});

VerticalRepeat(block).Map(space).Dump();
