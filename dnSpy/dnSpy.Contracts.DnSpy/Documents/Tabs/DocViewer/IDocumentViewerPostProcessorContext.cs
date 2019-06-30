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

using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Documents.Tabs.DocViewer {
	/// <summary>
	/// Context passed to <see cref="IDocumentViewerPostProcessor"/>
	/// </summary>
	public interface IDocumentViewerPostProcessorContext {
		/// <summary>
		/// Gets the output. It's not possible to write new text, but custom data can be added.
		/// </summary>
		IDocumentViewerOutput DocumentViewerOutput { get; }

		/// <summary>
		/// Gets the document viewer
		/// </summary>
		IDocumentViewer DocumentViewer { get; }

		/// <summary>
		/// Gets the new text
		/// </summary>
		string Text { get; }

		/// <summary>
		/// Gets the content type
		/// </summary>
		IContentType ContentType { get; }
	}
}
