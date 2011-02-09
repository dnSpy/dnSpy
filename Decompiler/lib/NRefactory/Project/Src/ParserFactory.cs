// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="none" email=""/>
//     <version>$Revision$</version>
// </file>

using System;
using System.IO;
using System.Text;

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
			if (ext.Equals(".cs", StringComparison.InvariantCultureIgnoreCase))
				return CreateParser(SupportedLanguage.CSharp, new StreamReader(fileName, encoding));
			if (ext.Equals(".vb", StringComparison.InvariantCultureIgnoreCase))
				return CreateParser(SupportedLanguage.VBNet, new StreamReader(fileName, encoding));
			return null;
		}
	}
}
