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

using System;
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Async method debug info
	/// </summary>
	public sealed class AsyncMethodDebugInfo {
		/// <summary>
		/// Async step infos
		/// </summary>
		public AsyncStepInfo[] StepInfos { get; }

		/// <summary>
		/// Async method builder field or null
		/// </summary>
		public FieldDef? BuilderField { get; }

		/// <summary>
		/// Catch handler offset or <see cref="uint.MaxValue"/>. Only used if it's an async void method
		/// </summary>
		public uint CatchHandlerOffset { get; }

		/// <summary>
		/// Offset of SetResult() call, or <see cref="uint.MaxValue"/> if it's unknown
		/// </summary>
		public uint SetResultOffset { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="stepInfos">Async step infos</param>
		/// <param name="builderField">Async method builder field or null if it's unknown</param>
		/// <param name="catchHandlerOffset">Catch handler offset or <see cref="uint.MaxValue"/>. Only used if it's a async void method</param>
		/// <param name="setResultOffset">Offset of SetResult() call, or <see cref="uint.MaxValue"/> if it's unknown</param>
		public AsyncMethodDebugInfo(AsyncStepInfo[] stepInfos, FieldDef? builderField, uint catchHandlerOffset, uint setResultOffset) {
			StepInfos = stepInfos ?? throw new ArgumentNullException(nameof(stepInfos));
			BuilderField = builderField;
			CatchHandlerOffset = catchHandlerOffset;
			SetResultOffset = setResultOffset;
		}
	}
}
