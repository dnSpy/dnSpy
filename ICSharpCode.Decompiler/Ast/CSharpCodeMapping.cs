// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.Decompiler;

namespace Decompiler
{
	/// <summary>
	/// Stores the C# code mappings.
	/// </summary>
	public static class CSharpCodeMapping
	{
		static Dictionary<string, List<MethodMapping>> codeMappings = new Dictionary<string, List<MethodMapping>>();
		
		/// <summary>
		/// Stores the source codes mappings: CSharp &lt;-&gt; editor lines
		/// </summary>
		public static Dictionary<string, List<MethodMapping>> SourceCodeMappings {
			get { return codeMappings; }
			set { codeMappings = value; }
		}
	}
}
