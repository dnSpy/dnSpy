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
				ThrowIfFrozen();
				this.modifier = value; 
			}
		}

		public override TextLocation EndLocation {
			get {
				return new TextLocation (StartLocation.Line, StartLocation.Column + GetModifierLength (Modifier));
			}
		}

		public override string ToString(CSharpFormattingOptions formattingOptions)
		{
			return GetModifierName (Modifier);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			CSharpModifierToken o = other as CSharpModifierToken;
			return o != null && this.modifier == o.modifier;
		}
		
		// Not worth using a dictionary for such few elements.
		// This table is sorted in the order that modifiers should be output when generating code.
		static readonly Modifiers[] allModifiers = {
			Modifiers.Public, Modifiers.Protected, Modifiers.Private, Modifiers.Internal,
			Modifiers.New,
			Modifiers.Unsafe,
			Modifiers.Abstract, Modifiers.Virtual, Modifiers.Sealed, Modifiers.Static, Modifiers.Override,
			Modifiers.Readonly, Modifiers.Volatile,
			Modifiers.Extern, Modifiers.Partial, Modifiers.Const,
			Modifiers.Async,
			Modifiers.Any
		};
		
		public static IEnumerable<Modifiers> AllModifiers {
			get { return allModifiers; }
		}
		
		public CSharpModifierToken (TextLocation location, Modifiers modifier) : base (location, null)
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
				case Modifiers.Async:
					return "async";
				case Modifiers.Any:
					// even though it's used for pattern matching only, 'any' needs to be in this list to be usable in the AST
					return "any";
				default:
					throw new NotSupportedException("Invalid value for Modifiers");
			}
		}

		public static int GetModifierLength(Modifiers modifier)
		{
			switch (modifier) {
				case Modifiers.Private:
					return "private".Length;
				case Modifiers.Internal:
					return "internal".Length;
				case Modifiers.Protected:
					return "protected".Length;
				case Modifiers.Public:
					return "public".Length;
				case Modifiers.Abstract:
					return "abstract".Length;
				case Modifiers.Virtual:
					return "virtual".Length;
				case Modifiers.Sealed:
					return "sealed".Length;
				case Modifiers.Static:
					return "static".Length;
				case Modifiers.Override:
					return "override".Length;
				case Modifiers.Readonly:
					return "readonly".Length;
				case Modifiers.Const:
					return "const".Length;
				case Modifiers.New:
					return "new".Length;
				case Modifiers.Partial:
					return "partial".Length;
				case Modifiers.Extern:
					return "extern".Length;
				case Modifiers.Volatile:
					return "volatile".Length;
				case Modifiers.Unsafe:
					return "unsafe".Length;
				case Modifiers.Async:
					return "async".Length;
				case Modifiers.Any:
					// even though it's used for pattern matching only, 'any' needs to be in this list to be usable in the AST
					return "any".Length;
				default:
					throw new NotSupportedException("Invalid value for Modifiers");
			}
		}
		
		public static Modifiers GetModifierValue(string modifier)
		{
			switch (modifier) {
				case "private":
					return Modifiers.Private;
				case "internal":
					return Modifiers.Internal;
				case "protected":
					return Modifiers.Protected;
				case "public":
					return Modifiers.Public;
				case "abstract":
					return Modifiers.Abstract;
				case "virtual":
					return Modifiers.Virtual;
				case "sealed":
					return Modifiers.Sealed;
				case "static":
					return Modifiers.Static;
				case "override":
					return Modifiers.Override;
				case "readonly":
					return Modifiers.Readonly;
				case "const":
					return Modifiers.Const;
				case "new":
					return Modifiers.New;
				case "partial":
					return Modifiers.Partial;
				case "extern":
					return Modifiers.Extern;
				case "volatile":
					return Modifiers.Volatile;
				case "unsafe":
					return Modifiers.Unsafe;
				case "async":
					return Modifiers.Async;
				case "any":
					// even though it's used for pattern matching only, 'any' needs to be in this list to be usable in the AST
					return Modifiers.Any;
				default:
					throw new NotSupportedException("Invalid value for Modifiers");
			}
		}
	}
}