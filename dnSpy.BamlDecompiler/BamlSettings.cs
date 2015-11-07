/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Threading;
using System.Windows;
using System.Xml.Linq;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.Options;

namespace dnSpy.BamlDecompiler {
	[ExportOptionPage(Title = "BAML", Order = 3)]
	internal sealed class BamlSettingsCreator : IOptionPageCreator {
		bool loaded = false;

		public OptionPage Create() {
			if (!loaded) {
				var asmName = GetType().Assembly.FullName;
				var src = new Uri("pack://application:,,,/" + asmName + ";component/BamlSettings.xaml", UriKind.RelativeOrAbsolute);
				Application.Current.Resources.MergedDictionaries.Add(new ResourceDictionary {
					Source = src
				});
				loaded = true;
			}
			return new BamlSettings();
		}
	}

	public sealed class BamlSettings : OptionPage {
		static BamlSettings settings;

		public static BamlSettings Instance {
			get {
				if (settings != null)
					return settings;
				var s = new BamlSettings();
				s.Load(DNSpySettings.Load());
				Interlocked.CompareExchange(ref settings, s, null);
				return settings;
			}
		}

		bool disassembleBaml;

		public bool DisassembleBaml {
			get { return disassembleBaml; }
			set {
				if (disassembleBaml != value) {
					disassembleBaml = value;
					OnPropertyChanged("DisassembleBaml");
				}
			}
		}

		const string SETTINGS_SECTION_NAME = "BamlSettings";

		public override void Load(DNSpySettings settings) {
			var xelem = settings[SETTINGS_SECTION_NAME];
			DisassembleBaml = (bool?)xelem.Attribute("DisassembleBaml") ?? false;
		}

		public override RefreshFlags Save(XElement root) {
			var xelem = new XElement(SETTINGS_SECTION_NAME);
			xelem.SetAttributeValue("DisassembleBaml", DisassembleBaml);

			var currElem = root.Element(SETTINGS_SECTION_NAME);
			if (currElem != null)
				currElem.ReplaceWith(xelem);
			else
				root.Add(xelem);

			WriteTo(Instance);

			return RefreshFlags.None;
		}

		void WriteTo(BamlSettings other) {
			other.DisassembleBaml = DisassembleBaml;
		}
	}
}