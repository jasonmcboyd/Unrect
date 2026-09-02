using Unrect.Core;

namespace Unrect.Strategies
{
  /// <summary>
  /// Selects the rows before a landmark, so an offset lifted from this strategy lands a region ON
  /// the match — or one row past it, which is what a shape that does not want to own the row asks
  /// for. Anchoring on presence rather than absence is what survives junk inserted above the thing
  /// being looked for.
  /// <para>
  /// The landmark reports absence rather than throwing; deciding what absence means is the lift's
  /// job, and an offset's answer is that the anchor was required. Because that surfaces as an
  /// <see cref="OutOfBoundsException"/> raised from a placement, a repeat treats it as "no more
  /// sections" rather than as an error.
  /// </para>
  /// </summary>
  internal sealed class LandmarkRowStrategy : IRowStrategy
  {
    public LandmarkRowStrategy(IRowLandmark landmark, bool past)
    {
      Landmark = landmark;
      Past = past;
    }

    private IRowLandmark Landmark { get; }
    private bool Past { get; }

    public int SelectRows(ISpace space)
      => Landmark.FindRow(space) is int row
        ? row + (Past ? 1 : 0)
        : throw new AnchorNotFoundException(Landmark.Description);
  }
}
