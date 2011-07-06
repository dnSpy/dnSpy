// 
// StringBuilderOutputFormatter.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Text;

namespace ICSharpCode.NRefactory.CSharp.Refactoring
{
	public class StringBuilderOutputFormatter : IOutputFormatter
	{
		readonly StringBuilder sb = new StringBuilder ();
		int indentation;
		bool needsIndent = true;
		
		public int Length {
			get {
				WriteIndentation ();
				return sb.Length;
			}
		}
		
		public int Indentation {
			get {
				return this.indentation;
			}
			set {
				indentation = value;
			}
		}
		
		public string EolMarker {
			get;
			set;
		}
		
		public override string ToString ()
		{
			return sb.ToString ();
		}
		
		public void WriteIdentifier (string ident)
		{
			WriteIndentation ();
			sb.Append (ident);
		}
		
		public void WriteKeyword (string keyword)
		{
			WriteIndentation ();
			sb.Append (keyword);
		}
		
		public void WriteToken (string token)
		{
			WriteIndentation ();
			sb.Append (token);
		}
		
		public void Space ()
		{
			WriteIndentation ();
			sb.Append (' ');
		}
		
		public void OpenBrace (BraceStyle style)
		{
			WriteIndentation ();
			sb.Append (' ');
			sb.Append ('{');
			Indent ();
			NewLine ();
		}
		
		public void CloseBrace (BraceStyle style)
		{
			Unindent ();
			WriteIndentation ();
			sb.Append ('}');
		}
		
		void WriteIndentation ()
		{
			if (needsIndent) {
				needsIndent = false;
				for (int i = 0; i < indentation; i++) {
					sb.Append ('\t');
				}
			}
		}
		
		public void NewLine ()
		{
			sb.Append (EolMarker);
			needsIndent = true;
		}
		
		public void Indent ()
		{
			indentation++;
		}
		
		public void Unindent ()
		{
			indentation--;
		}
		
		public void WriteComment (CommentType commentType, string content)
		{
			WriteIndentation ();
			switch (commentType) {
			case CommentType.SingleLine:
				sb.Append ("//");
				sb.AppendLine (content);
				break;
			case CommentType.MultiLine:
				sb.Append ("/*");
				sb.Append (content);
				sb.Append ("*/");
				break;
			case CommentType.Documentation:
				sb.Append ("///");
				sb.AppendLine (content);
				break;
			}
		}
		
		public virtual void StartNode (AstNode node)
		{
		}
		
		public virtual void EndNode (AstNode node)
		{
		}
	}
}
