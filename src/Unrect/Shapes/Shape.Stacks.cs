namespace Unrect.Shapes
{
  /// <summary>
  /// Mechanical: <c>Vertical</c> and <c>Horizontal</c> for arities 2 through 8, written out rather
  /// than abstracted. Arity lives here and nowhere else — one <c>StackShape</c> backs them all, and
  /// eight is where <c>ValueTuple</c> stops before <c>TRest</c>; nest a stack to go further.
  /// <para>
  /// Every overload lays its children out in declaration order along its axis, consuming along that
  /// axis only, and returns their results as a tuple — which <c>Select</c> unpacks.
  /// </para>
  /// </summary>
  public static partial class Shape
  {
    /// <summary>2 shapes downwards, in declaration order.</summary>
    public static IShape<(T1, T2)> Vertical<T1, T2>(
      IShape<T1> first,
      IShape<T2> second)
      => Stack<(T1, T2)>(
        Orientation.Vertical,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)) },
        values => ((T1)values[0]!, (T2)values[1]!));

    /// <summary>3 shapes downwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3)> Vertical<T1, T2, T3>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third)
      => Stack<(T1, T2, T3)>(
        Orientation.Vertical,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!));

    /// <summary>4 shapes downwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4)> Vertical<T1, T2, T3, T4>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth)
      => Stack<(T1, T2, T3, T4)>(
        Orientation.Vertical,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!));

    /// <summary>5 shapes downwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4, T5)> Vertical<T1, T2, T3, T4, T5>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth)
      => Stack<(T1, T2, T3, T4, T5)>(
        Orientation.Vertical,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!));

    /// <summary>6 shapes downwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4, T5, T6)> Vertical<T1, T2, T3, T4, T5, T6>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth,
      IShape<T6> sixth)
      => Stack<(T1, T2, T3, T4, T5, T6)>(
        Orientation.Vertical,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)), NotNull(sixth, nameof(sixth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!, (T6)values[5]!));

    /// <summary>7 shapes downwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4, T5, T6, T7)> Vertical<T1, T2, T3, T4, T5, T6, T7>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth,
      IShape<T6> sixth,
      IShape<T7> seventh)
      => Stack<(T1, T2, T3, T4, T5, T6, T7)>(
        Orientation.Vertical,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)), NotNull(sixth, nameof(sixth)), NotNull(seventh, nameof(seventh)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!, (T6)values[5]!, (T7)values[6]!));

    /// <summary>8 shapes downwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4, T5, T6, T7, T8)> Vertical<T1, T2, T3, T4, T5, T6, T7, T8>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth,
      IShape<T6> sixth,
      IShape<T7> seventh,
      IShape<T8> eighth)
      => Stack<(T1, T2, T3, T4, T5, T6, T7, T8)>(
        Orientation.Vertical,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)), NotNull(sixth, nameof(sixth)), NotNull(seventh, nameof(seventh)), NotNull(eighth, nameof(eighth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!, (T6)values[5]!, (T7)values[6]!, (T8)values[7]!));

    /// <summary>2 shapes rightwards, in declaration order.</summary>
    public static IShape<(T1, T2)> Horizontal<T1, T2>(
      IShape<T1> first,
      IShape<T2> second)
      => Stack<(T1, T2)>(
        Orientation.Horizontal,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)) },
        values => ((T1)values[0]!, (T2)values[1]!));

    /// <summary>3 shapes rightwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3)> Horizontal<T1, T2, T3>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third)
      => Stack<(T1, T2, T3)>(
        Orientation.Horizontal,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!));

    /// <summary>4 shapes rightwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4)> Horizontal<T1, T2, T3, T4>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth)
      => Stack<(T1, T2, T3, T4)>(
        Orientation.Horizontal,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!));

    /// <summary>5 shapes rightwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4, T5)> Horizontal<T1, T2, T3, T4, T5>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth)
      => Stack<(T1, T2, T3, T4, T5)>(
        Orientation.Horizontal,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!));

    /// <summary>6 shapes rightwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4, T5, T6)> Horizontal<T1, T2, T3, T4, T5, T6>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth,
      IShape<T6> sixth)
      => Stack<(T1, T2, T3, T4, T5, T6)>(
        Orientation.Horizontal,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)), NotNull(sixth, nameof(sixth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!, (T6)values[5]!));

    /// <summary>7 shapes rightwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4, T5, T6, T7)> Horizontal<T1, T2, T3, T4, T5, T6, T7>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth,
      IShape<T6> sixth,
      IShape<T7> seventh)
      => Stack<(T1, T2, T3, T4, T5, T6, T7)>(
        Orientation.Horizontal,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)), NotNull(sixth, nameof(sixth)), NotNull(seventh, nameof(seventh)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!, (T6)values[5]!, (T7)values[6]!));

    /// <summary>8 shapes rightwards, in declaration order.</summary>
    public static IShape<(T1, T2, T3, T4, T5, T6, T7, T8)> Horizontal<T1, T2, T3, T4, T5, T6, T7, T8>(
      IShape<T1> first,
      IShape<T2> second,
      IShape<T3> third,
      IShape<T4> fourth,
      IShape<T5> fifth,
      IShape<T6> sixth,
      IShape<T7> seventh,
      IShape<T8> eighth)
      => Stack<(T1, T2, T3, T4, T5, T6, T7, T8)>(
        Orientation.Horizontal,
        new IShape[] { NotNull(first, nameof(first)), NotNull(second, nameof(second)), NotNull(third, nameof(third)), NotNull(fourth, nameof(fourth)), NotNull(fifth, nameof(fifth)), NotNull(sixth, nameof(sixth)), NotNull(seventh, nameof(seventh)), NotNull(eighth, nameof(eighth)) },
        values => ((T1)values[0]!, (T2)values[1]!, (T3)values[2]!, (T4)values[3]!, (T5)values[4]!, (T6)values[5]!, (T7)values[6]!, (T8)values[7]!));
  }
}
