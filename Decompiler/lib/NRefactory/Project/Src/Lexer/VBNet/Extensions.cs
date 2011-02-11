// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ICSharpCode.NRefactory.Parser.VB
{
	public static class Extensions
	{
		public static bool IsElement<T>(this IEnumerable<T> items, Func<T, bool> check)
		{
			T item = items.FirstOrDefault();
			
			if (item != null)
				return check(item);
			return false;
		}
	}
}
