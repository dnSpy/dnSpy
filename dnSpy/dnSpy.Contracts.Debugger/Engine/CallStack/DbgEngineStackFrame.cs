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

using System.Globalization;
using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Contracts.Documents;

namespace dnSpy.Contracts.Debugger.Engine.CallStack {
	/// <summary>
	/// Stack frame implemented by a <see cref="DbgEngine"/>
	/// </summary>
	public abstract class DbgEngineStackFrame : DbgObject {
		/// <summary>
		/// Gets the module or null if it's unknown
		/// </summary>
		public abstract DbgModule? Module { get; }

		/// <summary>
		/// Gets the location or null if none. Can be passed to <see cref="ReferenceNavigatorService.GoTo(object, object[])"/>
		/// </summary>
		public abstract DbgCodeLocation? Location { get; }

		/// <summary>
		/// Gets the offset of the IP relative to the start of the function
		/// </summary>
		public abstract uint FunctionOffset { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		public abstract DbgStackFrameFlags Flags { get; }

		/// <summary>
		/// Gets the function token or <see cref="InvalidFunctionToken"/> if it doesn't have a token.
		/// </summary>
		public abstract uint FunctionToken { get; }

		/// <summary>
		/// Invalid function token
		/// </summary>
		public const uint InvalidFunctionToken = uint.MaxValue;

		/// <summary>
		/// Formats the stack frame or returns false
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="output">Output</param>
		/// <param name="options">Stack frame options</param>
		/// <param name="valueOptions">Value option</param>
		/// <param name="cultureInfo">Culture or null to use invariant culture</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public virtual bool TryFormat(DbgEvaluationContext context, IDbgTextWriter output, DbgStackFrameFormatterOptions options, DbgValueFormatterOptions valueOptions, CultureInfo? cultureInfo, CancellationToken cancellationToken) => false;

		/// <summary>
		/// Called after the <see cref="DbgStackFrame"/> has been created
		/// </summary>
		/// <param name="frame">Stack frame</param>
		public abstract void OnFrameCreated(DbgStackFrame frame);
	}
}
