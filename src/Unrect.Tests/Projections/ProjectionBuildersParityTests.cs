using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Spreadsheets.SheetProjectionBuilders<Unrect.Spreadsheets.ISheetCells>;
using static Unrect.Tests.ProjectionTestSpaces;

namespace Unrect.Tests.Projections
{
  /// <summary>
  /// <see cref="ProjectionBuilders{TSpace}"/> — the file-scoped vocabulary, and now the only one.
  /// <para>
  /// This suite used to compare the closed class against a second, open <c>Projection</c> family it
  /// re-exported, member for member. That family is gone: there is one vocabulary, so there is
  /// nothing left to be at parity <em>with</em>, and the twin tables, the completeness covenant and
  /// the leaf-stays-plain boundary went with it. What survives is what was never about the pairing:
  /// </para>
  /// <para>
  /// <b>Name capture, per site.</b> The members that forward a <c>CallerArgumentExpression</c> are
  /// the one defect class a one-line forwarder can hide completely — drop the <c>declared</c>
  /// argument and the inner factory captures the text at the FORWARDING site, which reads like a
  /// correct capture wherever the caller's identifier happens to match the forwarder's parameter
  /// name. No identifier below is called <c>item</c> or <c>eachRow</c>, for exactly that reason.
  /// </para>
  /// <para>
  /// <b>Disjointness.</b> Two <c>using static</c> imports coexist exactly when they share no simple
  /// name, and a declaration file imports the core class beside a backend one. The rule binds every
  /// pair that is ever imported together, and the failure it prevents is reported at a use site in
  /// somebody else's file rather than here.
  /// </para>
  /// <para>
  /// The reflection machinery below is shared with
  /// <see cref="Unrect.Tests.Spreadsheets.SpreadsheetProjectionBuildersParityTests"/>, which states
  /// the backend's own covenant — a backend member that is not re-exported — against the plain
  /// family that still exists on that side.
  /// </para>
  /// </summary>
  public class ProjectionBuildersParityTests
  {
    private static ISheetCells Ledger() => Mixed(new object?[,]
    {
      { "Fund", "Amount", "Units" },
      { "Alpha", 100m, 2 },
      { "Beta", 250m, 4 },
    });

    /// <summary>The same bind pointed at the column of fund names, so every record fails.</summary>
    private static IProjectionDefinition<ISheetCells, decimal> FundColumnAsANumber(LabelMap captions) => Right(captions["Fund"]).Of(Decimal());

    // --- 1. Name capture, one pin per forwarding site ----------------------------------------------

    [Fact]
    public void AVerticalRepeatsItemKeepsTheIdentifierItWasWrittenAs()
    {
      var investorDetail = Decimal();

      var failure = Assert.Throws<ProjectionException>(() => VerticalRepeat(investorDetail).Map(Ledger()));

      Assert.Equal("'investorDetail'", failure.Subject);
      Assert.Equal("VerticalRepeat[0] -> 'investorDetail' (Decimal)", failure.Path);
    }

    [Fact]
    public void AndSoDoesAHorizontalRepeatsItem()
    {
      var quarterlyColumn = Decimal();

      var failure = Assert.Throws<ProjectionException>(() => HorizontalRepeat(quarterlyColumn).Map(Ledger()));

      Assert.Equal("HorizontalRepeat[0] -> 'quarterlyColumn' (Decimal)", failure.Path);
    }

    [Fact]
    public void AndATablesRowSlot()
    {
      var allocationRow = Decimal();

      var failure = Assert.Throws<ProjectionException>(
        () => Table(headerRows: 1, eachRow: allocationRow).Map(Ledger()));

      Assert.Equal("Table[0] -> 'allocationRow' (Decimal)", failure.Path);
    }

    [Fact]
    public void AndATablesBind_LabelledByTheMethodGroupItWasPassedAs()
    {
      var failure = Assert.Throws<ProjectionException>(
        () => Table(headerRows: 1, eachRow: FundColumnAsANumber).Map(Ledger()));

      Assert.Equal("Table[0] -> 'FundColumnAsANumber' (Decimal)", failure.Path);
    }

    [Fact]
    public void AndAnExplicitNameStillOutranksTheIdentifier()
    {
      var investorDetail = Decimal().Named("detail");

      var failure = Assert.Throws<ProjectionException>(() => VerticalRepeat(investorDetail).Map(Ledger()));

      Assert.Equal("VerticalRepeat[0] -> 'detail' (Decimal)", failure.Path);
    }

    // --- 2. Disjointness, against every class a file may import beside this one ----------------------

    [Fact]
    public void TheCoreClassSharesNoSignatureWithEitherBackendClass()
    {
      // The backend rule made CI. Two `using static` directives join one candidate set PER SIMPLE
      // NAME, so the two vocabularies do not collide as wholes: a call is ambiguous only where two
      // candidates of one name fit it equally well. What a backend must never do is republish a core
      // member with the SAME SIGNATURE, which would make that one call ambiguous in every file
      // importing both — and nothing but this pin would catch a re-export that drifted.
      //
      // Until phase 6 this asserted disjointness by NAME, which the vocabulary has since outgrown:
      // `Table` and `Record` are deliberately shared now (see below), and a rule stated over names
      // would have to forbid what the design asks for.
      //
      // Both backend classes, because a file imports one of them: the sheet class where the space
      // carries kinds only, the spreadsheet class where it carries formulas too. (The two backend
      // classes publish the same vocabulary deliberately, and are never imported together — which is
      // why THAT pair is not asserted here.)
      var core = Signatures(typeof(ProjectionBuilders<>));

      Assert.Empty(core.Intersect(Signatures(typeof(SheetProjectionBuilders<>)), StringComparer.Ordinal));
      Assert.Empty(core.Intersect(Signatures(typeof(SpreadsheetProjectionBuilders<>)), StringComparer.Ordinal));
    }

    [Fact]
    public void AndTheOnlyNamesItDoesShareAreTableAndRecord()
    {
      // Non-vacuity for the pin above, and the rule stated positively: exactly two names are shared,
      // and they are shared on purpose. A backend binds a record TYPE where the core's rungs take a
      // header count, a row lambda or a view lambda, and takes this file's captions where the core's
      // `Record` takes a row lambda — so the two vocabularies genuinely extend one member each.
      //
      // Asserting the SET rather than merely "some overlap" is what keeps the signature pin honest:
      // a third shared name appearing would be a design decision, and it would be made here.
      var core = Names(typeof(ProjectionBuilders<>));

      Assert.Equal(
        new[] { "Record", "Table" },
        core.Intersect(Names(typeof(SheetProjectionBuilders<>)), StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal));

      Assert.Equal(
        new[] { "Record", "Table" },
        core.Intersect(Names(typeof(SpreadsheetProjectionBuilders<>)), StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal));
    }

    [Fact]
    public void TheTwoBackendVocabulariesCollideCompletely_AndAreNeverImportedTogether()
    {
      // The one pair the signature rule is deliberately NOT applied to, recorded here because a
      // reader meeting the collision has to be told it is a design and not an oversight.
      //
      // SheetProjectionBuilders and SpreadsheetProjectionBuilders publish the kinded vocabulary under
      // the same names AND the same signatures — the sheet class is the spreadsheet one minus the
      // four members that need a formula. Imported together they would be ambiguous on every shared
      // call. They are never imported together: a file scopes itself to what its declarations READ,
      // and the two classes answer two different questions about one space — does it carry formulas
      // — so no file can want both. Which one a file wants is the same question as which one its
      // space can answer, and its constraint decides for it (ISheetCells versus ISpreadsheetSpace).
      //
      // Asserted rather than described, so that a member drifting out of the sheet class — leaving
      // it a THIRD vocabulary rather than a subset — is a failure here.
      var sheet = Signatures(typeof(SheetProjectionBuilders<>)).ToList();
      var spreadsheet = Signatures(typeof(SpreadsheetProjectionBuilders<>)).ToList();

      Assert.Empty(sheet.Except(spreadsheet, StringComparer.Ordinal));
      Assert.NotEmpty(sheet.Intersect(spreadsheet, StringComparer.Ordinal));

      // And what the larger one adds is exactly the formula-reading half, which is the whole of the
      // difference between the two spaces.
      Assert.All(
        spreadsheet.Except(sheet, StringComparer.Ordinal),
        signature => Assert.Contains("Formula", signature));
    }

    [Theory]
    [InlineData("Table")]
    [InlineData("Record")]
    public void AndTheSharedNamesOverloadSetsAreDisjointBySignature(string shared)
    {
      // The shared names, checked where the sharing can go wrong. Every call site resolves against
      // the union of the two overload sets, so what must hold is that no two members of that union
      // have the same signature — otherwise that one call is ambiguous in every file importing both,
      // and the compiler reports it at the use site rather than here.
      //
      // Non-vacuous by construction: both sides must contribute, or the disjointness is trivially
      // true of a set that is not shared at all.
      var core = Signatures(typeof(ProjectionBuilders<>)).Where(signature => Name(signature) == shared).ToList();
      var sheet = Signatures(typeof(SheetProjectionBuilders<>)).Where(signature => Name(signature) == shared).ToList();
      var spreadsheet = Signatures(typeof(SpreadsheetProjectionBuilders<>)).Where(signature => Name(signature) == shared).ToList();

      Assert.NotEmpty(core);
      Assert.NotEmpty(sheet);
      Assert.NotEmpty(spreadsheet);

      Assert.Empty(core.Intersect(sheet, StringComparer.Ordinal));
      Assert.Empty(core.Intersect(spreadsheet, StringComparer.Ordinal));
    }

    [Fact]
    public void AndTheTwoBackendClassesAreTheSameVocabularyMinusTheFormulaMembers()
    {
      // Non-vacuity for the pin above, and the claim the sheet class's own documentation makes: it
      // is the spreadsheet class without the four members that need a formula. Stated as names, so
      // the pair cannot drift into two different kinded vocabularies while both still avoid the core
      // one's names.
      var sheet = Names(typeof(SheetProjectionBuilders<>));
      var spreadsheet = Names(typeof(SpreadsheetProjectionBuilders<>));

      Assert.Empty(sheet.Except(spreadsheet, StringComparer.Ordinal));
      Assert.Equal(
        new[] { "ColumnWithFormula", "Formula", "RowWithFormula" },
        spreadsheet.Except(sheet, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal));
    }

    // --- Reflection: how a vocabulary is enumerated ---------------------------------------------------

    /// <summary>
    /// Every public static member a declaration could write, rendered as a signature. Property
    /// accessors are dropped and the property itself kept under its own name, because a member
    /// written without parentheses is as much of the vocabulary as a factory is.
    /// </summary>
    internal static IReadOnlyCollection<string> Signatures(Type vocabulary)
    {
      const BindingFlags Declared = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

      var methods = vocabulary
        .GetMethods(Declared)
        .Where(method => !method.IsSpecialName)
        .Select(Signature);

      var properties = vocabulary.GetProperties(Declared).Select(property => property.Name);

      return methods.Concat(properties).ToList();
    }

    private static IReadOnlyCollection<string> Names(Type vocabulary)
      => Signatures(vocabulary).Select(Name).Distinct(StringComparer.Ordinal).ToList();

    private static string Signature(MethodInfo method)
    {
      var arguments = method.IsGenericMethodDefinition
        ? "<" + string.Join(", ", method.GetGenericArguments().Select(argument => argument.Name)) + ">"
        : string.Empty;

      return method.Name
        + arguments
        + "(" + string.Join(", ", method.GetParameters().Select(parameter => Render(parameter.ParameterType))) + ")";
    }

    private static string Name(string signature)
    {
      var end = signature.IndexOfAny(new[] { '<', '(' });

      return end < 0 ? signature : signature.Substring(0, end);
    }

    private static string Render(Type type)
    {
      if (type.IsGenericParameter)
        return type.Name;

      if (type.IsArray)
        return Render(type.GetElementType()!) + "[]";

      if (type.IsGenericType)
      {
        var name = type.Name;
        var tick = name.IndexOf('`');

        return (tick < 0 ? name : name.Substring(0, tick))
          + "<" + string.Join(", ", type.GetGenericArguments().Select(Render)) + ">";
      }

      // The keyword spellings, so a rendered signature reads the way the source does.
      return type == typeof(int) ? "int"
        : type == typeof(string) ? "string"
        : type == typeof(bool) ? "bool"
        : type == typeof(double) ? "double"
        : type == typeof(decimal) ? "decimal"
        : type == typeof(object) ? "object"
        : type.Name;
    }
  }
}
