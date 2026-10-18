using System;

namespace Unrect.Projections
{
  /// <summary>
  /// One labelled pair in a <c>Fields</c> block: a label cell, and the value cell immediately to
  /// its right. A declaration value rather than a projection, because the block needs each field's
  /// label to key its result, and a projection could not supply one without being type-tested.
  /// </summary>
  public sealed class Field
  {
    /// <summary>A field whose left cell must say <paramref name="label"/>; refused when null or empty.</summary>
    public Field(string label)
    {
      if (string.IsNullOrEmpty(label))
        throw new ArgumentException("A field label must say something.", nameof(label));

      Label = label;
    }

    /// <summary>The label as the declaration wrote it — this is the key in the block's result.</summary>
    public string Label { get; }
  }
}
