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
using System.IO;
using System.Text.RegularExpressions;
using dnSpy.Contracts;
using dnSpy.Contracts.Menus;
using dnSpy.NRefactory;
using dnSpy.Shared.UI.Menus;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TextView;

namespace ICSharpCode.ILSpy {
	[ExportMenuItem(OwnerGuid = MenuConstants.APP_MENU_HELP_GUID, Header = "_About", Group = MenuConstants.GROUP_APP_MENU_HELP_ABOUT, Order = 1000000)]
	sealed class AboutPage : MenuItemBase {
		public override void Execute(IMenuItemContext context) {
			// Make sure to get the textview before unselecting anything in case we have two tab
			// groups opened and the active tab is not a textview
			var activeTextView = MainWindow.Instance.SafeActiveTextView;
			MainWindow.Instance.UnselectAll();
			Display(activeTextView);
		}

		public static void Display(DecompilerTextView textView) {
			AvalonEditTextOutput output = new AvalonEditTextOutput();
			output.WriteLine(string.Format("dnSpy version {0}", typeof(MainWindow).Assembly.GetName().Version), TextTokenType.Text);
			var decVer = typeof(ICSharpCode.Decompiler.Ast.AstBuilder).Assembly.GetName().Version;
			output.WriteLine(string.Format("ILSpy Decompiler version {0}.{1}.{2}", decVer.Major, decVer.Minor, decVer.Build), TextTokenType.Text);
			foreach (var plugin in DnSpy.App.CompositionContainer.GetExportedValues<IAboutPageAddition>())
				plugin.Write(output);
			output.WriteLine();
			using (Stream s = typeof(AboutPage).Assembly.GetManifestResourceStream(typeof(dnSpy.StartUpClass), "README.txt")) {
				using (StreamReader r = new StreamReader(s)) {
					string line;
					while ((line = r.ReadLine()) != null) {
						output.WriteLine(line, TextTokenType.Text);
					}
				}
			}
			output.AddVisualLineElementGenerator(new MyLinkElementGenerator("SharpDevelop", "http://www.icsharpcode.net/opensource/sd/"));
			output.AddVisualLineElementGenerator(new MyLinkElementGenerator("MIT License", "resource:license.txt"));
			output.AddVisualLineElementGenerator(new MyLinkElementGenerator("LGPL", "resource:LGPL.txt"));
			output.AddVisualLineElementGenerator(new MyLinkElementGenerator("COPYING", "resource:COPYING"));
			textView.ShowText(output);
			MainWindow.Instance.SetTitle(textView, "About");
		}

		sealed class MyLinkElementGenerator : LinkElementGenerator {
			readonly Uri uri;

			public MyLinkElementGenerator(string matchText, string url) : base(new Regex(Regex.Escape(matchText))) {
				this.uri = new Uri(url);
				this.RequireControlModifierForClick = false;
			}

			protected override Uri GetUriFromMatch(Match match) {
				return uri;
			}
		}
	}

	/// <summary>
	/// Interface that allows plugins to extend the about page.
	/// </summary>
	public interface IAboutPageAddition {
		void Write(ISmartTextOutput textOutput);
	}
}
