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
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Debugger.Engine.CallStack {
	/// <summary>
	/// Stack frame implemented by a <see cref="DbgEngine"/>
	/// </summary>
	public abstract class DbgEngineStackFrame : DbgObject {
		/// <summary>
		/// Gets the module or null if it's unknown
		/// </summary>
		public abstract DbgModule Module { get; }

		/// <summary>
		/// Gets the location or null if none. Can be passed to <see cref="ReferenceNavigatorService.GoTo(object, object[])"/>
		/// </summary>
		public abstract DbgCodeLocation Location { get; }

		/// <summary>
		/// Gets the offset of the IP relative to the start of the function
		/// </summary>
		public abstract uint FunctionOffset { get; }

		/// <summary>
		/// Gets the function token or <see cref="InvalidFunctionToken"/> if it doesn't have a token.
		/// </summary>
		public abstract uint FunctionToken { get; }

		/// <summary>
		/// Invalid function token
		/// </summary>
		public const uint InvalidFunctionToken = uint.MaxValue;

		/// <summary>
		/// Formats the stack frame
		/// </summary>
		/// <param name="writer">Writer</param>
		/// <param name="options">Options</param>
		public abstract void Format(ITextColorWriter writer, DbgStackFrameFormatOptions options);
	}
}
