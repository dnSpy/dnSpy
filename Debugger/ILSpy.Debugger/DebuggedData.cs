// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
	public static class DebugData
	{
		static DecompiledLanguages language;
		
		/// <summary>
		/// Gets or sets the current debugged member reference. Can be a type or a member of a type (method, property).
		/// </summary>
		public static MemberReference CurrentMemberReference { get; set; }

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
		/// Returns true if the CurrentMember is a type (TypeDefinition). Otherwise, returns false (is MethodDefinition or PropertyDefinition).
		/// </summary>
		public static bool IsCurrentMemberReferenceType {
			get {
				return CurrentMemberReference is TypeDefinition;
			}
		}
		
		/// <summary>
		/// Gets or sets the current code mappings.
		/// </summary>
		public static Tuple<string, List<MemberMapping>> CodeMappings { get; set; }
		
		/// <summary>
		/// Gets or sets the local variables of the current decompiled type, method, etc.
		/// </summary>
		public static ConcurrentDictionary<int, IEnumerable<ILVariable>> LocalVariables { get; set; }
		
		/// <summary>
		/// Gets or sets the old code mappings.
		/// </summary>
		public static Tuple<string, List<MemberMapping>> OldCodeMappings { get; set; }
		
		/// <summary>
		/// Occures when the language is changed.
		/// </summary>
		public static event EventHandler<LanguageEventArgs> LanguageChanged;
		
		private static void OnLanguageChanged(LanguageEventArgs e)
		{
			if (LanguageChanged != null) {
				LanguageChanged(null, e);
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
