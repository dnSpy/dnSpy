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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.HexEditor;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Properties;
using dnSpy.Contracts.Settings.Dialog;

namespace dnSpy.Contracts.Hex {
	[Export(typeof(IAppSettingsTabProvider))]
	sealed class HexEditorAppSettingsTabProvider : IAppSettingsTabProvider {
		readonly HexEditorSettingsImpl hexEditorSettingsImpl;

		[ImportingConstructor]
		HexEditorAppSettingsTabProvider(HexEditorSettingsImpl hexEditorSettingsImpl) {
			this.hexEditorSettingsImpl = hexEditorSettingsImpl;
		}

		public IEnumerable<IAppSettingsTab> Create() {
			yield return new HexEditorAppSettingsTab(hexEditorSettingsImpl);
		}
	}

	sealed class HexEditorAppSettingsTab : IAppSettingsTab {
		public Guid ParentGuid => Guid.Empty;
		public Guid Guid => new Guid("4BEAD407-839F-489B-A874-2B3325776366");
		public double Order => AppSettingsConstants.ORDER_HEXEDITOR;
		public string Title => dnSpy_Contracts_DnSpy_Resources.HexEditorAppDlgTitle;
		public ImageReference Icon => ImageReference.None;
		public object UIObject => displayAppSettingsVM;

		readonly HexEditorSettingsImpl hexEditorSettingsImpl;
		readonly HexEditorAppSettingsVM displayAppSettingsVM;

		public HexEditorAppSettingsTab(HexEditorSettingsImpl hexEditorSettingsImpl) {
			this.hexEditorSettingsImpl = hexEditorSettingsImpl;
			this.displayAppSettingsVM = new HexEditorAppSettingsVM(hexEditorSettingsImpl.Clone());
		}

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			if (!saveSettings)
				return;

			displayAppSettingsVM.Settings.CopyTo(hexEditorSettingsImpl);
		}
	}

	sealed class HexEditorAppSettingsVM : ViewModelBase {
		public HexEditorSettings Settings { get; }
		public Int32VM BytesGroupCountVM { get; }
		public Int32VM BytesPerLineVM { get; }

		public FontFamily[] FontFamilies {
			get { return fontFamilies; }
			set {
				if (fontFamilies != value) {
					fontFamilies = value;
					OnPropertyChanged(nameof(FontFamilies));
				}
			}
		}
		FontFamily[] fontFamilies;

		public FontFamily FontFamily {
			get { return fontFamily; }
			set {
				if (fontFamily != value) {
					fontFamily = value;
					Settings.FontFamily = fontFamily;
					OnPropertyChanged(nameof(FontFamily));
				}
			}
		}
		FontFamily fontFamily;

		public EnumListVM AsciiEncodingVM { get; }
		readonly EnumVM[] asciiEncodingList = new EnumVM[] {
			new EnumVM(AsciiEncoding.ASCII, dnSpy_Contracts_DnSpy_Resources.HexEditor_CharacterEncoding_ASCII_2),
			new EnumVM(AsciiEncoding.ANSI, dnSpy_Contracts_DnSpy_Resources.HexEditor_CharacterEncoding_ANSI_2),
			new EnumVM(AsciiEncoding.UTF7, dnSpy_Contracts_DnSpy_Resources.HexEditor_CharacterEncoding_UTF7_2),
			new EnumVM(AsciiEncoding.UTF8, dnSpy_Contracts_DnSpy_Resources.HexEditor_CharacterEncoding_UTF8_2),
			new EnumVM(AsciiEncoding.UTF32, dnSpy_Contracts_DnSpy_Resources.HexEditor_CharacterEncoding_UTF32_2),
			new EnumVM(AsciiEncoding.Unicode, dnSpy_Contracts_DnSpy_Resources.HexEditor_CharacterEncoding_UNICODE_2),
			new EnumVM(AsciiEncoding.BigEndianUnicode, dnSpy_Contracts_DnSpy_Resources.HexEditor_CharacterEncoding_BIG_ENDIAN_UNICODE_2),
		};

		public HexEditorAppSettingsVM(HexEditorSettings hexEditorSettings) {
			this.Settings = hexEditorSettings;
			this.AsciiEncodingVM = new EnumListVM(asciiEncodingList, (a, b) => hexEditorSettings.AsciiEncoding = (AsciiEncoding)AsciiEncodingVM.SelectedItem);
			this.BytesGroupCountVM = new Int32VM(a => { HasErrorUpdated(); hexEditorSettings.BytesGroupCount = BytesGroupCountVM.Value; });
			this.BytesPerLineVM = new Int32VM(a => { HasErrorUpdated(); hexEditorSettings.BytesPerLine = BytesPerLineVM.Value; }) {
				Min = 0,
				Max = HexEditorSettings.MAX_BYTES_PER_LINE,
			};
			AsciiEncodingVM.SelectedItem = hexEditorSettings.AsciiEncoding;
			BytesGroupCountVM.Value = hexEditorSettings.BytesGroupCount;
			BytesPerLineVM.Value = hexEditorSettings.BytesPerLine;
			FontFamily = hexEditorSettings.FontFamily;
			Task.Factory.StartNew(() => FontUtilities.GetMonospacedFonts())
			.ContinueWith(t => {
				var ex = t.Exception;
				if (!t.IsCanceled && !t.IsFaulted)
					FontFamilies = t.Result;
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}
	}
}
