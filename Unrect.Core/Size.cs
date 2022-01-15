﻿namespace Unrect.Core
{
  public struct Size
  {
    public Size(uint width, uint height)
    {
      Width = width;
      Height = height;
    }

    public uint Width { get; }
    public uint Height { get; }
  }
}
