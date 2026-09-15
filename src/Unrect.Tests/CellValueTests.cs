using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

using Unrect.Core;

using Xunit;

namespace Unrect.Tests
{
  /// <summary>
  /// The canonical value model: a small
  /// closed set of kinds, granular checked accessors, and blankness decided by the adapter — not by
  /// Core.
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
      // to mean "empty cell" makes that decision at adaptation time (see GridSpaceTests).
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
    public void Of_NullString_ReturnsBlank()
    {
      // Was Assert.Same on the blank singleton: CellValue is a value type, so what is asserted is
      // that a null string produces the blank value, which is the whole of what the singleton
      // meant.
      Assert.Equal(CellValue.Blank, CellValue.Of((string?)null));
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
      // The Try twin of GetDate, so a caller reading an optional date need not reach for
      // exceptions.
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
    [InlineData(CellError.Spill)]
    [InlineData(CellError.Calc)]
    [InlineData(CellError.Field)]
    [InlineData(CellError.Blocked)]
    [InlineData(CellError.Connect)]
    [InlineData(CellError.Busy)]
    [InlineData(CellError.External)]
    [InlineData(CellError.Other)]
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
    // The modern errors: dynamic arrays (#SPILL!, #CALC!) and linked data types (the rest). A
    // workbook saved by a current Excel can hold any of them, so an adapter must be able to name
    // them rather than fall back to Other.
    [InlineData(CellError.Spill, "Error(#SPILL!)")]
    [InlineData(CellError.Calc, "Error(#CALC!)")]
    [InlineData(CellError.Field, "Error(#FIELD!)")]
    [InlineData(CellError.Blocked, "Error(#BLOCKED!)")]
    [InlineData(CellError.Connect, "Error(#CONNECT!)")]
    [InlineData(CellError.Busy, "Error(#BUSY!)")]
    [InlineData(CellError.External, "Error(#EXTERNAL!)")]
    public void ToString_SpellsAnErrorTheWayASheetShowsIt(CellError error, string expected)
    {
      Assert.Equal(expected, CellValue.OfError(error).ToString());
    }

    [Fact]
    public void EveryDeclaredErrorHasItsOwnSpelling()
    {
      // Guards the spelling table against a copy-paste: every error renders differently, and every
      // NAMED one renders as the literal a spreadsheet shows. Other is excluded from the second
      // check on purpose — it is the catch-all, so it has no "#..." of its own and renders as
      // itself until a literal is supplied.
      var errors = Enum.GetValues(typeof(CellError)).Cast<CellError>().ToArray();
      var spellings = errors.Select(error => CellValue.OfError(error).ToString()).ToArray();

      Assert.Equal(16, spellings.Length);
      Assert.Equal(spellings.Length, spellings.Distinct().Count());
      Assert.All(
        errors.Where(error => error != CellError.Other).Select(error => CellValue.OfError(error).ToString()),
        spelling => Assert.StartsWith("Error(#", spelling));
      // ...and Other's own rendering is pinned here rather than left to the exclusion above, so the
      // exclusion cannot quietly grow to cover a member that simply lost its spelling.
      Assert.Equal("Error(Other)", CellValue.OfError(CellError.Other).ToString());
    }

    [Fact]
    public void Other_IsTheZeroValueSoAnUnnamedErrorIsTheDefault()
    {
      // Load-bearing, not incidental: an adapter that forgets to set the error must produce "an
      // error we could not name", never #NULL! — an error the sheet does not contain. Reordering
      // the enum would silently reassign every persisted or defaulted value.
      Assert.Equal(0, (int)CellError.Other);
      Assert.Equal(CellError.Other, default(CellError));
      Assert.Equal(CellValue.OfError(CellError.Other), CellValue.OfError(default));
    }

    [Fact]
    public void AnErrorCodeThisLibraryDoesNotDeclare_StillMakesAnErrorCell()
    {
      // The .xls path casts a raw byte to the reader's error enum, so an undefined code is a file
      // this library should still read. Nothing throws; the code is carried as it arrived.
      var value = CellValue.OfError((CellError)99);

      Assert.Equal(CellKind.Error, value.Kind);
      Assert.Equal((CellError)99, value.GetError());
      Assert.Equal("Error(99)", value.ToString());
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

    // --- Error literals -------------------------------------------------------------------------
    //
    // An adapter may fail to NAME an error but may never discard the evidence: the literal the cell
    // arrived as rides along. It is stored only when it says something the error code does not, so
    // the common case — a recognised error spelled the ordinary way — carries no extra state and
    // two adapters that recognise the same error produce equal values.

    [Fact]
    public void OfError_KeepsALiteralOnlyWhenItDiffersFromTheCanonicalSpelling()
    {
      // "#SPILL!" is exactly what Spill already means, so there is nothing to remember.
      Assert.Null(CellValue.OfError(CellError.Spill, "#SPILL!").TryGetErrorText());
      Assert.Null(CellValue.OfError(CellError.Spill).TryGetErrorText());
      Assert.Null(CellValue.OfError(CellError.Other, "Other").TryGetErrorText());

      // LibreOffice's spelling of an error this library cannot name: drop it and the cell becomes
      // an anonymous "something went wrong", which is not what the file said.
      Assert.Equal("Err:501", CellValue.OfError(CellError.Other, "Err:501").TryGetErrorText());
      Assert.Equal("#ERROR!", CellValue.OfError(CellError.Other, "#ERROR!").TryGetErrorText());
    }

    [Fact]
    public void ACanonicalLiteralLeavesTheCellSpelledTheCanonicalWay()
    {
      // The other side of not storing it: nothing about the cell records that a literal was passed
      // at all, so an adapter that echoes the spelling it read produces a value indistinguishable
      // from one that says nothing.
      Assert.Equal("Error(#SPILL!)", CellValue.OfError(CellError.Spill, "#SPILL!").ToString());
      Assert.Equal("Error(Other)", CellValue.OfError(CellError.Other, "Other").ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("   ")]
    public void OfError_TreatsABlankLiteralAsNoLiteralAtAll(string literal)
    {
      // A literal is carried because it says something the error code does not. Whitespace says
      // nothing, and storing it would spell the cell "Error()" — a message that reads like a bug in
      // this library rather than a fact about the sheet. An adapter handing us the empty string it
      // found gets the same cell as one that hands us nothing.
      var value = CellValue.OfError(CellError.Value, literal);

      Assert.Null(value.TryGetErrorText());
      Assert.Equal("Error(#VALUE!)", value.ToString());
      Assert.Equal(CellValue.OfError(CellError.Value), value);
      Assert.Equal(CellValue.OfError(CellError.Value).GetHashCode(), value.GetHashCode());
      Assert.Equal(
        "Cell value is Error (#VALUE!); expected Number.",
        Assert.Throws<InvalidOperationException>(() => value.GetDouble()).Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ABlankLiteralOnAnUnnameableErrorStillLeavesTheErrorNamed(string literal)
    {
      // Other is where a blank literal is most likely to arrive — a reader that recognised an error
      // but exposed no text for it. The cell falls back to naming the only thing it knows.
      var value = CellValue.OfError(CellError.Other, literal);

      Assert.Null(value.TryGetErrorText());
      Assert.Equal(CellError.Other, value.GetError());
      Assert.Equal("Error(Other)", value.ToString());
    }

    [Fact]
    public void OfError_KeepsALiteralThatDiffersFromANamedErrorOnlyInSpelling()
    {
      // Comparison against the canonical spelling is exact, so a differently cased literal is
      // preserved rather than assumed to be the same string. The error is still Value: the adapter
      // named it, and the literal only records how the file wrote it.
      var value = CellValue.OfError(CellError.Value, "#value!");

      Assert.Equal(CellError.Value, value.GetError());
      Assert.Equal("#value!", value.TryGetErrorText());
    }

    [Fact]
    public void TryGetErrorText_OnANonErrorCell_ReturnsNull()
    {
      Assert.Null(CellValue.Of(1).TryGetErrorText());
      Assert.Null(CellValue.Of("#VALUE!").TryGetErrorText());
      Assert.Null(CellValue.Blank.TryGetErrorText());
    }

    [Fact]
    public void ErrorsCarryingDifferentLiterals_AreDifferentValues()
    {
      // Err:501 and #ERROR! are both "an error we could not name", but they are not the same cell:
      // equality that ignored the literal would make the surviving evidence invisible to callers
      // that group or de-duplicate cells.
      var libreOffice = CellValue.OfError(CellError.Other, "Err:501");
      var sheets = CellValue.OfError(CellError.Other, "#ERROR!");

      Assert.NotEqual(libreOffice, sheets);
      Assert.True(libreOffice != sheets);
      Assert.Equal(libreOffice, CellValue.OfError(CellError.Other, "Err:501"));
      Assert.Equal(libreOffice.GetHashCode(), CellValue.OfError(CellError.Other, "Err:501").GetHashCode());
    }

    [Fact]
    public void ACanonicalLiteralLeavesAnErrorEqualToTheBareOne()
    {
      // The other half of "stored only when it differs": one adapter that passes the literal it
      // read and another that passes nothing must agree, or equality would depend on adapter
      // bookkeeping rather than on what the cell says.
      Assert.Equal(CellValue.OfError(CellError.Spill), CellValue.OfError(CellError.Spill, "#SPILL!"));
      Assert.Equal(
        CellValue.OfError(CellError.Spill).GetHashCode(),
        CellValue.OfError(CellError.Spill, "#SPILL!").GetHashCode());
    }

    [Fact]
    public void ToString_ShowsTheLiteralTheCellArrivedAs()
    {
      // Diagnostics are where the preserved literal earns its keep: "Error(Err:501)" names a cell
      // someone can find in the file, where "Error(Other)" would send them looking for nothing.
      Assert.Equal("Error(Err:501)", CellValue.OfError(CellError.Other, "Err:501").ToString());
      Assert.Equal("Error(#value!)", CellValue.OfError(CellError.Value, "#value!").ToString());
    }

    [Fact]
    public void TypedAccessorsOnAnErrorCell_NameTheLiteralItArrivedAs()
    {
      var value = CellValue.OfError(CellError.Other, "Err:501");

      Assert.Equal(
        "Cell value is Error (Err:501); expected Number.",
        Assert.Throws<InvalidOperationException>(() => value.GetDecimal()).Message);
      Assert.Equal(
        "Cell value is Error (Err:501); expected Text.",
        Assert.Throws<InvalidOperationException>(() => value.GetString()).Message);
    }

    [Fact]
    public void ALiteralChangesNothingElseAboutTheCell()
    {
      // The literal is evidence carried alongside the error, not a second reading of the cell: it
      // is not text, does not make the cell blank, and does not change what GetError answers.
      var value = CellValue.OfError(CellError.Other, "Err:501");

      Assert.Equal(CellKind.Error, value.Kind);
      Assert.Equal(CellError.Other, value.GetError());
      Assert.Equal(CellError.Other, value.TryGetError());
      Assert.True(value.HasValue);
      Assert.False(value.IsBlank);
      Assert.Null(value.TryGetString());
    }

    // --- Canonical text -------------------------------------------------------------------------
    //
    // What a cell SAYS, as against what it holds. A text cell says its own string; every other kind
    // is rendered, from the value and invariantly, so the answer does not move with the reader's
    // culture and carries no trace of the format the backend displayed. A blank says nothing at
    // all, which is the equivalence a space contract turns on: AsText is null exactly where the
    // cell is blank.

    [Fact]
    public void AsText_OfATextCell_IsItsOwnString()
    {
      Assert.Equal("Total", CellValue.Of("Total").AsText());
      Assert.Equal("  spaced  ", CellValue.Of("  spaced  ").AsText());
      Assert.Equal("", CellValue.Of("").AsText());
    }

    [Fact]
    public void AsText_OfABlankCell_IsNull()
    {
      Assert.Null(CellValue.Blank.AsText());
      Assert.Null(default(CellValue).AsText());
      Assert.Null(CellValue.Of((string?)null).AsText());
    }

    /// <summary>
    /// The number cells the rendering rule is stated over, named by the CLR type each arrived as —
    /// which is the whole of what decides the answer, so it cannot be inferred from the value.
    /// </summary>
    private static CellValue Number(string arrivedAs) =>
      arrivedAs switch
      {
        "int" => CellValue.Of(42),
        "decimal" => CellValue.Of(42.50m),
        "long" => CellValue.Of(-3L),
        "double" => CellValue.Of(1.5),
        "double-inexact" => CellValue.Of(0.1),
        "double-large" => CellValue.Of(1e20),
        _ => throw new ArgumentOutOfRangeException(nameof(arrivedAs), arrivedAs, "No such number.")
      };

    [Theory]
    [InlineData("int", "42")]
    [InlineData("decimal", "42.50")]
    [InlineData("long", "-3")]
    [InlineData("double", "1.5")]
    [InlineData("double-inexact", "0.1")]
    // A double too big to write out in full renders in exponent form, which is what "R" does and
    // what any reader of this rendering has to be ready for: it is the double's own shortest
    // round-trip spelling, not a decision about how big is big.
    [InlineData("double-large", "1E+20")]
    public void AsText_OfANumber_SaysTheExactValueWhereOneWasKept(string arrivedAs, string said)
    {
      // A number that arrived exact says its exact digits, trailing zeroes and all, because that is
      // what the file wrote; one that arrived as a double says what the double is.
      Assert.Equal(said, Number(arrivedAs).AsText());
    }

    [Fact]
    public void AsText_OfADouble_SaysTheSameDigitsOnEveryTargetFramework()
    {
      // The one rendering that is not the same under both of this package's targets unless it is
      // asked for round-trip: .NET Framework's default is 15 significant digits, which rounds both
      // of these to something shorter and prettier than the double actually is. A declaration
      // matching on a rendered number would find it through one target and not the other.
      //
      // Only the Windows net48 leg can fail this. On Linux the suite builds net8.0 alone, where the
      // default and "R" agree — so a regression here is invisible until somebody runs
      // `dotnet test -f net48`, and this comment is the notice that it must be run.
      Assert.Equal("0.30000000000000004", CellValue.Of(0.1 + 0.2).AsText());
      Assert.Equal("0.3333333333333333", CellValue.Of(1.0 / 3.0).AsText());
    }

    [Fact]
    public void AsText_OfNegativeZero_DiffersByFrameworkAndIsRecordedRatherThanChosen()
    {
      // The one rendering this library does not control and has not decided. IEEE has two zeroes;
      // "R" preserves the sign on .NET Core and drops it on .NET Framework, and no argument from
      // the cell model picks between them — so the pin states both spellings and names which one
      // this run produced, rather than asserting a single answer that would fail on the other leg.
      //
      // What IS decided, and asserted below, is that the two zeroes are the same CELL: equality is
      // over the number, so a declaration can never see one of them and not the other.
      var rendered = CellValue.Of(-0.0).AsText();

      Assert.True(
        rendered == "-0" || rendered == "0",
        $"negative zero rendered as '{rendered}' on {RuntimeInformation.FrameworkDescription}; "
        + "the two known answers are '-0' (.NET Core) and '0' (.NET Framework)");

      Assert.Equal(CellValue.Of(0.0), CellValue.Of(-0.0));
      Assert.Equal(CellValue.Of(0.0).GetHashCode(), CellValue.Of(-0.0).GetHashCode());
    }

    /// <summary>
    /// The cultures a rendering has to survive: one that writes numbers the other way round, and
    /// one whose calendar is not the Gregorian one at all.
    /// </summary>
    public static TheoryData<string> Cultures => new TheoryData<string> { "de-DE", "th-TH" };

    [Theory]
    [MemberData(nameof(Cultures))]
    public void AsText_DoesNotMoveWithTheReadersCulture(string culture)
    {
      // A rendering a matcher compares against has to be the same everywhere, or a declaration
      // written in one office stops anchoring in another. Every kind that renders is swept, not
      // just the numeric ones: th-TH is here because its default calendar is Buddhist, so a date
      // rendered through the ambient culture would say 2569 rather than 2026 — a four-digit
      // difference no number-format test would ever catch.
      var ambient = CultureInfo.CurrentCulture;

      try
      {
        CultureInfo.CurrentCulture = new CultureInfo(culture);

        Assert.Equal("Total", CellValue.Of("Total").AsText());
        Assert.Equal("42.50", CellValue.Of(42.50m).AsText());
        Assert.Equal("1.5", CellValue.Of(1.5).AsText());
        Assert.Equal("2026-03-04", CellValue.Of(new DateTime(2026, 3, 4)).AsText());
        Assert.Equal("2026-03-04T09:30:00", CellValue.Of(new DateTime(2026, 3, 4, 9, 30, 0)).AsText());
        Assert.Equal("TRUE", CellValue.Of(true).AsText());
        Assert.Equal("FALSE", CellValue.Of(false).AsText());
        Assert.Equal("#DIV/0!", CellValue.OfError(CellError.DivisionByZero).AsText());
        Assert.Null(CellValue.Blank.AsText());
      }
      finally
      {
        CultureInfo.CurrentCulture = ambient;
      }
    }

    [Theory]
    [InlineData(0, 0, 0, 0, "2026-03-04")]
    [InlineData(9, 30, 0, 0, "2026-03-04T09:30:00")]
    [InlineData(9, 30, 0, 250, "2026-03-04T09:30:00.25")]
    public void AsText_OfATemporalCell_IsItsIsoForm(int hour, int minute, int second, int milliseconds, string said)
    {
      // A date says its date; a moment within a day says the time too, rather than rendering as the
      // midnight it is not. Sub-second digits appear only where there are some, so the ordinary
      // whole-second case is unchanged by carrying them.
      var moment = new DateTime(2026, 3, 4, hour, minute, second).AddMilliseconds(milliseconds);

      Assert.Equal(said, CellValue.Of(moment).AsText());
    }

    [Theory]
    [InlineData(true, "TRUE")]
    [InlineData(false, "FALSE")]
    public void AsText_OfABooleanCell_IsTheSpreadsheetSpelling(bool value, string said)
      => Assert.Equal(said, CellValue.Of(value).AsText());

    [Theory]
    [InlineData(CellError.DivisionByZero, null, "#DIV/0!")]
    [InlineData(CellError.NotAvailable, null, "#N/A")]
    [InlineData(CellError.Value, "#value!", "#value!")]
    [InlineData(CellError.Other, "Err:501", "Err:501")]
    public void AsText_OfAnErrorCell_IsTheLiteralTheCellShows(CellError error, string? literal, string said)
    {
      // The same spelling Excel puts in the cell — and the literal the adapter kept, where it knew
      // one the error code does not carry.
      Assert.Equal(said, CellValue.OfError(error, literal).AsText());
    }

    [Fact]
    public void AsText_OfAnUnnamedOtherError_SaysOther_WhichIsProvisional()
    {
      // Split out and named for what it is: the one error rendering that is not a spelling any
      // spreadsheet ever put in a cell. CellError.Other exists for the codes this vocabulary does
      // not enumerate, and a cell that arrived without a literal has nothing to show — so "Other"
      // is a placeholder standing where the file's own word would go.
      //
      // It is pinned so a change to it is deliberate rather than incidental, NOT because it is
      // settled: the error vocabulary is decided in phase 7, and this is one of the things it
      // decides. Expect this test to be rewritten there.
      Assert.Equal("Other", CellValue.OfError(CellError.Other).AsText());
    }

    [Fact]
    public void AsText_OfACellWithAValue_IsNeverNull()
    {
      // The empty string, zero and false are all cells with something in them. A rendering that
      // came back null for any of them would make blankness mean "says nothing useful" rather than
      // "is not there".
      var valued = new[]
      {
        CellValue.Of(""),
        CellValue.Of(0),
        CellValue.Of(0.0),
        CellValue.Of(false),
        CellValue.Of(DateTime.MinValue),
        CellValue.OfError(CellError.Null),
      };

      Assert.All(valued, value => Assert.NotNull(value.AsText()));
      Assert.All(valued, value => Assert.True(value.HasValue));
    }
  }
}
