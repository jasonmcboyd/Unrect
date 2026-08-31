using System;

using Unrect.Core;

using Xunit;

namespace Unrect.Tests
{
  /// <summary>
  /// The canonical value model (docs/design/canonical-model-and-shapes.md section 2): a small closed
  /// set of kinds, granular checked accessors, and blankness decided by the adapter — not by Core.
  /// </summary>
  public class CellValueTests
  {
    // --- Kind classification -------------------------------------------------------------------

    [Fact]
    public void Of_String_IsText()
    {
      Assert.Equal(CellKind.Text, CellValue.Of("hello").Kind);
    }

    [Fact]
    public void Of_EmptyString_IsTextNotBlank()
    {
      // Core never guesses at blankness: only a null string is absent. An adapter that wants ""
      // to mean "empty cell" makes that decision at adaptation time (see ArraySpaceTests).
      var value = CellValue.Of("");

      Assert.Equal(CellKind.Text, value.Kind);
      Assert.True(value.HasValue);
      Assert.False(value.IsBlank);
    }

    [Fact]
    public void Of_NullString_IsBlank()
    {
      var value = CellValue.Of((string?)null);

      Assert.Equal(CellKind.Blank, value.Kind);
      Assert.True(value.IsBlank);
      Assert.False(value.HasValue);
    }

    [Fact]
    public void Of_NullString_ReturnsTheBlankSingleton()
    {
      Assert.Same(CellValue.Blank, CellValue.Of((string?)null));
    }

    [Fact]
    public void Blank_IsTheBlankKind()
    {
      Assert.Equal(CellKind.Blank, CellValue.Blank.Kind);
      Assert.True(CellValue.Blank.IsBlank);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-7)]
    [InlineData(int.MaxValue)]
    public void Of_Int_IsNumber(int value)
    {
      Assert.Equal(CellKind.Number, CellValue.Of(value).Kind);
    }

    [Fact]
    public void Of_Long_IsNumber()
    {
      Assert.Equal(CellKind.Number, CellValue.Of(9_000_000_000L).Kind);
    }

    [Fact]
    public void Of_Double_IsNumber()
    {
      Assert.Equal(CellKind.Number, CellValue.Of(1.5).Kind);
    }

    [Fact]
    public void Of_Decimal_IsNumber()
    {
      Assert.Equal(CellKind.Number, CellValue.Of(1.5m).Kind);
    }

    [Fact]
    public void Of_DateTime_IsTemporal()
    {
      Assert.Equal(CellKind.Temporal, CellValue.Of(new DateTime(2026, 6, 30)).Kind);
    }

    [Fact]
    public void Of_Boolean_IsBoolean()
    {
      Assert.Equal(CellKind.Boolean, CellValue.Of(true).Kind);
    }

    // --- Kind-strict accessors: Try* returns null on a kind mismatch ----------------------------

    [Fact]
    public void TryGetString_OnNonText_ReturnsNull()
    {
      Assert.Null(CellValue.Of(1).TryGetString());
      Assert.Null(CellValue.Blank.TryGetString());
      Assert.Null(CellValue.Of(true).TryGetString());
    }

    [Fact]
    public void TryGetDouble_OnNonNumber_ReturnsNull()
    {
      Assert.Null(CellValue.Of("1").TryGetDouble());
      Assert.Null(CellValue.Blank.TryGetDouble());
      Assert.Null(CellValue.Of(new DateTime(2026, 1, 1)).TryGetDouble());
    }

    [Fact]
    public void TryGetDecimal_OnNonNumber_ReturnsNull()
    {
      Assert.Null(CellValue.Of("1").TryGetDecimal());
      Assert.Null(CellValue.Blank.TryGetDecimal());
    }

    [Fact]
    public void TryGetInt_OnNonNumber_ReturnsNull()
    {
      Assert.Null(CellValue.Of("1").TryGetInt());
      Assert.Null(CellValue.Blank.TryGetInt());
      Assert.Null(CellValue.Of(true).TryGetInt());
    }

    [Fact]
    public void TryGetDateTime_OnNonTemporal_ReturnsNull()
    {
      Assert.Null(CellValue.Of(45000).TryGetDateTime());
      Assert.Null(CellValue.Blank.TryGetDateTime());
    }

    [Fact]
    public void TryGetBoolean_OnNonBoolean_ReturnsNull()
    {
      Assert.Null(CellValue.Of(1).TryGetBoolean());
      Assert.Null(CellValue.Of("true").TryGetBoolean());
      Assert.Null(CellValue.Blank.TryGetBoolean());
    }

    // --- Kind-strict accessors: Get* throws on a kind mismatch ---------------------------------

    [Fact]
    public void GetString_OnNonText_Throws()
    {
      Assert.Throws<InvalidOperationException>(() => CellValue.Of(1).GetString());
      Assert.Throws<InvalidOperationException>(() => CellValue.Blank.GetString());
    }

    [Fact]
    public void GetDouble_OnNonNumber_Throws()
    {
      Assert.Throws<InvalidOperationException>(() => CellValue.Of("1").GetDouble());
      Assert.Throws<InvalidOperationException>(() => CellValue.Blank.GetDouble());
    }

    [Fact]
    public void GetDecimal_OnNonNumber_Throws()
    {
      Assert.Throws<InvalidOperationException>(() => CellValue.Of("1").GetDecimal());
      Assert.Throws<InvalidOperationException>(() => CellValue.Blank.GetDecimal());
    }

    [Fact]
    public void GetInt_OnNonNumber_Throws()
    {
      Assert.Throws<InvalidOperationException>(() => CellValue.Of("1").GetInt());
      Assert.Throws<InvalidOperationException>(() => CellValue.Blank.GetInt());
    }

    [Fact]
    public void GetDateTime_OnNonTemporal_Throws()
    {
      Assert.Throws<InvalidOperationException>(() => CellValue.Of(45000).GetDateTime());
    }

    [Fact]
    public void GetBoolean_OnNonBoolean_Throws()
    {
      Assert.Throws<InvalidOperationException>(() => CellValue.Of(1).GetBoolean());
    }

    // --- Accessors on the matching kind ---------------------------------------------------------

    [Fact]
    public void GetString_OnText_ReturnsTheText()
    {
      Assert.Equal("RPT-00042", CellValue.Of("RPT-00042").GetString());
    }

    [Fact]
    public void GetBoolean_OnBoolean_ReturnsTheValue()
    {
      Assert.True(CellValue.Of(true).GetBoolean());
      Assert.False(CellValue.Of(false).GetBoolean());
    }

    [Fact]
    public void GetDateTime_OnTemporal_ReturnsTheValue()
    {
      var moment = new DateTime(2026, 6, 30, 13, 45, 0);

      Assert.Equal(moment, CellValue.Of(moment).GetDateTime());
    }

    [Fact]
    public void GetDate_TruncatesTheTimeOfDay()
    {
      var moment = new DateTime(2026, 6, 30, 13, 45, 0);

      Assert.Equal(new DateTime(2026, 6, 30), CellValue.Of(moment).GetDate());
    }

    // --- Exact numbers: int / long / decimal sources retain decimal fidelity --------------------

    [Fact]
    public void Of_Int_RetainsAnExactDecimal()
    {
      Assert.Equal(5m, CellValue.Of(5).GetDecimal());
      Assert.Equal(5, CellValue.Of(5).GetInt());
      Assert.Equal(5.0, CellValue.Of(5).GetDouble());
    }

    [Fact]
    public void Of_Long_RetainsAnExactDecimal()
    {
      const long value = 9_007_199_254_740_993L; // 2^53 + 1: not representable exactly as a double

      Assert.Equal(9_007_199_254_740_993m, CellValue.Of(value).GetDecimal());
    }

    [Fact]
    public void Of_Decimal_RoundTripsExactly()
    {
      // 0.1 has no exact binary representation; going through the double would lose it.
      Assert.Equal(0.1m, CellValue.Of(0.1m).GetDecimal());
    }

    [Fact]
    public void Of_Decimal_PreservesPrecisionADoubleWouldLose()
    {
      const decimal value = 1234567890123456789.01234m;

      Assert.Equal(value, CellValue.Of(value).GetDecimal());
    }

    [Fact]
    public void GetDecimal_OnDoubleSourcedNumber_ConvertsFromTheDouble()
    {
      // No exact decimal was supplied, so the double is converted on demand.
      Assert.Equal(99999.99m, CellValue.Of(99999.99).GetDecimal());
      Assert.Equal(-82750.25m, CellValue.Of(-82750.25).GetDecimal());
    }

    // --- Numbers with no decimal representation -------------------------------------------------

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(1e300)]
    [InlineData(-1e300)]
    public void TryGetDecimal_OnUnrepresentableNumber_ReturnsNullWithoutThrowing(double value)
    {
      Assert.Null(CellValue.Of(value).TryGetDecimal());
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(1e300)]
    [InlineData(-1e300)]
    public void GetDecimal_OnUnrepresentableNumber_Throws(double value)
    {
      Assert.Throws<InvalidOperationException>(() => CellValue.Of(value).GetDecimal());
    }

    [Fact]
    public void TryGetDouble_OnUnrepresentableNumber_StillReturnsTheDouble()
    {
      // Only the decimal projection is lossy; the number itself is still a number.
      Assert.Equal(1e300, CellValue.Of(1e300).GetDouble());
      Assert.True(double.IsNaN(CellValue.Of(double.NaN).GetDouble()));
    }

    // --- Integral projection --------------------------------------------------------------------

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(3.0, 3)]
    [InlineData(-3.0, -3)]
    [InlineData(2147483647.0, int.MaxValue)]
    [InlineData(-2147483648.0, int.MinValue)]
    public void TryGetInt_OnIntegralInRangeNumber_ReturnsTheValue(double value, int expected)
    {
      Assert.Equal(expected, CellValue.Of(value).TryGetInt());
    }

    [Theory]
    [InlineData(3.5)]
    [InlineData(-0.5)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void TryGetInt_OnNonIntegralNumber_ReturnsNull(double value)
    {
      Assert.Null(CellValue.Of(value).TryGetInt());
    }

    [Theory]
    [InlineData(2147483648.0)]
    [InlineData(-2147483649.0)]
    [InlineData(1e300)]
    public void TryGetInt_OnOutOfRangeNumber_ReturnsNull(double value)
    {
      Assert.Null(CellValue.Of(value).TryGetInt());
    }

    [Fact]
    public void GetInt_OnNonIntegralNumber_Throws()
    {
      Assert.Throws<InvalidOperationException>(() => CellValue.Of(3.5).GetInt());
    }

    [Fact]
    public void GetInt_OnOutOfRangeNumber_Throws()
    {
      Assert.Throws<InvalidOperationException>(() => CellValue.Of(long.MaxValue).GetInt());
    }

    // --- Equality -------------------------------------------------------------------------------

    [Fact]
    public void Equals_ComparesNumbersOnTheirDoubleRepresentation()
    {
      // Documented behaviour: equality is for matching cells, GetDecimal is for extracting values.
      Assert.Equal(CellValue.Of(1.0), CellValue.Of(1m));
      Assert.Equal(CellValue.Of(1L), CellValue.Of(1));
    }

    [Fact]
    public void Equals_OnSamePayload_IsTrue()
    {
      Assert.Equal(CellValue.Of("abc"), CellValue.Of("abc"));
      Assert.Equal(CellValue.Of(new DateTime(2026, 3, 12)), CellValue.Of(new DateTime(2026, 3, 12)));
      Assert.Equal(CellValue.Of(true), CellValue.Of(true));
      Assert.Equal(CellValue.Blank, CellValue.Of((string?)null));
    }

    [Fact]
    public void Equals_OnDifferentPayload_IsFalse()
    {
      Assert.NotEqual(CellValue.Of("abc"), CellValue.Of("abd"));
      Assert.NotEqual(CellValue.Of(1), CellValue.Of(2));
      Assert.NotEqual(CellValue.Of(true), CellValue.Of(false));
      Assert.NotEqual(CellValue.Of(new DateTime(2026, 3, 12)), CellValue.Of(new DateTime(2026, 3, 13)));
    }

    [Fact]
    public void Equals_AcrossKinds_IsFalse()
    {
      Assert.NotEqual(CellValue.Of(1), CellValue.Of("1"));
      Assert.NotEqual(CellValue.Of(1), CellValue.Of(true));
      Assert.NotEqual(CellValue.Of(0), CellValue.Blank);
      Assert.NotEqual(CellValue.Of(""), CellValue.Blank);
      Assert.NotEqual(CellValue.Of(new DateTime(2026, 3, 12)), CellValue.Of(45_000));
    }

    [Fact]
    public void Equals_AgainstNullOrOtherTypes_IsFalse()
    {
      var value = CellValue.Of(1);

      Assert.False(value.Equals((CellValue?)null));
      Assert.False(value.Equals((object?)null));
      Assert.False(value.Equals("not a cell value"));
    }

    [Fact]
    public void GetHashCode_IsEqualForEqualValues()
    {
      Assert.Equal(CellValue.Of(1m).GetHashCode(), CellValue.Of(1.0).GetHashCode());
      Assert.Equal(CellValue.Of("abc").GetHashCode(), CellValue.Of("abc").GetHashCode());
      Assert.Equal(CellValue.Blank.GetHashCode(), CellValue.Of((string?)null).GetHashCode());
      Assert.Equal(
        CellValue.Of(new DateTime(2026, 3, 12)).GetHashCode(),
        CellValue.Of(new DateTime(2026, 3, 12)).GetHashCode());
    }

    [Fact]
    public void EqualityOperator_MatchesEquals()
    {
      Assert.True(CellValue.Of(1m) == CellValue.Of(1.0));
      Assert.False(CellValue.Of(1) == CellValue.Of("1"));
      Assert.True(CellValue.Of(1) != CellValue.Of(2));
      Assert.False(CellValue.Of("a") != CellValue.Of("a"));
    }

    [Fact]
    public void EqualityOperator_HandlesNullOperands()
    {
      CellValue? none = null;
      CellValue? alsoNone = null;
      var some = CellValue.Of(1);

      Assert.True(none == alsoNone);
      Assert.False(none != alsoNone);
      Assert.False(none == some);
      Assert.False(some == none);
      Assert.True(none != some);
      Assert.True(some != none);
    }
  }
}
