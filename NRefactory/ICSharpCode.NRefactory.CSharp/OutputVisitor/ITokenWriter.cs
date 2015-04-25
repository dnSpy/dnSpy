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
using System.IO;

namespace ICSharpCode.NRefactory.CSharp
{
	public abstract class TokenWriter
	{
		public abstract void StartNode(AstNode node);
		public abstract void EndNode(AstNode node);
		
		/// <summary>
		/// Writes an identifier.
		/// </summary>
		public abstract void WriteIdentifier(Identifier identifier);
		
		/// <summary>
		/// Writes a keyword to the output.
		/// </summary>
		public abstract void WriteKeyword(Role role, string keyword);
		
		/// <summary>
		/// Writes a token to the output.
		/// </summary>
		public abstract void WriteToken(Role role, string token);
		
		/// <summary>
		/// Writes a primitive/literal value
		/// </summary>
		public abstract void WritePrimitiveValue(object value, string literalValue = null);
		
		public abstract void WritePrimitiveType(string type);
		
		public abstract void Space();
		public abstract void Indent();
		public abstract void Unindent();
		public abstract void NewLine();
		
		public abstract void WriteComment(CommentType commentType, string content);
		public abstract void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument);
		
		public static TokenWriter Create(TextWriter writer, string indentation = "\t")
		{
			return new InsertSpecialsDecorator(new InsertRequiredSpacesDecorator(new TextWriterTokenWriter(writer) { IndentationString = indentation }));
		}
		
		public static TokenWriter CreateWriterThatSetsLocationsInAST(TextWriter writer, string indentation = "\t")
		{
			var target = new TextWriterTokenWriter(writer) { IndentationString = indentation };
			return new InsertSpecialsDecorator(new InsertRequiredSpacesDecorator(new InsertMissingTokensDecorator(target, target)));
		}
		
		public static TokenWriter WrapInWriterThatSetsLocationsInAST(TokenWriter writer)
		{
			if (!(writer is ILocatable))
				throw new InvalidOperationException("writer does not provide locations!");
			return new InsertSpecialsDecorator(new InsertRequiredSpacesDecorator(new InsertMissingTokensDecorator(writer, (ILocatable)writer)));
		}
	}
	
	public interface ILocatable
	{
		TextLocation Location { get; }
	}
	
	public abstract class DecoratingTokenWriter : TokenWriter
	{
		TokenWriter decoratedWriter;
		
		protected DecoratingTokenWriter(TokenWriter decoratedWriter)
		{
			if (decoratedWriter == null)
				throw new ArgumentNullException("decoratedWriter");
			this.decoratedWriter = decoratedWriter;
		}
		
		public override void StartNode(AstNode node)
		{
			decoratedWriter.StartNode(node);
		}
		
		public override void EndNode(AstNode node)
		{
			decoratedWriter.EndNode(node);
		}
		
		public override void WriteIdentifier(Identifier identifier)
		{
			decoratedWriter.WriteIdentifier(identifier);
		}
		
		public override void WriteKeyword(Role role, string keyword)
		{
			decoratedWriter.WriteKeyword(role, keyword);
		}
		
		public override void WriteToken(Role role, string token)
		{
			decoratedWriter.WriteToken(role, token);
		}
		
		public override void WritePrimitiveValue(object value, string literalValue = null)
		{
			decoratedWriter.WritePrimitiveValue(value, literalValue);
		}
		
		public override void WritePrimitiveType(string type)
		{
			decoratedWriter.WritePrimitiveType(type);
		}
		
		public override void Space()
		{
			decoratedWriter.Space();
		}
		
		public override void Indent()
		{
			decoratedWriter.Indent();
		}
		
		public override void Unindent()
		{
			decoratedWriter.Unindent();
		}
		
		public override void NewLine()
		{
			decoratedWriter.NewLine();
		}
		
		public override void WriteComment(CommentType commentType, string content)
		{
			decoratedWriter.WriteComment(commentType, content);
		}
		
		public override void WritePreProcessorDirective(PreProcessorDirectiveType type, string argument)
		{
			decoratedWriter.WritePreProcessorDirective(type, argument);
		}
	}
}


