# Vendor Cell-Type Survey and Canonical-Model Audit

> Written while the adapter package was named `Unrect.Excel`; it is `Unrect.Spreadsheets`
> since the rename, and the references below have been updated to match the code.

**Status:** Research/audit. No code changed. Written 2026-09-02, before the first NuGet
publish of `Unrect` (which bundles `Unrect.Core`, hence `CellKind`/`CellError`) and
`Unrect.Spreadsheets`.
**Purpose:** verify the canonical cell vocabulary against the primitive type systems of
every spreadsheet vendor/format Unrect could plausibly adapt, *while the contract can
still change*, and leave behind a per-vendor lexing crib sheet so that every future
adapter lexes identically.
**Governing rule under test** (`canonical-model-and-shapes.md` §2): *the canonical model
captures only distinctions the source formats reliably make; anything finer is a
consumer-side conversion requested through an accessor. Adapters must never guess.*

---

## 0. Verdict

The position offered for the audit was:

> "The six kinds survive contact with every vendor; the deltas are error-vocabulary
> extensibility and the duration question."

**Both halves hold, but the duration half resolves the opposite way from how it reads.**

1. **The six kinds survive.** Every cell primitive in OOXML, BIFF, ODF, Google Sheets,
   Apple Numbers, Gnumeric and CSV lands in `Blank | Text | Number | Temporal | Boolean |
   Error` without a lie. No seventh kind is warranted. (§4, §5)
2. **The error vocabulary is genuinely under-specified and must change before publish.**
   `CellError` enumerates the eight codes of the *binary* formats. The live Excel error
   vocabulary is **eighteen** types and growing, none of the modern ones have a binary
   code, and every adapter that meets one today either throws or lies. This is the one
   real contract defect. (§6, §8.1, §8.2)
3. **Duration is *not* a missing kind — it is a missing lexing rule plus a live adapter
   bug.** ODS and Apple Numbers do have a first-class duration primitive; Excel, Google
   Sheets, Gnumeric and CSV do not (elapsed time is a *number format* over a number).
   Two of six vendors ⇒ the distinction is not "reliably made" ⇒ by the governing rule it
   does not earn a kind. The correct canonical lex is `Number`, in **days**, because
   days-as-a-double is Excel's own representation and therefore the poorest-common
   denominator. But `Unrect.Spreadsheets` currently **throws** on such cells, which makes a
   perfectly ordinary timesheet workbook unparseable. (§7, §8.3)
4. A fourth finding fell out that was not on the brief: **`CellValue`'s exact-decimal
   slot is dead on the Excel path** — ExcelDataReader hands us a `double`, so
   `ExactNumber` is always null for a workbook. Nothing to fix now; it is an argument for
   the native-payload seam and for a future first-party OOXML reader. (§4.1)

---

## 1. The contract under audit

```
CellKind  : Blank, Text, Number, Temporal, Boolean, Error
CellError : Null, DivisionByZero, Value, Reference, Name, Number, NotAvailable, GettingData
```

`CellValue` carries: `string? Text`, `double Number` + `decimal? ExactNumber`,
`DateTime Temporal`, `bool Boolean`, `CellError Error`. Accessors are checked and
granular (`GetDouble`/`GetDecimal`/`GetInt`, `GetDateTime`/`GetDate`). Blankness is
decided by the adapter at construction time; `Blank` is a kind, so strategies test
`IsBlank`/`HasValue`.

Files: `src/Unrect.Core/CellKind.cs`, `src/Unrect.Core/CellValue.cs`,
`src/Unrect.Core/CellError.cs`, `src/Unrect.Spreadsheets/ExcelDataReaderExtensions.cs`.

Both `Unrect` and `Unrect.Spreadsheets` are `IsPackable` (`src/Directory.Build.props`), so all
of the above is about to become a public contract.

---

## 2. Survey: what each vendor's cell type system actually is

### 2.1 Excel, OOXML (`.xlsx`, `.xlsm`, `.xlsb`) — ECMA-376 / ISO 29500

The cell element `<c>` carries a `t` attribute of type `ST_CellType`. ECMA-376 1st
edition defines **six** values:

| `t` | Meaning (spec wording) |
|---|---|
| `n` | Cell containing a number *(the default when `t` is absent)* |
| `s` | Cell containing a shared string (index into `sharedStrings.xml`) |
| `str` | Cell containing a formula string (cached string result) |
| `inlineStr` | Cell containing an (inline) rich string, i.e. one not in the shared string table |
| `b` | Cell containing a boolean |
| `e` | Cell containing an error |

ISO 29500 / Office 2010 added a seventh, `d` — an ISO 8601 date — documented by the
Open XML SDK as *"only available in Office 2010 and later"*. Microsoft's own
implementation notes are blunt about how little of this Excel writes: *"Excel limits the
values of this attribute to be `b`, `e`, `inlineStr`, or `n`"* ([MS-OE376] Part 4
§3.11.1.3). In practice you will also see `s` and `str`; you will essentially never see
`d`.

**There is no date/time type.** A date is `t="n"` (or no `t`) whose *style* points at a
date `numFmt`. Everything temporal — date, datetime, time-of-day, elapsed duration — is
one `double` serial plus a format string. Consequences worth naming:

- The serial epoch is 1899-12-30 in the 1900 date system, and the system is workbook-wide
  (`date1904`). Serial 60 is the fictitious 1900-02-29 (the inherited Lotus leap-year
  bug).
- A *time-of-day* cell is a serial in `[0,1)`; converting it to a `DateTime` necessarily
  invents the date part (1899-12-30, or 1904-01-01 under the 1904 system).
- An *elapsed time* cell (`[h]:mm:ss` — built-in `numFmt` id **46** in ECMA-376's
  standard set, id **79** in the extended set ExcelDataReader also classifies as elapsed,
  or any custom format with a bracketed elapsed section) is an ordinary number of days. It may
  exceed 1 and may be negative. Excel has no duration type; the brackets are a format.

**Numbers** are stored in the XML as decimal *strings*. Excel's own precision ceiling is
15 significant digits, but the file itself is lossless text; any precision loss is the
reader's.

**Errors** are stored as the literal display string inside `<v>`, e.g.
`<c r="A1" t="e"><v>#DIV/0!</v></c>`. ECMA-376 does not enumerate the permitted
literals. The only normative enumeration anywhere in the family is the binary one
([MS-XLSB] §2.5.98.2 `BErr`), which has exactly eight entries:

| Code | Literal | Code | Literal |
|---|---|---|---|
| 0x00 | `#NULL!` | 0x1D | `#NAME?` |
| 0x07 | `#DIV/0!` | 0x24 | `#NUM!` |
| 0x0F | `#VALUE!` | 0x2A | `#N/A` |
| 0x17 | `#REF!` | 0x2B | `#GETTING_DATA` |

Modern Excel's error vocabulary is much larger. `Excel.ErrorCellValueType` (Office JS,
ExcelApi 1.16 and later) enumerates **eighteen**:

`blocked`, `busy`, `calc`, `connect`, `div0`, `external`, `field`, `gettingData`,
`name`, `notAvailable`, `null`, `num`, `placeholder`, `ref`, `spill`, `value`
(all GA in 1.16), plus `python` and `timeout` (preview).

Microsoft's user-facing docs additionally name `#UNKNOWN!` — *"your version of Excel
doesn't support Python in Excel"* — a literal with no member in the JS enum at all. So
the modern literals are at least: `#SPILL!`, `#CALC!`, `#FIELD!`, `#BLOCKED!`,
`#CONNECT!`, `#BUSY!`, `#EXTERNAL!`, `#PYTHON!`, `#TIMEOUT!`, `#UNKNOWN!`
(`Excel.ExternalErrorCellValue.basicValue` is typed `"#EXTERNAL!" | string`, and each
`*ErrorCellValue` interface carries its literal the same way).

**One member of that enum is not literal-distinguishable and therefore not adaptable.**
`Excel.PlaceholderErrorCellValue` — *"the value of a cell containing a `#BUSY!` error …
used as a placeholder while the value of a cell is downloaded"* — has
`basicValue?: "#BUSY!" | string`, the *same* literal as `BusyErrorCellValue`. The
distinction exists only in the live object model (a `target` property naming the
downloading entity), never in a saved file. An adapter reading `#BUSY!` therefore cannot
tell `Busy` from `Placeholder` without guessing, which settles whether `Placeholder`
belongs in `CellError`: it does not. (§8.1)

That these really are written into `.xlsx` as literals is corroborated by LibreOffice's
OOXML import filter, which maps error strings to internal codes and includes
`#SPILL!` alongside the classic seven (`sc/source/filter/oox/unitconverter.cxx`), and by
`ScXMLTableRowCellContext`'s note that an error cell "has a constant error value
beginning with `#`". Note also LibreOffice's fallback:
`calcBiffErrorCode` returns `BIFF_ERR_NA` for anything it does not recognise — i.e. a
mature, widely-used implementation **silently reports unknown errors as `#N/A`**. That is
the failure mode Unrect must not reproduce.

**Rich / linked data types (Stocks, Geography, entities, images).** These live in
`xl/richData/*` parts and are attached to a cell through the `vm` (value metadata) index;
the cell's own `<v>` is a *fallback*. Microsoft documents the fallback precisely:
`Excel.EntityCellValue.basicValue` is typed `"#VALUE!" | string` and `basicType` is
typed `RangeValueType.error`. **A value-level reader sees a linked-data-type cell as a
`#VALUE!` error.** `Excel.CellValueType` — Microsoft's modern cell vocabulary — is
`array`, `boolean`, `double`, `empty`, `entity`, `error`, `externalCodeServiceObject`,
`formattedNumber`, `function`, `linkedEntity`, `localImage`, `notAvailable`, `reference`,
`string`, `webImage`. Two observations matter for us: there is **still no temporal type**
(dates are `double`/`formattedNumber`), and `notAvailable` exists as an explicit
*"this API version cannot express this cell's type"* escape hatch — Microsoft's own
precedent for a catch-all member.

**Dynamic arrays.** A spilled range is ordinary cells; the anchor carries `cm` (cell
metadata). Nothing new at the value level. When a spill is blocked the anchor holds
`#SPILL!` — i.e. dynamic arrays reach the type system only through the error vocabulary.

**What ExcelDataReader can surface.** `ExcelDataReader.CellError` is exactly the eight
`BErr` values, so `Unrect.Spreadsheets`'s adaptation is currently total *over what the reader
offers*. Everything above that is invisible, and two behaviours are load-bearing:

- `XmlWorksheetReader.ConvertError` returns `null` for any literal outside the eight, and
  sets `value = null` at the same time. The cell therefore reaches us with
  `GetValue() == null` and `GetCellError() == null` — **indistinguishable from an empty
  cell**. A `#SPILL!` becomes `CellValue.Blank`. (§8.2)
- `XlsWorksheet` casts the raw BIFF byte straight to the enum
  (`(CellError)cell.ReadByte(6)`), so a malformed or future `.xls` can yield an undefined
  enum value, which `Unrect.Spreadsheets` turns into a thrown `InvalidOperationException`.
- `XlsxWorksheet.ConvertCellValue` consults the number format and returns
  `DateTime` for date formats, `TimeSpan` for elapsed formats
  (`TimeSpan.FromDays(number)`), and `double` otherwise. `Unrect.Spreadsheets` throws on
  `TimeSpan`.
- The `d` cell type is parsed with `DateTime.TryParseExact(rawValue, "yyyy-MM-dd", …)`;
  any other ISO 8601 form falls through to the raw string, i.e. would arrive as **Text**.
- Booleans are decided by `rawValue == "1"`, so a conforming writer emitting `true`
  would be read as `false`.

### 2.2 Excel, legacy BIFF (`.xls`, BIFF2–BIFF8)

BIFF is where the eight error codes come from; `BoolErr` records carry a bool-or-error
discriminator, and formula cells cache a number, string, boolean, error or blank.
Nothing in BIFF is *richer* than OOXML at the value level — it is poorer:

- Same "no date type" story; same serial epoch and 1904 flag.
- Numbers are `RK`-encoded or IEEE doubles — a genuine binary double, so unlike xlsx the
  original decimal text does not exist to be recovered.
- Strings are byte- or UTF-16 encoded with a code page; the adapter must not lose that,
  but the *kind* is unchanged.
- No modern errors, no rich data, no `d` type.

**Delta versus modern: none that affects the kind set.** BIFF only tightens the case for
`Number` + `Temporal` + the eight classic errors being the true poorest common
denominator.

### 2.3 OpenDocument (`.ods` — LibreOffice / OpenOffice / Collabora)

ODF is the *only* surveyed format with a genuinely richer cell type system than Excel.
`office:value-type` is drawn from a closed list of seven (ODF 1.3 schema, `valueType`):

`float`, `time`, `date`, `percentage`, `currency`, `boolean`, `string`.

ODF 1.3 part 3 §19.389 Table 14 gives the encodings verbatim:

| Value type | Value attribute(s) | Encoded as | Example |
|---|---|---|---|
| `boolean` | `office:boolean-value` | true or false | `"true"` |
| `currency` | `office:value` **and** `office:currency` | Numeric value and currency symbol | `"100"` `"USD"` |
| `date` | `office:date-value` | xsd:date **or** xsd:dateTime | `"2003-04-17"` |
| `float` | `office:value` | Numeric value | `"12.345"` |
| `percentage` | `office:value` | Numeric value | `"0.50"` |
| `string` | `office:string-value` | Strings | `"abc def"` |
| `time` | `office:time-value` | **Duration**, per §3.2.6 of [xmlschema-2] | `"PT03H30M00S"` |
| `void` | none | — | — |

Four things follow that no other format forces on us:

1. **Percentage and currency are value types, not formats.** But their *payload* is a
   plain number, and the percentage payload is the fraction (`"0.50"` for 50%) — exactly
   Excel's convention. So the extra information is (a) "this is a percentage" and (b) the
   ISO 4217 currency string. Both are semantics about presentation, not about the number.
2. **`time` is `xsd:duration`, not a clock time.** This is the single most important
   finding of the ODS section, and it is the spec's own word ("Duration"). A duration may
   exceed 24 hours (`PT26H30M` is legal) and may be negative. It cannot be represented as
   a `DateTime` without lying. Independent corroboration: calamine models ODS cells as
   `Data::DurationIso` for `office:time-value` and `Data::DateTimeIso` for
   `office:date-value` — two distinct variants (`src/ods.rs`).
3. **Dates are ISO strings, not serials**, and `dateOrDateTime` permits `xsd:dateTime`,
   which admits a **timezone offset**. ODF can therefore carry a temporal fact that
   `DateTime` cannot hold.
4. **ODF has no error value type.** LibreOffice writes errors as
   `office:value-type="string"` plus the extension attribute `calcext:value-type="error"`,
   with the literal in the cell's text. The LibreOffice source says so explicitly:
   *"Libreoffice 4.1+ with ODF1.2 extended write however `calcext:value-type="error"` in
   that case"* (`sc/source/filter/xml/xmlcelli.cxx`), and the import handler is literally
   `if (it.isString("error")) mbErrorValue = true;`.

   Worse for a naive adapter: LibreOffice's error vocabulary is not Excel's.
   `include/formula/errorcodes.hxx` defines ~40 codes, of which only seven have `#…!`
   spellings; the rest surface to the user as **`Err:501`, `Err:508`, `Err:522`…**. There
   is also `FormulaError::Spill = 541`. So an ODS adapter will meet error literals that
   have no member in *any* Excel-derived enum.

Two ODS structural traps for a grid adapter (not type-system issues, but they belong in
the crib sheet): `table:number-columns-repeated` / `table:number-rows-repeated` compress
runs of identical cells and are routinely used to pad rows to 1024 columns, and a
`string` cell with no `office:string-value` takes its value from its `<text:p>` element
content (multiple paragraphs = multiple lines).

### 2.4 Google Sheets API v4

The value model is `ExtendedValue`, a union of exactly five:

`numberValue` (double), `stringValue`, `boolValue`, `formulaValue`, `errorValue`.

*"Dates, Times and DateTimes are represented as doubles in `SERIAL_NUMBER` format"* —
whole part = days since 1899-12-30, fraction = time of day. Identical to Excel, including
the epoch. Temporal-ness is carried by `CellFormat.numberFormat.type`, a
`NumberFormatType`: `TEXT`, `NUMBER`, `PERCENT`, `CURRENCY`, `DATE`, `TIME`,
`DATE_TIME`, `SCIENTIFIC` (plus `NUMBER_FORMAT_TYPE_UNSPECIFIED`). **Percent and currency
are formats here, not types** — the exact opposite of ODS, and a useful demonstration
that the distinction is presentational. Elapsed time is the `[hh]`/`[mm]`/`[ss]` pattern
family; the docs confirm these accumulate past 24 hours. Sheets, like Excel, has no
duration type.

`ErrorType` is ten values: `ERROR_TYPE_UNSPECIFIED`, `ERROR`, `NULL_VALUE`,
`DIVIDE_BY_ZERO`, `VALUE`, `REF`, `NAME`, `NUM`, `N_A`, `LOADING`. Two of these have no
Excel counterpart: **`ERROR` (`#ERROR!`, a Sheets-only parse error)** and
**`ERROR_TYPE_UNSPECIFIED`**. `LOADING` is the semantic twin of `#GETTING_DATA`.

Three fields per cell matter to an adapter: `userEnteredValue` (what was typed —
includes `formulaValue`), `effectiveValue` (the computed value — this is the lex source),
and `formattedValue` (the display string — never a lex source). Apps Script's
`ValueType` enum adds one more primitive that has no API-level `ExtendedValue`
representation: **`IMAGE`**.

### 2.5 Apple Numbers (proprietary iWork; via reverse-engineered `numbers-parser`)

Numbers stores cells in TSTArchives protobufs. The cell-type discriminator observed in
the format is `genericCellType`, `numberCellType`, `textCellType`, `dateCellType`,
`boolCellType`, `durationCellType`, `formulaErrorCellType`, `automaticCellType`; the
parser's own surface enum is:

`EMPTY`, `NUMBER`, `TEXT`, `DATE`, `BOOL`, `DURATION`, `ERROR`, `RICH_TEXT`, `CURRENCY`,
`MERGED`.

Findings:

- **`DURATION` is a genuine first-class primitive**, stored as a double count of
  **seconds** (`DurationCell(row, col, timedelta(seconds=double))`) with an independent
  display style (`DurationStyle`, `DurationUnits` from week down to millisecond).
  Numbers is the second of six vendors to make the distinction.
- **`CURRENCY` is a distinct cell type**, and its payload is an **IEEE decimal128**
  (`d128`) — genuinely higher-fidelity money than any other surveyed format, and a
  perfect fit for `CellValue`'s `ExactNumber` slot when an adapter ever exists.
- **Cell controls are formatting over primitives, not primitives.** `numbers-parser`'s
  own mapping: `tickbox`/checkbox → `BoolCell`; `rating` (stars), `slider`, `stepper` →
  `NumberCell`; `popup` menu → `NumberCell` *or* `TextCell`. The checkbox glyphs (`☐`
  `☑`) and star (`★`) are display artefacts. This settles the checkbox/rating/slider
  question cleanly: they lex to Boolean and Number respectively, with the control type
  riding a formatting capability.
- `RICH_TEXT` is styled text; the plain-text flattening is the honest lex.

### 2.6 CSV / TSV

RFC 4180 defines records, fields, quoting and escaping. It defines **no types**: every
field is text, and a "blank" field is an empty string. Typing is entirely consumer-side
and entirely a guess — which is precisely why Unrect must not do it. A CSV adapter lexes
every present field to `Text`, decides blankness explicitly (empty string? whitespace?
`"NULL"`? — the caller's `isBlank`), and offers no other kind. Any adapter that sniffs
`"1/2/2024"` into a `Temporal` is doing the thing the governing rule forbids; if a caller
wants that, they ask for it at the map site.

### 2.7 Gnumeric (corroboration only)

`GnmValueType` is `VALUE_EMPTY`, `VALUE_BOOLEAN`, `VALUE_FLOAT`, `VALUE_ERROR`,
`VALUE_STRING`, plus `VALUE_CELLRANGE` and `VALUE_ARRAY` (formula-expression values, not
cell contents). Five cell primitives, **no temporal type** — dates are floats, as in
Excel. Included because it is a third independent implementation converging on the same
poorest-common-denominator set, and because its `VALUE_EMPTY`-as-a-type matches Unrect's
`Blank`-as-a-kind decision.

### 2.8 Cross-vendor summary

| Primitive | OOXML | BIFF | ODF | Sheets | Numbers | Gnumeric | CSV |
|---|---|---|---|---|---|---|---|
| Empty / blank | ✔ (absent cell) | ✔ | ✔ (`void`/absent) | ✔ | ✔ `EMPTY` | ✔ `VALUE_EMPTY` | ✔ (empty field) |
| Text | ✔ `s`/`str`/`inlineStr` | ✔ | ✔ `string` | ✔ `stringValue` | ✔ `TEXT`/`RICH_TEXT` | ✔ | ✔ (everything) |
| Number | ✔ `n` | ✔ | ✔ `float` | ✔ `numberValue` | ✔ `NUMBER` | ✔ `VALUE_FLOAT` | ✘ |
| Boolean | ✔ `b` | ✔ | ✔ `boolean` | ✔ `boolValue` | ✔ `BOOL` | ✔ | ✘ |
| Error | ✔ `e` (8 coded, 18+ live) | ✔ (8) | ✽ `calcext` ext. only | ✔ `errorValue` (10) | ✔ `ERROR` | ✔ | ✘ |
| Date / datetime | ✽ number + format (`d` rare) | ✽ number + format | ✔ `date` (ISO, may carry offset) | ✽ number + format | ✔ `DATE` | ✽ float | ✘ |
| **Duration** | ✽ number + `[h]` format | ✽ | **✔ `time` (xsd:duration)** | ✽ `[hh]` format | **✔ `DURATION` (seconds)** | ✽ | ✘ |
| Percentage | ✽ format | ✽ | **✔ type** | ✽ format | ✽ format | ✽ | ✘ |
| Currency | ✽ format | ✽ | **✔ type** (+ISO code) | ✽ format | **✔ type** (decimal128) | ✽ | ✘ |
| Rich/linked entity | ✔ (falls back to `#VALUE!`) | ✘ | ✘ | ✘ | ✘ | ✘ | ✘ |
| Image | ✔ (rich value) | ✘ | ✘ | ✔ (Apps Script `IMAGE`) | ✘ | ✘ | ✘ |

✔ = first-class value type · ✽ = expressible, but as a format over another type · ✘ = absent

---

## 3. What this says about the kind set

Applying the governing rule — *only distinctions the source formats reliably make* —
to every candidate seventh kind:

| Candidate | Vendors making it a **type** | Verdict |
|---|---|---|
| Duration | 2 of 6 (ODF, Numbers) | **No.** Lex to `Number`, days. §7 |
| Percentage | 1 of 6 (ODF) | **No.** `Number` (fraction) + formatting capability |
| Currency | 2 of 6 (ODF, Numbers) | **No.** `Number` + formatting capability (ISO code) |
| Entity / linked | 1 of 6 (OOXML) | **No.** Vendor's own fallback is `#VALUE!`; a native-payload seam later |
| Image | 2 of 6 (OOXML rich value, Sheets/Apps Script) | **No.** Not data to decompose |
| Array / spill | 1 (a formula result shape, not a cell type) | **No.** Spilled cells are ordinary cells |
| Time-of-day (distinct from datetime) | 0 (everyone formats a number or reuses date) | **No.** `Temporal` with a sentinel date, documented |
| Integer / decimal (distinct from float) | 0 at the file level | **No.** Already resolved: one `Number`, granular accessors |

The kind set stands at six. That is a real result, not a rubber stamp: ODF and Numbers
*do* carry primitives Unrect cannot name, and the reason to refuse them is that four
other vendors would then need adapters that invent the distinction — the exact failure
the rule exists to prevent.

---

## 4. Mapping table: vendor type → `CellKind` → information lost

### 4.1 Excel OOXML / BIFF (`Unrect.Spreadsheets`, today)

| Source | `CellKind` | Lexing rule | Information lost |
|---|---|---|---|
| Absent cell, `<c/>`, empty `<v>` | `Blank` | — | Whether the cell exists but is empty vs. does not exist |
| `t="s"` / `inlineStr` / `str` | `Text` | Resolve SST index; unescape `_x????_` | Rich-text runs (bold spans etc.); whether it was a formula's cached string |
| `t="s"` etc. with `""` | `Blank` | Adapter floors empty string to Blank *before* `isBlank` runs | The distinction from an absent cell (documented, deliberate) |
| Whitespace-only text | `Blank` by default | `SpreadsheetSpace`'s default `isBlank`; `_ => false` for fidelity | The whitespace itself |
| `t="n"` / no `t`, general format | `Number` | `double` from the reader | **The exact decimal.** The XML holds decimal text; ExcelDataReader parses to `double`, so `ExactNumber` is always null. Also: the number format |
| `t="n"` + date format | `Temporal` | Reader applies OA conversion, 1904 shift and the 1900 leap-year workaround | Which format (date vs datetime); the serial itself |
| `t="n"` + time-of-day format | `Temporal` | Same path | **The date part is invented** (1899-12-30, or 1904-01-01) |
| `t="n"` + elapsed format `[h]:mm` (ids 46, 79, custom) | **throws today** → should be `Number` (days) | See §8.3 | Duration-ness; and `TimeSpan.FromDays` has already rounded to the millisecond |
| `t="b"` | `Boolean` | `<v>` is `1`/`0` | — |
| `t="e"`, one of the eight | `Error` | Literal → `BErr` code → `CellError` | The literal spelling (recoverable from the enum today) |
| `t="e"`, any modern literal | **`Blank` today** (silent) | See §8.2 | *Everything* — and blankness is load-bearing for decomposition |
| `t="d"` (Strict / Office 2010+) | `Temporal` if `yyyy-MM-dd`, else `Text` | Reader limitation | Time components of a full ISO 8601 value |
| Rich / linked data type cell (`vm`) | `Error(Value)` | Microsoft's documented `basicValue` is `"#VALUE!"` | The entity, its properties, its provider — all of it |
| BIFF error byte outside the eight | **throws today** → should be `Other` | See §8.1 | The code |

### 4.2 OpenDocument (guidance for a future `Unrect.Ods`)

| Source | `CellKind` | Lexing rule | Information lost |
|---|---|---|---|
| No `office:value-type`, or `void` | `Blank` | — | — |
| `string` + `office:string-value` | `Text` | Attribute value verbatim | — |
| `string`, attribute absent | `Text` | Concatenate `<text:p>` content, one `\n` per paragraph | Paragraph structure, inline styling |
| `float` | `Number` | `office:value` | Number format |
| `percentage` | `Number` | `office:value` **as-is** — it is already the fraction (`0.50` = 50%) | Percentage-ness → formatting capability |
| `currency` | `Number` | `office:value` | `office:currency` (ISO 4217) → formatting capability / native payload |
| `date` (xsd:date) | `Temporal` | Parse; time is midnight | — |
| `date` (xsd:dateTime with offset) | `Temporal` | Parse; **take the local wall-clock time and drop the offset** — do not convert to UTC | The offset. Attaching a zone is the consumer's job |
| `time` (xsd:duration) | `Number`, **days** | `xsd:duration` → total days as a `double`. May exceed 1; may be negative | Duration-ness; the ISO string's original units |
| `boolean` | `Boolean` | `true`/`false` | — |
| `calcext:value-type="error"` | `Error` | Map the literal; `#…!` forms per §9; **`Err:NNN` and anything unmapped → `Other` + preserve the literal** | Nothing, if the literal is preserved |
| `table:number-columns-repeated` | (structural) | **Expand** into that many cells; do not treat the run as one cell | — |
| `<table:covered-table-cell>` | `Blank` | A merged cell's non-anchor positions | Merge geometry → capability |

### 4.3 Google Sheets API (guidance for a future service adapter)

| Source | `CellKind` | Lexing rule | Information lost |
|---|---|---|---|
| No `effectiveValue` | `Blank` | Lex from `effectiveValue`, never `formattedValue`, never `userEnteredValue` | — |
| `stringValue` | `Text` | — | — |
| `boolValue` | `Boolean` | — | — |
| `numberValue`, format `DATE`/`TIME`/`DATE_TIME` | `Temporal` | Serial → epoch 1899-12-30; `TIME` produces the sentinel date | Which of the three; the serial |
| `numberValue`, elapsed pattern `[hh]`/`[mm]`/`[ss]` | `Number`, days | The serial already **is** days | Duration-ness |
| `numberValue`, any other format (incl. `PERCENT`, `CURRENCY`) | `Number` | The serial as-is; percent is the fraction | The format |
| `errorValue.type` | `Error` | Per §9; `LOADING` → `GettingData`; `ERROR`, `ERROR_TYPE_UNSPECIFIED` → `Other` + literal | — |
| `formulaValue` (only in `userEnteredValue`) | n/a | **Never a lex source.** We read cached/computed values | The formula → capability |
| Apps Script `IMAGE` | `Error(Value)` or `Blank` per what the API returns | Not data to decompose | The image |

### 4.4 Apple Numbers (guidance only; no adapter planned)

| Source | `CellKind` | Lexing rule | Information lost |
|---|---|---|---|
| `EMPTY` | `Blank` | — | — |
| `TEXT`, `RICH_TEXT` | `Text` | Flatten runs to plain text | Styling, bullets |
| `NUMBER` | `Number` | double | Format |
| `CURRENCY` | `Number` | **decimal128 → `CellValue.Of(decimal)`** where it fits, preserving `ExactNumber` | Currency code → capability |
| `DATE` | `Temporal` | — | — |
| `BOOL` (incl. checkbox control) | `Boolean` | — | Control type (checkbox vs plain) → capability |
| `DURATION` (seconds) | `Number`, **days** | seconds / 86400 | Duration-ness; the display units |
| rating / slider / stepper | `Number` | The underlying number | Control type → capability |
| popup menu | `Number` or `Text` | Whichever the underlying cell is | The menu's option list |
| `ERROR` | `Error` | Map the literal; unmapped → `Other` + literal | — |
| `MERGED` | `Blank` | Non-anchor of a merge | Merge geometry |

### 4.5 CSV / TSV

Every present field → `Text`. Blankness is the caller's `isBlank`. No other kind is ever
produced. One paragraph is the whole story, and the discipline is the point: a CSV
adapter that infers `Number` or `Temporal` is guessing, and the map site is where that
guess belongs.

---

## 5. Confirmed: the six kinds absorb every vendor primitive

Nothing in §2 is un-lexable. The three "extra" ODF/Numbers types (percentage, currency,
duration) all have numeric payloads; the OOXML rich types have a vendor-defined error
fallback; images and controls are presentation. The kind set does not change.

---

## 6. Confirmed: the error vocabulary does not survive contact

`CellError` is an accurate model of `BErr` — a 1996 binary enumeration. It is not an
accurate model of the error vocabulary of any live product:

| Source | Error types | In `CellError` today |
|---|---|---|
| MS-XLSB `BErr` | 8 | 8 |
| Excel (Office JS `ErrorCellValueType`) | 18 (16 GA + 2 preview) | 8 |
| Excel user-facing literals incl. `#UNKNOWN!` | 19+ | 8 |
| Google Sheets `ErrorType` | 10 | 8 (no `ERROR`, no `ERROR_TYPE_UNSPECIFIED`; `LOADING`≈`GettingData`) |
| LibreOffice `FormulaError` | ~40 (7 with `#…!` spellings, rest `Err:NNN`) | 8 |

And there is no safe behaviour on the unmapped path. Every implementation surveyed gets
this wrong in a different direction:

- **Unrect (`.xls` path)**: `Adapt` throws `InvalidOperationException` — the whole
  workbook fails because of one cell.
- **Unrect (`.xlsx` path)**: ExcelDataReader hands us `null`/`null`, so the cell becomes
  `Blank` — a silent lie that changes decomposition.
- **LibreOffice**: `calcBiffErrorCode` falls back to `BIFF_ERR_NA` — reports `#N/A`.
- **calamine**: `Tag::E => Data::Error(CellErrorType::Ref)` — reports `#REF!`; its
  `FromStr` handles only seven literals and does not know `#GETTING_DATA` at all.

An adapter that cannot say *"an error, and here is what it said"* is forced to guess.
That is a contract gap, not an adapter bug.

---

## 7. The duration question, resolved

**Question:** does `Temporal`'s inability to represent a duration require anything in the
contract now?

**Answer: no new kind; yes to a written rule and an adapter fix.**

Reasoning:

1. **The distinction is not reliably made.** Four of six formats express elapsed time as
   a number wearing a bracketed format. Only ODF (`office:value-type="time"`, typed
   `xsd:duration`) and Apple Numbers (`durationCellType`, seconds) make it a type. Under
   the governing rule that is disqualifying: a `Duration` kind would force the Excel,
   Sheets, Gnumeric and CSV adapters to synthesise a distinction from formatting — which
   is a guess about intent, exactly what §2's principle forbids. (Note that
   ExcelDataReader *already* makes that guess on our behalf by reading the number format
   and returning `TimeSpan`; Unrect should undo the guess, not enshrine it.)
2. **`Number` in days is not a fudge; it is Excel's own model** and Sheets' too, and it
   is lossless with respect to what the file stores. It also keeps arithmetic honest: a
   duration and a datetime serial are commensurable in days, which is why the formats
   chose it.
3. **A duration cannot lex to `Temporal`.** `PT26H30M` is not a `DateTime`. Any adapter
   that maps ODS `time` to `Temporal` is either wrong or silently clamping. That is worth
   writing down precisely because it is the intuitive-but-wrong move.
4. **`Temporal` should be documented for what it is**: a wall-clock instant with no zone
   and no offset, where a time-of-day cell carries a sentinel date (1899-12-30 under the
   1900 system) and a date-only cell carries midnight. `GetDate()` on a time-only cell
   returning 1899-12-30 is not a bug; it is the format's own shape showing through.

**What must change now:** nothing in `CellKind`. `Unrect.Spreadsheets` must stop throwing (§8.3),
and this document's rule — *durations lex to `Number` in days, in every adapter* — must be
the written contract so the ODS and Numbers adapters do not each invent their own unit.
Seconds (Numbers' native unit) and ISO strings (ODF's) are both wrong for the canonical
model; days is the one unit all the serial-based formats already agree on.

---

## 8. Change-now recommendations

Ranked by pre-publish urgency. Each states the cost of deferring past publish.

### 8.1 Add a catch-all and the modern named errors to `CellError` — **do this**

**Recommendation.** Extend `CellError` with:

- a catch-all member for any error the adapter recognises *as an error* but cannot name;
- the seven generally-available modern Excel errors that are distinguishable **from a
  saved file's error literal**: `Spill`, `Calc`, `Field`, `Blocked`, `Connect`, `Busy`,
  `External`.

Deliberately excluded: `Placeholder`. Office JS lists it, but its literal is `#BUSY!` —
identical to `Busy` — so the two are separable only in the live object model, never in a
file. Including it would require adapters to guess, which the governing rule forbids. A
`#BUSY!` cell lexes to `Busy`, full stop.

**Name the catch-all `Other`, not `Unknown`.** `#UNKNOWN!` is a real Excel error literal
(Python in Excel, "your version of Excel doesn't support Python"). A member called
`Unknown` would be ambiguous between "the error named `#UNKNOWN!`" and "an error we could
not name" — a documentation problem that never goes away. `Other` (or `Unrecognised`) has
no such collision.

Leave `Python` and `Timeout` out for now: they are Office JS *preview* and can be added
later without harm — provided the catch-all exists, because their absence then reads as
`Other` rather than as a lie.

**Why now rather than later.** Adding enum members after publish is binary-compatible, so
the naive read is "defer it". That read is wrong for a specific reason: **adding
`Spill` later silently changes behaviour for every consumer who wrote
`if (e == CellError.Other)` against a `#SPILL!` cell.** Consumers who correctly handled
the catch-all case get quietly reclassified by a minor version bump. The set of named
errors is effectively frozen at publish even though the enum technically is not.

**Cost if deferred:**
- Consumers write `Other`-based handling for `#SPILL!`/`#CALC!` that breaks on a later
  minor release — a silent behaviour change, the worst kind.
- Without `Other` at all, the `.xls` path keeps throwing on any undefined error byte and
  every future adapter (ODS `Err:501`, Sheets `#ERROR!`) has no legal lex and must guess.
- Cost of doing it now: eight enum members and eight `Display` cases. Effectively zero.

**Test impact:** `CellValueTests` has `[InlineData]` rows enumerating every member twice
(round-trip and `ToString`), and `SpreadsheetSpaceEdgeCaseTests` pins seven of the eight
against `examples/edge-cases.xlsx`. Adding members means adding rows, not changing
assertions. A synthetic fixture exercising an unrecognised literal cannot be produced with
ExcelDataReader (§8.4), so `Other` is best pinned through `ArraySpace` and through the
`.xls` byte-cast path.

**Sub-recommendation (lower confidence, pre-publish-only):** consider `Other = 0` so that
`default(CellError)` means "unrecognised error" rather than `#NULL!`. Today
`default(CellError) == CellError.Null`, which is a semantic accident of declaration order.
Reordering is a binary-breaking change the moment the package ships (anyone persisting the
underlying `int` is affected), so this is a now-or-never call. Against it: the enum reads
more naturally with the classic errors first; and `CellError` is only ever produced through
`CellValue.OfError`, so `default` is rarely observed. Judgement: worth doing, but the
weakest item on this list.

### 8.2 Let `CellValue` carry the unrecognised error's literal — **do this**

**Recommendation.** Add an optional literal alongside the `CellError`:

```csharp
public static CellValue OfError(CellError error, string? literal = null);
public string? TryGetErrorText();   // preserved literal, else the canonical spelling
public string GetErrorText();
```

`ToString()` then renders `Error(#SPILL!)` for a named member and `Error(Err:501)` for an
`Other`, and `WrongKindMessage` — which already names the error in its message — keeps
telling the truth.

**Why now.** Without it, `Other` is only half a fix: the adapter stops lying about *which*
error, but throws the evidence away, so a user staring at `Error(Other)` cannot tell
`Err:522` (circular reference) from `#PYTHON!`. And a diagnostic-quality library whose
error diagnostics say "some error" is a poor showing. Concretely it is needed for:
LibreOffice `Err:NNN`, Google Sheets `#ERROR!`, Excel `#PYTHON!`/`#TIMEOUT!`/`#UNKNOWN!`,
and every error not yet invented.

**Cost if deferred:** adding the accessor later is additive and safe; adding the *optional
parameter* later is a source-compatible but **binary-breaking** signature change (callers
must recompile), and adding it as a separate overload later leaves two near-identical
`OfError` methods forever. Deciding the shape now costs one commit; deciding it later
costs an awkward API shape permanently.

**Design notes.** Store the literal only when it differs from the canonical spelling, so
the common path allocates nothing extra. Equality must stay kind+payload based — two
`Other` errors with different literals are *not* equal, which is the desired behaviour and
matches how `Text` already works.

### 8.3 Stop `Unrect.Spreadsheets` throwing on durations and unknown error codes — **do this**

Two live throws in `src/Unrect.Spreadsheets/ExcelDataReaderExtensions.cs`:

```csharp
TimeSpan => throw new InvalidOperationException("TimeSpan cell values are not yet supported; …"),
…
_ => throw new InvalidOperationException($"Unsupported cell error {error}.")
```

**Recommendation.**
- `TimeSpan value => CellValue.Of(value.TotalDays)`, per §7. This is not a guess: the
  reader produced the `TimeSpan` as `TimeSpan.FromDays(number)`, so `TotalDays` returns
  the stored serial. (Caveat worth a code comment: `TimeSpan.FromDays` rounds to the
  nearest millisecond, so the round-trip is not bit-exact. The loss is the reader's, not
  ours, and is another argument for a first-party OOXML reader behind the native-payload
  seam.)
- Unknown `ExcelError` → `CellError.Other` (with the enum value's text as the literal),
  not an exception. This is the `.xls` path, where the reader casts a raw byte to the
  enum.

**Cost if deferred:** this is not a theoretical corner. Built-in number format **46**
(`[h]:mm:ss`, plus id **79** and any custom `[h]`/`[m]`/`[s]` format) is an elapsed-time
format; any timesheet, SLA report or duration column in a real workbook throws, and it throws from inside
`SpreadsheetSpace.Create` — before any shape runs, so the user gets no path and no A1
location, just "TimeSpan cell values are not yet supported". For a v1 whose pitch is
"parse the real spreadsheets you were sent", this is a first-week bug report.

### 8.4 Known limitation to document, not fix: modern errors arrive as `Blank` on the xlsx path

`XmlWorksheetReader.ConvertError` returns `null` for unrecognised literals *and* nulls the
value, so `#SPILL!` reaches `Unrect.Spreadsheets` as `GetValue() == null, GetCellError() == null`
— byte-for-byte identical to an empty cell. **Adding `CellError.Other` does not fix
this**, because the information is destroyed upstream of us.

This matters more than a normal fidelity gap because blankness is load-bearing:
`SkipBlankRows`, `RowsWhileAnyValue` and `Repeat`'s separators all key off it, so a single
`#SPILL!` in a data column can silently truncate a region rather than fail loudly.

Actions: (a) document it in `Unrect.Spreadsheets`'s XML docs and in Known Bugs; (b) open an
upstream issue on ExcelDataReader proposing that `ConvertError` preserve unrecognised
literals; (c) note it as motivation for the eventual first-party OOXML reader. Do **not**
attempt a workaround in `Unrect.Spreadsheets` — there is nothing to detect.

### 8.5 Not recommended

- **A `Duration` kind.** §7.
- **`Percentage` / `Currency` kinds.** §3, §8.6.
- **A `Formula` kind.** Family-wide rule: Unrect reads cached/computed values. OOXML's
  `<f>`, ODF's `table:formula`, Sheets' `userEnteredValue.formulaValue` are all *inputs*,
  not values; they belong to a formula-view capability, not the value model.
- **Reordering `CellKind`.** `Blank = 0` is already the right default.
- **Renaming `CellError.Number`.** It shadows `CellKind.Number` in prose but is
  unambiguous in code (`CellError.Number` is `#NUM!`), and churning it buys nothing.
- **Making `CellError` a struct or a class to hold the literal.** An enum plus an optional
  string on `CellValue` (§8.2) is smaller, keeps `switch` exhaustive, and keeps the
  `CellError` comparison story trivial.

### 8.6 Capability-seam confirmations (no contract change)

Each of these was checked against the vendors and holds. They are recorded here so the
future formatting-capability work has its requirements written down.

| Question | Confirmed answer | Rides on |
|---|---|---|
| ODS `percentage` | `Number`, payload is the **fraction** (spec example `"0.50"`) — identical to Excel's convention | Formatting capability exposes "is a percentage" |
| ODS `currency` | `Number`; `office:currency` is an ISO 4217 string | Formatting capability exposes the code |
| Numbers `CURRENCY` | `Number`, decimal128 → `CellValue.Of(decimal)` so `ExactNumber` is populated | — (already expressible) |
| Sheets `PERCENT`/`CURRENCY` | Already formats, not types — nothing to lose | Formatting capability |
| Checkboxes (Numbers `tickbox`) | `Boolean` — the vendor's own parser produces `BoolCell` | Formatting capability exposes the control |
| Star ratings, sliders, steppers | `Number` — vendor produces `NumberCell` | Formatting capability |
| Pop-up menus | `Number` or `Text`, whichever the underlying cell is | Formatting capability exposes the option list |
| Formulas (all vendors) | Never lexed. We read the cached/computed value | Formula-view overlay |
| Rich/linked data types (Stocks, Geography, entities) | `Error(Value)` — Microsoft's own `basicValue` is `"#VALUE!"` and `basicType` is `error` | Native payload seam, later |
| Images | Not data; whatever the vendor's fallback is | — |
| Merged cells | Anchor holds the value; covered cells are `Blank` | Merged-cell capability |
| Number formats generally | Never affect the kind, except that a date/elapsed format is how a serial's *meaning* is discovered — and that discovery belongs to the adapter, once, at lex time | Formatting capability |

Note the asymmetry worth remembering: **formatting is not purely presentational in the
serial formats.** In OOXML/BIFF/Sheets, the number format is the *only* thing that
distinguishes a date from a number, so the adapter must read it to lex at all. That is
consistent with "adapters must never guess" only because the format is a fact recorded in
the file, not an inference about intent — but it is the one place the boundary is thin,
and it is why "is this a duration?" (also a format fact) resolves to a documented rule
(§7) rather than to a kind.

---

## 9. Adapter lexing crib sheet

The normative part of this document. Every Unrect adapter, present and future, lexes by
these rules so that a shape written against a workbook behaves identically against the
same data in ODS or Sheets.

### 9.1 Family-wide rules

1. **Lex the cached/computed value, never the formula and never the display string.**
   OOXML `<v>` (not `<f>`); ODF value attributes (not `table:formula`); Sheets
   `effectiveValue` (not `userEnteredValue`, not `formattedValue`).
2. **Blankness is decided once, at adaptation, by the adapter's `isBlank`.** Absent cells
   and empty strings floor to `Blank` before `isBlank` runs. An error cell is never blank.
3. **Never infer a type from a string's shape.** CSV `"1/2/2024"` is `Text`. Full stop.
4. **Number formats may be read to discover date-ness and elapsed-ness — nothing else.**
   A format never changes a value's magnitude and never produces `Text`.
5. **Durations lex to `Number`, in days**, in every adapter. Not `Temporal`, not seconds,
   not an ISO string.
6. **Percentages lex to `Number` as the stored fraction.** Never multiply by 100.
7. **An unrecognised error is `CellError.Other` with its literal preserved** (§8.1, §8.2).
   Never `#N/A`, never `#REF!`, never `Blank`, never an exception.
8. **Temporal has no zone and no offset.** If the source carries one, take the local
   wall-clock reading and drop it; attaching a zone is the consumer's job.
9. **Flatten rich text to plain text.** Runs and styling ride a formatting capability.
10. **Expand repeat-compressed cells** (ODF) and treat merge-covered cells as `Blank`.

### 9.2 Error literal → `CellError` (all adapters)

| Literal / API value | `CellError` |
|---|---|
| `#NULL!` · Sheets `NULL_VALUE` | `Null` |
| `#DIV/0!` · Sheets `DIVIDE_BY_ZERO` | `DivisionByZero` |
| `#VALUE!` · Sheets `VALUE` | `Value` |
| `#REF!` · Sheets `REF` | `Reference` |
| `#NAME?` · Sheets `NAME` | `Name` |
| `#NUM!` · Sheets `NUM` | `Number` |
| `#N/A` · Sheets `N_A` | `NotAvailable` |
| `#GETTING_DATA` · Sheets `LOADING` | `GettingData` |
| `#SPILL!` · LO `FormulaError::Spill` (541) | `Spill` *(proposed §8.1)* |
| `#CALC!` | `Calc` *(proposed)* |
| `#FIELD!` | `Field` *(proposed)* |
| `#BLOCKED!` | `Blocked` *(proposed)* |
| `#CONNECT!` | `Connect` *(proposed)* |
| `#BUSY!` | `Busy` *(proposed)* |
| `#EXTERNAL!` | `External` *(proposed)* |
| Office JS `placeholder` (literal is `#BUSY!`) | `Busy` — **not** a separate member; the distinction does not exist in a file (§2.1, §8.1) |
| `#PYTHON!`, `#TIMEOUT!`, `#UNKNOWN!` | `Other` + literal *(preview-only in the vendor API; promote if they stabilise)* |
| Sheets `ERROR` (`#ERROR!`), `ERROR_TYPE_UNSPECIFIED` | `Other` + literal |
| LibreOffice `Err:501` … `Err:542` | `Other` + literal |
| Anything else | `Other` + literal |

### 9.3 Per-format quick reference

**OOXML / BIFF** — §4.1. Watch: no date type (read the `numFmt`); the 1904 flag; elapsed
formats 46/79 → `Number` days; `t="d"` is rare and the current reader only handles
`yyyy-MM-dd`; linked data types read as `#VALUE!`; unrecognised error literals are lost by
ExcelDataReader (§8.4).

**ODF / ODS** — §4.2. Watch: `time` is a **duration**, not a clock time; percentage and
currency are types whose payload is a plain number; errors only exist via
`calcext:value-type="error"` with the literal in the cell text and a vocabulary that
includes `Err:NNN`; `table:number-columns-repeated` must be expanded; a `string` cell may
take its value from `<text:p>` content.

**Google Sheets** — §4.3. Watch: lex `effectiveValue`; serials share Excel's epoch;
`LOADING` ≈ `GettingData`; `#ERROR!` has no Excel counterpart; percent/currency are
formats.

**Apple Numbers** — §4.4. Watch: `DURATION` is seconds (divide by 86400); `CURRENCY` is
decimal128 (use `CellValue.Of(decimal)`); controls lex to their underlying primitive.

**CSV / TSV** — §4.5. Everything is `Text`; blankness is the caller's; no inference.

---

## 10. Follow-ups this survey opens (not pre-publish)

- **Formatting capability requirements** are now enumerated (§8.6) — percentage-ness,
  currency code, duration-ness, control type, rich-text runs, merge geometry. Whoever
  designs that seam should start from that table.
- **Native payload candidates** are likewise enumerated: ODF's timezone offset, Numbers'
  decimal128 and duration units, OOXML rich entities, and — the sleeper — **the exact
  decimal that xlsx stores as text and ExcelDataReader discards**. `CellValue`'s
  `ExactNumber` slot exists for money fidelity and currently never fires on the Excel
  path; a first-party OOXML reader would light it up without any contract change, which is
  a nice confirmation that the wave-1 dual-storage decision was right.
- **`Unrect.Ods`** is the most valuable second adapter precisely because ODF is the only
  format richer than the canonical model; §4.2 and §9 are its specification.
- **Upstream ExcelDataReader issue** for §8.4.

---

## 11. Sources

**OOXML / Excel**
- ECMA-376 `ST_CellType` (Part 1, SpreadsheetML): <http://webapp.docx4java.org/OnlineDemo/ecma376/SpreadsheetML/ST_CellType.html>, <https://schemas.liquid-technologies.com/officeopenxml/2006/st_celltype.html>
- Open XML SDK `CellValues` enum (documents `d` as Office 2010+): <https://learn.microsoft.com/en-us/dotnet/api/documentformat.openxml.spreadsheet.cellvalues?view=openxml-3.0.1>
- [MS-OE376] Part 4 §3.11.1.3 (Excel limits `t` to `b`, `e`, `inlineStr`, `n`): <https://learn.microsoft.com/en-us/openspecs/office_standards/ms-oe376/60ecd5c2-38c8-4624-9de4-e76a036f4442>
- [MS-XLSB] `BErr` (the eight error codes): <https://learn.microsoft.com/en-us/openspecs/office_file_formats/ms-xlsb/c3d25119-1da4-44dc-bdb5-19b8ba2ddf90>
- `Excel.CellValueType` (modern cell vocabulary): <https://learn.microsoft.com/en-us/javascript/api/excel/excel.cellvaluetype?view=excel-js-preview>
- `Excel.ErrorCellValueType` (18 error types): <https://learn.microsoft.com/en-us/javascript/api/excel/excel.errorcellvaluetype?view=excel-js-preview>
- `Excel.EntityCellValue` (`basicValue: "#VALUE!"`, `basicType: error`): <https://learn.microsoft.com/en-us/javascript/api/excel/excel.entitycellvalue?view=excel-js-preview>
- `Excel.ExternalErrorCellValue` (`basicValue: "#EXTERNAL!"`): <https://learn.microsoft.com/en-us/javascript/api/excel/excel.externalerrorcellvalue?view=excel-js-preview>
- `Excel.PlaceholderErrorCellValue` (`basicValue: "#BUSY!"` — same literal as `Busy`): <https://learn.microsoft.com/en-us/javascript/api/excel/excel.placeholdererrorcellvalue?view=excel-js-preview>
- [MS-XLSX] `CT_RichValue`: <https://learn.microsoft.com/en-us/openspecs/office_standards/ms-xlsx/b8d7927a-79b4-4f2c-b76b-3a6e9cd7ad40>
- Troubleshoot Python in Excel errors (`#PYTHON!`, `#BUSY!`, `#BLOCKED!`, `#CALC!`, `#CONNECT!`, `#SPILL!`, `#TIMEOUT!`, `#UNKNOWN!`): <https://support.microsoft.com/en-us/excel/python/troubleshoot-python-in-excel-errors>
- `#SPILL!`: <https://support.microsoft.com/en-us/excel/how-to-correct-a-spill-error> · `#BLOCKED!`: <https://support.microsoft.com/en-us/office/how-to-correct-a-blocked-error-13be117b-92e4-400a-a215-aa59d37d6e7c> · `#BUSY!`: <https://support.microsoft.com/en-us/office/how-to-correct-a-busy-error-8bdce02f-9dc0-48b9-9326-49326f294619>
- Dynamic arrays and spilled array behavior: <https://support.microsoft.com/en-us/office/dynamic-array-formulas-and-spilled-array-behavior-205c6b06-03ba-4151-89a1-87a7eb36e531>
- BIFF error codes (OpenOffice.org's documentation of the Excel file format): <https://www.openoffice.org/sc/excelfileformat.pdf>

**OpenDocument**
- ODF 1.3 OS schema (`valueType`, `common-value-and-type-attlist`): <https://docs.oasis-open.org/office/OpenDocument/v1.3/os/schemas/OpenDocument-v1.3-schema.rng>
- ODF 1.3 Part 3 §19.389 `office:value-type` + Table 14, §19.386 `office:time-value` (data type **duration**), §19.373 `office:currency`, §19.374 `office:date-value`: <https://docs.oasis-open.org/office/OpenDocument/v1.3/os/part3-schema/OpenDocument-v1.3-os-part3-schema.html>
- LibreOffice `sc/source/filter/xml/xmlcelli.cxx` (`calcext:value-type="error"`): <https://git.libreoffice.org/core/+/refs/heads/master/sc/source/filter/xml/xmlcelli.cxx>
- LibreOffice `include/formula/errorcodes.hxx` (`Err:NNN` vocabulary, `Spill = 541`): <https://git.libreoffice.org/core/+/refs/heads/master/include/formula/errorcodes.hxx>
- LibreOffice `sc/source/filter/oox/unitconverter.cxx` (OOXML error literals incl. `#SPILL!`; `#N/A` fallback): <https://git.libreoffice.org/core/+/refs/heads/master/sc/source/filter/oox/unitconverter.cxx>

**Google Sheets**
- `ExtendedValue`, `ErrorValue`, `ErrorType`: <https://developers.google.com/workspace/sheets/api/reference/rest/v4/spreadsheets/other>
- `NumberFormatType`: <https://developers.google.com/workspace/sheets/api/reference/rest/v4/spreadsheets/cells>
- Date and number formats (serial epoch, elapsed `[hh]` patterns): <https://developers.google.com/sheets/api/guides/formats>
- Apps Script `ValueType` (adds `IMAGE`): <https://developers.google.com/apps-script/reference/spreadsheet/value-type>

**Apple Numbers**
- `numbers-parser` (reverse-engineered iWork TSTArchives) `constants.py` `CellType`, `cell.py` cell classes and control formatting: <https://github.com/masaccio/numbers-parser>

**Other**
- Gnumeric `GnmValueType` (`src/value.h`): <https://gitlab.gnome.org/GNOME/gnumeric/-/blob/master/src/value.h>
- calamine `Data` / `CellErrorType` / ODS reader (`src/datatype.rs`, `src/xlsx/mod.rs`, `src/ods.rs`): <https://github.com/tafia/calamine>
- ExcelDataReader `CellError.cs`, `Core/OpenXmlFormat/XmlFormat/XmlWorksheetReader.cs`, `Core/OpenXmlFormat/XlsxWorksheet.cs`, `Core/BuiltinNumberFormat.cs`: <https://github.com/ExcelDataReader/ExcelDataReader>
- RFC 4180, Common Format and MIME Type for CSV Files: <https://www.rfc-editor.org/rfc/rfc4180>
