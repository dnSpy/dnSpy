// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB
{
	/// <summary>
	/// Output formatter for the Output visitor.
	/// </summary>
	public interface IOutputFormatter
	{
		void StartNode(AstNode node);
		void EndNode(AstNode node);
		
		/// <summary>
		/// Writes an identifier.
		/// If the identifier conflicts with a keyword, the output visitor will
		/// call <c>WriteToken("[")</c> before and <c>WriteToken("]")</c> after calling WriteIdentifier().
		/// </summary>
		void WriteIdentifier(string identifier);
		
		/// <summary>
		/// Writes a keyword to the output.
		/// </summary>
		void WriteKeyword(string keyword);
		
		/// <summary>
		/// Writes a token to the output.
		/// </summary>
		void WriteToken(string token);
		void Space();
		
		void Indent();
		void Unindent();
		
		void NewLine();
		
		void WriteComment(bool isDocumentation, string content);
		
		void MarkFoldStart();
		void MarkFoldEnd();
	}
}
