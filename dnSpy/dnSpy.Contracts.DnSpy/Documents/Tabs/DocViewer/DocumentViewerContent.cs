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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Documents.Tabs.DocViewer {
	/// <summary>
	/// <see cref="IDocumentViewer"/> content
	/// </summary>
	public sealed class DocumentViewerContent {
		/// <summary>
		/// Gets the text
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Gets the colors
		/// </summary>
		internal CachedTextColorsCollection ColorCollection { get; }

		/// <summary>
		/// Gets the references
		/// </summary>
		public SpanDataCollection<ReferenceInfo> ReferenceCollection { get; }

		/// <summary>
		/// Gets the method debug info collection
		/// </summary>
		public IReadOnlyList<MethodDebugInfo> MethodDebugInfos { get; }

		readonly Dictionary<string, object> customDataDict;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="colorCollection">Colors</param>
		/// <param name="referenceCollection">References</param>
		/// <param name="customDataDict">Custom data dictionary</param>
		internal DocumentViewerContent(string text, CachedTextColorsCollection colorCollection, SpanDataCollection<ReferenceInfo> referenceCollection, Dictionary<string, object> customDataDict) {
			if (colorCollection is null)
				throw new ArgumentNullException(nameof(colorCollection));
			colorCollection.Freeze();
			Text = text ?? throw new ArgumentNullException(nameof(text));
			ColorCollection = colorCollection;
			ReferenceCollection = referenceCollection ?? throw new ArgumentNullException(nameof(referenceCollection));
			this.customDataDict = customDataDict ?? throw new ArgumentNullException(nameof(customDataDict));
			MethodDebugInfos = (IReadOnlyList<MethodDebugInfo>?)GetCustomData<ReadOnlyCollection<MethodDebugInfo>>(DocumentViewerContentDataIds.DebugInfo) ?? Array.Empty<MethodDebugInfo>();
		}

		/// <summary>
		/// Gets custom data. Returns false if it doesn't exist.
		/// </summary>
		/// <typeparam name="TData">Type of data</typeparam>
		/// <param name="id">Key, eg., <see cref="DocumentViewerContentDataIds.DebugInfo"/></param>
		/// <param name="data">Updated with data</param>
		/// <returns></returns>
		public bool TryGetCustomData<TData>(string id, out TData data) {
			if (!customDataDict.TryGetValue(id, out var obj)) {
				data = default!;
				return false;
			}

			data = (TData)obj;
			return true;
		}

		/// <summary>
		/// Gets custom data
		/// </summary>
		/// <typeparam name="TData">Type of data</typeparam>
		/// <param name="id">Key, eg., <see cref="DocumentViewerContentDataIds.DebugInfo"/></param>
		/// <returns></returns>
		[return: MaybeNull]
		public TData GetCustomData<TData>(string id) {
			if (!customDataDict.TryGetValue(id, out var obj))
				return default!;
			return (TData)obj;
		}
	}
}
