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

    private IColumnLandmark Landmark { get; }
    private bool Past { get; }

    public int SelectColumns(ISpace space)
      => Landmark.FindColumn(space) is int column
        ? column + (Past ? 1 : 0)
        : throw new AnchorNotFoundException(Landmark.Description);
  }
}
