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
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Files;
using dnSpy.Shared.UI.Files;

namespace dnSpy.Files {
	[Export(typeof(IDnSpyFileCreator))]
	sealed class DefaultDnSpyFileCreator : IDnSpyFileCreator {
		public double Order {
			get { return FilesConstants.ORDER_DEFAULT_FILE_CREATOR; }
		}

		public IDnSpyFile Create(IFileManager fileManager, DnSpyFileInfo fileInfo) {
			if (fileInfo.Type == FilesConstants.FILETYPE_FILE)
				return FileManager.CreateDnSpyFileFromFile(fileInfo, fileInfo.Name, fileManager.Settings.UseMemoryMappedIO, fileManager.Settings.LoadPDBFiles, fileManager.AssemblyResolver);
			else if (fileInfo.Type == FilesConstants.FILETYPE_GAC) {
				var filename = GetGacFilename(fileInfo.Name);
				if (filename != null)
					return FileManager.CreateDnSpyFileFromFile(fileInfo, filename, fileManager.Settings.UseMemoryMappedIO, fileManager.Settings.LoadPDBFiles, fileManager.AssemblyResolver);
			}
			else if (fileInfo.Type == FilesConstants.FILETYPE_REFASM) {
				var filename = GetRefFileFilename(fileInfo.Name);
				if (filename != null)
					return FileManager.CreateDnSpyFileFromFile(fileInfo, filename, fileManager.Settings.UseMemoryMappedIO, fileManager.Settings.LoadPDBFiles, fileManager.AssemblyResolver);
			}
			return null;
		}

		public IDnSpyFilenameKey CreateKey(IFileManager fileManager, DnSpyFileInfo fileInfo) {
			if (fileInfo.Type == FilesConstants.FILETYPE_FILE)
				return new FilenameKey(fileInfo.Name);
			else if (fileInfo.Type == FilesConstants.FILETYPE_GAC) {
				var filename = GetGacFilename(fileInfo.Name);
				if (filename != null)
					return new FilenameKey(filename);
			}
			else if (fileInfo.Type == FilesConstants.FILETYPE_REFASM) {
				var filename = GetRefFileFilename(fileInfo.Name);
				if (filename != null)
					return new FilenameKey(filename);
			}
			return null;
		}

		static string GetGacFilename(string asmFullName) {
			return GacInfo.FindInGac(new AssemblyNameInfo(asmFullName));
		}

		static string GetRefFileFilename(string s) {
			int index = s.LastIndexOf(FilesConstants.REFERENCE_ASSEMBLY_SEPARATOR);
			Debug.Assert(index >= 0);
			if (index < 0)
				return null;

			var asmFullName = s.Substring(0, index);
			var refFileName = s.Substring(index + FilesConstants.REFERENCE_ASSEMBLY_SEPARATOR.Length);
			var f = GetGacFilename(asmFullName);
			if (f != null)
				return f;

			return refFileName.Trim();
		}
	}
}
