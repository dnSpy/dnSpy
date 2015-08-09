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
using System.Xml.Linq;
using dnSpy.Options;

namespace ICSharpCode.ILSpy.Options {
	[ExportOptionPage(Title = "Display", Order = 1)]
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

		public override void Load(ILSpySettings settings) {
			this.settings = LoadDisplaySettings(settings);
		}

		static DisplaySettings currentDisplaySettings;

		public static DisplaySettings CurrentDisplaySettings {
			get {
				if (currentDisplaySettings != null)
					return currentDisplaySettings;
				Interlocked.CompareExchange(ref currentDisplaySettings, LoadDisplaySettings(ILSpySettings.Load()), null);
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

		public static DisplaySettings LoadDisplaySettings(ILSpySettings settings) {
			XElement e = settings["DisplaySettings"];
			DisplaySettings s = new DisplaySettings();
			s.SelectedFont = new FontFamily(SessionSettings.Unescape((string)e.Attribute("Font")) ?? FontUtils.GetDefaultFont());
			s.SelectedFontSize = (double?)e.Attribute("FontSize") ?? FontUtils.DEFAULT_FONT_SIZE;
			s.ShowLineNumbers = (bool?)e.Attribute("ShowLineNumbers") ?? true;
			s.ShowMetadataTokens = (bool?)e.Attribute("ShowMetadataTokens") ?? true;
			s.ShowAssemblyVersion = (bool?)e.Attribute("ShowAssemblyVersion") ?? true;
			s.ShowAssemblyPublicKeyToken = (bool?)e.Attribute("ShowAssemblyPublicKeyToken") ?? false;
			s.DecompileFullType = (bool?)e.Attribute("DecompileFullType") ?? true;
			s.NewEmptyTabs = (bool?)e.Attribute("NewEmptyTabs") ?? false;
			s.RestoreTabsAtStartup = (bool?)e.Attribute("RestoreTabsAtStartup") ?? true;
			s.AutoHighlightRefs = (bool?)e.Attribute("AutoHighlightRefs") ?? true;
			s.SyntaxHighlightTreeViewUI = (bool?)e.Attribute("SyntaxHighlightTreeViewUI") ?? true;
			s.SyntaxHighlightAnalyzerTreeViewUI = (bool?)e.Attribute("SyntaxHighlightAnalyzerTreeViewUI") ?? true;
			s.SyntaxHighlightSearchListUI = (bool?)e.Attribute("SyntaxHighlightSearchListUI") ?? true;
			s.SingleClickExpandsChildren = (bool?)e.Attribute("SingleClickExpandsChildren") ?? true;

			return s;
		}

		public override RefreshFlags Save(XElement root) {
			DisplaySettings s = this.settings;

			var flags = RefreshFlags.None;
			if (currentDisplaySettings.ShowMetadataTokens != s.ShowMetadataTokens ||
				currentDisplaySettings.ShowAssemblyVersion != s.ShowAssemblyVersion ||
				currentDisplaySettings.ShowAssemblyPublicKeyToken != s.ShowAssemblyPublicKeyToken) {
				flags |= RefreshFlags.TreeViewNodeNames;
			}

			currentDisplaySettings.CopyValues(s);

			XElement section = new XElement("DisplaySettings");
			section.SetAttributeValue("Font", SessionSettings.Escape(s.SelectedFont.Source));
			section.SetAttributeValue("FontSize", s.SelectedFontSize);
			section.SetAttributeValue("ShowLineNumbers", s.ShowLineNumbers);
			section.SetAttributeValue("ShowMetadataTokens", s.ShowMetadataTokens);
			section.SetAttributeValue("ShowAssemblyVersion", s.ShowAssemblyVersion);
			section.SetAttributeValue("ShowAssemblyPublicKeyToken", s.ShowAssemblyPublicKeyToken);
			section.SetAttributeValue("DecompileFullType", s.DecompileFullType);
			section.SetAttributeValue("NewEmptyTabs", s.NewEmptyTabs);
			section.SetAttributeValue("RestoreTabsAtStartup", s.RestoreTabsAtStartup);
			section.SetAttributeValue("AutoHighlightRefs", s.AutoHighlightRefs);
			section.SetAttributeValue("SyntaxHighlightTreeViewUI", s.SyntaxHighlightTreeViewUI);
			section.SetAttributeValue("SyntaxHighlightAnalyzerTreeViewUI", s.SyntaxHighlightAnalyzerTreeViewUI);
			section.SetAttributeValue("SyntaxHighlightSearchListUI", s.SyntaxHighlightSearchListUI);
			section.SetAttributeValue("SingleClickExpandsChildren", s.SingleClickExpandsChildren);

			XElement existingElement = root.Element("DisplaySettings");
			if (existingElement != null)
				existingElement.ReplaceWith(section);
			else
				root.Add(section);

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