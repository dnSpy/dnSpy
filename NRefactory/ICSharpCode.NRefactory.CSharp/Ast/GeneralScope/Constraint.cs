// 
// Constraint.cs
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

using System.Collections.Generic;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// where TypeParameter : BaseTypes
	/// </summary>
	/// <remarks>
	/// new(), struct and class constraints are represented using a PrimitiveType "new", "struct" or "class"
	/// </remarks>
	public class Constraint : AstNode
	{
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}

		public CSharpTokenNode WhereKeyword {
			get { return GetChildByRole (Roles.WhereKeyword); }
		}

		public SimpleType TypeParameter {
			get {
				return GetChildByRole (Roles.ConstraintTypeParameter);
			}
			set {
				SetChildByRole(Roles.ConstraintTypeParameter, value);
			}
		}
		
		public AstNodeCollection<AstType> BaseTypes {
			get {
				return GetChildrenByRole(Roles.BaseType); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitConstraint (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitConstraint (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitConstraint (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			Constraint o = other as Constraint;
			return o != null && this.TypeParameter.DoMatch (o.TypeParameter, match) && this.BaseTypes.DoMatch(o.BaseTypes, match);
		}
	}
}

