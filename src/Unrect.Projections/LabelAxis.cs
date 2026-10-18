namespace Unrect.Projections
{
  /// <summary>
  /// Which axis a set of labels names: a <see cref="Column"/> label answers a column, translating
  /// along <c>Offset.Column</c>; a <see cref="Row"/> label answers a row, translating along
  /// <c>Offset.Row</c>. Kept separate on the stack so a row-labelled and a column-labelled scope
  /// coexist rather than shadow one another.
  /// </summary>
  public enum LabelAxis
  {
    /// <summary>The labels name columns: a header row over a body.</summary>
    Column,
    /// <summary>The labels name rows: a label column beside a body.</summary>
    Row,
  }
}
