// 
// EventDeclaration.cs
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
using System.Collections.Generic;
using System.ComponentModel;

using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp
{
	public class EventDeclaration : EntityDeclaration
	{
		public static readonly TokenRole EventKeywordRole = new TokenRole ("event");
		
		public override SymbolKind SymbolKind {
			get { return SymbolKind.Event; }
		}

		public CSharpTokenNode EventToken {
			get { return GetChildByRole (EventKeywordRole); }
		}

		public AstNodeCollection<VariableInitializer> Variables {
			get { return GetChildrenByRole (Roles.Variable); }
		}
		
		// Hide .Name and .NameToken from users; the actual field names
		// are stored in the VariableInitializer.
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override string Name {
			get { return string.Empty; }
			set { throw new NotSupportedException(); }
		}
		
		[EditorBrowsable(EditorBrowsableState.Never)]
		public override Identifier NameToken {
			get { return Identifier.Null; }
			set { throw new NotSupportedException(); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitEventDeclaration (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitEventDeclaration (this);
		}

		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitEventDeclaration (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			EventDeclaration o = other as EventDeclaration;
			return o != null && this.MatchAttributesAndModifiers(o, match)
				&& this.ReturnType.DoMatch(o.ReturnType, match) && this.Variables.DoMatch(o.Variables, match);
		}
	}
	
	public class CustomEventDeclaration : EntityDeclaration
	{
		public static readonly TokenRole EventKeywordRole = new TokenRole ("event");
		public static readonly TokenRole AddKeywordRole = new TokenRole ("add");
		public static readonly TokenRole RemoveKeywordRole = new TokenRole ("remove");
		
		public static readonly Role<Accessor> AddAccessorRole = new Role<Accessor>("AddAccessor", Accessor.Null);
		public static readonly Role<Accessor> RemoveAccessorRole = new Role<Accessor>("RemoveAccessor", Accessor.Null);
		
		public override SymbolKind SymbolKind {
			get { return SymbolKind.Event; }
		}
		
		/// <summary>
		/// Gets/Sets the type reference of the interface that is explicitly implemented.
		/// Null node if this member is not an explicit interface implementation.
		/// </summary>
		public AstType PrivateImplementationType {
			get { return GetChildByRole (PrivateImplementationTypeRole); }
			set { SetChildByRole (PrivateImplementationTypeRole, value); }
		}
		
		public CSharpTokenNode LBraceToken {
			get { return GetChildByRole (Roles.LBrace); }
		}
		
		public Accessor AddAccessor {
			get { return GetChildByRole (AddAccessorRole); }
			set { SetChildByRole (AddAccessorRole, value); }
		}
		
		public Accessor RemoveAccessor {
			get { return GetChildByRole (RemoveAccessorRole); }
			set { SetChildByRole (RemoveAccessorRole, value); }
		}
		
		public CSharpTokenNode RBraceToken {
			get { return GetChildByRole (Roles.RBrace); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitCustomEventDeclaration (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitCustomEventDeclaration (this);
		}

		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitCustomEventDeclaration (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			CustomEventDeclaration o = other as CustomEventDeclaration;
			return o != null && MatchString(this.Name, o.Name)
				&& this.MatchAttributesAndModifiers(o, match) && this.ReturnType.DoMatch(o.ReturnType, match)
				&& this.PrivateImplementationType.DoMatch(o.PrivateImplementationType, match)
				&& this.AddAccessor.DoMatch(o.AddAccessor, match) && this.RemoveAccessor.DoMatch(o.RemoveAccessor, match);
		}
	}
}
