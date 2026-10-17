using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Strategies;

namespace Unrect.Fluent
{
  // --- The reflective rungs ------------------------------------------------------------------------
  //
  // A record filled from a row by matching member names to captions, and the table that applies
  // one to every body row. They are COMPOSED, not implemented: a bound table is the vocabulary's own
  // caption-bound rung with a record projection written by reflection instead of by hand, so
  // placement, blank-row policy, streaming and diagnostics are the ones every table has. Filling a
  // member reads a cell as the member's type — the same reading Point().Decimal() is — and every
  // space answers those reads, so the rungs are generic in the space like everything else here.
  // Everything reflective resolves once, where the projection is written: a bad member type is an
  // error there, not per file.

  public static partial class ProjectionBuilders<TSpace>
    where TSpace : class, ISpace
  {
    // --- Held-text matching -----------------------------------------------------------------------
    //
    // The generic matchers ask what a cell SAYS, which every space answers. These ask what a cell
    // HOLDS — text cells alone, so a numeric 2024 is not a row containing "2024" — which is a
    // question about the value's kind, and so a sheet's to ask. The comparison is the one every
    // matcher and caption shares (CellMatching.TextComparer): whole-cell, trimmed, case-insensitive.

    /// <summary>
    /// The first row holding <paramref name="text"/> as a whole cell value, trimmed and
    /// case-insensitively — text cells alone; <c>RowSaying</c> is the same comparison against what
    /// every cell says.
    /// </summary>
    public static ILineLandmark<TSpace> RowContaining(string text)
      => (RowLandmarks.RowWhere(
        CellMatching.AnyCellInRow(Holding(NotNullText(text))), $"no row containing '{text}'"));

    /// <summary>The column twin of <see cref="RowContaining"/>, with the same rule.</summary>
    public static ILineLandmark<TSpace> ColumnContaining(string text)
      => (ColumnLandmarks.ColumnWhere(
        CellMatching.AnyCellInColumn(Holding(NotNullText(text))), $"no column containing '{text}'"));

    /// <summary>
    /// Rows up to and including the first whose cell in <paramref name="column"/> holds
    /// <paramref name="text"/> — whole-cell, trimmed, case-insensitive, text cells alone.
    /// </summary>
    public static ILineStrategy<TSpace> TakeRowsToText(int column, string text)
    {
      var matches = Holding(NotNullText(text));

      return RowStrategies.TakeRowsTo<TSpace>((space, row) => matches(space[column, row]));
    }

    /// <summary>The column twin of <see cref="TakeRowsToText"/>: columns up to and including the first whose cell in <paramref name="row"/> holds <paramref name="text"/>.</summary>
    public static ILineStrategy<TSpace> TakeColumnsToText(int row, string text)
    {
      var matches = Holding(NotNullText(text));

      return ColumnStrategies.TakeColumnsTo<TSpace>((space, column) => matches(space[column, row]));
    }

    /// <summary>A cell holding <paramref name="text"/>, lowered to the calculus: the value's own text, compared by the shared rule.</summary>
    private static Func<Point<TSpace>, bool> Holding(string text)
      => point => point.TryGetText(out var held) && CellMatching.TextComparer.Equals(held, text);

    private static string NotNullText(string text) => text ?? throw new ArgumentNullException(nameof(text));

    /// <summary>
    /// One record of <typeparamref name="T"/>, each member filled from the column whose caption
    /// matches its name and read as the member's own type declares.
    /// <para>
    /// Captions bind to members by <see cref="CaptionComparer"/> — case and whitespace are ignored,
    /// so <c>"Contribution ITD"</c> fills <c>ContributionItd</c> with nothing declared. The member's
    /// type chooses the kind to assert and the reading to use, from the closed set the readings
    /// cover: <c>string</c>, <c>decimal</c>, <c>double</c>, <c>int</c>, <c>DateTime</c>, <c>bool</c>,
    /// and the nullable forms. A nullable member tolerates a <em>blank</em> cell and still fails on
    /// the wrong kind — tolerating a blank says something about the data, tolerating a kind would
    /// say something about the format, and no real format has that.
    /// </para>
    /// <para>
    /// One member type asserts nothing: <c>Point&lt;TSpace&gt;</c> is filled with the address of the
    /// labelled cell and leaves every question about it to the reader — the escape hatch for a column
    /// the six kinds do not describe, and the reason the closed set needs no seventh entry.
    /// </para>
    /// <para>
    /// Each member reads through a leaf marked as its column, so a bad cell is reported at
    /// <c>Table&lt;Money&gt;[0] -&gt; column 'Amount'</c>, with the problem the leaf's own
    /// (<c>expected Number at A2, found Text</c>): the caption is a path segment rather than a prefix
    /// on the problem, which is what makes it drill-through-able, and the index is the body row it
    /// was read from.
    /// </para>
    /// </summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="labels">This file's captions — what a table's bind rung hands its record.</param>
    public static IProjectionDefinition<TSpace, T> Record<T>(LabelMap labels)
      => RecordRow<T>(RowBinding<T>.Create<TSpace>(null, typeof(TSpace)), labels);

    /// <summary>
    /// Every body row as a <typeparamref name="T"/> — <see cref="Record{T}(LabelMap)"/> applied to
    /// each row of a table whose header supplies the captions.
    /// <para>
    /// Binding is strict in one direction: every member must find a column, and one that does not is
    /// a loud failure listing the table's captions. A column no member claims is fine — real reports
    /// carry columns a consumer does not want.
    /// </para>
    /// </summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<T>()
      => Bound<T>(RowBinding<T>.Create<TSpace>(null, typeof(TSpace)), BlankRowStrategy.Stop);

    /// <summary>
    /// <see cref="Table{T}()"/> with a blank-row strategy: <paramref name="onBlank"/> says
    /// how a fully-blank body row is treated — <c>Stop</c> (the default, self-bounding),
    /// <c>Skip</c>, <c>Fault</c>, or <c>Tolerate</c>. Every non-<c>Stop</c> policy is not
    /// self-bounding, so the table runs to the enclosing edge (declare <c>Until</c> or a count to
    /// bound it sooner).
    /// </summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="onBlank">How a fully-blank body row is treated.</param>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<T>(BlankRowStrategy onBlank)
      => Bound<T>(RowBinding<T>.Create<TSpace>(null, typeof(TSpace)), onBlank);

    /// <summary>
    /// <see cref="Table{T}()"/> with per-member declarations: <c>Column</c> for a caption
    /// the comparer would not have found, <c>Ignore</c> for a member this table does not carry.
    /// <code>
    /// Table&lt;Transaction&gt;(bind =&gt; bind
    ///   .Column(t =&gt; t.Date, "Transaction Date")
    ///   .Column(t =&gt; t.Type, "Transaction Type"))
    /// </code>
    /// </summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="bind">The per-member declarations applied to what reflection would have written.</param>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<T>(Func<TableBinding<TSpace, T>, TableBinding<TSpace, T>> bind)
      => Bound<T>(Planned<T>(bind), BlankRowStrategy.Stop);

    /// <summary>
    /// <see cref="Table{T}(Func{TableBinding{TSpace, T}, TableBinding{TSpace, T}})"/> with a blank-row
    /// strategy: <paramref name="onBlank"/> says how a fully-blank body row is treated — <c>Stop</c>
    /// (the default, self-bounding), <c>Skip</c>, <c>Fault</c>, or <c>Tolerate</c>. Every
    /// non-<c>Stop</c> policy is not self-bounding, so the table runs to the enclosing edge (declare
    /// <c>Until</c> or a count to bound it sooner).
    /// </summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="bind">The per-member declarations applied to what reflection would have written.</param>
    /// <param name="onBlank">How a fully-blank body row is treated.</param>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<T>(Func<TableBinding<TSpace, T>, TableBinding<TSpace, T>> bind, BlankRowStrategy onBlank)
      => Bound<T>(Planned<T>(bind), onBlank);

    /// <summary>
    /// A table whose header is <paramref name="headerRows"/> rows tall: the captions, under
    /// <paramref name="headerRows"/> − 1 rows of bands. Members bind to a caption that is unique as
    /// they always have; a column under a band that shares its caption with another is bound by its
    /// path — <c>bind.Column(t =&gt; t.FromId, "From", "Id")</c> — because a flat member never
    /// binds across a band by itself.
    /// </summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows the header is; at least 1, since members bind by what it says.</param>
    /// <param name="bind">The per-member declarations, or null for none.</param>
    /// <param name="onBlank">How a fully-blank body row is treated; <c>Stop</c> where omitted.</param>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<T>(
      int headerRows,
      Func<TableBinding<TSpace, T>, TableBinding<TSpace, T>>? bind = null,
      BlankRowStrategy? onBlank = null)
      => Bound<T>(
        bind is null ? RowBinding<T>.Create<TSpace>(null, typeof(TSpace)) : Planned<T>(bind, headerRows),
        onBlank ?? BlankRowStrategy.Stop,
        loose: false,
        headerRows);

    private static RowBinding<T> Planned<T>(Func<TableBinding<TSpace, T>, TableBinding<TSpace, T>> bind, int headerRows = 1)
    {
      var binding = (bind ?? throw new ArgumentNullException(nameof(bind)))(new TableBinding<TSpace, T>())
        ?? throw new ArgumentException("The binding lambda returned null.", nameof(bind));

      // A path goes through one band per step before its last, and a header has one row of bands
      // per row above its captions — so this can be refused here, before any file is opened.
      foreach (var bound in binding.Paths)
        if (bound.Value.Length > headerRows)
          throw new ArgumentException(
            $"{typeof(T).Name}.{bound.Key} is bound to a path of {bound.Value.Length} steps, and the table declares "
            + $"{headerRows} header row{(headerRows == 1 ? string.Empty : "s")}, which is a header {(headerRows == 1 ? "with no bands" : $"{headerRows} deep")}; "
            + $"declare headerRows: {bound.Value.Length}, or shorten the path.",
            nameof(bind));

      return RowBinding<T>.Create(binding, typeof(TSpace));
    }

    /// <summary>
    /// The exploratory table <c>Unrect.Interactive</c> offers: the same projection as
    /// <see cref="Table{T}(Func{TableBinding{TSpace, T}, TableBinding{TSpace, T}}, BlankRowStrategy)"/>
    /// with its one strictness switched. A member no column binds is left at its default and said
    /// so in a Warning, and the columns no member reads are listed in an Info — the two things a
    /// type still being written wants to be told. Everything else fails as it always does.
    /// <para>
    /// Internal, and reached only through that package on purpose: a shipped declaration does not
    /// reference it, so a forgiving table cannot reach production without a package reference a
    /// reviewer can see.
    /// </para>
    /// </summary>
    internal static IProjectionDefinition<TSpace, IReadOnlyList<T>> LooseTable<T>(
      Func<TableBinding<TSpace, T>, TableBinding<TSpace, T>>? bind,
      BlankRowStrategy onBlank,
      int headerRows = 1)
      => Bound<T>(
        bind is null ? RowBinding<T>.Create<TSpace>(null, typeof(TSpace)) : Planned<T>(bind, headerRows),
        onBlank,
        loose: true,
        headerRows);

    private static IProjectionDefinition<TSpace, IReadOnlyList<T>> Bound<T>(RowBinding<T> plan, BlankRowStrategy onBlank, bool loose = false, int headerRows = 1)
      => Table(headerRows: headerRows, eachRow: labels => RecordRow<T>(plan, labels, loose), onBlank, declared: null)
        .AsUnit($"Table<{typeof(T).Name}>");

    /// <summary>
    /// The plan as a projection of one row: an overlay, because a caption's position is absolute —
    /// every member is handed the whole row and says which column it is, so a file that reorders its
    /// columns changes nothing but the numbers the map hands back.
    /// <para>
    /// The captions are resolved here, once per application of the table, and every member that
    /// found none is reported together: a record that named three columns the file does not carry
    /// says so in one sentence rather than three files later.
    /// </para>
    /// </summary>
    private static IProjectionDefinition<TSpace, T> RecordRow<T>(RowBinding<T> plan, LabelMap labels, bool loose = false)
    {
      var columns = Columns<T>(plan, labels, loose);
      var members = new IProjectionDefinition<TSpace, object?>[plan.Members.Count];

      for (var member = 0; member < members.Length; member++)
        members[member] = columns[member] == Unbound
          // A loose table's member that found no column: its default, read from nowhere.
          ? Defaulted(plan.Members[member])
          : plan.Members[member].Reading is Func<object, object?> reading
          // The member's own reading of the row: the whole band, under the table's captions.
          ? Record(row => reading(row)).AsUnit($"member '{plan.Members[member].Name}'")
          : ProjectionBuilders<TSpace>
          .Right(columns[member])
          .Of(MemberReads.For<TSpace>(plan.Members[member]))
          .AsUnit(ColumnName(plan.Members[member], labels, columns[member]));

      // Scaffolding: the overlay is how this rung is assembled, not something the declaration wrote,
      // so it contributes no segment of its own and a path reads Table<Txn>[2] -> column 'Amount' —
      // the table with the record's row index on it, then the column.
      return Overlay(cursor =>
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

    /// <summary>
    /// The column each member reads, or one failure naming every member that found none. The
    /// example names an UNBOUND member: advice pointing at a member which already found its column
    /// would send a reader to fix the one thing that is not broken.
    /// </summary>
    private const int Unbound = -1;
    private const int FromRow = -2;

    /// <summary>What a member no column binds is left holding: null where it can hold one, its type's default where it cannot.</summary>
    private static object? Default(MemberPlan member)
      => member.BlankTolerant || !member.Type.IsValueType ? null : Activator.CreateInstance(member.Type);

    /// <summary>
    /// The member as a projection that reads nothing. Its own method so that the value is computed
    /// here, once, rather than captured with a loop variable that has moved on by the time it runs.
    /// </summary>
    private static IProjectionDefinition<TSpace, object?> Defaulted(MemberPlan member)
    {
      var value = Default(member);

      return Record(_ => value).AsUnit($"member '{member.Name}'");
    }

    private static int[] Columns<T>(RowBinding<T> plan, LabelMap labels, bool loose = false)
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
        $"no column binds {Join(unbound.Select(name => $"{typeof(T).Name}.{name}").ToList())}; {Columns(labels)}. "
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
          + $"{(unbound.Count == 1 ? "its default" : "their defaults")}; {Columns(labels)}");

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
    private static string Columns(LabelMap labels)
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
