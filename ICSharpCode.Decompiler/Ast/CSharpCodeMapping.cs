// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using ICSharpCode.Decompiler;

namespace ICSharpCode.Decompiler.Ast
{
	/// <summary>
	/// Stores the C# code mappings.
	/// </summary>
	public static class CSharpCodeMapping
	{
		static ConcurrentDictionary<string, List<MemberMapping>> codeMappings = new ConcurrentDictionary<string, List<MemberMapping>>();
		
		/// <summary>
		/// Stores the source codes mappings: CSharp &lt;-&gt; editor lines
		/// </summary>
		public static ConcurrentDictionary<string, List<MemberMapping>> SourceCodeMappings {
			get { return codeMappings; }
			set { codeMappings = value; }
		}
	}
}
