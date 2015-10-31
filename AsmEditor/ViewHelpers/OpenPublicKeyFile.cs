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
using System.Windows.Forms;
using dnlib.DotNet;
using dnSpy.MVVM;
using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor.ViewHelpers {
	sealed class OpenPublicKeyFile : IOpenPublicKeyFile {
		readonly Window ownerWindow;

		public OpenPublicKeyFile()
			: this(null) {
		}

		public OpenPublicKeyFile(Window ownerWindow) {
			this.ownerWindow = ownerWindow;
		}

		public PublicKey Open() {
			var dialog = new OpenFileDialog() {
				Filter = PickFilenameConstants.StrongNameKeyFilter,
				RestoreDirectory = true,
			};
			if (dialog.ShowDialog() != DialogResult.OK)
				return null;
			if (string.IsNullOrEmpty(dialog.FileName))
				return null;

			try {
				var snk = new StrongNameKey(dialog.FileName);
				return new PublicKey(snk.PublicKey);
			}
			catch {
			}

			MainWindow.Instance.ShowMessageBox(string.Format("'{0}' is not a strong name key file (snk)", dialog.FileName), MessageBoxButton.OK, ownerWindow);
			return null;
		}
	}
}
