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
using System.Linq;
using System.Diagnostics;
using dnSpy.Contracts.Disassembly.Viewer;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Properties;
using System.Collections.ObjectModel;

namespace dnSpy.Disassembly.Viewer {
	sealed class DisassemblyAppSettingsPage : AppSettingsPage {
		readonly DisassemblyViewerServiceSettings _global_viewerSettings;
		readonly DisassemblyContentSettingsBase _global_x86DisassemblySettings;
		readonly DisassemblyContentSettingsBase x86DisassemblySettings;

		public override double Order => AppSettingsConstants.ORDER_DISASSEMBLER;
		public DisassemblyContentSettings Settings => x86DisassemblySettings;
		public override Guid ParentGuid => Guid.Empty;
		public override Guid Guid => new Guid(AppSettingsConstants.GUID_DISASSEMBLER);
		public override string Title => dnSpy_Resources.DisassemblerDlgTabTitle;
		public override object UIObject => this;

		public bool NewTab {
			get => newTab;
			set {
				if (newTab != value) {
					newTab = value;
					OnPropertyChanged(nameof(NewTab));
				}
			}
		}
		bool newTab;

		public ObservableCollection<X86DisassemblerVM> X86DisassemblerVM { get; }

		public X86DisassemblerVM SelectedX86DisassemblerVM {
			get => selectedX86DisassemblerVM;
			set {
				if (selectedX86DisassemblerVM != value) {
					selectedX86DisassemblerVM = value;
					OnPropertyChanged(nameof(SelectedX86DisassemblerVM));
				}
			}
		}
		X86DisassemblerVM selectedX86DisassemblerVM;

		static readonly (X86Disassembler disasm, string name)[] x86DisasmInfos = new (X86Disassembler disasm, string name)[] {
			(X86Disassembler.Masm, CodeStyleConstants.MASM_NAME),
			(X86Disassembler.Nasm, CodeStyleConstants.NASM_NAME),
			(X86Disassembler.Gas, CodeStyleConstants.GAS_NAME),
		};

		public DisassemblyAppSettingsPage(DisassemblyViewerServiceSettings viewerSettings, DisassemblyContentSettingsBase x86DisassemblySettings) {
			_global_viewerSettings = viewerSettings;
			_global_x86DisassemblySettings = x86DisassemblySettings;
			this.x86DisassemblySettings = x86DisassemblySettings.Clone();

			NewTab = viewerSettings.OpenNewTab;
			X86DisassemblerVM = new ObservableCollection<X86DisassemblerVM>(x86DisasmInfos.Select(a => new X86DisassemblerVM(a.disasm, a.name)));

			var tox86DisasmName = x86DisasmInfos.ToDictionary(k => k.disasm, v => v.name);
			var x86Disassembler = x86DisassemblySettings.X86Disassembler;
			bool found = tox86DisasmName.TryGetValue(x86Disassembler, out var disasmName);
			Debug.Assert(found);
			if (!found) {
				x86Disassembler = X86Disassembler.Masm;
				disasmName = tox86DisasmName[x86Disassembler];
			}
			selectedX86DisassemblerVM = X86DisassemblerVM.First(a => a.Disassembler == x86Disassembler);
		}

		public override void OnApply() {
			_global_viewerSettings.OpenNewTab = NewTab;
			x86DisassemblySettings.X86Disassembler = SelectedX86DisassemblerVM.Disassembler;
			x86DisassemblySettings.CopyTo(_global_x86DisassemblySettings);
		}
	}

	sealed class X86DisassemblerVM : ViewModelBase {
		public X86Disassembler Disassembler { get; }
		public string Name { get; }

		public X86DisassemblerVM(X86Disassembler disassembler, string name) {
			Disassembler = disassembler;
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}
	}
}
