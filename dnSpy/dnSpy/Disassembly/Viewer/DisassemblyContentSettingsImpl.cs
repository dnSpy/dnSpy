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
using System.ComponentModel;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Disassembly.Viewer;
using dnSpy.Contracts.Settings;

namespace dnSpy.Disassembly.Viewer {
	class DisassemblyContentSettingsBase : DisassemblyContentSettings {
		public override bool ShowInstructionAddress {
			get => showInstructionAddress;
			set {
				if (value != showInstructionAddress) {
					showInstructionAddress = value;
					OnPropertyChanged(nameof(ShowInstructionAddress));
				}
			}
		}
		bool showInstructionAddress = true;

		public override bool ShowInstructionBytes {
			get => showInstructionBytes;
			set {
				if (value != showInstructionBytes) {
					showInstructionBytes = value;
					OnPropertyChanged(nameof(ShowInstructionBytes));
				}
			}
		}
		bool showInstructionBytes = true;

		public override bool EmptyLineBetweenBasicBlocks {
			get => emptyLineBetweenBasicBlocks;
			set {
				if (value != emptyLineBetweenBasicBlocks) {
					emptyLineBetweenBasicBlocks = value;
					OnPropertyChanged(nameof(EmptyLineBetweenBasicBlocks));
				}
			}
		}
		bool emptyLineBetweenBasicBlocks = true;

		public override bool AddLabels {
			get => addLabels;
			set {
				if (value != addLabels) {
					addLabels = value;
					OnPropertyChanged(nameof(AddLabels));
				}
			}
		}
		bool addLabels = true;

		public override bool ShowILCode {
			get => showILCode;
			set {
				if (value != showILCode) {
					showILCode = value;
					OnPropertyChanged(nameof(ShowILCode));
				}
			}
		}
		bool showILCode;

		public override bool ShowCode {
			get => showCode;
			set {
				if (value != showCode) {
					showCode = value;
					OnPropertyChanged(nameof(ShowCode));
				}
			}
		}
		bool showCode = true;

		public override X86Disassembler X86Disassembler {
			get => x86Disassembler;
			set {
				var newValue = value;
				if ((uint)newValue > (uint)X86Disassembler.Gas)
					newValue = X86Disassembler.Masm;
				if (newValue != x86Disassembler) {
					x86Disassembler = newValue;
					OnPropertyChanged(nameof(X86Disassembler));
				}
			}
		}
		X86Disassembler x86Disassembler = X86Disassembler.Masm;

		public DisassemblyContentSettingsBase Clone() => CopyTo(new DisassemblyContentSettingsBase());

		public DisassemblyContentSettingsBase CopyTo(DisassemblyContentSettingsBase other) {
			if (other == null)
				throw new ArgumentNullException(nameof(other));
			other.ShowInstructionAddress = ShowInstructionAddress;
			other.ShowInstructionBytes = ShowInstructionBytes;
			other.EmptyLineBetweenBasicBlocks = EmptyLineBetweenBasicBlocks;
			other.AddLabels = AddLabels;
			other.ShowILCode = ShowILCode;
			other.ShowCode = ShowCode;
			other.X86Disassembler = X86Disassembler;
			return other;
		}
	}

	[Export(typeof(DisassemblyContentSettings))]
	[Export(typeof(DisassemblyContentSettingsImpl))]
	sealed class DisassemblyContentSettingsImpl : DisassemblyContentSettingsBase {
		static readonly Guid SETTINGS_GUID = new Guid("3126AB8A-167A-4071-8335-E16D6188F6C9");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		DisassemblyContentSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			ShowInstructionAddress = sect.Attribute<bool?>(nameof(ShowInstructionAddress)) ?? ShowInstructionAddress;
			ShowInstructionBytes = sect.Attribute<bool?>(nameof(ShowInstructionBytes)) ?? ShowInstructionBytes;
			EmptyLineBetweenBasicBlocks = sect.Attribute<bool?>(nameof(EmptyLineBetweenBasicBlocks)) ?? EmptyLineBetweenBasicBlocks;
			AddLabels = sect.Attribute<bool?>(nameof(AddLabels)) ?? AddLabels;
			ShowILCode = sect.Attribute<bool?>(nameof(ShowILCode)) ?? ShowILCode;
			ShowCode = sect.Attribute<bool?>(nameof(ShowCode)) ?? ShowCode;
			X86Disassembler = sect.Attribute<X86Disassembler?>(nameof(X86Disassembler)) ?? X86Disassembler;

			PropertyChanged += OnPropertyChanged;
		}

		void OnPropertyChanged(object sender, PropertyChangedEventArgs e) => Save();

		void Save() {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(ShowInstructionAddress), ShowInstructionAddress);
			sect.Attribute(nameof(ShowInstructionBytes), ShowInstructionBytes);
			sect.Attribute(nameof(EmptyLineBetweenBasicBlocks), EmptyLineBetweenBasicBlocks);
			sect.Attribute(nameof(AddLabels), AddLabels);
			sect.Attribute(nameof(ShowILCode), ShowILCode);
			sect.Attribute(nameof(ShowCode), ShowCode);
			sect.Attribute(nameof(X86Disassembler), X86Disassembler);
		}
	}
}
