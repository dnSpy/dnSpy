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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.Settings {
	[Export(typeof(IAppSettingsPageProvider))]
	sealed class DebuggerAppSettingsPageProvider : IAppSettingsPageProvider {
		readonly DebuggerSettingsImpl debuggerSettingsImpl;

		[ImportingConstructor]
		DebuggerAppSettingsPageProvider(DebuggerSettingsImpl debuggerSettingsImpl) => this.debuggerSettingsImpl = debuggerSettingsImpl;

		public IEnumerable<AppSettingsPage> Create() {
			yield return new DebuggerAppSettingsPage(debuggerSettingsImpl);
		}
	}

	sealed class DebuggerAppSettingsPage : AppSettingsPage {
		readonly DebuggerSettingsImpl _global_settings;

		internal static readonly Guid PageGuid = new Guid("8D2BC2FB-5CA4-4907-84C7-F4F705327AC8");
		public override Guid Guid => PageGuid;
		public DebuggerSettingsBase Settings { get; }
		public override double Order => AppSettingsConstants.ORDER_DEBUGGER;
		public override string Title => dnSpy_Debugger_Resources.DebuggerOptDlgTab;
		public override object UIObject => this;

		public DebuggerAppSettingsPage(DebuggerSettingsImpl debuggerSettingsImpl) {
			_global_settings = debuggerSettingsImpl;
			Settings = debuggerSettingsImpl.Clone();
		}

		public override void OnApply() => Settings.CopyTo(_global_settings);
	}
}
