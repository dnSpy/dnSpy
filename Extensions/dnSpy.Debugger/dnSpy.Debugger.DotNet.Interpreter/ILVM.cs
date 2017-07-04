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

using System;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Interpreter {
	/// <summary>
	/// Interprets IL code and returns the result
	/// </summary>
	internal abstract class ILVM {
		/// <summary>
		/// Interprets the IL instructions in the method body
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="method">Method</param>
		/// <param name="variablesProvider">Variables provider</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract object Execute(IDmdEvaluationContext context, DmdMethodBase method, VariablesProvider variablesProvider, ExecuteOptions options);
	}

	/// <summary>
	/// <see cref="ILVM"/> execute options
	/// </summary>
	[Flags]
	internal enum ExecuteOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None						= 0,
	}
}
