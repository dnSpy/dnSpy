// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ICSharpCode.Decompiler.Disassembler
{
	/// <summary>
	/// Stores the IL code mappings.
	/// </summary>
	public static class ILCodeMapping
	{
		static ConcurrentDictionary<string, List<MethodMapping>> codeMappings = new ConcurrentDictionary<string, List<MethodMapping>>();
		
		/// <summary>
		/// Stores the source codes mappings: IL &lt;-&gt; editor lines
		/// </summary>
		public static ConcurrentDictionary<string, List<MethodMapping>> SourceCodeMappings {
			get { return codeMappings; }
			set { codeMappings = value; }
		}
	}
}
