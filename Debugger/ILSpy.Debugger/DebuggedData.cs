// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.Decompiler;
using Mono.Cecil;

namespace ILSpy.Debugger
{
	/// <summary>
	/// Contains the data important for debugger from the main application.
	/// </summary>
	public static class DebugData
	{
		static DecompiledLanguages language;
		
		/// <summary>
		/// Gets or sets the current debugged type
		/// </summary>
		public static TypeDefinition CurrentType { get; set; }

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
