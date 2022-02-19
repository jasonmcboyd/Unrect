namespace Unrect.Excel
{
  public readonly struct SpreadsheetContext
  {
    public SpreadsheetContext(int index, string name)
    {
      Index = index;
      Name = name;
    }

    public int Index { get; }
    public string Name { get; }
  }
}
