using System;
using Unrect.Core;
using static Unrect.Strategies.SizeStrategies;

namespace Unrect.Strategies
{
  /// <summary>Factories for <see cref="IOffsetStrategy{TSpace}"/> — how a projection's origin is found within the space it is handed.</summary>
  public static class OffsetStrategies
  {
    // "No movement" is one value rather than one per call: a placement recognises it to decide
    // whether a later movement composes onto a declared offset or replaces an undeclared one, so
    // two spellings of nothing must not be two different offsets. The strategy is immutable, so
    // there is nothing to share but the answer.
    private static class NoMovement<TSpace>
      where TSpace : class, ISpace
    {
      internal static readonly IOffsetStrategy<TSpace> Value = MinSize<TSpace>().ToOffsetStrategy();
    }

    /// <summary>No movement — the origin the projection was handed.</summary>
    public static IOffsetStrategy<TSpace> MinOffset<TSpace>()
      where TSpace : class, ISpace
      => NoMovement<TSpace>.Value;

    /// <summary>A fixed displacement of <paramref name="width"/> columns and <paramref name="height"/> rows.</summary>
    public static IOffsetStrategy<TSpace> ExplicitOffset<TSpace>(int width, int height)
      where TSpace : class, ISpace
      => ExplicitSize<TSpace>(width, height).ToOffsetStrategy();

    /// <summary>Whatever <paramref name="selector"/> computes from the available space.</summary>
    public static IOffsetStrategy<TSpace> SelectOffset<TSpace>(Func<Plane<TSpace>, Size> selector)
      where TSpace : class, ISpace
      => SelectSize(selector).ToOffsetStrategy();

    /// <summary>Past the leading rows in which every cell satisfies <paramref name="predicate"/>.</summary>
    public static IOffsetStrategy<TSpace> SkipRowsWhileAll<TSpace>(Func<Point<TSpace>, bool> predicate)
      where TSpace : class, ISpace
      => new RowOffsetSizeStrategy<TSpace>(RowStrategies.TakeRowsWhileAll(predicate)).ToOffsetStrategy();

    /// <summary>Past the leading rows in which at least one cell satisfies <paramref name="predicate"/>.</summary>
    public static IOffsetStrategy<TSpace> SkipRowsWhileAny<TSpace>(Func<Point<TSpace>, bool> predicate)
      where TSpace : class, ISpace
      => new RowOffsetSizeStrategy<TSpace>(RowStrategies.TakeRowsWhileAny(predicate)).ToOffsetStrategy();

    /// <summary>Past the leading entirely-blank rows — the zero-argument form of <see cref="SkipRowsWhileAll"/>.</summary>
    public static IOffsetStrategy<TSpace> SkipBlankRows<TSpace>()
      where TSpace : class, ISpace
      => SkipRowsWhileAll<TSpace>(v => v.IsBlank());

    /// <summary>Past the leading columns in which every cell satisfies <paramref name="predicate"/>; the column twin of <see cref="SkipRowsWhileAll"/>.</summary>
    public static IOffsetStrategy<TSpace> SkipColumnsWhileAll<TSpace>(Func<Point<TSpace>, bool> predicate)
      where TSpace : class, ISpace
      => new ColumnOffsetSizeStrategy<TSpace>(ColumnStrategies.TakeColumnsWhileAll(predicate)).ToOffsetStrategy();

    /// <summary>Past the leading columns in which at least one cell satisfies <paramref name="predicate"/>; the column twin of <see cref="SkipRowsWhileAny"/>.</summary>
    public static IOffsetStrategy<TSpace> SkipColumnsWhileAny<TSpace>(Func<Point<TSpace>, bool> predicate)
      where TSpace : class, ISpace
      => new ColumnOffsetSizeStrategy<TSpace>(ColumnStrategies.TakeColumnsWhileAny(predicate)).ToOffsetStrategy();

    /// <summary>Past the leading entirely-blank columns — the zero-argument form of <see cref="SkipColumnsWhileAll"/>.</summary>
    public static IOffsetStrategy<TSpace> SkipBlankColumns<TSpace>()
      where TSpace : class, ISpace
      => SkipColumnsWhileAll<TSpace>(v => v.IsBlank());

    /// <summary>
    /// Onto the first non-blank cell scanning row-major from the top-left — down to the first row
    /// that carries content, then across it to its first non-blank cell. The lazy corner heuristic:
    /// a row at a time, reading across only as far as it takes to find content, never down a column.
    /// <para>
    /// It finds the first content row's first non-blank cell, the region's true corner only when
    /// the region is top-left-aligned; a ragged region whose lower rows reach further left is the
    /// accepted miss. An entirely
    /// blank space resolves to its end, an empty subspace, exactly as <see cref="SkipBlankRows"/>
    /// does.
    /// </para>
    /// </summary>
    public static IOffsetStrategy<TSpace> SkipToFirstNonBlankCell<TSpace>()
      where TSpace : class, ISpace
      => new SkipToFirstNonBlankCellStrategy<TSpace>();

    /// <summary>
    /// Sequences <paramref name="offsets"/>: each is resolved against the space the one before it
    /// left, and the displacements sum — so <c>Then(SkipBlankRows(), ExplicitOffset(0, 1))</c>
    /// reads as "past the blank band, then one more row".
    /// </summary>
    public static IOffsetStrategy<TSpace> Then<TSpace>(params IOffsetStrategy<TSpace>[] offsets)
      where TSpace : class, ISpace
      => new CompositeOffsetStrategy<TSpace>(offsets);

    // --- The two lifts: where a matcher puts a projection ---------------------------------------------
    //
    // A skip-while stops at the first row that fails its predicate, so anything inserted above the
    // thing you are looking for moves it. A matcher scans to the first row that matches instead,
    // which is what survives that. It locates content and reports absence without deciding what
    // absence means; these two lifts decide it for a placement — the anchor was required. That
    // answer arrives as an OutOfBoundsException from an offset strategy, which is how a strict
    // projection reports a missing anchor and how a repeat learns there are no more sections.
    //
    // These are the calculus's spelling and stay mirror-symmetric with the rest of it. A projection
    // declaration says the same two things as .On (both axes) and .Below/.RightOf (one each),
    // where the word carries the relation and a direction appears only where the concept has one.

    /// <summary>
    /// Onto the line <paramref name="landmark"/> matches — a row or a column, whichever the landmark
    /// looks for. The region starts AT that line, so the projection owns it — a caption its section
    /// should describe, or a label row it reads.
    /// </summary>
    public static IOffsetStrategy<TSpace> To<TSpace>(ILineLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => new LandmarkOffsetStrategy<TSpace>(NotNull(landmark, nameof(landmark)), past: false);

    /// <summary>
    /// Onto the line after the one <paramref name="landmark"/> matches, for a projection that
    /// starts below a row, or right of a column, it does not want to own. This is the whole of the
    /// old anchor-then-skip idiom, without the hard-coded 1 that stood in for the matched line's
    /// own size.
    /// </summary>
    public static IOffsetStrategy<TSpace> Past<TSpace>(ILineLandmark<TSpace> landmark)
      where TSpace : class, ISpace
      => new LandmarkOffsetStrategy<TSpace>(NotNull(landmark, nameof(landmark)), past: true);

    // --- Anchoring to the far edge --------------------------------------------------------------
    //
    // Both measure back from the end of the available space, so they are normally spelled with
    // .OffsetBy(...), which replaces: composing a movement before a from-end anchor rarely means
    // anything, since the anchor discards where the movement left off.

    /// <summary>The rightmost <paramref name="width"/> columns of the available space.</summary>
    public static IOffsetStrategy<TSpace> FromRight<TSpace>(int width)
      where TSpace : class, ISpace
    {
      NotNegative(width, nameof(width));

      return SelectOffset<TSpace>(space => new Size(Reserve(space.Width, width), 0));
    }

    /// <summary>The bottom <paramref name="height"/> rows of the available space.</summary>
    public static IOffsetStrategy<TSpace> FromBottom<TSpace>(int height)
      where TSpace : class, ISpace
    {
      NotNegative(height, nameof(height));

      return SelectOffset<TSpace>(space => new Size(0, Reserve(space.Height, height)));
    }

    /// <summary>How far in to start so that <paramref name="extent"/> reaches the far edge.</summary>
    private static int Reserve(int available, int extent)
      => extent <= available ? available - extent : throw new OutOfBoundsException();

    private static T NotNull<T>(T value, string parameter) where T : class
      => value ?? throw new ArgumentNullException(parameter);

    private static int NotNegative(int extent, string parameter)
      => extent >= 0 ? extent : throw new ArgumentOutOfRangeException(parameter, extent, "An extent cannot be negative.");
  }
}
