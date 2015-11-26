/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Languages;

namespace dnSpy.Files.Tabs.TextEditor.ToolTips {
	struct ReferenceInfo {
		public ILanguage Language;
		public object Reference;

		public ReferenceInfo(ILanguage language, object @ref) {
			this.Language = language;
			this.Reference = @ref;
		}
	}

	interface IToolTipReferenceFinder {
		ReferenceInfo? GetReference(MouseEventArgs e);
	}

	sealed class ToolTipHelper {
		readonly ICodeToolTipManager codeToolTipManager;
		readonly IToolTipReferenceFinder refFinder;
		ToolTip toolTip;
		ICSharpCode.AvalonEdit.TextEditor textEditor;

		public ToolTipHelper(ICodeToolTipManager codeToolTipManager, IToolTipReferenceFinder refFinder) {
			this.codeToolTipManager = codeToolTipManager;
			this.refFinder = refFinder;
			this.toolTip = new ToolTip();
		}

		public void Initialize(ICSharpCode.AvalonEdit.TextEditor textEditor) {
			this.textEditor = textEditor;
			var view = textEditor.TextArea.TextView;
			view.MouseHover += TextView_MouseHover;
			view.MouseHoverStopped += (s, e) => Close();
		}

		void TextView_MouseHover(object sender, MouseEventArgs e) {
			var info = refFinder.GetReference(e);
			if (info == null || info.Value.Language == null || info.Value.Reference == null)
				return;

			var ttContent = codeToolTipManager.CreateToolTip(info.Value.Language, info.Value.Reference);
			if (ttContent == null)
				return;
			Close();
			toolTip = new ToolTip {
				Content = ttContent,
				Style = (Style)textEditor.FindResource("CodeToolTip"),
				IsOpen = true,
			};
		}

		public void Close() {
			if (toolTip != null) {
				toolTip.IsOpen = false;
				toolTip = null;
			}
		}
	}
}
