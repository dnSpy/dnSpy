// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using System.Text;
using ICSharpCode.NRefactory.Parser;

namespace ICSharpCode.NRefactory
{
	public enum SupportedLanguage {
		CSharp,
		VBNet
	}
	
	/// <summary>
	/// Static helper class that constructs lexer and parser objects.
	/// </summary>
	public static class ParserFactory
	{
		public static Parser.ILexer CreateLexer(SupportedLanguage language, TextReader textReader)
		{
			switch (language) {
				case SupportedLanguage.CSharp:
					return new ICSharpCode.NRefactory.Parser.CSharp.Lexer(textReader);
				case SupportedLanguage.VBNet:
					return new ICSharpCode.NRefactory.Parser.VB.Lexer(textReader);
			}
			throw new System.NotSupportedException(language + " not supported.");
		}
		
		public static Parser.ILexer CreateLexer(SupportedLanguage language, TextReader textReader, LexerMemento state)
		{
			switch (language) {
				case SupportedLanguage.CSharp:
					//return new ICSharpCode.NRefactory.Parser.CSharp.Lexer(textReader, state);
					throw new System.NotSupportedException("C# Lexer does not support loading a previous state.");
				case SupportedLanguage.VBNet:
					return new ICSharpCode.NRefactory.Parser.VB.Lexer(textReader, state);
			}
			throw new System.NotSupportedException(language + " not supported.");
		}
		
		public static IParser CreateParser(SupportedLanguage language, TextReader textReader)
		{
			Parser.ILexer lexer = CreateLexer(language, textReader);
			switch (language) {
				case SupportedLanguage.CSharp:
					return new ICSharpCode.NRefactory.Parser.CSharp.Parser(lexer);
				case SupportedLanguage.VBNet:
					return new ICSharpCode.NRefactory.Parser.VB.Parser(lexer);
			}
			throw new System.NotSupportedException(language + " not supported.");
		}
		
		public static IParser CreateParser(string fileName)
		{
			return CreateParser(fileName, Encoding.UTF8);
		}
		
		public static IParser CreateParser(string fileName, Encoding encoding)
		{
			string ext = Path.GetExtension(fileName);
			if (ext.Equals(".cs", StringComparison.OrdinalIgnoreCase))
				return CreateParser(SupportedLanguage.CSharp, new StreamReader(fileName, encoding));
			if (ext.Equals(".vb", StringComparison.OrdinalIgnoreCase))
				return CreateParser(SupportedLanguage.VBNet, new StreamReader(fileName, encoding));
			return null;
		}
	}
}
