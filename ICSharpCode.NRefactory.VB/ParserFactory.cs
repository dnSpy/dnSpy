// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using System.Text;
using ICSharpCode.NRefactory.VB.Parser;

namespace ICSharpCode.NRefactory.VB
{
	/// <summary>
	/// Static helper class that constructs lexer and parser objects.
	/// </summary>
	public static class ParserFactory
	{
		public static VBLexer CreateLexer(TextReader textReader)
		{
			return new ICSharpCode.NRefactory.VB.Parser.VBLexer(textReader);
		}
		
		public static VBLexer CreateLexer(TextReader textReader, VBLexerMemento state)
		{
			return new ICSharpCode.NRefactory.VB.Parser.VBLexer(textReader, state);
		}
		
		public static VBParser CreateParser(TextReader textReader)
		{
			Parser.VBLexer lexer = CreateLexer(textReader);
			return new ICSharpCode.NRefactory.VB.Parser.VBParser(lexer);
		}
		
		public static VBParser CreateParser(string fileName)
		{
			return CreateParser(fileName, Encoding.UTF8);
		}
		
		public static VBParser CreateParser(string fileName, Encoding encoding)
		{
			string ext = Path.GetExtension(fileName);
			if (ext.Equals(".vb", StringComparison.OrdinalIgnoreCase))
				return CreateParser(new StreamReader(fileName, encoding));
			return null;
		}
	}
}
