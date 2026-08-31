<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Array.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Array.dll</Reference>
  <Reference Relative="..\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Excel.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Excel.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>Unrect</Namespace>
  <Namespace>Unrect.Array</Namespace>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Excel</Namespace>
  <Namespace>Unrect.Strategies</Namespace>
  <Namespace>static Unrect.RegionBuilderFactory&lt;Unrect.Excel.SpreadsheetValueBase&gt;</Namespace>
  <Namespace>static Unrect.Strategies.SizeStrategies</Namespace>
</Query>

var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\simple-report.xlsx");

var space = SpreadsheetSpace.Create(path, "Report");

// Declared shape (vertical stack). Only the report header uses explicit bounds —
// it is structurally fixed. Everything else is discovered by strategies:
//   1. header block   — A1:A4, a 1-wide, 4-tall column of values
//   2. column headers — skip however many blank rows, take 1 row, as many columns as have values
//   3. data           — rows while any cell has a value
var builder =
	Vertical(
		Builder(0, 0, 1, 4),
		Builder(
			OffsetStrategies<SpreadsheetValueBase>.SkipRowsWhileAll(v => !v.HasValue),
			RowStrategies<SpreadsheetValueBase>.TakeRowsWhile((s, r) => r < 1).TakeColumnsWhileAny(v => v.HasValue)),
		Builder(WhileAny<SpreadsheetValueBase>(v => v.HasValue).ToAreaStrategy()));

var region = builder.Build(space);

// `columns` anchors the decomposition but is deliberately not projected —
// declaring a region and surfacing it in the result are independent choices.
var report = region.Map((header, columns, data, _) => new
{
	ReportHeader = new
	{
		Title = header.Space[0, 0].GetString(),
		SubTitle = header.Space[0, 1].GetString(),
		ReportDate = header.Space[0, 2].GetDateTime(),
		ReportId = header.Space[0, 3].GetString(),
	},
	Transactions = data.Rows()
		.Select(r => new
		{
			Client = r[0].GetString(),
			Date = r[1].GetDateTime(),
			Type = r[2].GetString(),
			Amount = r[3].GetDouble(),
		})
		.ToArray(),
});

report.Dump();
