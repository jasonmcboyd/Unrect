namespace Unrect.Core
{
  /// <summary>
  /// The canonical spreadsheet error values. An error is a value a cell genuinely holds — a formula
  /// that could not produce a result — not a missing cell, so error cells are never blank.
  /// <para>
  /// The named members are the errors an adapter can identify from a <em>saved file's</em> error
  /// literal. Anything else lexes to <see cref="Other"/> rather than throwing, and the literal it
  /// arrived as is preserved on the cell (<see cref="CellValue.TryGetErrorText"/>) so nothing is
  /// silently lost: an adapter may fail to name an error, but it may never invent one and may never
  /// discard the evidence.
  /// </para>
  /// </summary>
  public enum CellError
  {
    /// <summary>
    /// An error the adapter recognised as an error but could not name — LibreOffice's
    /// <c>Err:501</c>, Google Sheets' <c>#ERROR!</c>, an Excel error newer than this library. The
    /// cell keeps the literal it arrived as.
    /// <para>
    /// Named <c>Other</c> rather than <c>Unknown</c> deliberately: <c>#UNKNOWN!</c> is itself a real
    /// Excel literal, so a member called <c>Unknown</c> would be permanently ambiguous between "the
    /// error named #UNKNOWN!" and "an error we could not name".
    /// </para>
    /// <para>
    /// It is zero so that <c>default(CellError)</c> reads as "unrecognised" rather than accidentally
    /// meaning <c>#NULL!</c>.
    /// </para>
    /// </summary>
    Other = 0,

    /// <summary>#NULL! — an empty intersection of two ranges.</summary>
    Null,

    /// <summary>#DIV/0! — division by zero.</summary>
    DivisionByZero,

    /// <summary>#VALUE! — an operand of the wrong type.</summary>
    Value,

    /// <summary>#REF! — a reference to a cell that no longer exists.</summary>
    Reference,

    /// <summary>#NAME? — an unrecognised name.</summary>
    Name,

    /// <summary>#NUM! — a numeric value outside the representable range.</summary>
    Number,

    /// <summary>#N/A — a value that is not available, typically a failed lookup.</summary>
    NotAvailable,

    /// <summary>
    /// #GETTING_DATA — the transient state of an asynchronous (e.g. RTD) formula, occasionally
    /// found cached in saved workbooks. Faithfully represented rather than rejected so such
    /// files remain parseable.
    /// </summary>
    GettingData,

    /// <summary>#SPILL! — a dynamic-array formula whose results cannot fit the range ahead of it.</summary>
    Spill,

    /// <summary>#CALC! — a calculation engine failure, such as an empty array result.</summary>
    Calc,

    /// <summary>#FIELD! — a linked data type with no such field.</summary>
    Field,

    /// <summary>#BLOCKED! — a result withheld, typically by a privacy or connection policy.</summary>
    Blocked,

    /// <summary>#CONNECT! — a linked data type whose source could not be reached.</summary>
    Connect,

    /// <summary>
    /// #BUSY! — a linked data type still loading.
    /// <para>
    /// Office JS also lists a <c>Placeholder</c> error, deliberately not represented here: its
    /// literal is <c>#BUSY!</c> too, so the two are separable only in the live object model and
    /// never in a file. An adapter reading a saved workbook would have to guess, so a <c>#BUSY!</c>
    /// cell lexes to this and nothing else.
    /// </para>
    /// </summary>
    Busy,

    /// <summary>#EXTERNAL! — an external call, such as a UDF, that could not be completed.</summary>
    External
  }
}
