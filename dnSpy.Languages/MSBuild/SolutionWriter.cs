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
using System.IO;
using System.Linq;
using System.Text;
using dnlib.PE;

namespace dnSpy.Languages.MSBuild {
	sealed class SolutionWriter {
		readonly ProjectVersion projectVersion;
		readonly List<Project> projects;
		readonly string filename;
		readonly List<string> configs;
		readonly List<string> platforms;

		public SolutionWriter(ProjectVersion projectVersion, IList<Project> projects, string filename) {
			this.projectVersion = projectVersion;
			this.projects = projects.ToList();
			this.projects.Sort((a, b) => {
				// Sort exes first since VS picks the first file in the solution to be the
				// "StartUp Project".
				int ae = (a.Module.Characteristics & Characteristics.Dll) == 0 ? 0 : 1;
				int be = (b.Module.Characteristics & Characteristics.Dll) == 0 ? 0 : 1;
				int c = ae.CompareTo(be);
				if (c != 0)
					return c;
				return StringComparer.OrdinalIgnoreCase.Compare(a.Filename, b.Filename);
			});
			this.filename = filename;

			this.configs = new List<string>();
			this.configs.Add("Debug");
			this.configs.Add("Release");

			var hash = new HashSet<string>(projects.Select(a => a.Platform));
			this.platforms = new List<string>(hash.Count);
			this.platforms.Add("Any CPU");
			hash.Remove("AnyCPU");
			if (hash.Count > 0)
				this.platforms.Add("Mixed Platforms");
			foreach (var p in hash)
				this.platforms.Add(p);
		}

		public void Write() {
			Directory.CreateDirectory(Path.GetDirectoryName(filename));
			using (var writer = new StreamWriter(filename, false, Encoding.UTF8)) {
				const string crlf = "\r\n"; // Make sure it's always CRLF
				writer.Write(crlf);
				switch (projectVersion) {
				case ProjectVersion.VS2005:
					writer.Write("Microsoft Visual Studio Solution File, Format Version 9.00" + crlf);
					writer.Write("# Visual Studio 2005" + crlf);
					break;

				case ProjectVersion.VS2008:
					writer.Write("Microsoft Visual Studio Solution File, Format Version 10.00" + crlf);
					writer.Write("# Visual Studio 2008" + crlf);
					break;

				case ProjectVersion.VS2010:
					writer.Write("Microsoft Visual Studio Solution File, Format Version 11.00" + crlf);
					writer.Write("# Visual Studio 2010" + crlf);
					break;

				case ProjectVersion.VS2012:
					writer.Write("Microsoft Visual Studio Solution File, Format Version 12.00" + crlf);
					writer.Write("# Visual Studio 2012" + crlf);
					break;

				case ProjectVersion.VS2013:
					writer.Write("Microsoft Visual Studio Solution File, Format Version 12.00" + crlf);
					writer.Write("# Visual Studio 2013" + crlf);
					// 2013 RTM = 12.0.21005.1
					// 2013.1 = 12.0.30110.0
					// 2013.2 = 12.0.30324.0
					// 2013.3 = 12.0.30723.0
					// 2013.4 = 12.0.31101.0
					// 2013.5 = 12.0.40629.0
					writer.Write("VisualStudioVersion = 12.0.21005.1" + crlf);
					writer.Write("MinimumVisualStudioVersion = 10.0.40219.1" + crlf);
					break;

				case ProjectVersion.VS2015:
					writer.Write("Microsoft Visual Studio Solution File, Format Version 12.00" + crlf);
					writer.Write("# Visual Studio 14" + crlf);
					// 2015 RTM = 14.0.23107.0
					// 2015.1 = 14.0.24720.0
					// 2015.2 = 14.0.25123.0
					writer.Write("VisualStudioVersion = 14.0.23107.0" + crlf);
					writer.Write("MinimumVisualStudioVersion = 10.0.40219.1" + crlf);
					break;

				default:
					throw new InvalidOperationException();
				}
				foreach (var p in projects) {
					writer.Write("Project(\"{0}\") = \"{1}\", \"{1}\\{2}\", \"{3}\"" + crlf,
						p.LanguageGuid.ToString("B").ToUpperInvariant(),
						Path.GetFileName(Path.GetDirectoryName(p.Filename)),
						Path.GetFileName(p.Filename),
						p.Guid.ToString("B").ToUpperInvariant()
					);
					writer.Write("EndProject" + crlf);
				}
				writer.Write("Global" + crlf);
				writer.Write("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution" + crlf);
				foreach (var c in configs) {
					foreach (var p in platforms)
						writer.Write("\t\t{0}|{1} = {0}|{1}" + crlf, c, p);
				}
				writer.Write("\tEndGlobalSection" + crlf);
				writer.Write("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution" + crlf);
				foreach (var p in projects) {
					var prjGuid = p.Guid.ToString("B").ToUpperInvariant();
					var pp = p.Platform.Equals("AnyCPU") ? "Any CPU" : p.Platform;
					foreach (var c in configs) {
						foreach (var f in platforms) {
							writer.Write("\t\t{0}.{1}|{2}.ActiveCfg = {1}|{3}" + crlf, prjGuid, c, f, pp);
							writer.Write("\t\t{0}.{1}|{2}.Build.0 = {1}|{3}" + crlf, prjGuid, c, f, pp);
						}
					}
				}
				writer.Write("\tEndGlobalSection" + crlf);
				writer.Write("\tGlobalSection(SolutionProperties) = preSolution" + crlf);
				writer.Write("\t\tHideSolutionNode = FALSE" + crlf);
				writer.Write("\tEndGlobalSection" + crlf);
				writer.Write("EndGlobal" + crlf);
			}
		}
	}
}
