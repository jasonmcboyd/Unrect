using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Projections
{
  /// <summary>
  /// How the exploratory table rung and the labelled-pair block actually read a region. Kept apart
  /// from the vocabulary so that file stays a list of what a user can say.
  /// </summary>
  public static partial class ProjectionBuilders<TSpace>
    where TSpace : class, ISpace
  {
    private static IReadOnlyList<IReadOnlyDictionary<string, Point<TSpace>>> DictionaryRows(TableView<TSpace> table)
    {
      var captions = new string?[table.ColumnCount];

      for (var column = 0; column < table.ColumnCount; column++)
      {
        var caption = table.ColumnNames[column];

        if (caption.Length == 0)
        {
          // A column with no caption and nothing in it is not a column anyone could miss — the
          // blank lead of an indented table is the usual one — so it simply has no entry. One that
          // holds a value would be dropped silently, which is the thing this rung refuses to do.
          if (table.Rows.All(row => row[column].IsBlank))
            continue;

          throw table.Failure(
            $"the column at {table.Header.AddressOf(column).A1} has no caption and holds values; every column needs a caption to be read by name");
        }

        for (var earlier = 0; earlier < column; earlier++)
          if (captions[earlier] is string other && CaptionComparer.Default.Equals(other, caption))
            throw table.Failure(
              $"the columns at {table.Header.AddressOf(earlier).A1} ('{captions[earlier]}') and "
              + $"{table.Header.AddressOf(column).A1} ('{caption}') carry the same caption; "
              + "captions are matched ignoring case and whitespace");

        captions[column] = caption;
      }

      var rows = new List<IReadOnlyDictionary<string, Point<TSpace>>>(table.RowCount);

      foreach (var row in table.Rows)
      {
        var cells = new Dictionary<string, Point<TSpace>>(captions.Length, CaptionComparer.Default);

        for (var column = 0; column < captions.Length; column++)
          if (captions[column] is string caption)
            cells[caption] = row[column];

        rows.Add(cells);
      }

      return rows;
    }

    /// <summary>
    /// The block finds its own first label, column then row — the order that works when the label
    /// column sits far to the right of a wide sheet.
    /// </summary>
    private static Placement FieldsPlacement(string label)
      => new Placement(
        OffsetStrategies.Then(
          OffsetStrategies.To(ColumnLandmarks.ColumnWhere(
            CellMatching.AnyCellInColumn(CellMatching.LabelEquals(label)), $"no column with the label '{label}'")),
          OffsetStrategies.To(RowLandmarks.RowWhere(
            CellMatching.AnyCellInRow(CellMatching.LabelEquals(label)), $"no row with the label '{label}'"))),
        null);

    private static string NotEmptyLabel(string label)
    {
      if (label is null)
        throw new ArgumentNullException(nameof(label));

      if (label.Trim().Length == 0)
        throw new ArgumentException("A field label cannot be empty or whitespace.", nameof(label));

      return label;
    }
  }
}
