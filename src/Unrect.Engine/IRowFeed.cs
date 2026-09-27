namespace Unrect
{
  /// <summary>
  /// A space whose rows arrive in order and are kept only while a machine may still read them: the
  /// one door a streamed source has under the push interpreter. The driver loads a row, offers it,
  /// and releases what no open machine holds; a read of a released row is a located failure, not a
  /// re-read, because a forward pass cannot go back.
  /// </summary>
  public interface IRowFeed
  {
    /// <summary>Loads the next row; false when the source has none left.</summary>
    bool Advance();

    /// <summary>How many rows have been loaded so far — the next row's index.</summary>
    int Loaded { get; }

    /// <summary>How many rows are held right now.</summary>
    int Retained { get; }

    /// <summary>The most rows the feed may hold at once, or null for no limit.</summary>
    int? Cap { get; }

    /// <summary>Rows before <paramref name="row"/> may be dropped; the feed keeps at least the newest.</summary>
    void Release(int row);
  }
}
