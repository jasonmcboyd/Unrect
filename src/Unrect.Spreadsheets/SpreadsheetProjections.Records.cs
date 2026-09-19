using System;
using System.Collections.Generic;
using System.Linq;

using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The reflective rungs: a record filled from a row by matching member names to captions, and the
  /// table that applies one to every body row.
  /// <para>
  /// They are here rather than in the vocabulary because filling a member asserts a <em>kind</em> —
  /// <c>decimal</c> means "this column holds numbers" — and only a space that carries kinds can
  /// answer that. Everything reflective resolves once, where the projection is written: a bad member
  /// type is an error there, not per file.
  /// </para>
  /// <para>
  /// <b>They are composed, not implemented.</b> A bound table is the vocabulary's own
  /// caption-bound rung with a record projection written by reflection instead of by hand, so
  /// placement, blank-row policy, streaming and diagnostics are the ones every table has.
  /// </para>
  /// </summary>
  public static partial class SpreadsheetProjections
  {
    /// <summary>
    /// One record of <typeparamref name="T"/>, each member filled from the column whose caption
    /// matches its name and read as the member's own type declares.
    /// <para>
    /// Captions bind to members by <see cref="CaptionComparer"/> — case and whitespace are ignored,
    /// so <c>"Contribution ITD"</c> fills <c>ContributionItd</c> with nothing declared. The member's
    /// type chooses the kind to assert and the reading to use, from the closed set the kinded leaves
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
    /// <typeparam name="TSpace">The sheet the record is declared over.</typeparam>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="labels">This file's captions — what a table's bind rung hands its record.</param>
    public static IProjectionDefinition<TSpace, T> Record<TSpace, T>(LabelMap labels)
      where TSpace : class, ISheetCells
      => RecordRow<TSpace, T>(RowBinding<T>.Create(null, typeof(TSpace)), labels);

    /// <summary>
    /// Every body row as a <typeparamref name="T"/> — <see cref="Record{TSpace, T}"/> applied to
    /// each row of a table whose header supplies the captions.
    /// <para>
    /// Binding is strict in one direction: every member must find a column, and one that does not is
    /// a loud failure listing the table's captions. A column no member claims is fine — real reports
    /// carry columns a consumer does not want.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The sheet the table is declared over.</typeparam>
    /// <typeparam name="T">What one record reads.</typeparam>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<TSpace, T>()
      where TSpace : class, ISheetCells
      => Bound<TSpace, T>(RowBinding<T>.Create(null, typeof(TSpace)), BlankRowStrategy.Stop);

    /// <summary>
    /// <see cref="Table{TSpace, T}()"/> with a blank-row strategy: <paramref name="onBlank"/> says
    /// how a fully-blank body row is treated — <c>Stop</c> (the default, self-bounding),
    /// <c>Skip</c>, <c>Fault</c>, or <c>Tolerate</c>. Every non-<c>Stop</c> policy is not
    /// self-bounding, so the table runs to the enclosing edge (declare <c>Until</c> or a count to
    /// bound it sooner).
    /// </summary>
    /// <typeparam name="TSpace">The sheet the table is declared over.</typeparam>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="onBlank">How a fully-blank body row is treated.</param>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<TSpace, T>(BlankRowStrategy onBlank)
      where TSpace : class, ISheetCells
      => Bound<TSpace, T>(RowBinding<T>.Create(null, typeof(TSpace)), onBlank);

    /// <summary>
    /// <see cref="Table{TSpace, T}()"/> with per-member declarations: <c>Column</c> for a caption
    /// the comparer would not have found, <c>Ignore</c> for a member this table does not carry.
    /// <code>
    /// Table&lt;Transaction&gt;(bind =&gt; bind
    ///   .Column(t =&gt; t.Date, "Transaction Date")
    ///   .Column(t =&gt; t.Type, "Transaction Type"))
    /// </code>
    /// </summary>
    /// <typeparam name="TSpace">The sheet the table is declared over.</typeparam>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="bind">The per-member declarations applied to what reflection would have written.</param>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<TSpace, T>(Func<TableBinding<T>, TableBinding<T>> bind)
      where TSpace : class, ISheetCells
      => Bound<TSpace, T>(Planned<TSpace, T>(bind), BlankRowStrategy.Stop);

    /// <summary>
    /// <see cref="Table{TSpace, T}(Func{TableBinding{T}, TableBinding{T}})"/> with a blank-row
    /// strategy: <paramref name="onBlank"/> says how a fully-blank body row is treated — <c>Stop</c>
    /// (the default, self-bounding), <c>Skip</c>, <c>Fault</c>, or <c>Tolerate</c>. Every
    /// non-<c>Stop</c> policy is not self-bounding, so the table runs to the enclosing edge (declare
    /// <c>Until</c> or a count to bound it sooner).
    /// </summary>
    /// <typeparam name="TSpace">The sheet the table is declared over.</typeparam>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="bind">The per-member declarations applied to what reflection would have written.</param>
    /// <param name="onBlank">How a fully-blank body row is treated.</param>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<TSpace, T>(Func<TableBinding<T>, TableBinding<T>> bind, BlankRowStrategy onBlank)
      where TSpace : class, ISheetCells
      => Bound<TSpace, T>(Planned<TSpace, T>(bind), onBlank);

    private static RowBinding<T> Planned<TSpace, T>(Func<TableBinding<T>, TableBinding<T>> bind)
      where TSpace : class, ISheetCells
      => RowBinding<T>.Create(
        (bind ?? throw new ArgumentNullException(nameof(bind)))(new TableBinding<T>())
        ?? throw new ArgumentException("The binding lambda returned null.", nameof(bind)),
        typeof(TSpace));

    private static IProjectionDefinition<TSpace, IReadOnlyList<T>> Bound<TSpace, T>(RowBinding<T> plan, BlankRowStrategy onBlank)
      where TSpace : class, ISheetCells
      => ProjectionBuilders<TSpace>
        .Table(headerRows: 1, eachRow: labels => RecordRow<TSpace, T>(plan, labels), onBlank, declared: null)
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
    private static IProjectionDefinition<TSpace, T> RecordRow<TSpace, T>(RowBinding<T> plan, LabelMap labels)
      where TSpace : class, ISheetCells
    {
      var columns = Columns<T>(plan, labels);
      var members = new IProjectionDefinition<TSpace, object?>[plan.Members.Count];

      for (var member = 0; member < members.Length; member++)
        members[member] = ProjectionBuilders<TSpace>
          .Right(columns[member])
          .Of(KindedLeaves.For<TSpace>(plan.Members[member]))
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

    /// <summary>
    /// The column each member reads, or one failure naming every member that found none. The
    /// example names an UNBOUND member: advice pointing at a member which already found its column
    /// would send a reader to fix the one thing that is not broken.
    /// </summary>
    private static int[] Columns<T>(RowBinding<T> plan, LabelMap labels)
    {
      var columns = new int[plan.Members.Count];
      var unbound = new List<string>();

      for (var member = 0; member < plan.Members.Count; member++)
      {
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

        var matches = labels.Bound(plan.Members[member].Caption);

        // A member with no column joins the aggregate below; one with two is a table nobody can read
        // by name, and it says so at once, naming the member rather than the caption.
        if (matches.Count == 0)
          unbound.Add(plan.Members[member].Name);
        else if (matches.Count > 1)
          throw Ambiguous<T>(labels, plan.Members[member], matches);
        else
          columns[member] = matches[0];
      }

      if (unbound.Count == 0)
        return columns;

      throw labels.Failure(
        $"no column binds {Join(unbound.Select(name => $"{typeof(T).Name}.{name}").ToList())}; the table's captions are "
        + $"{string.Join(", ", labels.Labels.Select(caption => $"'{caption}'"))}. "
        + $"Bind one with Column(t => t.{unbound[0]}, \"…\") or drop it with Ignore(t => t.{unbound[0]})");
    }

    /// <summary>
    /// A bound column's path segment: the caption the member binds by, or — bound by position — the
    /// caption that column carries, and its position where it carries none.
    /// </summary>
    private static string ColumnName(MemberPlan member, LabelMap labels, int column)
      => member.Position is null ? $"column '{member.Caption}'"
        : labels.Labels[column].Length > 0 ? $"column '{labels.Labels[column]}'"
        : $"column {column}";

    /// <summary>
    /// A member whose caption two columns carry: both are named, with their own spellings, because
    /// the fix is in the file or in a <c>Column(…)</c> declaration and the reader needs to see which.
    /// </summary>
    private static ProjectionException Ambiguous<T>(LabelMap labels, MemberPlan member, IReadOnlyList<int> matches)
      => labels.Failure(
        $"{typeof(T).Name}.{member.Name} matches the columns at "
        + $"{labels.AddressOf(matches[0]).A1} ('{labels.Labels[matches[0]]}') and "
        + $"{labels.AddressOf(matches[1]).A1} ('{labels.Labels[matches[1]]}'); "
        + "captions are matched ignoring case and whitespace. "
        + $"Bind it by position with Column(t => t.{member.Name}, {matches[0]})");

    private static string Join(IReadOnlyList<string> names)
      => names.Count == 1
        ? names[0]
        : string.Join(", ", names.Take(names.Count - 1)) + " or " + names[names.Count - 1];
  }
}
