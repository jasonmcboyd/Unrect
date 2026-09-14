using System;

using Unrect.Core;

namespace Unrect.Projections
{
  /// <summary>
  /// The members that exist <em>because</em> a declaration can demand
  /// more of a space than <see cref="ICellValues"/> — and only those.
  /// <para>
  /// A modifier does not appear here. Every modifier is written once, generic in the projection's
  /// own type, and hands that type straight back, so it preserves a demand without knowing there is
  /// such a thing. Raising a demand is not a modifier either any longer: a demanding matcher raises
  /// it through the placement pipeline's entries (<c>On&lt;TSpace&gt;(RowWithFormula())</c>,
  /// <c>Until&lt;TSpace&gt;(…)</c>), which carry the demand out to the terminal with nothing
  /// annotated, now that geometry is the pipeline's alone. What is left here is the two jobs that are
  /// neither a modifier nor geometry:
  /// </para>
  /// <list type="number">
  /// <item>
  /// <b>Changing the result type.</b> <c>Optional</c>, <c>Select</c> and <c>Else(value)</c> hand
  /// back a projection reading something else, and C# cannot say "the receiver's type with its
  /// result swapped". These three are the residual doubling, and they are irreducible for a
  /// language reason rather than a design one.
  /// </item>
  /// <item>
  /// <b>Ascribing a demand a lambda hides.</b> <c>Demanding</c> states a capability the type system
  /// cannot see a projection reach for — a promise, carried by a witness rather than a type argument.
  /// </item>
  /// </list>
  /// </summary>
  public static partial class ProjectionExtensions
  {
    // Demand-raising geometry lifts (On/Below/RightOf/Until/UntilColumn over a demanding matcher)
    // retired with the rest of the postfix geometry: geometry is the pipeline's, and a demanding
    // matcher opens a demanding pipeline through the entries (On<TSpace>(matcher), Until<TSpace>(...))
    // with nothing annotated. What is left here is the two jobs a self-typed modifier cannot do that
    // are NOT geometry — changing the result type, and ascribing a demand a lambda hides.

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
      where TSpace : class, ICellValues
      => Plain(projection).Else(fallbackValue);

    /// <inheritdoc cref="Optional{T}"/>
    /// <typeparam name="TSpace">What the projection demands.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    public static IProjection<TSpace, T?> Optional<TSpace, T>(this IProjection<TSpace, T> projection)
      where TSpace : class, ICellValues
      => Plain(projection).Optional();

    /// <inheritdoc cref="Select{T, TResult}"/>
    /// <typeparam name="TSpace">What the projection demands.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <typeparam name="TResult">What the selector produces.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="selector">The transformation applied to what it reads.</param>
    public static IProjection<TSpace, TResult> Select<TSpace, T, TResult>(this IProjection<TSpace, T> projection, Func<T, TResult> selector)
      where TSpace : class, ICellValues
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
    /// <para>
    /// The demand is carried by a <em>witness</em> — <c>projection.Demanding(Formulas)</c> — rather
    /// than by type arguments, so the result type is still inferred and nothing is written twice.
    /// There is no type-argument spelling: it said the same thing with one argument of requirement
    /// and one of pure ceremony, and any capability can witness itself through
    /// <see cref="Demand{TSpace}.Instance"/> without its package publishing a name for it.
    /// </para>
    /// </summary>
    /// <typeparam name="TSpace">The demand being stated, inferred from <paramref name="demand"/>.</typeparam>
    /// <typeparam name="T">What the projection reads.</typeparam>
    /// <param name="projection">The declaration.</param>
    /// <param name="demand">The capability's witness, published by the package that owns it.</param>
    public static IProjection<TSpace, T> Demanding<TSpace, T>(this IProjection<T> projection, Demand<TSpace> demand)
      where TSpace : class, ICellValues
      => demand is null
        ? throw new ArgumentNullException(nameof(demand))
        : projection ?? throw new ArgumentNullException(nameof(projection));
  }
}
