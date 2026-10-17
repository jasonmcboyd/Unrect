using System;
using System.Globalization;

namespace Unrect.Projections
{
  /// <summary>
  /// One step of a path through a header: a name, the nth of a name, or a position.
  /// <code>
  /// row["From", "Id"]            // a band, then a caption under it
  /// row["From", ("Id", 1)]       // the second Id under From
  /// row[("Totals", 1), "Amount"] // the second band called Totals
  /// row["From", 1]               // the second column under From
  /// </code>
  /// <para>
  /// Nobody names this type: a step is written as a <c>string</c>, an <c>int</c> or a
  /// <c>(string, int)</c> and converts. It exists so that a path is a list of TYPED values and is
  /// never text — <c>"From 2"</c> could be a band and a position, or a caption that says exactly
  /// that, and no string could tell them apart once it was written.
  /// </para>
  /// <para>
  /// Every number is an index and indexes start at zero: <c>("Id", 0)</c> is the first Id, and a
  /// bare <c>0</c> is the first column of whatever the path has reached.
  /// </para>
  /// </summary>
  public readonly struct LabelStep
  {
    private LabelStep(string? name, int? index)
    {
      Name = name;
      Index = index;
    }

    /// <summary>The name this step looks for; null where the step is a position.</summary>
    public string? Name { get; }

    /// <summary>Which of the things so named, or — with no name — which column; null for a name that expects to be the only one.</summary>
    public int? Index { get; }

    /// <summary>Whether this step is a position rather than a name.</summary>
    public bool IsPosition => Name is null;

    /// <summary>A step by name: the one thing so called, where the path has reached.</summary>
    public static implicit operator LabelStep(string name)
      => new LabelStep(name ?? throw new ArgumentNullException(nameof(name)), null);

    /// <summary>A step by position: the column at <paramref name="position"/>, counted from zero within what the path has reached.</summary>
    public static implicit operator LabelStep(int position) => new LabelStep(null, position);

    /// <summary>The nth thing called <c>Name</c>, counted from zero — how a declaration says it knows there are several.</summary>
    public static implicit operator LabelStep((string Name, int Index) nth)
      => new LabelStep(nth.Name ?? throw new ArgumentNullException(nameof(nth)), nth.Index);

    /// <summary>The step as it would be written: <c>"Id"</c>, <c>("Id", 1)</c> or <c>1</c>.</summary>
    public override string ToString()
      => Name is null ? Index.GetValueOrDefault().ToString(CultureInfo.InvariantCulture)
       : Index is int index ? $"(\"{Name}\", {index.ToString(CultureInfo.InvariantCulture)})"
       : $"\"{Name}\"";
  }
}
