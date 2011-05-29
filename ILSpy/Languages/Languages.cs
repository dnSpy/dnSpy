// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition.Hosting;
using System.Linq;

namespace ICSharpCode.ILSpy
{
	public static class Languages
	{
		private static ReadOnlyCollection<Language> allLanguages;

		/// <summary>
		/// A list of all languages.
		/// </summary>
		public static ReadOnlyCollection<Language> AllLanguages
		{
			get { return allLanguages; }
		}

		internal static void Initialize(CompositionContainer composition)
		{
			List<Language> languages = new List<Language>();
			languages.AddRange(composition.GetExportedValues<Language>());
			languages.Add(new ILLanguage(true));
			#if DEBUG
			languages.AddRange(ILAstLanguage.GetDebugLanguages());
			languages.AddRange(CSharpLanguage.GetDebugLanguages());
			#endif
			allLanguages = languages.AsReadOnly();
		}

		/// <summary>
		/// Gets a language using its name.
		/// If the language is not found, C# is returned instead.
		/// </summary>
		public static Language GetLanguage(string name)
		{
			return AllLanguages.FirstOrDefault(l => l.Name == name) ?? AllLanguages.First();
		}
	}
}
