// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

using ICSharpCode.NRefactory.VB.Ast;
using ICSharpCode.NRefactory.VB.Parser;

namespace ICSharpCode.NRefactory.VB
{
	public enum SnippetType
	{
		None,
		CompilationUnit,
		Expression,
		Statements,
		TypeMembers
	}
	
	public class VBParser
	{
		bool hasErrors;
		Errors errors;
		
		public CompilationUnit Parse(string content)
		{
			return Parse(new StringReader(content));
		}
		
		public CompilationUnit Parse(TextReader reader)
		{
			var parser = new ICSharpCode.NRefactory.VB.Parser.VBParser(new VBLexer(reader));
			parser.Parse();
			hasErrors = parser.Errors.Count > 0;
			errors = parser.Errors;
			return parser.CompilationUnit;
		}
		
		public AstNode ParseSnippet(TextReader reader)
		{
			throw new NotImplementedException();
		}
		
		public bool HasErrors {
			get { return hasErrors; }
		}
		
		public Errors Errors {
			get { return errors; }
		}
	}
}