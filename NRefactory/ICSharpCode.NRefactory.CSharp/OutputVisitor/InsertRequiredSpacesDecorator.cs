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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp;

namespace ICSharpCode.NRefactory.CSharp
{
	class InsertRequiredSpacesDecorator : DecoratingTokenWriter
	{
		/// <summary>
		/// Used to insert the minimal amount of spaces so that the lexer recognizes the tokens that were written.
		/// </summary>
		LastWritten lastWritten;
		
		enum LastWritten
		{
			Whitespace,
			Other,
			KeywordOrIdentifier,
			Plus,
			Minus,
			Ampersand,
			QuestionMark,
			Division
		}
		
		public InsertRequiredSpacesDecorator(TokenWriter writer)
			: base(writer)
		{
		}
		
		public override void WriteIdentifier(Identifier identifier)
		{
			if (identifier.IsVerbatim || CSharpOutputVisitor.IsKeyword(identifier.Name, identifier)) {
				if (lastWritten == LastWritten.KeywordOrIdentifier) {
					// this space is not strictly required, so we call Space()
					Space();
				}
			} else if (lastWritten == LastWritten.KeywordOrIdentifier) {
				// this space is strictly required, so we directly call the formatter
				base.Space();
			}
			base.WriteIdentifier(identifier);
			lastWritten = LastWritten.KeywordOrIdentifier;
		}
		
		public override void WriteKeyword(Role role, string keyword)
		{
			if (lastWritten == LastWritten.KeywordOrIdentifier) {
				Space();
			}
			base.WriteKeyword(role, keyword);
			lastWritten = LastWritten.KeywordOrIdentifier;
		}
		
		public override void WriteToken(Role role, string token)
		{
			// Avoid that two +, - or ? tokens are combined into a ++, -- or ?? token.
			// Note that we don't need to handle tokens like = because there's no valid
			// C# program that contains the single token twice in a row.
			// (for +, - and &, this can happen with unary operators;
			// for ?, this can happen in "a is int? ? b : c" or "a as int? ?? 0";
			// and for /, this can happen with "1/ *ptr" or "1/ //comment".)
			if (lastWritten == LastWritten.Plus && token[0] == '+' ||
			    lastWritten == LastWritten.Minus && token[0] == '-' ||
			    lastWritten == LastWritten.Ampersand && token[0] == '&' ||
			    lastWritten == LastWritten.QuestionMark && token[0] == '?' ||
			    lastWritten == LastWritten.Division && token[0] == '*') {
				base.Space();
			}
			base.WriteToken(role, token);
			if (token == "+") {
				lastWritten = LastWritten.Plus;
			} else if (token == "-") {
				lastWritten = LastWritten.Minus;
			} else if (token == "&") {
				lastWritten = LastWritten.Ampersand;
			} else if (token == "?") {
				lastWritten = LastWritten.QuestionMark;
			} else if (token == "/") {
				lastWritten = LastWritten.Division;
			} else {
				lastWritten = LastWritten.Other;
			}
		}
		
		public override void Space()
		{
			base.Space();
			lastWritten = LastWritten.Whitespace;
		}
		
		public override void NewLine()
		{
			base.NewLine();
			lastWritten = LastWritten.Whitespace;
		}
		
		public override void WriteComment(CommentType commentType, string content)
		{
			if (lastWritten == LastWritten.Division) {
				// When there's a comment starting after a division operator
				// "1.0 / /*comment*/a", then we need to insert a space in front of the comment.
				base.Space();
			}
			base.WriteComment(commentType, content);
			lastWritten = LastWritten.Whitespace;
		}
		
		public override void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument)
		{
			base.WritePreProcessorDirective(type, argument);
			lastWritten = LastWritten.Whitespace;
		}
		
		public override void WritePrimitiveValue(object value, string literalValue = null)
		{
			base.WritePrimitiveValue(value, literalValue);
			if (value == null || value is bool)
				return;
			if (value is string) {
				lastWritten = LastWritten.Other;
			} else if (value is char) {
				lastWritten = LastWritten.Other;
			} else if (value is decimal) {
				lastWritten = LastWritten.Other;
			} else if (value is float) {
				float f = (float)value;
				if (float.IsInfinity(f) || float.IsNaN(f)) return;
				lastWritten = LastWritten.Other;
			} else if (value is double) {
				double f = (double)value;
				if (double.IsInfinity(f) || double.IsNaN(f)) return;
				// needs space if identifier follows number;
				// this avoids mistaking the following identifier as type suffix
				lastWritten = LastWritten.KeywordOrIdentifier;
			} else if (value is IFormattable) {
				// needs space if identifier follows number;
				// this avoids mistaking the following identifier as type suffix
				lastWritten = LastWritten.KeywordOrIdentifier;
			} else {
				lastWritten = LastWritten.Other;
			}
		}
		
		public override void WritePrimitiveType(string type)
		{
			if (lastWritten == LastWritten.KeywordOrIdentifier) {
				Space();
			}
			base.WritePrimitiveType(type);
			if (type == "new") {
				lastWritten = LastWritten.Other;
			} else {
				lastWritten = LastWritten.KeywordOrIdentifier;
			}
		}
	}
}