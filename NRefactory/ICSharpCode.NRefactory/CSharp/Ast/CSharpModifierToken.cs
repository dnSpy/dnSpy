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

namespace ICSharpCode.NRefactory.CSharp
{
	
	public class CSharpModifierToken : CSharpTokenNode
	{
		Modifiers modifier;
		
		public Modifiers Modifier {
			get { return modifier; }
			set {
				modifier = value;
				if (!lengthTable.TryGetValue (modifier, out tokenLength))
					throw new InvalidOperationException ("Modifier " + modifier + " is invalid.");
			}
		}
		
		static Dictionary<Modifiers, int> lengthTable = new Dictionary<Modifiers, int> () {
			{ Modifiers.Public, "public".Length },
			{ Modifiers.Protected, "protected".Length },
			{ Modifiers.Private, "private".Length },
			{ Modifiers.Internal, "internal".Length },
			{ Modifiers.New, "new".Length },
			{ Modifiers.Unsafe, "unsafe".Length },
			{ Modifiers.Abstract, "abstract".Length },
			{ Modifiers.Virtual, "virtual".Length },
			{ Modifiers.Sealed, "sealed".Length },
			{ Modifiers.Static, "static".Length },
			{ Modifiers.Override, "override".Length },
			{ Modifiers.Readonly, "readonly".Length },
			{ Modifiers.Volatile, "volatile".Length },
			{ Modifiers.Extern, "extern".Length },
			{ Modifiers.Partial, "partial".Length },
		};
		
		public static ICollection<Modifiers> AllModifiers {
			get { return lengthTable.Keys; }
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