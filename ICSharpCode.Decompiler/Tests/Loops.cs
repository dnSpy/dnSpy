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
using System.Collections;
using System.Collections.Generic;

public class Loops
{
	public void ForEach(IEnumerable<string> enumerable)
	{
		foreach (string current in enumerable)
		{
			current.ToLower();
		}
	}
	
	public void ForEachOverList(List<string> list)
	{
		// List has a struct as enumerator, so produces quite different IL than foreach over the IEnumerable interface
		foreach (string current in list)
		{
			current.ToLower();
		}
	}
	
	public void ForEachOverNonGenericEnumerable(IEnumerable enumerable)
	{
		foreach (object current in enumerable)
		{
			current.ToString();
		}
	}
	
	public void ForEachOverNonGenericEnumerableWithAutomaticCast(IEnumerable enumerable)
	{
		foreach (int num in enumerable)
		{
			num.ToString();
		}
	}
	
//	public void ForEachOverArray(string[] array)
//	{
//		foreach (string text in array)
//		{
//			text.ToLower();
//		}
//	}
	
	public void ForOverArray(string[] array)
	{
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ToLower();
		}
	}
}

