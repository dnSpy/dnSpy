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
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.Editor {
	[Export(typeof(IReplaceListenerProvider))]
	sealed class ReplEditorReplaceListenerProvider : IReplaceListenerProvider {
		public IReplaceListener Create(ITextView textView) {
			var replEditor = ReplEditorUtils.TryGetInstance(textView) as ReplEditor;
			if (replEditor == null)
				return null;
			return new ReplEditorReplaceListener(replEditor);
		}
	}

	sealed class ReplEditorReplaceListener : IReplaceListener {
		readonly ReplEditor replEditor;

		public ReplEditorReplaceListener(ReplEditor replEditor) => this.replEditor = replEditor ?? throw new ArgumentNullException(nameof(replEditor));

		public bool CanReplace(SnapshotSpan span, string newText) => replEditor.CanReplace(span, newText);
	}
}
