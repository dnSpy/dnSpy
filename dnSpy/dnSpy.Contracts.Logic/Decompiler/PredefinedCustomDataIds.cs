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

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Predefined custom data IDs passed to <see cref="IDecompilerOutput.AddCustomData{TData}(string, TData)"/>
	/// </summary>
	public static class PredefinedCustomDataIds {
		/// <summary>
		/// TData = <see cref="MethodDebugInfo"/>
		/// </summary>
		public const string DebugInfo = nameof(DebugInfo);

		/// <summary>
		/// TData = <see cref="Decompiler.SpanReference"/>
		/// </summary>
		public const string SpanReference = nameof(SpanReference);

		/// <summary>
		/// TData = <see cref="Decompiler.CodeBracesRange"/>
		/// </summary>
		public const string CodeBracesRange = nameof(CodeBracesRange);

		/// <summary>
		/// TData = <see cref="Decompiler.LineSeparator"/>
		/// </summary>
		public const string LineSeparator = nameof(LineSeparator);
	}
}
