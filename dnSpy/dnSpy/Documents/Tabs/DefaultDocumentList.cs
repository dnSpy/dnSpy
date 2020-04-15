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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Documents;

namespace dnSpy.Documents.Tabs {
	sealed class DefaultDocumentList {
		public string Name { get; }

		public DsDocumentInfo[] Documents => files.ToArray();
		readonly List<DsDocumentInfo> files;

		public DefaultDocumentList(string name) {
			Name = name;
			files = new List<DsDocumentInfo>();
		}

		public DefaultDocumentList(string name, IEnumerable<DsDocumentInfo> asmNames) {
			Name = name;
			files = new List<DsDocumentInfo>(asmNames);
			files.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
		}

		public void Add(DsDocumentInfo file) => files.Add(file);
	}

	sealed class ReferenceFileFinder {
		readonly CancellationToken cancellationToken;
		readonly List<RefFileList> allFiles;

		public ReferenceFileFinder(CancellationToken cancellationToken) {
			this.cancellationToken = cancellationToken;
			allFiles = new List<RefFileList>();
		}

		public IEnumerable<DefaultDocumentList> AllFiles =>
			allFiles.Where(a => a.Files.Count > 0 && !(a.Name is null)).
					Select(a => new DefaultDocumentList(a.Name!, a.Files.Select(b => b.ToDsDocumentInfo()))).
					Where(a => a.Documents.Length > 0);

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

			var pfd = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			if (string.IsNullOrEmpty(pfd))
				pfd = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			Find(pfd);
		}

		static bool IsDNF20Path(string s) => s.EndsWith(@"\Microsoft.NET\Framework\v2.0.50727\RedistList\FrameworkList.xml", StringComparison.OrdinalIgnoreCase) ||
		s.EndsWith(@"\Microsoft.NET\Framework64\v2.0.50727\RedistList\FrameworkList.xml", StringComparison.OrdinalIgnoreCase);

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
				if (!(redistDir is null))
					AddRedistList(redistDir);

				foreach (var d2 in GetDirs(d1)) { // d2 = eg. v4.5.1, etc
					cancellationToken.ThrowIfCancellationRequested();
					dirs = GetDirs(d2);
					redistDir = dirs.FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a, Path.Combine(d2, "RedistList")));
					if (!(redistDir is null))
						AddRedistList(redistDir);

					var profileDir = dirs.FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a, Path.Combine(d2, "Profile")));
					if (!(profileDir is null)) {
						foreach (var d3 in GetDirs(profileDir)) { // d3 = eg. Client
							cancellationToken.ThrowIfCancellationRequested();
							dirs = GetDirs(d3);
							redistDir = dirs.FirstOrDefault(a => StringComparer.OrdinalIgnoreCase.Equals(a, Path.Combine(d3, "RedistList")));
							if (!(redistDir is null))
								AddRedistList(redistDir);
						}
					}
				}
			}

			var net20 = allFiles.FirstOrDefault(a => IsDNF20Path(a.Filename));
			var net30 = allFiles.FirstOrDefault(a => a.Filename.EndsWith(@"\Reference Assemblies\Microsoft\Framework\v3.0\RedistList\FrameworkList.xml", StringComparison.OrdinalIgnoreCase));
			var net35 = allFiles.FirstOrDefault(a => a.Filename.EndsWith(@"\Reference Assemblies\Microsoft\Framework\v3.5\RedistList\FrameworkList.xml", StringComparison.OrdinalIgnoreCase));
			var net35C = allFiles.FirstOrDefault(a => a.Filename.EndsWith(@"\Reference Assemblies\Microsoft\Framework\.NETFramework\v3.5\Profile\Client\RedistList\FrameworkList.xml", StringComparison.OrdinalIgnoreCase));
			var wpa81 = allFiles.FirstOrDefault(a => a.Filename.EndsWith(@"\Reference Assemblies\Microsoft\Framework\WindowsPhoneApp\v8.1\RedistList\FrameworkList.xml", StringComparison.OrdinalIgnoreCase));
			if (!(wpa81 is null))
				wpa81.Name = "Windows Phone App 8.1";	// Another one has the identical name so add "App" to it
			if (!(net30 is null) && !(net20 is null))
				net30.AddFilesFrom(net20);
			if (!(net35 is null) && !(net30 is null))
				net35.AddFilesFrom(net30);
			if (!(net35C is null) && !(net30 is null))
				net35C.AddFilesFrom(net30);
			if (!(net20 is null))
				net20.Name = ".NET Framework 2.0";
			if (!(net30 is null))
				net30.Name = ".NET Framework 3.0";
			if (!(net35 is null))
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
			return Array.Empty<string>();
		}

		sealed class RefFileList {
			public string Filename { get; }
			public string? Redist { get; }
			public string? Name { get; set; }
			public string? RuntimeVersion { get; }
			public string? ToolsVersion { get; }
			public string? ShortName { get; }
			public string? IncludeFramework { get; }
			public string? TargetFrameworkDirectory { get; }
			public string TargetFilename { get; set; }
			public List<RefFile> Files { get; } = new List<RefFile>();

			public RefFileList(string filename) {
				Filename = filename;
				var refFilePath = Path.GetDirectoryName(Path.GetDirectoryName(filename));
				var doc = XDocument.Load(filename, LoadOptions.None);
				var root = doc.Root;
				if (root.Name != "FileList")
					throw new InvalidOperationException();
				foreach (var attr in root.Attributes()) {
					switch (attr.Name.ToString()) {
					case "Redist":
						Redist = attr.Value;
						break;
					case "Name":
						Name = attr.Value;
						break;
					case "RuntimeVersion":
						RuntimeVersion = attr.Value;
						break;
					case "ToolsVersion":
						ToolsVersion = attr.Value;
						break;
					case "ShortName":
						ShortName = attr.Value;
						break;
					case "IncludeFramework":
						IncludeFramework = attr.Value;
						break;
					case "TargetFrameworkDirectory":
						TargetFrameworkDirectory = attr.Value;
						break;
					default:
						Debug.Fail("Unknown attr");
						break;
					}
				}

				foreach (var sect in root.Elements()) {
					if (sect.Name == "File") {
						var file = new RefFile(sect, refFilePath);
						if (!string.IsNullOrEmpty(file.Filename))
							Files.Add(file);
					}
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
				if (IsDNF20Path(f))
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

			public override string ToString() => $"{Filename} - {Redist} - {Name}";

			public void AddFilesFrom(RefFileList olderList) {
				if (olderList is null)
					return;
				var existing = new HashSet<string?>(Files.Select(a => a.AssemblyName), StringComparer.OrdinalIgnoreCase);
				Files.AddRange(olderList.Files.Where(a => !existing.Contains(a.AssemblyName)));
			}
		}

		sealed class RefFile {
			public string? AssemblyName { get; }
			public Version? Version { get; }
			public string? PublicKeyToken { get; }
			public string? Culture { get; }
			public string? ProcessorArchitecture { get; }
			public bool InGac { get; }
			public bool IsRedistRoot { get; }
			public string? FileVersion { get; }
			public string Filename { get; }

			public RefFile(XElement sect, string? refFilePath) {
				foreach (var attr in sect.Attributes()) {
					switch (attr.Name.ToString()) {
					case "AssemblyName":
						AssemblyName = attr.Value;
						break;
					case "Version":
						Version = new Version(attr.Value);
						break;
					case "PublicKeyToken":
						PublicKeyToken = attr.Value;
						break;
					case "Culture":
						Culture = attr.Value;
						break;
					case "ProcessorArchitecture":
						ProcessorArchitecture = attr.Value;
						break;
					case "InGac":
					case "InGAC":
						InGac = bool.Parse(attr.Value);
						break;
					case "IsRedistRoot":
						IsRedistRoot = bool.Parse(attr.Value);
						break;
					case "FileVersion":
						FileVersion = attr.Value;
						break;
					default:
						Debug.Fail("Unknown attr");
						break;
					}
				}

				string fn;
				try {
					var f = Path.Combine(refFilePath!, AssemblyName!);
					fn = f + ".dll";
					if (!File.Exists(fn))
						fn = f + ".exe";
					if (!File.Exists(fn))
						fn = string.Empty;
				}
				catch (ArgumentException) {
					fn = string.Empty;
				}
				Filename = fn;
			}

			public DsDocumentInfo ToDsDocumentInfo() {
				if (string.IsNullOrEmpty(Filename))
					return DsDocumentInfo.CreateGacDocument(AssemblyFullName);
				return DsDocumentInfo.CreateReferenceAssembly(AssemblyFullName, Filename);
			}

			public string AssemblyFullName =>
				$"{AssemblyName}, Version={Version}, Culture={(string.IsNullOrEmpty(Culture) ? "null" : Culture)}, PublicKeyToken={(string.IsNullOrEmpty(PublicKeyToken) ? "null" : PublicKeyToken)}";

			public override string ToString() => AssemblyFullName;
			public void Write(XElement elem) => elem.SetAttributeValue("name", AssemblyFullName);
		}
	}

	sealed class DefaultDocumentListFinder {
		readonly CancellationToken cancellationToken;

		public DefaultDocumentListFinder(CancellationToken cancellationToken) => this.cancellationToken = cancellationToken;

		static IEnumerable<string> FilesDirs => AppDirectories.GetDirectories("FileLists");

		public DefaultDocumentList[] Find() {
			var lists = new List<DefaultDocumentList>();

			var finder = new ReferenceFileFinder(cancellationToken);
			finder.Find();

			var xmlFiles = new List<(DefaultDocumentList list, bool isDefault)>();
			foreach (var dir in FilesDirs) {
				cancellationToken.ThrowIfCancellationRequested();
				if (!Directory.Exists(dir))
					continue;

				var files = GetFiles(dir);
				foreach (var f in files) {
					cancellationToken.ThrowIfCancellationRequested();
					var d = ReadDefaultFileList(f);
					if (!(d is null))
						xmlFiles.Add(d.Value);
				}
			}

			var dict = new Dictionary<string, DefaultDocumentList>(StringComparer.OrdinalIgnoreCase);

			foreach (var t in xmlFiles) {
				if (t.isDefault)	// if non-user file
					dict[t.list.Name] = t.list;
			}

			foreach (var f in finder.AllFiles)
				dict[f.Name] = f;

			foreach (var t in xmlFiles) {
				if (!t.isDefault)	// if user file
					dict[t.list.Name] = t.list;
			}

			lists.AddRange(dict.Values);
			lists.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
			return lists.ToArray();
		}

		static (DefaultDocumentList list, bool isDefault)? ReadDefaultFileList(string filename) {
			try {
				var doc = XDocument.Load(filename, LoadOptions.None);
				var root = doc.Root;
				if (root.Name != "FileList")
					return null;
				var name = (string)root.Attribute("name");
				if (string.IsNullOrWhiteSpace(name))
					return null;
				bool? isDefault = (bool?)root.Attribute("default");
				var l = new DefaultDocumentList(name);
				foreach (var sect in root.Elements("File")) {
					var name2 = (string)sect.Attribute("name");
					if (string.IsNullOrWhiteSpace(name2))
						return null;
					var type = (string)sect.Attribute("type") ?? "gac";
					var guidStr = (string)sect.Attribute("guid");
					Guid guid = Guid.Empty;
					bool hasGuid = !(guidStr is null) && Guid.TryParse(guidStr, out guid);
					if (type.Equals("file"))
						l.Add(DsDocumentInfo.CreateDocument(name2));
					else if (type.Equals("refasm"))
						l.Add(new DsDocumentInfo(name2, DocumentConstants.DOCUMENTTYPE_REFASM));
					else if (type.Equals("user-file") && hasGuid)
						l.Add(new DsDocumentInfo(name2, guid));
					else // should be "gac"
						l.Add(DsDocumentInfo.CreateGacDocument(name2));
				}
				return (l, isDefault ?? false);
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
			return Array.Empty<string>();
		}
	}
}
