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
using Iced.Intel;

namespace dnSpy.Disassembly.X86 {
	sealed class NasmAppSettingsPage : DisassemblyCodeStyleAppSettingsPage {
		public override double Order => CodeStyleConstants.CODESTYLE_NASM_ORDER;
		public override Guid Guid => new Guid("929A1758-C8CC-450E-A214-90B390A846DF");
		public override string Title => CodeStyleConstants.NASM_NAME;

		public NasmAppSettingsPage(NasmDisassemblySettings x86DisassemblySettings)
			: base(x86DisassemblySettings, x86DisassemblySettings.Clone(), new NasmFormatter(SymbolResolver.Instance)) { }

		public override void OnApply() =>
			((NasmDisassemblySettings)x86DisassemblySettings).CopyTo((NasmDisassemblySettings)_global_x86DisassemblySettings);
	}
}
