using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Unrect.Analyzers
{
  /// <summary>
  /// The handful of Unrect types a demand is written in, resolved once per compilation — and the
  /// one question asked of them: <em>what does this type demand of a space?</em>
  /// <para>
  /// <see cref="TryLoad"/> answers null when the compilation does not reference Unrect at all,
  /// which is every analyzer's first exit.
  /// </para>
  /// </summary>
  internal sealed class UnrectSymbols
  {
    private UnrectSymbols(
      INamedTypeSymbol space,
      INamedTypeSymbol projection,
      INamedTypeSymbol scope,
      INamedTypeSymbol builders,
      INamedTypeSymbol stage,
      INamedTypeSymbol demand,
      INamedTypeSymbol? rowLandmark,
      INamedTypeSymbol? columnLandmark,
      INamedTypeSymbol? cursor,
      INamedTypeSymbol? scopedCursor)
    {
      Space = space;
      Projection = projection;
      Scope = scope;
      Builders = builders;
      Stage = stage;
      Demand = demand;
      RowLandmark = rowLandmark;
      ColumnLandmark = columnLandmark;
      Cursor = cursor;
      ScopedCursor = scopedCursor;
    }

    /// <summary><c>Unrect.Core.ICellValues</c> — the demand a declaration makes when it makes none.</summary>
    public INamedTypeSymbol Space { get; }

    /// <summary><c>IProjection&lt;TSpace, TResult&gt;</c>, the only place a demand is written.</summary>
    public INamedTypeSymbol Projection { get; }

    /// <summary><c>ProjectionScope&lt;TSpace&gt;</c>.</summary>
    public INamedTypeSymbol Scope { get; }

    /// <summary><c>ProjectionBuilders&lt;TSpace&gt;</c>.</summary>
    public INamedTypeSymbol Builders { get; }

    /// <summary><c>PlacementStage&lt;TSpace&gt;</c>, the base of the scoped pipeline stages.</summary>
    public INamedTypeSymbol Stage { get; }

    /// <summary><c>Demand&lt;TSpace&gt;</c>, the witness a capability's package publishes.</summary>
    public INamedTypeSymbol Demand { get; }

    /// <summary><c>IRowLandmark&lt;TSpace&gt;</c>; null if the projection layer predates it.</summary>
    public INamedTypeSymbol? RowLandmark { get; }

    /// <summary><c>IColumnLandmark&lt;TSpace&gt;</c>; null if the projection layer predates it.</summary>
    public INamedTypeSymbol? ColumnLandmark { get; }

    /// <summary><c>LayoutCursor</c>, the plain layout's cursor.</summary>
    public INamedTypeSymbol? Cursor { get; }

    /// <summary><c>LayoutCursor&lt;TSpace&gt;</c>, a scoped layout's cursor.</summary>
    public INamedTypeSymbol? ScopedCursor { get; }

    /// <summary>True for either layout cursor — the receiver a child is declared on.</summary>
    public bool IsCursor(ITypeSymbol type)
      => SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, Cursor)
        || SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, ScopedCursor);

    /// <summary>The Unrect types in <paramref name="compilation"/>, or null if it has none.</summary>
    public static UnrectSymbols? TryLoad(Compilation compilation)
    {
      var space = compilation.GetTypeByMetadataName("Unrect.Core.ICellValues");
      var projection = compilation.GetTypeByMetadataName("Unrect.Projections.IProjection`2");
      var scope = compilation.GetTypeByMetadataName("Unrect.Projections.ProjectionScope`1");
      var builders = compilation.GetTypeByMetadataName("Unrect.Projections.ProjectionBuilders`1");
      var stage = compilation.GetTypeByMetadataName("Unrect.Projections.PlacementStage`1");
      var demand = compilation.GetTypeByMetadataName("Unrect.Projections.Demand`1");

      if (space is null || projection is null || scope is null || builders is null || stage is null || demand is null)
        return null;

      return new UnrectSymbols(
        space,
        projection,
        scope,
        builders,
        stage,
        demand,
        compilation.GetTypeByMetadataName("Unrect.Projections.IRowLandmark`1"),
        compilation.GetTypeByMetadataName("Unrect.Projections.IColumnLandmark`1"),
        compilation.GetTypeByMetadataName("Unrect.Projections.LayoutCursor"),
        compilation.GetTypeByMetadataName("Unrect.Projections.LayoutCursor`1"));
    }

    /// <summary>
    /// What <paramref name="type"/> demands of a space — the <c>TSpace</c> of the projection,
    /// matcher or witness it is — or null where the type says nothing about a space at all.
    /// </summary>
    public ITypeSymbol? DemandOf(ITypeSymbol? type)
      => SpaceArgumentOf(type, Projection)
        ?? SpaceArgumentOf(type, Demand)
        ?? SpaceArgumentOf(type, RowLandmark)
        ?? SpaceArgumentOf(type, ColumnLandmark);

    /// <summary>True when <paramref name="type"/> asks for more of a space than <c>ICellValues</c>.</summary>
    public bool DemandsBeyondSpace(ITypeSymbol? type)
      => DemandOf(type) is ITypeSymbol demanded && !IsSpace(demanded);

    /// <summary>True for <c>ICellValues</c> itself — the demand that is no demand.</summary>
    public bool IsSpace(ITypeSymbol type) => SymbolEqualityComparer.Default.Equals(type, Space);

    /// <summary>
    /// True when a space of type <paramref name="offered"/> satisfies a demand for
    /// <paramref name="demanded"/>.
    /// </summary>
    public static bool Satisfies(ITypeSymbol offered, ITypeSymbol demanded)
    {
      if (SymbolEqualityComparer.Default.Equals(offered, demanded))
        return true;

      foreach (var implemented in offered.AllInterfaces)
      {
        if (SymbolEqualityComparer.Default.Equals(implemented, demanded))
          return true;
      }

      for (var current = offered.BaseType; current is object; current = current.BaseType)
      {
        if (SymbolEqualityComparer.Default.Equals(current, demanded))
          return true;
      }

      return false;
    }

    /// <summary>
    /// The space argument of <paramref name="definition"/> as <paramref name="type"/> constructs it
    /// — first in every one of these types — preferring the most demanding construction where the
    /// type reaches the definition more than once.
    /// </summary>
    private ITypeSymbol? SpaceArgumentOf(ITypeSymbol? type, INamedTypeSymbol? definition)
    {
      if (type is null || definition is null)
        return null;

      ITypeSymbol? found = null;

      foreach (var candidate in Constructions(type, definition))
      {
        var argument = candidate.TypeArguments[0];

        // A projection that names a capability lists that construction alongside the ICellValues one it
        // inherits (IProjection<T> IS an IProjection<ICellValues, T>); the demanding one is the answer.
        if (found is null || (IsSpace(found) && !IsSpace(argument)))
          found = argument;
      }

      return found;
    }

    private static IEnumerable<INamedTypeSymbol> Constructions(
      ITypeSymbol type,
      INamedTypeSymbol definition)
    {
      if (type is INamedTypeSymbol named
        && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, definition))
      {
        yield return named;
      }

      foreach (var implemented in type.AllInterfaces)
      {
        if (SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, definition))
          yield return implemented;
      }
    }
  }
}
