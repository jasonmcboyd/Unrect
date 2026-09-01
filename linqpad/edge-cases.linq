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

var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\edge-cases.xlsx");

// The corner-case fixture (distilled from the real K-1 workbook):
//   row 1: one of each ordinary kind      row 2: five error cells
//   row 3: whitespace / empty / absent    row 4: the remaining two errors
var defaultSpace = SpreadsheetSpace.Create(path, "Edges");
var strictSpace = SpreadsheetSpace.Create(path, "Edges", isBlank: _ => false);

// 1. The kind map — errors are first-class, never blank.
Cells(5, 4, b => Enumerable.Range(0, 4)
		.Select(r => Enumerable.Range(0, 5).Select(c => b[c, r].ToString()).ToArray())
		.ToArray())
	.Map(defaultSpace)
	.Dump("cell kinds (default blankness)");

// 2. Error cells answer Try* with null and Get* with a message naming the error.
var err = defaultSpace[0, 1];
new
{
	Kind = err.Kind.ToString(),
	Error = err.GetError().ToString(),
	err.HasValue,
	TryGetDecimal = err.TryGetDecimal()?.ToString() ?? "null",
	GetDecimalThrows = ((Func<string>)(() => { try { err.GetDecimal(); return "no"; } catch (InvalidOperationException ex) { return ex.Message; } }))(),
}.Dump("the #VALUE! cell");

// 3. Blankness belongs to the adapter: the same whitespace row under both rules.
new
{
	TwoSpaces_Default = defaultSpace[0, 2].ToString(),
	TwoSpaces_Strict = strictSpace[0, 2].ToString(),
	EmptyString_Strict = strictSpace[2, 2].ToString(),   // "" maps to Blank before the predicate — the fidelity floor
	AbsentCell_Strict = strictSpace[3, 2].ToString(),
}.Dump("whitespace vs empty vs absent");

// 4. And it changes decomposition: the value-bearing block over cols A-D stops at the
// whitespace row by default, but includes it under strict fidelity.
var block = Cells(b => new Unrect.Core.Size(b.Width, b.Height));
new
{
	Default = block.Map(defaultSpace.GetSubspace(new Offset(0, 0), new Area(4, 4))).ToString(),
	Strict = block.Map(strictSpace.GetSubspace(new Offset(0, 0), new Area(4, 4))).ToString(),
}.Dump("discovered extent, cols A-D");
