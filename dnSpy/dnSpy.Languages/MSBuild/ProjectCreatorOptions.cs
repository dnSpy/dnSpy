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

using System;
using System.Collections.Generic;
using System.Threading;

namespace dnSpy.Languages.MSBuild {
	public sealed class ProjectCreatorOptions {
		/// <summary>
		/// The logger or null
		/// </summary>
		public IMSBuildProjectWriterLogger Logger { get; set; }

		/// <summary>
		/// Gets notified when the progress gets updated
		/// </summary>
		public IMSBuildProgressListener ProgressListener { get; set; }

		/// <summary>
		/// All modules that should be decompiled
		/// </summary>
		public List<ProjectModuleOptions> ProjectModules { get; }

		/// <summary>
		/// Project version
		/// </summary>
		public ProjectVersion ProjectVersion { get; set; }

		/// <summary>
		/// Number of threads to use or 0 to use as many threads as there are processors.
		/// </summary>
		public int NumberOfThreads { get; set; }

		/// <summary>
		/// Base directory of all project dirs. This is the directory where the .sln file is saved
		/// if it's written.
		/// </summary>
		public string Directory { get; }

		/// <summary>
		/// Filename relative to <see cref="Directory"/>. Use null if no solution file should be
		/// written.
		/// </summary>
		public string SolutionFilename { get; set; }

		/// <summary>
		/// User GAC paths. All files stored in any of these directories are considered GAC files.
		/// </summary>
		public List<string> UserGACPaths { get; }

		/// <summary>
		/// Cancellation token
		/// </summary>
		public CancellationToken CancellationToken { get; }

		public ProjectCreatorOptions(string dir, CancellationToken cancellationToken) {
			if (dir == null)
				throw new ArgumentNullException();
			this.Directory = dir;
			this.CancellationToken = cancellationToken;
			this.ProjectModules = new List<ProjectModuleOptions>();
			this.UserGACPaths = new List<string>();
		}
	}
}
