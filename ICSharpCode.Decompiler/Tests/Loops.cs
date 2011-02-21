// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

public class Loops
{
	public void ForEach(IEnumerable<string> enumerable)
	{
		foreach (string text in enumerable) {
			text.ToLower();
		}
	}
	
	public void ForEachOverList(List<string> list)
	{
		// List has a struct as enumerator, so produces quite different IL than foreach over the IEnumerable interface
		foreach (string text in list) {
			text.ToLower();
		}
	}
	
	public void ForEachOverArray(string[] array)
	{
		foreach (string text in array) {
			text.ToLower();
		}
	}
	
	public void ForOverArray(string[] array)
	{
		for (int i = 0; i < array.Length; i++) {
			array[i].ToLower();
		}
	}
}

