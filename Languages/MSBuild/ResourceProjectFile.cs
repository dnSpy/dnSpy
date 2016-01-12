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
	sealed class ResourceProjectFile : ProjectFile {
		public override string Description {
			get { return string.Format(Languages_Resources.MSBuild_CreateResource, rsrcName); }
		}

		public override BuildAction BuildAction {
			get { return BuildAction.Resource; }
		}

		public override string Filename {
			get { return filename; }
		}
		readonly string filename;

		readonly byte[] data;
		readonly string rsrcName;

		public ResourceProjectFile(string filename, byte[] data, string rsrcName) {
			this.filename = filename;
			this.data = data;
			this.rsrcName = rsrcName;
		}

		public override void Create(DecompileContext ctx) {
			using (var stream = File.Create(Filename))
				stream.Write(data, 0, data.Length);
		}
	}
}
