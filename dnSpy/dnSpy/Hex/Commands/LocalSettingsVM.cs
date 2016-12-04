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

using System.Linq;
using System.Windows.Input;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor.HexGroups;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Hex.Commands {
	sealed class LocalSettingsVM : ViewModelBase {
		readonly LocalGroupOptions origOptions;
		readonly LocalGroupOptions defaultOptions;

		public ICommand ReinitializeCommand => new RelayCommand(a => Reinitialize());
		public ICommand ResetToDefaultCommand => new RelayCommand(a => ResetToDefault(), a => ResetToDefaultCanExecute());

		public bool ShowOffsetColumn {
			get { return showOffset; }
			set {
				if (showOffset != value) {
					showOffset = value;
					OnPropertyChanged(nameof(ShowOffsetColumn));
				}
			}
		}
		bool showOffset;

		public bool ShowValuesColumn {
			get { return showValues; }
			set {
				if (showValues != value) {
					showValues = value;
					OnPropertyChanged(nameof(ShowValuesColumn));
				}
			}
		}
		bool showValues;

		public bool ShowAsciiColumn {
			get { return showAscii; }
			set {
				if (showAscii != value) {
					showAscii = value;
					OnPropertyChanged(nameof(ShowAsciiColumn));
				}
			}
		}
		bool showAscii;

		public bool UseRelativePositions {
			get { return useRelativeOffsets; }
			set {
				if (useRelativeOffsets != value) {
					useRelativeOffsets = value;
					OnPropertyChanged(nameof(UseRelativePositions));
				}
			}
		}
		bool useRelativeOffsets;

		public UInt64VM StartPositionVM { get; }
		public UInt64VM EndPositionVM { get; }
		public UInt64VM BasePositionVM { get; }
		public Int32VM OffsetBitSizeVM { get; }
		public Int32VM BytesPerLineVM { get; }
		public EnumListVM HexValuesDisplayFormatVM { get; }
		static readonly EnumVM[] hexValuesDisplayFormatList = SettingsConstants.ValueFormatList.Select(a => new EnumVM(a.Key, a.Value)).ToArray();

		public LocalSettingsVM(LocalGroupOptions options, LocalGroupOptions defaultOptions) {
			origOptions = options;
			this.defaultOptions = defaultOptions;
			BytesPerLineVM = new Int32VM(a => HasErrorUpdated(), true) {
				Min = HexBufferLineProviderOptions.MinBytesPerLine,
				Max = HexBufferLineProviderOptions.MaxBytesPerLine,
			};
			OffsetBitSizeVM = new Int32VM(a => HasErrorUpdated(), true) {
				Min = HexBufferLineProviderOptions.MinOffsetBitSize,
				Max = HexBufferLineProviderOptions.MaxOffsetBitSize,
			};
			BasePositionVM = new UInt64VM(a => HasErrorUpdated());
			StartPositionVM = new UInt64VM(a => HasErrorUpdated());
			EndPositionVM = new UInt64VM(a => HasErrorUpdated());
			HexValuesDisplayFormatVM = new EnumListVM(hexValuesDisplayFormatList);

			Reinitialize();
		}

		public LocalGroupOptions TryGetLocalGroupOptions() {
			var options = new LocalGroupOptions();
			options.ShowOffsetColumn = ShowOffsetColumn;
			options.ShowValuesColumn = ShowValuesColumn;
			options.ShowAsciiColumn = ShowAsciiColumn;
			if (StartPositionVM.HasError)
				return null;
			options.StartPosition = StartPositionVM.Value;
			if (EndPositionVM.HasError)
				return null;
			options.EndPosition = new HexPosition(EndPositionVM.Value) + 1;
			if (BasePositionVM.HasError)
				return null;
			options.BasePosition = BasePositionVM.Value;
			options.UseRelativePositions = UseRelativePositions;
			if (OffsetBitSizeVM.HasError)
				return null;
			options.OffsetBitSize = OffsetBitSizeVM.Value;
			options.HexValuesDisplayFormat = (HexValuesDisplayFormat)HexValuesDisplayFormatVM.SelectedItem;
			if (BytesPerLineVM.HasError)
				return null;
			options.BytesPerLine = BytesPerLineVM.Value;
			return options;
		}

		bool ResetToDefaultCanExecute() => !defaultOptions.Equals(TryGetLocalGroupOptions());
		void ResetToDefault() => InitializeFrom(defaultOptions);
		void Reinitialize() => InitializeFrom(origOptions);

		void InitializeFrom(LocalGroupOptions options) {
			ShowOffsetColumn = options.ShowOffsetColumn;
			ShowValuesColumn = options.ShowValuesColumn;
			ShowAsciiColumn = options.ShowAsciiColumn;
			StartPositionVM.Value = options.StartPosition.ToUInt64();
			EndPositionVM.Value = (options.EndPosition > HexPosition.Zero ? options.EndPosition - 1 : options.EndPosition).ToUInt64();
			BasePositionVM.Value = options.BasePosition.ToUInt64();
			UseRelativePositions = options.UseRelativePositions;
			OffsetBitSizeVM.Value = options.OffsetBitSize;
			HexValuesDisplayFormatVM.SelectedItem = options.HexValuesDisplayFormat;
			BytesPerLineVM.Value = options.BytesPerLine;
		}

		public override bool HasError {
			get {
				return
					BytesPerLineVM.HasError ||
					OffsetBitSizeVM.HasError ||
					BasePositionVM.HasError ||
					StartPositionVM.HasError ||
					EndPositionVM.HasError;
			}
		}
	}
}
