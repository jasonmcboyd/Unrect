namespace Unrect.Core
{
  /// <summary>
  /// The canonical cell kinds every <see cref="CellValue"/> is classified into — small and closed,
  /// so a strategy or leaf can test <see cref="CellValue.Kind"/> without knowing which backend
  /// produced the cell. Adapters decide which of a backend's own states map to which kind; nothing
  /// finer than this set is a value distinction, only a conversion (<see cref="CellValue"/>'s
  /// typed accessors).
  /// </summary>
  public enum CellKind
  {
    /// <summary>No value: an empty cell, or one an adapter's blankness rule chose to treat as empty.</summary>
    Blank,

    /// <summary>A string value.</summary>
    Text,

    /// <summary>
    /// A numeric value — one kind regardless of how it was stored; <see cref="CellValue"/>'s granular
    /// accessors (<see cref="CellValue.GetDouble"/>/<see cref="CellValue.GetDecimal"/>/<see cref="CellValue.GetInt"/>)
    /// do the interpreting, not the kind.
    /// </summary>
    Number,

    /// <summary>A date or date-time value.</summary>
    Temporal,

    /// <summary>A boolean value.</summary>
    Boolean,

    /// <summary>A spreadsheet error the cell holds as its actual value; see <see cref="CellError"/>. Never blank.</summary>
    Error
  }
}
