/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using System.Windows;
using ICSharpCode.ILSpy;

namespace dnSpy.Debugger.Exceptions {
	interface IGetNewExceptionName {
		string GetName();
	}

	sealed class GetNewExceptionName : IGetNewExceptionName {
		readonly Window ownerWindow;

		public GetNewExceptionName(Window ownerWindow) {
			this.ownerWindow = ownerWindow;
		}

		public string GetName() {
			var ask = new AskForInput();
			ask.Owner = ownerWindow ?? MainWindow.Instance;
			ask.Title = "Add an Exception";
			ask.label.Content = "_Full name";
			ask.textBox.Text = string.Empty;
			ask.ShowDialog();
			if (ask.DialogResult != true)
				return null;
			var t = ask.textBox.Text.Trim();
			if (string.IsNullOrEmpty(t))
				return null;

			return t;
		}
	}
}
