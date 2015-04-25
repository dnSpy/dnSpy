// 
// SyntaxTree.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using System.Threading;
using Mono.CSharp;
using System.IO;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.NRefactory.CSharp
{
	[Obsolete("CompilationUnit was renamed to SyntaxTree", true)]
	public class CompilationUnit {}
	
	public class SyntaxTree : AstNode
	{
		public static readonly Role<AstNode> MemberRole = new Role<AstNode>("Member", AstNode.Null);
		
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		string fileName;
		
		/// <summary>
		/// Gets/Sets the file name of this syntax tree.
		/// </summary>
		public string FileName {
			get { return fileName; }
			set {
				ThrowIfFrozen();
				fileName = value;
			}
		}
		
		public AstNodeCollection<AstNode> Members {
			get { return GetChildrenByRole(MemberRole); }
		}

		IList<string> conditionalSymbols = null;

		List<Error> errors = new List<Error> ();
		
		public List<Error> Errors {
			get { return errors; }
		}


		/// <summary>
		/// Gets the conditional symbols used to parse the source file. Note that this list contains
		/// the conditional symbols at the start of the first token in the file - including the ones defined
		/// in the source file.
		/// </summary>
		public IList<string> ConditionalSymbols {
			get {
				return conditionalSymbols ?? EmptyList<string>.Instance;
			}
			internal set {
				conditionalSymbols = value;
			}
		}

		/// <summary>
		/// Gets the expression that was on top of the parse stack.
		/// This is the only way to get an expression that isn't part of a statment.
		/// (eg. when an error follows an expression).
		/// 
		/// This is used for code completion to 'get the expression before a token - like ., &lt;, ('.
		/// </summary>
		public AstNode TopExpression {
			get;
			internal set;
		}
		
		public SyntaxTree ()
		{
		}
		
		/// <summary>
		/// Gets all defined types in this syntax tree.
		/// </summary>
		/// <returns>
		/// A list containing <see cref="TypeDeclaration"/> or <see cref="DelegateDeclaration"/> nodes.
		/// </returns>
		public IEnumerable<EntityDeclaration> GetTypes(bool includeInnerTypes = false)
		{
			Stack<AstNode> nodeStack = new Stack<AstNode> ();
			nodeStack.Push(this);
			while (nodeStack.Count > 0) {
				var curNode = nodeStack.Pop();
				if (curNode is TypeDeclaration || curNode is DelegateDeclaration) {
					yield return (EntityDeclaration)curNode;
				}
				foreach (var child in curNode.Children) {
					if (!(child is Statement || child is Expression) &&
					    (child.Role != Roles.TypeMemberRole || ((child is TypeDeclaration || child is DelegateDeclaration) && includeInnerTypes)))
						nodeStack.Push (child);
				}
			}
		}

		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			SyntaxTree o = other as SyntaxTree;
			return o != null && this.Members.DoMatch(o.Members, match);
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitSyntaxTree (this);
		}
		
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitSyntaxTree (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSyntaxTree (this, data);
		}
		
		/// <summary>
		/// Converts this syntax tree into a parsed file that can be stored in the type system.
		/// </summary>
		public CSharpUnresolvedFile ToTypeSystem ()
		{
			if (string.IsNullOrEmpty (this.FileName))
				throw new InvalidOperationException ("Cannot use ToTypeSystem() on a syntax tree without file name.");
			var v = new TypeSystemConvertVisitor (this.FileName);
			v.VisitSyntaxTree (this);
			return v.UnresolvedFile;
		}
		
		public static SyntaxTree Parse (string program, string fileName = "", CompilerSettings settings = null, CancellationToken cancellationToken = default (CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			var parser = new CSharpParser (settings);
			return parser.Parse (program, fileName);
		}
		
		public static SyntaxTree Parse (TextReader reader, string fileName = "", CompilerSettings settings = null, CancellationToken cancellationToken = default (CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			var parser = new CSharpParser (settings);
			return parser.Parse (reader, fileName);
		}
		
		public static SyntaxTree Parse (Stream stream, string fileName = "", CompilerSettings settings = null, CancellationToken cancellationToken = default (CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			var parser = new CSharpParser (settings);
			return parser.Parse (stream, fileName);
		}
		
		public static SyntaxTree Parse (ITextSource textSource, string fileName = "", CompilerSettings settings = null, CancellationToken cancellationToken = default (CancellationToken))
		{
			cancellationToken.ThrowIfCancellationRequested();
			var parser = new CSharpParser (settings);
			return parser.Parse (textSource, fileName);
		}
	}
}
