// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml.Linq;
using ICSharpCode.Decompiler;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// Interaction logic for DisplaySettingsPanel.xaml
	/// </summary>
	[ExportOptionPage("Display")]
	public partial class DisplaySettingsPanel : UserControl, IOptionPage
	{
		public DisplaySettingsPanel()
		{
			InitializeComponent();
			
			Task<FontFamily[]> task = new Task<FontFamily[]>(FontLoader);
			task.Start();
			task.ContinueWith(
				delegate(Task continuation) {
					App.Current.Dispatcher.Invoke(
						DispatcherPriority.Normal,
						(Action)(
							() => {
								fontSelector.ItemsSource = task.Result;
								if (continuation.Exception != null) {
									foreach (var ex in continuation.Exception.InnerExceptions) {
										MessageBox.Show(ex.ToString());
									}
								}
							})
					);
				}
			);
		}
		
		public void Load(ILSpySettings settings)
		{
			this.DataContext = LoadDisplaySettings(settings);
		}
		
		static DisplaySettings currentDisplaySettings;
		
		public static DisplaySettings CurrentDisplaySettings {
			get {
				return currentDisplaySettings ?? (currentDisplaySettings = LoadDisplaySettings(ILSpySettings.Load()));
			}
		}
		
		static bool IsSymbolFont(FontFamily fontFamily)
		{
			foreach (var tf in fontFamily.GetTypefaces()) {
				GlyphTypeface glyph;
				try {
					if (tf.TryGetGlyphTypeface(out glyph))
						return glyph.Symbol;
				} catch (Exception) {
					return true;
				}
			}
			return false;
		}
		
		static FontFamily[] FontLoader()
		{
			return Fonts.SystemFontFamilies
				.Where(ff => !IsSymbolFont(ff))
				.OrderBy(ff => ff.Source)
				.ToArray();
		}
		
		public static DisplaySettings LoadDisplaySettings(ILSpySettings settings)
		{
			XElement e = settings["DisplaySettings"];
			DisplaySettings s = new DisplaySettings();
			s.SelectedFont = new FontFamily((string)e.Attribute("Font") ?? "Consolas");
			s.SelectedFontSize = (double?)e.Attribute("FontSize") ?? 10.0 * 4 / 3;
			
			return s;
		}
		
		public void Save(XElement root)
		{
			DisplaySettings s = (DisplaySettings)this.DataContext;
			
			currentDisplaySettings.CopyValues(s);
			
			XElement section = new XElement("DisplaySettings");
			section.SetAttributeValue("Font", s.SelectedFont.Source);
			section.SetAttributeValue("FontSize", s.SelectedFontSize);
			
			XElement existingElement = root.Element("DisplaySettings");
			if (existingElement != null)
				existingElement.ReplaceWith(section);
			else
				root.Add(section);
		}
	}
	
	public class FontSizeConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is double) {
				return Math.Round((double)value / 4 * 3);
			}
			
			throw new NotImplementedException();
		}
		
		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
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