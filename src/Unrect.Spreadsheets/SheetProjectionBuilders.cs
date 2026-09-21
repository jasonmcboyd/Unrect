using System;
using System.Collections.Generic;

using Unrect.Projections;

namespace Unrect.Spreadsheets
{
  /// <summary>
  /// The kinded vocabulary as a file scope, for a declaration written over a sheet that carries no
  /// formulas — which is what the streaming door vends:
  /// <code>
  /// using static Unrect.Projections.ProjectionBuilders&lt;Unrect.Spreadsheets.ICellSpace&gt;;
  /// using static Unrect.Spreadsheets.SheetProjectionBuilders&lt;Unrect.Spreadsheets.ICellSpace&gt;;
  ///
  /// var row = HorizontalFlow(h =&gt;
  /// {
  ///     var label  = h.Next(Text());
  ///     var amount = h.Next(Decimal());
  ///
  ///     return h.Build(read =&gt; new Line(read.Of(label), read.Of(amount)));
  /// });
  /// </code>
  /// <para>
  /// It is <see cref="SpreadsheetProjectionBuilders{TSpace}"/> without the four members that need a
  /// formula, and its constraint says so: <see cref="Workbook.Sheet"/> hands back an
  /// <see cref="ICellSpace"/>, so a streamed declaration cannot be scoped to the bundle and would
  /// otherwise have no closed vocabulary at all.
  /// </para>
  /// <para>
  /// <b>Import one of the two, never both.</b> They publish the kinded vocabulary under the same
  /// names deliberately; what a name can collide with is what a file imports beside it, and these
  /// two are never imported together. Which one a file wants is the same question as which one its
  /// space can answer.
  /// </para>
  /// <para>
  /// Against the core vocabulary the rule is <see cref="SpreadsheetProjectionBuilders{TSpace}"/>'s:
  /// <c>Table</c> and <c>Record</c> are names the core already publishes, and what is added here are
  /// OVERLOADS separated by signature — never a second member the core publishes with the same one.
  /// </para>
  /// </summary>
  /// <typeparam name="TSpace">The space every declaration in the importing file is written over.</typeparam>
  public static class SheetProjectionBuilders<TSpace>
    where TSpace : class, ICellSpace
  {
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
  }
}
