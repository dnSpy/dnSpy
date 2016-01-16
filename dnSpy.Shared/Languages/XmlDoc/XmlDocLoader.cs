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
using System.Runtime.CompilerServices;
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dnSpy.Shared.Languages.XmlDoc {
	/// <summary>
	/// Helps finding and loading .xml documentation.
	/// </summary>
	public static class XmlDocLoader {
		static readonly Lazy<XmlDocumentationProvider> mscorlibDocumentation = new Lazy<XmlDocumentationProvider>(LoadMscorlibDocumentation);
		static readonly ConditionalWeakTable<ModuleDef, XmlDocumentationProvider> cache = new ConditionalWeakTable<ModuleDef, XmlDocumentationProvider>();

		static XmlDocumentationProvider LoadMscorlibDocumentation() {
			string xmlDocFile = FindXmlDocumentation("mscorlib.dll", MDHeaderRuntimeVersion.MS_CLR_40)
				?? FindXmlDocumentation("mscorlib.dll", MDHeaderRuntimeVersion.MS_CLR_20);
			if (xmlDocFile != null)
				return XmlDocumentationProvider.Create(xmlDocFile);
			else
				return null;
		}

		public static XmlDocumentationProvider MscorlibDocumentation {
			get { return mscorlibDocumentation.Value; }
		}

		public static XmlDocumentationProvider LoadDocumentation(ModuleDef module) {
			if (module == null)
				throw new ArgumentNullException("module");
			lock (cache) {
				XmlDocumentationProvider xmlDoc;
				if (!cache.TryGetValue(module, out xmlDoc)) {
					string xmlDocFile = LookupLocalizedXmlDoc(module.Location);
					if (xmlDocFile == null) {
						xmlDocFile = FindXmlDocumentation(Path.GetFileName(module.Location), module.RuntimeVersion);
					}
					xmlDoc = xmlDocFile == null ? null : XmlDocumentationProvider.Create(xmlDocFile);
					cache.Add(module, xmlDoc);
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
		}

		static readonly string referenceAssembliesPath;
		static readonly string frameworkPath;

		static string FindXmlDocumentation(string assemblyFileName, string runtime) {
			if (string.IsNullOrEmpty(assemblyFileName))
				return null;
			if (runtime.StartsWith(MDHeaderRuntimeVersion.MS_CLR_10_PREFIX_X86RETAIL) ||
				runtime == MDHeaderRuntimeVersion.MS_CLR_10_RETAIL ||
				runtime == MDHeaderRuntimeVersion.MS_CLR_10_COMPLUS)
				runtime = MDHeaderRuntimeVersion.MS_CLR_10;
			runtime = FixRuntimeString(runtime);

			string fileName;
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
				fileName = LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, ".NETFramework", "v4.5.1", assemblyFileName))
					?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, ".NETFramework", "v4.5", assemblyFileName))
					?? LookupLocalizedXmlDoc(Path.Combine(referenceAssembliesPath, ".NETFramework", "v4.0", assemblyFileName))
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

		static string LookupLocalizedXmlDoc(string fileName) {
			if (string.IsNullOrEmpty(fileName))
				return null;

			string xmlFileName = Path.ChangeExtension(fileName, ".xml");
			string currentCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
			string localizedXmlDocFile = GetLocalizedName(xmlFileName, currentCulture);

			//Debug.WriteLine("Try find XMLDoc @" + localizedXmlDocFile);
			if (File.Exists(localizedXmlDocFile)) {
				return localizedXmlDocFile;
			}
			//Debug.WriteLine("Try find XMLDoc @" + xmlFileName);
			if (File.Exists(xmlFileName)) {
				return xmlFileName;
			}
			if (currentCulture != "en") {
				string englishXmlDocFile = GetLocalizedName(xmlFileName, "en");
				//Debug.WriteLine("Try find XMLDoc @" + englishXmlDocFile);
				if (File.Exists(englishXmlDocFile)) {
					return englishXmlDocFile;
				}
			}
			return null;
		}

		static string GetLocalizedName(string fileName, string language) {
			string localizedXmlDocFile = Path.GetDirectoryName(fileName);
			localizedXmlDocFile = Path.Combine(localizedXmlDocFile, language);
			localizedXmlDocFile = Path.Combine(localizedXmlDocFile, Path.GetFileName(fileName));
			return localizedXmlDocFile;
		}
	}
}
