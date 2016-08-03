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

using System.Collections.ObjectModel;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Files.Tabs.DocViewer {
	/// <summary>
	/// Custom data IDs passed to eg. <see cref="DocumentViewerContent.GetCustomData{TData}(string)"/>
	/// </summary>
	public static class DocumentViewerContentDataIds {
		/// <summary>
		/// Data is a <see cref="ReadOnlyCollection{T}"/> collection (<see cref="MethodDebugInfo"/> elements)
		/// </summary>
		public const string DebugInfo = "DebugInfo-Content";

		/// <summary>
		/// Data is a <see cref="SpanDataCollection{TData}"/> (<see cref="ReferenceAndId"/> elements)
		/// </summary>
		public const string SpanReference = "DebugInfo-SpanReference";

		/// <summary>
		/// <see cref="Decompiler.BracePair"/> data
		/// </summary>
		public const string BracePair = "DebugInfo-BracePair";

		/// <summary>
		/// <see cref="Decompiler.LineSeparator"/> data
		/// </summary>
		public const string LineSeparator = "DebugInfo-LineSeparator";
	}
}
