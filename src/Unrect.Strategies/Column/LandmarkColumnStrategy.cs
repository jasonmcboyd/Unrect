using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>The column twin of <see cref="LandmarkRowStrategy"/>.</summary>
  internal sealed class LandmarkColumnStrategy : IColumnStrategy
  {
    public LandmarkColumnStrategy(IColumnLandmark landmark, bool past)
    {
      Landmark = landmark;
      Past = past;
    }

    internal IColumnLandmark Landmark { get; }
    internal bool Past { get; }

    public int SelectColumns(Plane<ISpace> space)
      => Landmark.FindColumn(space) is int column
        ? column + (Past ? 1 : 0)
        : throw new AnchorNotFoundException(Landmark.Description);
  }
}
