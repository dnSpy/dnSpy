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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using dnSpy.Languages.Properties;

namespace dnSpy.Languages.MSBuild {
	public sealed class MSBuildProjectCreator {
		readonly ProjectCreatorOptions options;
		readonly List<Project> projects;
		readonly IMSBuildProjectWriterLogger logger;
		readonly IMSBuildProgressListener progressListener;
		int errors;
		int totalProgress;

		public IEnumerable<string> ProjectFilenames {
			get { return projects.Select(a => a.Filename); }
		}

		public string SolutionFilename {
			get { return Path.Combine(options.Directory, options.SolutionFilename); }
		}

		sealed class MyLogger : IMSBuildProjectWriterLogger {
			readonly MSBuildProjectCreator owner;
			readonly IMSBuildProjectWriterLogger logger;

			public MyLogger(MSBuildProjectCreator owner, IMSBuildProjectWriterLogger logger) {
				this.owner = owner;
				this.logger = logger ?? NoMSBuildProjectWriterLogger.Instance;
			}

			public void Error(string message) {
				Interlocked.Increment(ref owner.errors);
				this.logger.Error(message);
			}
		}

		public MSBuildProjectCreator(ProjectCreatorOptions options) {
			if (options == null)
				throw new ArgumentNullException();
			this.options = options;
			this.logger = new MyLogger(this, options.Logger);
			this.progressListener = options.ProgressListener ?? NoMSBuildProgressListener.Instance;
			this.projects = new List<Project>();
		}

		public void Create() {
			SatelliteAssemblyFinder satelliteAssemblyFinder = null;
			try {
				var opts = new ParallelOptions {
					CancellationToken = options.CancellationToken,
					MaxDegreeOfParallelism = options.NumberOfThreads <= 0 ? Environment.ProcessorCount : options.NumberOfThreads,
				};
				var filenameCreator = new FilenameCreator(options.Directory);
				var ctx = new DecompileContext(options.CancellationToken, logger);
				satelliteAssemblyFinder = new SatelliteAssemblyFinder();
				Parallel.ForEach(options.ProjectModules, opts, modOpts => {
					options.CancellationToken.ThrowIfCancellationRequested();
					string name;
					lock (filenameCreator)
						name = filenameCreator.Create(modOpts.Module);
					var p = new Project(modOpts, name, satelliteAssemblyFinder);
					lock (projects)
						projects.Add(p);
					p.CreateProjectFiles(ctx);
				});

				var jobs = GetJobs().ToArray();
				bool writeSolutionFile = !string.IsNullOrEmpty(options.SolutionFilename);
				int maxProgress = jobs.Length + projects.Count;
				if (writeSolutionFile)
					maxProgress++;
				progressListener.SetMaxProgress(maxProgress);

				Parallel.ForEach(GetJobs(), opts, job => {
					options.CancellationToken.ThrowIfCancellationRequested();
					try {
						job.Create(ctx);
					}
					catch (OperationCanceledException) {
						throw;
					}
					catch (Exception ex) {
						var fjob = job as IFileJob;
						if (fjob != null)
							logger.Error(string.Format(Languages_Resources.MSBuild_FileCreationFailed3, fjob.Filename, job.Description, ex.Message));
						else
							logger.Error(string.Format(Languages_Resources.MSBuild_FileCreationFailed2, job.Description, ex.Message));
					}
					progressListener.SetProgress(Interlocked.Increment(ref totalProgress));
				});
				Parallel.ForEach(projects, opts, p => {
					options.CancellationToken.ThrowIfCancellationRequested();
					try {
						var writer = new ProjectWriter(p, p.Options.ProjectVersion ?? options.ProjectVersion, projects, options.UserGACPaths);
						writer.Write();
					}
					catch (OperationCanceledException) {
						throw;
					}
					catch (Exception ex) {
						logger.Error(string.Format(Languages_Resources.MSBuild_FailedToCreateProjectFile, p.Filename, ex.Message));
					}
					progressListener.SetProgress(Interlocked.Increment(ref totalProgress));
				});
				if (writeSolutionFile) {
					options.CancellationToken.ThrowIfCancellationRequested();
					try {
						var writer = new SolutionWriter(options.ProjectVersion, projects, SolutionFilename);
						writer.Write();
					}
					catch (OperationCanceledException) {
						throw;
					}
					catch (Exception ex) {
						logger.Error(string.Format(Languages_Resources.MSBuild_FailedToCreateSolutionFile, SolutionFilename, ex.Message));
					}
					progressListener.SetProgress(Interlocked.Increment(ref totalProgress));
				}
				Debug.Assert(totalProgress == maxProgress);
				progressListener.SetProgress(maxProgress);
			}
			finally {
				if (satelliteAssemblyFinder != null)
					satelliteAssemblyFinder.Dispose();
			}
		}

		IEnumerable<IJob> GetJobs() {
			foreach (var p in projects) {
				foreach (var j in p.GetJobs())
					yield return j;
			}
		}
	}
}
