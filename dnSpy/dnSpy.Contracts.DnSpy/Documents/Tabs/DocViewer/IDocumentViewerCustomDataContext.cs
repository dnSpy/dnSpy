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

using dnSpy.Contracts.Decompiler;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Documents.Tabs.DocViewer {
	/// <summary>
	/// Context passed to <see cref="IDocumentViewerCustomDataProvider"/>
	/// </summary>
	public interface IDocumentViewerCustomDataContext {
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

		/// <summary>
		/// Gets data added by <see cref="IDecompilerOutput.AddCustomData{TData}(string, TData)"/>
		/// </summary>
		/// <typeparam name="TData">Type of data</typeparam>
		/// <param name="id">Key, eg. <see cref="PredefinedCustomDataIds.DebugInfo"/></param>
		/// <returns></returns>
		TData[] GetData<TData>(string id);

		/// <summary>
		/// Adds data that gets stored in <see cref="DocumentViewerContent"/>
		/// </summary>
		/// <param name="id">Key, eg. <see cref="DocumentViewerContentDataIds.DebugInfo"/></param>
		/// <param name="data">Data</param>
		void AddCustomData(string id, object data);
	}
}
