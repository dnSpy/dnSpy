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
using System.Windows;
using System.Windows.Input;
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.TextEditor;
using dnSpy.Contracts.Themes;

namespace dnSpy.Scripting.Roslyn.Common {
	interface IScriptContent {
		object UIObject { get; }
		IInputElement FocusedElement { get; }
		FrameworkElement ScaleElement { get; }
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
	}

	abstract class ScriptContent : IScriptContent {
		public object UIObject => scriptControl;
		public IInputElement FocusedElement => replEditorUI.FocusedElement;
		public FrameworkElement ScaleElement => replEditorUI.ScaleElement;
		public ScriptControlVM ScriptControlVM => scriptControlVM;

		readonly IReplEditorUI replEditorUI;
		readonly ScriptControl scriptControl;
		readonly ScriptControlVM scriptControlVM;

		protected ScriptContent(IThemeManager themeManager, IReplEditorCreator replEditorCreator, ReplEditorOptions replOpts, IServiceLocator serviceLocator) {
			this.replEditorUI = replEditorCreator.Create(replOpts);
			this.scriptControl = new ScriptControl();
			this.scriptControl.SetTextEditorObject(this.replEditorUI.UIObject);
			this.scriptControlVM = CreateScriptControlVM(this.replEditorUI, serviceLocator);
			this.scriptControlVM.OnCommandExecuted += ScriptControlVM_OnCommandExecuted;
			this.replEditorUI.Tag = this;
			this.scriptControl.DataContext = this.scriptControlVM;
			themeManager.ThemeChanged += ThemeManager_ThemeChanged;
		}

		void ScriptControlVM_OnCommandExecuted(object sender, EventArgs e) {
			// Make sure the up/down arrow icons are updated
			CommandManager.InvalidateRequerySuggested();
		}

		public static ScriptContent GetScriptContent(IReplEditorUI replEditorUI) =>
			(ScriptContent)replEditorUI.Tag;

		protected abstract ScriptControlVM CreateScriptControlVM(IReplEditor replEditor, IServiceLocator serviceLocator);

		void ThemeManager_ThemeChanged(object sender, ThemeChangedEventArgs e) =>
			scriptControlVM.RefreshThemeFields();

		public void OnClose() {
		}

		public void OnShow() {
		}

		public void OnHidden() {
		}

		public void OnVisible() => scriptControlVM.OnVisible();
	}
}
