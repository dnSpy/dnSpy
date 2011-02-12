// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	public interface ICompilationUnit : IFreezable
	{
		string FileName {
			get;
			set;
		}
		
		bool ErrorsDuringCompile {
			get;
			set;
		}
		
		object Tag {
			get;
			set;
		}
		
		IProjectContent ProjectContent {
			get;
		}
		
		/// <summary>
		/// Gets the language this compilation unit was written in.
		/// </summary>
		LanguageProperties Language {
			get;
		}
		
		/// <summary>
		/// Gets the main using scope of the compilation unit.
		/// That scope usually represents the root namespace.
		/// </summary>
		IUsingScope UsingScope {
			get;
		}
		
		IList<IAttribute> Attributes {
			get;
		}
		
		IList<IClass> Classes {
			get;
		}
		
		IList<IComment> MiscComments {
			get;
		}
		
		IList<IComment> DokuComments {
			get;
		}
		
		IList<TagComment> TagComments {
			get;
		}
		
		IList<FoldingRegion> FoldingRegions {
			get;
		}
		
		/// <summary>
		/// Returns the innermost class in which the carret currently is, returns null
		/// if the carret is outside any class boundaries.
		/// </summary>
		IClass GetInnermostClass(int caretLine, int caretColumn);
	}
}
