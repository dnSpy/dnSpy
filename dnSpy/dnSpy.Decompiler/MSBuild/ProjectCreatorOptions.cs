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

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Decompiler.MSBuild {
	sealed class ProjectCreatorOptions {
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

		/// <summary>
		/// Creates a <see cref="IDecompilerOutput"/>
		/// </summary>
		public Func<TextWriter, IDecompilerOutput> CreateDecompilerOutput { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="directory">Base directory</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public ProjectCreatorOptions(string directory, CancellationToken cancellationToken) {
			Directory = directory ?? throw new ArgumentNullException(nameof(directory));
			CancellationToken = cancellationToken;
			ProjectModules = new List<ProjectModuleOptions>();
			UserGACPaths = new List<string>();
			CreateDecompilerOutput = textWriter => new TextWriterDecompilerOutput(textWriter);
		}
	}
}
