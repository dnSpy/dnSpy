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
using System.Collections.ObjectModel;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Files.Tabs.DocViewer {
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
		public CachedTextTokenColors ColorCollection { get; }

		/// <summary>
		/// Gets the references
		/// </summary>
		public SpanDataCollection<ReferenceInfo> ReferenceCollection { get; }

		/// <summary>
		/// Gets the IL code mappings
		/// </summary>
		public ReadOnlyCollection<MethodDebugInfo> MethodDebugInfos { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="text">Text</param>
		/// <param name="colorCollection">Colors</param>
		/// <param name="referenceCollection">References</param>
		/// <param name="methodDebugInfos">Debug info</param>
		public DocumentViewerContent(string text, CachedTextTokenColors colorCollection, SpanDataCollection<ReferenceInfo> referenceCollection, MethodDebugInfo[] methodDebugInfos) {
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			if (colorCollection == null)
				throw new ArgumentNullException(nameof(colorCollection));
			if (referenceCollection == null)
				throw new ArgumentNullException(nameof(referenceCollection));
			if (methodDebugInfos == null)
				throw new ArgumentNullException(nameof(methodDebugInfos));
			colorCollection.Freeze();
			Text = text;
			ColorCollection = colorCollection;
			ReferenceCollection = referenceCollection;
			MethodDebugInfos = new ReadOnlyCollection<MethodDebugInfo>(methodDebugInfos);
		}
	}
}
