/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// <see cref="CodeBracesRange"/> flags, see also <see cref="CodeBracesRangeFlagsHelper"/>
	/// </summary>
	[Flags]
	public enum CodeBracesRangeFlags {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		BraceKind_None				= 0,
		BraceKind_Parentheses		= 0x00000001,
		BraceKind_CurlyBraces		= 0x00000002,
		BraceKind_SquareBrackets	= 0x00000003,
		BraceKind_AngleBrackets		= 0x00000004,
		BraceKind_DoubleQuotes		= 0x00000005,
		BraceKind_SingleQuotes		= 0x00000006,
		// Update CodeBracesRangeFlagsHelper.ToBraceKind() if mask changes

		BlockKind_None				= 0,
		BlockKind_Namespace			= 0x00000100,
		BlockKind_Type				= 0x00000200,
		BlockKind_Method			= 0x00000300,
		BlockKind_Conditional		= 0x00000400,
		BlockKind_Loop				= 0x00000500,
		BlockKind_Property			= 0x00000600,
		BlockKind_Event				= 0x00000700,
		BlockKind_Try				= 0x00000800,
		BlockKind_Catch				= 0x00000900,
		BlockKind_Filter			= 0x00000A00,
		BlockKind_Finally			= 0x00000B00,
		BlockKind_Fault				= 0x00000C00,
		BlockKind_Other				= 0x00000D00,
		// Update CodeBracesRangeFlagsHelper.ToBlockKind() if mask changes

		NamespaceBraces				= BraceKind_CurlyBraces | BlockKind_Namespace,
		TypeBraces					= BraceKind_CurlyBraces | BlockKind_Type,
		MethodBraces				= BraceKind_CurlyBraces | BlockKind_Method,
		ConditionalBraces			= BraceKind_CurlyBraces | BlockKind_Conditional,
		LoopBraces					= BraceKind_CurlyBraces | BlockKind_Loop,
		PropertyBraces				= BraceKind_CurlyBraces | BlockKind_Property,
		EventBraces					= BraceKind_CurlyBraces | BlockKind_Event,
		TryBraces					= BraceKind_CurlyBraces | BlockKind_Try,
		CatchBraces					= BraceKind_CurlyBraces | BlockKind_Catch,
		FilterBraces				= BraceKind_CurlyBraces | BlockKind_Filter,
		FinallyBraces				= BraceKind_CurlyBraces | BlockKind_Finally,
		FaultBraces					= BraceKind_CurlyBraces | BlockKind_Fault,
		OtherBlockBraces			= BraceKind_CurlyBraces | BlockKind_Other,

		SingleQuotes				= BraceKind_SingleQuotes | BlockKind_None,
		DoubleQuotes				= BraceKind_DoubleQuotes | BlockKind_None,
		Parentheses					= BraceKind_Parentheses | BlockKind_None,
		AngleBrackets				= BraceKind_AngleBrackets | BlockKind_None,
		SquareBrackets				= BraceKind_SquareBrackets | BlockKind_None,
		CurlyBraces					= BraceKind_CurlyBraces | BlockKind_None,
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}

	/// <summary>
	/// <see cref="CodeBracesRangeFlags"/> helper methods
	/// </summary>
	public static class CodeBracesRangeFlagsHelper {
		const CodeBracesRangeFlags BraceKindMask = (CodeBracesRangeFlags)0x00000007;
		const CodeBracesRangeFlags BlockKindMask = (CodeBracesRangeFlags)0x00000F00;

		/// <summary>
		/// Extracts the brace kind
		/// </summary>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public static CodeBracesRangeFlags ToBraceKind(this CodeBracesRangeFlags flags) => flags & BraceKindMask;

		/// <summary>
		/// Extracts the block kind
		/// </summary>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public static CodeBracesRangeFlags ToBlockKind(this CodeBracesRangeFlags flags) => flags & BlockKindMask;

		/// <summary>
		/// Returns true if it's braces
		/// </summary>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public static bool IsBraces(this CodeBracesRangeFlags flags) => (flags & BraceKindMask) != CodeBracesRangeFlags.BraceKind_None;

		/// <summary>
		/// Returns true if it's a block
		/// </summary>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public static bool IsBlock(this CodeBracesRangeFlags flags) => (flags & BlockKindMask) != CodeBracesRangeFlags.BlockKind_None;
	}
}
