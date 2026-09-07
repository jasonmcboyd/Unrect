using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The members that exist <em>because</em> a declaration can demand
  /// more of a space than <see cref="ISpace"/> — and only those.
  /// <para>
  /// A modifier does not appear here. Every modifier is written once, generic in the projection's
  /// own type, and hands that type straight back, so it preserves a demand without knowing there is
  /// such a thing. What lives here is the two jobs a self-typed modifier genuinely cannot do:
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
  /// back a projection reading something else, and C# cannot say "the receiver's type with its
  /// result swapped". These three are the residual doubling, and they are irreducible for a
  /// language reason rather than a design one.
  /// </item>
  /// </list>
  /// </summary>
  public static partial class ProjectionExtensions
  {
    // --- Demand-raising lifts ---------------------------------------------------------------------
    //
    // A matcher only locates; a lift decides what absence means — and, here, carries the matcher's
    // demand onto the projection. `plainProjection.On(RowWithFormula())` needs no annotation and comes back as
    // IProjection<IFormulaSpace, T>. The plain twins of these take an IRowLandmark / IColumnLandmark,
    // which the demanding matchers deliberately do not implement, so the two never compete.

    /// <inheritdoc cref="On{TProjection}(TProjection, IRowLandmark)"/>
    /// <typeparam name="TSpace">The demand, unified from the projection's and the matcher's.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="landmark">The row to sit on.</param>
    public static IProjection<TSpace, T> On<TSpace, T>(this IProjection<TSpace, T> projection, IRowLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => Plain(projection).On(Required(landmark).Landmark);

    /// <inheritdoc cref="On{TProjection}(TProjection, IColumnLandmark)"/>
    /// <typeparam name="TSpace">The demand, unified from the projection's and the matcher's.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="landmark">The column to sit on.</param>
    public static IProjection<TSpace, T> On<TSpace, T>(this IProjection<TSpace, T> projection, IColumnLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => Plain(projection).On(Required(landmark).Landmark);

    /// <inheritdoc cref="Below{TProjection}"/>
    /// <typeparam name="TSpace">The demand, unified from the projection's and the matcher's.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="landmark">The row to sit below.</param>
    public static IProjection<TSpace, T> Below<TSpace, T>(this IProjection<TSpace, T> projection, IRowLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => Plain(projection).Below(Required(landmark).Landmark);

    /// <inheritdoc cref="RightOf{TProjection}"/>
    /// <typeparam name="TSpace">The demand, unified from the projection's and the matcher's.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="landmark">The column to sit right of.</param>
    public static IProjection<TSpace, T> RightOf<TSpace, T>(this IProjection<TSpace, T> projection, IColumnLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => Plain(projection).RightOf(Required(landmark).Landmark);

    /// <inheritdoc cref="Until{TProjection}(TProjection, IRowLandmark, bool)"/>
    /// <typeparam name="TSpace">The demand, unified from the projection's and the matcher's.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="landmark">The row the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static IProjection<TSpace, T> Until<TSpace, T>(this IProjection<TSpace, T> projection, IRowLandmark<TSpace> landmark, bool orEnd = false)
      where TSpace : class, ISpace
      => Plain(projection).Until(Required(landmark).Landmark, orEnd);

    /// <inheritdoc cref="UntilColumn{TProjection}"/>
    /// <typeparam name="TSpace">The demand, unified from the projection's and the matcher's.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="landmark">The column the extent stops before.</param>
    /// <param name="orEnd">Whether running to the end of the space is acceptable.</param>
    public static IProjection<TSpace, T> UntilColumn<TSpace, T>(this IProjection<TSpace, T> projection, IColumnLandmark<TSpace> landmark, bool orEnd = false)
      where TSpace : class, ISpace
      => Plain(projection).UntilColumn(Required(landmark).Landmark, orEnd);

    // --- The result-changing three ----------------------------------------------------------------
    //
    // The whole of the residual doubling. Each hands back a projection reading something other than what
    // its receiver read, and a self type names one type, not a type function — so there is no way to
    // say "this same projection, reading T? instead of T". A language limit, not a design choice.

    /// <inheritdoc cref="Else{T}(IProjection{T}, T)"/>
    /// <typeparam name="TSpace">What the projection demands.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="fallbackValue">What to yield instead.</param>
    public static IProjection<TSpace, T> Else<TSpace, T>(this IProjection<TSpace, T> projection, T fallbackValue)
      where TSpace : class, ISpace
      => Plain(projection).Else(fallbackValue);

    /// <inheritdoc cref="Optional{T}"/>
    /// <typeparam name="TSpace">What the projection demands.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    public static IProjection<TSpace, T?> Optional<TSpace, T>(this IProjection<TSpace, T> projection)
      where TSpace : class, ISpace
      => Plain(projection).Optional();

    /// <inheritdoc cref="Select{T, TResult}"/>
    /// <typeparam name="TSpace">What the projection demands.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <typeparam name="TResult">What the selector produces.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="selector">The transformation applied to what it reads.</param>
    public static IProjection<TSpace, TResult> Select<TSpace, T, TResult>(this IProjection<TSpace, T> projection, Func<T, TResult> selector)
      where TSpace : class, ISpace
      => Plain(projection).Select(selector);

    // --- Ascription -------------------------------------------------------------------------------

    /// <summary>
    /// States that <paramref name="projection"/> needs a
    /// <typeparamref name="TSpace"/>, where nothing about the declaration says so — a projection
    /// lambda that reaches through to a capability, which the type system cannot see into.
    /// <para>
    /// <b>It is a promise, not a proof.</b> Nothing checks that the projection really needs the
    /// capability, and nothing stops the opposite mistake — a projection that probes formulas and
    /// is never ascribed compiles fine and faults at run time. It belongs to a backend package
    /// declaring its own vocabulary once (<c>Formula()</c> is written with it), not to a
    /// declaration's author.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The demand being stated.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    public static IProjection<TSpace, T> Demanding<TSpace, T>(this IProjection<T> projection)
      where TSpace : class, ISpace
      => projection ?? throw new ArgumentNullException(nameof(projection));

    /// <summary>
    /// <see cref="Demanding{TSpace, T}(IProjection{T})"/> stated with a witness rather than with
    /// type arguments — <c>projection.Demanding(Formulas)</c> — so the result type is still
    /// inferred. Same promise, same lack of proof; only the spelling is better.
    /// </summary>
    /// <typeparam name="TSpace">The demand being stated, inferred from <paramref name="demand"/>.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="demand">The capability's witness, published by the package that owns it.</param>
    public static IProjection<TSpace, T> Demanding<TSpace, T>(this IProjection<T> projection, Demand<TSpace> demand)
      where TSpace : class, ISpace
      => demand is null
        ? throw new ArgumentNullException(nameof(demand))
        : projection ?? throw new ArgumentNullException(nameof(projection));

    private static TLandmark Required<TLandmark>(TLandmark landmark)
      where TLandmark : class
      => landmark ?? throw new ArgumentNullException(nameof(landmark));
  }
}
