// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dnSpy.Contracts.Decompiler.XmlDoc {
	/// <summary>
	/// Helps finding and loading .xml documentation.
	/// </summary>
	public static class XmlDocLoader {
		static readonly Lazy<XmlDocumentationProvider?> mscorlibDocumentation = new Lazy<XmlDocumentationProvider?>(LoadMscorlibDocumentation);
		static readonly ConditionalWeakTable<object, XmlDocumentationProvider?> cache = new ConditionalWeakTable<object, XmlDocumentationProvider?>();
		static readonly string[] refAsmPathsV4;

		static XmlDocumentationProvider? LoadMscorlibDocumentation() {
			string? xmlDocFile = FindXmlDocumentation("mscorlib.dll", MDHeaderRuntimeVersion.MS_CLR_40)
				?? FindXmlDocumentation("mscorlib.dll", MDHeaderRuntimeVersion.MS_CLR_20);
			if (xmlDocFile is not null)
				return XmlDocumentationProvider.Create(xmlDocFile);
			else
				return null;
		}

		/// <summary>
		/// mscorlib documentation
		/// </summary>
		public static XmlDocumentationProvider? MscorlibDocumentation => mscorlibDocumentation.Value;

		/// <summary>
		/// Loads XML documentation
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns></returns>
		public static XmlDocumentationProvider? LoadDocumentation(ModuleDef module) {
			if (module is null)
				throw new ArgumentNullException(nameof(module));
			return LoadDocumentation(module, module.Location, module.RuntimeVersion);
		}

		/// <summary>
		/// Loads XML documentation
		/// </summary>
		/// <param name="key">Key used to lookup cached documentation, eg. the <see cref="ModuleDef"/> instance</param>
		/// <param name="assemblyFilename">Filename of the assembly or module</param>
		/// <param name="runtimeVersion">Optional runtime version, eg. <see cref="ModuleDef.RuntimeVersion"/></param>
		/// <returns></returns>
		public static XmlDocumentationProvider? LoadDocumentation(object key, string assemblyFilename, string? runtimeVersion = null) {
			if (key is null)
				throw new ArgumentNullException(nameof(key));
			if (assemblyFilename is null)
				throw new ArgumentNullException(nameof(assemblyFilename));
			lock (cache) {
				if (!cache.TryGetValue(key, out var xmlDoc)) {
					string? xmlDocFile = LookupLocalizedXmlDoc(assemblyFilename);
					if (xmlDocFile is null) {
						xmlDocFile = FindXmlDocumentation(Path.GetFileName(assemblyFilename), runtimeVersion);
					}
					xmlDoc = xmlDocFile is null ? null : XmlDocumentationProvider.Create(xmlDocFile);
					cache.Add(key, xmlDoc);
				}
				return xmlDoc;
			}
		}

		static XmlDocLoader() {
			var pfd = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
			if (string.IsNullOrEmpty(pfd))
				pfd = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
			referenceAssembliesPath = Path.Combine(pfd, "Reference Assemblies", "Microsoft", "Framework");
			frameworkPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Microsoft.NET", "Framework");
			refAsmPathsV4 = GetReferenceV4PathsSortedByHighestestVersion();
		}

		static string[] GetReferenceV4PathsSortedByHighestestVersion() {
			var baseDir = Path.Combine(referenceAssembliesPath, ".NETFramework");
			var list = new List<(string dir, Version version)>();
			foreach (var dir in GetDirectories(baseDir)) {
				var s = Path.GetFileName(dir);
				if (!s.StartsWith("v", StringComparison.OrdinalIgnoreCase))
					continue;
				if (!Version.TryParse(s.Substring(1), out var version))
					continue;
				if (version.Major < 4)
					continue;
				list.Add((dir, version));
			}
			return list.OrderByDescending(a => a.version).Select(a => a.dir).ToArray();
		}

		static string[] GetDirectories(string path) {
			if (!Directory.Exists(path))
				return Array.Empty<string>();
			try {
				return Directory.GetDirectories(path);
			}
			catch {
			}
			return Array.Empty<string>();
		}

		static readonly string referenceAssembliesPath;
		static readonly string frameworkPath;

		static string? FindXmlDocumentation(string assemblyFileName, string? runtime) {
			if (string.IsNullOrEmpty(assemblyFileName))
				return null;
			if (runtime is null)
				runtime = MDHeaderRuntimeVersion.MS_CLR_40;
			if (runtime.StartsWith(MDHeaderRuntimeVersion.MS_CLR_10_PREFIX_X86RETAIL) ||
				runtime == MDHeaderRuntimeVersion.MS_CLR_10_RETAIL ||
				runtime == MDHeaderRuntimeVersion.MS_CLR_10_COMPLUS)
				runtime = MDHeaderRuntimeVersion.MS_CLR_10;
			runtime = FixRuntimeString(runtime);

			string? fileName;
			if (runtime.StartsWith(MDHeaderRuntimeVersion.MS_CLR_10_PREFIX))
				fileName = LookupLocalizedXmlDoc(Path.Combine(frameworkPath, runtime, assemblyFileName))
					?? LookupLocalizedXmlDoc(Path.Combine(frameworkPath, "v1.0.3705", assemblyFileName));
			else if (runtime.StartsWith(MDHeaderRuntimeVersion.MS_CLR_11_PREFIX))
				fileName = LookupLocalizedXmlDoc(Path.Combine(frameworkPath, runtime, assemblyFileName))
					?? LookupLocalizedXmlDoc(Path.Combine(frameworkPath, "v1.1.4322", assemblyFileName));
			else if (runtime.StartsWith(MDHeaderRuntimeVersion.MS_CLR_20_PREFIX)) {
				fileName = LookupLocalizedXmlDoc(Path.Combine(frameworkPath, runtime, assemblyFileName))
					?? LookupLocalizedXmlDoc(Path.Combine(frameworkPath, "v2.0.50727", assemblyFileName))
					?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, "v3.5", assemblyFileName))
					?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, "v3.0", assemblyFileName))
					?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, ".NETFramework", "v3.5", "Profile", "Client", assemblyFileName));
			}
			else {  // .NET 4.0
				fileName = null;
				foreach (var path in refAsmPathsV4) {
					fileName = LookupLocalizedXmlDoc(Path.Combine(path, assemblyFileName));
					if (fileName is not null)
						break;
				}
				fileName = fileName
					?? LookupLocalizedXmlDoc(Path.Combine(frameworkPath, runtime, assemblyFileName))
					?? LookupLocalizedXmlDoc(Path.Combine(frameworkPath, "v4.0.30319", assemblyFileName));
			}
			return fileName;
		}

		static readonly List<char> InvalidChars = new List<char>(Path.GetInvalidPathChars()) {
			Path.PathSeparator,
			Path.VolumeSeparatorChar,
			Path.DirectorySeparatorChar,
			Path.AltDirectorySeparatorChar,
		};
		static string FixRuntimeString(string runtime) {
			int min = int.MaxValue;
			foreach (var c in InvalidChars) {
				int index = runtime.IndexOf(c);
				if (index >= 0 && index < min)
					min = index;
			}
			if (min == int.MaxValue)
				return runtime;
			return runtime.Substring(0, min);
		}

		static string? LookupLocalizedXmlDoc(string assemblyFileName) {
			if (string.IsNullOrEmpty(assemblyFileName))
				return null;

			var xmlDocFileCandidates = GetXmlDocFileCandidates(assemblyFileName);
			return xmlDocFileCandidates.FirstOrDefault(File.Exists);
		}

		static IEnumerable<string> GetXmlDocFileCandidates(string assemblyFileName) {
			var xmlFileName = Path.ChangeExtension(assemblyFileName, ".xml");
			yield return GetLocalizedXmlDocFile(xmlFileName, Thread.CurrentThread.CurrentUICulture.Name);
			yield return GetLocalizedXmlDocFile(xmlFileName, Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName);
			yield return xmlFileName;
			yield return GetLocalizedXmlDocFile(xmlFileName, "en");
		}

		static string GetLocalizedXmlDocFile(string xmlFileName, string language) {
			var localizedDirectory = Path.Combine(Path.GetDirectoryName(xmlFileName)!, language);
			var localizedXmlDocFile = Path.Combine(localizedDirectory, Path.GetFileName(xmlFileName));
			return localizedXmlDocFile;
		}
	}
}
