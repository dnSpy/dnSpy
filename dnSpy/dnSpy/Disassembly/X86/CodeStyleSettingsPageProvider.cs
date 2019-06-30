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

using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Settings.Dialog;

namespace dnSpy.Disassembly.X86 {
	[Export(typeof(IAppSettingsPageProvider))]
	sealed class CodeStyleSettingsPageProvider : IAppSettingsPageProvider {
		readonly MasmDisassemblySettingsImpl masmSettings;
		readonly NasmDisassemblySettingsImpl nasmSettings;
		readonly GasDisassemblySettingsImpl gasSettings;

		[ImportingConstructor]
		CodeStyleSettingsPageProvider(MasmDisassemblySettingsImpl masmSettings, NasmDisassemblySettingsImpl nasmSettings, GasDisassemblySettingsImpl gasSettings) {
			this.masmSettings = masmSettings;
			this.nasmSettings = nasmSettings;
			this.gasSettings = gasSettings;
		}

		public IEnumerable<AppSettingsPage> Create() {
			yield return new MasmAppSettingsPage(masmSettings);
			yield return new NasmAppSettingsPage(nasmSettings);
			yield return new GasAppSettingsPage(gasSettings);
		}
	}
}
