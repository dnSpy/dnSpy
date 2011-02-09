// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;

namespace ICSharpCode.NRefactory.Parser
{
	public class Token
	{
		public int kind;
		
		public int col;
		public int line;
		
		public object literalValue;
		public string val;
		public Token  next;
		
		public Location EndLocation {
			get {
				return new Location(val == null ? col + 1 : col + val.Length, line);
			}
		}
		public Location Location {
			get {
				return new Location(col, line);
			}
		}
		
		public Token(int kind) : this(kind, 0, 0)
		{
		}
		
		public Token(int kind, int col, int line) : this (kind, col, line, null)
		{
		}
		
		public Token(int kind, int col, int line, string val) : this(kind, col, line, val, null)
		{
		}
		
		public Token(int kind, int col, int line, string val, object literalValue)
		{
			this.kind         = kind;
			this.col          = col;
			this.line         = line;
			this.val          = val;
			this.literalValue = literalValue;
		}
		
		public override string ToString()
		{
			return string.Format("[C# {0}/VB {1} line={2} col={3} val={4}]",
			                     CSharp.Tokens.GetTokenString(kind),
			                     VB.Tokens.GetTokenString(kind),
			                     line, col, val);
			
		}
	}
}
