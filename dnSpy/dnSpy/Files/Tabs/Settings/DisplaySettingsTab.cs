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
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Contracts.Text.Editor;
using dnSpy.Contracts.Text.Editor.OptionsExtensionMethods;
using dnSpy.Files.TreeView;
using dnSpy.Properties;
using dnSpy.Text.Editor;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;

namespace dnSpy.Files.Tabs.Settings {
	[Export(typeof(IAppSettingsTabProvider))]
	sealed class DisplayAppSettingsTabProvider : IAppSettingsTabProvider {
		readonly TextEditorSettingsImpl textEditorSettingsImpl;
		readonly IEditorOptions editorOptions;
		readonly FileTreeViewSettingsImpl fileTreeViewSettings;
		readonly FileTabManagerSettingsImpl fileTabManagerSettings;

		[ImportingConstructor]
		DisplayAppSettingsTabProvider(TextEditorSettingsImpl textEditorSettingsImpl, IEditorOptionsFactoryService editorOptionsFactoryService, FileTreeViewSettingsImpl fileTreeViewSettings, FileTabManagerSettingsImpl fileTabManagerSettings) {
			this.textEditorSettingsImpl = textEditorSettingsImpl;
			this.editorOptions = editorOptionsFactoryService.GlobalOptions;
			this.fileTreeViewSettings = fileTreeViewSettings;
			this.fileTabManagerSettings = fileTabManagerSettings;
		}

		public IEnumerable<IAppSettingsTab> Create() {
			yield return new DisplayAppSettingsTab(textEditorSettingsImpl, editorOptions, fileTreeViewSettings, fileTabManagerSettings);
		}
	}

	sealed class DisplayAppSettingsTab : IAppSettingsTab {
		public double Order => AppSettingsConstants.ORDER_SETTINGS_TAB_DISPLAY;
		public string Title => dnSpy_Resources.DisplayDlgTabTitle;
		public object UIObject => displayAppSettingsVM;

		readonly TextEditorSettingsImpl textEditorSettingsImpl;
		readonly IEditorOptions editorOptions;
		readonly FileTreeViewSettings fileTreeViewSettings;
		readonly FileTabManagerSettings fileTabManagerSettings;
		readonly DisplayAppSettingsVM displayAppSettingsVM;

		public DisplayAppSettingsTab(TextEditorSettingsImpl textEditorSettingsImpl, IEditorOptions editorOptions, FileTreeViewSettings fileTreeViewSettings, FileTabManagerSettings fileTabManagerSettings) {
			this.textEditorSettingsImpl = textEditorSettingsImpl;
			this.editorOptions = editorOptions;
			this.fileTreeViewSettings = fileTreeViewSettings;
			this.fileTabManagerSettings = fileTabManagerSettings;
			this.displayAppSettingsVM = new DisplayAppSettingsVM(new TextEditorSettingsVM(textEditorSettingsImpl, editorOptions), fileTreeViewSettings.Clone(), fileTabManagerSettings.Clone());
		}

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			if (!saveSettings)
				return;

			displayAppSettingsVM.OnBeforeSave(appRefreshSettings);
			displayAppSettingsVM.TextEditorSettings.CopyTo(textEditorSettingsImpl, editorOptions);
			displayAppSettingsVM.FileTreeViewSettings.CopyTo(fileTreeViewSettings);
			displayAppSettingsVM.FileTabManagerSettings.CopyTo(fileTabManagerSettings);
		}
	}

	sealed class TextEditorSettingsVM : ViewModelBase {
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

		public bool ShowLineNumbers {
			get { return showLineNumbers; }
			set {
				if (showLineNumbers != value) {
					showLineNumbers = value;
					OnPropertyChanged(nameof(ShowLineNumbers));
				}
			}
		}
		bool showLineNumbers = true;

		public bool HighlightReferences {
			get { return highlightReferences; }
			set {
				if (highlightReferences != value) {
					highlightReferences = value;
					OnPropertyChanged(nameof(HighlightReferences));
				}
			}
		}
		bool highlightReferences = true;

		public TextEditorSettingsVM(TextEditorSettingsImpl textEditorSettings, IEditorOptions editorOptions) {
			FontFamily = textEditorSettings.FontFamily;
			FontSize = textEditorSettings.FontSize;
			ShowLineNumbers = editorOptions.IsLineNumberMarginEnabled();
			HighlightReferences = editorOptions.IsReferenceHighlightingEnabled();
		}

		public void CopyTo(TextEditorSettingsImpl textEditorSettings, IEditorOptions editorOptions) {
			textEditorSettings.FontFamily = FontFamily;
			textEditorSettings.FontSize = FontSize;
			editorOptions.SetOptionValue(DefaultTextViewHostOptions.LineNumberMarginId, ShowLineNumbers);
			editorOptions.SetOptionValue(DefaultDnSpyTextViewOptions.ReferenceHighlightingId, HighlightReferences);
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

	sealed class DisplayAppSettingsVM : ViewModelBase {
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
					TextEditorSettings.FontFamily = fontFamilyVM.FontFamily;
					OnPropertyChanged(nameof(FontFamilyVM));
				}
			}
		}
		FontFamilyVM fontFamilyVM;

		public TextEditorSettingsVM TextEditorSettings { get; }
		public FileTreeViewSettings FileTreeViewSettings { get; }
		public FileTabManagerSettings FileTabManagerSettings { get; }

		public MemberKindVM[] MemberKindsArray => memberKindVMs2;
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
				throw new ArgumentNullException(nameof(newValue));
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

		public DisplayAppSettingsVM(TextEditorSettingsVM textEditorSettingsVM, FileTreeViewSettings fileTreeViewSettings, FileTabManagerSettings fileTabManagerSettings) {
			this.TextEditorSettings = textEditorSettingsVM;
			this.FileTreeViewSettings = fileTreeViewSettings;
			this.FileTabManagerSettings = fileTabManagerSettings;
			this.fontFamilies = null;
			this.fontFamilyVM = new FontFamilyVM(textEditorSettingsVM.FontFamily);
			Task.Factory.StartNew(() =>
				Fonts.SystemFontFamilies.Where(a => !FontUtilities.IsSymbol(a)).OrderBy(a => a.Source.ToUpperInvariant()).Select(a => new FontFamilyVM(a)).ToArray()
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
				FileTreeViewSettings.MemberKind0 != MemberKind0.Object ||
				FileTreeViewSettings.MemberKind1 != MemberKind1.Object ||
				FileTreeViewSettings.MemberKind2 != MemberKind2.Object ||
				FileTreeViewSettings.MemberKind3 != MemberKind3.Object ||
				FileTreeViewSettings.MemberKind4 != MemberKind4.Object;
			if (update)
				appRefreshSettings.Add(AppSettingsConstants.REFRESH_TREEVIEW_MEMBER_ORDER);

			FileTreeViewSettings.MemberKind0 = MemberKind0.Object;
			FileTreeViewSettings.MemberKind1 = MemberKind1.Object;
			FileTreeViewSettings.MemberKind2 = MemberKind2.Object;
			FileTreeViewSettings.MemberKind3 = MemberKind3.Object;
			FileTreeViewSettings.MemberKind4 = MemberKind4.Object;
		}
	}

	sealed class MemberKindVM : ViewModelBase {
		public MemberKind Object { get; }
		public string Text { get; }

		public MemberKindVM(MemberKind memberKind, string text) {
			this.Object = memberKind;
			this.Text = text;
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
