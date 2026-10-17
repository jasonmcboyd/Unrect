using System;
using System.Collections.Generic;

using Unrect.Fluent;
using Unrect.Interactive;
using Unrect.Spreadsheets;

using Xunit;

namespace Unrect.Tests.Interactive
{
  /// <summary>
  /// A scaffolded class is written in a member style, and every style is one the binder fills: the
  /// three settable shapes through their properties, the read-only shape through its constructor.
  /// The default follows the runtime the scaffolder runs in, because that is the process whose
  /// editor the source is pasted into.
  /// </summary>
  public class ScaffoldStyleTests
  {
    private static ICellSpace Sheet() => SheetGrid.Of(new object?[,]
    {
      { "Client", "Amount" },
      { "Acme", 10.5 },
      { "Bolt", 20.25 },
    });

    private static string Lines(params string[] lines) => string.Join(Environment.NewLine, lines);

    [Fact]
    public void InitWritesInitOnlyProperties()
      => Assert.Equal(
        Lines(
          "public sealed class Line",
          "{",
          "    public string Client { get; init; } = \"\";",
          "    public decimal Amount { get; init; }",
          "}"),
        Sheet().ScaffoldClass("Line", style: MemberStyle.Init));

    [Fact]
    public void SetWritesPlainProperties()
      => Assert.Equal(
        Lines(
          "public sealed class Line",
          "{",
          "    public string Client { get; set; } = \"\";",
          "    public decimal Amount { get; set; }",
          "}"),
        Sheet().ScaffoldClass("Line", style: MemberStyle.Set));

    [Fact]
    public void PrivateSetWritesPrivateSetters()
      => Assert.Equal(
        Lines(
          "public sealed class Line",
          "{",
          "    public string Client { get; private set; } = \"\";",
          "    public decimal Amount { get; private set; }",
          "}"),
        Sheet().ScaffoldClass("Line", style: MemberStyle.PrivateSet));

    [Fact]
    public void ReadOnlyWritesAConstructorAndGetOnlyProperties()
      => Assert.Equal(
        Lines(
          "public sealed class Line",
          "{",
          "    public Line(string client, decimal amount)",
          "    {",
          "        Client = client;",
          "        Amount = amount;",
          "    }",
          "",
          "    public string Client { get; }",
          "    public decimal Amount { get; }",
          "}"),
        Sheet().ScaffoldClass("Line", style: MemberStyle.ReadOnly));

    [Fact]
    public void TheDefaultStyleIsTheRuntimes()
    {
      // Init where the compiler can take it, Set on .NET Framework; the same answer on both legs.
      Assert.Equal(RuntimeDefaults.IsNetFramework ? MemberStyle.Set : MemberStyle.Init, RuntimeDefaults.MemberStyle);
      Assert.Equal(Sheet().ScaffoldClass("Line", style: RuntimeDefaults.MemberStyle), Sheet().ScaffoldClass("Line"));
    }

    [Fact]
    public void ARecordSaysWhereItCannotCompile()
    {
      var source = Sheet().ScaffoldRecord("Line");

      Assert.Equal(
        RuntimeDefaults.IsNetFramework,
        source.StartsWith("// a positional record needs C# 9; on .NET Framework use ScaffoldClass(…)", StringComparison.Ordinal));
      Assert.EndsWith("public sealed record Line(string Client, decimal Amount);", source, StringComparison.Ordinal);
    }

    // --- The styles bind ---------------------------------------------------------------------------

    public sealed class PrivatelySet
    {
      public string Client { get; private set; } = "";

      public decimal Amount { get; private set; }
    }

    public sealed class ReadOnlyLine
    {
      public ReadOnlyLine(string client, decimal amount)
      {
        Client = client;
        Amount = amount;
      }

      public string Client { get; }

      public decimal Amount { get; }
    }

    [Fact]
    public void APrivateSetterIsFilledByTheBinder()
    {
      IReadOnlyList<PrivatelySet> rows = ProjectionBuilders<ICellSpace>.Table<PrivatelySet>().Map(Sheet());

      Assert.Equal(new[] { ("Acme", 10.5m), ("Bolt", 20.25m) }, Array.ConvertAll(new List<PrivatelySet>(rows).ToArray(), row => (row.Client, row.Amount)));
    }

    [Fact]
    public void AReadOnlyClassIsFilledThroughItsConstructor()
    {
      IReadOnlyList<ReadOnlyLine> rows = ProjectionBuilders<ICellSpace>.Table<ReadOnlyLine>().Map(Sheet());

      Assert.Equal(new[] { ("Acme", 10.5m), ("Bolt", 20.25m) }, Array.ConvertAll(new List<ReadOnlyLine>(rows).ToArray(), row => (row.Client, row.Amount)));
    }
  }
}
