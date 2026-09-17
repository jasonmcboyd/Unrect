using System;
using System.Linq;
using System.Threading.Tasks;

using Unrect.Projections;
using Unrect.Spreadsheets;
using Unrect.Strategies;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// A flow whose children are declared by calling <c>Next</c> rather than by being passed in. The
  /// order of the calls is the order of the children, and nothing else about the lambda is a
  /// declaration — there is deliberately no way to ask the cursor where it is or how much is left.
  /// <para>
  /// The arithmetic these tests exercise is pinned against fixed expected values in
  /// <see cref="FlowCompositionTests"/>; what is here is the behaviour that belongs to the flow
  /// itself — misuse of the cursor, faults raised by the lambda, and the opacity that is the cost
  /// of declaring children by running a lambda.
  /// </para>
  /// </summary>
  public class FlowProjectionTests
  {
    /// <summary>A cell read as text — named for what it does, so it cannot be mistaken for the
    /// <c>Text()</c> leaf that the vocabulary now has.</summary>
    private static IProjection<ISheetCells, string> StringCell() => TextCell();

    // --- Flow arithmetic ---------------------------------------------------------------------------

    [Fact]
    public void TheOrderOfTheNextCallsIsTheOrderOfTheChildren()
    {
      Assert.Equal("1|2|3", VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());
        var intCell3 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}|{read.Of(intCell2)}|{read.Of(intCell3)}");
      }).Map(Ladder()));
    }

    [Fact]
    public void AFlowAdvancesAlongItsOwnAxisOnly()
    {
      // The first child is inset one column; the second starts back at column 0, one row down.
      Assert.Equal("2|3", VerticalFlow(v =>
      {
        var right = v.Next(Right(1).Of(IntCell()));
        var intCell = v.Next(IntCell());

        return v.Build(read => $"{read.Of(right)}|{read.Of(intCell)}");
      }).Map(Grid(new[,] { { 1, 2 }, { 3, 4 } })));
    }

    [Fact]
    public void AHorizontalFlowAdvancesAcrossOnly()
    {
      Assert.Equal("3|2", HorizontalFlow(v =>
      {
        var down = v.Next(Down(1).Of(IntCell()));
        var intCell = v.Next(IntCell());

        return v.Build(read => $"{read.Of(down)}|{read.Of(intCell)}");
      }).Map(Grid(new[,] { { 1, 2 }, { 3, 4 } })));
    }

    [Fact]
    public void AFlowIsAsWideAsItsWidestChild()
    {
      var applied = VerticalFlow(v =>
      {
        var rowSlot = v.Next(Row(2, r => r.Count));
        var rowSlot2 = v.Next(Row(3, r => r.Count));

        return v.Build(read => $"{read.Of(rowSlot)}|{read.Of(rowSlot2)}");
      }).Apply(CoordinateGrid());

      Assert.Equal(3, applied.Consumed.Width);
      Assert.Equal(2, applied.Consumed.Height);
    }

    [Fact]
    public void AChildWithADeclaredArea_IsConsumedInFull()
    {
      // The block only reads two rows because it was told to; the next child starts after them.
      Assert.Equal("2|3", VerticalFlow(v =>
      {
        var rangeSlot = v.Next(Range(1, 2, b => b.Height));
        var intCell = v.Next(IntCell());

        return v.Build(read => $"{read.Of(rangeSlot)}|{read.Of(intCell)}");
      }).Map(Ladder()));
    }

    [Fact]
    public void Sized_OverridesWhatTheFlowDerived()
    {
      var applied = Sized(AreaStrategies.ExplicitArea(1, 3)).Of(VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}|{read.Of(intCell2)}");
      }))
        .Apply(Ladder());

      Assert.Equal(1, applied.Consumed.Width);
      Assert.Equal(3, applied.Consumed.Height);
    }

    [Fact]
    public void ASingleNextCallIsALegalFlow()
    {
      // One child is a Select with a placement, which is a useful thing to declare.
      var applied = Down(1).Of(VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());

        return v.Build(read => read.Of(intCell));
      })).Apply(Ladder());

      Assert.Equal(2, applied.Value);
      Assert.Equal(1, applied.Consumed.Height);
    }

    [Fact]
    public void AHorizontalFlowReadsItsChildrenLeftToRight()
    {
      var applied = HorizontalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());
        var intCell3 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}|{read.Of(intCell2)}|{read.Of(intCell3)}");
      })
        .Apply(Grid(new[,] { { 1, 2, 3 } }));

      Assert.Equal("1|2|3", applied.Value);
      Assert.Equal(3, applied.Consumed.Width);
      Assert.Equal(1, applied.Consumed.Height);
    }

    [Fact]
    public void AHorizontalFlowIsAsTallAsItsTallestChild()
    {
      // The cross-axis rule in the other orientation: along the axis the children accumulate,
      // across it the furthest-reaching one wins.
      var applied = HorizontalFlow(v =>
      {
        var columnSlot = v.Next(Column(2, s => s.Count));
        var columnSlot2 = v.Next(Column(3, s => s.Count));

        return v.Build(read => $"{read.Of(columnSlot)}|{read.Of(columnSlot2)}");
      })
        .Apply(Grid(new[,] { { 1, 2 }, { 3, 4 }, { 5, 6 } }));

      Assert.Equal("2|3", applied.Value);
      Assert.Equal(2, applied.Consumed.Width);
      Assert.Equal(3, applied.Consumed.Height);
    }

    [Fact]
    public void AFlowCountsAChildsOwnOffsetInWhatItConsumed()
    {
      // The first child sits one row down, and that row is part of what the flow took: a following
      // sibling of the flow must clear the gap the flow's own child opened.
      var applied = VerticalFlow(v =>
      {
        var down = v.Next(Down(1).Of(IntCell()));
        var intCell = v.Next(IntCell());

        return v.Build(read => $"{read.Of(down)}|{read.Of(intCell)}");
      })
        .Apply(Grid(new[,] { { 0 }, { 1 }, { 2 } }));

      Assert.Equal("1|2", applied.Value);
      Assert.Equal(3, applied.Consumed.Height);
    }

    [Fact]
    public void AChildWithoutADeclaredArea_ConsumesOnlyWhatItsContentUsed()
    {
      // The contrast to the .Sized case above: an inner flow with no declared extent takes the two
      // rows it read, so the next child starts on the third rather than the fourth.
      var projection = VerticalFlow(v =>
      {
        var verticalFlow = v.Next(VerticalFlow(w =>
        {
          var intCell = w.Next(IntCell());
          var intCell2 = w.Next(IntCell());

          return w.Build(read => $"({read.Of(intCell)},{read.Of(intCell2)})");
        }));
        var intCell = v.Next(IntCell());

        return v.Build(read => $"{read.Of(verticalFlow)}|{read.Of(intCell)}");
      });

      Assert.Equal("(1,2)|3", projection.Map(Ladder(4)));
    }

    [Fact]
    public void ANestedFlowWithADeclaredArea_IsConsumedInFull()
    {
      // Declared three rows tall while reading only two, so the next child starts after the third.
      var projection = VerticalFlow(v =>
      {
        var sized = v.Next(Sized(AreaStrategies.ExplicitArea(1, 3)).Of(VerticalFlow(w =>
          {
            var intCell = w.Next(IntCell());
            var intCell2 = w.Next(IntCell());

            return w.Build(read => $"({read.Of(intCell)},{read.Of(intCell2)})");
          })));
        var intCell = v.Next(IntCell());

        return v.Build(read => $"{read.Of(sized)}|{read.Of(intCell)}");
      });

      Assert.Equal("(1,2)|4", projection.Map(Ladder(4)));
    }

    [Fact]
    public void AFlowHasNoArityLimit()
    {
      // The point of the whole spelling: children are Next calls, so there is no tuple to run out
      // of and no nesting to reach for. Twelve here; there is no number that would fail.
      var projection = VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());
        var intCell3 = v.Next(IntCell());
        var intCell4 = v.Next(IntCell());
        var intCell5 = v.Next(IntCell());
        var intCell6 = v.Next(IntCell());
        var intCell7 = v.Next(IntCell());
        var intCell8 = v.Next(IntCell());
        var intCell9 = v.Next(IntCell());
        var intCell10 = v.Next(IntCell());
        var intCell11 = v.Next(IntCell());
        var intCell12 = v.Next(IntCell());

        return v.Build(read => string.Join(",", new[]
        {
          read.Of(intCell), read.Of(intCell2), read.Of(intCell3), read.Of(intCell4),
          read.Of(intCell5), read.Of(intCell6), read.Of(intCell7), read.Of(intCell8),
          read.Of(intCell9), read.Of(intCell10), read.Of(intCell11), read.Of(intCell12),
        }));
      });

      var applied = projection.Apply(Ladder(12));

      Assert.Equal("1,2,3,4,5,6,7,8,9,10,11,12", applied.Value);
      Assert.Equal(12, applied.Consumed.Height);
    }

    // --- The sibling note ---------------------------------------------------------------------------

    [Fact]
    public void AFailureAfterASiblingThatConsumedNothing_IsNoted()
    {
      var space = Mixed(new object?[,] { { "x" }, { 5 } });

      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v =>
        {
          var intCell = v.Next(IntCell().Optional());
          var intCell2 = v.Next(IntCell());

          return v.Build(read => $"{read.Of(intCell)}|{read.Of(intCell2)}");
        }).Map(space));

      Assert.Contains("note: the preceding sibling consumed nothing at this position", failure.Message);
    }

    [Fact]
    public void AChildThatReAnchoredItselfAndFailedElsewhere_IsNotNoted()
    {
      var space = Mixed(new object?[,] { { "x" }, { null }, { null }, { 5 }, { 6 } });

      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v =>
        {
          var intCell = v.Next(IntCell().Optional());
          var offsetBy = v.Next(OffsetBy(BlankRows()).Down(2).Of(StringCell()));

          return v.Build(read => $"{read.Of(intCell)}|{read.Of(offsetBy)}");
        }).Map(space));

      Assert.DoesNotContain("note:", failure.Message);
      Assert.Equal("A3", failure.Location.A1);
    }

    // --- Repetition ------------------------------------------------------------------------------------

    [Fact]
    public void ARepeatedFlow_StopsAtTheSeparator()
    {
      var space = Grid(new[,] { { 1 }, { 2 }, { 0 }, { 3 }, { 4 }, { 0 }, { 0 } });

      var items = VerticalRepeat(VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}+{read.Of(intCell2)}");
      }), separatedBy: BlankRows()).Map(space);

      Assert.Equal(new[] { "1+2", "3+4" }, items);
    }

    [Fact]
    public void ARepeatedFlow_HonoursAtLeast()
    {
      var space = Grid(new[,] { { 1 }, { 2 }, { 0 }, { 3 }, { 4 }, { 0 }, { 0 } });

      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalRepeat(VerticalFlow(v =>
        {
          var intCell = v.Next(IntCell());
          var intCell2 = v.Next(IntCell());

          return v.Build(read => $"{read.Of(intCell)}+{read.Of(intCell2)}");
        }), separatedBy: BlankRows(), atLeast: 5).Map(space));

      Assert.Contains("expected at least 5 occurrences but found 2", failure.Message);
    }

    [Fact]
    public void AFailureInsideARepeatedFlow_IsLoudRatherThanAStop()
    {
      // A Next call is deeper than the item's own placement, so it is drift, not the end of the
      // run.
      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalRepeat(VerticalFlow(v =>
        {
          var intCell = v.Next(IntCell());
          var stringCell = v.Next(StringCell());

          return v.Build(read => $"{read.Of(intCell)}+{read.Of(stringCell)}");
        })).Map(Ladder()));

      Assert.Contains("VerticalRepeat[0]", failure.Path);
      Assert.Contains("expected Text", failure.Message);
    }

    [Fact]
    public void ARepeatedFlowThatConsumesNothing_Terminates()
    {
      Assert.Empty(VerticalRepeat(VerticalFlow(v =>
      {
        var rangeSlot = v.Next(Range(AreaStrategies.MinArea(), b => b.Width));

        return v.Build(read => read.Of(rangeSlot));
      })).Map(Ladder()));
    }

    // --- Alternation -----------------------------------------------------------------------------------

    [Fact]
    public void ALosingBranchLeavesNoDiagnosticsAndNeverReachesItsCombiner()
    {
      // The combiner runs only once every child has read, so a branch that loses on a child never
      // runs it at all — and the declaration lambda ran once, at declaration, before any branch was
      // tried. Diagnostics roll back; a combiner that did run (one that threw, say) leaves its side
      // effects behind, which is why the factory's documentation says to capture nothing you write to.
      var declared = 0;
      var combined = 0;

      var losing = VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var stringCell = v.Next(StringCell());
        declared++;

        return v.Build(read =>
        {
          combined++;
          return $"{read.Of(intCell)}{read.Of(stringCell)}";
        });
      });

      Assert.Equal(1, declared);

      var result = Choice(losing, VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}w");
      })).MapWithDiagnostics(Ladder());

      Assert.Equal("1w", result.Value);
      Assert.Equal(1, declared);
      Assert.Equal(0, combined);
      Assert.DoesNotContain(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    [Fact]
    public void AWinningBranchNamesTheOnesBeforeIt()
    {
      var result = Choice(
        VerticalFlow(v =>
        {
          var stringCell = v.Next(StringCell());

          return v.Build(read => $"{read.Of(stringCell)}");
        }),
        VerticalFlow(v =>
        {
          var intCell = v.Next(IntCell());

          return v.Build(read => $"{read.Of(intCell)}");
        }))
        .MapWithDiagnostics(Ladder());

      Assert.Equal("1", result.Value);
      Assert.Contains(
        result.Diagnostics,
        d => d.Severity == DiagnosticSeverity.Info && d.Message.StartsWith("alternative 1 (VerticalFlow) did not match: "));
    }

    // --- Tolerance -------------------------------------------------------------------------------------

    [Fact]
    public void ABoundaryAroundAFlow_AbsorbsADeepFailureWithTheInnerPath()
    {
      var deep = VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var verticalFlow = v.Next(VerticalFlow(w =>
          {
            var intCell = w.Next(IntCell());
            var stringCell = w.Next(StringCell().Named("deep"));

            return w.Build(read => $"{read.Of(intCell)}{read.Of(stringCell)}");
          }));

        return v.Build(read => $"{read.Of(intCell)}|{read.Of(verticalFlow)}");
      });

      var result = deep.Optional().MapWithDiagnostics(Ladder());

      Assert.Null(result.Value);

      var warning = Assert.Single(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
      Assert.Equal("VerticalFlow -> VerticalFlow#2 -> 'deep' (Text)", warning.Path);
      Assert.Equal("A3", warning.Location.A1);
    }

    [Fact]
    public void AnAbsorbedFlow_ConsumesNothing()
    {
      var applied = VerticalFlow(v =>
      {
        var verticalFlow = v.Next(VerticalFlow(w =>
        {
          var stringCell = w.Next(StringCell());

          return w.Build(read2 => read2.Of(stringCell));
        }).Else("fallback"));
        var intCell = v.Next(IntCell());

        return v.Build(read => $"{read.Of(verticalFlow)}|{read.Of(intCell)}");
      })
        .Apply(Ladder());

      Assert.Equal("fallback|1", applied.Value);
      Assert.Equal(1, applied.Consumed.Height);
    }

    // --- Faults ------------------------------------------------------------------------------------------

    [Fact]
    public void AChildFailureInsideNext_IsNotWrappedAgain()
    {
      // The failure belongs to the child, with the child's path and cell. Only the sibling note may
      // ever be added to it.
      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow(v =>
        {
          var intCell = v.Next(IntCell());
          var stringCell = v.Next(StringCell().Named("title"));

          return v.Build(read => $"{read.Of(intCell)}|{read.Of(stringCell)}");
        }).Map(Ladder()));

      Assert.Equal("'title'", failure.Subject);
      Assert.Equal("VerticalFlow -> 'title' (Text)", failure.Path);
      Assert.Equal("A2", failure.Location.A1);

      // Nothing wrapped: a kinded leaf is the reader, so its refusal is the failure rather than
      // something caught and re-described. Until phase 6 the leaf threw an InvalidOperationException
      // from inside its own lambda, and this asserted that.
      Assert.Null(failure.InnerException);
    }

    [Fact]
    public void UserCodeThrowingInTheCombiner_IsWrappedOnceAtTheFlowsOwnOrigin()
    {
      // The throw happened in the combiner, so the flow is what failed. This is why parsing
      // belongs inside a leaf's projection, where the location is the cell.
      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow<int>(v => { _ = v.Next(IntCell()); return v.Build<int>(_ => throw new InvalidOperationException("boom")); }).Map(Ladder()));

      Assert.Equal("VerticalFlow", failure.Subject);
      Assert.Equal("VerticalFlow", failure.Path);
      Assert.Equal("A1", failure.Location.A1);
      Assert.Contains("the projection threw InvalidOperationException: boom", failure.Message);
    }

    [Fact]
    public void ANullReferenceInTheCombiner_IsAFaultAndIsNotAbsorbed()
    {
      var failure = Assert.Throws<ProjectionException>(() =>
        VerticalFlow<int>(v => { _ = v.Next(IntCell()); return v.Build<int>(_ => throw new NullReferenceException("boom")); })
          .Optional()
          .Map(Ladder()));

      Assert.IsType<NullReferenceException>(failure.GetBaseException());
    }

    [Fact]
    public void AnArgumentExceptionInTheCombiner_IsStillAbsorbable()
    {
      // A parse that disagreed with the data is what tolerance is for; only the broken-code
      // exceptions are exempt.
      var result = VerticalFlow<int>(v => { _ = v.Next(IntCell()); return v.Build<int>(_ => throw new ArgumentException("bad")); })
        .Optional()
        .MapWithDiagnostics(Ladder());

      Assert.Equal(0, result.Value);
      Assert.Contains(result.Diagnostics, d => d.Severity == DiagnosticSeverity.Warning);
    }

    // --- Misuse -----------------------------------------------------------------------------------------------

    [Fact]
    public void AFlowThatDeclaresNothing_IsRefusedWhereItIsWritten()
    {
      // It would match anything, describe nothing, and quietly end an enclosing repetition — so
      // Build refuses it at declaration, before there is a space or a boundary to hide it.
      var failure = Assert.Throws<InvalidOperationException>(() => VerticalFlow<int>(v => v.Build(_ => 42)));

      Assert.Equal("a flow must declare at least one projection; this one called Next zero times", failure.Message);
    }

    [Fact]
    public void ANullProjectionIsRefusedWhereItIsWritten()
    {
      // A hole in the declaration is refused at the declaration, naming the child it would have
      // been; there is no space yet, so no cell to report it against and no boundary to absorb it.
      IProjection<ISheetCells, int>? missing = null;

      var failure = Assert.Throws<ArgumentNullException>(() =>
        VerticalFlow(v =>
        {
          var intCell = v.Next(IntCell());
          var missing2 = v.Next(missing!);

          return v.Build(read => $"{read.Of(intCell)}|{read.Of(missing2)}");
        }));

      Assert.Contains("a null projection was declared as child 2", failure.Message);
    }

    [Fact]
    public void ANullFirstChildIsRefusedAsChildOne()
    {
      IProjection<ISheetCells, int>? missing = null;

      var failure = Assert.Throws<ArgumentNullException>(() => VerticalFlow(v =>
      {
        var missing2 = v.Next(missing!);

        return v.Build(read => $"{read.Of(missing2)}");
      }));

      Assert.Contains("a null projection was declared as child 1", failure.Message);
    }

    [Fact]
    public void ALayoutLambdaThatForgetsToBuild_DoesNotCompile()
    {
      // Pinned as prose: `VerticalFlow(v => v.Next(IntCell()))` returns a Slot where a Layout is
      // required, and `VerticalFlow(_ => 42)` an int. The one diagnostic that moved into the type
      // system needs no run-time test, only this note that it was not forgotten.
    }

    [Fact]
    public void ACursorUsedAfterItsLayoutWasBuilt_IsRefused()
    {
      var failure = Assert.Throws<InvalidOperationException>(() => VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var layout = v.Build(read => read.Of(intCell));

        v.Next(IntCell());

        return layout;
      }));

      Assert.Equal("A layout cursor cannot be used outside the layout that created it; this one has already been built.", failure.Message);
    }

    [Fact]
    public void ALayoutBuiltByAnotherCursor_IsRefused()
    {
      Layout<ISheetCells, int>? stolen = null;

      VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        stolen = v.Build(read => read.Of(intCell));
        return stolen;
      });

      var failure = Assert.Throws<ArgumentException>(() => VerticalFlow(v => stolen!));

      Assert.Contains("another cursor built", failure.Message);
    }

    [Fact]
    public void ASlotReadsOnlyInTheLayoutThatDeclaredIt()
    {
      Slot<int> foreign = default;

      VerticalFlow(v =>
      {
        foreign = v.Next(IntCell());
        return v.Build(read => read.Of(foreign));
      });

      var other = VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        return v.Build(read => read.Of(intCell) + read.Of(foreign));
      });

      var failure = Assert.Throws<ProjectionException>(() => other.Map(Ladder()));

      Assert.Contains("belongs to another layout", failure.Message);
      Assert.Contains("never declared", Assert.Throws<ProjectionException>(() => VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        return v.Build(read => read.Of(intCell) + read.Of(default(Slot<int>)));
      }).Map(Ladder())).Message);
    }

    [Fact]
    public void ACursorThatNeverHadALayout_RefusesToBeUsed()
    {
      // The only escape the compiler cannot catch, because anyone can construct it. The message
      // says which of the two ways of being outside a layout this is; the other — a cursor used
      // after its layout returned — cannot be reached from C# at all, so it has no test.
      var failure = Assert.Throws<InvalidOperationException>(() => default(LayoutCursor<ISheetCells>).Next(IntCell()));

      Assert.Equal(
        "A layout cursor cannot be used outside the layout that created it; this one never had a layout.",
        failure.Message);
    }

    // --- Inspection -------------------------------------------------------------------------------------------------

    [Fact]
    public void AFlowDescribesItselfByItsOrientation()
    {
      // The description is what a path segment renders as, so it is pinned as a literal rather
      // than compared against another spelling of the same thing.
      Assert.Equal("VerticalFlow", VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}{read.Of(intCell2)}");
      }).Description);
      Assert.Equal("HorizontalFlow", HorizontalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}{read.Of(intCell2)}");
      }).Description);
    }

    [Fact]
    public void AFlowEnumeratesItsChildrenInDeclarationOrder()
    {
      // The lambda ran once, at declaration, so what it declared is complete before any space exists:
      // anything structural that runs without a space sees every child, in order, at its ordinal.
      var children = VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}{read.Of(intCell2)}");
      }).Children;

      Assert.Equal(2, children.Count);
      Assert.Equal(1, children[0].Site.Ordinal);
      Assert.Equal(2, children[1].Site.Ordinal);
    }

    [Fact]
    public void AFlowHidesNothing()
    {
      // Its children are the whole truth, so it has no opacity to declare.
      var projection = VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}{read.Of(intCell2)}");
      });

      Assert.Null(projection.Opacity);
    }

    [Fact]
    public void AFlowIsAProjectionAndCanBeNamedAndPlaced()
    {
      var space = Grid(new[,] { { 0 }, { 1 }, { 2 } });

      var projection = AfterBlankRows().Of(VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}|{read.Of(intCell2)}");
      })).Named("block");

      Assert.Equal("1|2", projection.Map(space));
      Assert.Equal("block", projection.Name);
      Assert.False(projection.IsWrapper);
      Assert.Null(projection.Placement.Area);
    }

    [Fact]
    public void ACaptureNothingFlowIsSafeToApplyToManySpacesAtOnce()
    {
      // The immutability guarantee survives the lambda as long as the lambda writes to nothing.
      var projection = VerticalFlow(v =>
      {
        var intCell = v.Next(IntCell());
        var intCell2 = v.Next(IntCell());

        return v.Build(read => $"{read.Of(intCell)}|{read.Of(intCell2)}");
      });

      var spaces = Enumerable.Range(0, 64)
        .Select(seed => Grid(new[,] { { seed + 1 }, { seed + 2 } }))
        .ToArray();

      var results = new string[spaces.Length];

      Parallel.For(0, spaces.Length, index => results[index] = projection.Map(spaces[index]));

      for (var index = 0; index < spaces.Length; index++)
        Assert.Equal($"{index + 1}|{index + 2}", results[index]);
    }

    // --- Escape hazards: compiler diagnostics, not assertions ------------------------------------------------------------
    //
    // LayoutCursor is a readonly ref struct, so every way of using it outside its own lambda is a
    // compile error rather than a runtime one. That is the guard, and it cannot be written as a
    // test without a compilation harness — so the snippets live here, each verified against this
    // build to produce the code shown.
    //
    //   IProjection<ISheetCells, List<int>> s = VerticalFlow(v => Enumerable.Range(0, 3).Select(i => v.Next(x)).ToList());
    //     CS9108 — cannot use ref-like 'v' inside an anonymous method or lambda. Covers the
    //     deferred-query hazard too: unmaterialised, the query fails the same way.
    //
    //   IProjection<ISheetCells, int> s = VerticalFlow(v => { int F() => v.Next(x); return F(); });
    //     CS9108 — the same rule for a local function.
    //
    //   IProjection<ISheetCells, LayoutCursor> s = VerticalFlow(v => v);
    //     CS9244 — the type 'LayoutCursor' may not be a type argument (returning the cursor).
    //
    //   static LayoutCursor field; ... VerticalFlow(v => { field = v; return v.Next(x); });
    //     CS8345 — a ref-struct field may not be a member of a class.
    //
    //   VerticalFlow(v => { var list = new List<LayoutCursor>(); list.Add(v); return v.Next(x); });
    //     CS9244 — the generic argument fails before the Add does.
    //
    //   VerticalFlow(v => { var array = new LayoutCursor[1]; array[0] = v; return v.Next(x); });
    //     CS0611 — an array element may not be a ref struct.
    //
    //   VerticalFlow(header);   // one projection, no lambda
    //     CS0411 — no silent binding to an unintended overload.
    //
    // The one escape the compiler cannot see is default(LayoutCursor), which anyone can construct;
    // ACursorThatNeverHadALayout_RefusesToBeUsed above is that guard.
  }
}
