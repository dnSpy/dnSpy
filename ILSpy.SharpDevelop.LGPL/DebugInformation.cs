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
		static DecompiledLanguages language;
		
		/// <summary>
		/// Gets or sets the decompiled language.
		/// </summary>
		public static DecompiledLanguages Language {
			get { return language; }
			set {
				var oldLanguage = language;
				if (value != language) {
					language = value;
					OnLanguageChanged(new LanguageEventArgs(oldLanguage, language));
				}
			}
		}
		
		/// <summary>
		/// List of loaded assemblies.
		/// </summary>
		public static IEnumerable<AssemblyDefinition> LoadedAssemblies { get; set; }
		
		/// <summary>
		/// Gets or sets the current code mappings.
		/// </summary>
		public static Dictionary<int, List<MemberMapping>> CodeMappings { get; set; }
		
		/// <summary>
		/// Gets or sets the local variables of the current decompiled type, method, etc.
		/// </summary>
		public static ConcurrentDictionary<int, IEnumerable<ILVariable>> LocalVariables { get; set; }
		
		/// <summary>
		/// Gets or sets the old code mappings.
		/// </summary>
		public static Dictionary<int, List<MemberMapping>> OldCodeMappings { get; set; }
		
		/// <summary>
		/// Gets or sets the MembeReference that was decompiled (a TypeDefinition, MethodDefinition, etc)
		/// </summary>
		public static Dictionary<int, MemberReference> DecompiledMemberReferences { get; set; }
		
		/// <summary>
		/// Gets or sets the current token, IL offset and member reference. Used for step in/out.
		/// </summary>
		public static Tuple<int, int, MemberReference> DebugStepInformation { get; set; }
		
		/// <summary>
		/// Gets or sets whether the debugger is loaded.
		/// </summary>
		public static bool IsDebuggerLoaded { get; set; }
		
		/// <summary>
		/// Occures when the language is changed.
		/// </summary>
		public static event EventHandler<LanguageEventArgs> LanguageChanged;
		
		private static void OnLanguageChanged(LanguageEventArgs e)
		{
			var handler = LanguageChanged;
			if (handler != null) {
				handler(null, e);
			}
		}
	}
	
	public class LanguageEventArgs : EventArgs
	{
		public DecompiledLanguages OldLanguage { get; private set; }
		
		public DecompiledLanguages NewLanguage { get; private set; }
		
		public LanguageEventArgs(DecompiledLanguages oldLanguage, DecompiledLanguages newLanguage)
		{
			this.OldLanguage = oldLanguage;
			this.NewLanguage = newLanguage;
		}
	}
}
