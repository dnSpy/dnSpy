// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.VB.Ast
{
	public abstract class AttributedNode : AstNode
	{
		public AstNodeCollection<AttributeBlock> Attributes {
			get { return GetChildrenByRole(AttributeBlock.AttributeBlockRole); }
		}
		
		public static readonly Role<VBModifierToken> ModifierRole = new Role<VBModifierToken>("Modifier");
		
		public Modifiers Modifiers {
			get { return GetModifiers(this); }
			set { SetModifiers(this, value); }
		}
		
		public AstNodeCollection<VBModifierToken> ModifierTokens {
			get { return GetChildrenByRole (ModifierRole); }
		}
		
		internal static Modifiers GetModifiers(AstNode node)
		{
			Modifiers m = 0;
			foreach (VBModifierToken t in node.GetChildrenByRole (ModifierRole)) {
				m |= t.Modifier;
			}
			return m;
		}
		
		internal static void SetModifiers(AstNode node, Modifiers newValue)
		{
			Modifiers oldValue = GetModifiers(node);
			AstNode insertionPos = node.GetChildrenByRole(Attribute.AttributeRole).LastOrDefault();
			foreach (Modifiers m in VBModifierToken.AllModifiers) {
				if ((m & newValue) != 0) {
					if ((m & oldValue) == 0) {
						// Modifier was added
						var newToken = new VBModifierToken(TextLocation.Empty, m);
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
		
		protected bool MatchAttributesAndModifiers(AttributedNode o, PatternMatching.Match match)
		{
			return (this.Modifiers == Modifiers.Any || this.Modifiers == o.Modifiers) && this.Attributes.DoMatch(o.Attributes, match);
		}
	}
	

}
