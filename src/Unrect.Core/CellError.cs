namespace Unrect.Core
{
  /// <summary>
  /// The canonical spreadsheet error values. An error is a value a cell genuinely holds — a formula
  /// that could not produce a result — not a missing cell, so error cells are never blank.
  /// </summary>
  public enum CellError
  {
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
    GettingData
  }
}
