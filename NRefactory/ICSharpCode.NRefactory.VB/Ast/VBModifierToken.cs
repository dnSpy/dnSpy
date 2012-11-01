// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Description of VBModifierToken.
	/// </summary>
	public class VBModifierToken : VBTokenNode
	{
		Modifiers modifier;
		
		public Modifiers Modifier {
			get { return modifier; }
			set {
				for (int i = 0; i < lengthTable.Count; i++) {
					if (lengthTable[i].Key == value) {
						this.modifier = value;
						this.tokenLength = lengthTable[i].Value;
						return;
					}
				}
				throw new ArgumentException ("Modifier " + value + " is invalid.");
			}
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			VBModifierToken o = other as VBModifierToken;
			return o != null && this.modifier == o.modifier;
		}
		
		// Not worth using a dictionary for such few elements.
		// This table is sorted in the order that modifiers should be output when generating code.
		static readonly List<KeyValuePair<Modifiers, int>> lengthTable = new List<KeyValuePair<Modifiers, int>> () {
			new KeyValuePair<Modifiers, int>(Modifiers.Public, "Public".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Protected, "Protected".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Private, "Private".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Friend, "Friend".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.MustInherit, "MustInherit".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.MustOverride, "MustOverride".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Overridable, "Overridable".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.NotInheritable, "NotInheritable".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.NotOverridable, "NotOverridable".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Dim, "Dim".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Const, "Const".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Shared, "Shared".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Static, "Static".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Overrides, "Overrides".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.ReadOnly, "ReadOnly".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.WriteOnly, "WriteOnly".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Shadows, "Shadows".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Partial, "Partial".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Overloads, "Overloads".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.WithEvents, "WithEvents".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Default, "Default".Length),
			// parameter modifiers
			new KeyValuePair<Modifiers, int>(Modifiers.Optional, "Optional".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.ByVal, "ByVal".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.ByRef, "ByRef".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.ParamArray, "ParamArray".Length),
			// operator modifiers
			new KeyValuePair<Modifiers, int>(Modifiers.Narrowing, "Narrowing".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Widening, "Widening".Length),
			// VB 11 modifiers
			new KeyValuePair<Modifiers, int>(Modifiers.Async, "Async".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Iterator, "Iterator".Length),
			// even though it's used for patterns only, it needs to be in this table to be usable in the AST
			new KeyValuePair<Modifiers, int>(Modifiers.Any, "Any".Length)
		};
		
		public static IEnumerable<Modifiers> AllModifiers {
			get { return lengthTable.Select(p => p.Key); }
		}
		
		public VBModifierToken(TextLocation location, Modifiers modifier) : base (location, 0)
		{
			this.Modifier = modifier;
		}
		
		public static string GetModifierName(Modifiers modifier)
		{
			switch (modifier) {
				case Modifiers.Private:
					return "Private";
				case Modifiers.Friend:
					return "Friend";
				case Modifiers.Protected:
					return "Protected";
				case Modifiers.Public:
					return "Public";
				case Modifiers.MustInherit:
					return "MustInherit";
				case Modifiers.MustOverride:
					return "MustOverride";
				case Modifiers.Overridable:
					return "Overridable";
				case Modifiers.NotInheritable:
					return "NotInheritable";
				case Modifiers.NotOverridable:
					return "NotOverridable";
				case Modifiers.Const:
					return "Const";
				case Modifiers.Shared:
					return "Shared";
				case Modifiers.Static:
					return "Static";
				case Modifiers.Overrides:
					return "Overrides";
				case Modifiers.ReadOnly:
					return "ReadOnly";
				case Modifiers.Shadows:
					return "Shadows";
				case Modifiers.Partial:
					return "Partial";
				case Modifiers.Overloads:
					return "Overloads";
				case Modifiers.WithEvents:
					return "WithEvents";
				case Modifiers.Default:
					return "Default";
				case Modifiers.Dim:
					return "Dim";
				case Modifiers.WriteOnly:
					return "WriteOnly";
				case Modifiers.Optional:
					return "Optional";
				case Modifiers.ByVal:
					return "ByVal";
				case Modifiers.ByRef:
					return "ByRef";
				case Modifiers.ParamArray:
					return "ParamArray";
				case Modifiers.Widening:
					return "Widening";
				case Modifiers.Narrowing:
					return "Narrowing";
				case Modifiers.Async:
					return "Async";
				case Modifiers.Iterator:
					return "Iterator";
				default:
					throw new NotSupportedException("Invalid value for Modifiers: " + modifier);
			}
		}
	}
}
