// 
// FullTypeName.cs
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
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp
{
	public class SimpleType : AstType
	{
		#region Null
		public new static readonly SimpleType Null = new NullSimpleType ();
		
		sealed class NullSimpleType : SimpleType
		{
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override void AcceptVisitor (IAstVisitor visitor)
			{
				visitor.VisitNullNode(this);
			}
			
			public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
			{
				return visitor.VisitNullNode(this);
			}
			
			public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
			{
				return visitor.VisitNullNode(this, data);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
			
			public override ITypeReference ToTypeReference(NameLookupMode lookupMode, InterningProvider interningProvider)
			{
				return SpecialType.UnknownType;
			}
		}
		#endregion
		
		public SimpleType()
		{
		}
		
		public SimpleType(string identifier)
		{
			this.Identifier = identifier;
		}
		
		public SimpleType (Identifier identifier)
		{
			this.IdentifierToken = identifier;
		}
		
		public SimpleType(string identifier, TextLocation location)
		{
			SetChildByRole (Roles.Identifier, CSharp.Identifier.Create (identifier, location));
		}
		
		public SimpleType (string identifier, IEnumerable<AstType> typeArguments)
		{
			this.Identifier = identifier;
			foreach (var arg in typeArguments) {
				AddChild (arg, Roles.TypeArgument);
			}
		}
		
		public SimpleType (string identifier, params AstType[] typeArguments) : this (identifier, (IEnumerable<AstType>)typeArguments)
		{
		}
		
		public string Identifier {
			get {
				return GetChildByRole (Roles.Identifier).Name;
			}
			set {
				SetChildByRole (Roles.Identifier, CSharp.Identifier.Create (value));
			}
		}
		
		public Identifier IdentifierToken {
			get {
				return GetChildByRole (Roles.Identifier);
			}
			set {
				SetChildByRole (Roles.Identifier, value);
			}
		}
		
		public AstNodeCollection<AstType> TypeArguments {
			get { return GetChildrenByRole (Roles.TypeArgument); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitSimpleType (this);
		}
		
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitSimpleType (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitSimpleType (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			SimpleType o = other as SimpleType;
			return o != null && MatchString(this.Identifier, o.Identifier) && this.TypeArguments.DoMatch(o.TypeArguments, match);
		}
		
		public override ITypeReference ToTypeReference(NameLookupMode lookupMode, InterningProvider interningProvider = null)
		{
			if (interningProvider == null)
				interningProvider = InterningProvider.Dummy;
			var typeArguments = new List<ITypeReference>();
			foreach (var ta in this.TypeArguments) {
				typeArguments.Add(ta.ToTypeReference(lookupMode, interningProvider));
			}
			string identifier = interningProvider.Intern(this.Identifier);
			if (typeArguments.Count == 0 && string.IsNullOrEmpty(identifier)) {
				// empty SimpleType is used for typeof(List<>).
				return SpecialType.UnboundTypeArgument;
			}
			var t = new SimpleTypeOrNamespaceReference(identifier, interningProvider.InternList(typeArguments), lookupMode);
			return interningProvider.Intern(t);
		}
	}
}

