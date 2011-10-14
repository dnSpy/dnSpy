// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.Debugger
{
	/// <summary>
	/// Contains the data important for debugger from the main application.
	/// </summary>
	public static class DebugInformation
	{
		/// <summary>
		/// List of loaded assemblies.
		/// </summary>
		public static IEnumerable<AssemblyDefinition> LoadedAssemblies { get; set; }
		
		/// <summary>
		/// Gets or sets the current code mappings.
		/// </summary>
		public static Dictionary<int, MemberMapping> CodeMappings { get; set; }
		
		/// <summary>
		/// Gets or sets the current token, IL offset and member reference. Used for step in/out.
		/// </summary>
		public static Tuple<int, int, MemberReference> DebugStepInformation { get; set; }
		
		/// <summary>
		/// Gets or sets whether the debugger is loaded.
		/// </summary>
		public static bool IsDebuggerLoaded { get; set; }
	}
}
