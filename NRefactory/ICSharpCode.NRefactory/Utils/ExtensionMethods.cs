// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.Utils
{
	/// <summary>
	/// Contains extension methods for use within NRefactory.
	/// </summary>
	internal static class ExtensionMethods
	{
		public static void AddRange<T>(this ICollection<T> target, IEnumerable<T> input)
		{
			foreach (T item in input)
				target.Add(item);
		}
	}
}
