// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.CSharp
{
	/// <summary>
	/// A type reference in the C# AST.
	/// </summary>
	public abstract class AstType : AstNode
	{
		#region Null
		public new static readonly AstType Null = new NullAstType ();
		
		sealed class NullAstType : AstType
		{
			public override bool IsNull {
				get {
					return true;
				}
			}
			
			public override S AcceptVisitor<T, S> (IAstVisitor<T, S> visitor, T data = default(T))
			{
				return default (S);
			}
			
			protected internal override bool DoMatch(AstNode other, PatternMatching.Match match)
			{
				return other == null || other.IsNull;
			}
		}
		#endregion
		
		#region PatternPlaceholder
		public static implicit operator AstType(PatternMatching.Pattern pattern)
		{
			return pattern != null ? new PatternPlaceholder(pattern) : null;
		}
		
		sealed class PatternPlaceholder : AstType, PatternMatching.INode
		{
			readonly PatternMatching.Pattern child;
			
			public PatternPlaceholder(PatternMatching.Pattern child)
			{
				this.child = child;
			}
			
			public override NodeType NodeType {
				get { return NodeType.Pattern; }
			}
			
			public override S AcceptVisitor<T, S>(IAstVisitor<T, S> visitor, T data = default(T))
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
			get { return NodeType.TypeReference; }
		}
		
		public new AstType Clone()
		{
			return (AstType)base.Clone();
		}
		
		/// <summary>
		/// Creates a pointer type from this type by nesting it in a <see cref="ComposedType"/>.
		/// If this type already is a pointer type, this method just increases the PointerRank of the existing pointer type.
		/// </summary>
		public virtual AstType MakePointerType()
		{
			return new ComposedType { BaseType = this }.MakePointerType();
		}
		
		/// <summary>
		/// Creates an array type from this type by nesting it in a <see cref="ComposedType"/>.
		/// If this type already is an array type, the additional rank is prepended to the existing array specifier list.
		/// Thus, <c>new SimpleType("T").MakeArrayType(1).MakeArrayType(2)</c> will result in "T[,][]".
		/// </summary>
		public virtual AstType MakeArrayType(int rank = 1)
		{
			return new ComposedType { BaseType = this }.MakeArrayType(rank);
		}
		
		/// <summary>
		/// Creates a nullable type from this type by nesting it in a <see cref="ComposedType"/>.
		/// </summary>
		public AstType MakeNullableType()
		{
			return new ComposedType { BaseType = this, HasNullableSpecifier = true };
		}
		
		/// <summary>
		/// Builds an expression that can be used to access a static member on this type.
		/// </summary>
		public MemberReferenceExpression Member(string memberName)
		{
			return new TypeReferenceExpression { Type = this }.Member(memberName);
		}
		
		/// <summary>
		/// Builds an invocation expression using this type as target.
		/// </summary>
		public InvocationExpression Invoke(string methodName, IEnumerable<Expression> arguments)
		{
			return new TypeReferenceExpression { Type = this }.Invoke(methodName, arguments);
		}
		
		/// <summary>
		/// Builds an invocation expression using this type as target.
		/// </summary>
		public InvocationExpression Invoke(string methodName, params Expression[] arguments)
		{
			return new TypeReferenceExpression { Type = this }.Invoke(methodName, arguments);
		}
		
		/// <summary>
		/// Builds an invocation expression using this type as target.
		/// </summary>
		public InvocationExpression Invoke(string methodName, IEnumerable<AstType> typeArguments, IEnumerable<Expression> arguments)
		{
			return new TypeReferenceExpression { Type = this }.Invoke(methodName, typeArguments, arguments);
		}
		
		public static AstType Create(Type type)
		{
			switch (Type.GetTypeCode(type)) {
				case TypeCode.Object:
					return new PrimitiveType("object");
				case TypeCode.Boolean:
					return new PrimitiveType("bool");
				case TypeCode.Char:
					return new PrimitiveType("char");
				case TypeCode.SByte:
					return new PrimitiveType("sbyte");
				case TypeCode.Byte:
					return new PrimitiveType("byte");
				case TypeCode.Int16:
					return new PrimitiveType("short");
				case TypeCode.UInt16:
					return new PrimitiveType("ushort");
				case TypeCode.Int32:
					return new PrimitiveType("int");
				case TypeCode.UInt32:
					return new PrimitiveType("uint");
				case TypeCode.Int64:
					return new PrimitiveType("long");
				case TypeCode.UInt64:
					return new PrimitiveType("ulong");
				case TypeCode.Single:
					return new PrimitiveType("float");
				case TypeCode.Double:
					return new PrimitiveType("double");
				case TypeCode.Decimal:
					return new PrimitiveType("decimal");
				case TypeCode.String:
					return new PrimitiveType("string");
			}
			return new SimpleType(type.FullName); // TODO: implement this correctly
		}
	}
}
