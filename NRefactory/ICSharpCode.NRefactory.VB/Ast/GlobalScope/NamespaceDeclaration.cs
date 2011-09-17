// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ICSharpCode.NRefactory.PatternMatching;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Namespace Name
	/// 	Members
	/// End Namespace
	/// </summary>
	public class NamespaceDeclaration : AstNode
	{
		public static readonly Role<AstNode> MemberRole = CompilationUnit.MemberRole;
		
		public string Name {
			get {
				StringBuilder builder = new StringBuilder();
				foreach (Identifier identifier in GetChildrenByRole (Roles.Identifier)) {
					if (builder.Length > 0)
						builder.Append ('.');
					builder.Append (identifier.Name);
				}
				return builder.ToString ();
			}
			set {
				GetChildrenByRole(Roles.Identifier).ReplaceWith(value.Split('.').Select(ident => new Identifier (ident, TextLocation.Empty)));
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
		
		public AstNodeCollection<AstNode> Members {
			get { return GetChildrenByRole(MemberRole); }
		}
		
		public static string BuildQualifiedName (string name1, string name2)
		{
			if (string.IsNullOrEmpty (name1))
				return name2;
			if (string.IsNullOrEmpty (name2))
				return name1;
			return name1 + "." + name2;
		}
		
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitNamespaceDeclaration(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			NamespaceDeclaration o = other as NamespaceDeclaration;
			return o != null && MatchString(this.Name, o.Name) && this.Members.DoMatch(o.Members, match);
		}
	}
};
