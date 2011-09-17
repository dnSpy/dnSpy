// 
// AbstractMember.cs
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

namespace ICSharpCode.NRefactory.CSharp
{
	public abstract class MemberDeclaration : AttributedNode
	{
		public static readonly Role<AstType> PrivateImplementationTypeRole = new Role<AstType>("PrivateImplementationType", AstType.Null);
		
		public AstType ReturnType {
			get { return GetChildByRole (Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}
		
		/// <summary>
		/// Only supported on members that can be declared in an interface.
		/// </summary>
		public AstType PrivateImplementationType {
			get { return GetChildByRole (PrivateImplementationTypeRole); }
			set { SetChildByRole (PrivateImplementationTypeRole, value); }
		}
		
		public string Name {
			get {
				return GetChildByRole (Roles.Identifier).Name;
			}
			set {
				SetChildByRole (Roles.Identifier, Identifier.Create (value, TextLocation.Empty));
			}
		}
		
		public Identifier NameToken {
			get {
				return GetChildByRole (Roles.Identifier);
			}
			set {
				SetChildByRole (Roles.Identifier, value);
			}
		}
		
		public override NodeType NodeType {
			get { return NodeType.Member; }
		}
		
		protected bool MatchMember(MemberDeclaration o, PatternMatching.Match match)
		{
			return MatchAttributesAndModifiers(o, match) && this.ReturnType.DoMatch(o.ReturnType, match)
				&& this.PrivateImplementationType.DoMatch(o.PrivateImplementationType, match) && MatchString(this.Name, o.Name);
		}
	}
}
