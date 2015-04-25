// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// Represents a 'cref' reference in XML documentation.
	/// </summary>
	public class DocumentationReference : AstNode
	{
		public static readonly Role<AstType> DeclaringTypeRole = new Role<AstType>("DeclaringType", AstType.Null);
		public static readonly Role<AstType> ConversionOperatorReturnTypeRole = new Role<AstType>("ConversionOperatorReturnType", AstType.Null);
		
		SymbolKind symbolKind;
		OperatorType operatorType;
		bool hasParameterList;
		
		/// <summary>
		/// Gets/Sets the entity type.
		/// Possible values are:
		///   <c>SymbolKind.Operator</c> for operators,
		///   <c>SymbolKind.Indexer</c> for indexers,
		///   <c>SymbolKind.TypeDefinition</c> for references to primitive types,
		///   and <c>SymbolKind.None</c> for everything else.
		/// </summary>
		public SymbolKind SymbolKind {
			get { return symbolKind; }
			set {
				ThrowIfFrozen();
				symbolKind = value;
			}
		}
		
		/// <summary>
		/// Gets/Sets the operator type.
		/// This property is only used when SymbolKind==Operator.
		/// </summary>
		public OperatorType OperatorType {
			get { return operatorType; }
			set {
				ThrowIfFrozen();
				operatorType = value;
			}
		}
		
		/// <summary>
		/// Gets/Sets whether a parameter list was provided.
		/// </summary>
		public bool HasParameterList {
			get { return hasParameterList; }
			set {
				ThrowIfFrozen();
				hasParameterList = value;
			}
		}
		
		public override NodeType NodeType {
			get { return NodeType.Unknown; }
		}
		
		/// <summary>
		/// Gets/Sets the declaring type.
		/// </summary>
		public AstType DeclaringType {
			get { return GetChildByRole(DeclaringTypeRole); }
			set { SetChildByRole(DeclaringTypeRole, value); }
		}
		
		/// <summary>
		/// Gets/sets the member name.
		/// This property is only used when SymbolKind==None.
		/// </summary>
		public string MemberName {
			get { return GetChildByRole(Roles.Identifier).Name; }
			set { SetChildByRole(Roles.Identifier, Identifier.Create(value)); }
		}
		
		/// <summary>
		/// Gets/Sets the return type of conversion operators.
		/// This property is only used when SymbolKind==Operator and OperatorType is explicit or implicit.
		/// </summary>
		public AstType ConversionOperatorReturnType {
			get { return GetChildByRole(ConversionOperatorReturnTypeRole); }
			set { SetChildByRole(ConversionOperatorReturnTypeRole, value); }
		}
		
		public AstNodeCollection<AstType> TypeArguments {
			get { return GetChildrenByRole (Roles.TypeArgument); }
		}
		
		public AstNodeCollection<ParameterDeclaration> Parameters {
			get { return GetChildrenByRole (Roles.Parameter); }
		}
		
		protected internal override bool DoMatch(AstNode other, ICSharpCode.NRefactory.PatternMatching.Match match)
		{
			DocumentationReference o = other as DocumentationReference;
			if (!(o != null && this.SymbolKind == o.SymbolKind && this.HasParameterList == o.HasParameterList))
				return false;
			if (this.SymbolKind == SymbolKind.Operator) {
				if (this.OperatorType != o.OperatorType)
					return false;
				if (this.OperatorType == OperatorType.Implicit || this.OperatorType == OperatorType.Explicit) {
					if (!this.ConversionOperatorReturnType.DoMatch(o.ConversionOperatorReturnType, match))
						return false;
				}
			} else if (this.SymbolKind == SymbolKind.None) {
				if (!MatchString(this.MemberName, o.MemberName))
					return false;
				if (!this.TypeArguments.DoMatch(o.TypeArguments, match))
					return false;
			}
			return this.Parameters.DoMatch(o.Parameters, match);
		}
		
		public override void AcceptVisitor (IAstVisitor visitor)
		{
			visitor.VisitDocumentationReference (this);
		}
		
		public override T AcceptVisitor<T> (IAstVisitor<T> visitor)
		{
			return visitor.VisitDocumentationReference (this);
		}
		
		public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data)
		{
			return visitor.VisitDocumentationReference (this, data);
		}
	}
}
