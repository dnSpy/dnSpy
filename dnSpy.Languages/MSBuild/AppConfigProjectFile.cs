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

using System.IO;
using dnSpy.Languages.Properties;

namespace dnSpy.Languages.MSBuild {
	sealed class AppConfigProjectFile : ProjectFile {
		public override string Description {
			get { return string.Format(Languages_Resources.MSBuild_CopyAppConfig, existingName); }
		}

		public override BuildAction BuildAction {
			get { return BuildAction.None; }
		}

		public override string Filename {
			get { return filename; }
		}
		readonly string filename;

		readonly string existingName;

		public AppConfigProjectFile(string filename, string existingName) {
			this.filename = filename;
			this.existingName = existingName;
		}

		public override void Create(DecompileContext ctx) {
			File.Copy(existingName, Filename, true);
		}
	}
}
