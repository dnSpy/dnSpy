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
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Writer;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ConvertToNetstandardReferences {
	public sealed class ConvertToNetstandardReferences : Task {
		// Increment it if something changes so the files are re-created
		const string VERSION = "cnsrefs_v1";

		[Required]
		public string DestinationDirectory { get; set; }

		[Required]
		public ITaskItem[] ReferencePath { get; set; }

		[Output]
		public ITaskItem[] OutputReferencePath { get; private set; }

		bool ShouldPatchAssembly(string simpleName) {
			if (simpleName.StartsWith("Microsoft.VisualStudio."))
				return true;

			return false;
		}

		static bool IsPublic(TypeDef type) {
			while (type != null) {
				if (!type.IsPublic && !type.IsNestedPublic)
					return false;
				type = type.DeclaringType;
			}
			return true;
		}

		static bool IsPublic(ExportedType type) {
			while (type != null) {
				if (!type.IsPublic && !type.IsNestedPublic)
					return false;
				type = type.DeclaringType;
			}
			return true;
		}

		public override bool Execute() {
			if (string.IsNullOrWhiteSpace(DestinationDirectory)) {
				Log.LogMessageFromText(nameof(DestinationDirectory) + " is an empty string", MessageImportance.High);
				return false;
			}

			using (var assemblyFactory = new AssemblyFactory(ReferencePath.Select(a => a.ItemSpec))) {
				AssemblyRef netstandardAsmRef = null;
				AssemblyDef netstandardAsm = null;
				var typeComparer = new TypeEqualityComparer(SigComparerOptions.DontCompareTypeScope);
				var netstandardTypes = new HashSet<IType>(typeComparer);
				OutputReferencePath = new ITaskItem[ReferencePath.Length];
				for (int i = 0; i < ReferencePath.Length; i++) {
					var file = ReferencePath[i];
					OutputReferencePath[i] = file;
					var filename = file.ItemSpec;
					var fileExt = Path.GetExtension(filename);
					var asmSimpleName = Path.GetFileNameWithoutExtension(filename);
					if (!ShouldPatchAssembly(asmSimpleName))
						continue;
					if (!File.Exists(filename)) {
						Log.LogMessageFromText($"File does not exist: {filename}", MessageImportance.High);
						return false;
					}

					var patchDir = DestinationDirectory;
					Directory.CreateDirectory(patchDir);

					var fileInfo = new FileInfo(filename);
					long filesize = fileInfo.Length;
					long writeTime = fileInfo.LastWriteTimeUtc.ToBinary();

					var extraInfo = $"_{VERSION} {filesize} {writeTime}_";
					var patchedFilename = Path.Combine(patchDir, asmSimpleName + extraInfo + fileExt);
					if (StringComparer.OrdinalIgnoreCase.Equals(patchedFilename, filename))
						continue;

					if (!File.Exists(patchedFilename)) {
						var asm = assemblyFactory.Resolve(asmSimpleName);
						if (asm == null)
							throw new Exception($"Couldn't resolve assembly {filename}");
						var mod = (ModuleDefMD)asm.ManifestModule;
						if (!ShouldPatchAssembly(mod))
							continue;

						if (netstandardAsm == null) {
							netstandardAsm = assemblyFactory.Resolve("netstandard");
							if (netstandardAsm == null)
								throw new Exception("Couldn't find a netstandard file");
							netstandardAsmRef = netstandardAsm.ToAssemblyRef();
							foreach (var type in netstandardAsm.ManifestModule.GetTypes()) {
								if (type.IsGlobalModuleType)
									continue;
								if (IsPublic(type))
									netstandardTypes.Add(type);
							}
							foreach (var type in netstandardAsm.ManifestModule.ExportedTypes)
								netstandardTypes.Add(type);
						}

						for (uint rid = 1; ; rid++) {
							var tr = mod.ResolveTypeRef(rid);
							if (tr == null)
								break;
							if (!netstandardTypes.Contains(tr))
								continue;
							if (tr.ResolutionScope is AssemblyRef asmRef && CanReplaceAssemblyRef(asmRef))
								tr.ResolutionScope = netstandardAsmRef;
						}

						var options = new ModuleWriterOptions(mod);
						mod.Write(patchedFilename, options);

						var xmlDocFile = Path.ChangeExtension(filename, "xml");
						if (File.Exists(xmlDocFile)) {
							var newXmlDocFile = Path.ChangeExtension(patchedFilename, "xml");
							if (File.Exists(newXmlDocFile))
								File.Delete(newXmlDocFile);
							File.Copy(xmlDocFile, newXmlDocFile);
						}
					}

					OutputReferencePath[i] = new TaskItem(patchedFilename);
				}

				return true;
			}
		}

		static bool CanReplaceAssemblyRef(AssemblyRef asmRef) => true;

		static bool ShouldPatchAssembly(ModuleDefMD mod) {
			foreach (var asmRef in mod.GetAssemblyRefs()) {
				string name = asmRef.Name;
				if (name == "netstandard")
					return false;
			}
			return true;
		}
	}

	sealed class AssemblyFactory : IAssemblyResolver, IDisposable {
		readonly Dictionary<string, ModuleDef> modules;
		readonly Dictionary<string, string> nameToPath;
		readonly ModuleContext context;

		public AssemblyFactory(IEnumerable<string> filenames) {
			modules = new Dictionary<string, ModuleDef>(StringComparer.Ordinal);
			nameToPath = filenames.ToDictionary(key => Path.GetFileNameWithoutExtension(key), value => value, StringComparer.Ordinal);
			context = new ModuleContext(this, new Resolver(this));
		}

		AssemblyDef IAssemblyResolver.Resolve(IAssembly assembly, ModuleDef sourceModule) =>
			Resolve(assembly.Name);

		public AssemblyDef Resolve(string name) {
			if (modules.TryGetValue(name, out var module))
				return module.Assembly;
			if (!nameToPath.TryGetValue(name, out var path))
				return null;
			var options = new ModuleCreationOptions(context);
			options.TryToLoadPdbFromDisk = false;
			module = ModuleDefMD.Load(path, options);
			modules.Add(name, module);
			return module.Assembly ?? throw new InvalidOperationException("It's a netmodule");
		}

		public void Dispose() {
			foreach (var module in modules)
				module.Value.Dispose();
		}
	}
}
