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

using System.Collections.Generic;
using dnSpy.Contracts.Files;

namespace dnSpy.Files {
	sealed class DefaultDnSpyFileLoader : IDnSpyFileLoader {
		readonly IFileManager fileManager;

		public DefaultDnSpyFileLoader(IFileManager fileManager) {
			this.fileManager = fileManager;
		}

		public IDnSpyFile[] Load(IEnumerable<DnSpyFileInfo> files) {
			var loadedFiles = new List<IDnSpyFile>();
			var hash = new HashSet<IDnSpyFile>();
			foreach (var f in files) {
				if (f.Type == FileConstants.FILETYPE_FILE && string.IsNullOrEmpty(f.Name))
					continue;
				var file = fileManager.TryGetOrCreate(f);
				if (file != null && !hash.Contains(file)) {
					hash.Add(file);
					loadedFiles.Add(file);
				}
			}
			return loadedFiles.ToArray();
		}
	}
}
