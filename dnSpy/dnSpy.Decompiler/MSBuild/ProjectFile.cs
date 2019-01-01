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

namespace dnSpy.Decompiler.MSBuild {
	abstract class ProjectFile : IFileJob {
		public abstract string Description { get; }
		public abstract string Filename { get; }
		public abstract BuildAction BuildAction { get; }
		public ProjectFile DependentUpon { get; set; }
		public string SubType { get; set; }
		public string Generator { get; set; }
		public ProjectFile LastGenOutput { get; set; }
		public bool AutoGen { get; set; }
		public bool DesignTime { get; set; }
		public bool DesignTimeSharedInput { get; set; }

		public abstract void Create(DecompileContext ctx);
	}
}
