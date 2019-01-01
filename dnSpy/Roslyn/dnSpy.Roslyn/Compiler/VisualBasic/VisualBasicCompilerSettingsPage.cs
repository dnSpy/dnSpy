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
using dnSpy.Contracts.Settings.Dialog;

namespace dnSpy.Roslyn.Compiler.VisualBasic {
	sealed class VisualBasicCompilerSettingsPage : AppSettingsPage {
		readonly VisualBasicCompilerSettingsBase _global_compilerSettings;
		readonly VisualBasicCompilerSettingsBase compilerSettings;

		public override double Order => AppSettingsConstants.ORDER_COMPILER_SETTINGS_VISUALBASIC;
		public VisualBasicCompilerSettings Settings => compilerSettings;
		public override Guid ParentGuid => new Guid(AppSettingsConstants.GUID_COMPILER);
		public override Guid Guid => new Guid("D1163C8F-F590-4A2D-9539-EE4E9CCE64B2");
		public override string Title => "Visual Basic";
		public override object UIObject => this;

		public VisualBasicCompilerSettingsPage(VisualBasicCompilerSettingsBase compilerSettings) {
			_global_compilerSettings = compilerSettings;
			this.compilerSettings = compilerSettings.Clone();
		}

		public override void OnApply() => compilerSettings.CopyTo(_global_compilerSettings);
	}
}
