// 
// AttributeSection.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
	/// <summary>
	/// [AttributeTarget: Attributes]
	/// </summary>
	public class AttributeSection : AstNode
	{
		#region PatternPlaceholder
		public static implicit operator AttributeSection(PatternMatching.Pattern pattern)
		{
			return pattern != null ? new PatternPlaceholder(pattern) : null;
		}
		
		sealed class PatternPlaceholder : AttributeSection, PatternMatching.INode
		{
			readonly PatternMatching.Pattern child;
			
			public PatternPlaceholder(PatternMatching.Pattern child)
			{
				this.child = child;
			}
			
			public override NodeType NodeType {
				get { return NodeType.Pattern; }
			}
			
			public override void AcceptVisitor (IAstVisitor visitor)
			{
				visitor.VisitPatternPlaceholder (this, child);
			}
				
			public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
			{
				return visitor.VisitPatternPlaceholder (this, child);
			}
			
			public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
			{
				return visitor.VisitPatternPlaceholder(this, child, data);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return child.DoMatch(other, match);
			}
			
			bool PatternMatching.INode.DoMatchCollection(Role role, PatternMatching.INode pos, PatternMatching.Match match, PatternMatching.BacktrackingInfo backtrackingInfo)
			{
				return child.DoMatchCollection(role, pos, match, backtrackingInfo);
			}
		}
		#endregion
		
		public override NodeType NodeType {
			get {
				return NodeType.Unknown;
			}
		}
		
		public CSharpTokenNode LBracketToken {
			get { return GetChildByRole (Roles.LBracket); }
		}
		
		public string AttributeTarget {
			get {
				return GetChildByRole (Roles.Identifier).Name;
			}
			set {
				SetChildByRole (Roles.Identifier, CSharp.Identifier.Create (value));
			}
		}
		
		public Identifier AttributeTargetToken {
			get {
				return GetChildByRole (Roles.Identifier);
			}
			set {
				SetChildByRole (Roles.Identifier, value);
			}
		}
		
		public AstNodeCollection<Attribute> Attributes {
			get { return base.GetChildrenByRole (Roles.Attribute); }
		}
		
		public CSharpTokenNode RBracketToken {
			get { return GetChildByRole (Roles.RBracket); }
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitAttributeSection (this);
		}
			
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitAttributeSection (this);
		}
		
		public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitAttributeSection (this, data);
		}
		
		protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
		{
			AttributeSection o = other as AttributeSection;
			return o != null && MatchString(this.AttributeTarget, o.AttributeTarget) && this.Attributes.DoMatch(o.Attributes, match);
		}
		
		public AttributeSection()
		{
		}
		
		public AttributeSection(Attribute attr)
		{
			this.Attributes.Add(attr);
		}
		
//		public static string GetAttributeTargetName(AttributeTarget attributeTarget)
//		{
//			switch (attributeTarget) {
//				case AttributeTarget.None:
//					return null;
//				case AttributeTarget.Assembly:
//					return "assembly";
//				case AttributeTarget.Module:
//					return "module";
//				case AttributeTarget.Type:
//					return "type";
//				case AttributeTarget.Param:
//					return "param";
//				case AttributeTarget.Field:
//					return "field";
//				case AttributeTarget.Return:
//					return "return";
//				case AttributeTarget.Method:
//					return "method";
//				default:
//					throw new NotSupportedException("Invalid value for AttributeTarget");
//			}
//		}
	}
}
