/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
using dnlib.IO;
using dnlib.W32Resources;
using dnSpy.Decompiler.Properties;

namespace dnSpy.Decompiler.MSBuild {
	sealed class ApplicationManifest : IFileJob {
		const int RT_MANIFEST = 24;

		public string Description => dnSpy_Decompiler_Resources.MSBuild_CreateAppManifest;
		public string Filename { get; }

		DataReader reader;

		ApplicationManifest(string filename, ref DataReader reader) {
			Filename = filename;
			this.reader = reader;
		}

		public static ApplicationManifest? TryCreate(Win32Resources resources, FilenameCreator filenameCreator) {
			if (resources is null)
				return null;

			var dir = resources.Find(new ResourceName(RT_MANIFEST));
			if (dir is null || dir.Directories.Count == 0)
				return null;
			dir = dir.Directories[0];
			if (dir.Data.Count == 0)
				return null;

			var reader = dir.Data[0].CreateReader();
			return new ApplicationManifest(filenameCreator.CreateName("app.manifest"), ref reader);
		}

		public void Create(DecompileContext ctx) {
			using (var stream = File.Create(Filename))
				reader.CopyTo(stream);
		}
	}

	sealed class ApplicationManifestProjectFile : ProjectFile {
		public override BuildAction BuildAction => BuildAction.None;
		public override string Description => dnSpy_Decompiler_Resources.MSBuild_CreateAppManifest;
		public override string Filename { get; }

		public ApplicationManifestProjectFile(string filename) => Filename = filename;

		public override void Create(DecompileContext ctx) {
			// ApplicationManifest writes the file
		}
	}
}
