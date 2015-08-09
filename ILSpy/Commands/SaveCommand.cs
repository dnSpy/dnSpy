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

using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Tabs;

namespace ICSharpCode.ILSpy {
	[ExportMainMenuCommand(Menu = "_File", MenuIcon = "Save", MenuCategory = "Save", MenuOrder = 1000)]
	[ExportContextMenuEntry(Order = 100, InputGestureText = "Ctrl+S", Category = "Tabs", Icon = "Save")]
	sealed class SaveCommand : CommandWrapper, IMainMenuCommandInitialize, IContextMenuEntry2 {
		public SaveCommand()
			: base(ApplicationCommands.Save) {
		}

		public bool IsVisible(TextViewContext context) {
			return MainWindow.Instance.IsDecompilerTabControl(context.TabControl) &&
				CanExecute(null);
		}

		public bool IsEnabled(TextViewContext context) {
			return CanExecute(null);
		}

		public void Execute(TextViewContext context) {
			base.Execute(null);
		}

		public void Initialize(MenuItem menuItem) {
			menuItem.Header = MainWindow.Instance.ActiveTabState is DecompileTabState ? "_Save Code…" : "_Save…";
		}

		public void Initialize(TextViewContext context, MenuItem menuItem) {
			Initialize(menuItem);
		}
	}
}
