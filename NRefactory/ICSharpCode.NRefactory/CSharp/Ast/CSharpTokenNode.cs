// 
// TokenNode.cs
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

namespace ICSharpCode.NRefactory.CSharp
{
	public class CSharpTokenNode : AstNode, IRelocatable
	{
		public static new readonly CSharpTokenNode Null = new NullCSharpTokenNode ();
		class NullCSharpTokenNode : CSharpTokenNode
		{
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public NullCSharpTokenNode () : base (AstLocation.Empty, 0)
			{
			}
			
			public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
			{
				return default (S);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
		}
		
		
		public override NodeType NodeType {
			get {
				return NodeType.Token;
			}
		}
		
		AstLocation startLocation;
		public override AstLocation StartLocation {
			get {
				return startLocation;
			}
		}
		
		protected int tokenLength;
		public override AstLocation EndLocation {
			get {
				return new AstLocation (StartLocation.Line, StartLocation.Column + tokenLength);
			}
		}
		
		public CSharpTokenNode (AstLocation location, int tokenLength)
		{
			this.startLocation = location;
			this.tokenLength = tokenLength;
		}
		
		#region IRelocationable implementation
		void IRelocatable.SetStartLocation (AstLocation startLocation)
		{
			this.startLocation = startLocation;
		}
		#endregion
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitCSharpTokenNode (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			CSharpTokenNode o = other as CSharpTokenNode;
			return o != null && !o.IsNull && !(o is CSharpModifierToken);
		}
		
		public override string ToString ()
		{
			return string.Format ("[CSharpTokenNode: StartLocation={0}, EndLocation={1}, Role={2}]", StartLocation, EndLocation, Role);
		}
	}
}

