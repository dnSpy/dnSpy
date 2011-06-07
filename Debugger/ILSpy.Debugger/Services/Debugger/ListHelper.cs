// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;

namespace ICSharpCode.ILSpy.Debugger.Services.Debugger
{
	/// <summary>
	/// ListHelper wraps System.Collection.Generic.List methods to return the original list,
	/// instead of returning 'void', so we can write eg. list.Sorted().First()
	/// </summary>
	static class ListHelper
	{
		public static List<T> Sorted<T>(this List<T> list, IComparer<T> comparer)
		{
			list.Sort(comparer);
			return list;
		}
		
		public static List<T> Sorted<T>(this List<T> list)
		{
			list.Sort();
			return list;
		}
		
		public static List<T> ToList<T>(this T singleItem)
		{
			var newList = new List<T>();
			newList.Add(singleItem);
			return newList;
		}
	}
}
