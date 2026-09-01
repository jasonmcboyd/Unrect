using System;
using System.Linq;

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
    public void TryGetDate_TruncatesTheTimeOfDay()
    {
      // The Try twin of GetDate, so a caller reading an optional date need not reach for exceptions.
      var moment = new DateTime(2026, 6, 30, 13, 45, 0);

      Assert.Equal(new DateTime(2026, 6, 30), CellValue.Of(moment).TryGetDate());
    }

    [Fact]
    public void TryGetDate_OnNonTemporal_ReturnsNull()
    {
      Assert.Null(CellValue.Of(45000).TryGetDate());
      Assert.Null(CellValue.Of("2026-06-30").TryGetDate());
      Assert.Null(CellValue.Blank.TryGetDate());
      Assert.Null(CellValue.OfError(CellError.Value).TryGetDate());
    }

    [Fact]
    public void GetDate_OnNonTemporal_StillThrows()
    {
      // Adding the Try form must not soften the strict one.
      Assert.Throws<InvalidOperationException>(() => CellValue.Of(45000).GetDate());
      Assert.Throws<InvalidOperationException>(() => CellValue.Blank.GetDate());
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

    // --- Errors ---------------------------------------------------------------------------------
    //
    // An error is a value a cell genuinely holds — a formula that could not produce a result — and
    // not a missing cell. It therefore has a value, is never blank, and must never be skippable as
    // empty space by a strategy looking for the end of a region.

    [Theory]
    [InlineData(CellError.Null)]
    [InlineData(CellError.DivisionByZero)]
    [InlineData(CellError.Value)]
    [InlineData(CellError.Reference)]
    [InlineData(CellError.Name)]
    [InlineData(CellError.Number)]
    [InlineData(CellError.NotAvailable)]
    [InlineData(CellError.GettingData)]
    public void OfError_IsAnErrorCellThatCarriesAValue(CellError error)
    {
      var value = CellValue.OfError(error);

      Assert.Equal(CellKind.Error, value.Kind);
      Assert.True(value.HasValue);
      Assert.False(value.IsBlank);
      Assert.Equal(error, value.GetError());
      Assert.Equal(error, value.TryGetError());
    }

    [Theory]
    [InlineData(CellError.Null, "Error(#NULL!)")]
    [InlineData(CellError.DivisionByZero, "Error(#DIV/0!)")]
    [InlineData(CellError.Value, "Error(#VALUE!)")]
    [InlineData(CellError.Reference, "Error(#REF!)")]
    [InlineData(CellError.Name, "Error(#NAME?)")]
    [InlineData(CellError.Number, "Error(#NUM!)")]
    [InlineData(CellError.NotAvailable, "Error(#N/A)")]
    [InlineData(CellError.GettingData, "Error(#GETTING_DATA)")]
    public void ToString_SpellsAnErrorTheWayASheetShowsIt(CellError error, string expected)
    {
      Assert.Equal(expected, CellValue.OfError(error).ToString());
    }

    [Fact]
    public void EveryDeclaredErrorHasItsOwnSpelling()
    {
      // Guards the spelling table against a copy-paste: eight errors, eight distinct renderings.
      var spellings = Enum.GetValues(typeof(CellError))
        .Cast<CellError>()
        .Select(error => CellValue.OfError(error).ToString())
        .ToArray();

      Assert.Equal(8, spellings.Length);
      Assert.Equal(spellings.Length, spellings.Distinct().Count());
      Assert.All(spellings, spelling => Assert.StartsWith("Error(#", spelling));
    }

    [Fact]
    public void GettingData_IsRepresentedRatherThanRejected()
    {
      // The transient state of an asynchronous formula, occasionally found cached in a saved
      // workbook: carrying it faithfully is what keeps such a file parseable at all.
      var value = CellValue.OfError(CellError.GettingData);

      Assert.Equal(CellError.GettingData, value.GetError());
      Assert.Equal("Error(#GETTING_DATA)", value.ToString());
    }

    [Fact]
    public void TryGetError_OnANonErrorCell_ReturnsNull()
    {
      Assert.Null(CellValue.Of(1).TryGetError());
      Assert.Null(CellValue.Of("#VALUE!").TryGetError());
      Assert.Null(CellValue.Blank.TryGetError());
    }

    [Fact]
    public void GetError_OnANonErrorCell_Throws()
    {
      Assert.Throws<InvalidOperationException>(() => CellValue.Of(1).GetError());
      Assert.Throws<InvalidOperationException>(() => CellValue.Blank.GetError());
    }

    [Fact]
    public void TryAccessorsOnAnErrorCell_ReturnNull()
    {
      var value = CellValue.OfError(CellError.Reference);

      Assert.Null(value.TryGetString());
      Assert.Null(value.TryGetDouble());
      Assert.Null(value.TryGetDecimal());
      Assert.Null(value.TryGetInt());
      Assert.Null(value.TryGetDateTime());
      Assert.Null(value.TryGetBoolean());
    }

    [Fact]
    public void TypedAccessorsOnAnErrorCell_ThrowAndNameTheError()
    {
      // "expected Number" alone would send the reader looking for a type mismatch; naming the
      // error says the sheet is broken, not the declaration.
      var value = CellValue.OfError(CellError.Value);

      Assert.Equal(
        "Cell value is Error (#VALUE!); expected Number.",
        Assert.Throws<InvalidOperationException>(() => value.GetDouble()).Message);
      Assert.Equal(
        "Cell value is Error (#VALUE!); expected Text.",
        Assert.Throws<InvalidOperationException>(() => value.GetString()).Message);
      Assert.Equal(
        "Cell value is Error (#VALUE!); expected Number.",
        Assert.Throws<InvalidOperationException>(() => value.GetDecimal()).Message);
      Assert.Equal(
        "Cell value is Error (#VALUE!); expected Number.",
        Assert.Throws<InvalidOperationException>(() => value.GetInt()).Message);
      Assert.Equal(
        "Cell value is Error (#VALUE!); expected Temporal.",
        Assert.Throws<InvalidOperationException>(() => value.GetDateTime()).Message);
      Assert.Equal(
        "Cell value is Error (#VALUE!); expected Boolean.",
        Assert.Throws<InvalidOperationException>(() => value.GetBoolean()).Message);
    }

    [Fact]
    public void Errors_AreEqualWhenTheyAreTheSameError()
    {
      Assert.Equal(CellValue.OfError(CellError.Value), CellValue.OfError(CellError.Value));
      Assert.NotEqual(CellValue.OfError(CellError.Value), CellValue.OfError(CellError.Name));
      Assert.True(CellValue.OfError(CellError.Value) == CellValue.OfError(CellError.Value));
      Assert.True(CellValue.OfError(CellError.Value) != CellValue.OfError(CellError.Name));
    }

    [Fact]
    public void AnErrorIsNotItsOwnSpelling()
    {
      // #VALUE! the error and "#VALUE!" the text are different kinds of cell, and neither is empty.
      Assert.NotEqual(CellValue.OfError(CellError.Value), CellValue.Of("#VALUE!"));
      Assert.NotEqual(CellValue.OfError(CellError.Value), CellValue.Blank);
      Assert.NotEqual(CellValue.OfError(CellError.Number), CellValue.Of(0));
    }

    [Fact]
    public void GetHashCode_IsEqualForEqualErrors()
    {
      Assert.Equal(
        CellValue.OfError(CellError.NotAvailable).GetHashCode(),
        CellValue.OfError(CellError.NotAvailable).GetHashCode());
    }
  }
}
