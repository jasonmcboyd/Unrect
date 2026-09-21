namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The canonical cell kinds every <see cref="Cell"/> is classified into — small and closed,
  /// so a strategy or leaf can test <see cref="Cell.Kind"/> without knowing which backend
  /// produced the cell. Adapters decide which of a backend's own states map to which kind; nothing
  /// finer than this set is a value distinction, only a conversion (<see cref="Cell"/>'s
  /// typed accessors).
  /// </summary>
  public enum CellKind
  {
    /// <summary>No value: an empty cell, or one an adapter's blankness rule chose to treat as empty.</summary>
    Blank,

    /// <summary>A string value.</summary>
    Text,

    /// <summary>
    /// A numeric value — one kind, and one representation: a double, which is what the store
    /// holds. A decimal or a whole number is a conversion a reader asks for, above the space.
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
