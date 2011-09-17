// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Parser
{
	public class Token
	{
		internal readonly int kind;
		
		internal readonly int col;
		internal readonly int line;
		
		internal readonly object literalValue;
		internal readonly string val;
		internal Token next;
		readonly TextLocation endLocation;
		
		public int Kind {
			get { return kind; }
		}
		
		public object LiteralValue {
			get { return literalValue; }
		}
		
		public string Value {
			get { return val; }
		}
		
		public TextLocation EndLocation {
			get { return endLocation; }
		}
		
		public TextLocation Location {
			get {
				return new TextLocation(line, col);
			}
		}
		
		public Token()
			: this(0, 1, 1)
		{
		}
		
		public Token(int kind, int col, int line) : this (kind, col, line, null)
		{
		}
		
		public Token(int kind, TextLocation startLocation, TextLocation endLocation) : this(kind, startLocation, endLocation, "", null)
		{
		}
		
		public Token(int kind, int col, int line, string val)
		{
			this.kind         = kind;
			this.col          = col;
			this.line         = line;
			this.val          = val;
			this.endLocation  = new TextLocation(line, col + (val == null ? 1 : val.Length));
		}
		
		internal Token(int kind, int x, int y, string val, object literalValue)
			: this(kind, new TextLocation(y, x), new TextLocation(y, x + val.Length), val, literalValue)
		{
		}
		
		public Token(int kind, TextLocation startLocation, TextLocation endLocation, string val, object literalValue)
		{
			this.kind         = kind;
			this.col          = startLocation.Column;
			this.line         = startLocation.Line;
			this.endLocation = endLocation;
			this.val          = val;
			this.literalValue = literalValue;
		}
		
		public override string ToString()
		{
			string vbToken;
			
			try {
				vbToken = Tokens.GetTokenString(kind);
			} catch (NotSupportedException) {
				vbToken = "<unknown>";
			}
			
			return string.Format("[Token {0} Location={1} EndLocation={2} val={3}]",
			                     vbToken, Location, EndLocation, val);
		}
	}
}
