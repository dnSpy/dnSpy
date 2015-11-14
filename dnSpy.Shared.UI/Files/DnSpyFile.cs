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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Files;

namespace dnSpy.Shared.UI.Files {
	public abstract class DnSpyFile : IDnSpyFile {
		public virtual IDnSpyFilenameKey Key {
			get { return new FilenameKey(Filename); }
		}

		public virtual SerializedDnSpyModule? SerializedDnSpyModule {
			get {
				var mod = ModuleDef;
				return mod == null ? (SerializedDnSpyModule?)null : Contracts.Files.SerializedDnSpyModule.CreateFromFile(mod);
			}
		}

		public AssemblyDef AssemblyDef {
			get { var m = ModuleDef; return m == null ? null : m.Assembly; }
		}

		public virtual ModuleDef ModuleDef {
			get { return null; }
		}

		public virtual IPEImage PEImage {
			get {
				var m = ModuleDef as ModuleDefMD;
				return m == null ? null : m.MetaData.PEImage;
			}
		}

		public string Filename {
			get { return filename; }
			set { this.filename = value; }
		}
		string filename;

		public bool IsAutoLoaded { get; set; }

		public virtual bool LoadedFromFile {
			get { return true; }
		}

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

		protected virtual List<IDnSpyFile> CreateChildren() {
			return new List<IDnSpyFile>();
		}

		protected DnSpyFile() {
		}

		public T AddAnnotation<T>(T annotation) where T : class {
			return annotations.AddAnnotation(annotation);
		}

		public T Annotation<T>() where T : class {
			return annotations.Annotation<T>();
		}

		public IEnumerable<T> Annotations<T>() where T : class {
			return annotations.Annotations<T>();
		}

		public void RemoveAnnotations<T>() where T : class {
			annotations.RemoveAnnotations<T>();
		}
		readonly AnnotationsImpl annotations = new AnnotationsImpl();
	}

	public sealed class DnSpyUnknownFile : DnSpyFile {
		public DnSpyUnknownFile(string filename) {
			Filename = filename;
		}
	}

	public sealed class DnSpyPEFile : DnSpyFile, IDnSpyPEFile, IDisposable {
		public override IPEImage PEImage {
			get { return peImage; }
		}
		readonly IPEImage peImage;

		public DnSpyPEFile(IPEImage peImage) {
			this.peImage = peImage;
			Filename = peImage.FileName ?? string.Empty;
		}

		public void Dispose() {
			peImage.Dispose();
		}
	}

	public abstract class DnSpyDotNetFileBase : DnSpyFile, IDnSpyDotNetFile {
		public override ModuleDef ModuleDef {
			get { return module; }
		}
		protected readonly ModuleDef module;

		protected bool loadedSymbols;

		protected DnSpyDotNetFileBase(ModuleDef module, bool loadSyms) {
			this.module = module;
			this.loadedSymbols = loadSyms;
			Filename = module.Location;
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
			if (module.PdbState != null)
				return;

			//TODO: Support CorModuleDef too
			var m = module as ModuleDefMD;
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

	public sealed class DnSpyDotNetFile : DnSpyDotNetFileBase, IDisposable {
		readonly bool isAsmNode;

		DnSpyDotNetFile(ModuleDef module, bool loadSyms, bool isAsmNode)
			: base(module, loadSyms) {
			this.isAsmNode = isAsmNode;
		}

		public static DnSpyDotNetFile CreateAssembly(ModuleDef module, bool loadSyms) {
			return new DnSpyDotNetFile(module, loadSyms, true);
		}

		protected override List<IDnSpyFile> CreateChildren() {
			var asm = AssemblyDef;
			var list = new List<IDnSpyFile>(asm == null ? 1 : asm.Modules.Count);
			if (isAsmNode && asm != null) {
				bool foundThis = false;
				foreach (var module in asm.Modules) {
					if (this.module == module) {
						Debug.Assert(!foundThis);
						foundThis = true;
					}
					list.Add(new DnSpyDotNetFile(module, loadedSymbols, false));
				}
				Debug.Assert(foundThis);
			}
			return list;
		}

		public void Dispose() {
			module.Dispose();
		}
	}
}
