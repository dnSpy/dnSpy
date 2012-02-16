// 
// ComposedType.cs
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
using System.Linq;
using System.Text;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp
{
	public class ComposedType : AstType
	{
		public static readonly Role<CSharpTokenNode> NullableRole = new Role<CSharpTokenNode>("Nullable", CSharpTokenNode.Null);
		public static readonly Role<CSharpTokenNode> PointerRole = new Role<CSharpTokenNode>("Pointer", CSharpTokenNode.Null);
		public static readonly Role<ArraySpecifier> ArraySpecifierRole = new Role<ArraySpecifier>("ArraySpecifier");
		
		public AstType BaseType {
			get { return GetChildByRole(Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}
		
		public bool HasNullableSpecifier {
			get {
				return !GetChildByRole(NullableRole).IsNull;
			}
			set {
				SetChildByRole(NullableRole, value ? new CSharpTokenNode(TextLocation.Empty, 1) : null);
			}
		}
		
		public int PointerRank {
			get {
				return GetChildrenByRole(PointerRole).Count;
			}
			set {
				if (value < 0)
					throw new ArgumentOutOfRangeException();
				int d = this.PointerRank;
				while (d > value) {
					GetChildByRole(PointerRole).Remove();
					d--;
				}
				while (d < value) {
					InsertChildBefore(GetChildByRole(PointerRole), new CSharpTokenNode(TextLocation.Empty, 1), PointerRole);
					d++;
				}
			}
		}
		
		public AstNodeCollection<ArraySpecifier> ArraySpecifiers {
			get { return GetChildrenByRole (ArraySpecifierRole); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data = default(T))
		{
			return visitor.VisitComposedType (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ComposedType o = other as ComposedType;
			return o != null && this.HasNullableSpecifier == o.HasNullableSpecifier && this.PointerRank == o.PointerRank && this.ArraySpecifiers.DoMatch(o.ArraySpecifiers, match);
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder();
			b.Append(this.BaseType.ToString());
			if (this.HasNullableSpecifier)
				b.Append('?');
			b.Append('*', this.PointerRank);
			foreach (var arraySpecifier in this.ArraySpecifiers) {
				b.Append('[');
				b.Append(',', arraySpecifier.Dimensions - 1);
				b.Append(']');
			}
			return b.ToString();
		}
		
		public override AstType MakePointerType()
		{
			if (ArraySpecifiers.Any()) {
				return base.MakePointerType();
			} else {
				this.PointerRank++;
				return this;
			}
		}
		
		public override AstType MakeArrayType(int dimensions)
		{
			InsertChildBefore(this.ArraySpecifiers.FirstOrDefault(), new ArraySpecifier(dimensions), ArraySpecifierRole);
			return this;
		}
		
		public override ITypeReference ToTypeReference(SimpleNameLookupMode lookupMode = SimpleNameLookupMode.Type)
		{
			ITypeReference t = this.BaseType.ToTypeReference(lookupMode);
			if (this.HasNullableSpecifier) {
				t = NullableType.Create(t);
			}
			int pointerRank = this.PointerRank;
			for (int i = 0; i < pointerRank; i++) {
				t = new PointerTypeReference(t);
			}
			foreach (var a in this.ArraySpecifiers.Reverse()) {
				t = new ArrayTypeReference(t, a.Dimensions);
			}
			return t;
		}
	}
	
	/// <summary>
	/// [,,,]
	/// </summary>
	public class ArraySpecifier : AstNode
	{
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public ArraySpecifier()
		{
		}
		
		public ArraySpecifier(int dimensions)
		{
			this.Dimensions = dimensions;
		}
		
		public CSharpTokenNode LBracketToken {
			get { return GetChildByRole (Roles.LBracket); }
		}
		
		public int Dimensions {
			get { return 1 + GetChildrenByRole(Roles.Comma).Count; }
			set {
				int d = this.Dimensions;
				while (d > value) {
					GetChildByRole(Roles.Comma).Remove();
					d--;
				}
				while (d < value) {
					InsertChildBefore(GetChildByRole(Roles.Comma), new CSharpTokenNode(TextLocation.Empty, 1), Roles.Comma);
					d++;
				}
			}
		}
		
		public CSharpTokenNode RBracketToken {
			get { return GetChildByRole (Roles.RBracket); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data = default(T))
		{
			return visitor.VisitArraySpecifier(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ArraySpecifier o = other as ArraySpecifier;
			return o != null && this.Dimensions == o.Dimensions;
		}
		
		public override string ToString()
		{
			return "[" + new string(',', this.Dimensions - 1) + "]";
		}
	}
}

