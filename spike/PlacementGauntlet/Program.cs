using System;

namespace PlacementGauntlet
{
  /// <summary>
  /// SPIKE — <c>docs/design/placement-gauntlet-spec.md</c> scenarios 1–5, each compiled and RUN,
  /// each asserting that the inverted pipeline's spelling reads exactly what today's spelling reads.
  /// Scenario 6 is <c>MustNotCompile.cs</c> (build with <c>-p:DefineConstants=MUST_NOT_COMPILE</c>);
  /// scenario 7 is <c>MIGRATION-READ.md</c>.
  /// </summary>
  public static class Program
  {
    public static int Main()
    {
      Scenario1.Run();
      Scenario2.Run();
      Scenario3.Run();
      Scenario4.Run();
      Scenario5.Run();
      ScenarioG.Run();
      ScenarioC.Run();
      Intermediates();

      Console.WriteLine();
      Console.WriteLine(Judge.FailureCount == 0
        ? "All differentials agree: the façade is a spelling, not a semantics."
        : $"{Judge.FailureCount} differential(s) DISAGREE — see [DIFF] above.");

      return Judge.FailureCount == 0 ? 0 : 1;
    }

    /// <summary>
    /// What a reader sees when they hover an intermediate — the §14.5 tradition of judging a design
    /// by what a tooltip shows. The type name is the whole story a tooltip tells; the rendering
    /// beside it is what the stage would have to say for itself in a debugger or a dry-run renderer.
    /// </summary>
    private static void Intermediates()
    {
      Judge.Section("The intermediates, as a reader meets them");

      var mark = Unrect.Projections.Projection.RowContaining("Total");

      Console.WriteLine($"  Below(mark)                          : {Staged.Place.Below(mark)}");
      Console.WriteLine($"  Below(mark).Down(1)                  : {Staged.Place.Below(mark).Down(1)}");
      Console.WriteLine($"  Below(mark).Down(1).Sized(3x4)       : {Staged.Place.Below(mark).Down(1).Sized(Unrect.Projections.Projection.Extent(3, 4))}");
      Console.WriteLine($"  Below(mark).Until(mark)              : {Staged.Place.Below(mark).Until(mark)}");
      Console.WriteLine($"  Offset()                             : {Staged.Place.Offset()}");
      Console.WriteLine($"  Over<ISpace>().Below(mark)           : {Staged.Place.Over<Unrect.Core.ISpace>().Below(mark)}");
    }
  }
}
