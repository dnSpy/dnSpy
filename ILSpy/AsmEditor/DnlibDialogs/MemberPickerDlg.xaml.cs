﻿/*
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
using ICSharpCode.ILSpy.AsmEditor.ViewHelpers;

namespace ICSharpCode.ILSpy.AsmEditor.DnlibDialogs
{
	/// <summary>
	/// Interaction logic for MemberPickerDlg.xaml
	/// </summary>
	public partial class MemberPickerDlg : Window
	{
		public MemberPickerDlg()
		{
			InitializeComponent();
			DataContextChanged += (s, e) => {
				var data = DataContext as MemberPickerVM;
				if (data != null)
					data.OpenAssembly = new OpenAssembly();
			};
		}

		private void okButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
			Close();
		}
	}
}
