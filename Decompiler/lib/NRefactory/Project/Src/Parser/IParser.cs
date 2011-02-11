// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Ast;

namespace ICSharpCode.NRefactory
{
	/// <summary>
	/// Parser interface.
	/// </summary>
	public interface IParser : IDisposable
	{
		Parser.Errors Errors {
			get;
		}
		
		Parser.ILexer Lexer {
			get;
		}
		
		CompilationUnit CompilationUnit {
			get;
		}
		
		bool ParseMethodBodies {
			get; set;
		}
		
		void Parse();
		
		Expression ParseExpression();
		TypeReference ParseTypeReference ();
		BlockStatement ParseBlock();
		List<INode> ParseTypeMembers();
	}
}
