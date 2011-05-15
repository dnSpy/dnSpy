// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.VB.Ast
{
	[Flags]
	public enum Modifiers
	{
		None      = 0x0000,
		
		// Accessibility
		Private   = 0x0001,
		Friend    = 0x0002,
		Protected = 0x0004,
		Public    = 0x0008,
		
		// Scope
		MustInherit     = 0x0010,  // Types
		MustOverride    = 0x0020,  // Members
		Overridable     = 0x0040,
		NotInheritable  = 0x0080,  // Types
		NotOverridable  = 0x0100,  // Members
		Const           = 0x0200,
		Shared          = 0x0400,
		Static          = 0x0800,
		Override        = 0x1000,
		ReadOnly        = 0x2000,
		Shadows         = 0x4000,
		Partial         = 0x8000,
		
		// Special
		Overloads  = 0x10000, // VB specific
		WithEvents = 0x20000, // VB specific
		Default    = 0x40000, // VB specific
		
		Dim	   = 0x80000,	// VB.NET SPECIFIC, for fields/local variables only
		
		/// <summary>Only for VB properties.</summary>
		WriteOnly  = 0x100000, // VB specific
		
		ByVal      = 0x200000,
		ByRef      = 0x400000,
		ParamArray = 0x800000,
		Optional   = 0x1000000,
		
		/// <summary>
		/// Special value used to match any modifiers during pattern matching.
		/// </summary>
		Any = unchecked((int)0x80000000)
	}
	
	public enum ParentType
	{
		ClassOrStruct,
		InterfaceOrEnum,
		Namespace,
		Unknown
	}
	
	public enum FieldDirection
	{
		None,
		In,
		Out,
		Ref
	}
	
	public enum VarianceModifier
	{
		Invariant,
		Covariant,
		Contravariant
	};
	
	public enum AssignmentOperatorType
	{
		None,
		Assign,
		
		Add,
		Subtract,
		Multiply,
		Divide,
		Modulus,
		
		Power,         // (VB only)
		DivideInteger, // (VB only)
		ConcatString,  // (VB only)
		
		ShiftLeft,
		ShiftRight,
		
		BitwiseAnd,
		BitwiseOr,
		ExclusiveOr,
	}
	
	public enum BinaryOperatorType
	{
		None,
		
		/// <summary>'&amp;' in C#, 'And' in VB.</summary>
		BitwiseAnd,
		/// <summary>'|' in C#, 'Or' in VB.</summary>
		BitwiseOr,
		/// <summary>'&amp;&amp;' in C#, 'AndAlso' in VB.</summary>
		LogicalAnd,
		/// <summary>'||' in C#, 'OrElse' in VB.</summary>
		LogicalOr,
		/// <summary>'^' in C#, 'Xor' in VB.</summary>
		ExclusiveOr,
		
		/// <summary>&gt;</summary>
		GreaterThan,
		/// <summary>&gt;=</summary>
		GreaterThanOrEqual,
		/// <summary>'==' in C#, '=' in VB.</summary>
		Equality,
		/// <summary>'!=' in C#, '&lt;&gt;' in VB.</summary>
		InEquality,
		/// <summary>&lt;</summary>
		LessThan,
		/// <summary>&lt;=</summary>
		LessThanOrEqual,
		
		/// <summary>+</summary>
		Add,
		/// <summary>-</summary>
		Subtract,
		/// <summary>*</summary>
		Multiply,
		/// <summary>/</summary>
		Divide,
		/// <summary>'%' in C#, 'Mod' in VB.</summary>
		Modulus,
		/// <summary>VB-only: \</summary>
		DivideInteger,
		/// <summary>VB-only: ^</summary>
		Power,
		/// <summary>VB-only: &amp;</summary>
		Concat,
		
		/// <summary>C#: &lt;&lt;</summary>
		ShiftLeft,
		/// <summary>C#: &gt;&gt;</summary>
		ShiftRight,
		/// <summary>VB-only: Is</summary>
		ReferenceEquality,
		/// <summary>VB-only: IsNot</summary>
		ReferenceInequality,
		
		/// <summary>VB-only: Like</summary>
		Like,
		/// <summary>
		/// 	C#: ??
		/// 	VB: IF(x, y)
		/// </summary>
		NullCoalescing,
		
		/// <summary>VB-only: !</summary>
		DictionaryAccess
	}
	
	public enum CastType
	{
		/// <summary>
		/// direct cast (C#, VB "DirectCast")
		/// </summary>
		Cast,
		/// <summary>
		/// try cast (C# "as", VB "TryCast")
		/// </summary>
		TryCast,
		/// <summary>
		/// converting cast (VB "CType")
		/// </summary>
		Conversion,
		/// <summary>
		/// primitive converting cast (VB "CString" etc.)
		/// </summary>
		PrimitiveConversion
	}
	
	public enum UnaryOperatorType
	{
		None,
		Not,
		BitNot,
		
		Minus,
		Plus,
		
		Increment,
		Decrement,
		
		PostIncrement,
		PostDecrement,
		
		/// <summary>Dereferencing pointer</summary>
		Dereference,
		/// <summary>Get address of</summary>
		AddressOf
	}
	
	public enum ContinueType
	{
		None,
		Do,
		For,
		While
	}
	
	public enum ConditionType
	{
		None,
		Until,
		While,
		DoWhile
	}
	
	public enum ConditionPosition
	{
		None,
		Start,
		End
	}
	
	public enum ExitType
	{
		None,
		Sub,
		Function,
		Property,
		Do,
		For,
		While,
		Select,
		Try
	}
	
	public enum ConstructorInitializerType
	{
		None,
		Base,
		This
	}
	
	public enum ConversionType
	{
		None,
		Implicit,
		Explicit
	}
	
	public enum OverloadableOperatorType
	{
		None,
		
		Add,
		Subtract,
		Multiply,
		Divide,
		Modulus,
		Concat,
		
		UnaryPlus,
		UnaryMinus,
		
		Not,
		BitNot,
		
		BitwiseAnd,
		BitwiseOr,
		ExclusiveOr,
		
		ShiftLeft,
		ShiftRight,
		
		GreaterThan,
		GreaterThanOrEqual,
		Equality,
		InEquality,
		LessThan,
		LessThanOrEqual,
		
		Increment,
		Decrement,
		
		IsTrue,
		IsFalse,
		
		// VB specific
		Like,
		Power,
		CType,
		DivideInteger
	}
	
	///<summary>
	/// Charset types, used in external methods
	/// declarations (VB only).
	///</summary>
	public enum CharsetModifier
	{
		None,
		Auto,
		Unicode,
		Ansi
	}
	
	/// <summary>
	/// Specifies the ordering direction of a QueryExpressionOrdering node.
	/// </summary>
	public enum QueryExpressionOrderingDirection
	{
		None,
		Ascending,
		Descending
	}
	
	/// <summary>
	/// Specifies the partition type for a VB.NET
	/// query expression.
	/// </summary>
	public enum QueryExpressionPartitionType
	{
		Take,
		TakeWhile,
		Skip,
		SkipWhile
	}
	
	public enum XmlAxisType
	{
		Element, // .
		Attribute, // .@
		Descendents // ...
	}
	
	public enum XmlContentType
	{
		Comment,
		Text,
		CData,
		ProcessingInstruction
	}
}
