// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.ObjectModel;

namespace ICSharpCode.NRefactory
{
	static class EmptyList<T>
	{
		public static readonly ReadOnlyCollection<T> Instance = new ReadOnlyCollection<T>(new T[0]);
	}
}
