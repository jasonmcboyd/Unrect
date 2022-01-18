﻿using System;

namespace Unrect.Excel
{
  public readonly struct StringSpreadsheetValue : ISpreadsheetValue
  {
    public StringSpreadsheetValue(string value)
    {
      TypedValue = value;
    }

    private string TypedValue { get; }
    public object? Value => TypedValue;

    public bool HasValue => true;

    private static readonly Type _ValueType = typeof(string);
    public Type GetValueType() => typeof(double);

    public DateTime GetDateTime() => throw new InvalidOperationException("Incorrect type.");
    public double GetDouble() => throw new InvalidOperationException("Incorrect type.");
    public int GetInt() => throw new InvalidOperationException("Incorrect type.");
    public string GetString() => TypedValue;
  }
}