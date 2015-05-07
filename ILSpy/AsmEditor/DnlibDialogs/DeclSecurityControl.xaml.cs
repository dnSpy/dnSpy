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
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.ILSpy.AsmEditor.ViewHelpers;

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	/// <summary>
	/// Interaction logic for DeclSecurityControl.xaml
	/// </summary>
	public partial class DeclSecurityControl : UserControl
	{
		public DeclSecurityControl()
		{
			InitializeComponent();
			DataContextChanged += (s, e) => {
				var data = DataContext as DeclSecurityVM;
				if (data != null) {
					var ownerWindow = Window.GetWindow(this);
					data.EditSecurityAttribute = new EditSecurityAttribute(ownerWindow);
				}
			};
		}

		private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (!UIUtils.IsLeftDoubleClick<ListViewItem>(listView, e))
				return;
			var data = DataContext as DeclSecurityVM;
			if (data != null)
				data.EditCurrent();
		}
	}
}
