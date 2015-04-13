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
using System.ComponentModel;
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

namespace ICSharpCode.ILSpy.AsmEditor.SaveModule
{
	/// <summary>
	/// Interaction logic for SaveMultiModule.xaml
	/// </summary>
	public partial class SaveMultiModule : SaveModuleWindow
	{
		public SaveMultiModule()
		{
			InitializeComponent();
		}

		private void Options_CanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = e.Parameter is SaveModuleOptionsVM;
		}

		private void Options_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			ShowOptions((SaveModuleOptionsVM)e.Parameter);
		}

		ListViewItem GetListViewItem(object o)
		{
			var depo = o as DependencyObject;
			while (depo != null && !(depo is ListViewItem) && depo != listView)
				depo = VisualTreeHelper.GetParent(depo);
			return depo as ListViewItem;
		}

		private void listView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left)
				return;
			if (GetListViewItem(e.OriginalSource) == null)
				return;
			ShowOptions((SaveModuleOptionsVM)listView.SelectedItem);
		}
	}
}
