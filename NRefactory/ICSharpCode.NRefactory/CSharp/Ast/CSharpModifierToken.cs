// 
// CSharpModifierToken.cs
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

namespace ICSharpCode.NRefactory.CSharp
{
	
	public class CSharpModifierToken : CSharpTokenNode
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
			CSharpModifierToken o = other as CSharpModifierToken;
			return o != null && this.modifier == o.modifier;
		}
		
		// Not worth using a dictionary for such few elements.
		// This table is sorted in the order that modifiers should be output when generating code.
		static readonly List<KeyValuePair<Modifiers, int>> lengthTable = new List<KeyValuePair<Modifiers, int>> () {
			new KeyValuePair<Modifiers, int>(Modifiers.Public, "public".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Protected, "protected".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Private, "private".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Internal, "internal".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.New, "new".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Unsafe, "unsafe".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Abstract, "abstract".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Virtual, "virtual".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Sealed, "sealed".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Static, "static".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Override, "override".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Readonly, "readonly".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Volatile, "volatile".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Extern, "extern".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Partial, "partial".Length),
			new KeyValuePair<Modifiers, int>(Modifiers.Const, "const".Length)
		};
		
		public static IEnumerable<Modifiers> AllModifiers {
			get { return lengthTable.Select(p => p.Key); }
		}
		
		public CSharpModifierToken (AstLocation location, Modifiers modifier) : base (location, 0)
		{
			this.Modifier = modifier;
		}
		
		public static string GetModifierName(Modifiers modifier)
		{
			switch (modifier) {
				case Modifiers.Private:
					return "private";
				case Modifiers.Internal:
					return "internal";
				case Modifiers.Protected:
					return "protected";
				case Modifiers.Public:
					return "public";
				case Modifiers.Abstract:
					return "abstract";
				case Modifiers.Virtual:
					return "virtual";
				case Modifiers.Sealed:
					return "sealed";
				case Modifiers.Static:
					return "static";
				case Modifiers.Override:
					return "override";
				case Modifiers.Readonly:
					return "readonly";
				case Modifiers.Const:
					return "const";
				case Modifiers.New:
					return "new";
				case Modifiers.Partial:
					return "partial";
				case Modifiers.Extern:
					return "extern";
				case Modifiers.Volatile:
					return "volatile";
				case Modifiers.Unsafe:
					return "unsafe";
				case Modifiers.Fixed:
					return "fixed";
				default:
					throw new NotSupportedException("Invalid value for Modifiers");
			}
		}
	}
}