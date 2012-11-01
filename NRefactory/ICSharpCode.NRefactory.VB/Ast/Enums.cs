// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

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
		Overrides       = 0x1000,
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
		
		Narrowing  = 0x2000000,
		Widening   = 0x4000000,
		
		Iterator   = 0x8000000,
		Async      = 0x10000000,
		
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
	
	public enum AssignmentOperatorType
	{
		None,
		Assign,
		
		Add,
		Subtract,
		Multiply,
		Divide,
		
		Power,         // (VB only)
		DivideInteger, // (VB only)
		ConcatString,  // (VB only)
		
		ShiftLeft,
		ShiftRight,
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
		LoopUntil,
		LoopWhile,
		DoUntil,
		DoWhile
	}
	
	public enum ConversionType
	{
		None,
		Implicit,
		Explicit
	}
	
	/// <summary>
	/// Specifies the ordering direction of a QueryExpressionOrdering node.
	/// </summary>
	public enum QueryOrderingDirection
	{
		None,
		Ascending,
		Descending
	}
	
	/// <summary>
	/// Specifies the partition type for a VB.NET
	/// query expression.
	/// </summary>
	public enum PartitionKind
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
