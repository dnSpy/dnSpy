// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

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
	
	/// <summary>
	/// The snippet parser supports parsing code snippets that are not valid as a full compilation unit.
	/// </summary>
	public class SnippetParser
	{
		/// <summary>
		/// Gets the errors of the last call to Parse(). Returns null if parse was not yet called.
		/// </summary>
		public Errors Errors { get; private set; }
		
		/// <summary>
		/// Gets the specials of the last call to Parse(). Returns null if parse was not yet called.
		/// </summary>
		public List<ISpecial> Specials { get; private set; }
		
		/// <summary>
		/// Gets the snippet type of the last call to Parse(). Returns None if parse was not yet called.
		/// </summary>
		public SnippetType SnippetType { get; private set; }
		
		/// <summary>
		/// Parse the code. The result may be a CompilationUnit, an Expression, a list of statements or a list of class
		/// members.
		/// </summary>
		public INode Parse(string code)
		{
			VBParser parser = ParserFactory.CreateParser(new StringReader(code));
			parser.Parse();
			this.Errors = parser.Errors;
			this.Specials = parser.Lexer.SpecialTracker.RetrieveSpecials();
			this.SnippetType = SnippetType.CompilationUnit;
			INode result = parser.CompilationUnit;
			
			if (this.Errors.Count > 0) {
				parser = ParserFactory.CreateParser(new StringReader(code));
				Expression expression = parser.ParseExpression();
				if (expression != null && parser.Errors.Count < this.Errors.Count) {
					this.Errors = parser.Errors;
					this.Specials = parser.Lexer.SpecialTracker.RetrieveSpecials();
					this.SnippetType = SnippetType.Expression;
					result = expression;
				}
			}
			if (this.Errors.Count > 0) {
				parser = ParserFactory.CreateParser(new StringReader(code));
				BlockStatement block = parser.ParseBlock();
				if (block != null && parser.Errors.Count < this.Errors.Count) {
					this.Errors = parser.Errors;
					this.Specials = parser.Lexer.SpecialTracker.RetrieveSpecials();
					this.SnippetType = SnippetType.Statements;
					result = block;
				}
			}
			if (this.Errors.Count > 0) {
				parser = ParserFactory.CreateParser(new StringReader(code));
				List<INode> members = parser.ParseTypeMembers();
				if (members != null && members.Count > 0 && parser.Errors.Count < this.Errors.Count) {
					this.Errors = parser.Errors;
					this.Specials = parser.Lexer.SpecialTracker.RetrieveSpecials();
					this.SnippetType = SnippetType.TypeMembers;
					result = new NodeListNode(members);
					result.StartLocation = members[0].StartLocation;
					result.EndLocation = members[members.Count - 1].EndLocation;
				}
			}
			Debug.Assert(result is CompilationUnit || !result.StartLocation.IsEmpty);
			Debug.Assert(result is CompilationUnit || !result.EndLocation.IsEmpty);
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
			
			public Location StartLocation { get; set; }
			public Location EndLocation { get; set; }
			
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
