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

using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Properties;

namespace dnSpy.Hex.Settings {
	abstract class GeneralAppSettingsPageBase : AppSettingsPage, INotifyPropertyChanged {
		public sealed override string Title => dnSpy_Resources.GeneralSettings;
		public sealed override object UIObject => this;

		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		public bool EnableHighlightCurrentLine {
			get { return enableHighlightCurrentLine; }
			set {
				if (enableHighlightCurrentLine != value) {
					enableHighlightCurrentLine = value;
					OnPropertyChanged(nameof(EnableHighlightCurrentLine));
				}
			}
		}
		bool enableHighlightCurrentLine;

		public bool HighlightStructureUnderMouseCursor {
			get { return highlightStructureUnderMouseCursor; }
			set {
				if (highlightStructureUnderMouseCursor != value) {
					highlightStructureUnderMouseCursor = value;
					OnPropertyChanged(nameof(HighlightStructureUnderMouseCursor));
				}
			}
		}
		bool highlightStructureUnderMouseCursor;

		public bool HighlightCurrentValue {
			get { return highlightCurrentValue; }
			set {
				if (highlightCurrentValue != value) {
					highlightCurrentValue = value;
					OnPropertyChanged(nameof(HighlightCurrentValue));
				}
			}
		}
		bool highlightCurrentValue;

		public bool HighlightActiveColumn {
			get { return highlightActiveColumn; }
			set {
				if (highlightActiveColumn != value) {
					highlightActiveColumn = value;
					OnPropertyChanged(nameof(HighlightActiveColumn));
				}
			}
		}
		bool highlightActiveColumn;

		public bool ValuesLowerCaseHex {
			get { return valuesLowerCaseHex; }
			set {
				if (valuesLowerCaseHex != value) {
					valuesLowerCaseHex = value;
					OnPropertyChanged(nameof(ValuesLowerCaseHex));
				}
			}
		}
		bool valuesLowerCaseHex;

		public bool OffsetLowerCaseHex {
			get { return offsetLowerCaseHex; }
			set {
				if (offsetLowerCaseHex != value) {
					offsetLowerCaseHex = value;
					OnPropertyChanged(nameof(OffsetLowerCaseHex));
				}
			}
		}
		bool offsetLowerCaseHex;

		public bool EnableColorization {
			get { return enableColorization; }
			set {
				if (enableColorization != value) {
					enableColorization = value;
					OnPropertyChanged(nameof(EnableColorization));
				}
			}
		}
		bool enableColorization;

		public Int32VM GroupSizeInBytesVM { get; }

		public EnumListVM HexOffsetFormatVM { get; }
		public HexOffsetFormat HexOffsetFormat {
			get { return (HexOffsetFormat)HexOffsetFormatVM.SelectedItem; }
			set { HexOffsetFormatVM.SelectedItem = value; }
		}
		static readonly EnumVM[] hexOffsetFormatList = new EnumVM[] {
			new EnumVM(HexOffsetFormat.Hex, "6789ABCD"),
			new EnumVM(HexOffsetFormat.HexCSharp, "0x6789ABCD"),
			new EnumVM(HexOffsetFormat.HexVisualBasic, "&H6789ABCD"),
			new EnumVM(HexOffsetFormat.HexAssembly, "6789ABCDh"),
		};

		public EnumListVM EncodingInfoVM { get; }
		public EncodingInfo EncodingInfo {
			get { return (EncodingInfo)EncodingInfoVM.SelectedItem; }
			set { EncodingInfoVM.SelectedItem = value; }
		}

		readonly CommonEditorOptions options;

		protected GeneralAppSettingsPageBase(CommonEditorOptions options) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			this.options = options;
			GroupSizeInBytesVM = new Int32VM(a => { }, useDecimal: true) { Min = 0, Max = int.MaxValue };
			HexOffsetFormatVM = new EnumListVM(hexOffsetFormatList);
			EncodingInfoVM = new EnumListVM(Encoding.GetEncodings().OrderBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase).Select(a => new EnumVM(a, a.DisplayName)).ToArray());

			EnableHighlightCurrentLine = options.EnableHighlightCurrentLine;
			HighlightCurrentValue = options.HighlightCurrentValue;
			HighlightStructureUnderMouseCursor = options.HighlightStructureUnderMouseCursor;
			HighlightActiveColumn = options.HighlightActiveColumn;
			ValuesLowerCaseHex = options.ValuesLowerCaseHex;
			OffsetLowerCaseHex = options.OffsetLowerCaseHex;
			EnableColorization = options.EnableColorization;
			GroupSizeInBytesVM.Value = options.GroupSizeInBytes;
			HexOffsetFormat = options.HexOffsetFormat;
			EncodingInfo = GetEncodingInfo(options.EncodingCodePage) ?? GetEncodingInfo(Encoding.UTF8.CodePage) ?? (EncodingInfo)EncodingInfoVM.Items.FirstOrDefault()?.Value;
		}

		EncodingInfo GetEncodingInfo(int codePage) {
			foreach (var vm in EncodingInfoVM.Items) {
				var info = (EncodingInfo)vm.Value;
				if (info.CodePage == codePage)
					return info;
			}
			return null;
		}

		public override void OnApply() {
			options.EnableHighlightCurrentLine = EnableHighlightCurrentLine;
			options.HighlightStructureUnderMouseCursor = HighlightStructureUnderMouseCursor;
			options.HighlightCurrentValue = HighlightCurrentValue;
			options.HighlightActiveColumn = HighlightActiveColumn;
			options.ValuesLowerCaseHex = ValuesLowerCaseHex;
			options.OffsetLowerCaseHex = OffsetLowerCaseHex;
			options.EnableColorization = EnableColorization;
			options.HexOffsetFormat = HexOffsetFormat;

			if (!GroupSizeInBytesVM.HasError)
				options.GroupSizeInBytes = GroupSizeInBytesVM.Value;

			var encodingInfo = EncodingInfo;
			if (encodingInfo != null)
				options.EncodingCodePage = encodingInfo.CodePage;
		}
	}
}
