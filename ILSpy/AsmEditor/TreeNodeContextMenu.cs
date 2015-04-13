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

using System.Windows.Controls;

namespace ICSharpCode.ILSpy.AsmEditor
{
	public abstract class TreeNodeContextMenu : IContextMenuEntry2
	{
		public virtual bool IsVisible(TextViewContext context)
		{
			return context.TreeView == MainWindow.Instance.treeView &&
				context.SelectedTreeNodes != null &&
				context.SelectedTreeNodes.Length > 0;
		}

		public virtual bool IsEnabled(TextViewContext context)
		{
			return IsVisible(context);
		}

		public abstract void Execute(TextViewContext context);

		public virtual void Initialize(TextViewContext context, MenuItem menuItem)
		{
		}

		protected void SetHeader(TextViewContext context, MenuItem menuItem, string singular, string plural)
		{
			menuItem.Header = context.SelectedTreeNodes != null &&
						context.SelectedTreeNodes.Length == 1 ?
						singular : plural;
		}
	}
}
