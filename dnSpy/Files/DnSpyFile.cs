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
using System.IO;
using dnlib.DotNet;
using dnlib.PE;

namespace dnSpy.Files {
	public abstract class DnSpyFile : IDisposable {
		/// <summary>
		/// Gets a key for this file. Eg. a <see cref="FilenameKey"/> instance if it's a
		/// file loaded from disk.
		/// </summary>
		public virtual IDnSpyFilenameKey Key {
			get { return new FilenameKey(Filename); }
		}

		public virtual SerializedDnSpyModule? SerializedDnSpyModule {
			get {
				var mod = ModuleDef;
				return mod == null ? (SerializedDnSpyModule?)null : Files.SerializedDnSpyModule.CreateFromFile(mod);
			}
		}

		/// <summary>
		/// Gets the assembly or null if it's not a .NET file or if it's a netmodule
		/// </summary>
		public AssemblyDef AssemblyDef {
			get { var m = ModuleDef; return m == null ? null : m.Assembly; }
		}

		/// <summary>
		/// Gets the module or null if it's not a .NET file
		/// </summary>
		public virtual ModuleDef ModuleDef {
			get { return null; }
		}

		/// <summary>
		/// Gets the PE image or null if none available
		/// </summary>
		public virtual IPEImage PEImage {
			get {
				var m = ModuleDef as ModuleDefMD;
				return m == null ? null : m.MetaData.PEImage;
			}
		}

		/// <summary>
		/// Gets the filename.
		/// </summary>
		public string Filename {
			get { return filename; }
			set {
				this.filename = value;
				InitShortName(filename, DefaultShortName);
			}
		}
		string filename;

		string DefaultShortName {
			get {
				var m = ModuleDef;
				return m == null ? null : m.Name;
			}
		}

		public string ShortName {
			get { return shortName; }
		}
		protected string shortName;

		public bool IsAutoLoaded { get; set; }

		public virtual bool CanBeSavedToSettingsFile {
			get { return true; }
		}

		/// <summary>
		/// true if it was loaded from a file, false if it was eg. loaded from memory
		/// </summary>
		public virtual bool LoadedFromFile {
			get { return true; }
		}

		/// <summary>
		/// true if the file can't be edited, eg. it's an in-memory module and we're debugging it
		/// </summary>
		public virtual bool IsReadOnly {
			get { return false; }
		}

		protected DnSpyFile() {
		}

		void InitShortName(string filename, string defaultName) {
			this.shortName = GetShortName(filename);
			if (string.IsNullOrEmpty(this.shortName))
				this.shortName = defaultName ?? string.Empty;
		}

		static string GetShortName(string fileName) {
			var s = Path.GetFileNameWithoutExtension(fileName);
			if (!string.IsNullOrWhiteSpace(s))
				return s;
			s = Path.GetFileName(fileName);
			if (!string.IsNullOrWhiteSpace(s))
				return s;
			return fileName;
		}

		public static DnSpyFile Create(ModuleDef module, bool loadSyms, IAssemblyResolver asmResolver) {
			module.Context = CreateModuleContext(asmResolver);
			return new DotNetFile(module, loadSyms);
		}

		public static DnSpyFile CreateFromFile(string filename, bool useMemoryMappedIO, bool loadSyms, IAssemblyResolver asmResolver) {
			try {
				// Quick check to prevent exceptions being thrown
				if (!File.Exists(filename))
					return new UnknownFile(filename);

				IPEImage peImage;

				if (useMemoryMappedIO)
					peImage = new PEImage(filename);
				else
					peImage = new PEImage(File.ReadAllBytes(filename), filename);

				var dotNetDir = peImage.ImageNTHeaders.OptionalHeader.DataDirectories[14];
				bool isDotNet = dotNetDir.VirtualAddress != 0 && dotNetDir.Size >= 0x48;

				if (isDotNet) {
					try {
						var options = new ModuleCreationOptions(CreateModuleContext(asmResolver));
						return new DotNetFile(ModuleDefMD.Load(peImage, options), loadSyms);
					}
					catch {
					}
				}

				return new PEFile(peImage);
			}
			catch {
			}
			return new UnknownFile(filename);
		}

		internal static ModuleContext CreateModuleContext(IAssemblyResolver asmResolver) {
			ModuleContext moduleCtx = new ModuleContext();
			moduleCtx.AssemblyResolver = asmResolver;
			// Disable WinMD projection since the user probably expects that clicking on a type
			// will take you to that type, and not to the projected CLR type.
			// The decompiler shouldn't have a problem with this since it uses SigComparer() which
			// defaults to projecting WinMD types.
			moduleCtx.Resolver = new Resolver(moduleCtx.AssemblyResolver) { ProjectWinMDRefs = false };
			return moduleCtx;
		}

		readonly Dictionary<object, object> dict = new Dictionary<object, object>();
		public TValue GetOrCreateAnnotation<TKey, TValue>(TKey key) where TValue : new() {
			object value;
			if (dict.TryGetValue(key, out value))
				return (TValue)value;
			var t = new TValue();
			dict.Add(key, t);
			return t;
		}

		public TValue GetAnnotation<TKey, TValue>(TKey key) where TValue : new() {
			object value;
			if (dict.TryGetValue(key, out value))
				return (TValue)value;
			return default(TValue);
		}

		public void RemoveAnnotation<TKey>(TKey key) {
			dict.Remove(key);
		}

		public virtual DnSpyFile CreateDnSpyFile(ModuleDef module) {
			return null;
		}

		public virtual void Dispose() {
		}
	}

	sealed class UnknownFile : DnSpyFile {
		public UnknownFile(string filename) {
			Filename = filename;
		}
	}

	sealed class PEFile : DnSpyFile {
		public override IPEImage PEImage {
			get { return peImage; }
		}
		readonly IPEImage peImage;

		public PEFile(IPEImage peImage) {
			this.peImage = peImage;
			Filename = peImage.FileName ?? string.Empty;
		}

		public override void Dispose() {
			peImage.Dispose();
		}
	}

	abstract class DotNetFileBase : DnSpyFile {
		public override ModuleDef ModuleDef {
			get { return module; }
		}
		protected readonly ModuleDef module;

		protected DotNetFileBase(ModuleDef module, bool loadSyms) {
			this.module = module;
			Filename = module.Location;
			module.EnableTypeDefFindCache = true;
			if (loadSyms)
				LoadSymbols(module.Location);
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

	sealed class DotNetFile : DotNetFileBase {
		public DotNetFile(ModuleDef module, bool loadSyms)
			: base(module, loadSyms) {
		}

		public override void Dispose() {
			module.Dispose();
		}
	}
}
