// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Output formatter for the Output visitor.
	/// </summary>
	public interface IOutputFormatter
	{
		void WriteIdentifier(string ident);
		void WriteKeyword(string keyword);
		void WriteToken(string token);
		void Space();
		
		void OpenBrace(BraceStyle style);
		void CloseBrace(BraceStyle style);
		
		void Indent();
		void Unindent();
		
		void NewLine();
		
		void WriteComment(CommentType commentType, string content);
	}
}
