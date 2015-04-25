// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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
using ICSharpCode.NRefactory.TypeSystem.Implementation;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// C# compiler settings.
	/// </summary>
	[Serializable]
	public class CompilerSettings : AbstractFreezable
	{
		protected override void FreezeInternal()
		{
			conditionalSymbols = FreezableHelper.FreezeList(conditionalSymbols);
			specificWarningsAsErrors = FreezableHelper.FreezeList(specificWarningsAsErrors);
			disabledWarnings = FreezableHelper.FreezeList(disabledWarnings);
			base.FreezeInternal();
		}
		
		/// <summary>
		/// Creates a new CompilerSettings instance.
		/// </summary>
		public CompilerSettings()
		{
		}
		
		bool allowUnsafeBlocks = true;
		
		/// <summary>
		/// Gets/Sets whether <c>unsafe</c> code is allowed.
		/// The default is <c>true</c>. If set to false, parsing unsafe code will result in parser errors.
		/// </summary>
		public bool AllowUnsafeBlocks {
			get { return allowUnsafeBlocks; }
			set {
				FreezableHelper.ThrowIfFrozen(this);
				allowUnsafeBlocks = value;
			}
		}
		
		bool checkForOverflow;
		
		/// <summary>
		/// Gets/Sets whether overflow checking is enabled.
		/// The default is <c>false</c>. This setting effects semantic analysis.
		/// </summary>
		public bool CheckForOverflow {
			get { return checkForOverflow; }
			set { checkForOverflow = value; }
		}
		
		Version languageVersion = new Version((int)Mono.CSharp.LanguageVersion.Default, 0);
		
		/// <summary>
		/// Gets/Sets the language version used by the parser.
		/// Using language constructs newer than the supplied version will result in parser errors.
		/// </summary>
		public Version LanguageVersion {
			get { return languageVersion; }
			set {
				FreezableHelper.ThrowIfFrozen(this);
				if (value == null)
					throw new ArgumentNullException();
				languageVersion = value;
			}
		}
		
		IList<string> conditionalSymbols = new List<string>();
		
		/// <summary>
		/// Gets/Sets the list of conditional symbols that are defined project-wide.
		/// </summary>
		public IList<string> ConditionalSymbols {
			get { return conditionalSymbols; }
		}
		
		bool treatWarningsAsErrors;
		
		public bool TreatWarningsAsErrors {
			get { return treatWarningsAsErrors; }
			set {
				FreezableHelper.ThrowIfFrozen(this);
				treatWarningsAsErrors = value;
			}
		}
		
		IList<int> specificWarningsAsErrors = new List<int>();
		
		/// <summary>
		/// Allows treating specific warnings as errors without setting <see cref="TreatWarningsAsErrors"/> to true.
		/// </summary>
		public IList<int> SpecificWarningsAsErrors {
			get { return specificWarningsAsErrors; }
		}
		
		int warningLevel = 4;
		
		public int WarningLevel {
			get { return warningLevel; }
			set {
				FreezableHelper.ThrowIfFrozen(this);
				warningLevel = value;
			}
		}
		
		IList<int> disabledWarnings = new List<int>();
		
		/// <summary>
		/// Disables the specified warnings.
		/// </summary>
		public IList<int> DisabledWarnings {
			get { return disabledWarnings; }
		}
		
		internal Mono.CSharp.CompilerSettings ToMono()
		{
			var s = new Mono.CSharp.CompilerSettings();
			s.Unsafe = allowUnsafeBlocks;
			s.Checked = checkForOverflow;
			s.Version = (Mono.CSharp.LanguageVersion)languageVersion.Major;
			s.WarningsAreErrors = treatWarningsAsErrors;
			s.WarningLevel = warningLevel;
			foreach (int code in disabledWarnings)
				s.SetIgnoreWarning(code);
			foreach (int code in specificWarningsAsErrors)
				s.AddWarningAsError(code);
			foreach (string sym in conditionalSymbols)
				s.AddConditionalSymbol(sym);
			return s;
		}
	}
}
