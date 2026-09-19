using System;
using System.Collections.Generic;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

namespace Unrect.Interactive
{
  /// <summary>
  /// The scaffolds as leaves, so a type can be guessed where a declaration has already found it:
  /// <code>
  /// using static Unrect.Interactive.ScaffoldBuilders&lt;Unrect.Spreadsheets.ISheetCells&gt;;
  ///
  /// var report = VerticalFlow(v =&gt; new
  /// {
  ///   Title        = v.Next(Text()),
  ///   Transactions = v.Next(ScaffoldRecord("Transaction")),
  /// });
  ///
  /// report.Map(sheet).Dump();
  /// </code>
  /// Each yields the C# source <see cref="Scaffolding"/> writes — a string to dump, paste and edit —
  /// and nothing is searched for: the declaration around it says where the labels are.
  /// <para>
  /// <b>A scaffold stands where the table will.</b> With the labels along a row it is placed and
  /// sized exactly as <c>Table&lt;T&gt;()</c> is — past any blank rows, one header row, rows while
  /// they carry values — so once the type is pasted, <c>ScaffoldRecord("Transaction")</c> becomes
  /// <c>Table&lt;Transaction&gt;()</c> and nothing else in the declaration moves.
  /// </para>
  /// <para>
  /// Scaffold the thing that repeats, not the repeat: inside a <c>VerticalRepeat</c> a scaffold
  /// yields one copy of its source per occurrence, which is what it was asked for and rarely what
  /// anyone wants. Map the item alone — it reads the first occurrence and stops — and wrap it once
  /// its types are real.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The sheet every declaration in the file is written against.</typeparam>
  public static class ScaffoldBuilders<TSpace>
    where TSpace : class, ISheetCells
  {
    /// <summary>
    /// The source of a positional record matching the labels of the region this is placed on; see
    /// <see cref="Scaffolding.ScaffoldRecord{TSpace}"/> for how names and types are arrived at.
    /// </summary>
    /// <param name="typeName">The type's name.</param>
    /// <param name="labels">Whether the labels run along the region's first row or down its first column.</param>
    /// <param name="samples">How many cells beside each label to type the member from.</param>
    public static IProjectionDefinition<TSpace, string> ScaffoldRecord(string typeName, LabelsIn labels = LabelsIn.Row, int samples = 5)
      => Leaf(typeName, labels, samples, Scaffolding.Record, nameof(ScaffoldRecord));

    /// <summary>
    /// The same guess as <see cref="ScaffoldRecord"/>, written as a class with init-only properties.
    /// </summary>
    /// <param name="typeName">The type's name.</param>
    /// <param name="labels">Whether the labels run along the region's first row or down its first column.</param>
    /// <param name="samples">How many cells beside each label to type the member from.</param>
    public static IProjectionDefinition<TSpace, string> ScaffoldClass(string typeName, LabelsIn labels = LabelsIn.Row, int samples = 5)
      => Leaf(typeName, labels, samples, Scaffolding.Class, nameof(ScaffoldClass));

    private static IProjectionDefinition<TSpace, string> Leaf(
      string typeName,
      LabelsIn labels,
      int samples,
      Func<List<Scaffolding.Member>, string, LabelsIn, string> write,
      string noun)
    {
      Scaffolding.Name(typeName);

      if (samples < 0)
        throw new ArgumentOutOfRangeException(nameof(samples));

      string Source(Plane<TSpace> region)
        => write(Scaffolding.Read(Scaffolding.Region.Of(region), typeName, labels, at: 0, samples), typeName, labels);

      // The labels are the first line of whatever region the declaration hands over, so there is no
      // search and no `at`: along a row that region is the table's own; down a column it is the
      // card's — the full width, for as many rows as carry a value.
      var leaf = labels == LabelsIn.Row
        ? ProjectionBuilders<TSpace>.Table((TableView<TSpace> view) => Source(view.Space))
        : ProjectionBuilders<TSpace>.AfterBlankRows().Range(ProjectionBuilders<TSpace>.RowsWhileAnyValue(), block => Source(block.Space));

      return leaf.AsUnit($"{noun}(\"{typeName}\")");
    }
  }
}
