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
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;

namespace dnSpy.Debugger.Modules {
	interface IModuleContext {
		IImageService ImageService { get; }
		ImageOptions ImageOptions { get; }
		ITheDebugger TheDebugger { get; }
		bool SyntaxHighlight { get; }
		bool UseHexadecimal { get; }
	}

	sealed class ModuleContext : IModuleContext {
		public IImageService ImageService { get; }
		public ITheDebugger TheDebugger { get; }
		public ImageOptions ImageOptions { get; set; }
		public bool SyntaxHighlight { get; set; }
		public bool UseHexadecimal { get; set; }

		public ModuleContext(IImageService imageService, ITheDebugger theDebugger) {
			this.ImageService = imageService;
			this.TheDebugger = theDebugger;
		}
	}

	sealed class ModuleVM : ViewModelBase {
		public bool IsExe {
			get {
				InitializeExeFields();
				return isExe;
			}
		}
		bool isExe;

		public uint? Timestamp {
			get {
				InitializeExeFields();
				return timestamp;
			}
		}
		uint? timestamp;

		public Version Version {
			get {
				InitializeExeFields();
				return version;
			}
		}
		Version version;

		public bool IsOptimized => Module.CachedJITCompilerFlags == CorDebugJITCompilerFlags.CORDEBUG_JIT_DEFAULT;
		public object ImageObject => this;
		public object NameObject => this;
		public object PathObject => this;
		public object OptimizedObject => this;
		public object DynamicObject => this;
		public object InMemoryObject => this;
		public object OrderObject => this;
		public object VersionObject => this;
		public object TimestampObject => this;
		public object AddressObject => this;
		public object ProcessObject => this;
		public object AppDomainObject => this;
		public DnModule Module { get; }
		public IModuleContext Context { get; }

		public ModuleVM(DnModule module, IModuleContext context) {
			this.Module = module;
			this.Context = context;
		}

		internal void RefreshThemeFields() {
			OnPropertyChanged(nameof(ImageObject));
			OnPropertyChanged(nameof(NameObject));
			OnPropertyChanged(nameof(PathObject));
			OnPropertyChanged(nameof(OptimizedObject));
			OnPropertyChanged(nameof(DynamicObject));
			OnPropertyChanged(nameof(InMemoryObject));
			OnPropertyChanged(nameof(OrderObject));
			OnPropertyChanged(nameof(VersionObject));
			OnPropertyChanged(nameof(TimestampObject));
			OnPropertyChanged(nameof(AddressObject));
			OnPropertyChanged(nameof(ProcessObject));
			OnPropertyChanged(nameof(AppDomainObject));
		}

		internal void RefreshHexFields() => OnPropertyChanged(nameof(ProcessObject));

		internal void RefreshAppDomainNames(DnAppDomain appDomain) {
			if (Module.AppDomain == appDomain)
				OnPropertyChanged(nameof(AppDomainObject));
		}

		void InitializeExeFields() {
			if (exeFieldsInitialized)
				return;
			exeFieldsInitialized = true;

			isExe = false;
			timestamp = null;
			version = null;

			if (!Module.IsDynamic && Module.IsInMemory) {
				var bytes = Module.Process.CorProcess.ReadMemory(Module.Address, (int)Module.Size);
				if (bytes != null) {
					try {
						using (var peImage = new PEImage(bytes))
							InitializeExeFieldsFrom(peImage);
					}
					catch {
					}
				}
			}
			else if (Module.IsDynamic || Module.IsInMemory) {
				if (Module.CorModule.IsManifestModule)
					version = new AssemblyNameInfo(Module.Assembly.FullName).Version;
			}
			else {
				try {
					using (var peImage = new PEImage(Module.Name))
						InitializeExeFieldsFrom(peImage);
				}
				catch {
				}
			}
		}
		bool exeFieldsInitialized = false;

		void InitializeExeFieldsFrom(IPEImage peImage) {
			isExe = (peImage.ImageNTHeaders.FileHeader.Characteristics & Characteristics.Dll) == 0;
			timestamp = peImage.ImageNTHeaders.FileHeader.TimeDateStamp;

			using (var mod = ModuleDefMD.Load(peImage))
				version = mod.Assembly?.Version;
		}
	}
}
