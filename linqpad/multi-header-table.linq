<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Engine.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Engine.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>static Unrect.Projections.ProjectionBuilders&lt;Unrect.Spreadsheets.ISpreadsheetSpace&gt;</Namespace>
  <Namespace>static Unrect.Spreadsheets.SpreadsheetProjectionBuilders&lt;Unrect.Spreadsheets.ISpreadsheetSpace&gt;</Namespace>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Projections</Namespace>
  <Namespace>Unrect.Spreadsheets</Namespace>
</Query>

// A header two rows tall: "From" and "To", each merged over two columns, above the captions
// Id | Code | Id | Code. The captions repeat, so a column is addressed by its PATH through the
// header — the band, then the caption — and a path is a list, never a joined string.
//
// Without merge ranges a merged cell reads as a value in its first cell and blanks beside it, so a
// band reaches rightward over the blank cells after it, as every reader of such headers assumes.
// The sheet is indented a row and a column; nothing here says so, because a projection that does
// not say where it starts steps over the blank rows in front of it, and the blank column before
// the first caption is a column of the table with no label.
var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\multi-header-table.xlsx");
var sheet = SpreadsheetSpace.CreateWithFormulas(path, "Sheet1");

// What the header says, as the table sees it.
Table(2, t => t.ColumnPaths.Select(p => string.Join(" / ", p)).ToList()).Map(sheet).Dump("column paths");

// 1. By hand. A step is a name, the nth of a name ("Id", 1), or a position 1 — every number counted
//    from zero. A bare r["Id"] is refused: it is two columns, and picking the first would hide the
//    day a report grows a second one.
var byHand = Table(2, r => new
	{
		FromId = r["From", "Id"].Integer(),
		FromCode = r["From", "Code"].Text(),
		ToId = r["To", "Id"].Integer(),
		ToCode = r["To", 1].Text(),
	});

byHand.Map(sheet).Dump("by path");

// 2. Bound. Flat binding is the rule: a member answers to a caption, or to the banded column whose
//    whole path run together is its name — FromId for the column at From, Id. Nothing declared.
Table<Transfer>(2).Map(sheet).Dump("Table<Transfer>(2)");

record Transfer(int FromId, string FromCode, int ToId, string ToCode);
