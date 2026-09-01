<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Array.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Array.dll</Reference>
  <Reference Relative="..\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Excel.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Excel\bin\Debug\netstandard2.1\Unrect.Excel.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Excel</Namespace>
  <Namespace>Unrect.Shapes</Namespace>
  <Namespace>Unrect.Strategies</Namespace>
  <Namespace>static Unrect.Shapes.Shape</Namespace>
  <Namespace>static Unrect.Strategies.SizeStrategies</Namespace>
</Query>

// NOTE: examples/scrubbed-k1.xlsx is a LOCAL-ONLY fixture (gitignored, never committed).
//
// ONE root shape, ZERO hard-coded coordinates. The working style that survives
// real-world drift (extra rows, moved columns, varying fund counts):
//   - rows anchor by content seeks (SeekRowContaining);
//   - the header is an Overlay (independent blocks sharing rows — placement, not flow),
//     bounded with .Sized so every seek inside it is unambiguous;
//   - columns are resolved from CONTENT in the final Select: caption row -> column
//     indexes by name; fund columns -> cells right of the "Fund Short Name" label.
var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\scrubbed-k1.xlsx");
var space = SpreadsheetSpace.Create(path, "Sheet1");

string Code(CellValue v) => v.TryGetString() ?? v.TryGetInt()?.ToString() ?? "";

// A full-width single row anchored by a content seek.
IShape<CellValue[]> FullRow(string anchor) =>
	Cells(RowStrategies.TakeRows(1).TakeColumnsWhile((s, c) => true), b => b.Row(0).ToArray())
		.After(SeekRowContaining(anchor)).Named(anchor);

var header =
	Overlay(
		Cells(2, 5, b => Enumerable.Range(0, 5)
				.ToDictionary(r => b[0, r].GetString().TrimEnd(':'), r => b[1, r].ToString()))
			.After(Then(SeekColumnContaining("EIN:"), SeekRowContaining("EIN:"))).Named("entity"),
		FullRow("ATAX"),                                            // the caption row
		FullRow("Fund Short Name"),                                 // fund codes
		FullRow("Fund Short Name").Down(4).Named("ownership pcts"),
		FullRow("Taxable Income"))
	.Sized(RowsWhileAnyValue().ToAreaStrategy())                    // bounded: seeks stay unambiguous
	.Named("header")
	.Select((entity, captions, fundRow, pctRow, tiRow) =>
		new { Entity = entity, Captions = captions, FundRow = fundRow, PctRow = pctRow, TiRow = tiRow });

var section =
	Cells(RowsWhileAnyValue().ToAreaStrategy(), b => b.Rows.Select(r => r.ToArray()).ToArray())
		.After(SeekRowContaining("K-1 Lines 1-21")).Named("K-1 lines 1-21");

var report = Vertical(header, section).Select((h, rows) =>
{
	int Col(string caption) => Array.FindIndex(h.Captions,
		v => string.Equals(v.TryGetString()?.Trim(), caption, StringComparison.OrdinalIgnoreCase));
	int dt = Col("DT"), atax = Col("ATAX"), fed = Col("Federal");

	var labelIdx = Array.FindIndex(h.FundRow,
		v => string.Equals(v.TryGetString()?.Trim(), "Fund Short Name", StringComparison.OrdinalIgnoreCase));
	var fundCols = h.FundRow
		.Select((v, i) => (v, i))
		.Where(x => x.i > labelIdx && x.v.HasValue)
		.Select(x => x.i)
		.ToArray();

	return new
	{
		Entity = h.Entity,
		Funds = fundCols.Select(c => new { Code = h.FundRow[c].GetString(), Pct = h.PctRow[c].GetDouble() }).ToArray(),
		FederalTI = h.TiRow[fed].GetDecimal(),
		FundTI = fundCols.Select(c => h.TiRow[c].GetDecimal()).ToArray(),
		LineItems = rows
			.Where(r => r[atax].HasValue)
			.Select(r => new
			{
				Dt = Code(r[dt]),
				Atax = Code(r[atax]),
				Label = r[atax + 1].TryGetString() ?? "",
				Federal = r[fed].TryGetDecimal(),
				FundAmounts = fundCols.Select(c => r[c].TryGetDecimal()).ToArray(),
			})
			.ToArray(),
	};
});

var result = report.Map(space);

// Post-parse validation: the workbook's own semantics, checked from outside.
new
{
	FundCount = result.Funds.Length,
	PctSum = result.Funds.Sum(f => f.Pct),
	FederalTI = result.FederalTI,
	FundTISumsExactly = result.FundTI.Sum() == result.FederalTI,
	LineItems = result.LineItems.Length,
	AllAllocationsSumToFederal = result.LineItems
		.Where(i => i.Federal is decimal f && f != 0)
		.All(i => i.FundAmounts.Sum(v => v ?? 0m) == i.Federal),
}.Dump("validation");

result.Dump();
