using System;

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
			
			public override S AcceptVisitor<T, S> (AstVisitor<T, S> visitor, T data)
			{
				return default (S);
			}
		}
		#endregion
		
		public override NodeType NodeType {
			get { return NodeType.TypeReference; }
		}
		
		public virtual AstType MakePointerType()
		{
			return new ComposedType { BaseType = this }.MakePointerType();
		}
		
		public virtual AstType MakeArrayType(int rank)
		{
			return new ComposedType { BaseType = this }.MakeArrayType(rank);
		}
		
		/// <summary>
		/// Builds an expression that can be used to access a static member on this type.
		/// </summary>
		public MemberReferenceExpression Member(string memberName)
		{
			return new TypeReferenceExpression { Type = this }.Member(memberName);
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
