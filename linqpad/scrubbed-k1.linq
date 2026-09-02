<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Spreadsheets</Namespace>
  <Namespace>Unrect.Shapes</Namespace>
  <Namespace>static Unrect.Shapes.Shape</Namespace>
</Query>

// NOTE: examples/scrubbed-k1.xlsx is a LOCAL-ONLY fixture (gitignored, never committed).
//
// ONE root shape, ZERO hard-coded coordinates. The working style that survives real-world
// drift (extra rows, moved columns, varying fund counts):
//   - rows anchor by content matchers (To(RowContaining(...)));
//   - the header is an Overlay — independent blocks sharing rows, placement rather than flow —
//     bounded with .Sized so every seek inside it is unambiguous;
//   - each layout lambda DIGESTS ITSELF: the header resolves its own columns from content and
//     hands back what the rest of the declaration needs, so no raw rows travel any further;
//   - a section's caption is declared with Caption and placed with Under, so the row that
//     announces the section belongs to it instead of being swallowed by an anchor's offset;
//   - the entity card is a Fields block: labels declared once, extent from the child count, and
//     the block finds itself by its own first label;
//   - one `section` shape, declared once and placed twice under two different captions.
var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\scrubbed-k1.xlsx");
var space = SpreadsheetSpace.Create(path, "Sheet1");

string Code(CellValue v) => v.TryGetString() ?? v.TryGetInt()?.ToString() ?? "";

int Find(CellValue[] row, string caption) => Array.FindIndex(row,
	v => string.Equals(v.TryGetString()?.Trim(), caption, StringComparison.OrdinalIgnoreCase));

// A full-width single row anchored by a content seek. AllColumns() is the declared spelling of
// "the whole width" — Row's default discovers its width and would stop at the first gap, and a
// caption band has gaps. The helper does NOT name what it returns: a name baked in here would call
// every row the same thing at every use site, and the use site is the only place that knows which
// row this is.
IShape<CellValue[]> FullRow(string anchor) =>
	Row(AllColumns(), r => r.ToArray())
		.After(To(RowContaining(anchor)));

// The entity card: five labels, and nothing else. The block's extent comes from the child count
// (no 2, 5 to get wrong), it anchors itself on its first label instead of repeating that literal in
// a second vocabulary, and the label rule absorbs the trailing colon that two of these five carry —
// so TrimEnd(':') is gone and the keys are the labels as written here.
var entity = Fields(
	Field("EIN"),
	Field("Entity Type"),
	Field("Deal Type"),
	Field("State Sourced Income"),
	Field("Underlying CFC(s)/PFIC(s)"));

var captionRow = FullRow("ATAX");
var fundNameRow = FullRow("Fund Short Name");
var ownershipRow = FullRow("Fund Short Name").Down(4);

// The header reads four independent blocks off the same band of rows and resolves the sheet's
// column layout from them, so what leaves here is the answer, not the evidence.
var header = Overlay(o =>
{
	var entityFields = o.Next(entity);
	var captions = o.Next(captionRow);
	var fundNames = o.Next(fundNameRow);
	var ownership = o.Next(ownershipRow);

	var label = Find(fundNames, "Fund Short Name");

	// Federal rides along as a pseudo-fund at 100% so every consumer downstream is uniform.
	var columns = new[] { (Code: "FEDERAL", Percent: 1.0, Column: Find(captions, "Federal")) }
		.Concat(fundNames
			.Select((v, i) => (Value: v, Index: i))
			.Where(x => x.Index > label && x.Value.HasValue)
			.Select(x => (Code: x.Value.GetString(), Percent: ownership[x.Index].GetDouble(), Column: x.Index)))
		.ToArray();

	return new { Entity = entityFields, AtaxColumn = Find(captions, "ATAX"), Columns = columns };
})
	.Sized(RowsWhileAnyValue());   // bounded: seeks inside stay unambiguous

// One section shape: rows while any value, wherever it is anchored.
var section = Range(RowsWhileAnyValue(), b => b.Rows.Select(r => r.ToArray()).ToArray());

var k1Lines = section.Under(Caption("K-1 Lines 1-21"));

// The production posture: this section is best-effort. On a clean file Optional changes nothing;
// on a broken one the import survives with null here and a Warning in the diagnostics citing
// exactly where and why the section failed.
var portfolio = section.Under(Caption("Portfolio Income")).Optional();

var report = VerticalFlow(v =>
{
	var head = v.Next(header);
	var k1Rows = v.Next(k1Lines);
	var portfolioRows = v.Next(portfolio);

	// Every coded row across both sections, pivot-neutral.
	var allRows = k1Rows.Concat(portfolioRows ?? Array.Empty<CellValue[]>())
		.Where(r => r[head.AtaxColumn].HasValue)
		.ToArray();

	// Fund-centric pivot, legacy-import-style: each fund carries only its non-empty, non-zero
	// line items — sparse, like the legacy cells table.
	var funds = head.Columns.Select(f => new
	{
		FundCode = f.Code,
		Percent = f.Percent,
		LineItems = allRows
			.Select(r => new
			{
				Atax = Code(r[head.AtaxColumn]),
				Label = r[head.AtaxColumn + 1].TryGetString() ?? "",
				Amount = r[f.Column].TryGetDecimal(),
			})
			.Where(i => i.Amount is decimal a && a != 0m)
			.ToArray(),
	}).ToArray();

	return new
	{
		Entity = head.Entity,
		Funds = funds,
		// Cross-region correlation: every FEDERAL line item's amount equals the sum of that
		// line item across the real funds.
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
