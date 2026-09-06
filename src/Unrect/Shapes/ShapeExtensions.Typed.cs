using System;

using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// EXPERIMENT (typed-spaces): the members that exist <em>because</em> a declaration can demand
  /// more of a space than <see cref="ISpace"/> — and only those.
  /// <para>
  /// A modifier does not appear here. Every modifier is written once, generic in the shape's own
  /// type, and hands that type straight back, so it preserves a demand without knowing there is such
  /// a thing. What lives here is the two jobs a self-typed modifier genuinely cannot do:
  /// </para>
  /// <list type="number">
  /// <item>
  /// <b>Raising a demand.</b> <c>x.On(RowWithFormula())</c> must come back demanding formulas
  /// however plain <c>x</c> was. Inference does it: the receiver contributes one bound on the
  /// demand, the matcher another, and the more demanding of the two wins — so nothing is annotated.
  /// A fixed receiver type could not; these are overloads, not duplicates, and the family is
  /// exactly the lifts that take a matcher.
  /// </item>
  /// <item>
  /// <b>Changing the result type.</b> <c>Optional</c>, <c>Select</c> and <c>Else(value)</c> hand
  /// back a shape reading something else, and C# cannot say "the receiver's type with its result
  /// swapped". These three are the residual doubling, and they are irreducible for a language
  /// reason rather than a design one.
  /// </item>
  /// </list>
  /// </summary>
  public static partial class ShapeExtensions
  {
    // --- Demand-raising lifts ---------------------------------------------------------------------
    //
    // A matcher only locates; a lift decides what absence means — and, here, carries the matcher's
    // demand onto the shape. `plainShape.On(RowWithFormula())` needs no annotation and comes back as
    // IShape<IFormulaSpace, T>. The plain twins of these take an IRowLandmark / IColumnLandmark,
    // which the demanding matchers deliberately do not implement, so the two never compete.

    /// <inheritdoc cref="On{TShape}(TShape, IRowLandmark)"/>
    /// <typeparam name="TSpace">The demand, unified from the shape's and the matcher's.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The row to sit on.</param>
    public static IShape<TSpace, T> On<TSpace, T>(this IShape<TSpace, T> shape, IRowLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => Plain(shape).On(Required(landmark).Landmark);

    /// <inheritdoc cref="On{TShape}(TShape, IColumnLandmark)"/>
    /// <typeparam name="TSpace">The demand, unified from the shape's and the matcher's.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The column to sit on.</param>
    public static IShape<TSpace, T> On<TSpace, T>(this IShape<TSpace, T> shape, IColumnLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => Plain(shape).On(Required(landmark).Landmark);

    /// <inheritdoc cref="Below{TShape}"/>
    /// <typeparam name="TSpace">The demand, unified from the shape's and the matcher's.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The row to sit below.</param>
    public static IShape<TSpace, T> Below<TSpace, T>(this IShape<TSpace, T> shape, IRowLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => Plain(shape).Below(Required(landmark).Landmark);

    /// <inheritdoc cref="RightOf{TShape}"/>
    /// <typeparam name="TSpace">The demand, unified from the shape's and the matcher's.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The column to sit right of.</param>
    public static IShape<TSpace, T> RightOf<TSpace, T>(this IShape<TSpace, T> shape, IColumnLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => Plain(shape).RightOf(Required(landmark).Landmark);

    /// <inheritdoc cref="Until{TShape}(TShape, IRowLandmark, bool)"/>
    /// <typeparam name="TSpace">The demand, unified from the shape's and the matcher's.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The row the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static IShape<TSpace, T> Until<TSpace, T>(this IShape<TSpace, T> shape, IRowLandmark<TSpace> landmark, bool orEnd = false)
      where TSpace : class, ISpace
      => Plain(shape).Until(Required(landmark).Landmark, orEnd);

    /// <inheritdoc cref="UntilColumn{TShape}"/>
    /// <typeparam name="TSpace">The demand, unified from the shape's and the matcher's.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="landmark">The column the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static IShape<TSpace, T> UntilColumn<TSpace, T>(this IShape<TSpace, T> shape, IColumnLandmark<TSpace> landmark, bool orEnd = false)
      where TSpace : class, ISpace
      => Plain(shape).UntilColumn(Required(landmark).Landmark, orEnd);

    // --- The result-changing three ----------------------------------------------------------------
    //
    // The whole of the residual doubling. Each hands back a shape reading something other than what
    // its receiver read, and a self type names one type, not a type function — so there is no way to
    // say "this same shape, reading T? instead of T". A language limit, not a design choice.

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

    /// <inheritdoc cref="Select{T, TResult}"/>
    /// <typeparam name="TSpace">What the shape demands.</typeparam>
    /// <typeparam name="T">What the shape reads.</typeparam>
    /// <typeparam name="TResult">What the selector produces.</typeparam>
    /// <param name="shape">The declaration.</param>
    /// <param name="selector">The projection.</param>
    public static IShape<TSpace, TResult> Select<TSpace, T, TResult>(this IShape<TSpace, T> shape, Func<T, TResult> selector)
      where TSpace : class, ISpace
      => Plain(shape).Select(selector);

    // --- Ascription -------------------------------------------------------------------------------

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
  }
}
