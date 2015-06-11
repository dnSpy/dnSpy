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
	sealed class EditMethodOverride : IEdit<MethodOverrideVM>
	{
		readonly Window ownerWindow;

		public EditMethodOverride()
			: this(null)
		{
		}

		public EditMethodOverride(Window ownerWindow)
		{
			this.ownerWindow = ownerWindow;
		}

		public MethodOverrideVM Edit(string title, MethodOverrideVM mo)
		{
			var dnlibPicker = new DnlibTypePicker(ownerWindow);
			var method = dnlibPicker.GetDnlibType<IMethodDefOrRef>(new FlagsTreeViewNodeFilter(VisibleMembersFlags.MethodDef), mo.MethodDeclaration, mo.OwnerModule);
			if (method == null)
				return null;

			mo.MethodDeclaration = method;
			return mo;
		}
	}
}
