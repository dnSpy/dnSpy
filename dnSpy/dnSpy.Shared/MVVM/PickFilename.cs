/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.IO;
using System.Windows.Forms;
using dnSpy.Shared.Properties;

namespace dnSpy.Shared.MVVM {
	/// <summary>
	/// Lets the user pick a new filename. Returns null if the user didn't pick a new filename.
	/// </summary>
	/// <param name="currentFileName">Current filename or null</param>
	/// <param name="defaultExtension">Default extension. It must not contain a period. Eg. valid
	/// extensions are "exe" and "dll" but not ".exe"</param>
	/// <param name="filter">Filename filter or null</param>
	/// <returns></returns>
	public interface IPickFilename {
		string GetFilename(string currentFileName, string defaultExtension, string filter = null);
	}

	public static class PickFilenameConstants {
		public static readonly string ImagesFilter = $"{dnSpy_Shared_Resources.Files_Images}|*.png;*.gif;*.bmp;*.dib;*.jpg;*.jpeg;*.jpe;*.jif;*.jfif;*.jfi;*.ico;*.cur|{dnSpy_Shared_Resources.AllFiles} (*.*)|*.*";
		public static readonly string StrongNameKeyFilter = $"{dnSpy_Shared_Resources.Files_StrongNameKeyFiles} (*.snk)|*.snk|{dnSpy_Shared_Resources.AllFiles} (*.*)|*.*";
		public static readonly string AnyFilenameFilter = $"{dnSpy_Shared_Resources.AllFiles} (*.*)|*.*";
		public static readonly string DotNetExecutableFilter = $"{dnSpy_Shared_Resources.Files_DotNetExecutables} (*.exe)|*.exe|{dnSpy_Shared_Resources.AllFiles} (*.*)|*.*";
		public static readonly string DotNetAssemblyOrModuleFilter = $"{dnSpy_Shared_Resources.Files_DotNetExecutables} (*.exe, *.dll, *.netmodule, *.winmd)|*.exe;*.dll;*.netmodule;*.winmd|{dnSpy_Shared_Resources.AllFiles} (*.*)|*.*";
		public static readonly string NetModuleFilter = $"{dnSpy_Shared_Resources.Files_DotNetNetModules} (*.netmodule)|*.netmodule|{dnSpy_Shared_Resources.AllFiles} (*.*)|*.*";
		public static readonly string ExecutableFilter = $"{dnSpy_Shared_Resources.Files_Executables} (*.exe)|*.exe|{dnSpy_Shared_Resources.AllFiles} (*.*)|*.*";
	}

	[Export(typeof(IPickFilename))]
	public sealed class PickFilename : IPickFilename {
		public string GetFilename(string currentFileName, string extension, string filter) {
			var dialog = new OpenFileDialog() {
				Filter = string.IsNullOrEmpty(filter) ? PickFilenameConstants.AnyFilenameFilter : filter,
				RestoreDirectory = true,
				DefaultExt = extension,
				ValidateNames = true,
			};
			if (File.Exists(currentFileName))
				dialog.InitialDirectory = Path.GetDirectoryName(currentFileName);

			if (dialog.ShowDialog() != DialogResult.OK)
				return null;

			return dialog.FileName;
		}
	}
}
