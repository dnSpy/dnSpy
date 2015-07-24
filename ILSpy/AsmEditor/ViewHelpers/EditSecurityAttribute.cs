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
using dnSpy.AsmEditor.DnlibDialogs;
using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor.ViewHelpers {
	sealed class EditSecurityAttribute : IEdit<SecurityAttributeVM> {
		readonly Window ownerWindow;

		public EditSecurityAttribute()
			: this(null) {
		}

		public EditSecurityAttribute(Window ownerWindow) {
			this.ownerWindow = ownerWindow;
		}

		public SecurityAttributeVM Edit(string title, SecurityAttributeVM sa) {
			var win = new SecurityAttributeDlg();
			if (title != null)
				win.Title = title;
			win.DataContext = sa;
			win.Owner = ownerWindow ?? MainWindow.Instance;
			if (win.ShowDialog() != true)
				return null;

			return sa;
		}
	}
}
