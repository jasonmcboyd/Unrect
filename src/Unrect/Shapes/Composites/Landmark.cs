using Unrect.Core;

namespace Unrect.Shapes
{
  /// <summary>
  /// A landmark and the axis it looks along, as one thing. The two public landmark interfaces are
  /// deliberately separate — a caller should not be able to end a horizontal shape at a row — but a
  /// bounded shape has exactly one of them, and holding "an orientation plus one of two nullable
  /// landmarks" would let a state exist that the type says nothing about.
  /// </summary>
  internal abstract class Landmark
  {
    public static Landmark Of(IRowLandmark landmark) => new OfRow(landmark);

    public static Landmark Of(IColumnLandmark landmark) => new OfColumn(landmark);

    /// <summary>The axis this landmark is found along, and therefore the axis a bound cuts.</summary>
    public abstract Orientation Orientation { get; }

    /// <summary>What was being looked for, phrased for a message: <c>no row containing 'Total'</c>.</summary>
    public abstract string Description { get; }

    /// <summary>How far along the axis the landmark is, or null when there is none.</summary>
    public abstract int? Find(ISpace space);

    private sealed class OfRow : Landmark
    {
      private readonly IRowLandmark _landmark;

      public OfRow(IRowLandmark landmark) => _landmark = landmark;

      public override Orientation Orientation => Orientation.Vertical;
      public override string Description => _landmark.Description;
      public override int? Find(ISpace space) => _landmark.FindRow(space);
    }

    private sealed class OfColumn : Landmark
    {
      private readonly IColumnLandmark _landmark;

      public OfColumn(IColumnLandmark landmark) => _landmark = landmark;

      public override Orientation Orientation => Orientation.Horizontal;
      public override string Description => _landmark.Description;
      public override int? Find(ISpace space) => _landmark.FindColumn(space);
    }
  }
}
