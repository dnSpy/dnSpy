// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Daniel Grunwald"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.IO;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Parser;

namespace ICSharpCode.NRefactory
{
	/// <summary>
	/// The snippet parser supports parsing code snippets that are not valid as a full compilation unit.
	/// </summary>
	public class SnippetParser
	{
		readonly SupportedLanguage language;
		
		public SnippetParser(SupportedLanguage language)
		{
			this.language = language;
		}
		
		Errors errors;
		List<ISpecial> specials;
		
		/// <summary>
		/// Gets the errors of the last call to Parse(). Returns null if parse was not yet called.
		/// </summary>
		public Errors Errors {
			get { return errors; }
		}
		
		/// <summary>
		/// Gets the specials of the last call to Parse(). Returns null if parse was not yet called.
		/// </summary>
		public List<ISpecial> Specials {
			get { return specials; }
		}
		
		/// <summary>
		/// Parse the code. The result may be a CompilationUnit, an Expression, a list of statements or a list of class
		/// members.
		/// </summary>
		public INode Parse(string code)
		{
			IParser parser = ParserFactory.CreateParser(language, new StringReader(code));
			parser.Parse();
			errors = parser.Errors;
			specials = parser.Lexer.SpecialTracker.RetrieveSpecials();
			INode result = parser.CompilationUnit;
			
			if (errors.Count > 0) {
				if (language == SupportedLanguage.CSharp) {
					// SEMICOLON HACK : without a trailing semicolon, parsing expressions does not work correctly
					parser = ParserFactory.CreateParser(language, new StringReader(code + ";"));
				} else {
					parser = ParserFactory.CreateParser(language, new StringReader(code));
				}
				Expression expression = parser.ParseExpression();
				if (expression != null && parser.Errors.Count < errors.Count) {
					errors = parser.Errors;
					specials = parser.Lexer.SpecialTracker.RetrieveSpecials();
					result = expression;
				}
			}
			if (errors.Count > 0) {
				parser = ParserFactory.CreateParser(language, new StringReader(code));
				BlockStatement block = parser.ParseBlock();
				if (block != null && parser.Errors.Count < errors.Count) {
					errors = parser.Errors;
					specials = parser.Lexer.SpecialTracker.RetrieveSpecials();
					result = block;
				}
			}
			if (errors.Count > 0) {
				parser = ParserFactory.CreateParser(language, new StringReader(code));
				List<INode> members = parser.ParseTypeMembers();
				if (members != null && members.Count > 0 && parser.Errors.Count < errors.Count) {
					errors = parser.Errors;
					specials = parser.Lexer.SpecialTracker.RetrieveSpecials();
					result = new NodeListNode(members);
				}
			}
			return result;
		}
		
		sealed class NodeListNode : INode
		{
			List<INode> nodes;
			
			public NodeListNode(List<INode> nodes)
			{
				this.nodes = nodes;
			}
			
			public INode Parent {
				get { return null; }
				set { throw new NotSupportedException(); }
			}
			
			public List<INode> Children {
				get { return nodes; }
			}
			
			public Location StartLocation {
				get { return Location.Empty; }
				set { throw new NotSupportedException(); }
			}
			
			public Location EndLocation {
				get { return Location.Empty; }
				set { throw new NotSupportedException(); }
			}
			
			public object UserData { get; set; }
			
			public object AcceptChildren(IAstVisitor visitor, object data)
			{
				foreach (INode n in nodes) {
					n.AcceptVisitor(visitor, data);
				}
				return null;
			}
			
			public object AcceptVisitor(IAstVisitor visitor, object data)
			{
				return AcceptChildren(visitor, data);
			}
		}
	}
}
