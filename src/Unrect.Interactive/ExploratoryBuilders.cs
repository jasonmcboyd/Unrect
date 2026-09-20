using System;
using System.Collections.Generic;

using Unrect.Projections;
using Unrect.Spreadsheets;

namespace Unrect.Interactive
{
  /// <summary>
  /// The forgiving table, for a type that is still being written:
  /// <code>
  /// using static Unrect.Interactive.ExploratoryBuilders&lt;Unrect.Spreadsheets.ISpreadsheetSpace&gt;;
  ///
  /// var read = LooseTable&lt;FundStructure&gt;().MapWithDiagnostics(sheet);
  /// </code>
  /// <para>
  /// <c>Table&lt;T&gt;()</c> is strict about one thing on purpose: a member no column binds is a
  /// failure, because next month's export renaming a caption must not turn into a column of nulls
  /// nobody notices. That is the right rule for a declaration that ships and the wrong one for the
  /// hour in which a property is added before the code that fills it. A loose table leaves such a
  /// member at its default and says so in a Warning, and it lists — as an Info — the columns no
  /// member reads, which is the other thing a half-written type wants to know. Wrong kinds,
  /// duplicated captions and every other failure are facts about the file and stay loud.
  /// </para>
  /// <para>
  /// It is the same projection as <c>Table&lt;T&gt;</c> with that one decision switched, so every
  /// binding, the blank-row strategies and the failure paths are identical. It lives here so that
  /// promoting a script is mechanical: drop the reference to this package, and every loose table is
  /// a compile error whose fix is a rename to <c>Table&lt;T&gt;</c>.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The sheet every declaration in the file is written against.</typeparam>
  public static class ExploratoryBuilders<TSpace>
    where TSpace : class, ISheetCells
  {
    /// <summary><c>Table&lt;T&gt;()</c>, forgiving a member no column binds.</summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> LooseTable<T>()
      => SpreadsheetProjections.LooseTable<TSpace, T>(null, BlankRowStrategy.Stop);

    /// <summary><c>Table&lt;T&gt;(onBlank)</c>, forgiving a member no column binds.</summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="onBlank">How a fully-blank body row is treated.</param>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> LooseTable<T>(BlankRowStrategy onBlank)
      => SpreadsheetProjections.LooseTable<TSpace, T>(null, onBlank);

    /// <summary><c>Table&lt;T&gt;(bind =&gt; …)</c>, forgiving a member no column binds.</summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="bind">The per-member declarations, exactly as <c>Table&lt;T&gt;</c> takes them.</param>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> LooseTable<T>(Func<TableBinding<TSpace, T>, TableBinding<TSpace, T>> bind)
      => SpreadsheetProjections.LooseTable<TSpace, T>(bind ?? throw new ArgumentNullException(nameof(bind)), BlankRowStrategy.Stop);

    /// <summary><c>Table&lt;T&gt;(bind =&gt; …, onBlank)</c>, forgiving a member no column binds.</summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="bind">The per-member declarations, exactly as <c>Table&lt;T&gt;</c> takes them.</param>
    /// <param name="onBlank">How a fully-blank body row is treated.</param>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> LooseTable<T>(Func<TableBinding<TSpace, T>, TableBinding<TSpace, T>> bind, BlankRowStrategy onBlank)
      => SpreadsheetProjections.LooseTable<TSpace, T>(bind ?? throw new ArgumentNullException(nameof(bind)), onBlank);

    /// <summary><c>Table&lt;T&gt;(headerRows, …)</c> — a header of captions under rows of bands — forgiving a member no column binds.</summary>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows the header is.</param>
    /// <param name="bind">The per-member declarations, or null for none.</param>
    /// <param name="onBlank">How a fully-blank body row is treated; <c>Stop</c> where omitted.</param>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> LooseTable<T>(
      int headerRows,
      Func<TableBinding<TSpace, T>, TableBinding<TSpace, T>>? bind = null,
      BlankRowStrategy? onBlank = null)
      => SpreadsheetProjections.LooseTable<TSpace, T>(bind, onBlank ?? BlankRowStrategy.Stop, headerRows);
  }
}
