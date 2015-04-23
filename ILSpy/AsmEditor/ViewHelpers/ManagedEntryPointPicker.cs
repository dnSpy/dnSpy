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
using dnlib.DotNet;
using ICSharpCode.ILSpy.AsmEditor.DnlibDialogs;
using ICSharpCode.ILSpy.TreeNodes.Filters;

namespace ICSharpCode.ILSpy.AsmEditor.ViewHelpers
{
	sealed class ManagedEntryPointPicker : IManagedEntryPointPicker
	{
		readonly Window ownerWindow;

		public ManagedEntryPointPicker()
			: this(null)
		{
		}

		public ManagedEntryPointPicker(Window ownerWindow)
		{
			this.ownerWindow = ownerWindow;
		}

		public IManagedEntryPoint GetEntryPoint(ModuleDef module, object selectedItem)
		{
			var data = new MemberPickerVM(MainWindow.Instance.CurrentLanguage, new EntryPointTreeViewNodeFilter(module));
			var win = new MemberPickerDlg();
			win.DataContext = data;
			win.Owner = ownerWindow ?? MainWindow.Instance;
			data.SelectItem(selectedItem);
			if (win.ShowDialog() != true)
				return null;

			return data.SelectedDnlibObject as IManagedEntryPoint;
		}
	}
}
