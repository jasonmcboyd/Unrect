using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Shapes
{
  /// <summary>
  /// EXPERIMENT (typed-spaces): the demanding twins of the vocabulary's composing factories.
  /// <para>
  /// Only the factories that <em>take</em> shapes need one. A leaf demands nothing, so
  /// <c>Text()</c>, <c>Range</c>, <c>TableRows&lt;T&gt;()</c> and the rest are unchanged and run
  /// everywhere by variance; a composite's demand is the union of its children's, and C# will not
  /// infer that union, so each of these has to be told.
  /// </para>
  /// </summary>
  public static partial class Shape
  {
    /// <summary>
    /// <see cref="VerticalFlow{T}(Layout{T})"/> over a space offering at least
    /// <typeparamref name="TSpace"/>. Both type arguments must be written: a lambda body cannot
    /// drive inference, so nothing about <c>v.Next(formulaChild)</c> can tell the compiler what the
    /// flow demands.
    /// </summary>
    /// <typeparam name="TSpace">The space this flow is declared over.</typeparam>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public static IShape<TSpace, T> VerticalFlow<TSpace, T>(Layout<TSpace, T> build)
      where TSpace : class, ISpace
      => new FlowShape<T>(Orientation.Vertical, Adapt(NotNull(build, nameof(build))), Placement.Default);

    /// <inheritdoc cref="VerticalFlow{TSpace, T}(Layout{TSpace, T})"/>
    /// <typeparam name="TSpace">The space this flow is declared over.</typeparam>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public static IShape<TSpace, T> HorizontalFlow<TSpace, T>(Layout<TSpace, T> build)
      where TSpace : class, ISpace
      => new FlowShape<T>(Orientation.Horizontal, Adapt(NotNull(build, nameof(build))), Placement.Default);

    /// <inheritdoc cref="VerticalFlow{TSpace, T}(Layout{TSpace, T})"/>
    /// <typeparam name="TSpace">The space this overlay is declared over.</typeparam>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public static IShape<TSpace, T> Overlay<TSpace, T>(Layout<TSpace, T> build)
      where TSpace : class, ISpace
      => new OverlayShape<T>(Adapt(NotNull(build, nameof(build))), Placement.Default);

    /// <summary>
    /// <see cref="VerticalFlow{T}(Layout{T})"/> over a space offering at least the capability
    /// <paramref name="over"/> witnesses — <c>VerticalFlow(Formulas, v =&gt; …)</c>.
    /// <para>
    /// The witness exists to carry <typeparamref name="TSpace"/> in an <em>argument</em>, where
    /// inference can read it, so the result type is still inferred from the lambda. That is the
    /// difference between stating one requirement and writing two type arguments of which one is
    /// ceremony.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The space this flow is declared over, inferred from <paramref name="over"/>.</typeparam>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="over">The capability's witness, published by the package that owns it.</param>
    /// <param name="build">The layout, declaring its children by calling <c>Next</c>.</param>
    public static IShape<TSpace, T> VerticalFlow<TSpace, T>(Demand<TSpace> over, Layout<TSpace, T> build)
      where TSpace : class, ISpace
      => Witnessed(over, VerticalFlow(build));

    /// <inheritdoc cref="VerticalFlow{TSpace, T}(Demand{TSpace}, Layout{TSpace, T})"/>
    /// <typeparam name="TSpace">The space this flow is declared over, inferred from <paramref name="over"/>.</typeparam>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="over">The capability's witness.</param>
    /// <param name="build">The layout.</param>
    public static IShape<TSpace, T> HorizontalFlow<TSpace, T>(Demand<TSpace> over, Layout<TSpace, T> build)
      where TSpace : class, ISpace
      => Witnessed(over, HorizontalFlow(build));

    /// <inheritdoc cref="VerticalFlow{TSpace, T}(Demand{TSpace}, Layout{TSpace, T})"/>
    /// <typeparam name="TSpace">The space this overlay is declared over, inferred from <paramref name="over"/>.</typeparam>
    /// <typeparam name="T">What the layout builds.</typeparam>
    /// <param name="over">The capability's witness.</param>
    /// <param name="build">The layout.</param>
    public static IShape<TSpace, T> Overlay<TSpace, T>(Demand<TSpace> over, Layout<TSpace, T> build)
      where TSpace : class, ISpace
      => Witnessed(over, Overlay(build));

    private static IShape<TSpace, T> Witnessed<TSpace, T>(Demand<TSpace> over, IShape<TSpace, T> shape)
      where TSpace : class, ISpace
      => over is null ? throw new ArgumentNullException(nameof(over)) : shape;

    /// <inheritdoc cref="VerticalRepeat{T}"/>
    /// <typeparam name="TSpace">The space the item is declared over, and therefore the repeat.</typeparam>
    /// <typeparam name="T">What one occurrence reads.</typeparam>
    /// <param name="item">The shape to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="item"/> argument.</param>
    public static IShape<TSpace, IReadOnlyList<T>> VerticalRepeat<TSpace, T>(
      IShape<TSpace, T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      where TSpace : class, ISpace
      => Repeat(Orientation.Vertical, ShapeExtensions.Plain(item), separatedBy, atLeast, declared);

    /// <inheritdoc cref="VerticalRepeat{TSpace, T}"/>
    /// <typeparam name="TSpace">The space the item is declared over, and therefore the repeat.</typeparam>
    /// <typeparam name="T">What one occurrence reads.</typeparam>
    /// <param name="item">The shape to apply repeatedly.</param>
    /// <param name="separatedBy">The offset between occurrences; never applied before the first.</param>
    /// <param name="atLeast">How many occurrences make a well-formed section.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="item"/> argument.</param>
    public static IShape<TSpace, IReadOnlyList<T>> HorizontalRepeat<TSpace, T>(
      IShape<TSpace, T> item,
      IOffsetStrategy? separatedBy = null,
      int atLeast = 0,
      [CallerArgumentExpression("item")] string? declared = null)
      where TSpace : class, ISpace
      => Repeat(Orientation.Horizontal, ShapeExtensions.Plain(item), separatedBy, atLeast, declared);

    /// <inheritdoc cref="Choice{T}"/>
    /// <typeparam name="TSpace">The space the alternatives are declared over.</typeparam>
    /// <typeparam name="T">What every alternative reads.</typeparam>
    /// <param name="alternatives">The alternatives, tried in declaration order.</param>
    public static IShape<TSpace, T> Choice<TSpace, T>(params IShape<TSpace, T>[] alternatives)
      where TSpace : class, ISpace
    {
      if (alternatives is null)
        throw new ArgumentNullException(nameof(alternatives));

      var plain = new IShape<T>[alternatives.Length];

      for (var index = 0; index < alternatives.Length; index++)
        plain[index] = alternatives[index] is null
          ? null!
          : ShapeExtensions.Plain(alternatives[index]);

      return Choice(plain);
    }

    /// <summary>
    /// The demanding layout as a plain one: the cursor is re-typed, the state is the same object,
    /// and nothing about how the layout runs changes.
    /// </summary>
    private static Layout<T> Adapt<TSpace, T>(Layout<TSpace, T> build)
      where TSpace : class, ISpace
      => cursor => build(new LayoutCursor<TSpace>(cursor.State));

    private static Layout<TSpace, T> NotNull<TSpace, T>(Layout<TSpace, T> build, string parameter)
      where TSpace : class, ISpace
      => build ?? throw new ArgumentNullException(parameter);
  }
}
