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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Files.Tabs.TextEditor;
using dnSpy.Files.TreeView;
using dnSpy.Shared.UI.Controls;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Files.Tabs.Settings {
	[Export(typeof(IAppSettingsTabCreator))]
	sealed class DisplayAppSettingsTabCreator : IAppSettingsTabCreator {
		readonly TextEditorSettingsImpl textEditorSettings;
		readonly FileTreeViewSettingsImpl fileTreeViewSettings;
		readonly FileTabManagerSettingsImpl fileTabManagerSettings;

		[ImportingConstructor]
		DisplayAppSettingsTabCreator(TextEditorSettingsImpl textEditorSettings, FileTreeViewSettingsImpl fileTreeViewSettings, FileTabManagerSettingsImpl fileTabManagerSettings) {
			this.textEditorSettings = textEditorSettings;
			this.fileTreeViewSettings = fileTreeViewSettings;
			this.fileTabManagerSettings = fileTabManagerSettings;
		}

		public IEnumerable<IAppSettingsTab> Create() {
			yield return new DisplayAppSettingsTab(textEditorSettings, fileTreeViewSettings, fileTabManagerSettings);
		}
	}

	sealed class DisplayAppSettingsTab : IAppSettingsTab {
		public double Order {
			get { return AppSettingsConstants.ORDER_SETTINGS_TAB_DISPLAY; }
		}

		public string Title {
			get { return "Display"; }
		}

		public object UIObject {
			get { return displayAppSettingsVM; }
		}

		readonly TextEditorSettings textEditorSettings;
		readonly FileTreeViewSettings fileTreeViewSettings;
		readonly FileTabManagerSettings fileTabManagerSettings;
		readonly DisplayAppSettingsVM displayAppSettingsVM;

		public DisplayAppSettingsTab(TextEditorSettings textEditorSettings, FileTreeViewSettings fileTreeViewSettings, FileTabManagerSettings fileTabManagerSettings) {
			this.textEditorSettings = textEditorSettings;
			this.fileTreeViewSettings = fileTreeViewSettings;
			this.fileTabManagerSettings = fileTabManagerSettings;
			this.displayAppSettingsVM = new DisplayAppSettingsVM(textEditorSettings.Clone(), fileTreeViewSettings.Clone(), fileTabManagerSettings.Clone());
		}

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			if (!saveSettings)
				return;

			displayAppSettingsVM.TextEditorSettings.CopyTo(textEditorSettings);
			displayAppSettingsVM.FileTreeViewSettings.CopyTo(fileTreeViewSettings);
			displayAppSettingsVM.FileTabManagerSettings.CopyTo(fileTabManagerSettings);
		}
	}

	sealed class FontFamilyVM : ViewModelBase {
		public FontFamily FontFamily {
			get { return ff; }
		}
		readonly FontFamily ff;

		public bool IsMonospaced {
			get { return isMonospaced; }
		}
		readonly bool isMonospaced;

		public FontFamilyVM(FontFamily ff) {
			this.ff = ff;
			this.isMonospaced = FontUtils.IsMonospacedFont(ff);
		}

		public override bool Equals(object obj) {
			var other = obj as FontFamilyVM;
			return other != null &&
				FontFamily.Equals(other.FontFamily);
		}

		public override int GetHashCode() {
			return FontFamily.GetHashCode();
		}
	}

	sealed class DisplayAppSettingsVM : ViewModelBase {
		public FontFamilyVM[] FontFamilies {
			get { return fontFamilies; }
			set {
				if (fontFamilies != value) {
					fontFamilies = value;
					OnPropertyChanged("FontFamilies");
				}
			}
		}
		FontFamilyVM[] fontFamilies;

		public FontFamilyVM FontFamilyVM {
			get { return fontFamilyVM; }
			set {
				if (fontFamilyVM != value) {
					fontFamilyVM = value;
					textEditorSettings.FontFamily = fontFamilyVM.FontFamily;
					OnPropertyChanged("FontFamilyVM");
				}
			}
		}
		FontFamilyVM fontFamilyVM;

		public TextEditorSettings TextEditorSettings {
			get { return textEditorSettings; }
		}
		readonly TextEditorSettings textEditorSettings;

		public FileTreeViewSettings FileTreeViewSettings {
			get { return fileTreeViewSettings; }
		}
		readonly FileTreeViewSettings fileTreeViewSettings;

		public FileTabManagerSettings FileTabManagerSettings {
			get { return fileTabManagerSettings; }
		}
		readonly FileTabManagerSettings fileTabManagerSettings;

		public DisplayAppSettingsVM(TextEditorSettings textEditorSettings, FileTreeViewSettings fileTreeViewSettings, FileTabManagerSettings fileTabManagerSettings) {
			this.textEditorSettings = textEditorSettings;
			this.fileTreeViewSettings = fileTreeViewSettings;
			this.fileTabManagerSettings = fileTabManagerSettings;
			this.fontFamilies = null;
			this.fontFamilyVM = new FontFamilyVM(textEditorSettings.FontFamily);
			Task.Factory.StartNew(() => {
				return Fonts.SystemFontFamilies.Where(a => !FontUtils.IsSymbol(a)).OrderBy(a => a.Source.ToUpperInvariant()).Select(a => new FontFamilyVM(a)).ToArray();
			})
			.ContinueWith(t => {
				var ex = t.Exception;
				FontFamilies = t.Result;
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}
	}

	sealed class FontSizeConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			return Math.Round((double)value * 3 / 4);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			var s = (string)value;
			double v;
			if (double.TryParse(s, out v))
				return v * 4 / 3;
			return FontUtils.DEFAULT_FONT_SIZE;
		}
	}

	sealed class FontFamilyVMConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var vm = (FontFamilyVM)value;
			if (!vm.IsMonospaced)
				return new TextBlock { Text = vm.FontFamily.Source };
			var tb = new TextBlock();
			tb.Inlines.Add(new Bold(new Run(vm.FontFamily.Source)));
			return tb;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}
