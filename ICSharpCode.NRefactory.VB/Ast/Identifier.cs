// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Represents an identifier in VB.
	/// </summary>
	public class Identifier : AstNode
	{
		public static readonly new Identifier Null = new NullIdentifier ();
		class NullIdentifier : Identifier
		{
			public override bool IsNull {
				get {
					return true;
				}
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
		
		string name;
		
		public string Name {
			get { return name; }
			set {
				if (value == null)
					throw new ArgumentNullException("value");
				name = value;
			}
		}
		
		public TypeCode TypeCharacter { get; set; }
		
		TextLocation startLocation;
		public override TextLocation StartLocation {
			get {
				return startLocation;
			}
		}
		
		public override TextLocation EndLocation {
			get {
				return new TextLocation (StartLocation.Line, StartLocation.Column + Name.Length);
			}
		}
		
		private Identifier()
		{
			this.name = string.Empty;
		}
		
		public Identifier (string name, TextLocation location)
		{
			if (name == null)
				throw new ArgumentNullException("name");
			this.Name = name;
			this.startLocation = location;
		}
		
		public static implicit operator Identifier(string name)
		{
			return new Identifier(name, TextLocation.Empty);
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			var node = other as Identifier;
			return node != null
				&& MatchString(node.name, name)
				&& node.TypeCharacter == TypeCharacter;
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitIdentifier(this, data);
		}
		
		public override string ToString()
		{
			return string.Format("{0}",
			                     name);
		}
	}
}
