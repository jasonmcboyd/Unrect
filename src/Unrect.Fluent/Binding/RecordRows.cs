using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;

namespace Unrect.Fluent
{
  /// <summary>
  /// A plan projected over one row's labels: the record projection a reflective table applies per
  /// row. The captions are resolved here, once per application of the table, and every member that
  /// found none is reported together — a record that named three columns the file does not carry
  /// says so in one sentence rather than three files later.
  /// </summary>
  internal static class RecordRows
  {
    private const int Unbound = -1;
    private const int FromRow = -2;

    /// <summary>
    /// The plan as a projection of one row: an overlay, because a caption's position is absolute —
    /// every member is handed the whole row and says which column it is, so a file that reorders its
    /// columns changes nothing but the numbers the map hands back.
    /// </summary>
    internal static IProjectionDefinition<TSpace, T> Project<TSpace, T>(RowBinding<T> plan, LabelMap labels, bool loose)
      where TSpace : class, ISpace
    {
      var columns = BoundColumns<T>(plan, labels, loose);
      var members = new IProjectionDefinition<TSpace, object?>[plan.Members.Count];

      for (var member = 0; member < members.Length; member++)
        members[member] = columns[member] == Unbound
          // A loose table's member that found no column: its default, read from nowhere.
          ? Defaulted<TSpace>(plan.Members[member])
          : plan.Members[member].Reading is Func<object, object?> reading
          // The member's own reading of the row: the whole band, under the table's captions.
          ? ProjectionBuilders<TSpace>.Record(row => reading(row)).AsUnit($"member '{plan.Members[member].Name}'")
          : ProjectionBuilders<TSpace>
          .Right(columns[member])
          .Of(MemberReads.For<TSpace>(plan.Members[member]))
          .AsUnit(ColumnName(plan.Members[member], labels, columns[member]));

      // Scaffolding: the overlay is how this rung is assembled, not something the declaration wrote,
      // so it contributes no segment of its own and a path reads Table<Txn>[2] -> column 'Amount' —
      // the table with the record's row index on it, then the column.
      return ProjectionBuilders<TSpace>.Overlay(cursor =>
      {
        var values = new object?[members.Length];

        // declared: null, and it is mandatory. Left to the compiler, the naming ladder would label
        // every member with this loop's own variable, an identifier the user never wrote.
        for (var member = 0; member < members.Length; member++)
          values[member] = cursor.Next(members[member], declared: null);

        // The record is the user's type, which may refuse a null it would never otherwise see, so
        // it is not built on the declaration pass, where every value is one.
        return cursor.Recording ? default! : plan.Materialize(values);
      })
      .AsScaffolding();
    }

    /// <summary>What a member no column binds is left holding: null where it can hold one, its type's default where it cannot.</summary>
    private static object? Default(MemberPlan member)
      => member.BlankTolerant || !member.Type.IsValueType ? null : Activator.CreateInstance(member.Type);

    /// <summary>
    /// The member as a projection that reads nothing. Its own method so that the value is computed
    /// here, once, rather than captured with a loop variable that has moved on by the time it runs.
    /// </summary>
    private static IProjectionDefinition<TSpace, object?> Defaulted<TSpace>(MemberPlan member)
      where TSpace : class, ISpace
    {
      var value = Default(member);

      return ProjectionBuilders<TSpace>.Record(_ => value).AsUnit($"member '{member.Name}'");
    }

    /// <summary>
    /// The column each member reads, or one failure naming every member that found none. The
    /// example names an UNBOUND member: advice pointing at a member which already found its column
    /// would send a reader to fix the one thing that is not broken.
    /// </summary>
    private static int[] BoundColumns<T>(RowBinding<T> plan, LabelMap labels, bool loose)
    {
      var columns = new int[plan.Members.Count];
      var unbound = new List<string>();

      for (var member = 0; member < plan.Members.Count; member++)
      {
        // Filled from the row: it reads no column of its own.
        if (plan.Members[member].Reading is not null)
        {
          columns[member] = FromRow;
          continue;
        }

        // Bound by its path through a banded header: the header answers, or says what it holds.
        if (plan.Members[member].Path is LabelStep[] path)
        {
          columns[member] = labels.Column(path, $"{typeof(T).Name}.{plan.Members[member].Name}");
          continue;
        }

        // Bound by position: the caption is not consulted, so neither a missing one nor a
        // duplicated one is this member's problem. The table has to be that wide.
        if (plan.Members[member].Position is int position)
        {
          if (position >= labels.Labels.Count)
            throw labels.Failure(
              $"{typeof(T).Name}.{plan.Members[member].Name} is bound to column {position}, and the table has "
              + $"{labels.Labels.Count} column{(labels.Labels.Count == 1 ? string.Empty : "s")} (0 to {labels.Labels.Count - 1})");

          columns[member] = position;
          continue;
        }

        // One name, one column. A member answers to a caption, and to the column whose whole path
        // run together is its name — FromId for the column at From, Id. Where both kinds answer,
        // that is two columns, and it is refused like any other two.
        var matches = labels.Bound(plan.Members[member].Caption)
          .Union(labels.BoundByPath(plan.Members[member].Caption))
          .OrderBy(column => column)
          .ToList();

        // A member with no column joins the aggregate below; one with two is a table nobody can read
        // by name, and it says so at once, naming the member rather than the caption.
        if (matches.Count == 0)
        {
          unbound.Add(plan.Members[member].Name);
          columns[member] = Unbound;
        }
        else if (matches.Count > 1)
          throw Ambiguous<T>(labels, plan.Members[member], matches);
        else
          columns[member] = matches[0];
      }

      if (loose)
      {
        Tell<T>(plan, labels, columns, unbound);
        return columns;
      }

      if (unbound.Count == 0)
        return columns;

      throw labels.Failure(
        $"no column binds {Join(unbound.Select(name => $"{typeof(T).Name}.{name}").ToList())}; {CaptionsOf(labels)}. "
        + $"Bind one with Column(t => t.{unbound[0]}, \"…\") or drop it with Ignore(t => t.{unbound[0]})");
    }

    /// <summary>
    /// What a loose table says where a strict one would have refused or stayed silent: the members
    /// that found no column, and the columns no member reads by name.
    /// </summary>
    private static void Tell<T>(RowBinding<T> plan, LabelMap labels, int[] columns, List<string> unbound)
    {
      if (unbound.Count > 0)
        labels.Note(
          DiagnosticSeverity.Warning,
          $"no column binds {Join(unbound.Select(name => $"{typeof(T).Name}.{name}").ToList())}, left at "
          + $"{(unbound.Count == 1 ? "its default" : "their defaults")}; {CaptionsOf(labels)}");

      var unread = Enumerable.Range(0, labels.Labels.Count)
        .Where(column => labels.Labels[column].Length > 0 && !columns.Contains(column))
        .Select(column => Said(labels, column))
        .ToList();

      if (unread.Count > 0)
        labels.Note(
          DiagnosticSeverity.Info,
          $"no member of {typeof(T).Name} reads the column{(unread.Count == 1 ? string.Empty : "s")} {string.Join(", ", unread)}");
    }

    /// <summary>
    /// A bound column's path segment: the caption the member binds by, or — bound by position — the
    /// caption that column carries, and its position where it carries none.
    /// </summary>
    private static string ColumnName(MemberPlan member, LabelMap labels, int column)
      => member.Path is LabelStep[] path ? $"column {string.Join(", ", path.Select(step => step.ToString()))}"
        : member.Position is null ? $"column '{member.Caption}'"
        : labels.Labels[column].Length > 0 ? $"column '{labels.Labels[column]}'"
        : $"column {column}";

    /// <summary>
    /// A member whose caption two columns carry: both are named, with their own spellings, because
    /// the fix is in the file or in a <c>Column(…)</c> declaration and the reader needs to see which.
    /// </summary>
    private static ProjectionException Ambiguous<T>(LabelMap labels, MemberPlan member, IReadOnlyList<int> matches)
      => labels.Failure(
        $"{typeof(T).Name}.{member.Name} matches the columns at "
        + $"{labels.AddressOf(matches[0]).A1} ({Said(labels, matches[0])}) and "
        + $"{labels.AddressOf(matches[1]).A1} ({Said(labels, matches[1])}); "
        + "captions are matched ignoring case and whitespace. "
        + (labels.Paths[matches[0]].Count > 1
          ? $"Bind it by its path with Column(t => t.{member.Name}, {string.Join(", ", labels.Paths[matches[0]].Select(step => $"\"{step}\""))})"
          : $"Bind it by position with Column(t => t.{member.Name}, {matches[0]})"));

    /// <summary>
    /// What a table holds, for the failure that has to say so: its captions, or — under bands — its
    /// columns' paths, which are what a flat member's name is matched against run together.
    /// </summary>
    private static string CaptionsOf(LabelMap labels)
      => labels.Depth > 1 && labels.Paths.Any(path => path.Count > 1)
        ? "the table's columns are " + string.Join(", ", Enumerable.Range(0, labels.Labels.Count).Where(column => labels.Labels[column].Length > 0).Select(column => Said(labels, column)))
          + " — a member binds to a caption, or to a whole path run together (FromId for [\"From\", \"Id\"])"
        : "the table's captions are " + string.Join(", ", labels.Labels.Select(caption => $"'{caption}'"));

    /// <summary>How a column is cited: its caption, or its whole path where it sits under a band.</summary>
    private static string Said(LabelMap labels, int column)
      => labels.Paths[column].Count > 1
        ? "[" + string.Join(", ", labels.Paths[column].Select(step => $"\"{step}\"")) + "]"
        : $"'{labels.Labels[column]}'";

    private static string Join(IReadOnlyList<string> names)
      => names.Count == 1
        ? names[0]
        : string.Join(", ", names.Take(names.Count - 1)) + " or " + names[names.Count - 1];
  }
}
