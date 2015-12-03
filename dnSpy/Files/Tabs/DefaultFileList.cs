/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

namespace dnSpy.Files.Tabs {
	sealed class DefaultFileList : IEnumerable {
		public string Name {
			get { return name; }
		}
		readonly string name;

		public string[] Assemblies {
			get { return assemblies.ToArray(); }
		}
		readonly List<string> assemblies;

		public DefaultFileList(string name) {
			this.name = name;
			this.assemblies = new List<string>();
		}

		public DefaultFileList(string name, IEnumerable<string> asmNames) {
			this.name = name;
			this.assemblies = new List<string>(asmNames);
			this.assemblies.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a, b));
		}

		public void Add(string assemblyFullName) {
			if (assemblyFullName == null)
				throw new ArgumentNullException();
			assemblies.Add(assemblyFullName);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return Assemblies.GetEnumerator();
		}
	}

	sealed class ReferenceFileFinder {
		readonly CancellationToken cancellationToken;
		readonly List<RefFileList> allFiles;

		public ReferenceFileFinder(CancellationToken cancellationToken) {
			this.cancellationToken = cancellationToken;
			this.allFiles = new List<RefFileList>();
		}

		public IEnumerable<DefaultFileList> AllFiles {
			get {
				return allFiles.Where(a => a.Files.Count > 0).Select(a => new DefaultFileList(a.Name, a.Files.Where(b => b.InGac).Select(b => b.AssemblyFullName))).Where(a => a.Assemblies.Length > 0);
			}
		}

		public void Find() {
			cancellationToken.ThrowIfCancellationRequested();
			var windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
			if (Directory.Exists(windir)) {
				var path = Path.Combine(windir, @"Microsoft.NET\Framework\v2.0.50727\RedistList");
				if (!Directory.Exists(path))
					path = Path.Combine(windir, @"Microsoft.NET\Framework64\v2.0.50727\RedistList");
				if (Directory.Exists(path))
					AddRedistList(path);
			}

			Find(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
		}

		void Find(string path) {
			if (!Directory.Exists(path))
				return;
			path = Path.Combine(path, @"Reference Assemblies\Microsoft\Framework");
			if (!Directory.Exists(path))
				return;

			foreach (var d1 in GetDirs(path)) { // d1 = eg. .NETCore, .NETFramework,  etc
				cancellationToken.ThrowIfCancellationRequested();
				var dirs = GetDirs(d1);
				var redistDir = dirs.FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a, Path.Combine(d1, "RedistList")));
				if (redistDir != null)
					AddRedistList(redistDir);

				foreach (var d2 in GetDirs(d1)) { // d2 = eg. v4.5.1, etc
					cancellationToken.ThrowIfCancellationRequested();
					dirs = GetDirs(d2);
					redistDir = dirs.FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a, Path.Combine(d2, "RedistList")));
					if (redistDir != null)
						AddRedistList(redistDir);

					var profileDir = dirs.FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a, Path.Combine(d2, "Profile")));
					if (profileDir != null) {
						foreach (var d3 in GetDirs(profileDir)) { // d3 = eg. Client
							cancellationToken.ThrowIfCancellationRequested();
							dirs = GetDirs(d3);
							redistDir = dirs.FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a, Path.Combine(d3, "RedistList")));
							if (redistDir != null)
								AddRedistList(redistDir);
						}
					}
				}
			}

			var net20 = allFiles.FirstOrDefault(a => a.Filename.EndsWith(@"\Microsoft.NET\Framework\v2.0.50727\RedistList\FrameworkList.xml", StringComparison.OrdinalIgnoreCase));
			var net30 = allFiles.FirstOrDefault(a => a.Filename.EndsWith(@"\Reference Assemblies\Microsoft\Framework\v3.0\RedistList\FrameworkList.xml", StringComparison.OrdinalIgnoreCase));
			var net35 = allFiles.FirstOrDefault(a => a.Filename.EndsWith(@"\Reference Assemblies\Microsoft\Framework\v3.5\RedistList\FrameworkList.xml", StringComparison.OrdinalIgnoreCase));
			var net35C = allFiles.FirstOrDefault(a => a.Filename.EndsWith(@"\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Client\RedistList\FrameworkList.xml", StringComparison.OrdinalIgnoreCase));
			var wpa81 = allFiles.FirstOrDefault(a => a.Filename.EndsWith(@"\Reference Assemblies\Microsoft\Framework\WindowsPhoneApp\v8.1\RedistList\FrameworkList.xml", StringComparison.OrdinalIgnoreCase));
			if (wpa81 != null)
				wpa81.Name = "Windows Phone App 8.1";	// Another one has the identical name so add "App" to it
			if (net30 != null)
				net30.AddFilesFrom(net20);
			if (net35 != null)
				net35.AddFilesFrom(net30);
			if (net35C != null)
				net35C.AddFilesFrom(net30);
			if (net20 != null)
				net20.Name = ".NET Framework 2.0";
			if (net30 != null)
				net30.Name = ".NET Framework 3.0";
			if (net35 != null)
				net35.Name = ".NET Framework 3.5";
		}

		void AddRedistList(string dir) {
			cancellationToken.ThrowIfCancellationRequested();
			var file = Path.Combine(dir, "FrameworkList.xml");
			if (!File.Exists(file))
				return;
			try {
				allFiles.Add(new RefFileList(file));
			}
			catch (InvalidOperationException) {
			}
		}

		static string[] GetDirs(string dir) {
			try {
				return Directory.GetDirectories(dir);
			}
			catch {
			}
			return new string[0];
		}

		sealed class RefFileList {
			public readonly string Filename;
			public readonly string Redist;
			public string Name { get; set; }
			public readonly string RuntimeVersion;
			public readonly string ToolsVersion;
			public readonly string ShortName;
			public readonly string IncludeFramework;
			public readonly string TargetFrameworkDirectory;
			public string TargetFilename { get; set; }
			public readonly List<RefFile> Files = new List<RefFile>();

			public RefFileList(string filename) {
				Filename = filename;
				var doc = XDocument.Load(filename, LoadOptions.None);
				var root = doc.Root;
				if (root.Name != "FileList")
					throw new InvalidOperationException();
				foreach (var attr in root.Attributes()) {
					if (attr.Name == "Redist")
						Redist = attr.Value;
					else if (attr.Name == "Name")
						Name = attr.Value;
					else if (attr.Name == "RuntimeVersion")
						RuntimeVersion = attr.Value;
					else if (attr.Name == "ToolsVersion")
						ToolsVersion = attr.Value;
					else if (attr.Name == "ShortName")
						ShortName = attr.Value;
					else if (attr.Name == "IncludeFramework")
						IncludeFramework = attr.Value;
					else if (attr.Name == "TargetFrameworkDirectory")
						TargetFrameworkDirectory = attr.Value;
					else
						Debug.Fail("Unknown attr");
				}

				foreach (var sect in root.Elements()) {
					if (sect.Name == "File")
						Files.Add(new RefFile(sect));
					else
						Debug.Fail("Unknown section");
				}

				var desc = CreateDescription();
				if (string.IsNullOrWhiteSpace(Name)) {
					var d = desc;
					if (d.StartsWith(".NET", StringComparison.OrdinalIgnoreCase))
						d = d.Substring(0, 4) + " " + d.Substring(4);
					Name = d;
				}
				TargetFilename = CreateTargetFilename(desc);
			}

			string CreateDescription() {
				var f = Filename;
				if (f.EndsWith(@"\Microsoft.NET\Framework\v2.0.50727\RedistList\FrameworkList.xml", StringComparison.OrdinalIgnoreCase))
					f = @"\Reference Assemblies\Microsoft\Framework\.NETFramework\v2.0\RedistList\FrameworkList.xml";
				else if (f.EndsWith(@"\Reference Assemblies\Microsoft\Framework\v3.0\RedistList\FrameworkList.xml", StringComparison.OrdinalIgnoreCase))
					f = @"\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.0\RedistList\FrameworkList.xml";
				else if (f.EndsWith(@"\Reference Assemblies\Microsoft\Framework\v3.5\RedistList\FrameworkList.xml", StringComparison.OrdinalIgnoreCase))
					f = @"\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\RedistList\FrameworkList.xml";
				const string pattern = @"\Reference Assemblies\Microsoft\Framework\";
				int index = f.IndexOf(pattern);
				if (index < 0)
					throw new InvalidOperationException();
				f = f.Substring(index + pattern.Length);
				var dirs = f.Split(new char[] { Path.DirectorySeparatorChar });
				string name = string.Empty;
				foreach (var dir in dirs) {
					if (StringComparer.OrdinalIgnoreCase.Equals(dir, "Profile"))
						continue;
					if (StringComparer.OrdinalIgnoreCase.Equals(dir, "RedistList"))
						break;
					if (name.Length != 0)
						name += " ";
					name += dir;
				}
				return name;
			}

			string CreateTargetFilename(string desc) {
				if (desc.StartsWith(".NET", StringComparison.OrdinalIgnoreCase))
					desc = "DOTNET " + desc.Substring(4);
				return desc + ".FileList.xml";
			}

			public override string ToString() {
				return string.Format("{0} - {1} - {2}", Filename, Redist, Name);
			}

			public void AddFilesFrom(RefFileList olderList) {
				if (olderList == null)
					return;
				var existing = new HashSet<string>(Files.Select(a => a.AssemblyName), StringComparer.OrdinalIgnoreCase);
				Files.AddRange(olderList.Files.Where(a => !existing.Contains(a.AssemblyName)));
			}
		}

		sealed class RefFile {
			public string AssemblyName { get; set; }
			public Version Version { get; set; }
			public string PublicKeyToken { get; set; }
			public string Culture { get; set; }
			public string ProcessorArchitecture { get; set; }
			public bool InGac { get; set; }
			public bool IsRedistRoot { get; set; }
			public string FileVersion { get; set; }

			public RefFile(XElement sect) {
				foreach (var attr in sect.Attributes()) {
					if (attr.Name == "AssemblyName")
						AssemblyName = attr.Value;
					else if (attr.Name == "Version")
						Version = new Version(attr.Value);
					else if (attr.Name == "PublicKeyToken")
						PublicKeyToken = attr.Value;
					else if (attr.Name == "Culture")
						Culture = attr.Value;
					else if (attr.Name == "ProcessorArchitecture")
						ProcessorArchitecture = attr.Value;
					else if (attr.Name == "InGac" || attr.Name == "InGAC")
						InGac = bool.Parse(attr.Value);
					else if (attr.Name == "IsRedistRoot")
						IsRedistRoot = bool.Parse(attr.Value);
					else if (attr.Name == "FileVersion")
						FileVersion = attr.Value;
					else
						Debug.Fail("Unknown attr");
				}
			}

			public string AssemblyFullName {
				get { return string.Format("{0}, Version={1}, Culture={2}, PublicKeyToken={3}", AssemblyName, Version, string.IsNullOrEmpty(Culture) ? "null" : Culture, string.IsNullOrEmpty(PublicKeyToken) ? "null" : PublicKeyToken); }
			}

			public override string ToString() {
				return AssemblyFullName;
			}

			public void Write(XElement elem) {
				elem.SetAttributeValue("name", AssemblyFullName);
			}
		}
	}

	sealed class DefaultFileListFinder {
		readonly CancellationToken cancellationToken;

		public DefaultFileListFinder(CancellationToken cancellationToken) {
			this.cancellationToken = cancellationToken;
		}

		static IEnumerable<string> FilesDirs {
			get {
				const string FILELISTS_DIR = "FileLists";
				yield return Path.Combine(Path.GetDirectoryName(typeof(DefaultFileListFinder).Assembly.Location), FILELISTS_DIR);
				yield return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "dnSpy", FILELISTS_DIR);
			}
		}

		public DefaultFileList[] Find() {
			var lists = new List<DefaultFileList>();

			var finder = new ReferenceFileFinder(cancellationToken);
			finder.Find();

			var xmlFiles = new List<Tuple<DefaultFileList, bool>>();
			foreach (var dir in FilesDirs) {
				cancellationToken.ThrowIfCancellationRequested();
				if (!Directory.Exists(dir))
					continue;

				var files = GetFiles(dir);
				foreach (var f in files) {
					cancellationToken.ThrowIfCancellationRequested();
					var d = ReadDefaultFileList(f);
					if (d != null)
						xmlFiles.Add(d);
				}
			}

			var dict = new Dictionary<string, DefaultFileList>(StringComparer.OrdinalIgnoreCase);

			foreach (var t in xmlFiles) {
				if (t.Item2)	// if non-user file
					dict[t.Item1.Name] = t.Item1;
			}

			foreach (var f in finder.AllFiles)
				dict[f.Name] = f;

			foreach (var t in xmlFiles) {
				if (!t.Item2)	// if user file
					dict[t.Item1.Name] = t.Item1;
			}

			lists.AddRange(dict.Values);
			lists.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
			return lists.ToArray();
		}

		static Tuple<DefaultFileList, bool> ReadDefaultFileList(string filename) {
			try {
				var doc = XDocument.Load(filename, LoadOptions.None);
				var root = doc.Root;
				if (root.Name != "FileList")
					return null;
				var name = (string)root.Attribute("name");
				if (string.IsNullOrWhiteSpace(name))
					return null;
				bool? isDefault = (bool?)root.Attribute("default");
				var l = new DefaultFileList(name);
				foreach (var sect in root.Elements("File")) {
					var asmFullName = (string)sect.Attribute("name");
					if (string.IsNullOrWhiteSpace(asmFullName))
						return null;
					l.Add(asmFullName);
				}
				return Tuple.Create(l, isDefault ?? false);
			}
			catch {
				Debug.Fail("Exception");
			}
			return null;
		}

		static string[] GetFiles(string dir) {
			try {
				return Directory.GetFiles(dir);
			}
			catch {
			}
			return new string[0];
		}
	}
}
