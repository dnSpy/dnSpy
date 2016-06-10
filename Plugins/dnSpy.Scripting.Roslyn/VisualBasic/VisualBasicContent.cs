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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Themes;
using dnSpy.Scripting.Roslyn.Common;

namespace dnSpy.Scripting.Roslyn.VisualBasic {
	interface IVisualBasicContent : IScriptContent {
	}

	[Export(typeof(IVisualBasicContent))]
	sealed class VisualBasicContent : ScriptContent, IVisualBasicContent {
		[ImportingConstructor]
		VisualBasicContent(IThemeManager themeManager, IReplEditorCreator replEditorCreator, IServiceLocator serviceLocator)
			: base(themeManager, replEditorCreator, CreateReplEditorOptions(), serviceLocator) {
		}

		protected override ScriptControlVM CreateScriptControlVM(IReplEditor replEditor, IServiceLocator serviceLocator) =>
			new VisualBasicControlVM(replEditor, serviceLocator);

		static ReplEditorOptions CreateReplEditorOptions() {
			var options = new ReplEditorOptions {
				MenuGuid = new Guid(MenuConstants.GUIDOBJ_REPL_TEXTEDITORCONTROL_GUID),
				ContentTypeGuid = new Guid(ContentTypes.REPL_VISUALBASIC_ROSLYN),
			};
			options.Roles.Add(RoslynScriptingTextViewRoles.VisualBasicRepl);
			return options;
		}
	}
}
