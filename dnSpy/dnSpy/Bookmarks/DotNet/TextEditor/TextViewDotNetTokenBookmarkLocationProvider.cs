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
using dnlib.DotNet;
using dnSpy.Contracts.Bookmarks.TextEditor;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Metadata;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Bookmarks.DotNet.TextEditor {
	[Export(typeof(TextViewBookmarkLocationProvider))]
	sealed class TextViewDotNetTokenBookmarkLocationProvider : TextViewBookmarkLocationProvider {
		readonly Lazy<DotNetBookmarkLocationFactory> dotNetBookmarkLocationFactory;
		readonly IModuleIdProvider moduleIdProvider;

		[ImportingConstructor]
		TextViewDotNetTokenBookmarkLocationProvider(Lazy<DotNetBookmarkLocationFactory> dotNetBookmarkLocationFactory, IModuleIdProvider moduleIdProvider) {
			this.dotNetBookmarkLocationFactory = dotNetBookmarkLocationFactory;
			this.moduleIdProvider = moduleIdProvider;
		}

		public override TextViewBookmarkLocationResult? CreateLocation(IDocumentTab tab, ITextView textView, VirtualSnapshotPoint position) {
			var documentViewer = tab.TryGetDocumentViewer();
			if (documentViewer == null)
				return null;

			// A bookmark should be set on the current line if possible, and the current position
			// isn't necessarily at the start of the line.
			int startPos = position.Position.GetContainingLine().Start.Position;
			foreach (var data in documentViewer.ReferenceCollection.FindFrom(startPos)) {
				if (!data.Data.IsDefinition)
					continue;
				var def = data.Data.Reference as IMemberDef;
				if (def == null)
					continue;
				var span = data.Span;

				var snapshot = textView.TextSnapshot;
				if (span.End > snapshot.Length)
					return null;

				var moduleId = moduleIdProvider.Create(def.Module);
				var location = dotNetBookmarkLocationFactory.Value.CreateTokenLocation(moduleId, def.MDToken.Raw);
				return new TextViewBookmarkLocationResult(location, new SnapshotSpan(snapshot, span));
			}

			return null;
		}
	}
}
