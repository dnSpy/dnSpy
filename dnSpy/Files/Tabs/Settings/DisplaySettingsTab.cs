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
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Files.TreeView;
using dnSpy.Properties;
using dnSpy.Shared.Controls;
using dnSpy.Shared.MVVM;
using dnSpy.TextEditor;

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
			get { return dnSpy_Resources.DisplayDlgTabTitle; }
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

			displayAppSettingsVM.OnBeforeSave(appRefreshSettings);
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

		public MemberKindVM[] MemberKindsArray {
			get { return memberKindVMs2; }
		}
		readonly MemberKindVM[] memberKindVMs;
		readonly MemberKindVM[] memberKindVMs2;

		public MemberKindVM MemberKind0 {
			get { return memberKindVMs[0]; }
			set { SetMemberKind(0, value); }
		}

		public MemberKindVM MemberKind1 {
			get { return memberKindVMs[1]; }
			set { SetMemberKind(1, value); }
		}

		public MemberKindVM MemberKind2 {
			get { return memberKindVMs[2]; }
			set { SetMemberKind(2, value); }
		}

		public MemberKindVM MemberKind3 {
			get { return memberKindVMs[3]; }
			set { SetMemberKind(3, value); }
		}

		public MemberKindVM MemberKind4 {
			get { return memberKindVMs[4]; }
			set { SetMemberKind(4, value); }
		}

		void SetMemberKind(int index, MemberKindVM newValue) {
			Debug.Assert(newValue != null);
			if (newValue == null)
				throw new ArgumentNullException();
			if (memberKindVMs[index] == newValue)
				return;

			int otherIndex = Array.IndexOf(memberKindVMs, newValue);
			Debug.Assert(otherIndex >= 0);
			if (otherIndex >= 0) {
				memberKindVMs[otherIndex] = memberKindVMs[index];
				memberKindVMs[index] = newValue;

				OnPropertyChanged(string.Format("MemberKind{0}", otherIndex));
			}
			OnPropertyChanged(string.Format("MemberKind{0}", index));
		}

		public DisplayAppSettingsVM(TextEditorSettings textEditorSettings, FileTreeViewSettings fileTreeViewSettings, FileTabManagerSettings fileTabManagerSettings) {
			this.textEditorSettings = textEditorSettings;
			this.fileTreeViewSettings = fileTreeViewSettings;
			this.fileTabManagerSettings = fileTabManagerSettings;
			this.fontFamilies = null;
			this.fontFamilyVM = new FontFamilyVM(textEditorSettings.FontFamily);
			Task.Factory.StartNew(() =>
				Fonts.SystemFontFamilies.Where(a => !FontUtils.IsSymbol(a)).OrderBy(a => a.Source.ToUpperInvariant()).Select(a => new FontFamilyVM(a)).ToArray()
			)
			.ContinueWith(t => {
				var ex = t.Exception;
				if (!t.IsCanceled && !t.IsFaulted)
					FontFamilies = t.Result;
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

			var defObjs = typeof(MemberKind).GetEnumValues().Cast<MemberKind>().ToArray();
			this.memberKindVMs = new MemberKindVM[defObjs.Length];
			for (int i = 0; i < defObjs.Length; i++)
				this.memberKindVMs[i] = new MemberKindVM(defObjs[i], ToString(defObjs[i]));
			this.memberKindVMs2 = this.memberKindVMs.ToArray();

			this.MemberKind0 = this.memberKindVMs.First(a => a.Object == fileTreeViewSettings.MemberKind0);
			this.MemberKind1 = this.memberKindVMs.First(a => a.Object == fileTreeViewSettings.MemberKind1);
			this.MemberKind2 = this.memberKindVMs.First(a => a.Object == fileTreeViewSettings.MemberKind2);
			this.MemberKind3 = this.memberKindVMs.First(a => a.Object == fileTreeViewSettings.MemberKind3);
			this.MemberKind4 = this.memberKindVMs.First(a => a.Object == fileTreeViewSettings.MemberKind4);
		}

		static string ToString(MemberKind o) {
			switch (o) {
			case MemberKind.NestedTypes:	return dnSpy_Resources.MemberKind_NestedTypes;
			case MemberKind.Fields:			return dnSpy_Resources.MemberKind_Fields;
			case MemberKind.Events:			return dnSpy_Resources.MemberKind_Events;
			case MemberKind.Properties:		return dnSpy_Resources.MemberKind_Properties;
			case MemberKind.Methods:		return dnSpy_Resources.MemberKind_Methods;
			default:
				Debug.Fail("Shouldn't be here");
				return "???";
			}
		}

		public void OnBeforeSave(IAppRefreshSettings appRefreshSettings) {
			bool update =
				fileTreeViewSettings.MemberKind0 != MemberKind0.Object ||
				fileTreeViewSettings.MemberKind1 != MemberKind1.Object ||
				fileTreeViewSettings.MemberKind2 != MemberKind2.Object ||
				fileTreeViewSettings.MemberKind3 != MemberKind3.Object ||
				fileTreeViewSettings.MemberKind4 != MemberKind4.Object;
			if (update)
				appRefreshSettings.Add(AppSettingsConstants.REFRESH_TREEVIEW_MEMBER_ORDER);

			fileTreeViewSettings.MemberKind0 = MemberKind0.Object;
			fileTreeViewSettings.MemberKind1 = MemberKind1.Object;
			fileTreeViewSettings.MemberKind2 = MemberKind2.Object;
			fileTreeViewSettings.MemberKind3 = MemberKind3.Object;
			fileTreeViewSettings.MemberKind4 = MemberKind4.Object;
		}
	}

	sealed class MemberKindVM : ViewModelBase {
		public MemberKind Object {
			get { return memberKind; }
		}
		readonly MemberKind memberKind;

		public string Text {
			get { return text; }
		}
		readonly string text;

		public MemberKindVM(MemberKind memberKind, string text) {
			this.memberKind = memberKind;
			this.text = text;
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
