using System;
using System.Runtime.CompilerServices;

using Unrect.Core;
using Unrect.Strategies;

namespace Unrect.Shapes
{
  /// <summary>
  /// EXPERIMENT (typed-spaces): the demanding twins of every modifier, and of the three application
  /// methods.
  /// <para>
  /// A modifier has to preserve the demand it is applied to — <c>x.Named("n")</c> on a
  /// formula-reading shape is still formula-reading — and a return type cannot mention a
  /// contravariant parameter, so the plain overloads cannot simply be reused. Every one of them
  /// therefore appears twice: once on <c>IShape&lt;T&gt;</c> (unchanged, and what a plain
  /// declaration binds to, being the better overload) and once here. That doubling is the API tax
  /// this experiment charges, and it is charged whether or not any consumer ever names a capability.
  /// </para>
  /// </summary>
  public static partial class ShapeExtensions
  {
    /// <summary>
    /// The shape as the engine sees it. Sound because every <see cref="IShape{TSpace, TResult}"/>
    /// this library produces is a <see cref="ShapeBase{TResult}"/>, which implements
    /// <see cref="IShape{TResult}"/>; the demand lives only in the static type, so forgetting it
    /// here is the identity.
    /// </summary>
    internal static IShape<T> Plain<TSpace, T>(IShape<TSpace, T> shape)
      where TSpace : class, ISpace
      => shape as IShape<T>
        ?? throw new ArgumentException(
          $"{shape?.GetType().Name ?? "null"} is not a shape this library built; the typed layer can only carry shapes that implement IShape<T>.",
          nameof(shape));

    // --- Application ----------------------------------------------------------------------------

    /// <summary>
    /// <see cref="Map{TResult}"/> for a shape that demands more than a bare
    /// <see cref="ISpace"/>. The space must satisfy the demand, which is the whole point: a
    /// declaration that reads formulas will not compile against a grid that has none.
    /// </summary>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="TResult">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="space">The space to decompose.</param>
    public static TResult Map<TSpace, TResult>(this IShape<TSpace, TResult> shape, TSpace space)
      where TSpace : class, ISpace
      => Plain(shape).Map(space);

    /// <inheritdoc cref="Apply{TResult}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="TResult">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="space">The space to decompose.</param>
    public static AppliedResult<TResult> Apply<TSpace, TResult>(this IShape<TSpace, TResult> shape, TSpace space)
      where TSpace : class, ISpace
      => Plain(shape).Apply(space);

    /// <inheritdoc cref="MapWithDiagnostics{TResult}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="TResult">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="space">The space to decompose.</param>
    public static MapResult<TResult> MapWithDiagnostics<TSpace, TResult>(this IShape<TSpace, TResult> shape, TSpace space)
      where TSpace : class, ISpace
      => Plain(shape).MapWithDiagnostics(space);

    // --- Modifiers ------------------------------------------------------------------------------

    /// <inheritdoc cref="Named{T}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="name">What failures and diagnostics should call it.</param>
    public static IShape<TSpace, T> Named<TSpace, T>(this IShape<TSpace, T> shape, string name)
      where TSpace : class, ISpace
      => Plain(shape).Named(name);

    /// <inheritdoc cref="On{T}(IShape{T}, IRowLandmark)"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The row to sit on.</param>
    public static IShape<TSpace, T> On<TSpace, T>(this IShape<TSpace, T> shape, IRowLandmark landmark)
      where TSpace : class, ISpace
      => Plain(shape).On(landmark);

    /// <inheritdoc cref="On{T}(IShape{T}, IColumnLandmark)"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The column to sit on.</param>
    public static IShape<TSpace, T> On<TSpace, T>(this IShape<TSpace, T> shape, IColumnLandmark landmark)
      where TSpace : class, ISpace
      => Plain(shape).On(landmark);

    /// <inheritdoc cref="Below{T}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The row to sit below.</param>
    public static IShape<TSpace, T> Below<TSpace, T>(this IShape<TSpace, T> shape, IRowLandmark landmark)
      where TSpace : class, ISpace
      => Plain(shape).Below(landmark);

    /// <inheritdoc cref="RightOf{T}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The column to sit right of.</param>
    public static IShape<TSpace, T> RightOf<TSpace, T>(this IShape<TSpace, T> shape, IColumnLandmark landmark)
      where TSpace : class, ISpace
      => Plain(shape).RightOf(landmark);

    /// <inheritdoc cref="OffsetBy{T}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="offset">Where the shape starts.</param>
    public static IShape<TSpace, T> OffsetBy<TSpace, T>(this IShape<TSpace, T> shape, IOffsetStrategy offset)
      where TSpace : class, ISpace
      => Plain(shape).OffsetBy(offset);

    /// <inheritdoc cref="AfterBlankRows{T}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    public static IShape<TSpace, T> AfterBlankRows<TSpace, T>(this IShape<TSpace, T> shape)
      where TSpace : class, ISpace
      => Plain(shape).AfterBlankRows();

    /// <inheritdoc cref="AfterBlankColumns{T}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    public static IShape<TSpace, T> AfterBlankColumns<TSpace, T>(this IShape<TSpace, T> shape)
      where TSpace : class, ISpace
      => Plain(shape).AfterBlankColumns();

    /// <inheritdoc cref="Down{T}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="rows">How far down.</param>
    public static IShape<TSpace, T> Down<TSpace, T>(this IShape<TSpace, T> shape, int rows)
      where TSpace : class, ISpace
      => Plain(shape).Down(rows);

    /// <inheritdoc cref="Right{T}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="columns">How far right.</param>
    public static IShape<TSpace, T> Right<TSpace, T>(this IShape<TSpace, T> shape, int columns)
      where TSpace : class, ISpace
      => Plain(shape).Right(columns);

    /// <inheritdoc cref="Sized{T}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="area">The extent.</param>
    public static IShape<TSpace, T> Sized<TSpace, T>(this IShape<TSpace, T> shape, IAreaStrategy area)
      where TSpace : class, ISpace
      => Plain(shape).Sized(area);

    /// <inheritdoc cref="Else{T}(IShape{T}, IShape{T}, string)"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="fallback">What to read instead.</param>
    /// <param name="declared">Supplied by the compiler as the text of the <paramref name="fallback"/> argument.</param>
    public static IShape<TSpace, T> Else<TSpace, T>(
      this IShape<TSpace, T> shape,
      IShape<TSpace, T> fallback,
      [CallerArgumentExpression("fallback")] string? declared = null)
      where TSpace : class, ISpace
      => Plain(shape).Else(
        fallback is null ? throw new ArgumentNullException(nameof(fallback)) : Plain(fallback),
        declared);

    /// <inheritdoc cref="Else{T}(IShape{T}, T)"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="fallbackValue">What to yield instead.</param>
    public static IShape<TSpace, T> Else<TSpace, T>(this IShape<TSpace, T> shape, T fallbackValue)
      where TSpace : class, ISpace
      => Plain(shape).Else(fallbackValue);

    /// <inheritdoc cref="Optional{T}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    public static IShape<TSpace, T?> Optional<TSpace, T>(this IShape<TSpace, T> shape)
      where TSpace : class, ISpace
      => Plain(shape).Optional();

    /// <inheritdoc cref="Padded{T}(IShape{T}, int)"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="all">The inset on every side.</param>
    public static IShape<TSpace, T> Padded<TSpace, T>(this IShape<TSpace, T> shape, int all)
      where TSpace : class, ISpace
      => Plain(shape).Padded(all);

    /// <inheritdoc cref="Padded{T}(IShape{T}, int, int)"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="horizontal">The inset left and right.</param>
    /// <param name="vertical">The inset top and bottom.</param>
    public static IShape<TSpace, T> Padded<TSpace, T>(this IShape<TSpace, T> shape, int horizontal, int vertical)
      where TSpace : class, ISpace
      => Plain(shape).Padded(horizontal, vertical);

    /// <inheritdoc cref="Padded{T}(IShape{T}, int, int, int, int)"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="left">The inset on the left.</param>
    /// <param name="top">The inset on the top.</param>
    /// <param name="right">The inset on the right.</param>
    /// <param name="bottom">The inset on the bottom.</param>
    public static IShape<TSpace, T> Padded<TSpace, T>(this IShape<TSpace, T> shape, int left, int top, int right, int bottom)
      where TSpace : class, ISpace
      => Plain(shape).Padded(left, top, right, bottom);

    /// <inheritdoc cref="Until{T}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The row the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static IShape<TSpace, T> Until<TSpace, T>(this IShape<TSpace, T> shape, IRowLandmark landmark, bool orEnd = false)
      where TSpace : class, ISpace
      => Plain(shape).Until(landmark, orEnd);

    /// <inheritdoc cref="UntilColumn{T}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The column the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static IShape<TSpace, T> UntilColumn<TSpace, T>(this IShape<TSpace, T> shape, IColumnLandmark landmark, bool orEnd = false)
      where TSpace : class, ISpace
      => Plain(shape).UntilColumn(landmark, orEnd);

    /// <inheritdoc cref="Under{T}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="captions">The rows that announce it.</param>
    public static IShape<TSpace, T> Under<TSpace, T>(this IShape<TSpace, T> shape, params IShape<TSpace, string>[] captions)
      where TSpace : class, ISpace
    {
      if (captions is null)
        throw new ArgumentNullException(nameof(captions));

      var plain = new IShape<string>[captions.Length];

      for (var index = 0; index < captions.Length; index++)
        plain[index] = captions[index] is null ? null! : Plain(captions[index]);

      return Plain(shape).Under(plain);
    }

    // --- Demanding matchers ---------------------------------------------------------------------
    //
    // The lift is what carries a matcher's demand onto the shape, and it does so by inference: the
    // receiver contributes an upper bound on TSpace, the matcher contributes another, and the more
    // demanding of the two wins. So `plainShape.On(RowWithFormula())` needs no annotation and comes
    // back as IShape<IFormulaSpace, T>. Every lift needs one of these, which is the second half of
    // the API tax.

    /// <inheritdoc cref="On{T}(IShape{T}, IRowLandmark)"/>
    /// <typeparam name="TSpace">The demand, unified from the shape's and the matcher's.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The row to sit on.</param>
    public static IShape<TSpace, T> On<TSpace, T>(this IShape<TSpace, T> shape, IRowLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => Plain(shape).On(Required(landmark).Landmark);

    /// <inheritdoc cref="On{T}(IShape{T}, IColumnLandmark)"/>
    /// <typeparam name="TSpace">The demand, unified from the shape's and the matcher's.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The column to sit on.</param>
    public static IShape<TSpace, T> On<TSpace, T>(this IShape<TSpace, T> shape, IColumnLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => Plain(shape).On(Required(landmark).Landmark);

    /// <inheritdoc cref="Below{T}"/>
    /// <typeparam name="TSpace">The demand, unified from the shape's and the matcher's.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The row to sit below.</param>
    public static IShape<TSpace, T> Below<TSpace, T>(this IShape<TSpace, T> shape, IRowLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => Plain(shape).Below(Required(landmark).Landmark);

    /// <inheritdoc cref="RightOf{T}"/>
    /// <typeparam name="TSpace">The demand, unified from the shape's and the matcher's.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The column to sit right of.</param>
    public static IShape<TSpace, T> RightOf<TSpace, T>(this IShape<TSpace, T> shape, IColumnLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => Plain(shape).RightOf(Required(landmark).Landmark);

    /// <inheritdoc cref="Until{T}"/>
    /// <typeparam name="TSpace">The demand, unified from the shape's and the matcher's.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The row the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static IShape<TSpace, T> Until<TSpace, T>(this IShape<TSpace, T> shape, IRowLandmark<TSpace> landmark, bool orEnd = false)
      where TSpace : class, ISpace
      => Plain(shape).Until(Required(landmark).Landmark, orEnd);

    /// <inheritdoc cref="UntilColumn{T}"/>
    /// <typeparam name="TSpace">The demand, unified from the shape's and the matcher's.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The column the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static IShape<TSpace, T> UntilColumn<TSpace, T>(this IShape<TSpace, T> shape, IColumnLandmark<TSpace> landmark, bool orEnd = false)
      where TSpace : class, ISpace
      => Plain(shape).UntilColumn(Required(landmark).Landmark, orEnd);

    // --- Ascription -----------------------------------------------------------------------------

    /// <summary>
    /// EXPERIMENT (typed-spaces): states that <paramref name="shape"/> needs a
    /// <typeparamref name="TSpace"/>, where nothing about the declaration says so — a projection
    /// lambda that reaches through to a capability, which the type system cannot see into.
    /// <para>
    /// <b>It is a promise, not a proof.</b> Nothing checks that the shape really needs the
    /// capability, and nothing stops the opposite mistake — a projection that probes formulas and is
    /// never ascribed compiles fine and faults at run time. It belongs to a backend package
    /// declaring its own vocabulary once (<c>Formula()</c> is written with it), not to a
    /// declaration's author.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The demand being stated.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    public static IShape<TSpace, T> Demanding<TSpace, T>(this IShape<T> shape)
      where TSpace : class, ISpace
      => shape ?? throw new ArgumentNullException(nameof(shape));

    /// <summary>
    /// <see cref="Demanding{TSpace, T}(IShape{T})"/> stated with a witness rather than with type
    /// arguments — <c>shape.Demanding(Formulas)</c> — so the result type is still inferred. Same
    /// promise, same lack of proof; only the spelling is better.
    /// </summary>
    /// <typeparam name="TSpace">The demand being stated, inferred from <paramref name="demand"/>.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="demand">The capability's witness, published by the package that owns it.</param>
    public static IShape<TSpace, T> Demanding<TSpace, T>(this IShape<T> shape, Demand<TSpace> demand)
      where TSpace : class, ISpace
      => demand is null
        ? throw new ArgumentNullException(nameof(demand))
        : shape ?? throw new ArgumentNullException(nameof(shape));

    private static TLandmark Required<TLandmark>(TLandmark landmark)
      where TLandmark : class
      => landmark ?? throw new ArgumentNullException(nameof(landmark));

    /// <inheritdoc cref="Select{T, TResult}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <typeparam name="TResult">What the selector produces.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="selector">The projection.</param>
    public static IShape<TSpace, TResult> Select<TSpace, T, TResult>(this IShape<TSpace, T> shape, Func<T, TResult> selector)
      where TSpace : class, ISpace
      => Plain(shape).Select(selector);
  }
}
