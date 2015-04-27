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
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp
{
	public class MemberType : AstType
	{
		public static readonly Role<AstType> TargetRole = new Role<AstType>("Target", AstType.Null);
		
		bool isDoubleColon;
		
		public bool IsDoubleColon {
			get { return isDoubleColon; }
			set {
				ThrowIfFrozen();
				isDoubleColon = value;
			}
		}
		
		public AstType Target {
			get { return GetChildByRole(TargetRole); }
			set { SetChildByRole(TargetRole, value); }
		}
		
		public string MemberName {
			get {
				return GetChildByRole (Roles.Identifier).Name;
			}
			set {
				SetChildByRole (Roles.Identifier, Identifier.Create (value));
			}
		}
		
		public Identifier MemberNameToken {
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
		
		public MemberType ()
		{
		}
		
		public MemberType (AstType target, string memberName)
		{
			this.Target = target;
			this.MemberName = memberName;
		}
		
		public MemberType (AstType target, string memberName, IEnumerable<AstType> typeArguments)
		{
			this.Target = target;
			this.MemberName = memberName;
			foreach (var arg in typeArguments) {
				AddChild (arg, Roles.TypeArgument);
			}
		}
		
		public MemberType (AstType target, string memberName, params AstType[] typeArguments) : this (target, memberName, (IEnumerable<AstType>)typeArguments)
		{
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitMemberType (this);
		}
		
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitMemberType (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitMemberType (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			MemberType o = other as MemberType;
			return o != null && this.IsDoubleColon == o.IsDoubleColon
				&& MatchString(this.MemberName, o.MemberName) && this.Target.DoMatch(o.Target, match)
				&& this.TypeArguments.DoMatch(o.TypeArguments, match);
		}

		public override ITypeReference ToTypeReference(NameLookupMode lookupMode, InterningProvider interningProvider = null)
		{
			if (interningProvider == null)
				interningProvider = InterningProvider.Dummy;
			
			TypeOrNamespaceReference t;
			if (this.IsDoubleColon) {
				SimpleType st = this.Target as SimpleType;
				if (st != null) {
					t = interningProvider.Intern(new AliasNamespaceReference(interningProvider.Intern(st.Identifier)));
				} else {
					t = null;
				}
			} else {
				t = this.Target.ToTypeReference(lookupMode, interningProvider) as TypeOrNamespaceReference;
			}
			if (t == null)
				return SpecialType.UnknownType;
			var typeArguments = new List<ITypeReference>();
			foreach (var ta in this.TypeArguments) {
				typeArguments.Add(ta.ToTypeReference(lookupMode, interningProvider));
			}
			string memberName = interningProvider.Intern(this.MemberName);
			return interningProvider.Intern(new MemberTypeOrNamespaceReference(t, memberName, interningProvider.InternList(typeArguments), lookupMode));
		}
	}
}

