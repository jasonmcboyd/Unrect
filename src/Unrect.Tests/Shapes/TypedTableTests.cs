using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Unrect.Core;
using Unrect.Shapes;

using Xunit;

using static Unrect.Shapes.Shape;
using static Unrect.Tests.ShapeTestSpaces;

namespace Unrect.Tests.Shapes
{
  /// <summary>
  /// <c>TableRows&lt;T&gt;()</c>: a table read into a type, with the captions bound to the members
  /// by name. The binding matrix below is the spine of the feature — what binds freely, what needs
  /// declaring, what is rejected, and where each failure is reported.
  /// <para>
  /// Everything reflective resolves when the shape is built, so a type that cannot be bound is an
  /// error at the declaration rather than once per file. Everything about the <em>data</em> is
  /// reported at map time, per cell, with the column caption and the cell's address.
  /// </para>
  /// </summary>
  public class TypedTableTests
  {
    // --- The types under test ------------------------------------------------------------------------

    public record Txn(string InvestorName, DateTime TransactionDate, decimal Amount);

    public record Wide(string Client, DateTime Date, string Type, decimal Amount);

    public record Optionals(string Client, decimal? Amount);

    // Declared in this file, which is #nullable enable: string? tolerates a blank, string does not.
    // The two are deliberately in the same type so the annotation is the only difference between
    // them — the pin for the fragility note about how nullability is read.
    public record Annotated(string? Note, string Client);

    public record Kinds(CellValue Any, string Client);

    public record Longy(string Client, long Quantity);

    public record Floaty(string Client, float Ratio);

    public record Custom(string Client, Uri Link);

    public class Settable
    {
      public string Client { get; set; } = string.Empty;

      public decimal Amount { get; set; }
    }

    public record Inits
    {
      public string Client { get; init; } = string.Empty;

      public decimal Amount { get; init; }
    }

    public record Every(
      string Text,
      decimal Money,
      double Ratio,
      int Count,
      DateTime When,
      bool Flag,
      CellValue Raw);

    // Four nullable strings and one that is not: enough of a majority that the compiler stops
    // annotating each parameter and puts a NullableContext(2) on the constructor instead. Reading
    // only the per-parameter annotations would make every one of these look non-nullable.
    public record MostlyNullable(string? A, string? B, string? C, string? D, string Client);

    public record ExtraInit(string Client)
    {
      public decimal Extra { get; init; }
    }

    public class Nothing
    {
    }

    public class NoConstructor
    {
      private NoConstructor()
      {
      }

      public string Client { get; set; } = string.Empty;
    }

    public class ManyConstructors
    {
      public ManyConstructors(string client) => Client = client;

      public ManyConstructors(string client, decimal amount)
      {
        Client = client;
        Amount = amount;
      }

      public string Client { get; set; } = string.Empty;

      public decimal Amount { get; set; }
    }

#nullable disable

    // Declared where nullability was never spoken: the compiler emits no annotation at all, and
    // "no annotation" must not be read as "nullable".
    public record Oblivious(string Client, string Note);

#nullable restore

    // --- Grids -----------------------------------------------------------------------------------------

    private static ISpace Free() => Mixed(new object?[,]
    {
      { "Investor Name", "Transaction Date", "Amount" },
      { "Acme", new DateTime(2026, 3, 4), 10m },
      { "Beta", new DateTime(2026, 5, 1), 20m },
    });

    private static ISpace Captioned() => Mixed(new object?[,]
    {
      { "Client", "Transaction Date", "Transaction Type", "Amount" },
      { "Acme", new DateTime(2026, 3, 4), "Capital Call", 10m },
    });

    // --- Binding free ------------------------------------------------------------------------------------

    [Fact]
    public void MembersBindToCaptionsThroughTheComparer()
    {
      // "Investor Name" ↔ InvestorName, "Transaction Date" ↔ TransactionDate: the same name written
      // for two audiences, and neither had to say so.
      var rows = TableRows<Txn>().Map(Free());

      Assert.Equal(2, rows.Count);
      Assert.Equal("Acme", rows[0].InvestorName);
      Assert.Equal(new DateTime(2026, 3, 4), rows[0].TransactionDate);
      Assert.Equal(10m, rows[0].Amount);
      Assert.Equal(20m, rows[1].Amount);
    }

    [Fact]
    public void EverySupportedTypeIsRead()
    {
      var space = Mixed(new object?[,]
      {
        { "Text", "Money", "Ratio", "Count", "When", "Flag", "Raw" },
        { "hello", 1.5m, 0.25, 42, new DateTime(2026, 6, 30), true, "anything" },
      });

      var row = TableRows<Every>().Map(space).Single();

      Assert.Equal("hello", row.Text);
      Assert.Equal(1.5m, row.Money);
      Assert.Equal(0.25, row.Ratio);
      Assert.Equal(42, row.Count);
      Assert.Equal(new DateTime(2026, 6, 30), row.When);
      Assert.True(row.Flag);
      Assert.Equal(CellKind.Text, row.Raw.Kind);
    }

    [Fact]
    public void ACellValueMemberIsKindAgnostic()
    {
      // The escape hatch for a column that is not one kind: text in one row, a number in the next,
      // and the member takes both without a word of declaration.
      var space = Mixed(new object?[,]
      {
        { "Any", "Client" },
        { "n/a", "Acme" },
        { 5m, "Beta" },
      });

      var rows = TableRows<Kinds>().Map(space);

      Assert.Equal(CellKind.Text, rows[0].Any.Kind);
      Assert.Equal(CellKind.Number, rows[1].Any.Kind);
    }

    // --- Construction shapes -------------------------------------------------------------------------------

    [Fact]
    public void APositionalRecordIsBuiltThroughItsConstructor()
    {
      Assert.Equal("Acme", TableRows<Txn>().Map(Free())[0].InvestorName);
    }

    [Fact]
    public void APlainClassIsBuiltThroughItsSetters()
    {
      var space = Mixed(new object?[,] { { "Client", "Amount" }, { "Acme", 1m } });

      var row = TableRows<Settable>().Map(space).Single();

      Assert.Equal("Acme", row.Client);
      Assert.Equal(1m, row.Amount);
    }

    [Fact]
    public void InitOnlyPropertiesAreBound()
    {
      // The mechanical claim: init accessors are settable through the property path, so a record
      // written the modern way needs no constructor.
      var space = Mixed(new object?[,] { { "Client", "Amount" }, { "Acme", 1m } });

      var row = TableRows<Inits>().Map(space).Single();

      Assert.Equal("Acme", row.Client);
      Assert.Equal(1m, row.Amount);
    }

    // --- Overrides ------------------------------------------------------------------------------------------

    [Fact]
    public void AColumnOverrideNamesTheCaptionTheComparerWouldNotHaveFound()
    {
      var row = TableRows<Wide>(bind => bind
        .Column(t => t.Date, "Transaction Date")
        .Column(t => t.Type, "Transaction Type"))
        .Map(Captioned())
        .Single();

      Assert.Equal("Acme", row.Client);
      Assert.Equal(new DateTime(2026, 3, 4), row.Date);
      Assert.Equal("Capital Call", row.Type);
      Assert.Equal(10m, row.Amount);
    }

    [Fact]
    public void AnOverrideStillGoesThroughTheComparer()
    {
      // The declaration says "Transaction Date"; the sheet says "Transaction  Date". An override
      // names a caption, and captions are still matched the way captions are.
      var space = Mixed(new object?[,]
      {
        { "Client", "Transaction  Date", "Transaction Type", "Amount" },
        { "Acme", new DateTime(2026, 3, 4), "Capital Call", 10m },
      });

      var row = TableRows<Wide>(bind => bind
        .Column(t => t.Date, "Transaction Date")
        .Column(t => t.Type, "Transaction Type"))
        .Map(space)
        .Single();

      Assert.Equal(new DateTime(2026, 3, 4), row.Date);
    }

    [Fact]
    public void IgnoreLeavesAPropertyAtItsDefault()
    {
      var space = Mixed(new object?[,] { { "Client" }, { "Acme" } });

      var row = TableRows<Settable>(bind => bind.Ignore(t => t.Amount)).Map(space).Single();

      Assert.Equal("Acme", row.Client);
      Assert.Equal(0m, row.Amount);
    }

    [Fact]
    public void IgnoreUsesAConstructorParametersDefault()
    {
      var space = Mixed(new object?[,] { { "Client" }, { "Acme" } });

      var row = TableRows<Defaulted>(bind => bind.Ignore(t => t.Amount)).Map(space).Single();

      Assert.Equal("Acme", row.Client);
      Assert.Equal(99m, row.Amount);
    }

    public record Defaulted(string Client, decimal Amount = 99m);

    // --- Nullability ------------------------------------------------------------------------------------------

    [Fact]
    public void ANullableValueTypeToleratesABlank()
    {
      var space = Mixed(new object?[,] { { "Client", "Amount" }, { "Acme", null } });

      Assert.Null(TableRows<Optionals>().Map(space).Single().Amount);
    }

    [Fact]
    public void ANullableValueTypeIsStillNotKindTolerant()
    {
      // Blank-tolerant, not kind-tolerant: a null says "the sheet left this out", and text in a
      // decimal column says something else entirely.
      var space = Mixed(new object?[,] { { "Client", "Amount" }, { "Acme", "x" } });

      var failure = Assert.Throws<ShapeException>(() => TableRows<Optionals>().Map(space));

      Assert.Contains("column 'Amount': expected Number at B2, found Text", failure.Message);
    }

    [Fact]
    public void ANonNullableValueTypeRejectsABlank()
    {
      var space = Mixed(new object?[,]
      {
        { "Investor Name", "Transaction Date", "Amount" },
        { "Acme", new DateTime(2026, 1, 1), null },
      });

      var failure = Assert.Throws<ShapeException>(() => TableRows<Txn>().Map(space));

      Assert.Contains("column 'Amount': expected Number at C2, found Blank", failure.Message);
    }

    [Fact]
    public void AnAnnotatedStringToleratesABlankWhileAPlainOneDoesNot()
    {
      // Both members are strings on the same record; the only difference is the annotation, which
      // is exactly what the reader of a #nullable enable file expects to be load-bearing.
      var tolerated = TableRows<Annotated>().Map(Mixed(new object?[,]
      {
        { "Note", "Client" },
        { null, "Acme" },
      }));

      Assert.Null(tolerated.Single().Note);
      Assert.Equal("Acme", tolerated.Single().Client);

      var failure = Assert.Throws<ShapeException>(() => TableRows<Annotated>().Map(Mixed(new object?[,]
      {
        { "Note", "Client" },
        { "a note", null },
      })));

      Assert.Contains("column 'Client': expected Text at B2, found Blank", failure.Message);
    }

    [Fact]
    public void AMostlyNullableConstructorIsStillReadPerParameter()
    {
      // The compiler compresses a mostly-nullable parameter list into one NullableContext on the
      // constructor rather than annotating each parameter. A reader that only looked at the
      // per-parameter annotations would find none and call all five non-nullable — which is
      // exactly backwards for four of them.
      var rows = TableRows<MostlyNullable>().Map(Mixed(new object?[,]
      {
        { "A", "B", "C", "D", "Client" },
        { null, null, null, null, "Acme" },
      }));

      var row = rows.Single();

      Assert.Null(row.A);
      Assert.Null(row.D);
      Assert.Equal("Acme", row.Client);
    }

    [Fact]
    public void TheOneNonNullableParameterInAMostlyNullableListIsStillStrict()
    {
      // The other half: the context says "nullable" for the group, and the one parameter that
      // opts out of it is still refused a blank.
      var failure = Assert.Throws<ShapeException>(() => TableRows<MostlyNullable>().Map(Mixed(new object?[,]
      {
        { "A", "B", "C", "D", "Client" },
        { "x", "x", "x", "x", null },
      })));

      Assert.Contains("column 'Client': expected Text at E2, found Blank", failure.Message);
    }

    [Fact]
    public void AnObliviousTypeIsStrict()
    {
      // Where nullability was never spoken, silence is not consent: an unannotated string is read
      // strictly, so a #nullable disable file does not quietly gain blank-tolerance everywhere.
      var failure = Assert.Throws<ShapeException>(() => TableRows<Oblivious>().Map(Mixed(new object?[,]
      {
        { "Client", "Note" },
        { "Acme", null },
      })));

      Assert.Contains("column 'Note': expected Text at B2, found Blank", failure.Message);
    }

    // --- Construction-time refusals -------------------------------------------------------------------------------

    [Fact]
    public void AnUnsupportedMemberTypeIsRefusedAtTheDeclaration()
    {
      var failure = Assert.Throws<ArgumentException>(() => TableRows<Longy>());

      Assert.Contains("Longy.Quantity is a long, and no cell accessor yields long.", failure.Message);
      Assert.Contains(
        "Supported: string, decimal, double, int, DateTime, bool, CellValue, and the nullable forms.",
        failure.Message);
      Assert.Contains("Read it as int or decimal and convert in Select.", failure.Message);
    }

    [Fact]
    public void EveryUnsupportedTypeIsRefusedTheSameWay()
    {
      Assert.Contains("Floaty.Ratio is a float", Assert.Throws<ArgumentException>(() => TableRows<Floaty>()).Message);
      Assert.Contains("Custom.Link is a Uri", Assert.Throws<ArgumentException>(() => TableRows<Custom>()).Message);
    }

    [Fact]
    public void ATypeThatCannotBeConstructedIsRefusedAtTheDeclaration()
    {
      Assert.Contains(
        "NoConstructor cannot be constructed: it has no public constructor.",
        Assert.Throws<ArgumentException>(() => TableRows<NoConstructor>()).Message);

      Assert.Contains(
        "ManyConstructors cannot be constructed: it has 2 public constructors and no parameterless one.",
        Assert.Throws<ArgumentException>(() => TableRows<ManyConstructors>()).Message);
    }

    [Fact]
    public void ABadBindingIsRefusedAtTheDeclaration()
    {
      Assert.Contains(
        "does not select a property of Wide; select a property directly.",
        Assert.Throws<ArgumentException>(() => TableRows<Wide>(bind => bind.Column(t => t.Date.Year.ToString(), "X"))).Message);

      Assert.Contains(
        "Wide.Date is bound twice.",
        Assert.Throws<ArgumentException>(() => TableRows<Wide>(bind => bind.Column(t => t.Date, "A").Column(t => t.Date, "B"))).Message);

      Assert.Contains(
        "Wide.Date is both bound and ignored.",
        Assert.Throws<ArgumentException>(() => TableRows<Wide>(bind => bind.Column(t => t.Date, "A").Ignore(t => t.Date))).Message);

      Assert.Contains(
        "A column caption cannot be empty or whitespace.",
        Assert.Throws<ArgumentException>(() => TableRows<Wide>(bind => bind.Column(t => t.Date, "   "))).Message);
    }

    [Fact]
    public void IgnoringAConstructorParameterWithNoDefaultIsRefused()
    {
      Assert.Contains(
        "Wide.Type cannot be ignored: the constructor parameter has no default value.",
        Assert.Throws<ArgumentException>(() => TableRows<Wide>(bind => bind.Ignore(t => t.Type))).Message);
    }

    [Fact]
    public void ATypeWithNothingToBindIsRefusedAtTheDeclaration()
    {
      Assert.Contains(
        "Nothing has no properties to bind.",
        Assert.Throws<ArgumentException>(() => TableRows<Nothing>()).Message);
    }

    [Fact]
    public void AFailedIgnoreSaysIgnoreRatherThanColumn()
    {
      // The guidance has to be the call the user actually wrote, or it reads as a suggestion to
      // fix a line that is not there.
      var failure = Assert.Throws<ArgumentException>(() => TableRows<Wide>(bind => bind.Ignore(t => t.Date.Year)));

      Assert.Contains("Ignore(t => t.Date.Year) does not select a property of Wide", failure.Message);
      Assert.DoesNotContain("Column(", failure.Message);
    }

    [Fact]
    public void APropertyThatIsNotAConstructorParameterCannotBeDeclared()
    {
      // A positional record's extra init property is filled by nobody: the type is built through
      // its constructor, and the constructor has never heard of it. Binding or ignoring it would
      // be a declaration with no effect, so it is refused instead.
      foreach (var declaration in new Func<IShape<IReadOnlyList<ExtraInit>>>[]
      {
        () => TableRows<ExtraInit>(bind => bind.Column(t => t.Extra, "Extra")),
        () => TableRows<ExtraInit>(bind => bind.Ignore(t => t.Extra)),
      })
      {
        var failure = Assert.Throws<ArgumentException>(() => declaration());

        Assert.Contains(
          "ExtraInit.Extra is not a constructor parameter, so it cannot be bound or ignored; "
          + "ExtraInit is built through its constructor, which fills only its parameters.",
          failure.Message);
      }
    }

    [Fact]
    public void AnUnsupportedNullableTypeIsNamedWithItsQuestionMark()
    {
      // "TimeSpan" would send the reader looking for a TimeSpan member they do not have.
      var failure = Assert.Throws<ArgumentException>(() => TableRows<Spanny>());

      Assert.Contains("Spanny.Span is a TimeSpan?, and no cell accessor yields TimeSpan?.", failure.Message);
    }

    public record Spanny(string Client, TimeSpan? Span);

    // --- Map-time strictness ---------------------------------------------------------------------------------------

    [Fact]
    public void EveryUnboundMemberIsListedInOneFailure()
    {
      // One failure naming all of them, with the captions that were available — so the reader fixes
      // the declaration once rather than discovering the members one run at a time.
      var failure = Assert.Throws<ShapeException>(() => TableRows<Wide>().Map(Captioned()));

      Assert.Contains("no column binds Wide.Date or Wide.Type;", failure.Message);
      Assert.Contains(
        "the table's captions are 'Client', 'Transaction Date', 'Transaction Type', 'Amount'.",
        failure.Message);
      // The guidance names a member that actually needs binding, spelled as the property is — so
      // it can be pasted into the declaration rather than retyped.
      Assert.Contains("Bind one with Column(t => t.Date, \"…\") or drop it with Ignore(t => t.Date)", failure.Message);
    }

    [Fact]
    public void AnExtraCaptionNoMemberClaimsIsNotAFailure()
    {
      // Real reports carry columns a consumer does not want; binding is strict in one direction only.
      var space = Mixed(new object?[,]
      {
        { "Investor Name", "Transaction Date", "Amount", "Notes" },
        { "Acme", new DateTime(2026, 3, 4), 10m, "ignore me" },
      });

      Assert.Equal("Acme", TableRows<Txn>().Map(space).Single().InvestorName);
    }

    [Fact]
    public void AMemberMatchingTwoColumnsIsALoudFailure()
    {
      var space = Mixed(new object?[,]
      {
        { "Investor Name", "Transaction Date", "Amount", "amount" },
        { "Acme", new DateTime(2026, 1, 1), 1m, 2m },
      });

      var failure = Assert.Throws<ShapeException>(() => TableRows<Txn>().Map(space));

      Assert.Contains(
        "Txn.Amount matches the columns at C1 ('Amount') and D1 ('amount'); "
        + "captions are matched ignoring case and whitespace",
        failure.Message);
    }

    [Fact]
    public void APerCellFailureCarriesTheColumnAndTheCellsAddress()
    {
      var kindFailure = Assert.Throws<ShapeException>(() => TableRows<Txn>().Map(Mixed(new object?[,]
      {
        { "Investor Name", "Transaction Date", "Amount" },
        { "Acme", new DateTime(2026, 1, 1), "x" },
      })));

      Assert.Contains("column 'Amount': expected Number at C2, found Text", kindFailure.Message);

      var conversionFailure = Assert.Throws<ShapeException>(() => TableRows<Counted>().Map(Mixed(new object?[,]
      {
        { "Client", "Count" },
        { "Acme", 1.5 },
      })));

      Assert.Contains("column 'Count': the Number at B2 (1.5) is not a whole number", conversionFailure.Message);
    }

    public record Counted(string Client, int Count);

    // --- Immutability and reuse ------------------------------------------------------------------------------------

    [Fact]
    public void ABindingMethodReturnsANewBindingAndLeavesTheOriginalAlone()
    {
      // The lambda hands out a builder, and a builder that mutated in place would make the order of
      // the calls matter in ways nobody wrote down.
      TableBinding<Wide>? captured = null;

      var shape = TableRows<Wide>(bind =>
      {
        captured = bind;
        return bind.Column(t => t.Date, "Transaction Date").Column(t => t.Type, "Transaction Type");
      });

      Assert.NotNull(captured);
      Assert.Equal("Acme", shape.Map(Captioned()).Single().Client);

      // The builder the lambda was handed never learned about those columns.
      Assert.Throws<ShapeException>(() => TableRows<Wide>(_ => captured!).Map(Captioned()));
    }

    [Fact]
    public void OneTypedTableIsSafeToMapFromManyThreads()
    {
      var shape = TableRows<Txn>();

      var spaces = Enumerable.Range(0, 32)
        .Select(seed => Mixed(new object?[,]
        {
          { "Investor Name", "Transaction Date", "Amount" },
          { $"Investor {seed}", new DateTime(2026, 1, 1), (decimal)seed },
        }))
        .ToArray();

      var results = new string[spaces.Length];

      Parallel.For(0, spaces.Length, index =>
      {
        var row = shape.Map(spaces[index]).Single();
        results[index] = $"{row.InvestorName}:{row.Amount}";
      });

      for (var index = 0; index < spaces.Length; index++)
        Assert.Equal($"Investor {index}:{index}", results[index]);
    }

    [Fact]
    public void MappingTwiceGivesTheSameAnswer()
    {
      // Binding resolves once, when the shape is built; applying it must not disturb it.
      var shape = TableRows<Txn>();

      Assert.Equal(shape.Map(Free())[0].InvestorName, shape.Map(Free())[0].InvestorName);
      Assert.Equal(2, shape.Map(Free()).Count);
    }

    // --- The typed form changed the projection and nothing else -----------------------------------------------------

    [Fact]
    public void TheTypedAndProjectingSpellingsLandInTheSamePlace()
    {
      var space = Mixed(new object?[,]
      {
        { null, null, null },
        { "Investor Name", "Transaction Date", "Amount" },
        { "Acme", new DateTime(2026, 3, 4), 10m },
        { "Beta", new DateTime(2026, 5, 1), 20m },
      });

      var typed = TableRows<Txn>().Apply(space);
      var projected = TableRows(r => r["Amount"].GetDecimal()).Apply(space);

      Assert.Equal(projected.Value, typed.Value.Select(row => row.Amount).ToArray());
      Assert.Equal(projected.Offset.Size.Height, typed.Offset.Size.Height);
      Assert.Equal(projected.Consumed.Width, typed.Consumed.Width);
      Assert.Equal(projected.Consumed.Height, typed.Consumed.Height);
    }

    [Fact]
    public void AllFiveTableRowsSpellingsResolve()
    {
      // A compile-time pin against the overload set becoming ambiguous.
      var space = Mixed(new object?[,] { { "Client", "Amount" }, { "Acme", 1m } });

      Assert.Single(TableRows(r => r["Client"].GetString()).Map(space));
      Assert.Equal(2, TableRows(0, r => r[0]).Map(space).Count);   // no header declared, so the caption row is data
      Assert.Single(TableRows().Map(space));
      Assert.Single(TableRows<Settable>().Map(space));
      Assert.Single(TableRows<Settable>(bind => bind.Column(t => t.Amount, "Amount")).Map(space));
    }
  }
}
