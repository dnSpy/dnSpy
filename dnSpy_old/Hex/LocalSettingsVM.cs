/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.HexEditor;
using dnSpy.MVVM;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Hex {
	sealed class LocalSettingsVM : ViewModelBase {
		readonly LocalHexSettings origOptions;

		public ICommand ReinitializeCommand {
			get { return new RelayCommand(a => Reinitialize()); }
		}

		public ICommand ResetToDefaultCommand {
			get { return new RelayCommand(a => ResetToDefault(), a => ResetToDefaultCanExecute()); }
		}

		public NullableInt32VM BytesGroupCountVM {
			get { return bytesGroupCountVM; }
		}
		readonly NullableInt32VM bytesGroupCountVM;

		public NullableInt32VM BytesPerLineVM {
			get { return bytesPerLineVM; }
		}
		readonly NullableInt32VM bytesPerLineVM;

		public bool? UseHexPrefix {
			get { return useHexPrefix; }
			set {
				if (useHexPrefix != value) {
					useHexPrefix = value;
					OnPropertyChanged("UseHexPrefix");
				}
			}
		}
		bool? useHexPrefix;

		public bool? ShowAscii {
			get { return showAscii; }
			set {
				if (showAscii != value) {
					showAscii = value;
					OnPropertyChanged("ShowAscii");
				}
			}
		}
		bool? showAscii;

		public bool? LowerCaseHex {
			get { return lowerCaseHex; }
			set {
				if (lowerCaseHex != value) {
					lowerCaseHex = value;
					OnPropertyChanged("LowerCaseHex");
				}
			}
		}
		bool? lowerCaseHex;

		public Int32VM HexOffsetSizeVM {
			get { return hexOffsetSizeVM; }
		}
		readonly Int32VM hexOffsetSizeVM;

		public bool UseRelativeOffsets {
			get { return useRelativeOffsets; }
			set {
				if (useRelativeOffsets != value) {
					useRelativeOffsets = value;
					OnPropertyChanged("UseRelativeOffsets");
				}
			}
		}
		bool useRelativeOffsets;

		public UInt64VM BaseOffsetVM {
			get { return baseOffsetVM; }
		}
		readonly UInt64VM baseOffsetVM;

		public NullableUInt64VM StartOffsetVM {
			get { return startOffsetVM; }
		}
		readonly NullableUInt64VM startOffsetVM;

		public NullableUInt64VM EndOffsetVM {
			get { return endOffsetVM; }
		}
		readonly NullableUInt64VM endOffsetVM;

		public EnumListVM AsciiEncodingVM {
			get { return asciiEncodingVM; }
		}
		readonly EnumListVM asciiEncodingVM;
		readonly EnumVM[] asciiEncodingList = new EnumVM[] {
			new EnumVM(AsciiEncoding.ASCII, "ASCII"),
			new EnumVM(AsciiEncoding.ANSI, "ANSI"),
			new EnumVM(AsciiEncoding.UTF7, "UTF-7"),
			new EnumVM(AsciiEncoding.UTF8, "UTF-8"),
			new EnumVM(AsciiEncoding.UTF32, "UTF-32"),
			new EnumVM(AsciiEncoding.Unicode, "Unicode"),
			new EnumVM(AsciiEncoding.BigEndianUnicode, "BE Unicode"),
			new EnumVM(AsciiEncoding_DEFAULT, "Default"),
		};
		const AsciiEncoding AsciiEncoding_DEFAULT = (AsciiEncoding)(-1);

		public LocalSettingsVM(LocalHexSettings options) {
			this.origOptions = options;
			this.bytesGroupCountVM = new NullableInt32VM(a => HasErrorUpdated());
			this.bytesPerLineVM = new NullableInt32VM(a => HasErrorUpdated(), true) {
				Min = 0,
				Max = HexSettings.MAX_BYTES_PER_LINE,
			};
			this.hexOffsetSizeVM = new Int32VM(a => HasErrorUpdated(), true) {
				Min = 0,
				Max = 64,
			};
			this.baseOffsetVM = new UInt64VM(a => HasErrorUpdated());
			this.startOffsetVM = new NullableUInt64VM(a => HasErrorUpdated());
			this.endOffsetVM = new NullableUInt64VM(a => HasErrorUpdated());
			this.asciiEncodingVM = new EnumListVM(asciiEncodingList);

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

		void Reinitialize() {
			InitializeFrom(origOptions);
		}

		public LocalHexSettings CreateLocalHexSettings() {
			return CopyTo(new LocalHexSettings());
		}

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
					bytesGroupCountVM.HasError ||
					bytesPerLineVM.HasError ||
					hexOffsetSizeVM.HasError ||
					baseOffsetVM.HasError ||
					StartOffsetVM.HasError ||
					EndOffsetVM.HasError;
			}
		}
	}
}
