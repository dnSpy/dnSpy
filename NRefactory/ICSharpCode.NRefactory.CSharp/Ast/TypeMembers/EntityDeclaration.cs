// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.CSharp
{
	public abstract class EntityDeclaration : AstNode
	{
		public static readonly Role<AttributeSection> AttributeRole = new Role<AttributeSection>("Attribute");
		public static readonly Role<AttributeSection> UnattachedAttributeRole = new Role<AttributeSection>("UnattachedAttribute");
		public static readonly Role<CSharpModifierToken> ModifierRole = new Role<CSharpModifierToken>("Modifier");
		public static readonly Role<AstType> PrivateImplementationTypeRole = new Role<AstType>("PrivateImplementationType", AstType.Null);
		
		public override NodeType NodeType {
			get { return NodeType.Member; }
		}
		
		public abstract NRefactory.TypeSystem.SymbolKind SymbolKind { get; }
		
		public AstNodeCollection<AttributeSection> Attributes {
			get { return base.GetChildrenByRole (AttributeRole); }
		}
		
		public Modifiers Modifiers {
			get { return GetModifiers(this); }
			set { SetModifiers(this, value); }
		}
		
		public bool HasModifier (Modifiers mod)
		{
			return (Modifiers & mod) == mod;
		}
		
		public IEnumerable<CSharpModifierToken> ModifierTokens {
			get { return GetChildrenByRole (ModifierRole); }
		}
		
		public virtual string Name {
			get {
				return GetChildByRole (Roles.Identifier).Name;
			}
			set {
				SetChildByRole (Roles.Identifier, Identifier.Create (value, TextLocation.Empty));
			}
		}
		
		public virtual Identifier NameToken {
			get { return GetChildByRole (Roles.Identifier); }
			set { SetChildByRole (Roles.Identifier, value); }
		}
		
		public virtual AstType ReturnType {
			get { return GetChildByRole (Roles.Type); }
			set { SetChildByRole(Roles.Type, value); }
		}

		public CSharpTokenNode SemicolonToken {
			get { return GetChildByRole (Roles.Semicolon); }
		}

		internal static Modifiers GetModifiers(AstNode node)
		{
			Modifiers m = 0;
			foreach (CSharpModifierToken t in node.GetChildrenByRole (ModifierRole)) {
				m |= t.Modifier;
			}
			return m;
		}
		
		internal static void SetModifiers(AstNode node, Modifiers newValue)
		{
			Modifiers oldValue = GetModifiers(node);
			AstNode insertionPos = node.GetChildrenByRole(AttributeRole).LastOrDefault();
			foreach (Modifiers m in CSharpModifierToken.AllModifiers) {
				if ((m & newValue) != 0) {
					if ((m & oldValue) == 0) {
						// Modifier was added
						var newToken = new CSharpModifierToken(TextLocation.Empty, m);
						node.InsertChildAfter(insertionPos, newToken, ModifierRole);
						insertionPos = newToken;
					} else {
						// Modifier already exists
						insertionPos = node.GetChildrenByRole(ModifierRole).First(t => t.Modifier == m);
					}
				} else {
					if ((m & oldValue) != 0) {
						// Modifier was removed
						node.GetChildrenByRole (ModifierRole).First(t => t.Modifier == m).Remove();
					}
				}
			}
		}
		
		protected bool MatchAttributesAndModifiers (EntityDeclaration o, PatternMatching.Match match)
		{
			return (this.Modifiers == Modifiers.Any || this.Modifiers == o.Modifiers) && this.Attributes.DoMatch (o.Attributes, match);
		}
	}
}
