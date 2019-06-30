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
using dnSpy.Decompiler.Properties;

namespace dnSpy.Decompiler.MSBuild {
	sealed class SplashScreenProjectFile : ProjectFile {
		public override string Description => string.Format(dnSpy_Decompiler_Resources.MSBuild_CreateSplashScreenResource, rsrcName);
		public override BuildAction BuildAction => BuildAction.SplashScreen;
		public override string Filename => filename;
		readonly string filename;

		readonly byte[] data;
		readonly string rsrcName;

		public SplashScreenProjectFile(string filename, byte[] data, string rsrcName) {
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
