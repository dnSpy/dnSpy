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
	sealed class MasmAppSettingsPage : DisassemblyCodeStyleAppSettingsPage {
		public override double Order => CodeStyleConstants.CODESTYLE_MASM_ORDER;
		public override Guid Guid => new Guid("B92522C8-3B79-4CC0-8E15-47F62F271269");
		public override string Title => CodeStyleConstants.MASM_NAME;

		public MasmAppSettingsPage(MasmDisassemblySettings x86DisassemblySettings)
			: base(x86DisassemblySettings, x86DisassemblySettings.Clone(), new MasmFormatter(new MasmFormatterOptions(), SymbolResolver.Instance)) { }

		protected override void InitializeFormatterOptionsCore(FormatterOptions options) { }

		public override void OnApply() =>
			((MasmDisassemblySettings)x86DisassemblySettings).CopyTo((MasmDisassemblySettings)_global_x86DisassemblySettings);
	}
}
