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

// The production posture: this section is best-effort. On a clean file Optional changes
// nothing; on a broken one the import survives with PortfolioItems = null and a Warning
// in the diagnostics citing exactly where and why the section failed.
var portfolio =
	Cells(RowsWhileAnyValue().ToAreaStrategy(), b => b.Rows.Select(r => r.ToArray()).ToArray())
		.After(SeekRowContaining("Portfolio Income")).Named("portfolio income")
		.Optional();

var report = Vertical(header, section, portfolio).Select((h, rows, portfolioRows) =>
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

	// Every coded row across both sections, pivot-neutral.
	var allRows = rows.Concat(portfolioRows ?? Array.Empty<CellValue[]>())
		.Where(r => r[atax].HasValue)
		.ToArray();

	// Fund-centric pivot, legacy-import-style: Federal rides along as a pseudo-fund with
	// ownership 1.0 (so every consumer is uniform), and each fund carries only its
	// non-empty, non-zero line items — sparse, like the legacy cells table.
	var columns = new[] { (Code: "FEDERAL", Pct: 1.0, Col: fed) }
		.Concat(fundCols.Select(c => (Code: h.FundRow[c].GetString(), Pct: h.PctRow[c].GetDouble(), Col: c)))
		.ToArray();

	var funds = columns.Select(f => new
	{
		FundCode = f.Code,
		Percent = f.Pct,
		LineItems = allRows
			.Select(r => new
			{
				Atax = Code(r[atax]),
				Label = r[atax + 1].TryGetString() ?? "",
				Amount = r[f.Col].TryGetDecimal(),
			})
			.Where(i => i.Amount is decimal a && a != 0m)
			.ToArray(),
	}).ToArray();

	return new
	{
		Entity = h.Entity,
		Funds = funds,
		// Cross-region correlation: every FEDERAL line item's amount equals the sum of
		// that line item across the real funds.
		AllAllocationsSumToFederal = funds[0].LineItems.All(fi =>
			funds.Skip(1).SelectMany(f => f.LineItems)
				.Where(i => i.Atax == fi.Atax)
				.Sum(i => i.Amount ?? 0m) == fi.Amount),
	};
});

var mapped = report.MapWithDiagnostics(space);
var result = mapped.Value;

// The unconsumed-space Info doubles as the campaign progress meter: as more of the
// 169 sections get shapes, "rows not described" burns down toward zero.
mapped.Diagnostics.Select(d => d.ToString()).Dump("diagnostics");

// Post-parse validation: the workbook's own semantics, checked from outside.
new
{
	FundCount = result.Funds.Length,                       // 15: FEDERAL + 14 funds
	PctSum = result.Funds.Skip(1).Sum(f => f.Percent),     // real funds sum to 1
	FederalLineItems = result.Funds[0].LineItems.Length,
	result.AllAllocationsSumToFederal,
}.Dump("validation");

result.Dump();
