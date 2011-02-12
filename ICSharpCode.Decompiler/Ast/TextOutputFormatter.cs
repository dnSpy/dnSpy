// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using ICSharpCode.Decompiler;
using ICSharpCode.NRefactory.CSharp;

namespace Decompiler
{
	public class TextOutputFormatter : IOutputFormatter
	{
		readonly ITextOutput output;
		
		public TextOutputFormatter(ITextOutput output)
		{
			if (output == null)
				throw new ArgumentNullException("output");
			this.output = output;
		}
		
		public void WriteIdentifier(string identifier)
		{
			output.Write(identifier);
		}
		
		public void WriteKeyword(string keyword)
		{
			output.Write(keyword);
		}
		
		public void WriteToken(string token)
		{
			output.Write(token);
		}
		
		public void Space()
		{
			output.Write(' ');
		}
		
		public void OpenBrace(BraceStyle style)
		{
			output.MarkFoldStart();
			output.WriteLine(" {");
			output.Indent();
		}
		
		public void CloseBrace(BraceStyle style)
		{
			output.Unindent();
			output.Write('}');
			output.MarkFoldEnd();
		}
		
		public void Indent()
		{
			output.Indent();
		}
		
		public void Unindent()
		{
			output.Unindent();
		}
		
		public void NewLine()
		{
			output.WriteLine();
		}
		
		public void WriteComment(CommentType commentType, string content)
		{
			switch (commentType) {
				case CommentType.SingleLine:
					output.Write("//");
					output.WriteLine(content);
					break;
				case CommentType.MultiLine:
					output.Write("/*");
					output.Write(content);
					output.Write("*/");
					break;
				case CommentType.Documentation:
					output.Write("///");
					output.WriteLine(content);
					break;
			}
		}
	}
}
