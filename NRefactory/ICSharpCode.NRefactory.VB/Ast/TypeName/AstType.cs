// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)
using System;
using System.Collections.Generic;
using System.Linq;

namespace ICSharpCode.NRefactory.VB.Ast
{
	/// <summary>
	/// A type reference in the VB AST.
	/// </summary>
	public abstract class AstType : AstNode
	{
		#region Null
		public new static readonly AstType Null = new NullAstType();
		
		sealed class NullAstType : AstType
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
		
		public virtual AstType MakeArrayType(int rank = 1)
		{
			return new ComposedType { BaseType = this }.MakeArrayType(rank);
		}
		
		public static AstType FromName(string fullName)
		{
			if (string.IsNullOrEmpty(fullName))
				throw new ArgumentNullException("fullName");
			fullName = fullName.Trim();
			if (!fullName.Contains("."))
				return new SimpleType(fullName);
			string[] parts = fullName.Split('.');
			
			AstType type = new SimpleType(parts.First());
			
			foreach (var part in parts.Skip(1)) {
				type = new QualifiedType(type, part);
			}
			
			return type;
		}
		
		/// <summary>
		/// Builds an expression that can be used to access a static member on this type.
		/// </summary>
		public MemberAccessExpression Member(string memberName)
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
					return new PrimitiveType("Object");
				case TypeCode.Boolean:
					return new PrimitiveType("Boolean");
				case TypeCode.Char:
					return new PrimitiveType("Char");
				case TypeCode.SByte:
					return new PrimitiveType("SByte");
				case TypeCode.Byte:
					return new PrimitiveType("Byte");
				case TypeCode.Int16:
					return new PrimitiveType("Short");
				case TypeCode.UInt16:
					return new PrimitiveType("UShort");
				case TypeCode.Int32:
					return new PrimitiveType("Integer");
				case TypeCode.UInt32:
					return new PrimitiveType("UInteger");
				case TypeCode.Int64:
					return new PrimitiveType("Long");
				case TypeCode.UInt64:
					return new PrimitiveType("ULong");
				case TypeCode.Single:
					return new PrimitiveType("Single");
				case TypeCode.Double:
					return new PrimitiveType("Double");
				case TypeCode.Decimal:
					return new PrimitiveType("Decimal");
				case TypeCode.String:
					return new PrimitiveType("String");
				case TypeCode.DateTime:
					return new PrimitiveType("Date");
			}
			return new SimpleType(type.FullName); // TODO: implement this correctly
		}
	}
}
