using System;
using System.Collections.Generic;

using Unrect.Projections;
using Unrect.Spreadsheets;

using Xunit;

using static Unrect.Projections.ProjectionBuilders<Unrect.Spreadsheets.ICellSpace>;

namespace Unrect.Tests
{
  /// <summary>
  /// The value facet's renderer, pinned — the one piece of <see cref="Observations"/> that decides
  /// what a differential law is ABLE to see. It earns its own suite because it was wrong once and
  /// nothing noticed: a record whose field was a list rendered through the record's synthesized
  /// <c>ToString</c>, which prints a list as its type name, so two readings with different rows
  /// compared EQUAL at L3. That was found by perturbing a declaration, not by reading the harness,
  /// which is exactly the way a blind instrument is normally found.
  /// <para>
  /// Two kinds of pin live here. The first four are the RECURSION GUARDS — a renderer that walks
  /// arbitrary object graphs can hang or throw, and either failure mode would be a test-suite defect
  /// wearing a product defect's clothes. The last two are the JUDGMENT LINE the fix drew: a
  /// hand-written <c>ToString</c> is trusted and a compiler-written one is not, because
  /// <c>Cell.ToString()</c> prints the value in the cell while its properties would print
  /// everything about the cell except that.
  /// </para>
  /// </summary>
  public class ObservationsRenderingTests
  {
    /// <summary>The smallest space a leaf can be read over — the value under test comes from the closure, not the sheet.</summary>
    private static ICellSpace One() => SheetGrid.Of(new object?[,] { { "x" } });

    /// <summary>Renders <paramref name="value"/> the way the value facet would.</summary>
    private static string Rendered<T>(T value) => Observations.Observe(Point().Select(_ => value), One()).Value;

    // --- The recursion guards ------------------------------------------------------------------------

    [Fact]
    public void AValueThatContainsItselfRendersAsACycleRatherThanRecursingForever()
    {
      // Both ways a graph can close on itself: through a property, and through a collection.
      var node = new Node { Name = "root" };

      node.Next = node;
      node.Children = new[] { node };

      var rendered = Rendered(node);

      Assert.Contains("<cycle>", rendered);
      Assert.StartsWith("Node { Name = root, Next = ", rendered);
    }

    [Fact]
    public void AChainDeeperThanTheCapStopsAtTheDepthCap()
    {
      // Not a cycle — forty distinct nodes. A visited set alone would render all forty; the cap is
      // what keeps a deep-but-finite graph from turning one assertion message into a wall.
      var head = new Node { Name = "0" };
      var tail = head;

      for (var step = 1; step < 40; step++)
      {
        tail.Next = new Node { Name = step.ToString() };
        tail = tail.Next;
      }

      Assert.Contains("<depth>", Rendered(head));
    }

    [Fact]
    public void TheSameObjectTwiceAsSiblingsIsNotACycle()
    {
      // The reason the guard tracks ANCESTRY and not history. A shared reference is ordinary — a
      // caption reused by two sections, one interned string in two records — and rendering the second
      // occurrence as <cycle> would make two equal readings compare unequal depending on which one
      // the renderer reached first.
      var shared = new Node { Name = "shared" };

      var rendered = Rendered(new { Left = shared, Right = shared });

      Assert.DoesNotContain("<cycle>", rendered);
      Assert.Equal(
        "{ Left = Node { Name = shared, Next = <null>, Children = <null> }, "
        + "Right = Node { Name = shared, Next = <null>, Children = <null> } }",
        rendered);
    }

    [Fact]
    public void AGetterThatThrowsIsReportedRatherThanThrown()
    {
      // The harness describes a reading; it must not become a second way for one to fail. A property
      // that throws is a fact about the value, so it is rendered as one — and named, so two values
      // that throw DIFFERENT exceptions still compare unequal.
      Assert.Equal("Trouble { Boom = <threw InvalidOperationException> }", Rendered(new Trouble()));
    }

    // --- The judgment line --------------------------------------------------------------------------

    [Fact]
    public void AHandWrittenToStringIsRespected()
    {
      // Cell is the case the rule was written for: its ToString prints the number in the cell,
      // and its three public properties (Kind, IsBlank, HasValue) would print everything about the
      // cell except the number. Reflecting over it would LOSE information, so it is not reflected.
      var cell = Cell.Of(42m);

      Assert.Equal(cell.ToString(), Rendered(cell));
      Assert.DoesNotContain("Kind = ", Rendered(cell));

      // The same rule stated over a type this file owns, so the pin does not rest on Cell's
      // current spelling: a sentence someone wrote is the rendering, in full.
      Assert.Equal("a fund, spelled by hand", Rendered(new Spoken()));
    }

    [Fact]
    public void ACompilerGeneratedToStringIsReflectedThroughSoAWrappedListIsCompared()
    {
      // The defect itself, pinned. The record's own ToString would print
      // "Wrapped { Rows = System.Collections.Generic.List`1[System.String] }" — equal for every
      // possible list — so the renderer spells the record out from its properties instead, and the
      // elements land in the value facet where a law can see them.
      var rendered = Rendered(new Wrapped("book", new List<string> { "one", "two" }));

      Assert.Equal("Wrapped { Name = book, Rows = [one, two] }", rendered);

      // ...and the half that makes it a comparison rather than a rendering: two wrappers differing
      // only inside the list are different values.
      Assert.NotEqual(rendered, Rendered(new Wrapped("book", new List<string> { "one", "three" })));
    }

    // --- What the pins are made of ------------------------------------------------------------------

    /// <summary>A node that can reach itself, both through a property and through a collection.</summary>
    private sealed class Node
    {
      public string Name { get; set; } = "n";

      public Node? Next { get; set; }

      public IReadOnlyList<Node>? Children { get; set; }
    }

    /// <summary>A value one of whose properties will not answer.</summary>
    private sealed class Trouble
    {
      public string Boom => throw new InvalidOperationException("This getter does not answer.");
    }

    /// <summary>A type that speaks for itself, and therefore is allowed to.</summary>
    private sealed class Spoken
    {
      public string Hidden => "not printed";

      public override string ToString() => "a fund, spelled by hand";
    }

    /// <summary>The shape the defect hid in: a record whose field is a collection.</summary>
    private sealed record Wrapped(string Name, IReadOnlyList<string> Rows);
  }
}
