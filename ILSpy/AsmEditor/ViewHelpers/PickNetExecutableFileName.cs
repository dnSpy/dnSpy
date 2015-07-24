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

using System.IO;
using System.Windows.Forms;

namespace dnSpy.AsmEditor.ViewHelpers {
	sealed class PickNetExecutableFileName : IPickNetExecutableFileName {
		public string GetFileName(string currentFileName, string extension) {
			var dialog = new SaveFileDialog() {
				Filter = ".NET Executables (*.exe, *.dll, *.netmodule, *.winmd)|*.exe;*.dll;*.netmodule;*.winmd|All files (*.*)|*.*",
				RestoreDirectory = true,
				DefaultExt = extension,
				ValidateNames = true,
			};
			if (!string.IsNullOrWhiteSpace(currentFileName))
				dialog.InitialDirectory = Path.GetDirectoryName(currentFileName);

			if (dialog.ShowDialog() != DialogResult.OK)
				return null;

			return dialog.FileName;
		}
	}
}
