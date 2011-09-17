// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public class ComposedType : AstType
	{
		public static readonly Role<VBTokenNode> NullableRole = new Role<VBTokenNode>("Nullable", VBTokenNode.Null);
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
				SetChildByRole(NullableRole, value ? new VBTokenNode(TextLocation.Empty, 1) : null);
			}
		}
		
		public AstNodeCollection<ArraySpecifier> ArraySpecifiers {
			get { return GetChildrenByRole (ArraySpecifierRole); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitComposedType (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			ComposedType o = other as ComposedType;
			return o != null && this.HasNullableSpecifier == o.HasNullableSpecifier && this.ArraySpecifiers.DoMatch(o.ArraySpecifiers, match);
		}
		
		public override string ToString()
		{
			StringBuilder b = new StringBuilder();
			b.Append(this.BaseType.ToString());
			if (this.HasNullableSpecifier)
				b.Append('?');
			foreach (var arraySpecifier in this.ArraySpecifiers) {
				b.Append('(');
				b.Append(',', arraySpecifier.Dimensions - 1);
				b.Append(')');
			}
			return b.ToString();
		}
		
		public override AstType MakeArrayType(int dimensions)
		{
			InsertChildBefore(this.ArraySpecifiers.FirstOrDefault(), new ArraySpecifier(dimensions), ArraySpecifierRole);
			return this;
		}
	}
	
	/// <summary>
	/// [,,,]
	/// </summary>
	public class ArraySpecifier : AstNode
	{
		public ArraySpecifier()
		{
		}
		
		public ArraySpecifier(int dimensions)
		{
			this.Dimensions = dimensions;
		}
		
		public VBTokenNode LParToken {
			get { return GetChildByRole (Roles.LPar); }
		}
		
		public int Dimensions {
			get { return 1 + GetChildrenByRole(Roles.Comma).Count(); }
			set {
				int d = this.Dimensions;
				while (d > value) {
					GetChildByRole(Roles.Comma).Remove();
					d--;
				}
				while (d < value) {
					InsertChildBefore(GetChildByRole(Roles.Comma), new VBTokenNode(TextLocation.Empty, 1), Roles.Comma);
					d++;
				}
			}
		}
		
		public VBTokenNode RParToken {
			get { return GetChildByRole (Roles.LPar); }
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
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
			return "(" + new string(',', this.Dimensions - 1) + ")";
		}
	}
}

