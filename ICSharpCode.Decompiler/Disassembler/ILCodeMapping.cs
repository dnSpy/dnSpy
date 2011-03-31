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
	static class ILCodeMapping
	{
		static ConcurrentDictionary<string, List<MemberMapping>> codeMappings = new ConcurrentDictionary<string, List<MemberMapping>>();
		
		/// <summary>
		/// Stores the source codes mappings: IL &lt;-&gt; editor lines
		/// </summary>
		public static ConcurrentDictionary<string, List<MemberMapping>> SourceCodeMappings {
			get { return codeMappings; }
			set { codeMappings = value; }
		}
	}
}
