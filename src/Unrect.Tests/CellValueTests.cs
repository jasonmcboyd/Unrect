using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

using Unrect.Spreadsheets;

using Xunit;

namespace Unrect.Tests
{
  /// <summary>
  /// The value at a cell of a sheet as a sum type: one case at a time, a tag to switch on, one
  /// <c>TryGet</c> per case, and a rendering that is the same everywhere.
  /// </summary>
  public class CellValueTests
  {
    // --- The cases --------------------------------------------------------------------------------

    [Fact]
    public void Of_String_IsText()
    {
      Assert.Equal(CellKind.Text, CellValue.Of("hello").Kind);
    }

    [Fact]
    public void Of_EmptyString_IsTextNotBlank()
    {
      // The value never guesses at blankness: only a null string is absent. An adapter that wants
      // "" to mean "empty cell" decides that at adaptation time.
      Assert.Equal(CellKind.Text, CellValue.Of("").Kind);
    }

    [Fact]
    public void Of_NullString_IsBlank()
    {
      Assert.Equal(CellKind.Blank, CellValue.Of((string?)null).Kind);
      Assert.Equal(CellValue.Blank, CellValue.Of((string?)null));
    }

    [Fact]
    public void Blank_IsTheDefault()
    {
      // What makes a freshly allocated CellValue[,] an already-blank sheet.
      Assert.Equal(CellKind.Blank, CellValue.Blank.Kind);
      Assert.Equal(CellValue.Blank, default(CellValue));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-7)]
    [InlineData(int.MaxValue)]
    public void Of_Int_IsANumber_ThroughTheOneNumericCase(int value)
    {
      // There is no integer case: an integer literal converts to the double on the way in, as it
      // does in a sheet.
      Assert.Equal(CellKind.Number, CellValue.Of(value).Kind);
      Assert.True(CellValue.Of(value).TryGetNumber(out var number));
      Assert.Equal(value, number);
    }

    [Fact]
    public void Of_Double_IsNumber()
    {
      Assert.Equal(CellKind.Number, CellValue.Of(1.5).Kind);
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

    // --- One TryGet per case: true with the payload for its own case, false for every other -------

    [Fact]
    public void TryGetText_HandsBackTheTextForATextValueOnly()
    {
      Assert.True(CellValue.Of("RPT-00042").TryGetText(out var text));
      Assert.Equal("RPT-00042", text);

      Assert.False(CellValue.Of(1).TryGetText(out _));
      Assert.False(CellValue.Blank.TryGetText(out _));
      Assert.False(CellValue.Of(true).TryGetText(out _));
      Assert.False(CellValue.OfError(CellError.Reference).TryGetText(out _));
    }

    [Fact]
    public void TryGetNumber_HandsBackTheDoubleForANumberOnly()
    {
      Assert.True(CellValue.Of(1.5).TryGetNumber(out var number));
      Assert.Equal(1.5, number);

      Assert.False(CellValue.Of("1").TryGetNumber(out _));
      Assert.False(CellValue.Blank.TryGetNumber(out _));
      Assert.False(CellValue.Of(new DateTime(2026, 1, 1)).TryGetNumber(out _));
      Assert.False(CellValue.OfError(CellError.Reference).TryGetNumber(out _));
    }

    [Fact]
    public void TryGetNumber_KeepsEveryDouble()
    {
      // A number is a number whatever its magnitude; only a conversion above the value is lossy.
      Assert.True(CellValue.Of(1e300).TryGetNumber(out var large));
      Assert.Equal(1e300, large);
      Assert.True(CellValue.Of(double.NaN).TryGetNumber(out var nan));
      Assert.True(double.IsNaN(nan));
    }

    [Fact]
    public void TryGetDate_HandsBackTheMomentVerbatimForATemporalValueOnly()
    {
      // The time of day is kept: truncating is the caller's, not the value's.
      var moment = new DateTime(2026, 6, 30, 13, 45, 0);

      Assert.True(CellValue.Of(moment).TryGetDate(out var date));
      Assert.Equal(moment, date);

      Assert.False(CellValue.Of(45000).TryGetDate(out _));
      Assert.False(CellValue.Of("2026-06-30").TryGetDate(out _));
      Assert.False(CellValue.Blank.TryGetDate(out _));
      Assert.False(CellValue.OfError(CellError.Value).TryGetDate(out _));
    }

    [Fact]
    public void TryGetDate_KeepsTheDateTimeKind()
    {
      // The kind is packed into the payload word beside the ticks and must come back out.
      var utc = new DateTime(2026, 6, 30, 13, 45, 0, DateTimeKind.Utc);

      Assert.True(CellValue.Of(utc).TryGetDate(out var date));
      Assert.Equal(DateTimeKind.Utc, date.Kind);
    }

    [Fact]
    public void TryGetBoolean_HandsBackTheFlagForABooleanOnly()
    {
      Assert.True(CellValue.Of(true).TryGetBoolean(out var flag));
      Assert.True(flag);
      Assert.True(CellValue.Of(false).TryGetBoolean(out flag));
      Assert.False(flag);

      Assert.False(CellValue.Of(1).TryGetBoolean(out _));
      Assert.False(CellValue.Of("true").TryGetBoolean(out _));
      Assert.False(CellValue.Blank.TryGetBoolean(out _));
      Assert.False(CellValue.OfError(CellError.Reference).TryGetBoolean(out _));
    }

    [Fact]
    public void TryGetError_HandsBackTheErrorForAnErrorOnly()
    {
      Assert.True(CellValue.OfError(CellError.Name).TryGetError(out var error));
      Assert.Equal(CellError.Name, error);

      Assert.False(CellValue.Of(1).TryGetError(out _));
      Assert.False(CellValue.Of("#VALUE!").TryGetError(out _));
      Assert.False(CellValue.Blank.TryGetError(out _));
    }

    [Fact]
    public void ASwitchOverTheKindIsExhaustive()
    {
      // The reason the tag is public: the cases partition the value, so a switch over Kind can be
      // total, where no switch over a point could be.
      static string Describe(CellValue value) => value.Kind switch
      {
        CellKind.Blank => "nothing",
        CellKind.Text => "words",
        CellKind.Number => "a number",
        CellKind.Temporal => "a moment",
        CellKind.Boolean => "a flag",
        CellKind.Error => "an error",
        _ => throw new ArgumentOutOfRangeException(nameof(value)),
      };

      Assert.Equal(
        new[] { "nothing", "words", "a number", "a moment", "a flag", "an error" },
        new[]
        {
          CellValue.Blank, CellValue.Of("x"), CellValue.Of(1), CellValue.Of(DateTime.MinValue), CellValue.Of(true),
          CellValue.OfError(CellError.Null),
        }.Select(Describe));
    }

    // --- Equality -------------------------------------------------------------------------------

    [Fact]
    public void Equals_OnSamePayload_IsTrue()
    {
      Assert.Equal(CellValue.Of("abc"), CellValue.Of("abc"));
      Assert.Equal(CellValue.Of(1.0), CellValue.Of(1));
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
      Assert.Equal(CellValue.Of(1).GetHashCode(), CellValue.Of(1.0).GetHashCode());
      Assert.Equal(CellValue.Of("abc").GetHashCode(), CellValue.Of("abc").GetHashCode());
      Assert.Equal(CellValue.Blank.GetHashCode(), CellValue.Of((string?)null).GetHashCode());
      Assert.Equal(
        CellValue.Of(new DateTime(2026, 3, 12)).GetHashCode(),
        CellValue.Of(new DateTime(2026, 3, 12)).GetHashCode());
    }

    [Fact]
    public void AValueIsIEquatableSoAComparisonDoesNotBox()
    {
      // A CellValue is 24 bytes and a sheet holds millions of them, so the difference between
      // IEquatable<CellValue> and object.Equals is an allocation per comparison on the hottest path
      // there is. The interface is what EqualityComparer<CellValue>.Default picks up — every
      // Contains, every Distinct, every dictionary keyed on a value — so it is asserted as the
      // contract it is rather than left to whatever the struct happens to expose.
      Assert.True(typeof(IEquatable<CellValue>).IsAssignableFrom(typeof(CellValue)));

      // And the strongly typed path agrees with the boxing one, which is the whole obligation the
      // interface carries: two answers to one question is worse than a slow answer.
      var comparer = EqualityComparer<CellValue>.Default;

      Assert.True(comparer.Equals(CellValue.Of(1), CellValue.Of(1.0)));
      Assert.Equal(CellValue.Of("abc").Equals(CellValue.Of("abc")), CellValue.Of("abc").Equals((object?)CellValue.Of("abc")));
      Assert.Equal(CellValue.Of(1).Equals(CellValue.Of(2)), CellValue.Of(1).Equals((object?)CellValue.Of(2)));
      Assert.Equal(comparer.GetHashCode(CellValue.Of(1)), comparer.GetHashCode(CellValue.Of(1.0)));
    }

    [Fact]
    public void Equals_IsTheOneSpellingComparisonHas()
    {
      // There are no equality operators: a CellValue is a value the library adapts data into and
      // compares, not one a declaration holds and tests — a declaration asks a point what it says,
      // and compares that.
      Assert.True(CellValue.Of(1).Equals(CellValue.Of(1.0)));
      Assert.False(CellValue.Of(1).Equals(CellValue.Of("1")));
      Assert.False(CellValue.Of(1).Equals(CellValue.Of(2)));
      Assert.True(CellValue.Of("a").Equals(CellValue.Of("a")));
    }

    [Fact]
    public void NaNEqualsNaN_SoEqualityIsReflexiveAndHashConsistent()
    {
      Assert.Equal(CellValue.Of(double.NaN), CellValue.Of(double.NaN));
      Assert.Equal(CellValue.Of(double.NaN).GetHashCode(), CellValue.Of(double.NaN).GetHashCode());
    }

    // --- Errors ---------------------------------------------------------------------------------
    //
    // An error is a value a cell genuinely holds — a formula that could not produce a result — and
    // not a missing cell. It is therefore never blank, and must never be skippable as empty space
    // by a strategy looking for the end of a region.

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
    public void OfError_IsAnErrorValue(CellError error)
    {
      var value = CellValue.OfError(error);

      Assert.Equal(CellKind.Error, value.Kind);
      Assert.True(value.TryGetError(out var read));
      Assert.Equal(error, read);
      Assert.NotNull(value.AsText());
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
    public void AnErrorCodeThisLibraryDoesNotDeclare_StillMakesAnErrorValue()
    {
      // The .xls path casts a raw byte to the reader's error enum, so an undefined code is a file
      // this library should still read. Nothing throws; the code is carried as it arrived.
      var value = CellValue.OfError((CellError)99);

      Assert.Equal(CellKind.Error, value.Kind);
      Assert.True(value.TryGetError(out var error));
      Assert.Equal((CellError)99, error);
      Assert.Equal("Error(99)", value.ToString());
    }

    [Fact]
    public void Errors_AreEqualWhenTheyAreTheSameError()
    {
      Assert.Equal(CellValue.OfError(CellError.Value), CellValue.OfError(CellError.Value));
      Assert.NotEqual(CellValue.OfError(CellError.Value), CellValue.OfError(CellError.Name));
      Assert.Equal(
        CellValue.OfError(CellError.NotAvailable).GetHashCode(),
        CellValue.OfError(CellError.NotAvailable).GetHashCode());
    }

    [Fact]
    public void AnErrorIsNotItsOwnSpelling()
    {
      // #VALUE! the error and "#VALUE!" the text are different cases, and neither is empty.
      Assert.NotEqual(CellValue.OfError(CellError.Value), CellValue.Of("#VALUE!"));
      Assert.NotEqual(CellValue.OfError(CellError.Value), CellValue.Blank);
      Assert.NotEqual(CellValue.OfError(CellError.Number), CellValue.Of(0));
    }

    // --- Error literals -------------------------------------------------------------------------
    //
    // An adapter may fail to NAME an error but may never discard the evidence: the literal the cell
    // arrived as rides along, and is what the value SAYS. It is kept only when it says something the
    // error code does not, so the common case — a recognised error spelled the ordinary way — carries
    // no extra state and two adapters that recognise the same error produce equal values.

    [Fact]
    public void OfError_KeepsALiteralOnlyWhenItDiffersFromTheCanonicalSpelling()
    {
      // "#SPILL!" is exactly what Spill already means, so there is nothing to remember, and the
      // value is indistinguishable from one given no literal at all.
      Assert.Equal(CellValue.OfError(CellError.Spill), CellValue.OfError(CellError.Spill, "#SPILL!"));
      Assert.Equal(
        CellValue.OfError(CellError.Spill).GetHashCode(),
        CellValue.OfError(CellError.Spill, "#SPILL!").GetHashCode());
      Assert.Equal("Error(#SPILL!)", CellValue.OfError(CellError.Spill, "#SPILL!").ToString());
      Assert.Equal("Error(Other)", CellValue.OfError(CellError.Other, "Other").ToString());

      // LibreOffice's spelling of an error this library cannot name: drop it and the cell becomes
      // an anonymous "something went wrong", which is not what the file said.
      Assert.Equal("Err:501", CellValue.OfError(CellError.Other, "Err:501").AsText());
      Assert.Equal("#ERROR!", CellValue.OfError(CellError.Other, "#ERROR!").AsText());
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
      // this library rather than a fact about the sheet.
      var value = CellValue.OfError(CellError.Value, literal);

      Assert.Equal("Error(#VALUE!)", value.ToString());
      Assert.Equal("#VALUE!", value.AsText());
      Assert.Equal(CellValue.OfError(CellError.Value), value);
      Assert.Equal(CellValue.OfError(CellError.Value).GetHashCode(), value.GetHashCode());
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ABlankLiteralOnAnUnnameableErrorStillLeavesTheErrorNamed(string literal)
    {
      // Other is where a blank literal is most likely to arrive — a reader that recognised an error
      // but exposed no text for it. The value falls back to naming the only thing it knows.
      var value = CellValue.OfError(CellError.Other, literal);

      Assert.True(value.TryGetError(out var error));
      Assert.Equal(CellError.Other, error);
      Assert.Equal("Error(Other)", value.ToString());
    }

    [Fact]
    public void OfError_KeepsALiteralThatDiffersFromANamedErrorOnlyInSpelling()
    {
      // Comparison against the canonical spelling is exact, so a differently cased literal is
      // preserved rather than assumed to be the same string. The error is still Value: the adapter
      // named it, and the literal only records how the file wrote it.
      var value = CellValue.OfError(CellError.Value, "#value!");

      Assert.True(value.TryGetError(out var error));
      Assert.Equal(CellError.Value, error);
      Assert.Equal("#value!", value.AsText());
      Assert.Equal("Error(#value!)", value.ToString());
    }

    [Fact]
    public void ErrorsCarryingDifferentLiterals_AreDifferentValues()
    {
      // Err:501 and #ERROR! are both "an error we could not name", but they are not the same cell:
      // equality that ignored the literal would make the surviving evidence invisible to callers
      // that group or de-duplicate values.
      var libreOffice = CellValue.OfError(CellError.Other, "Err:501");
      var sheets = CellValue.OfError(CellError.Other, "#ERROR!");

      Assert.NotEqual(libreOffice, sheets);
      Assert.Equal(libreOffice, CellValue.OfError(CellError.Other, "Err:501"));
      Assert.Equal(libreOffice.GetHashCode(), CellValue.OfError(CellError.Other, "Err:501").GetHashCode());
      Assert.Equal("Error(Err:501)", libreOffice.ToString());
    }

    [Fact]
    public void ALiteralChangesNothingElseAboutTheValue()
    {
      // The literal is evidence carried alongside the error, not a second case: it is not text, and
      // it does not change what TryGetError answers.
      var value = CellValue.OfError(CellError.Other, "Err:501");

      Assert.Equal(CellKind.Error, value.Kind);
      Assert.True(value.TryGetError(out var error));
      Assert.Equal(CellError.Other, error);
      Assert.False(value.TryGetText(out _));
    }

    // --- What the value says --------------------------------------------------------------------
    //
    // A text says its own string; every other case is rendered, from the payload and invariantly, so
    // the answer does not move with the reader's culture and carries no trace of the format the
    // backend displayed. A blank says nothing at all, which is the equivalence a space contract
    // turns on: AsText is null exactly where the value is blank.

    [Fact]
    public void AsText_OfAText_IsItsOwnString()
    {
      Assert.Equal("Total", CellValue.Of("Total").AsText());
      Assert.Equal("  spaced  ", CellValue.Of("  spaced  ").AsText());
      Assert.Equal("", CellValue.Of("").AsText());
    }

    [Fact]
    public void AsText_OfABlank_IsNull()
    {
      Assert.Null(CellValue.Blank.AsText());
      Assert.Null(default(CellValue).AsText());
      Assert.Null(CellValue.Of((string?)null).AsText());
    }

    [Theory]
    [InlineData(42, "42")]
    [InlineData(-3, "-3")]
    [InlineData(1.5, "1.5")]
    [InlineData(0.1, "0.1")]
    [InlineData(42.50, "42.5")]
    // A double too big to write out in full renders in exponent form, which is what "R" does and
    // what any reader of this rendering has to be ready for: it is the double's own shortest
    // round-trip spelling, not a decision about how big is big.
    [InlineData(1e20, "1E+20")]
    public void AsText_OfANumber_SaysTheDoubleItHolds(double number, string said)
    {
      // A number is a double and says what the double is. A fixture written as 42.50m does not say
      // "42.50": no workbook holds a scale, and a value that remembered one would render a cell
      // differently from the sheet it came from.
      Assert.Equal(said, CellValue.Of(number).AsText());
    }

    [Fact]
    public void AsText_OfADouble_SaysTheSameDigitsOnEveryTargetFramework()
    {
      // The one rendering the two targets do not agree on by themselves: .NET Framework's default
      // is 15 significant digits and its "R" is 15-then-17, so the second value here — whose
      // shortest exact form has sixteen — would say "0.33333333333333331" there and
      // "0.3333333333333333" on .NET. The rendering finds the shortest digits that read back the
      // same way on both, and these two values are the ones that tell 15, 16 and 17 apart.
      //
      // Only the Windows net48 leg can fail this. On Linux the suite builds net8.0 alone, where the
      // default already is shortest-round-trip — so a regression here is invisible until somebody
      // runs `dotnet test -f net48`, and this comment is the notice that it must be run.
      Assert.Equal("0.30000000000000004", CellValue.Of(0.1 + 0.2).AsText());
      Assert.Equal("0.3333333333333333", CellValue.Of(1.0 / 3.0).AsText());
      Assert.Equal("0.1", CellValue.Of(0.1).AsText());
    }

    [Fact]
    public void AsText_OfNegativeZero_DiffersByFrameworkAndIsRecordedRatherThanChosen()
    {
      // The one rendering this library does not control and has not decided. IEEE has two zeroes;
      // "R" preserves the sign on .NET Core and drops it on .NET Framework, and no argument from
      // the value model picks between them — so the pin states both spellings and names which one
      // this run produced, rather than asserting a single answer that would fail on the other leg.
      //
      // What IS decided, and asserted below, is that the two zeroes are the same VALUE: equality is
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
      // written in one office stops anchoring in another. Every case that renders is swept, not
      // just the numeric one: th-TH is here because its default calendar is Buddhist, so a date
      // rendered through the ambient culture would say 2569 rather than 2026 — a four-digit
      // difference no number-format test would ever catch.
      var ambient = CultureInfo.CurrentCulture;

      try
      {
        CultureInfo.CurrentCulture = new CultureInfo(culture);

        Assert.Equal("Total", CellValue.Of("Total").AsText());
        Assert.Equal("42.5", CellValue.Of(42.5).AsText());
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
    public void AsText_OfATemporalValue_IsItsIsoForm(int hour, int minute, int second, int milliseconds, string said)
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
    public void AsText_OfABoolean_IsTheSpreadsheetSpelling(bool value, string said)
      => Assert.Equal(said, CellValue.Of(value).AsText());

    [Theory]
    [InlineData(CellError.DivisionByZero, null, "#DIV/0!")]
    [InlineData(CellError.NotAvailable, null, "#N/A")]
    [InlineData(CellError.Value, "#value!", "#value!")]
    [InlineData(CellError.Other, "Err:501", "Err:501")]
    public void AsText_OfAnError_IsTheLiteralTheCellShows(CellError error, string? literal, string said)
    {
      // The same spelling Excel puts in the cell — and the literal the adapter kept, where it knew
      // one the error code does not carry.
      Assert.Equal(said, CellValue.OfError(error, literal).AsText());
    }

    [Fact]
    public void AsText_OfAnUnnamedOtherError_SaysOther_WhichIsProvisional()
    {
      // The one error rendering that is not a spelling any spreadsheet ever put in a cell:
      // CellError.Other exists for the codes this vocabulary does not enumerate, and a value that
      // arrived without a literal has nothing to show — so "Other" is a placeholder standing where
      // the file's own word would go. Pinned so a change to it is deliberate, not because it is
      // settled.
      Assert.Equal("Other", CellValue.OfError(CellError.Other).AsText());
    }

    [Fact]
    public void AsText_OfAnythingButBlank_IsNeverNull()
    {
      // The empty string, zero and false are all values. A rendering that came back null for any of
      // them would make blankness mean "says nothing useful" rather than "is not there".
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
      Assert.All(valued, value => Assert.NotEqual(CellKind.Blank, value.Kind));
    }
  }
}
