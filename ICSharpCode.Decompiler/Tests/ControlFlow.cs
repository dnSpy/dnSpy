// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;

public static class ControlFlow
{
	public static void EmptyIf(string input, List<string> value, Dictionary<int, string> _headers)
	{
		if (value.Contains("test"))
		{
		}
		_headers.Add(2, "result");
	}
	
	public static void NormalIf(string input, List<string> value, Dictionary<int, string> _headers)
	{
		if (value.Contains("test"))
		{
			_headers.Add(1, "result");
		}
		else
		{
			_headers.Add(1, "else");
		}
		_headers.Add(2, "end");
	}
	
	public static void NormalIf2(string input, List<string> value, Dictionary<int, string> _headers)
	{
		if (value.Contains("test"))
		{
			_headers.Add(1, "result");
		}
		_headers.Add(2, "end");
	}
	
	public static void NormalIf3(string input, List<string> value, Dictionary<int, string> _headers)
	{
		if (value.Contains("test"))
		{
			_headers.Add(1, "result");
		}
		else
		{
			_headers.Add(1, "else");
		}
	}
	
	public static void Test(string input, List<string> value, Dictionary<int, string> _headers)
	{
		foreach (string current in value)
		{
			_headers.Add(0, current);
		}
		if (value.Contains("test"))
		{
			_headers.Add(1, "result");
		}
		else
		{
			_headers.Add(1, "else");
		}
	}

	public static void CascadingIfElse(bool condition, string input, int index)
	{
		if (condition)
		{
			Console.WriteLine("condition");
		} 
		else if (input == null)
		{
			Console.WriteLine("condition2");
		}
		else if (index > 1)
		{
			Console.WriteLine("condition3");
		}
	}
}
