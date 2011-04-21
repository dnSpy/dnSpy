// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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

