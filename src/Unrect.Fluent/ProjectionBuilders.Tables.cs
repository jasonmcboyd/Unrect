using System;
using System.Collections.Generic;
using Unrect.Core;
using Unrect.Projections;

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
      => RowBinding<T>.Create<TSpace>(null, typeof(TSpace)).Project<TSpace>(labels);

    /// <summary>
    /// Every body row as a <typeparamref name="T"/> — <see cref="Record{T}(LabelMap)"/> applied to
    /// each row of a table whose header supplies the captions.
    /// <para>
    /// Binding is strict in one direction: every member must find a column, and one that does not is
    /// a loud failure listing the table's captions. A column no member claims is fine — real reports
    /// carry columns a consumer does not want.
    /// </para>
    /// <para>
    /// With <paramref name="headerRows"/> above 1 the header is that many rows tall: the captions, under
    /// <paramref name="headerRows"/> − 1 rows of bands. Members bind to a caption that is unique as
    /// they always have; a column under a band that shares its caption with another is bound by its
    /// path — <c>bind.Column(t =&gt; t.FromId, "From", "Id")</c> — because a flat member never
    /// binds across a band by itself.
    /// </para>
    /// </summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows the header is; at least 1, since members bind by what it says.</param>
    /// <param name="bind">The per-member declarations, or null for none.</param>
    /// <param name="onBlank">
    /// How a fully-blank body row is treated: <c>Stop</c> (the default, self-bounding), <c>Skip</c>,
    /// <c>Fault</c>, or <c>Tolerate</c>. Every non-<c>Stop</c> policy is not self-bounding, so the
    /// table runs to the enclosing edge (declare <c>Until</c> or a count to bound it sooner).
    /// </param>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<T>(
      int headerRows = 1,
      Func<TableBinding<TSpace, T>, TableBinding<TSpace, T>>? bind = null,
      BlankRowStrategy? onBlank = null)
      => Bound<T>(
        bind is null ? RowBinding<T>.Create<TSpace>(null, typeof(TSpace)) : Planned<T>(bind, headerRows),
        onBlank ?? BlankRowStrategy.Stop,
        loose: false,
        headerRows);

    /// <summary>
    /// <inheritdoc cref="Table{T}(int, Func{TableBinding{TSpace, T}, TableBinding{TSpace, T}}, BlankRowStrategy?)"/>
    /// The same table, with the per-member declarations first:
    /// <code>
    /// Table&lt;Transaction&gt;(bind =&gt; bind
    ///   .Column(t =&gt; t.Date, "Transaction Date")
    ///   .Column(t =&gt; t.Type, "Transaction Type"))
    /// </code>
    /// </summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="bind">The per-member declarations applied to what reflection would have written.</param>
    /// <param name="onBlank">How a fully-blank body row is treated; <c>Stop</c> where omitted.</param>
    /// <param name="headerRows">How many rows the header is; 1 where omitted.</param>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<T>(
      Func<TableBinding<TSpace, T>, TableBinding<TSpace, T>> bind,
      BlankRowStrategy? onBlank = null,
      int headerRows = 1)
      => Table<T>(headerRows, bind ?? throw new ArgumentNullException(nameof(bind)), onBlank);

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
    /// <see cref="Table{T}(Func{TableBinding{TSpace, T}, TableBinding{TSpace, T}}, BlankRowStrategy?, int)"/>
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
      => Table(headerRows: headerRows, eachRow: labels => plan.Project<TSpace>(labels, loose), onBlank, declared: null)
        .AsUnit($"Table<{typeof(T).Name}>");
  }
}
