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
using dnSpy.AsmEditor.DnlibDialogs;
using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor.ViewHelpers {
	sealed class TypeSigCreator : ITypeSigCreator {
		readonly Window ownerWindow;

		public TypeSigCreator()
			: this(null) {
		}

		public TypeSigCreator(Window ownerWindow) {
			this.ownerWindow = ownerWindow;
		}

		public TypeSig Create(TypeSigCreatorOptions options, TypeSig typeSig, out bool canceled) {
			var data = new TypeSigCreatorVM(options, typeSig);
			data.TypeSig = typeSig;
			var win = new TypeSigCreatorDlg();
			win.DataContext = data;
			win.Owner = ownerWindow ?? MainWindow.Instance;
			if (win.ShowDialog() != true) {
				canceled = true;
				return null;
			}

			canceled = false;
			return data.TypeSig;
		}
	}
}
