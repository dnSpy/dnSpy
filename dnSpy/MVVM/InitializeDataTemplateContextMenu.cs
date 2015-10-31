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

using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ICSharpCode.ILSpy;

namespace dnSpy.MVVM {
	[Export(typeof(IInitializeDataTemplate))]
	public sealed class InitializeDataTemplateContextMenu : IInitializeDataTemplate {
		public void Initialize(DependencyObject d) {
			var fwe = d as FrameworkElement;
			if (fwe == null)
				return;

			if (fwe is ListBox)
				ContextMenuProvider.Add(fwe, ListBoxIgnore);
			else
				ContextMenuProvider.Add(fwe);
		}

		static bool ListBoxIgnore(DependencyObject o) {
			return o is ScrollBar;
		}
	}
}
