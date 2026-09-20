using System;
using System.Collections.Generic;

using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The spreadsheet vocabulary as a file scope, imported beside
  /// <c>ProjectionBuilders&lt;TSpace&gt;</c> and closed over the same space:
  /// <code>
  /// using static Unrect.Projections.ProjectionBuilders&lt;Unrect.Spreadsheets.ISpreadsheetSpace&gt;;
  /// using static Unrect.Spreadsheets.SpreadsheetProjectionBuilders&lt;Unrect.Spreadsheets.ISpreadsheetSpace&gt;;
  ///
  /// var line = Overlay(o =&gt;
  /// {
  ///     var value   = o.Next(Decimal());
  ///     var formula = o.Next(Formula().Right(1));
  ///
  ///     return o.Build(read =&gt; new Line(read.Of(value), read.Of(formula)));
  /// });
  /// </code>
  /// <para>
  /// <b>The backend rule: overloads, never a second member of the same signature.</b> Methods
  /// imported by two <c>using static</c> directives join one candidate set per simple name, so the
  /// two vocabularies do not collide as wholes — a call is ambiguous only where two candidates of
  /// one name fit it equally well. Most of what is here is a name <c>Unrect</c> cannot spell: the
  /// six kinded leaves, <c>Formula</c>, and the formula matchers. The exceptions are deliberate and
  /// there are two — <c>Table</c> and <c>Record</c>, names the core vocabulary already publishes, to
  /// which this class adds OVERLOADS separated by signature: <c>Table&lt;T&gt;()</c>,
  /// <c>Table&lt;T&gt;(bind)</c> and their <c>onBlank</c> twins bind a record type where the core's
  /// rungs take a header count, a row lambda or a view lambda, and <c>Record&lt;T&gt;(LabelMap)</c>
  /// takes this file's captions where the core's takes a row lambda. What a backend must never do is
  /// republish a core member with the <em>same</em> signature, which would make that one call
  /// ambiguous in every file importing both.
  /// </para>
  /// <para>
  /// <b>Nothing binds classes that are never imported together.</b> This class and
  /// <see cref="SheetProjectionBuilders{TSpace}"/> publish the kinded vocabulary under the same
  /// names on purpose: a file imports one or the other — this one where the declaration reads
  /// formulas, that one where it reads a streamed sheet, which has none — and never both, so those
  /// names never meet.
  /// </para>
  /// <para>
  /// Every member is raised from the capability it needs to the space the file is written over,
  /// which is what a file scope is for: <c>Formula()</c> needs only <c>IFormulaSpace</c> and reads
  /// as a <typeparamref name="TSpace"/> declaration here, so a matcher and a leaf and a layout in
  /// one file all speak of one space. A shared helper wants the opposite — the narrowest space it
  /// actually reads — and is written as a generic method over
  /// <see cref="SpreadsheetProjections"/> directly.
  /// </para>
  /// <para>
  /// <typeparamref name="TSpace"/> is constrained to <see cref="ISpreadsheetSpace"/> rather than to
  /// the one capability today's members happen to need: the bundle is this package's versioning
  /// commitment, so a capability joining it later joins this class without moving its constraint.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space every declaration in the importing file is written over.</typeparam>
  public static class SpreadsheetProjectionBuilders<TSpace>
    where TSpace : class, ISpreadsheetSpace
  {
    /// <inheritdoc cref="SpreadsheetProjections.Text{TSpace}()"/>
    public static IProjectionDefinition<TSpace, string> Text() => SpreadsheetProjections.Text<TSpace>();

    /// <inheritdoc cref="SpreadsheetProjections.Decimal{TSpace}()"/>
    public static IProjectionDefinition<TSpace, decimal> Decimal() => SpreadsheetProjections.Decimal<TSpace>();

    /// <inheritdoc cref="SpreadsheetProjections.Integer{TSpace}()"/>
    public static IProjectionDefinition<TSpace, int> Integer() => SpreadsheetProjections.Integer<TSpace>();

    /// <inheritdoc cref="SpreadsheetProjections.Double{TSpace}()"/>
    public static IProjectionDefinition<TSpace, double> Double() => SpreadsheetProjections.Double<TSpace>();

    /// <inheritdoc cref="SpreadsheetProjections.Date{TSpace}()"/>
    public static IProjectionDefinition<TSpace, DateTime> Date() => SpreadsheetProjections.Date<TSpace>();

    /// <inheritdoc cref="SpreadsheetProjections.Boolean{TSpace}()"/>
    public static IProjectionDefinition<TSpace, bool> Boolean() => SpreadsheetProjections.Boolean<TSpace>();

    /// <inheritdoc cref="SpreadsheetProjections.Table{TSpace, T}()"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<T>() => SpreadsheetProjections.Table<TSpace, T>();

    /// <inheritdoc cref="SpreadsheetProjections.Table{TSpace, T}(BlankRowStrategy)"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="onBlank">How a fully-blank body row is treated.</param>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<T>(BlankRowStrategy onBlank)
      => SpreadsheetProjections.Table<TSpace, T>(onBlank);

    /// <inheritdoc cref="SpreadsheetProjections.Table{TSpace, T}(Func{TableBinding{TSpace, T}, TableBinding{TSpace, T}})"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="bind">The per-member declarations applied to what reflection would have written.</param>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<T>(Func<TableBinding<TSpace, T>, TableBinding<TSpace, T>> bind)
      => SpreadsheetProjections.Table<TSpace, T>(bind);

    /// <inheritdoc cref="SpreadsheetProjections.Table{TSpace, T}(Func{TableBinding{TSpace, T}, TableBinding{TSpace, T}}, BlankRowStrategy)"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="bind">The per-member declarations applied to what reflection would have written.</param>
    /// <param name="onBlank">How a fully-blank body row is treated.</param>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<T>(Func<TableBinding<TSpace, T>, TableBinding<TSpace, T>> bind, BlankRowStrategy onBlank)
      => SpreadsheetProjections.Table<TSpace, T>(bind, onBlank);

    /// <inheritdoc cref="SpreadsheetProjections.Table{TSpace, T}(int, Func{TableBinding{TSpace, T}, TableBinding{TSpace, T}}, BlankRowStrategy?)"/>
    public static IProjectionDefinition<TSpace, IReadOnlyList<T>> Table<T>(
      int headerRows,
      Func<TableBinding<TSpace, T>, TableBinding<TSpace, T>>? bind = null,
      BlankRowStrategy? onBlank = null)
      => SpreadsheetProjections.Table<TSpace, T>(headerRows, bind, onBlank);

    /// <inheritdoc cref="SpreadsheetProjections.Record{TSpace, T}(LabelMap)"/>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="labels">This file's captions — what a table's bind rung hands its record.</param>
    public static IProjectionDefinition<TSpace, T> Record<T>(LabelMap labels) => SpreadsheetProjections.Record<TSpace, T>(labels);

    /// <inheritdoc cref="SpreadsheetProjections.Formula{TSpace}()"/>
    public static IProjectionDefinition<TSpace, string?> Formula() => SpreadsheetProjections.Formula<TSpace>();

    /// <inheritdoc cref="SpreadsheetProjections.RowWithFormula()"/>
    public static IRowLandmark<TSpace> RowWithFormula() => SpreadsheetProjections.RowWithFormula();

    /// <inheritdoc cref="SpreadsheetProjections.RowWithFormula(string)"/>
    /// <param name="containing">The text the formula must mention.</param>
    public static IRowLandmark<TSpace> RowWithFormula(string containing)
      => SpreadsheetProjections.RowWithFormula(containing);

    /// <inheritdoc cref="SpreadsheetProjections.ColumnWithFormula()"/>
    public static IColumnLandmark<TSpace> ColumnWithFormula() => SpreadsheetProjections.ColumnWithFormula();

    /// <inheritdoc cref="SpreadsheetProjections.ColumnWithFormula(string)"/>
    /// <param name="containing">The text the formula must mention.</param>
    public static IColumnLandmark<TSpace> ColumnWithFormula(string containing)
      => SpreadsheetProjections.ColumnWithFormula(containing);
  }
}
