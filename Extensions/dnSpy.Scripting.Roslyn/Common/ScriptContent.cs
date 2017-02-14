/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Scripting.Roslyn.Common {
	interface IScriptContent : IUIObjectProvider {
		void OnShow();
		void OnClose();
		void OnVisible();
		void OnHidden();
		double ZoomLevel { get; }
	}

	abstract class ScriptContent : IScriptContent {
		public object UIObject => scriptControl;
		public IInputElement FocusedElement => replEditor.FocusedElement;
		public FrameworkElement ZoomElement => replEditor.ZoomElement;
		public ScriptControlVM ScriptControlVM => scriptControlVM;
		public double ZoomLevel => replEditor.TextView.ZoomLevel;

		readonly IReplEditor replEditor;
		readonly ScriptControl scriptControl;
		readonly ScriptControlVM scriptControlVM;

		protected ScriptContent(IReplEditorProvider replEditorProvider, ReplEditorOptions replOpts, ReplSettings replSettings, IServiceLocator serviceLocator, string appearanceCategory) {
			replOpts.Roles.Add(PredefinedDsTextViewRoles.RoslynRepl);
			replEditor = replEditorProvider.Create(replOpts);
			replEditor.TextView.Options.SetOptionValue(DefaultWpfViewOptions.AppearanceCategory, appearanceCategory);
			scriptControl = new ScriptControl();
			scriptControl.SetTextEditorObject(replEditor.UIObject);
			scriptControlVM = CreateScriptControlVM(replEditor, serviceLocator, replSettings);
			scriptControlVM.OnCommandExecuted += ScriptControlVM_OnCommandExecuted;
			RoslynReplEditorUtils.AddInstance(scriptControlVM, replEditor.TextView);
			replEditor.Tag = this;
			scriptControl.DataContext = scriptControlVM;
		}

		void ScriptControlVM_OnCommandExecuted(object sender, EventArgs e) =>
			// Make sure the up/down arrow icons are updated
			CommandManager.InvalidateRequerySuggested();

		public static ScriptContent GetScriptContent(IReplEditor replEditor) => (ScriptContent)replEditor.Tag;
		protected abstract ScriptControlVM CreateScriptControlVM(IReplEditor replEditor, IServiceLocator serviceLocator, ReplSettings replSettings);
		public void OnClose() { }
		public void OnHidden() { }
		public void OnShow() { }
		public void OnVisible() => scriptControlVM.OnVisible();
	}
}
