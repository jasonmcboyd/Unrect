using System;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

// THE COEXISTENCE PIN, written rather than asserted: this file imports both closed classes the way a
// real declaration file does, and everything below is spelled with no prefix. It compiles, and that
// is the claim — two `using static` imports coexist exactly when they share no simple name, so the
// backend rule (a backend re-exports its OWN vocabulary and not one member of the core one) is what
// makes this pair legal. CS0104 is reported per NAME at the use site, so a member repeated across
// the two would not break this file until something below tried to write it.
//
// Both type arguments are spelled in FULL, and must be: a `using` directive is resolved without the
// other `using`s around it, so neither `Unrect.Projections;` nor `Unrect.Spreadsheets;` above helps
// here. That is the price of naming the space exactly once per file, and it is paid in the one place
// where a namespace import cannot reach.
using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;
using static Unrect.Spreadsheets.SpreadsheetProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;

namespace Unrect.Tests.Spreadsheets
{
  /// <summary>
  /// <see cref="SpreadsheetProjectionBuilders{TSpace}"/> — the backend half of the file-scoped
  /// vocabulary — against the <see cref="SpreadsheetProjections"/> members it re-exports, plus the
  /// two rules that make the pair usable: the backend publishes only names <c>Unrect</c> cannot, and
  /// the two imports therefore coexist.
  /// <para>
  /// The file is deliberately written in the shipping shape: both closed classes imported statically,
  /// no prefix on anything, and the plain family reached by its class name where a comparison needs
  /// it. The core class's own parity suite is
  /// <see cref="Unrect.Tests.Projections.ProjectionBuildersParityTests"/>, which cannot be written
  /// this way and says why.
  /// </para>
  /// </summary>
  public class SpreadsheetProjectionBuildersParityTests
  {
    /// <summary>
    /// A capable sheet: a caption over a value, with a formula behind the value and none behind the
    /// caption, so a reading can tell the two halves of the capability apart.
    /// </summary>
    private static ISpreadsheetSpace Sheet() => new FormulaGridSpace(
      new[,]
      {
        { Cell.Of("Amount"), Cell.Blank },
        { Cell.Of(100m), Cell.Of(250m) },
      },
      new string?[,]
      {
        { null, null },
        { "SUM(A1:A1)", null },
      });

    // --- 1. The six members, each against its plain twin ---------------------------------------------

    [Fact]
    public void AKindedLeafReadsAsItsPlainTwin()
    {
      // The six kinded members, each one line to the plain family. A forwarder that named the wrong
      // factory would still compile and still read a cell, which is why the comparison is the
      // reading rather than the type.
      var sheet = Sheet();

      Observations.AssertL3(
        Observations.Observe(SpreadsheetProjections.Text<ISpreadsheetSpace>(), sheet),
        Observations.Observe(Text(), sheet));

      Observations.AssertL3(
        Observations.Observe(Down(1).Of(SpreadsheetProjections.Decimal<ISpreadsheetSpace>()), sheet),
        Observations.Observe(Down(1).Of(Decimal()), sheet));

      Assert.Equal("Amount", Text().Map(sheet));
      Assert.Equal(100m, Down(1).Of(Decimal()).Map(sheet));
    }

    [Fact]
    public void ATerminalIsThePipelineClosedOverTheSameLeaf()
    {
      // The kinded leaves are also pipeline terminals — `Down(1).Decimal()`, the placement said
      // before the subject. Each closes through the public `Of`, so the terminal and the hoisted
      // spelling must be the same declaration: same reading, same path, same description.
      var sheet = Sheet();

      Observations.AssertL3(
        Observations.Observe(Down(1).Of(Decimal()), sheet),
        Observations.Observe(Down(1).Decimal(), sheet));

      Observations.AssertL3(
        Observations.Observe(Down(1).Of(Formula()), sheet),
        Observations.Observe(Down(1).Formula(), sheet));

      Assert.Equal(100m, Down(1).Decimal().Map(sheet));
      Assert.Equal("SUM(A1:A1)", Down(1).Formula().Map(sheet));
    }

    [Fact]
    public void TheFormulaLeafReadsAsItsPlainTwin_WhereThereIsAFormula()
    {
      // Placed on A2, the one cell of the fixture that carries one. Both spellings are read through
      // the observation harness and compared at L3, so a divergence in what is consumed or in what
      // the parse noticed would show as well as one in the text.
      var sheet = Sheet();

      var throughBuilders = Down(1).Of(Formula());
      var plain = Down(1).Of(SpreadsheetProjections.Formula<ISpreadsheetSpace>());

      Observations.AssertL3(Observations.Observe(plain, sheet), Observations.Observe(throughBuilders, sheet));

      // ...and it really reads the formula rather than the value.
      Assert.Equal("SUM(A1:A1)", Down(1).Of(Formula()).Map(sheet));
    }

    [Fact]
    public void AndWhereThereIsNone_AsTheSameHonestNull()
    {
      // The other half, and the one a twin could diverge on without any test over a formula-bearing
      // cell noticing: absence is an honest per-cell null, not a failure.
      var sheet = Sheet();

      var throughBuilders = Formula();
      var plain = SpreadsheetProjections.Formula<ISpreadsheetSpace>();

      Observations.AssertL3(Observations.Observe(plain, sheet), Observations.Observe(throughBuilders, sheet));
      Assert.Null(Formula().Map(sheet));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("SUM")]
    [InlineData("PRODUCT")]
    public void TheFormulaMatchersLocateWhatTheirPlainTwinsLocate(string? containing)
    {
      var sheet = Sheet();

      var rowThroughBuilders = containing is null ? RowWithFormula() : RowWithFormula(containing);
      var rowPlain = containing is null
        ? SpreadsheetProjections.RowWithFormula()
        : SpreadsheetProjections.RowWithFormula(containing);

      var columnThroughBuilders = containing is null ? ColumnWithFormula() : ColumnWithFormula(containing);
      var columnPlain = containing is null
        ? SpreadsheetProjections.ColumnWithFormula()
        : SpreadsheetProjections.ColumnWithFormula(containing);

      var region = Plane<ISpace>.Of(sheet);

      Assert.Equal(rowPlain.Landmark.FindRow(region), rowThroughBuilders.Landmark.FindRow(region));
      Assert.Equal(columnPlain.Landmark.FindColumn(region), columnThroughBuilders.Landmark.FindColumn(region));

      // Non-vacuity: the first two cases find something and the third finds nothing, so the equality
      // above is not two nulls agreeing.
      Assert.Equal(containing == "PRODUCT" ? null : (int?)1, rowThroughBuilders.Landmark.FindRow(region));
      Assert.Equal(containing == "PRODUCT" ? null : (int?)0, columnThroughBuilders.Landmark.FindColumn(region));
    }

    // --- 2. The two rules that make the pair usable ---------------------------------------------------

    [Fact]
    public void AndOneDeclarationWrittenThroughBothImportsReadsTheSheet()
    {
      // The coexistence claim, exercised rather than merely compiled: a layout from the core class,
      // a leaf from each class, one space named nowhere in the declaration, and a demand answered by
      // the file's own import.
      var value = Decimal();
      var formula = Right(1).Of(Formula());

      var line = Overlay(o => (Value: o.Next(value), Formula: o.Next(formula)));

      var read = Down(1).Of(line).Map(Sheet());

      Assert.Equal(100m, read.Value);
      Assert.Null(read.Formula);
    }

    // --- 3. The backend's own completeness covenant ---------------------------------------------------

    [Fact]
    public void EveryBackendFactoryIsReachableFromTheBackendBuilders()
    {
      // The mirror of the core covenant, and with nothing to exclude: `Unrect` cannot name any of
      // these, so there is no plain spelling a closed one could fail to improve on. A member added to
      // SpreadsheetProjections and left un-re-exported fails here.
      var expected = Unrect.Tests.Projections.ProjectionBuildersParityTests
        .Signatures(typeof(SpreadsheetProjections))
        .Select(Shape)
        .OrderBy(shape => shape, StringComparer.Ordinal);

      var actual = Unrect.Tests.Projections.ProjectionBuildersParityTests
        .Signatures(typeof(SpreadsheetProjectionBuilders<>))
        .Select(Shape)
        .OrderBy(shape => shape, StringComparer.Ordinal);

      Assert.Equal(expected, actual);
    }

    private static string Shape(string signature)
    {
      var open = signature.IndexOf('(');

      if (open < 0)
        return signature + "/property";

      var parameters = signature.Substring(open + 1, signature.Length - open - 2);

      return Name(signature) + "/" + (parameters.Length == 0 ? 0 : parameters.Count(c => c == ',') + 1);
    }

    private static string Name(string signature)
    {
      var end = signature.IndexOfAny(new[] { '<', '(' });

      return end < 0 ? signature : signature.Substring(0, end);
    }
  }
}
