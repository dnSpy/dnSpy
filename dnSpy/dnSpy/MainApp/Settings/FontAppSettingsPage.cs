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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Properties;
using dnSpy.Text.Editor;

namespace dnSpy.MainApp.Settings {
	[Export(typeof(IAppSettingsPageProvider))]
	sealed class FontAppSettingsPageProvider : IAppSettingsPageProvider {
		readonly TextEditorSettingsImpl textEditorSettings;

		[ImportingConstructor]
		FontAppSettingsPageProvider(TextEditorSettingsImpl textEditorSettings) {
			this.textEditorSettings = textEditorSettings;
		}

		public IEnumerable<IAppSettingsPage> Create() {
			yield return new FontAppSettingsPage(textEditorSettings);
		}
	}

	sealed class FontAppSettingsPage : ViewModelBase, IAppSettingsPage {
		public Guid ParentGuid => new Guid(AppSettingsConstants.GUID_ENVIRONMENT);
		public Guid Guid => new Guid("915F7258-1441-4F80-9DB2-6DF6948C2E09");
		public double Order => AppSettingsConstants.ORDER_ENVIRONMENT_FONT;
		public string Title => dnSpy_Resources.FontSettings;
		public ImageReference Icon => ImageReference.None;
		public object UIObject => this;

		public FontFamilyVM[] FontFamilies {
			get { return fontFamilies; }
			set {
				if (fontFamilies != value) {
					fontFamilies = value;
					OnPropertyChanged(nameof(FontFamilies));
				}
			}
		}
		FontFamilyVM[] fontFamilies;

		public FontFamilyVM FontFamilyVM {
			get { return fontFamilyVM; }
			set {
				if (fontFamilyVM != value) {
					fontFamilyVM = value;
					FontFamily = fontFamilyVM.FontFamily;
					OnPropertyChanged(nameof(FontFamilyVM));
				}
			}
		}
		FontFamilyVM fontFamilyVM;

		public FontFamily FontFamily {
			get { return fontFamily; }
			set {
				if (fontFamily == null || fontFamily.Source != value.Source) {
					fontFamily = value;
					OnPropertyChanged(nameof(FontFamily));
				}
			}
		}
		FontFamily fontFamily;

		public double FontSize {
			get { return fontSize; }
			set {
				if (fontSize != value) {
					fontSize = FontUtilities.FilterFontSize(value);
					OnPropertyChanged(nameof(FontSize));
				}
			}
		}
		double fontSize = FontUtilities.DEFAULT_FONT_SIZE;

		readonly TextEditorSettingsImpl textEditorSettings;

		public FontAppSettingsPage(TextEditorSettingsImpl textEditorSettings) {
			if (textEditorSettings == null)
				throw new ArgumentNullException(nameof(textEditorSettings));
			this.textEditorSettings = textEditorSettings;

			FontFamily = textEditorSettings.FontFamily;
			FontSize = textEditorSettings.FontSize;

			this.fontFamilies = null;
			this.fontFamilyVM = new FontFamilyVM(FontFamily);
			Task.Factory.StartNew(() =>
				Fonts.SystemFontFamilies.Where(a => !FontUtilities.IsSymbol(a)).OrderBy(a => a.Source.ToUpperInvariant()).Select(a => new FontFamilyVM(a)).ToArray()
			)
			.ContinueWith(t => {
				var ex = t.Exception;
				if (!t.IsCanceled && !t.IsFaulted)
					FontFamilies = t.Result;
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			if (!saveSettings)
				return;

			textEditorSettings.FontFamily = FontFamily;
			textEditorSettings.FontSize = FontSize;
		}
	}

	sealed class FontFamilyVM : ViewModelBase {
		public FontFamily FontFamily { get; }
		public bool IsMonospaced { get; }

		public FontFamilyVM(FontFamily ff) {
			this.FontFamily = ff;
			this.IsMonospaced = FontUtilities.IsMonospacedFont(ff);
		}

		public override bool Equals(object obj) {
			var other = obj as FontFamilyVM;
			return other != null &&
				FontFamily.Equals(other.FontFamily);
		}

		public override int GetHashCode() => FontFamily.GetHashCode();
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
