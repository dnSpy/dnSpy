/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Contracts.Debugger.DotNet.CorDebug.CallStack {
	/// <summary>
	/// A .NET method body location including an exact native address
	/// </summary>
	public abstract class DbgDotNetStackFrameLocation : DbgStackFrameLocation {
		/// <summary>
		/// Gets the module
		/// </summary>
		public abstract ModuleId Module { get; }

		/// <summary>
		/// Gets the token of a method within the module
		/// </summary>
		public abstract uint Token { get; }

		/// <summary>
		/// Gets the IL offset within the method body
		/// </summary>
		public abstract uint ILOffset { get; }

		/// <summary>
		/// Gets the IL offset mapping
		/// </summary>
		public abstract DbgILOffsetMapping ILOffsetMapping { get; }

		/// <summary>
		/// Gets the address of the native method
		/// </summary>
		public abstract ulong NativeMethodAddress { get; }

		/// <summary>
		/// Gets the offset of the native instruction within the native method body
		/// </summary>
		public abstract uint NativeMethodOffset { get; }
	}
}
