// 
// UsingDeclaration.cs
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

using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// using Import;
	/// </summary>
	public class UsingDeclaration : AstNode
	{
		public static readonly TokenRole UsingKeywordRole = new TokenRole ("using");
		public static readonly Role<AstType> ImportRole = new Role<AstType>("Import", AstType.Null);
		
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public CSharpTokenNode UsingToken {
			get { return GetChildByRole (UsingKeywordRole); }
		}
		
		public AstType Import {
			get { return GetChildByRole (ImportRole); }
			set { SetChildByRole (ImportRole, value); }
		}
		
		public string Namespace {
			get { return ConstructNamespace (Import); }
		}

		internal static string ConstructNamespace (AstType type)
		{
			var stack = new Stack<string>();
			while (type is MemberType) {
				var mt = (MemberType)type;
				stack.Push(mt.MemberName);
				type = mt.Target;
				if (mt.IsDoubleColon) {
					stack.Push("::");
				} else {
					stack.Push("."); 
				}
			}
			if (type is SimpleType)
				stack.Push(((SimpleType)type).Identifier);

			var result = new StringBuilder();
			while (stack.Count > 0)
				result.Append(stack.Pop());
			return result.ToString();
		}
		
		public CSharpTokenNode SemicolonToken {
			get { return GetChildByRole (Roles.Semicolon); }
		}
		
		public UsingDeclaration ()
		{
		}
		
		public UsingDeclaration (string nameSpace)
		{
			AddChild (AstType.Create (nameSpace), ImportRole);
		}
		
		public UsingDeclaration (AstType import)
		{
			AddChild (import, ImportRole);
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitUsingDeclaration (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitUsingDeclaration (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitUsingDeclaration (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			UsingDeclaration o = other as UsingDeclaration;
			return o != null && this.Import.DoMatch(o.Import, match);
		}
	}
}
