/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

// This is needed because net4x reference assemblies don't have any nullable attributes

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace System {
	public static class string2 {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullOrEmpty([NotNullWhen(false)] string? value) => string.IsNullOrEmpty(value);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNullOrWhiteSpace([NotNullWhen(false)] string? value) => string.IsNullOrWhiteSpace(value);
	}
}

namespace System.Diagnostics {
	public static class Debug2 {
		[Conditional("DEBUG")]
		public static void Assert([DoesNotReturnIf(false)] bool condition) => Debug.Assert(condition);
		[Conditional("DEBUG")]
		public static void Assert([DoesNotReturnIf(false)] bool condition, string? message) => Debug.Assert(condition, message);
	}
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
