using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Shapes
{
  /// <summary>
  /// How the table and field factories in <see cref="Shape"/> actually read a region: caption
  /// binding, per-cell reading, and the two block validations. Kept apart from the vocabulary so
  /// that file stays a list of what a user can say.
  /// </summary>
  public static partial class Shape
  {
    private static IReadOnlyList<T> BindRows<T>(TableView table, RowBinding<T> plan)
    {
      var columns = new int[plan.Members.Count];
      var unbound = new List<string>();

      for (var member = 0; member < plan.Members.Count; member++)
      {
        var matches = new List<int>();

        for (var column = 0; column < table.ColumnCount; column++)
          if (CaptionComparer.Default.Equals(table.ColumnNames[column], plan.Members[member].Caption))
            matches.Add(column);

        if (matches.Count == 0)
        {
          unbound.Add(plan.Members[member].Name);
          continue;
        }

        if (matches.Count > 1)
          throw table.Failure(
            $"{typeof(T).Name}.{plan.Members[member].Name} matches the columns at "
            + $"{table.Header.AddressOf(matches[0]).A1} ('{table.ColumnNames[matches[0]]}') and "
            + $"{table.Header.AddressOf(matches[1]).A1} ('{table.ColumnNames[matches[1]]}'); "
            + "captions are matched ignoring case and whitespace");

        columns[member] = matches[0];
      }

      if (unbound.Count > 0)
      {
        // The example names an UNBOUND member: advice that pointed at a member which already found
        // its column would send a reader to fix the one thing that is not broken.
        var example = unbound[0];

        throw table.Failure(
          $"no column binds {Join(unbound.Select(name => $"{typeof(T).Name}.{name}").ToList())}; the table's captions are "
          + $"{string.Join(", ", table.ColumnNames.Select(c => $"'{c}'"))}. "
          + $"Bind one with Column(t => t.{example}, \"…\") or drop it with Ignore(t => t.{example})");
      }

      // Streamed rather than indexed: the columns are settled from the header above, and from here on
      // the table is read forward-only, one row per step. On an extent whose height is discovered as
      // it is read that is the difference between one pass over the sheet and two.
      var rows = new List<T>();
      var values = new object?[plan.Members.Count];

      foreach (var row in table.StreamRows())
      {
        for (var member = 0; member < plan.Members.Count; member++)
          values[member] = ReadCell(row, columns[member], plan.Members[member], table);

        rows.Add(plan.Materialize(values));
      }

      // Grown rather than pre-sized, because asking how many rows there are is the forcing question
      // streaming exists to avoid — so the doubling's slack is given back here instead.
      rows.TrimExcess();

      return rows;
    }

    private static object? ReadCell(TableRow row, int column, MemberPlan member, TableView table)
    {
      var cell = row[column];

      // A CellValue member asserts nothing: it is the in-table spelling of Cell(c => c), for the
      // column whose kind genuinely varies.
      if (member.Read is null)
        return cell;

      if (member.BlankTolerant && cell.IsBlank)
        return null;

      // Formatted only when something fails: a large sheet binds tens of thousands of cells, and
      // every one of them would otherwise build an A1 address that nobody reads.
      string At() => row.AddressOf(column).A1;

      if (cell.Kind != member.Kind!.Value)
        throw table.Failure($"column '{member.Caption}': {CellReading.WrongKind(member.Kind.Value, cell, At())}");

      if (!member.Read(cell, At, out var value, out var conversion))
        throw table.Failure($"column '{member.Caption}': {conversion}");

      return value;
    }

    private static string Join(IReadOnlyList<string> names)
      => names.Count == 1
        ? names[0]
        : string.Join(", ", names.Take(names.Count - 1)) + " or " + names[names.Count - 1];

    private static IReadOnlyList<IReadOnlyDictionary<string, CellValue>> DictionaryRows(TableView table)
    {
      var captions = new string[table.ColumnCount];

      for (var column = 0; column < table.ColumnCount; column++)
      {
        var caption = table.ColumnNames[column];

        if (caption.Length == 0)
          throw table.Failure(
            $"the column at {table.Header.AddressOf(column).A1} has no caption; every column needs one to be read by name");

        for (var earlier = 0; earlier < column; earlier++)
          if (CaptionComparer.Default.Equals(captions[earlier], caption))
            throw table.Failure(
              $"the columns at {table.Header.AddressOf(earlier).A1} ('{captions[earlier]}') and "
              + $"{table.Header.AddressOf(column).A1} ('{caption}') carry the same caption; "
              + "captions are matched ignoring case and whitespace");

        captions[column] = caption;
      }

      // The captions are settled from the header above; the body is read forward-only from here.
      var rows = new List<IReadOnlyDictionary<string, CellValue>>();

      foreach (var row in table.StreamRows())
      {
        var cells = new Dictionary<string, CellValue>(captions.Length, CaptionComparer.Default);

        for (var column = 0; column < captions.Length; column++)
          cells[captions[column]] = row[column];

        rows.Add(cells);
      }

      // Grown rather than pre-sized, for the reason BindRows gives; the slack goes back the same way.
      rows.TrimExcess();

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
