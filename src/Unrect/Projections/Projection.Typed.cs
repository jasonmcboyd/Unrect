using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Projections
{
  /// <summary>
  /// The composing factories that have to be told what they are declared
  /// over.
  /// <para>
  /// Only the factories that <em>take</em> projections appear here. A leaf demands nothing, so
  /// <c>Text()</c>, <c>Range</c>, <c>Table&lt;T&gt;()</c> and the rest are unchanged and run
  /// everywhere by variance; and a modifier is written once and hands its receiver's own type back,
  /// so none of those appear here either.
  /// </para>
  /// <para>
  /// The three layouts are here because a lambda body cannot drive inference: nothing about
  /// <c>v.Next(formulaChild)</c> tells the compiler what the flow demands, so the flow is told —
  /// by one witness word, and only that: the explicit-type-argument spelling was pruned in the
  /// projection rename, since it wrote one type argument of requirement and one of ceremony. The
  /// two repeats and the choice are here for a different and smaller reason: their result type is
  /// not their item's, and a type parameter standing for the whole projection cannot be re-pointed
  /// at a new result. That is the residual doubling, and it is a language limit.
  /// </para>
  /// </summary>
  public static partial class Projection
  {
    /// <summary>
    /// <see cref="VerticalFlow{T}(Layout{T})"/> over a space offering at least the capability
    /// <paramref name="over"/> witnesses — <c>VerticalFlow(Formulas, v =&gt; …)</c>.
    /// <para>
    /// The witness exists to carry <typeparamref name="TSpace"/> in an <em>argument</em>, where
    /// inference can read it, so the result type is still inferred from the lambda. This is the one
    /// spelling: the explicit-type-argument form (<c>VerticalFlow&lt;IFormulaSpace, T&gt;(v =&gt;
    /// …)</c>) was pruned in the projection rename, because it said the same thing with one type
    /// argument of requirement and one of ceremony.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The space this flow is declared over, inferred from <paramref name="over"/>.</typeparam>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="over">The capability's witness, published by the package that owns it.</param>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public static IProjection<TSpace, T> VerticalFlow<TSpace, T>(Demand<TSpace> over, Layout<TSpace, T> build)
      where TSpace : class, ISpace
      => Witnessed<TSpace, T>(over, new FlowProjection<T>(Orientation.Vertical, Adapt(NotNull(build, nameof(build))), Placement.Default));

    /// <inheritdoc cref="VerticalFlow{TSpace, T}(Demand{TSpace}, Layout{TSpace, T})"/>
    /// <typeparam name="TSpace">The space this flow is declared over, inferred from <paramref name="over"/>.</typeparam>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="over">The capability's witness.</param>
    /// <param name="build">The layout.</param>
    public static IProjection<TSpace, T> HorizontalFlow<TSpace, T>(Demand<TSpace> over, Layout<TSpace, T> build)
      where TSpace : class, ISpace
      => Witnessed<TSpace, T>(over, new FlowProjection<T>(Orientation.Horizontal, Adapt(NotNull(build, nameof(build))), Placement.Default));

    /// <inheritdoc cref="VerticalFlow{TSpace, T}(Demand{TSpace}, Layout{TSpace, T})"/>
    /// <typeparam name="TSpace">The space this overlay is declared over, inferred from <paramref name="over"/>.</typeparam>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="over">The capability's witness.</param>
    /// <param name="build">The layout.</param>
    public static IProjection<TSpace, T> Overlay<TSpace, T>(Demand<TSpace> over, Layout<TSpace, T> build)
      where TSpace : class, ISpace
      => Witnessed<TSpace, T>(over, new OverlayProjection<T>(Adapt(NotNull(build, nameof(build))), Placement.Default));

    private static IProjection<TSpace, T> Witnessed<TSpace, T>(Demand<TSpace> over, IProjection<TSpace, T> projection)
      where TSpace : class, ISpace
      => over is null ? throw new ArgumentNullException(nameof(over)) : projection;

    /// <inheritdoc cref="Table{T}(int, IProjection{T}, string)"/>
    /// <typeparam name="TSpace">The space the row is declared over, and therefore the table.</typeparam>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows to consume as the header, 0 or 1.</param>
    /// <param name="eachRow">The projection applied to each body row.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="eachRow"/> argument.</param>
    public static IProjection<TSpace, IReadOnlyList<T>> Table<TSpace, T>(
      int headerRows,
      IProjection<TSpace, T> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      where TSpace : class, ISpace
      => Table(headerRows, ProjectionExtensions.Plain(eachRow), declared);

    /// <inheritdoc cref="Table{T}(int, Func{CaptionMap, IProjection{T}}, string)"/>
    /// <typeparam name="TSpace">The space the bound row is declared over, and therefore the table.</typeparam>
    /// <typeparam name="T">What one record reads.</typeparam>
    /// <param name="headerRows">How many rows to read as the header; a bind needs 1.</param>
    /// <param name="eachRow">Given this file's captions, the projection that reads one record.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="eachRow"/> argument.</param>
    public static IProjection<TSpace, IReadOnlyList<T>> Table<TSpace, T>(
      int headerRows,
      Func<CaptionMap, IProjection<TSpace, T>> eachRow,
      [CallerArgumentExpression("eachRow")] string? declared = null)
      where TSpace : class, ISpace
      => eachRow is null
        ? throw new ArgumentNullException(nameof(eachRow))
        : Table(headerRows, Adapt(eachRow), declared);

    /// <inheritdoc cref="VerticalRepeat{T}"/>
    /// <typeparam name="TSpace">The space the item is declared over, and therefore the repeat.</typeparam>
    /// <typeparam name="T">What one occurrence reads.</typeparam>
    /// <param name="item">The projection to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="item"/> argument.</param>
    public static IProjection<TSpace, IReadOnlyList<T>> VerticalRepeat<TSpace, T>(
      IProjection<TSpace, T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      where TSpace : class, ISpace
      => Repeat(Orientation.Vertical, ProjectionExtensions.Plain(item), separatedBy, atLeast, declared);

    /// <inheritdoc cref="VerticalRepeat{TSpace, T}"/>
    /// <typeparam name="TSpace">The space the item is declared over, and therefore the repeat.</typeparam>
    /// <typeparam name="T">What one occurrence reads.</typeparam>
    /// <param name="item">The projection to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="item"/> argument.</param>
    public static IProjection<TSpace, IReadOnlyList<T>> HorizontalRepeat<TSpace, T>(
      IProjection<TSpace, T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      where TSpace : class, ISpace
      => Repeat(Orientation.Horizontal, ProjectionExtensions.Plain(item), separatedBy, atLeast, declared);

    /// <summary>
    /// <see cref="Choice{T}"/> over a space offering at least <typeparamref name="TSpace"/>.
    /// <para>
    /// <b>Kept doubled by judgment</b> (owner, at the projection rename), unlike the layouts: a
    /// <c>params</c> array of a self-type parameter cannot infer, so collapsing the pair would make
    /// every mixed-demand choice report <c>CS0411</c> — "the type arguments cannot be inferred" —
    /// where the doubled pair reports the named types that failed to unify. The explicit-arity
    /// spelling the suite uses survives unchanged, and that is what pays for the second definition.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The space the alternatives are declared over.</typeparam>
    /// <typeparam name="T">What every alternative reads.</typeparam>
    /// <param name="alternatives">The alternatives, tried in declaration order.</param>
    public static IProjection<TSpace, T> Choice<TSpace, T>(params IProjection<TSpace, T>[] alternatives)
      where TSpace : class, ISpace
    {
      if (alternatives is null)
        throw new ArgumentNullException(nameof(alternatives));

      var plain = new IProjection<T>[alternatives.Length];

      for (var index = 0; index < alternatives.Length; index++)
        plain[index] = alternatives[index] is null
          ? null!
          : ProjectionExtensions.Plain(alternatives[index]);

      return Choice(plain);
    }

    /// <summary>
    /// The demanding layout as a plain one: the cursor is re-typed, the state is the same object,
    /// and nothing about how the layout runs changes.
    /// </summary>
    private static Layout<T> Adapt<TSpace, T>(Layout<TSpace, T> build)
      where TSpace : class, ISpace
      => cursor => build(new LayoutCursor<TSpace>(cursor.State));

    /// <summary>
    /// The demanding bind as a plain one: the demand lives in the static type, so forgetting it
    /// here is the identity. A bind that returns null is passed through rather than reported here,
    /// because that is the plain form's failure to report and one message beats two.
    /// </summary>
    private static Func<CaptionMap, IProjection<T>> Adapt<TSpace, T>(Func<CaptionMap, IProjection<TSpace, T>> eachRow)
      where TSpace : class, ISpace
      => captions =>
      {
        var row = eachRow(captions);

        return row is null ? null! : ProjectionExtensions.Plain(row);
      };

    private static Layout<TSpace, T> NotNull<TSpace, T>(Layout<TSpace, T> build, string parameter)
      where TSpace : class, ISpace
      => build ?? throw new ArgumentNullException(parameter);
  }
}
