// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
	/// Reuse the same instances for boxed booleans.
	/// </summary>
	static class Boxes
	{
		public static readonly object True = true;
		public static readonly object False = false;
		
		public static object Box(bool value)
		{
			return value ? True : False;
		}
	}
}
