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

namespace dnSpy.Contracts.Documents.Tabs {
	/// <summary>
	/// Created by <see cref="IReferenceDocumentTabContentProvider"/>
	/// </summary>
	public sealed class DocumentTabReferenceResult {
		/// <summary>
		/// New tab content, never null
		/// </summary>
		public DocumentTabContent DocumentTabContent { get; }

		/// <summary>
		/// UI state (passed to <see cref="DocumentTabUIContext.RestoreUIState(object)"/>) or null if none
		/// </summary>
		public object? UIState { get; }

		/// <summary>
		/// Called when the output has been shown, can be null
		/// </summary>
		public Action<ShowTabContentEventArgs>? OnShownHandler { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="documentTabContent">New content</param>
		/// <param name="uiState">UI state (passed to <see cref="DocumentTabUIContext.RestoreUIState(object)"/>) or null</param>
		/// <param name="onShownHandler">Handler or null</param>
		public DocumentTabReferenceResult(DocumentTabContent documentTabContent, object? uiState = null, Action<ShowTabContentEventArgs>? onShownHandler = null) {
			DocumentTabContent = documentTabContent ?? throw new ArgumentNullException(nameof(documentTabContent));
			UIState = uiState;
			OnShownHandler = onShownHandler;
		}
	}
}
