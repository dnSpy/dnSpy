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

using System.Windows.Input;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.HexEditor;
using dnSpy.Contracts.Properties;

namespace dnSpy.Contracts.Hex {
	sealed class LocalSettingsVM : ViewModelBase {
		readonly LocalHexSettings origOptions;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public ICommand ResetToDefaultCommand => new RelayCommand(a => ResetToDefault(), a => ResetToDefaultCanExecute());

		public NullableInt32VM BytesGroupCountVM { get; }
		public NullableInt32VM BytesPerLineVM { get; }

		public bool? UseHexPrefix {
			get { return useHexPrefix; }
			set {
				if (useHexPrefix != value) {
					useHexPrefix = value;
					OnPropertyChanged(nameof(UseHexPrefix));
				}
			}
		}
		bool? useHexPrefix;

		public bool? ShowAscii {
			get { return showAscii; }
			set {
				if (showAscii != value) {
					showAscii = value;
					OnPropertyChanged(nameof(ShowAscii));
				}
			}
		}
		bool? showAscii;

		public bool? LowerCaseHex {
			get { return lowerCaseHex; }
			set {
				if (lowerCaseHex != value) {
					lowerCaseHex = value;
					OnPropertyChanged(nameof(LowerCaseHex));
				}
			}
		}
		bool? lowerCaseHex;

		public Int32VM HexOffsetSizeVM { get; }

		public bool UseRelativeOffsets {
			get { return useRelativeOffsets; }
			set {
				if (useRelativeOffsets != value) {
					useRelativeOffsets = value;
					OnPropertyChanged(nameof(UseRelativeOffsets));
				}
			}
		}
		bool useRelativeOffsets;

		public UInt64VM BaseOffsetVM { get; }
		public NullableUInt64VM StartOffsetVM { get; }

		public NullableUInt64VM EndOffsetVM { get; }

		public EnumListVM AsciiEncodingVM { get; }
		readonly EnumVM[] asciiEncodingList = new EnumVM[] {
			new EnumVM(AsciiEncoding.ASCII, dnSpy_Contracts_DnSpy.HexEditor_CharacterEncoding_ASCII_2),
			new EnumVM(AsciiEncoding.ANSI, dnSpy_Contracts_DnSpy.HexEditor_CharacterEncoding_ANSI_2),
			new EnumVM(AsciiEncoding.UTF7, dnSpy_Contracts_DnSpy.HexEditor_CharacterEncoding_UTF7_2),
			new EnumVM(AsciiEncoding.UTF8, dnSpy_Contracts_DnSpy.HexEditor_CharacterEncoding_UTF8_2),
			new EnumVM(AsciiEncoding.UTF32, dnSpy_Contracts_DnSpy.HexEditor_CharacterEncoding_UTF32_2),
			new EnumVM(AsciiEncoding.Unicode, dnSpy_Contracts_DnSpy.HexEditor_CharacterEncoding_UNICODE_2),
			new EnumVM(AsciiEncoding.BigEndianUnicode, dnSpy_Contracts_DnSpy.HexEditor_CharacterEncoding_BIG_ENDIAN_UNICODE_2),
			new EnumVM(AsciiEncoding_DEFAULT, dnSpy_Contracts_DnSpy.HexEditor_Default2),
		};
		const AsciiEncoding AsciiEncoding_DEFAULT = (AsciiEncoding)(-1);

		public LocalSettingsVM(LocalHexSettings options) {
			this.origOptions = options;
			this.BytesGroupCountVM = new NullableInt32VM(a => HasErrorUpdated());
			this.BytesPerLineVM = new NullableInt32VM(a => HasErrorUpdated(), true) {
				Min = 0,
				Max = HexEditorSettings.MAX_BYTES_PER_LINE,
			};
			this.HexOffsetSizeVM = new Int32VM(a => HasErrorUpdated(), true) {
				Min = 0,
				Max = 64,
			};
			this.BaseOffsetVM = new UInt64VM(a => HasErrorUpdated());
			this.StartOffsetVM = new NullableUInt64VM(a => HasErrorUpdated());
			this.EndOffsetVM = new NullableUInt64VM(a => HasErrorUpdated());
			this.AsciiEncodingVM = new EnumListVM(asciiEncodingList);

			Reinitialize();
		}

		bool ResetToDefaultCanExecute() {
			return
				(BytesGroupCountVM.HasError || BytesGroupCountVM.Value != null) ||
				(BytesPerLineVM.HasError || BytesPerLineVM.Value != null) ||
				UseHexPrefix != null ||
				ShowAscii != null ||
				LowerCaseHex != null ||
				(AsciiEncoding)AsciiEncodingVM.SelectedItem != AsciiEncoding_DEFAULT ||
				(HexOffsetSizeVM.HasError || HexOffsetSizeVM.Value != 0) ||
				UseRelativeOffsets ||
				(BaseOffsetVM.HasError || BaseOffsetVM.Value != 0) ||
				(StartOffsetVM.HasError || StartOffsetVM.Value != null) ||
				(EndOffsetVM.HasError || EndOffsetVM.Value != null);
		}

		void ResetToDefault() {
			BytesGroupCountVM.Value = null;
			BytesPerLineVM.Value = null;
			UseHexPrefix = null;
			ShowAscii = null;
			LowerCaseHex = null;
			AsciiEncodingVM.SelectedItem = AsciiEncoding_DEFAULT;
			HexOffsetSizeVM.Value = 0;
			UseRelativeOffsets = false;
			BaseOffsetVM.Value = 0;
			StartOffsetVM.Value = null;
			EndOffsetVM.Value = null;
		}

		void Reinitialize() => InitializeFrom(origOptions);
		public LocalHexSettings CreateLocalHexSettings() => CopyTo(new LocalHexSettings());

		void InitializeFrom(LocalHexSettings options) {
			BytesGroupCountVM.Value = options.BytesGroupCount;
			BytesPerLineVM.Value = options.BytesPerLine;
			UseHexPrefix = options.UseHexPrefix;
			ShowAscii = options.ShowAscii;
			LowerCaseHex = options.LowerCaseHex;
			AsciiEncodingVM.SelectedItem = options.AsciiEncoding ?? AsciiEncoding_DEFAULT;
			HexOffsetSizeVM.Value = options.HexOffsetSize;
			UseRelativeOffsets = options.UseRelativeOffsets;
			BaseOffsetVM.Value = options.BaseOffset;
			StartOffsetVM.Value = options.StartOffset;
			EndOffsetVM.Value = options.EndOffset;
		}

		LocalHexSettings CopyTo(LocalHexSettings options) {
			options.BytesGroupCount = BytesGroupCountVM.Value;
			options.BytesPerLine = BytesPerLineVM.Value;
			options.UseHexPrefix = UseHexPrefix;
			options.ShowAscii = ShowAscii;
			options.LowerCaseHex = LowerCaseHex;
			var val = (AsciiEncoding)AsciiEncodingVM.SelectedItem;
			options.AsciiEncoding = val == AsciiEncoding_DEFAULT ? (AsciiEncoding?)null : val;
			options.HexOffsetSize = HexOffsetSizeVM.Value;
			options.UseRelativeOffsets = UseRelativeOffsets;
			options.BaseOffset = BaseOffsetVM.Value;
			options.StartOffset = StartOffsetVM.Value;
			options.EndOffset = EndOffsetVM.Value;
			return options;
		}

		public override bool HasError {
			get {
				return
					BytesGroupCountVM.HasError ||
					BytesPerLineVM.HasError ||
					HexOffsetSizeVM.HasError ||
					BaseOffsetVM.HasError ||
					StartOffsetVM.HasError ||
					EndOffsetVM.HasError;
			}
		}
	}
}
