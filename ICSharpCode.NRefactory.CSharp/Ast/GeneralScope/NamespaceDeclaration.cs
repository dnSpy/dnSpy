// 
// NamespaceDeclaration.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// namespace Name { Members }
	/// </summary>
	public class NamespaceDeclaration : AstNode
	{
		public static readonly Role<AstNode> MemberRole = CompilationUnit.MemberRole;
		
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public string Name {
			get {
				StringBuilder builder = new StringBuilder ();
				foreach (Identifier identifier in GetChildrenByRole (Roles.Identifier)) {
					if (builder.Length > 0)
						builder.Append ('.');
					builder.Append (identifier.Name);
				}
				return builder.ToString ();
			}
			set {
				GetChildrenByRole(Roles.Identifier).ReplaceWith(value.Split('.').Select(ident => Identifier.Create (ident, TextLocation.Empty)));
			}
		}
		
		public AstNodeCollection<Identifier> Identifiers {
			get { return GetChildrenByRole (Roles.Identifier); }
		}
		
		/// <summary>
		/// Gets the full namespace name (including any parent namespaces)
		/// </summary>
		public string FullName {
			get {
				NamespaceDeclaration parentNamespace = Parent as NamespaceDeclaration;
				if (parentNamespace != null)
					return BuildQualifiedName (parentNamespace.FullName, Name);
				return Name;
			}
		}
		
		public CSharpTokenNode LBraceToken {
			get { return GetChildByRole (Roles.LBrace); }
		}
		
		public AstNodeCollection<AstNode> Members {
			get { return GetChildrenByRole(MemberRole); }
		}
		
		public CSharpTokenNode RBraceToken {
			get { return GetChildByRole (Roles.RBrace); }
		}
		
		public NamespaceDeclaration ()
		{
		}
		
		public NamespaceDeclaration (string name)
		{
			this.Name = name;
		}
		
		public static string BuildQualifiedName (string name1, string name2)
		{
			if (string.IsNullOrEmpty (name1))
				return name2;
			if (string.IsNullOrEmpty (name2))
				return name1;
			return name1 + "." + name2;
		}
		
		public void AddMember (AstNode child)
		{
			AddChild (child, MemberRole);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data = default(T))
		{
			return visitor.VisitNamespaceDeclaration (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			NamespaceDeclaration o = other as NamespaceDeclaration;
			return o != null && MatchString(this.Name, o.Name) && this.Members.DoMatch(o.Members, match);
		}
	}
};
