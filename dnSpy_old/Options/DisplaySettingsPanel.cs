// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using dnSpy.Contracts;
using dnSpy.Options;

namespace ICSharpCode.ILSpy.Options {
	[ExportOptionPage(Title = "Display", Order = 2)]
	sealed class DisplaySettingsPanelCreator : IOptionPageCreator {
		public OptionPage Create() {
			return new DisplaySettingsPanel();
		}
	}

	public class DisplaySettingsPanel : OptionPage {
		public DisplaySettings Settings {
			get { return settings; }
		}
		DisplaySettings settings;

		public FontFamily[] Fonts {
			get { return fonts; }
			set {
				if (fonts != value) {
					fonts = value;
					OnPropertyChanged("Fonts");
				}
			}
		}
		FontFamily[] fonts;

		public DisplaySettingsPanel() {
			Task<FontFamily[]> task = new Task<FontFamily[]>(FontLoader);
			task.Start();
			task.ContinueWith(
				delegate (Task continuation) {
					App.Current.Dispatcher.Invoke(
						DispatcherPriority.Normal,
						(Action)(
							() => {
								this.Fonts = task.Result;
								if (continuation.Exception != null) {
									foreach (var ex in continuation.Exception.InnerExceptions) {
										MainWindow.Instance.ShowMessageBox(ex.ToString());
									}
								}
							})
					);
				}
			);
		}

		public override void Load() {
			this.settings = LoadDisplaySettings();
		}

		static DisplaySettings currentDisplaySettings;

		public static DisplaySettings CurrentDisplaySettings {
			get {
				if (currentDisplaySettings != null)
					return currentDisplaySettings;
				Interlocked.CompareExchange(ref currentDisplaySettings, LoadDisplaySettings(), null);
				return currentDisplaySettings;
			}
		}

		static bool IsSymbolFont(FontFamily fontFamily) {
			foreach (var tf in fontFamily.GetTypefaces()) {
				GlyphTypeface glyph;
				try {
					if (tf.TryGetGlyphTypeface(out glyph))
						return glyph.Symbol;
				}
				catch (Exception) {
					return true;
				}
			}
			return false;
		}

		static FontFamily[] FontLoader() {
			return System.Windows.Media.Fonts.SystemFontFamilies
				.Where(ff => !IsSymbolFont(ff))
				.OrderBy(ff => ff.Source)
				.ToArray();
		}

		const string SETTINGS_NAME = "DD4AE909-A608-4DF8-816F-6967E8B441D5";

		public static DisplaySettings LoadDisplaySettings() {
			var section = DnSpy.App.SettingsManager.GetOrCreateSection(SETTINGS_NAME);
			DisplaySettings s = new DisplaySettings();
			s.SelectedFont = new FontFamily(section.Attribute<string>("Font") ?? FontUtils.GetDefaultFont());
			s.SelectedFontSize = section.Attribute<double?>("FontSize") ?? FontUtils.DEFAULT_FONT_SIZE;
			s.ShowLineNumbers = section.Attribute<bool?>("ShowLineNumbers") ?? true;
			s.ShowMetadataTokens = section.Attribute<bool?>("ShowMetadataTokens") ?? true;
			s.ShowAssemblyVersion = section.Attribute<bool?>("ShowAssemblyVersion") ?? true;
			s.ShowAssemblyPublicKeyToken = section.Attribute<bool?>("ShowAssemblyPublicKeyToken") ?? false;
			s.DecompileFullType = section.Attribute<bool?>("DecompileFullType") ?? true;
			s.NewEmptyTabs = section.Attribute<bool?>("NewEmptyTabs") ?? false;
			s.RestoreTabsAtStartup = section.Attribute<bool?>("RestoreTabsAtStartup") ?? true;
			s.AutoHighlightRefs = section.Attribute<bool?>("AutoHighlightRefs") ?? true;
			s.SyntaxHighlightTreeViewUI = section.Attribute<bool?>("SyntaxHighlightTreeViewUI") ?? true;
			s.SyntaxHighlightAnalyzerTreeViewUI = section.Attribute<bool?>("SyntaxHighlightAnalyzerTreeViewUI") ?? true;
			s.SyntaxHighlightSearchListUI = section.Attribute<bool?>("SyntaxHighlightSearchListUI") ?? true;
			s.SingleClickExpandsChildren = section.Attribute<bool?>("SingleClickExpandsChildren") ?? true;

			return s;
		}

		public override RefreshFlags Save() {
			DisplaySettings s = this.settings;

			var flags = RefreshFlags.None;
			if (currentDisplaySettings.ShowMetadataTokens != s.ShowMetadataTokens ||
				currentDisplaySettings.ShowAssemblyVersion != s.ShowAssemblyVersion ||
				currentDisplaySettings.ShowAssemblyPublicKeyToken != s.ShowAssemblyPublicKeyToken) {
				flags |= RefreshFlags.TreeViewNodes;
			}

			currentDisplaySettings.CopyValues(s);

			var section = DnSpy.App.SettingsManager.CreateSection(SETTINGS_NAME);
			section.Attribute("Font", s.SelectedFont.Source);
			section.Attribute("FontSize", s.SelectedFontSize);
			section.Attribute("ShowLineNumbers", s.ShowLineNumbers);
			section.Attribute("ShowMetadataTokens", s.ShowMetadataTokens);
			section.Attribute("ShowAssemblyVersion", s.ShowAssemblyVersion);
			section.Attribute("ShowAssemblyPublicKeyToken", s.ShowAssemblyPublicKeyToken);
			section.Attribute("DecompileFullType", s.DecompileFullType);
			section.Attribute("NewEmptyTabs", s.NewEmptyTabs);
			section.Attribute("RestoreTabsAtStartup", s.RestoreTabsAtStartup);
			section.Attribute("AutoHighlightRefs", s.AutoHighlightRefs);
			section.Attribute("SyntaxHighlightTreeViewUI", s.SyntaxHighlightTreeViewUI);
			section.Attribute("SyntaxHighlightAnalyzerTreeViewUI", s.SyntaxHighlightAnalyzerTreeViewUI);
			section.Attribute("SyntaxHighlightSearchListUI", s.SyntaxHighlightSearchListUI);
			section.Attribute("SingleClickExpandsChildren", s.SingleClickExpandsChildren);

			return flags;
		}
	}

	public class FontSizeConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			if (value is double) {
				return Math.Round((double)value / 4 * 3);
			}

			throw new NotImplementedException();
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			if (value is string) {
				double d;
				if (double.TryParse((string)value, out d))
					return d * 4 / 3;
				return 11 * 4 / 3;
			}

			throw new NotImplementedException();
		}
	}
}