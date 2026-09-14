// SPIKE, scenario 6 continued — the refusals ledger for ENTRY C, in its own file because every
// entry below needs its OWN import block and file-level usings would leak across them.
//
// TWO BUILDS, and this is a mechanic worth knowing before reading the messages:
//
//   dotnet build -p:DefineConstants=MUST_NOT_COMPILE        collects (p) (q) (t) (u)  — body-level
//   dotnet build -p:DefineConstants=MUST_NOT_COMPILE_DECL   collects (r) (s)          — declaration-level
//
// They cannot be collected together. A DECLARATION error (CS0102, CS1106) stops Roslyn before it
// binds any method body ANYWHERE IN THE COMPILATION — measured, not assumed: with (r) and (s)
// present, every CS0121 below vanishes from the build output and the ledger silently loses four
// entries. So the two classes of refusal are ledgered under two constants.
//
// Messages below are VERBATIM from those builds (2026-09-10, .NET SDK 8 / Roslyn).

#if MUST_NOT_COMPILE

namespace PlacementGauntlet.EntryC.TwoClosedImports
{
  using static PlacementGauntlet.Staged.ProjectionBuilders<Unrect.Core.ICellValues>;
  using static PlacementGauntlet.Staged.ProjectionBuilders<Unrect.Spreadsheets.ISpreadsheetSpace>;

  /// <summary>
  /// (p) TWO CLOSED IMPORTS IN ONE FILE — boundary (a), and the ratified dichotomy theorem's
  /// enforcement. It is refused, as §5.5 predicted; what §5.5 got wrong is the mechanism, and the
  /// difference matters.
  /// <para>
  /// <b>It is CS0121, not CS0104, and it fires PER CALL rather than per file.</b> CS0104 is for
  /// ambiguous TYPE names; the members of a closed static class are methods, so the two imports
  /// merge into one method group and the ambiguity surfaces only where a shared name is actually
  /// invoked. A file that imports two scopes and then names nothing they share compiles clean. The
  /// file is the scope, but the compiler enforces it one call at a time.
  /// </para>
  /// <para>
  /// <b>And the message cannot be read.</b> Roslyn renders both candidates with the type parameter's
  /// NAME rather than the closed argument, so the two alternatives print identically — the reader is
  /// told the call is ambiguous between a method and itself. This is the arm's sharpest negative
  /// finding: the refusal is real, the diagnostic is unusable, and unlike (h) and (l) there is no
  /// <c>[Obsolete(error)]</c> stub to reach for, because nothing is wrong with either member.
  /// </para>
  /// </summary>
  public static class P
  {
    // CS0121: The call is ambiguous between the following methods or properties:
    //         'ProjectionBuilders<TSpace>.Text()' and 'ProjectionBuilders<TSpace>.Text()'
    public static object Leaf() => Text();

    // CS0121: The call is ambiguous between the following methods or properties:
    //         'ProjectionBuilders<TSpace>.Table<T>()' and 'ProjectionBuilders<TSpace>.Table<T>()'
    public static object Rung() => Table<Line>();

    // CS0121: The call is ambiguous between the following methods or properties:
    //         'ProjectionBuilders<TSpace>.RowContaining(string)' and 'ProjectionBuilders<TSpace>.RowContaining(string)'
    public static object Matcher() => RowContaining("Total");
  }
}

namespace PlacementGauntlet.EntryC.WithTheOpenVocabulary
{
  using static Unrect.Projections.Projection;
  using static PlacementGauntlet.Staged.ProjectionBuilders<Unrect.Core.ICellValues>;

  /// <summary>
  /// (q) ENTRY C AND <c>using static Projection</c> TOGETHER — boundary (c), the completeness
  /// obligation, enforced. Every name the closed class re-exports collides with the one it forwards
  /// to, which is why <see cref="PlacementGauntlet.Staged.ProjectionBuilders{TSpace}"/> has to carry
  /// the WHOLE vocabulary rather than the interesting part of it.
  /// <para>
  /// This message is readable where (p)'s is not: the two candidates come from differently NAMED
  /// types, so the compiler's own rendering distinguishes them. The asymmetry is not the library's
  /// doing and cannot be fixed by it.
  /// </para>
  /// <para>
  /// Note what still works, and is the deliberately-effortful fallback the theorem asks for: full
  /// qualification. <c>Unrect.Projections.Projection.Text()</c> in an Entry C file compiles.
  /// </para>
  /// </summary>
  public static class Q
  {
    // CS0121: The call is ambiguous between the following methods or properties:
    //         'Unrect.Projections.Projection.Text()' and 'PlacementGauntlet.Staged.ProjectionBuilders<TSpace>.Text()'
    public static object Leaf() => Text();

    // CS0121: The call is ambiguous between the following methods or properties:
    //         'Unrect.Projections.Projection.Table<T>()' and 'PlacementGauntlet.Staged.ProjectionBuilders<TSpace>.Table<T>()'
    public static object Rung() => Table<Line>();

    // CS0121: The call is ambiguous between the following methods or properties:
    //         'Unrect.Projections.Projection.VerticalFlow<T>(Unrect.Projections.Layout<T>)' and
    //         'PlacementGauntlet.Staged.ProjectionBuilders<TSpace>.VerticalFlow<T>(Unrect.Projections.Layout<TSpace, T>)'
    public static object Layout() => VerticalFlow(v => v.Next(Unrect.Projections.Projection.Text()));
  }
}

namespace PlacementGauntlet.EntryC.OverDemanded
{
  using Unrect.Projections;

  /// <summary>
  /// (t) THE OVER-DEMAND TRAP, CAUGHT — boundary (d)'s guidance made enforceable at the one place it
  /// can be. A file scoped to <c>ISpreadsheetSpace</c> may declare a section that reads nothing but
  /// text; nothing refuses that. What is refused is APPLYING it to a space that cannot answer, and
  /// the refusal is at the call site, in the language, with no run-time check anywhere.
  /// <para>
  /// So the trap is not silent end to end: it is silent while the declaration is written and loud
  /// the moment it meets a document. That is the honest shape of §5.5's "soft spot" — unenforced at
  /// the point of the mistake, enforced at the point of the consequence.
  /// </para>
  /// <para>
  /// <b>But the loud half is not articulate.</b> Measured rather than predicted: the natural
  /// spelling gives CS0411, the worst message in the taxonomy — the same one (j) records — naming
  /// NEITHER space, because <c>Map</c> takes its two type arguments from the projection and the
  /// argument at once and cannot unify them. Stating the arguments turns it into the sentence a
  /// reader needs (CS1503, both types named), which is exactly the fix nobody knows to try.
  /// <b>An analyzer's third diagnostic, or a non-generic <c>Map</c> overload per capability
  /// bundle, is what would close this.</b>
  /// </para>
  /// </summary>
  public static class T
  {
    // CS0411: The type arguments for method
    //         'ProjectionExtensions.Map<TSpace, TResult>(IProjection<TSpace, TResult>, TSpace)'
    //         cannot be inferred from the usage. Try specifying the type arguments explicitly.
    public static object Applied() => ScenarioCAudited.Report.Map(Sheets.K1());

    // CS1503: Argument 2: cannot convert from 'Unrect.Core.ICellValues' to 'Unrect.Spreadsheets.ISpreadsheetSpace'
    public static object Stated()
      => ScenarioCAudited.Report.Map<Unrect.Spreadsheets.ISpreadsheetSpace, AuditedIrrReport>(Sheets.K1());
  }
}

namespace PlacementGauntlet.EntryC.TwoSplitImports
{
  using static PlacementGauntlet.Staged.SplitRungs<Unrect.Core.ICellValues>;
  using static PlacementGauntlet.Staged.SplitRungs<Unrect.Spreadsheets.ISpreadsheetSpace>;

  /// <summary>
  /// (u) THE SPLIT-TYPE TRICK'S OWN COLLISION, for the comparison (p) invites. A nested TYPE
  /// imported twice IS the CS0104 §5.5 predicted — so the trick changes which diagnostic a reader
  /// meets for the same mistake.
  /// <para>
  /// <b>And CS0104 is the better message,</b> which is the one argument for adopting the trick that
  /// this arm found: it names the two enclosing types with their type ARGUMENTS, where CS0121 names
  /// the type PARAMETER twice. Weigh it against re-spelling five table rungs (see
  /// <see cref="PlacementGauntlet.Staged.SplitRungs{TSpace}"/>); the arm's judgment is that it is
  /// not worth it, but the ledger records both halves.
  /// </para>
  /// </summary>
  public static class U
  {
    // CS0104: 'Table' is an ambiguous reference between
    //         'PlacementGauntlet.Staged.SplitRungs<Unrect.Core.ICellValues>.Table' and
    //         'PlacementGauntlet.Staged.SplitRungs<Unrect.Spreadsheets.ISpreadsheetSpace>.Table'
    public static object Rung() => Table.Of<Line>();
  }
}

#endif

#if MUST_NOT_COMPILE_DECL

namespace PlacementGauntlet.EntryC.Declarations
{
  using System;
  using System.Collections.Generic;

  using Unrect.Core;
  using Unrect.Projections;

  /// <summary>
  /// (r) A NESTED TYPE AND A METHOD OF ONE NAME — the reason the split-type trick cannot be adopted
  /// for one rung and left alone for the rest. <c>Table.Of&lt;T&gt;()</c> and
  /// <c>Table(headerRows:, eachRow:)</c> cannot coexist, so choosing the trick re-spells the whole
  /// family.
  /// </summary>
  public static class R<TSpace>
    where TSpace : class, ICellValues
  {
    // CS0102: The type 'R<TSpace>' already contains a definition for 'Table'
    public static class Table
    {
      public static IProjection<IReadOnlyList<T>> Of<T>() => Projection.Table<T>();
    }

    public static IProjection<TSpace, IReadOnlyList<T>> Table<T>(int headerRows, IProjection<TSpace, T> eachRow)
      => Projection.Over<TSpace>().Table(headerRows, eachRow);
  }

  /// <summary>
  /// (s) AN EXTENSION METHOD IN A GENERIC STATIC CLASS — <b>the constraint that fixes Entry C's
  /// ceiling</b>, and the one the arm was commissioned to price. It is a declaration error on the
  /// CLASS, not on the method, so there is no per-member workaround: a closed vocabulary class can
  /// never carry a postfix operator.
  /// <para>
  /// The consequence, measured across the acceptance reads: <c>.Until</c>, <c>.Named</c>,
  /// <c>.Optional</c>, <c>.OrBlank</c>, <c>.Select</c>, <c>.Padded</c> and the whole of
  /// <c>ProjectionExtensions</c> reach an Entry C file through <c>using Unrect.Projections;</c> —
  /// an ordinary namespace import, which publishes no simple names and therefore cannot collide
  /// with the closed class. Entry C carries the prefix half of the vocabulary; the postfix half
  /// stays where it is. Under the geography law that is a seam, not a leak.
  /// </para>
  /// </summary>
  public static class S<TSpace>
    where TSpace : class, ICellValues
  {
    // CS1106: Extension method must be defined in a non-generic static class
    public static IProjection<TSpace, T> Until<T>(this IProjection<TSpace, T> projection, IRowLandmark landmark)
      => projection;
  }
}

#endif
