namespace Unrect.Shapes
{
  /// <summary>
  /// Mechanical: <c>Overlay</c> for arities 2 through 8, written out rather than abstracted, the
  /// same way the stacks are — one <c>OverlayShape</c> backs them all.
  /// <para>
  /// Every child is applied to the overlay's whole extent and places itself inside it, so an
  /// overlay describes a region that several independent things share: a header band holding an
  /// entity block on the left, a fund band across the top, and a labelled total somewhere below.
  /// Children may overlap; the result is their values as a tuple, which <c>Select</c> unpacks.
  /// </para>
  /// </summary>
  public static partial class Shape
  {
    /// <summary>2 shapes sharing one extent, each placing itself inside it.</summary>
    public static IShape<(T1, T2)> Overlay<T1, T2>(
      IShape<T1> first,
      IShape<T2> second)
      => Overlay<(T1, T2)>(
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)) },
        values => ((T1)values[0]!, (T2)values[1]!));

    /// <summary>3 shapes sharing one extent, each placing itself inside it.</summary>
    public static IShape<(T1, T2, T3)> Overlay<T1, T2, T3>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third)
      => Overlay<(T1, T2, T3)>(
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!));

    /// <summary>4 shapes sharing one extent, each placing itself inside it.</summary>
    public static IShape<(T1, T2, T3, T4)> Overlay<T1, T2, T3, T4>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth)
      => Overlay<(T1, T2, T3, T4)>(
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!));

    /// <summary>5 shapes sharing one extent, each placing itself inside it.</summary>
    public static IShape<(T1, T2, T3, T4, T5)> Overlay<T1, T2, T3, T4, T5>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth)
      => Overlay<(T1, T2, T3, T4, T5)>(
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!));

    /// <summary>6 shapes sharing one extent, each placing itself inside it.</summary>
    public static IShape<(T1, T2, T3, T4, T5, T6)> Overlay<T1, T2, T3, T4, T5, T6>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth,
      IShape<T6> sixth)
      => Overlay<(T1, T2, T3, T4, T5, T6)>(
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)), NotNull(sixth, nameof(sixth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!, (T6)values[5]!));

    /// <summary>7 shapes sharing one extent, each placing itself inside it.</summary>
    public static IShape<(T1, T2, T3, T4, T5, T6, T7)> Overlay<T1, T2, T3, T4, T5, T6, T7>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth,
      IShape<T6> sixth,
      IShape<T7> seventh)
      => Overlay<(T1, T2, T3, T4, T5, T6, T7)>(
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)), NotNull(sixth, nameof(sixth)), NotNull(seventh, nameof(seventh)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!, (T6)values[5]!, (T7)values[6]!));

    /// <summary>8 shapes sharing one extent, each placing itself inside it.</summary>
    public static IShape<(T1, T2, T3, T4, T5, T6, T7, T8)> Overlay<T1, T2, T3, T4, T5, T6, T7, T8>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth,
      IShape<T6> sixth,
      IShape<T7> seventh,
      IShape<T8> eighth)
      => Overlay<(T1, T2, T3, T4, T5, T6, T7, T8)>(
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)), NotNull(sixth, nameof(sixth)), NotNull(seventh, nameof(seventh)), NotNull(eighth, nameof(eighth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!, (T6)values[5]!, (T7)values[6]!, (T8)values[7]!));
  }
}
