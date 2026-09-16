<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Core\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Core\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Namespace>static Unrect.Projections.ProjectionBuilders&lt;Unrect.Spreadsheets.ISheetCells&gt;</Namespace>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Projections</Namespace>
  <Namespace>Unrect.Spreadsheets</Namespace>
</Query>

// NOTE: examples/scrubbed-k1.xlsx is a LOCAL-ONLY fixture (gitignored, never committed).
//
// ONE root projection, ZERO hard-coded coordinates. The space is named once, in the query's
// namespace imports: `using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>` —
// this file declares nothing that asserts a kind, so it takes only the canonical vocabulary; the
// kinded READS below (.Text(), .Double(), .DecimalOrBlank()) are extensions on a point over a sheet,
// and come with the `Unrect.Spreadsheets` namespace import rather than with a second builder class.
// The working style that survives real-world drift (extra rows, moved columns, varying fund
// counts):
//   - rows anchor by content matchers, written as the pipeline's entry: On(RowContaining(...));
//   - the header is an Overlay — independent blocks sharing rows, placement rather than flow —
//     bounded with Sized so every seek inside it is unambiguous;
//   - each layout lambda DIGESTS ITSELF: the header resolves its own columns from content and
//     hands back what the rest of the declaration needs, so no raw cells travel any further;
//   - a section announces itself with Heading, which finds the row, asserts the text and consumes
//     it — the row belongs to the section instead of being swallowed by an anchor's offset;
//   - the entity card is a Fields block: labels declared once, extent from the child count, and
//     the block finds itself by its own first label;
//   - one `section` projection, declared once and placed twice under two different headings.
var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\scrubbed-k1.xlsx");
var space = SpreadsheetSpace.Create(path, "Sheet1");

// A cell comes back as a place, and the reading is written at it: an ATAX code is either the words
// in a text cell or a whole number, and the cell's own kind says which.
string Code(Point<ISheetCells> cell) => cell.IsText ? cell.Text() : cell.IntegerOrBlank()?.ToString() ?? "";

int Find(Point<ISheetCells>[] row, string caption) => Array.FindIndex(row,
	cell => cell.IsText && string.Equals(cell.Text().Trim(), caption, StringComparison.OrdinalIgnoreCase));

// A full-width single row anchored by a content seek. AllColumns() is the declared spelling of
// "the whole width" — Row's default discovers its width and would stop at the first gap, and a
// caption band has gaps. The helper does NOT name what it returns: a name baked in here would call
// every row the same thing at every use site, and the use site is the only place that knows which
// row this is.
IProjection<ISheetCells, Point<ISheetCells>[]> FullRow(string anchor) =>
	On(RowContaining(anchor))
		.Row(AllColumns(), r => r.ToArray());

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

// Four rows below the same anchor. .Of places a declaration made elsewhere, so the movement reads
// before the row it moves, and the helper stays the one place the anchoring is written.
var ownershipRow = Down(4).Of(FullRow("Fund Short Name"));

// The header reads four independent blocks off the same band of rows and resolves the sheet's
// column layout from them, so what leaves here is the answer, not the evidence. It is bounded, so
// every seek inside stays unambiguous — and the bound is declared where all geometry is, ahead of
// the shape it places: Sized is the entry for an extent with no movement to its left.
var header = Sized(RowsWhileAnyValue()).Of(Overlay(o =>
{
	// Fields hands back each label's cell as a place; AsText is the total reading, so the card
	// leaves here as what it says rather than as five addresses for someone else to read.
	var entityFields = o.Next(entity).ToDictionary(f => f.Key, f => f.Value.AsText() ?? "");
	var captions = o.Next(captionRow);
	var fundNames = o.Next(fundNameRow);
	var ownership = o.Next(ownershipRow);

	var label = Find(fundNames, "Fund Short Name");

	// Federal rides along as a pseudo-fund at 100% so every consumer downstream is uniform.
	var columns = new[] { (Code: "FEDERAL", Percent: 1.0, Column: Find(captions, "Federal")) }
		.Concat(fundNames
			.Select((cell, i) => (Cell: cell, Index: i))
			.Where(x => x.Index > label && x.Cell.HasValue)
			.Select(x => (Code: x.Cell.Text(), Percent: ownership[x.Index].Double(), Column: x.Index)))
		.ToArray();

	return new { Entity = entityFields, AtaxColumn = Find(captions, "ATAX"), Columns = columns };
}));

// One section projection: rows while any value, wherever it is anchored.
var section = Range(RowsWhileAnyValue(), b => b.Rows.Select(r => r.ToArray()).ToArray());

var k1Lines = Heading("K-1 Lines 1-21").Of(section);

// The production posture: this section is best-effort. On a clean file Optional changes nothing;
// on a broken one the import survives with null here and a Warning in the diagnostics citing
// exactly where and why the section failed.
var portfolio = Heading("Portfolio Income").Of(section).Optional();

var report = VerticalFlow(v =>
{
	var head = v.Next(header);
	var k1Rows = v.Next(k1Lines);
	var portfolioRows = v.Next(portfolio);

	// Every coded row across both sections, pivot-neutral.
	var allRows = k1Rows.Concat(portfolioRows ?? Array.Empty<Point<ISheetCells>[]>())
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
				Label = r[head.AtaxColumn + 1].TextOrBlank() ?? "",
				Amount = r[f.Column].DecimalOrBlank(),
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
// 169 sections get projections, "rows not described" burns down toward zero.
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
