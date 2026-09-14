# Spreadsheet reader survey — should ExcelDataReader be replaced?

**Status:** decision support for the owner, 2026-09-14. Produced during the Point-substrate design
review after the observation that formulas are read by a second pass over the xlsx zip
(`Formulas/XlsxFormulas.cs`) and only on the eager door. Every API claim below was checked against
the library's docs or source; unverifiable claims are marked.

## Verdict up front

**Do not replace ExcelDataReader.** Two of the three motivating gaps are not reader problems, and the third (streamed formulas) is solved by *no* library on the market.

- **The elapsed-time bug is already fixed** (verified in-tree): `ExcelDataReaderExtensions.cs:40` lexes `TimeSpan value => CellValue.Of(value.TotalDays)`, pinned by `src/Unrect.Tests/Spreadsheets/SpreadsheetSpaceDurationTests.cs`. `CLAUDE.md`'s "Known Bugs" entry is stale and should be struck.
- **`IFormattingSpace` is not blocked on the reader.** ExcelDataReader's own README documents the pairing with **ExcelNumberFormat** (MIT, `net20;netstandard1.0;netstandard2.0`) and ships the helper code. Adding one MIT package gets displayed text on *both* doors today.
- **Streamed formulas: no candidate delivers them.** Every streaming reader evaluated drops `<f>`. The only routes are Unrect's own OOXML reader, or the Open XML SDK's raw SAX reader (which is not a lexer at all).

## 1. Rubric

| | stream (fwd-only) | .xls | .xlsx | .xlsb | formula text in-pass | shared-formula expand | numfmt string | formatter | error cells | date/style | ns2.0 | license | health |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| **ExcelDataReader 3.9.0** (incumbent; 3.7.0 referenced) | yes | BIFF2–8 | yes | yes (+CSV) | **no** | n/a | `GetNumberFormatString(i[,provider])`, `GetNumberFormatIndex(i)` | no (pairs w/ ExcelNumberFormat) | `GetCellError(i)`; **xlsx loses `#SPILL!`/`#CALC!`/`#FIELD!` → blank** | automatic (`GetValue`→`DateTime`/`TimeSpan`) | yes | MIT | 3.9.0 Jun 2026, 34 open, 2 core maintainers |
| **Sylvan.Data.Excel 0.5.8** | yes (`DbDataReader`) | yes | yes | **yes** | **no** | n/a | `GetFormat(i)` → `ExcelFormat.Format`/`.Kind` | `FormatValue` is **internal** + simplified (`"G"`, ISO), not Excel-faithful | `GetExcelDataType`→`Error`, `GetFormulaError(i)`; unknown literal throws `FormatException`, but the cell still types as Error | manual: `ExcelFormat.Kind` / `GetDateTime` | yes | MIT | 0.5.8 Aug 2026, 8 open, **bus factor 1** (178/186 commits) |
| **MiniExcel 1.46.0 / 2.0-preview** | yes (deferred `Query`) | **no** | yes (+CSV) | no | no | n/a | not exposed | no | not exposed | partial | yes | Apache-2.0 | active, 42 open |
| **DocumentFormat.OpenXml 3.5.1** | yes (`OpenXmlPartReader`) | **no** | yes | no | **yes** (raw `<f>`+`<v>` together) | **you write it** | raw `s=` index → styles part | no | raw `t="e"` literal (all of them) | you write it | yes | MIT | active (MS), 124 open |
| **NPOI 2.8.0** SAX (`XSSFSheetXMLHandler`) | yes | via HSSF event model | yes (+`XSSFBReader`) | yes | **formula XOR value**, never both | **"shared formulas not yet supported"** (TODO in source) | via `DataFormatter` only | **yes — `DataFormatter`**, best-in-class | `"ERROR:"+literal` string | inside formatter | yes | Apache-2.0 **but the NuGet binary carries an OSM maintenance-fee EULA** | active, 65 open |
| **ClosedXML 0.105.1** | **no — DOM** | no | yes | no | yes (DOM) | yes (DOM) | yes (DOM) | yes | yes | yes | yes | MIT | active, **471 open** |
| **EPPlus 8.x** | — | — | — | — | — | — | — | — | — | — | — | **PolyForm Noncommercial** — disqualified | — |
| **LargeXlsx 2.0.2** | write-only | — | — | — | — | — | — | — | — | — | yes | — | **not read-capable** |

## 2. Per-library notes

**ExcelDataReader 3.9.0** — MIT, ns2.0/ns2.1/net462/net8. Widest format coverage of anything here (BIFF2–8, xlsx, **xlsb**, CSV — [README supported formats](https://github.com/ExcelDataReader/ExcelDataReader/blob/develop/README.md)). Public surface is exactly what `IExcelDataReader.cs` declares: `GetNumberFormatString(int)`, the 3.9.0 locale overload `GetNumberFormatString(int, IFormatProvider)`, `GetNumberFormatIndex(int)`, `GetCellStyle(int)`, `GetCellError(int)` — [source](https://github.com/ExcelDataReader/ExcelDataReader/blob/develop/src/ExcelDataReader/IExcelDataReader.cs). No formula member, confirming the second-pass design in `Formulas/XlsxFormulas.cs`. Its README §Formatting states outright: *"ExcelDataReader does not support formatting directly… use the third party ExcelNumberFormat library"*, with a worked `GetFormattedValue` sample. Real fidelity defect, already documented at `SpreadsheetSpace.cs:34-45`: modern xlsx error literals arrive **as blank**, which is load-bearing given `AfterBlankRows`/`RowsWhileAnyValue`. Health: [v3.9.0 released 2026-06-16](https://github.com/ExcelDataReader/ExcelDataReader/releases), 4.4k stars, 34 open issues, contributions concentrated in two maintainers — bus factor ~2.

**Sylvan.Data.Excel 0.5.8** — the only candidate that would be a genuine upgrade on several axes, and the only one worth prototyping.
- Formats: *"The most commonly used formats: .xlsx, .xlsb and .xls, are supported for reading"* ([readme](https://github.com/MarkPflug/Sylvan.Data.Excel/blob/main/readme.md)). TFMs `net8.0;net6.0;netstandard2.1;netstandard2.0`, zero deps on ns2.0 ([csproj](https://github.com/MarkPflug/Sylvan.Data.Excel/blob/main/source/Sylvan.Data.Excel/Sylvan.Data.Excel.csproj)). MIT ([license.txt](https://github.com/MarkPflug/Sylvan.Data.Excel/blob/main/license.txt)).
- **Formulas: none.** `XlsxWorkbookReader.cs` reads only `<v>` (`ReadToDescendant(reader, "v")`, line 717); the string "formula" never appears in the xlsx reader, and in `ExcelDataReader.cs` it appears only in error handling. Verified by source, not docs.
- Number format: `public ExcelFormat? GetFormat(int ordinal)` (`ExcelDataReader.cs:770`), exposing `Format` (the string) and `Kind` (`String|Number|Date|Time`) ([ExcelFormat.cs](https://github.com/MarkPflug/Sylvan.Data.Excel/blob/main/source/Sylvan.Data.Excel/ExcelFormat.cs)). **`FormatValue` is `internal`** (`ExcelFormat.cs:216`) and is *not* an Excel-faithful renderer — numbers go through `value.ToString("G", culture)`. So Sylvan does not give displayed text either; it would still be paired with ExcelNumberFormat.
- **Error cells are better than EDR's for Unrect's specific pain.** A `t="e"` cell types as `ExcelDataType.Error` and the literal is parsed lazily in `GetFormulaError` ([XlsxWorkbookReaderAccessors.cs:154](https://github.com/MarkPflug/Sylvan.Data.Excel/blob/main/source/Sylvan.Data.Excel/Xlsx/XlsxWorkbookReaderAccessors.cs)). `ExcelError.GetErrorCode` throws `FormatException` on `#SPILL!`, but the *kind* survives — so a spill cell would lex to `CellError.Other`, **not blank**. That closes the silent-truncation hazard `SpreadsheetSpace.cs` documents. (Cost: no `#GETTING_DATA` in `ExcelErrorCode`, and the literal text is unreachable.)
- **Biggest find: the shared-string table is read lazily and incrementally.** `GetSharedString(idx)` advances an `XmlReader` over `sharedStrings.xml` only as far as the highest index actually referenced (`XlsxWorkbookReader.cs:984-1012`). EDR parses the whole SST at open — precisely the *"~5s open"* documented at `Streaming/IRowSource.cs:40-44` and the reason `ReaderPool` warms and chases. Sylvan would make opens cheap and could shrink or simplify the pool.
- Row/column alignment is compatible with Unrect's absolute-coordinate law: blank interior rows are emitted with `rowFieldCount == 0` (`Read()`, lines 539-600), cells are indexed by their parsed `r=` column (`values[col]`, ~680), `RowNumber => rowIndex + 1`, and `RowFieldCount`/`RowCount`/`WorksheetName`/`NextResult()` cover the whole `IRowCursor` surface.
- Gotchas to re-pin: default schema **consumes row 1 as a header** — set `Schema = ExcelSchema.NoHeaders` ([ExcelSchema.cs:51/57](https://github.com/MarkPflug/Sylvan.Data.Excel/blob/main/source/Sylvan.Data.Excel/ExcelSchema.cs)); `ReadHiddenWorksheets` defaults **false** (sheet indices would shift vs. EDR); `FormulaErrorHandling` defaults to `Exception`; **dates are not auto-detected** — `GetExcelDataType` returns `Numeric` and the caller decides via `GetFormat(i).Kind`, i.e. Unrect takes on the date/duration classification EDR does for it today.
- Health: [0.5.8, 2026-08-10](https://www.nuget.org/packages/Sylvan.Data.Excel), 356 stars, 8 open issues, still 0.x. **Bus factor 1.** That is the decisive risk for a financial institution's core file-ingest path.

**MiniExcel** — Apache-2.0, ns2.0+net461, active ([1.46.0, 2026-08-22](https://www.nuget.org/packages/MiniExcel)). **No .xls reader exists**: the v2 source tree has only `src/MiniExcel.OpenXml` and `src/MiniExcel.Csv`. No formula/format/error surface in `OpenXmlReader.cs`. Fails req. 2 and req. 3. Out.

**DocumentFormat.OpenXml** — MIT, ns2.0, Microsoft-maintained. `OpenXmlPartReader` is a true forward-only SAX reader over a part (`Create(OpenXmlPart)`, `Read()`, `ReadFirstChild()`, `ElementType`, `GetText()`, `LoadCurrentElement()` — [API ref](https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.openxmlreader)). It is the **only** way to get value + type + `<f>` + style index in one pass — because it hands you the XML. But it is not a reader, it's a parser toolkit: shared strings, `s=`→`numFmt` style resolution, date detection, error literals, shared-formula master/follower expansion, inline strings, and the whole xls path would all be Unrect's code. xlsx only.

**NPOI** — two disqualifiers. (a) `XSSFSheetXMLHandler.ISheetContentsHandler.Cell(string cellReference, string formattedValue, XSSFComment comment)` emits **a formatted string only** — the typed value is gone before you see it; and `formulasNotResults` is an either/or switch, with shared formulas explicitly unimplemented (`// logger.log(WARN, "shared formulas not yet supported!")`, [XSSFSheetXMLHandler.cs:244](https://github.com/nissl-lab/npoi/blob/master/ooxml/XSSF/EventUserModel/XSSFSheetXMLHandler.cs)). (b) **Licensing**: repo LICENSE is Apache-2.0, but the NuGet package ships `OSMFEULA.txt` — an *Open Source Maintenance Fee Agreement* applying to users with revenue-generating use and annual gross revenue ≥ US$10,000. That is a fee obligation on the binary release. Worth stealing one idea: NPOI's `SS/UserModel/DataFormatter.cs` is a real Excel-faithful formatter — but ExcelNumberFormat gives the same thing in a 40 KB MIT package.

**ClosedXML** — confirmed fail on streaming. Its own README benchmarks show loading a 1M-row text workbook at **801 MiB / 49 s**; it is a DOM wrapper over the Open XML API with no row-by-row read path. 471 open issues.

**EPPlus v5+** — PolyForm Noncommercial. Commercial use inside a financial institution requires a paid license; not an OSI-approved permissive license. Disqualified, no further evaluation.

**LargeXlsx** — "A .net library to **write** large XLSX files". Not read-capable. Out. (Same for SpreadCheetah.)

## 3. What a Sylvan swap would touch, and what it would unblock

Touched (the EDR surface in-repo is small — 4 files):

- `src/Unrect.Spreadsheets/Streaming/SpreadsheetRowSource.cs` — rewrite the cursor (~124 lines). Mechanical; `IRowCursor` maps 1:1 onto `Read`/`NextResult`/`RowFieldCount`/`RowCount`/`WorksheetName`.
- `src/Unrect.Spreadsheets/ExcelDataReaderExtensions.cs` — **the real work.** Rewrite the lexer: `GetExcelDataType` + `GetFormat(i).Kind` replaces EDR's `GetValue()` type switch, so Unrect owns Number-vs-Temporal-vs-Duration classification and the error-code mapping (including catching `FormatException` from `GetFormulaError` → `CellError.Other`).
- `src/Unrect.Spreadsheets/SpreadsheetSpace.cs` — eager door (`ExcelReaderFactory.CreateReader` at :181, `ReadDeclared`/`ReadMeasured`/`Adapt` at :216-300), plus the "Known limitation" doc block at :34-45.
- `src/Unrect.Spreadsheets/SpreadsheetEncodings.cs` + csproj — re-check whether `System.Text.Encoding.CodePages` is still needed for Sylvan's BIFF path.
- Tests/benches to re-baseline: `SpreadsheetSpaceTests.cs`, `SpreadsheetSpaceEdgeCaseTests.cs`, `SpreadsheetSpaceDurationTests.cs`, `Streaming/CrossDoorDenotationTests.cs`, and the `Streaming`/`Retention` benchmark families (whose prose models EDR's open cost and SST retention — `Streaming.cs:33`, `RetentionSpaces.cs:36`).

Unblocked by the swap: **error cells surviving on xlsx**, **cheap opens** (lazy SST — potentially a structural simplification of `ReaderPool`'s warming), lower allocation (claimed; measure with the existing rig). **Not** unblocked: streamed formulas, displayed text, elapsed time. The swap buys nothing on the three things that prompted the question.

## 4. Recommendation

**Keep ExcelDataReader as the shipped reader.** Then, in priority order:

1. **Add `ExcelNumberFormat` (MIT, ns2.0) and build `IFormattingSpace` on it.** `GetNumberFormatString(i, culture)` is available per cell in the forward pass, so this works on *both* doors with no reader change and no second file pass. Honest cost: a stale dependency — last release 2020, last commit 2024-07-26, 90 stars, single author (also an EDR maintainer). It is small, MIT and vendorable if it ever goes cold. Do not adopt NPOI just for `DataFormatter`.
2. **Strike the elapsed-time entry from CLAUDE.md's Known Bugs** — fixed and pinned.
3. **For streamed formulas, plan a first-party windowed OOXML reader, not a vendor swap.** Unrect already owns 668 lines of it: `Formulas/XlsxFormulas.cs` (zip + sheet XML via `XmlReader` + dimension/rel resolution) and `Formulas/SharedFormulas.cs` (master/follower expansion, cross-checked against openpyxl's `Translator` on 19,452 real followers). What `Workbook.cs:55-63` describes as the missing work — a second windowed XML reader advancing in step with the value reader, with masters pinned past eviction — is an extension of those files, not greenfield. A first-party reader would also end the `#SPILL!`-as-blank defect and make the duration round-trip bit-exact (the caveat at `ExcelDataReaderExtensions.cs:36-39` names this itself).
4. **Do not support both readers behind `IRowSource` permanently.** Two lexers means two fidelity matrices, two blankness stories, and a doubled `CrossDoorDenotationTests` obligation. Instead: **write one throwaway Sylvan `IRowCursor` as a spike** to measure (a) open cost with lazy SST vs. EDR's SST parse on the K-1 file, and (b) live-bytes retention. If lazy opens collapse a meaningful part of `ReaderPool`'s complexity, that is a genuine architectural argument for Sylvan — and the only one. Revisit then, with numbers, weighing it against bus factor 1 on a 0.x package.

## Sources

- ExcelDataReader: [README](https://github.com/ExcelDataReader/ExcelDataReader/blob/develop/README.md) · [IExcelDataReader.cs](https://github.com/ExcelDataReader/ExcelDataReader/blob/develop/src/ExcelDataReader/IExcelDataReader.cs) · [releases](https://github.com/ExcelDataReader/ExcelDataReader/releases)
- Sylvan.Data.Excel: [readme](https://github.com/MarkPflug/Sylvan.Data.Excel/blob/main/readme.md) · [ExcelDataReader.cs](https://github.com/MarkPflug/Sylvan.Data.Excel/blob/main/source/Sylvan.Data.Excel/ExcelDataReader.cs) · [ExcelFormat.cs](https://github.com/MarkPflug/Sylvan.Data.Excel/blob/main/source/Sylvan.Data.Excel/ExcelFormat.cs) · [XlsxWorkbookReader.cs](https://github.com/MarkPflug/Sylvan.Data.Excel/blob/main/source/Sylvan.Data.Excel/Xlsx/XlsxWorkbookReader.cs) · [XlsxWorkbookReaderAccessors.cs](https://github.com/MarkPflug/Sylvan.Data.Excel/blob/main/source/Sylvan.Data.Excel/Xlsx/XlsxWorkbookReaderAccessors.cs) · [ExcelSchema.cs](https://github.com/MarkPflug/Sylvan.Data.Excel/blob/main/source/Sylvan.Data.Excel/ExcelSchema.cs) · [docs/Schema.md](https://github.com/MarkPflug/Sylvan.Data.Excel/blob/main/docs/Schema.md) · [NuGet](https://www.nuget.org/packages/Sylvan.Data.Excel)
- [ExcelNumberFormat](https://github.com/andersnm/ExcelNumberFormat) · [NuGet](https://www.nuget.org/packages/ExcelNumberFormat)
- [MiniExcel](https://github.com/mini-software/MiniExcel) · [NuGet](https://www.nuget.org/packages/MiniExcel)
- [OpenXmlReader API reference](https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.openxmlreader) · [Open-XML-SDK](https://github.com/dotnet/Open-XML-SDK)
- NPOI: [XSSFSheetXMLHandler.cs](https://github.com/nissl-lab/npoi/blob/master/ooxml/XSSF/EventUserModel/XSSFSheetXMLHandler.cs) · [OSMFEULA.txt](https://github.com/nissl-lab/npoi/blob/master/OSMFEULA.txt) · [NuGet](https://www.nuget.org/packages/NPOI)
- [ClosedXML README](https://github.com/ClosedXML/ClosedXML/blob/develop/README.md)
- [LargeXlsx README](https://github.com/salvois/LargeXlsx/blob/master/README.md) · [EPPlus on NuGet](https://www.nuget.org/packages/EPPlus)
