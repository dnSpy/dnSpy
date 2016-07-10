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
using dnSpy.Contracts.Utilities;

namespace dnSpy.Contracts.Files {
	/// <summary>
	/// File base class
	/// </summary>
	public abstract class DnSpyFile : IDnSpyFile {
		/// <inheritdoc/>
		public abstract DnSpyFileInfo? SerializedFile { get; }
		/// <inheritdoc/>
		public abstract IDnSpyFilenameKey Key { get; }
		/// <inheritdoc/>
		public AssemblyDef AssemblyDef => ModuleDef?.Assembly;
		/// <inheritdoc/>
		public virtual ModuleDef ModuleDef => null;
		/// <inheritdoc/>
		public virtual IPEImage PEImage => (ModuleDef as ModuleDefMD)?.MetaData?.PEImage;

		/// <inheritdoc/>
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

		/// <summary>
		/// Gets called when a property has changed
		/// </summary>
		/// <param name="propName">Name of property</param>
		protected virtual void OnPropertyChanged(string propName) {
		}

		/// <inheritdoc/>
		public bool IsAutoLoaded { get; set; }

		/// <inheritdoc/>
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

		/// <inheritdoc/>
		public bool ChildrenLoaded => children != null;

		/// <summary>
		/// Creates the children
		/// </summary>
		/// <returns></returns>
		protected virtual List<IDnSpyFile> CreateChildren() => new List<IDnSpyFile>();

		/// <summary>
		/// Constructor
		/// </summary>
		protected DnSpyFile() {
		}

		/// <inheritdoc/>
		public T AddAnnotation<T>(T annotation) where T : class => annotations.AddAnnotation(annotation);
		/// <inheritdoc/>
		public T Annotation<T>() where T : class => annotations.Annotation<T>();
		/// <inheritdoc/>
		public IEnumerable<T> Annotations<T>() where T : class => annotations.Annotations<T>();
		/// <inheritdoc/>
		public void RemoveAnnotations<T>() where T : class => annotations.RemoveAnnotations<T>();
		readonly AnnotationsImpl annotations = new AnnotationsImpl();

		/// <summary>
		/// Creates a <see cref="IDnSpyFile"/>
		/// </summary>
		/// <param name="fileInfo">File info</param>
		/// <param name="filename">Filename</param>
		/// <param name="useMemoryMappedIO">true to use memory mapped I/O</param>
		/// <param name="loadPDBFile">true to load the PDB file if available</param>
		/// <param name="asmResolver">Assembly resolver</param>
		/// <param name="isModule">true if it's a module, false if it's an assembly</param>
		/// <returns></returns>
		public static IDnSpyFile CreateDnSpyFileFromFile(DnSpyFileInfo fileInfo, string filename, bool useMemoryMappedIO, bool loadPDBFile, IAssemblyResolver asmResolver, bool isModule) {
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
							return DnSpyDotNetFile.CreateModule(fileInfo, ModuleDefMD.Load(peImage, options), loadPDBFile);
						return DnSpyDotNetFile.CreateAssembly(fileInfo, ModuleDefMD.Load(peImage, options), loadPDBFile);
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

	/// <summary>
	/// Unknown type of file
	/// </summary>
	public sealed class DnSpyUnknownFile : DnSpyFile {
		/// <inheritdoc/>
		public override DnSpyFileInfo? SerializedFile => DnSpyFileInfo.CreateFile(Filename);

		/// <inheritdoc/>
		public override IDnSpyFilenameKey Key => new FilenameKey(Filename);

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="filename">Filename</param>
		public DnSpyUnknownFile(string filename) {
			Filename = filename ?? string.Empty;
		}
	}

	/// <summary>
	/// PE file
	/// </summary>
	public sealed class DnSpyPEFile : DnSpyFile, IDnSpyPEFile, IDisposable {
		/// <inheritdoc/>
		public override DnSpyFileInfo? SerializedFile => DnSpyFileInfo.CreateFile(Filename);
		/// <inheritdoc/>
		public override IDnSpyFilenameKey Key => new FilenameKey(Filename);
		/// <inheritdoc/>
		public override IPEImage PEImage { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="peImage">PE image</param>
		public DnSpyPEFile(IPEImage peImage) {
			this.PEImage = peImage;
			Filename = peImage.FileName ?? string.Empty;
		}

		/// <inheritdoc/>
		public void Dispose() => PEImage.Dispose();
	}

	/// <summary>
	/// .NET file base class
	/// </summary>
	public abstract class DnSpyDotNetFileBase : DnSpyFile, IDnSpyDotNetFile {
		/// <inheritdoc/>
		public override ModuleDef ModuleDef { get; }

		/// <summary>true if the symbols have been loaded</summary>
		protected bool loadedSymbols;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="loadSyms">true if symbols should be loaded</param>
		protected DnSpyDotNetFileBase(ModuleDef module, bool loadSyms) {
			this.ModuleDef = module;
			this.loadedSymbols = loadSyms;
			Filename = module.Location ?? string.Empty;
			module.EnableTypeDefFindCache = true;
			if (loadSyms)
				LoadSymbols(module.Location);
		}

		/// <summary>
		/// Creates a module context
		/// </summary>
		/// <param name="asmResolver">Assembly resolver</param>
		/// <returns></returns>
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

	/// <summary>
	/// .NET file
	/// </summary>
	public class DnSpyDotNetFile : DnSpyDotNetFileBase, IDisposable {
		readonly bool isAsmNode;

		/// <inheritdoc/>
		public override IDnSpyFilenameKey Key => new FilenameKey(Filename);
		/// <inheritdoc/>
		public override DnSpyFileInfo? SerializedFile => fileInfo;
		DnSpyFileInfo fileInfo;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="fileInfo">File info</param>
		/// <param name="module">Module</param>
		/// <param name="loadSyms">true to load symbols</param>
		/// <param name="isAsmNode">true if it's an assembly node, false if it's a module node</param>
		protected DnSpyDotNetFile(DnSpyFileInfo fileInfo, ModuleDef module, bool loadSyms, bool isAsmNode)
			: base(module, loadSyms) {
			this.fileInfo = fileInfo;
			this.isAsmNode = isAsmNode;
		}

		/// <inheritdoc/>
		protected override void OnPropertyChanged(string propName) {
			base.OnPropertyChanged(propName);
			if (propName == nameof(Filename))
				fileInfo = DnSpyFileInfo.CreateFile(Filename);
		}

		/// <summary>
		/// Creates an assembly
		/// </summary>
		/// <param name="fileInfo">File info</param>
		/// <param name="module">Module</param>
		/// <param name="loadSyms">true to load symbols</param>
		/// <returns></returns>
		public static DnSpyDotNetFile CreateAssembly(DnSpyFileInfo fileInfo, ModuleDef module, bool loadSyms) => new DnSpyDotNetFile(fileInfo, module, loadSyms, true);

		/// <summary>
		/// Creates a module
		/// </summary>
		/// <param name="fileInfo">File info</param>
		/// <param name="module">Module</param>
		/// <param name="loadSyms">true to load symbols</param>
		/// <returns></returns>
		public static DnSpyDotNetFile CreateModule(DnSpyFileInfo fileInfo, ModuleDef module, bool loadSyms) => new DnSpyDotNetFile(fileInfo, module, loadSyms, false);

		/// <summary>
		/// Creates an assembly
		/// </summary>
		/// <param name="modFile">Module</param>
		/// <returns></returns>
		public static DnSpyDotNetFile CreateAssembly(IDnSpyDotNetFile modFile) => new DnSpyDotNetFileAsmWithMod(modFile);

		/// <inheritdoc/>
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

		/// <inheritdoc/>
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

	/// <summary>
	/// mmap'd I/O helper methods
	/// </summary>
	public static class MemoryMappedIOHelper {
		/// <summary>
		/// Disable memory mapped I/O
		/// </summary>
		/// <param name="file">File</param>
		public static void DisableMemoryMappedIO(IDnSpyFile file) {
			if (file == null)
				return;
			DisableMemoryMappedIO(file.PEImage);
		}

		/// <summary>
		/// Disable memory mapped I/O
		/// </summary>
		/// <param name="peImage">PE image</param>
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
