namespace Unrect.Projections
{
  /// <summary>
  /// A machine whose hold moves as it runs — a repeat, which may hand back the spans of the
  /// attempt in progress but never those of an occurrence it has committed. A machine that does
  /// not implement this holds what its node's <see cref="DefinitionNode.Retains"/> declares.
  /// </summary>
  internal interface IHolding
  {
    /// <summary>
    /// The position, among the spans this machine has been offered, of the oldest one it may still
    /// hand back; null when it holds nothing of its own beyond what its open children hold.
    /// </summary>
    int? HeldFrom { get; }
  }
}
