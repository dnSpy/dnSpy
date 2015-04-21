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

using System.Windows.Forms;

namespace ICSharpCode.ILSpy.AsmEditor.ViewHelpers
{
	sealed class OpenAssembly : IOpenAssembly
	{
		public LoadedAssembly Open()
		{
			var dialog = new OpenFileDialog() {
				Filter = ".NET Executables (*.exe, *.dll, *.netmodule)|*.exe;*.dll;*.netmodule|All files (*.*)|*.*",
				RestoreDirectory = true,
			};
			if (dialog.ShowDialog() != DialogResult.OK)
				return null;
			if (string.IsNullOrEmpty(dialog.FileName))
				return null;

			var asm = MainWindow.Instance.CurrentAssemblyList.FindAssemblyByFileName(dialog.FileName);
			if (asm != null)
				return null;

			return MainWindow.Instance.CurrentAssemblyList.OpenAssembly(dialog.FileName);
		}
	}
}
