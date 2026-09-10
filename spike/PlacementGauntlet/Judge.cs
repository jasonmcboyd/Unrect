using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using Unrect.Projections;

namespace PlacementGauntlet
{
  /// <summary>
  /// SPIKE — the differential harness. Every scenario writes its declaration BOTH ways and asserts
  /// that the two readings are the same value: the façade is a spelling, so a difference here is a
  /// finding about the façade, never a finding about the document.
  /// </summary>
  public static class Judge
  {
    private static readonly JsonSerializerOptions Rendering = new JsonSerializerOptions { IncludeFields = true, WriteIndented = false };
    private static readonly List<string> Failures = new List<string>();

    public static int FailureCount => Failures.Count;

    public static void Section(string title)
    {
      Console.WriteLine();
      Console.WriteLine(title);
      Console.WriteLine(new string('=', title.Length));
    }

    /// <summary>Asserts that two spellings read the same thing, and says so either way.</summary>
    public static void Same(string claim, object? oldSpelling, object? newSpelling)
    {
      var before = Render(oldSpelling);
      var after = Render(newSpelling);

      if (string.Equals(before, after, StringComparison.Ordinal))
      {
        Console.WriteLine($"  [same] {claim}");
        return;
      }

      Failures.Add(claim);
      Console.WriteLine($"  [DIFF] {claim}");
      Console.WriteLine($"           old: {Truncated(before)}");
      Console.WriteLine($"           new: {Truncated(after)}");
    }

    /// <summary>
    /// L2: the value AND the geometry — where the projection landed, what it consumed, and what a
    /// parent must step past it. Two spellings that agree here differ, at most, in the account they
    /// give of themselves.
    /// </summary>
    public static void SameL2<T>(string claim, AppliedResult<T> oldSpelling, AppliedResult<T> newSpelling)
      => Same(claim, Geometry(oldSpelling), Geometry(newSpelling));

    /// <summary>
    /// L3's account-of-itself half: the failure a broken spelling raises, verbatim — which carries
    /// the declaration PATH, the SUBJECT, and the A1 location. Compared as text, so a difference in
    /// any of the three is a difference here.
    /// </summary>
    public static void SameFailure(string claim, Func<object?> oldSpelling, Func<object?> newSpelling)
      => Same(claim, Attempt(oldSpelling), Attempt(newSpelling));

    /// <summary>
    /// A negative pin: two spellings that must NOT agree, with the difference stated rather than
    /// merely asserted — "differs somewhere" is not a finding.
    /// </summary>
    public static void Different(string claim, object? one, object? other)
    {
      var first = Render(one);
      var second = Render(other);

      if (!string.Equals(first, second, StringComparison.Ordinal))
      {
        Console.WriteLine($"  [differs] {claim}");
        Console.WriteLine($"           one: {Truncated(first)}");
        Console.WriteLine($"           the other: {Truncated(second)}");
        return;
      }

      Failures.Add(claim);
      Console.WriteLine($"  [SAME, EXPECTED A DIFFERENCE] {claim}");
    }

    public static void Note(string note) => Console.WriteLine($"  - {note}");

    private static object Geometry<T>(AppliedResult<T> result) => new
    {
      result.Value,
      OffsetWidth = result.Offset.Size.Width,
      OffsetHeight = result.Offset.Size.Height,
      ConsumedWidth = result.Consumed.Width,
      ConsumedHeight = result.Consumed.Height,
      AdvanceWidth = result.Advance.Width,
      AdvanceHeight = result.Advance.Height,
    };

    private static string Attempt(Func<object?> read)
    {
      try
      {
        return "read " + Render(read());
      }
      catch (Exception failure)
      {
        return $"{failure.GetType().Name}: {failure.Message}";
      }
    }

    /// <summary>Runs something that must be refused at construction, and prints the refusal.</summary>
    public static void Refused(string claim, Action attempt)
    {
      try
      {
        attempt();
        Failures.Add(claim);
        Console.WriteLine($"  [NOT REFUSED] {claim}");
      }
      catch (ArgumentException failure)
      {
        Console.WriteLine($"  [refused] {claim}");
        Console.WriteLine($"           {failure.Message.Split(new[] { " (Parameter" }, StringSplitOptions.None)[0]}");
      }
    }

    /// <summary>
    /// A value as evidence. A string is already its own rendering — JSON-quoting one only escapes the
    /// angle brackets that make an inferred type readable — so only everything else is serialised.
    /// </summary>
    public static string Render(object? value) => value as string ?? JsonSerializer.Serialize(value, Rendering);

    /// <summary>
    /// A type as C# spells it, rather than as the CLR does — evidence about an INFERRED type is only
    /// evidence if a reader can compare two of them at a glance.
    /// </summary>
    public static string TypeName(Type type)
      => type.IsGenericType
        ? $"{type.Name.Substring(0, type.Name.IndexOf('`'))}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>"
        : type.Name;

    private static string Truncated(string rendering) => rendering.Length <= 400 ? rendering : rendering.Substring(0, 400) + " …";
  }
}
