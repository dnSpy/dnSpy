/*
 * Created by SharpDevelop.
 * User: Siegfried
 * Date: 11.04.2011
 * Time: 20:44
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// Description of VBTokenNode.
	/// </summary>
	public class VBTokenNode : AstNode
	{
		public static new readonly VBTokenNode Null = new NullVBTokenNode();
		
		class NullVBTokenNode : VBTokenNode
		{
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public NullVBTokenNode() : base (AstLocation.Empty, 0)
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
		
		AstLocation startLocation;
		public override AstLocation StartLocation {
			get {
				return startLocation;
			}
		}
		
		protected int tokenLength = -1;
		
		AstLocation endLocation;
		public override AstLocation EndLocation {
			get {
				return tokenLength < 0 ? endLocation : new AstLocation(startLocation.Line, startLocation.Column + tokenLength);
			}
		}
		
		public VBTokenNode(AstLocation location, int tokenLength)
		{
			this.startLocation = location;
			this.tokenLength = tokenLength;
		}
		
		public VBTokenNode(AstLocation startLocation, AstLocation endLocation)
		{
			this.startLocation = startLocation;
			this.endLocation = endLocation;
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitVBTokenNode(this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			var node = other as VBTokenNode;
			return node != null && !node.IsNull;
		}
		
		public override string ToString ()
		{
			return string.Format ("[VBTokenNode: StartLocation={0}, EndLocation={1}, Role={2}]", StartLocation, EndLocation, Role);
		}
	}
}
