namespace Unrect.Projections
{
  /// <summary>
  /// One labelled pair in a <c>Fields</c> block: a label cell, and the value cell immediately to
  /// its right. A declaration value rather than a projection, because the block needs each field's
  /// label to key its result, and a projection could not supply one without being type-tested.
  /// </summary>
  public sealed class Field
  {
    internal Field(string label)
    {
      Label = label;
    }

    /// <summary>The label as the declaration wrote it — this is the key in the block's result.</summary>
    public string Label { get; }
  }
}
