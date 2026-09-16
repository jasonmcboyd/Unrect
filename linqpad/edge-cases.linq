<Query Kind="Statements">
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Core.dll</Reference>
  <Reference Relative="..\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Spreadsheets\bin\Debug\netstandard2.1\Unrect.Spreadsheets.dll</Reference>
  <Reference Relative="..\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect\bin\Debug\netstandard2.1\Unrect.dll</Reference>
  <Reference Relative="..\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Strategies\bin\Debug\netstandard2.1\Unrect.Strategies.dll</Reference>
  <Reference Relative="..\src\Unrect.Interactive\bin\Debug\netstandard2.1\Unrect.Interactive.dll">&lt;UserProfile&gt;\source\repos\Unrect\src\Unrect.Interactive\bin\Debug\netstandard2.1\Unrect.Interactive.dll</Reference>
  <Namespace>Unrect.Core</Namespace>
  <Namespace>Unrect.Spreadsheets</Namespace>
  <Namespace>Unrect.Projections</Namespace>
  <Namespace>Unrect.Interactive</Namespace>
  <Namespace>static Unrect.Projections.ProjectionBuilders&lt;Unrect.Spreadsheets.ISheetCells&gt;</Namespace>
  <Namespace>static Unrect.Spreadsheets.SheetProjectionBuilders&lt;Unrect.Spreadsheets.ISheetCells&gt;</Namespace>
</Query>

// The space is named once, in the query's namespace imports: the canonical vocabulary as
// `using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>`, and the
// sheet's own readings as `using static Unrect.Spreadsheets.SheetProjectionBuilders<...>`. That
// split IS this script's subject: every space answers the canonical questions — how big it is,
// whether a cell is blank, whether a cell's text is its own value, and what a cell says — while a
// kind is a claim only a sheet makes about its own cells, which is why the readings that assert one
// ship with the sheet.
var path = Path.Combine(Path.GetDirectoryName(Util.CurrentQueryPath)!, @"..\examples\edge-cases.xlsx");

// The corner-case fixture (distilled from the real K-1 workbook):
//   row 1: one of each ordinary kind      row 2: five error cells
//   row 3: whitespace / empty / absent    row 4: the remaining two errors
var defaultSpace = SpreadsheetSpace.Create(path, "Edges");
var strictSpace = SpreadsheetSpace.Create(path, "Edges", isBlank: _ => false);

// A space has no indexer: the locator does the addressing. A plane is the 2-D one, and asking it for
// a cell mints a point — an address, not a value, which is what every read below is written at.
// `space.At(column, row)` is Unrect.Interactive's spelling of exactly that (the whole sheet as one
// plane, which is what checks the coordinate), and it is a script's tool: inside a declaration the
// Point() leaf hands back the same thing without anyone naming a coordinate.

// 1. The kind map — errors are first-class, never blank. Describe is the document's own vocabulary
// for a cell, which is the vocabulary a complaint about one is written in.
Range(5, 4, b => Enumerable.Range(0, 4)
		.Select(r => Enumerable.Range(0, 5).Select(c => b[c, r].Describe()).ToArray())
		.ToArray())
	.Map(defaultSpace)
	.Dump("cell kinds (default blankness)");

// 2. An error cell, end to end. It is not blank and it says what the file says — but no kind agrees
// with it, so every kinded read refuses in the document's words and cites the cell in A1.
var err = defaultSpace.At(0, 1);
new
{
	Describe = err.Describe(),
	IsError = err.IsError(),
	ErrorText = err.ErrorText(),
	AsText = err.AsText(),
	err.IsBlank,
	err.IsText,
	DecimalRefuses = ((Func<string>)(() => { try { err.Decimal(); return "no"; } catch (CellReadException ex) { return ex.Message; } }))(),
}.Dump("the #VALUE! cell");

// 3. Blankness belongs to the adapter: the same whitespace row under both rules. IsText separates
// a cell whose text is its own value from one that merely renders — the distinction the canonical
// surface is built on.
string Say(Point<ISheetCells> p) => $"{p.Describe()}, says {p.AsText() ?? "null"}, IsBlank={p.IsBlank}, IsText={p.IsText}";
new
{
	TwoSpaces_Default = Say(defaultSpace.At(0, 2)),
	TwoSpaces_Strict = Say(strictSpace.At(0, 2)),
	EmptyString_Strict = Say(strictSpace.At(2, 2)),   // "" maps to Blank before the predicate — the fidelity floor
	AbsentCell_Strict = Say(strictSpace.At(3, 2)),
}.Dump("whitespace vs empty vs absent");

// 4. And it changes decomposition: the value-bearing block over cols A-D stops at the whitespace row
// by default, but includes it under strict fidelity. The window is declared, not sliced off the
// space: an overlay hands its whole extent to its child, so the Range keeps its own discovery and
// only has less to discover in. (Sized ON the Range would replace that discovery with the 4x4.)
var block = Sized(Extent(4, 4)).Of(Overlay(o => o.Next(Range(b => $"{b.Width}x{b.Height}"))));
new
{
	Default = block.Map(defaultSpace),
	Strict = block.Map(strictSpace),
}.Dump("discovered extent, cols A-D");

// 5. Typed leaves speak the document's vocabulary: kinds for a kind mismatch, conversions for a
// number that will not fit. Note that the error cell is reported as the Error it is, never as
// "blank" — and that the sentence changes entirely when the number is genuinely there.
string Message<T>(IProjection<ISheetCells, T> projection)
{
	try { projection.Map(defaultSpace); return "no failure"; }
	catch (ProjectionException failure) { return failure.Message.Split('\n')[0].TrimEnd('\r'); }
}

// Where the cell is, then what to read there: OffsetBy is the pipeline's strategy door, and the
// leaf that closes it is the subject of the sentence.
new
{
	DecimalOverAnError = Message(OffsetBy(SkipRows(1)).Decimal()),      // A2 is #VALUE!
	TextOverANumber = Message(OffsetBy(SkipColumns(1)).Text()),         // B1 is 42
	IntegerOverAFraction = Message(OffsetBy(SkipColumns(2)).Integer()), // C1 is 3.14
	AsTextOverTheSameNumber = Message(OffsetBy(SkipColumns(1)).AsText()),
}.Dump("typed-leaf diagnostics");

// 6. The two leaves are different words for a reason. Text() asserts the cell IS words, so a Choice
// of kinded leaves discriminates — the first alternative the cell agrees with wins. AsText() asserts
// nothing and therefore never fails over a cell that is there, so a Choice led by it can never reach
// a second alternative: total, and degenerate as an alternative.
var wordsOrNumber = Choice(
	Text().Select(t => $"text: {t}"),
	Decimal().Select(d => $"number: {d}"));
new
{
	AtA1 = wordsOrNumber.Map(defaultSpace),                                 // the word "text"
	AtB1 = OffsetBy(SkipColumns(1)).Of(wordsOrNumber).Map(defaultSpace),    // the number 42
	AsTextAtB1 = OffsetBy(SkipColumns(1)).AsText().Map(defaultSpace),       // "42", asserting nothing
}.Dump("a kind assertion discriminates; a total reading cannot");

// 7. The same split, one level up, in the matchers. RowContaining asks the text cells, because a row
// found by its caption is found by what someone typed. RowSaying asks every cell what it says, so it
// finds the row whose 42 is a number — the opt-in widening, marked at the use site.
new
{
	ContainingANumericFortyTwo = Message(On(RowContaining("42")).AsText()),                // no text cell says it
	SayingFindsThatRow = On(RowSaying("42")).Row(r => r.Location.A1).Map(defaultSpace),    // row 1, where B1 is the number
}.Dump("RowContaining is about text; RowSaying is about what a cell says");
