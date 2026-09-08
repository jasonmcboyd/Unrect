using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Unrect.Core;
using Unrect.Projections;

using Xunit;

namespace Unrect.Tests
{
  /// <summary>
  /// One reading of one declaration over one space, split into the facets an equivalence law is
  /// allowed to name. It exists so a law can be stated at the level it actually holds at rather
  /// than at the level one hopes for: L1 (value and failure classification), L2 (L1 plus what was
  /// consumed and where), L3 (L2 plus the diagnostics and the failure's path and subject).
  /// <para>
  /// Generalized by copy from the comparator at the bottom of
  /// <c>Projections/LazyDenotationTests</c> — the tree's strongest existing differential machinery,
  /// which compares one declaration read two ways. That suite is a finished record and is left
  /// alone; the mild duplication between it and this file is accepted, because a law suite wants to
  /// name a <em>level</em> and the lazy sweep wants to compare everything at once.
  /// </para>
  /// <para>
  /// <strong>The one rule for negative pins.</strong> A law that stops holding at some level is
  /// pinned by asserting the SPECIFIC difference — <c>Assert.Contains("Under", failure.Path)</c>,
  /// <c>Assert.NotEqual(expected.Consumed, actual.Consumed)</c> with both spelled out — never by a
  /// blanket "these differ somewhere". There is deliberately no <c>AssertNotL3</c>: a test that
  /// only says "not identical" would keep passing after the difference it was written for had been
  /// replaced by a different one.
  /// </para>
  /// </summary>
  internal sealed class Observation
  {
    internal Observation(
      string value,
      string consumed,
      string offset,
      string advance,
      IReadOnlyList<string> diagnostics,
      string? failure,
      string? failureSubject,
      string? failurePath)
    {
      Value = value;
      Consumed = consumed;
      Offset = offset;
      Advance = advance;
      Diagnostics = diagnostics;
      Failure = failure;
      FailureSubject = failureSubject;
      FailurePath = failurePath;
    }

    /// <summary>The projected value, rendered deeply so a list is compared by its elements.</summary>
    public string Value { get; }

    /// <summary>How much of its own extent the projection used, as "WxH".</summary>
    public string Consumed { get; }

    /// <summary>Where the placement resolved to, as "WxH" — L2's other half.</summary>
    public string Offset { get; }

    /// <summary>
    /// What a following sibling must step past: offset plus consumed. Distinct from <see
    /// cref="Consumed"/> on purpose — the offset-on-a-flow law is the case where two spellings agree
    /// on the advance and disagree on how it was split.
    /// </summary>
    public string Advance { get; }

    /// <summary>What the parse noticed, in order — order is part of the claim, so this is L3.</summary>
    public IReadOnlyList<string> Diagnostics { get; }

    /// <summary>
    /// The failure's L1 identity, or null where the declaration succeeded: the problem itself, the
    /// cell it was looking at, whether it was a fault, and what it wrapped.
    /// <para>
    /// Deliberately WITHOUT the path and the subject. A modifier that may change only diagnostics —
    /// <c>.Named</c> is the whole family — changes both of those and nothing else, so leaving them
    /// in would make L1 unstatable for exactly the laws that need it most. They are compared at L3,
    /// where they belong: a path is what a user reads, not what a declaration means.
    /// </para>
    /// </summary>
    public string? Failure { get; }

    /// <summary>The failing projection's label, or null where the declaration succeeded — L3.</summary>
    public string? FailureSubject { get; }

    /// <summary>The declaration path that reached the failure, or null — L3.</summary>
    public string? FailurePath { get; }
  }

  /// <summary>
  /// How a differential law suite reads a declaration and how it compares two readings. See <see
  /// cref="Observation"/> for what the levels mean and for the negative-pin rule.
  /// </summary>
  internal static class Observations
  {
    /// <summary>
    /// Reads <paramref name="projection"/> over <paramref name="space"/> and records everything a
    /// caller can observe about the reading.
    /// <para>
    /// Two entry points because they answer different halves of the question: the value and what the
    /// parse noticed come from <c>MapWithDiagnostics</c>, the offset and the extent consumed from
    /// <c>Apply</c>. A projection is a value safe to apply twice, so this is one reading asked about
    /// twice rather than two readings.
    /// </para>
    /// </summary>
    public static Observation Observe<T>(IProjection<T> projection, ISpace space)
    {
      try
      {
        var mapped = projection.MapWithDiagnostics(space);
        var applied = projection.Apply(space);

        return new Observation(
          RenderValue(mapped.Value),
          Render(applied.Consumed),
          Render(applied.Offset.Size),
          Render(applied.Advance),
          mapped.Diagnostics.Select(Describe).ToList(),
          null,
          null,
          null);
      }
      catch (ProjectionException failure)
      {
        return new Observation(
          "<threw>",
          "<threw>",
          "<threw>",
          "<threw>",
          Array.Empty<string>(),
          Describe(failure),
          failure.Subject,
          failure.Path);
      }
    }

    /// <summary>L1: the same value, or the same failure said in the same words about the same cell.</summary>
    public static void AssertL1(Observation expected, Observation actual)
    {
      Facet("value", expected.Value, actual.Value);
      Facet("failure", expected.Failure, actual.Failure);
    }

    /// <summary>L2: L1, plus the same extent consumed from the same place, and the same advance.</summary>
    public static void AssertL2(Observation expected, Observation actual)
    {
      AssertL1(expected, actual);

      Facet("consumed", expected.Consumed, actual.Consumed);
      Facet("offset", expected.Offset, actual.Offset);
      Facet("advance", expected.Advance, actual.Advance);
    }

    /// <summary>L3: L2, plus what a user reads — the diagnostics in order, and the failure's label and path.</summary>
    public static void AssertL3(Observation expected, Observation actual)
    {
      AssertL2(expected, actual);

      Facet("failure subject", expected.FailureSubject, actual.FailureSubject);
      Facet("failure path", expected.FailurePath, actual.FailurePath);

      Assert.Equal(expected.Diagnostics, actual.Diagnostics);
    }

    /// <summary>
    /// Compares one facet by name, so a failure reads "consumed: 3x2 != consumed: 3x3" rather than
    /// leaving the reader to work out which of eight strings moved.
    /// </summary>
    private static void Facet(string name, string? expected, string? actual)
      => Assert.Equal($"{name}: {expected ?? "<none>"}", $"{name}: {actual ?? "<none>"}");

    private static string Render(Size size) => $"{size.Width}x{size.Height}";

    private static string RenderValue(object? value) => value switch
    {
      null => "<null>",
      string text => text,
      IEnumerable items => "[" + string.Join(", ", items.Cast<object?>().Select(RenderValue)) + "]",
      _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
    };

    private static string Describe(ProjectionDiagnostic diagnostic)
      => $"{diagnostic.Severity} {diagnostic.Subject} at {diagnostic.Location.A1} in {diagnostic.Path}: {diagnostic.Message}";

    /// <summary>
    /// The failure's L1 identity. <c>Problem</c> rather than <c>Message</c> because the message
    /// template embeds the path and the subject, which are L3 — see <see cref="Observation.Failure"/>.
    /// </summary>
    private static string Describe(ProjectionException failure)
      => $"{failure.Problem} | at={failure.Location} "
       + $"| fault={failure.IsFault} | inner={failure.InnerException?.GetType().Name ?? "<none>"}";
  }
}
