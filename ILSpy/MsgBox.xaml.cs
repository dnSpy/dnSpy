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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ICSharpCode.ILSpy
{
	[Flags]
	public enum MsgBoxButton
	{
		None = 0,
		OK = 1,
		No = 2,
		Cancel = 4,
	}

	/// <summary>
	/// Interaction logic for MsgBox.xaml
	/// </summary>
	public partial class MsgBox : Window
	{
		public MsgBoxButton ButtonClicked { get; private set; }

		public MsgBox()
		{
			InitializeComponent();
		}

		private void okButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
			this.ButtonClicked = MsgBoxButton.OK;
			Close();
		}

		private void noButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.ButtonClicked = MsgBoxButton.No;
			Close();
		}

		private void cancelButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.ButtonClicked = MsgBoxButton.Cancel;
			Close();
		}
	}
}
