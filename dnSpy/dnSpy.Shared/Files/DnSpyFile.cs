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
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Files;

namespace dnSpy.Shared.Files {
	public abstract class DnSpyFile : IDnSpyFile {
		public abstract DnSpyFileInfo? SerializedFile { get; }
		public abstract IDnSpyFilenameKey Key { get; }
		public AssemblyDef AssemblyDef => ModuleDef?.Assembly;
		public virtual ModuleDef ModuleDef => null;
		public virtual IPEImage PEImage => (ModuleDef as ModuleDefMD)?.MetaData?.PEImage;

		public string Filename {
			get { return filename; }
			set {
				if (this.filename != value) {
					this.filename = value;
					OnPropertyChanged(nameof(Filename));
				}
			}
		}
		string filename;

		protected virtual void OnPropertyChanged(string propName) {
		}

		public bool IsAutoLoaded { get; set; }

		public List<IDnSpyFile> Children {
			get {
				if (children == null) {
					children = CreateChildren();
					Debug.Assert(children != null);
					if (children == null)
						children = new List<IDnSpyFile>();
				}
				return children;
			}
		}
		List<IDnSpyFile> children;

		public bool ChildrenLoaded => children != null;
		protected virtual List<IDnSpyFile> CreateChildren() => new List<IDnSpyFile>();

		protected DnSpyFile() {
		}

		public T AddAnnotation<T>(T annotation) where T : class => annotations.AddAnnotation(annotation);
		public T Annotation<T>() where T : class => annotations.Annotation<T>();
		public IEnumerable<T> Annotations<T>() where T : class => annotations.Annotations<T>();
		public void RemoveAnnotations<T>() where T : class => annotations.RemoveAnnotations<T>();
		readonly AnnotationsImpl annotations = new AnnotationsImpl();

		public static IDnSpyFile CreateDnSpyFileFromFile(DnSpyFileInfo fileInfo, string filename, bool useMemoryMappedIO, bool loadPDBFiles, IAssemblyResolver asmResolver, bool isModule) {
			try {
				// Quick check to prevent exceptions from being thrown
				if (!File.Exists(filename))
					return new DnSpyUnknownFile(filename);

				IPEImage peImage;

				if (useMemoryMappedIO)
					peImage = new PEImage(filename);
				else
					peImage = new PEImage(File.ReadAllBytes(filename), filename);

				var dotNetDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
				bool isDotNet = dotNetDir.VirtualAddress != 0 && dotNetDir.Size >= 0x48;
				if (isDotNet) {
					try {
						var options = new ModuleCreationOptions(DnSpyDotNetFileBase.CreateModuleContext(asmResolver));
						if (isModule)
							return DnSpyDotNetFile.CreateModule(fileInfo, ModuleDefMD.Load(peImage, options), loadPDBFiles);
						return DnSpyDotNetFile.CreateAssembly(fileInfo, ModuleDefMD.Load(peImage, options), loadPDBFiles);
					}
					catch {
					}
				}

				return new DnSpyPEFile(peImage);
			}
			catch {
			}

			return new DnSpyUnknownFile(filename);
		}
	}

	public sealed class DnSpyUnknownFile : DnSpyFile {
		public override DnSpyFileInfo? SerializedFile => DnSpyFileInfo.CreateFile(Filename);

		public override IDnSpyFilenameKey Key => new FilenameKey(Filename);

		public DnSpyUnknownFile(string filename) {
			Filename = filename ?? string.Empty;
		}
	}

	public sealed class DnSpyPEFile : DnSpyFile, IDnSpyPEFile, IDisposable {
		public override DnSpyFileInfo? SerializedFile => DnSpyFileInfo.CreateFile(Filename);
		public override IDnSpyFilenameKey Key => new FilenameKey(Filename);
		public override IPEImage PEImage { get; }

		public DnSpyPEFile(IPEImage peImage) {
			this.PEImage = peImage;
			Filename = peImage.FileName ?? string.Empty;
		}

		public void Dispose() => PEImage.Dispose();
	}

	public abstract class DnSpyDotNetFileBase : DnSpyFile, IDnSpyDotNetFile {
		public override ModuleDef ModuleDef { get; }

		protected bool loadedSymbols;

		protected DnSpyDotNetFileBase(ModuleDef module, bool loadSyms) {
			this.ModuleDef = module;
			this.loadedSymbols = loadSyms;
			Filename = module.Location ?? string.Empty;
			module.EnableTypeDefFindCache = true;
			if (loadSyms)
				LoadSymbols(module.Location);
		}

		public static ModuleContext CreateModuleContext(IAssemblyResolver asmResolver) {
			ModuleContext moduleCtx = new ModuleContext();
			moduleCtx.AssemblyResolver = asmResolver;
			// Disable WinMD projection since the user probably expects that clicking on a type
			// will take you to that type, and not to the projected CLR type.
			// The decompiler shouldn't have a problem with this since it uses SigComparer() which
			// defaults to projecting WinMD types.
			moduleCtx.Resolver = new Resolver(moduleCtx.AssemblyResolver) { ProjectWinMDRefs = false };
			return moduleCtx;
		}

		void LoadSymbols(string dotNetFilename) {
			if (!File.Exists(dotNetFilename))
				return;
			// Happens if a module has been removed but then the exact same instance
			// was re-added.
			if (ModuleDef.PdbState != null)
				return;

			var m = ModuleDef as ModuleDefMD;
			if (m == null)
				return;
			try {
				var pdbFilename = Path.Combine(Path.GetDirectoryName(dotNetFilename), Path.GetFileNameWithoutExtension(dotNetFilename) + ".pdb");
				if (File.Exists(pdbFilename))
					m.LoadPdb(pdbFilename);
			}
			catch {
			}
		}
	}

	public class DnSpyDotNetFile : DnSpyDotNetFileBase, IDisposable {
		readonly bool isAsmNode;

		public override IDnSpyFilenameKey Key => new FilenameKey(Filename);
		public override DnSpyFileInfo? SerializedFile => fileInfo;
		DnSpyFileInfo fileInfo;

		protected DnSpyDotNetFile(DnSpyFileInfo fileInfo, ModuleDef module, bool loadSyms, bool isAsmNode)
			: base(module, loadSyms) {
			this.fileInfo = fileInfo;
			this.isAsmNode = isAsmNode;
		}

		protected override void OnPropertyChanged(string propName) {
			base.OnPropertyChanged(propName);
			if (propName == nameof(Filename))
				fileInfo = DnSpyFileInfo.CreateFile(Filename);
		}

		public static DnSpyDotNetFile CreateAssembly(DnSpyFileInfo fileInfo, ModuleDef module, bool loadSyms) => new DnSpyDotNetFile(fileInfo, module, loadSyms, true);
		public static DnSpyDotNetFile CreateModule(DnSpyFileInfo fileInfo, ModuleDef module, bool loadSyms) => new DnSpyDotNetFile(fileInfo, module, loadSyms, false);
		public static DnSpyDotNetFile CreateAssembly(IDnSpyDotNetFile modFile) => new DnSpyDotNetFileAsmWithMod(modFile);

		protected override List<IDnSpyFile> CreateChildren() {
			var asm = AssemblyDef;
			var list = new List<IDnSpyFile>(asm == null ? 1 : asm.Modules.Count);
			if (isAsmNode && asm != null) {
				bool foundThis = false;
				foreach (var module in asm.Modules) {
					if (this.ModuleDef == module) {
						Debug.Assert(!foundThis);
						foundThis = true;
					}
					list.Add(new DnSpyDotNetFile(DnSpyFileInfo.CreateFile(module.Location), module, loadedSymbols, false));
				}
				Debug.Assert(foundThis);
			}
			return list;
		}

		public void Dispose() => ModuleDef.Dispose();
	}

	sealed class DnSpyDotNetFileAsmWithMod : DnSpyDotNetFile {
		IDnSpyDotNetFile modFile;

		public DnSpyDotNetFileAsmWithMod(IDnSpyDotNetFile modFile)
			: base(modFile.SerializedFile ?? new DnSpyFileInfo(), modFile.ModuleDef, false, true) {
			this.modFile = modFile;
		}

		protected override List<IDnSpyFile> CreateChildren() {
			Debug.Assert(this.modFile != null);
			var list = new List<IDnSpyFile>();
			if (this.modFile != null)
				list.Add(this.modFile);
			this.modFile = null;
			return list;
		}
	}

	public static class MemoryMappedIOHelper {
		public static void DisableMemoryMappedIO(IDnSpyFile file) {
			if (file == null)
				return;
			DisableMemoryMappedIO(file.PEImage);
		}

		public static void DisableMemoryMappedIO(IPEImage peImage) {
			if (peImage == null)
				return;
			// Files in the GAC are read-only so there's no need to disable memory mapped I/O to
			// allow other programs to write to the file.
			if (GacInfo.IsGacPath(peImage.FileName))
				return;
			peImage.UnsafeDisableMemoryMappedIO();
		}
	}
}
