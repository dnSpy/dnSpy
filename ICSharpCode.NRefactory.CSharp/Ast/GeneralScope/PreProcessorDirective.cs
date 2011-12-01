// 
// PreProcessorDirective.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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

namespace ICSharpCode.NRefactory.CSharp
{
	public enum PreProcessorDirectiveType : byte
	{
		Invalid = 0,
		Region = 1,
		Endregion = 2,
		
		If = 3,
		Endif = 4,
		Elif = 5,
		Else = 6,
		
		Define = 7,
		Undef = 8,
		Error = 9,
		Warning = 10,
		Pragma = 11,
		Line = 12
	}
	
	public class PreProcessorDirective : AstNode, IRelocatable
	{
		public override NodeType NodeType {
			get {
				return NodeType.Whitespace;
			}
		}
		
		public PreProcessorDirectiveType Type {
			get;
			set;
		}
		
		public string Argument {
			get;
			set;
		}
		
		/// <summary>
		/// For an '#if' directive, specifies whether the condition evaluated to true.
		/// </summary>
		public bool Take {
			get;
			set;
		}
		
		TextLocation startLocation;
		public override TextLocation StartLocation {
			get { 
				return startLocation;
			}
		}
		
		TextLocation endLocation;
		public override TextLocation EndLocation {
			get {
				return endLocation;
			}
		}
		
		public PreProcessorDirective (PreProcessorDirectiveType type, TextLocation startLocation, TextLocation endLocation)
		{
			this.Type = type;
			this.startLocation = startLocation;
			this.endLocation = endLocation;
		}
		
		#region IRelocationable implementation
		void IRelocatable.SetStartLocation (TextLocation startLocation)
		{
			int lineDelta = startLocation.Line - this.startLocation.Line;
			endLocation = new TextLocation (endLocation.Line + lineDelta, lineDelta != 0 ? endLocation.Column : endLocation.Column + startLocation.Column - this.startLocation.Column);
			this.startLocation = startLocation;
		}
		#endregion

		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data = default(T))
		{
			return visitor.VisitPreProcessorDirective (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			PreProcessorDirective o = other as PreProcessorDirective;
			return o != null && Type == o.Type && MatchString(Argument, o.Argument);
		}
	}
}

