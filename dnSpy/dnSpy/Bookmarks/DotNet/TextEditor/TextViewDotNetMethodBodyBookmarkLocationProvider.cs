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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Bookmarks.TextEditor;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Metadata;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Bookmarks.DotNet.TextEditor {
	[Export(typeof(TextViewBookmarkLocationProvider))]
	sealed class TextViewDotNetMethodBodyBookmarkLocationProvider : TextViewBookmarkLocationProvider {
		readonly Lazy<DotNetBookmarkLocationFactory> dotNetBookmarkLocationFactory;
		readonly IModuleIdProvider moduleIdProvider;

		[ImportingConstructor]
		TextViewDotNetMethodBodyBookmarkLocationProvider(Lazy<DotNetBookmarkLocationFactory> dotNetBookmarkLocationFactory, IModuleIdProvider moduleIdProvider) {
			this.dotNetBookmarkLocationFactory = dotNetBookmarkLocationFactory;
			this.moduleIdProvider = moduleIdProvider;
		}

		public override TextViewBookmarkLocationResult? CreateLocation(IDocumentTab tab, ITextView textView, VirtualSnapshotPoint position) {
			var documentViewer = tab.TryGetDocumentViewer();
			if (documentViewer == null)
				return null;
			var methodDebugService = documentViewer.GetMethodDebugService();
			if (methodDebugService == null)
				return null;
			// A bookmark should be set on the current line if possible, and the current position
			// isn't necessarily at the start of the line.
			var startPos = position.Position.GetContainingLine().Start;
			var methodStatements = methodDebugService.FindByTextPosition(startPos, FindByTextPositionOptions.None);
			if (methodStatements.Count == 0)
				return null;
			var textSpan = methodStatements[0].Statement.TextSpan;
			var snapshot = textView.TextSnapshot;
			if (textSpan.End > snapshot.Length)
				return null;
			var span = new VirtualSnapshotSpan(new SnapshotSpan(snapshot, new Span(textSpan.Start, textSpan.Length)));

			var statement = methodStatements[0];
			var moduleId = moduleIdProvider.Create(statement.Method.Module);
			var location = dotNetBookmarkLocationFactory.Value.CreateMethodBodyLocation(moduleId, statement.Method.MDToken.Raw, statement.Statement.ILSpan.Start);
			return new TextViewBookmarkLocationResult(location, span);
		}
	}
}
