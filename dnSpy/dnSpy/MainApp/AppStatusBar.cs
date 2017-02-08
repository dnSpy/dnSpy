/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using dnSpy.Contracts.App;
using dnSpy.Controls;

namespace dnSpy.MainApp {
	[Export]
	sealed class AppStatusBar : IAppStatusBar, IStackedContentChild {
		int openCounter;

		public object UIObject => statusBar;
		readonly StatusBar statusBar;
		readonly TextBlock textBlock;

		public AppStatusBar() {
			statusBar = new StatusBar { Visibility = Visibility.Collapsed };
			textBlock = new TextBlock();
			statusBar.Items.Add(new StatusBarItem { Content = textBlock });
		}

		public void Close() {
			Debug.Assert(openCounter > 0);
			openCounter--;
			if (openCounter == 0)
				statusBar.Visibility = Visibility.Collapsed;
		}

		public void Open() {
			openCounter++;
			if (openCounter == 1) {
				textBlock.Text = string.Empty;
				statusBar.Visibility = Visibility.Visible;
			}
		}

		public void Show(string text) {
			Debug.Assert(openCounter > 0);
			if (text == null)
				throw new ArgumentNullException(nameof(text));
			textBlock.Text = text;
		}
	}
}
