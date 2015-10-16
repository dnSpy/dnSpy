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
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.MVVM;

namespace dnSpy.Debugger.Modules {
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

		public bool IsOptimized {
			get { return module.CachedJITCompilerFlags == CorDebugJITCompilerFlags.CORDEBUG_JIT_DEFAULT; }
		}

		public object ImageObject { get { return this; } }
		public object NameObject { get { return this; } }
		public object PathObject { get { return this; } }
		public object OptimizedObject { get { return this; } }
		public object DynamicObject { get { return this; } }
		public object InMemoryObject { get { return this; } }
		public object OrderObject { get { return this; } }
		public object VersionObject { get { return this; } }
		public object TimestampObject { get { return this; } }
		public object AddressObject { get { return this; } }
		public object ProcessObject { get { return this; } }
		public object AppDomainObject { get { return this; } }

		public DnModule Module {
			get { return module; }
		}
		readonly DnModule module;

		public ModuleVM(DnModule module) {
			this.module = module;
		}

		internal void RefreshThemeFields() {
			OnPropertyChanged("ImageObject");
			OnPropertyChanged("NameObject");
			OnPropertyChanged("PathObject");
			OnPropertyChanged("OptimizedObject");
			OnPropertyChanged("DynamicObject");
			OnPropertyChanged("InMemoryObject");
			OnPropertyChanged("OrderObject");
			OnPropertyChanged("VersionObject");
			OnPropertyChanged("TimestampObject");
			OnPropertyChanged("AddressObject");
			OnPropertyChanged("ProcessObject");
			OnPropertyChanged("AppDomainObject");
		}

		internal void RefreshHexFields() {
			OnPropertyChanged("ProcessObject");
		}

		internal void RefreshAppDomainNames(DnAppDomain appDomain) {
			if (module.AppDomain == appDomain)
				OnPropertyChanged("AppDomainObject");
		}

		void InitializeExeFields() {
			if (exeFieldsInitialized)
				return;
			exeFieldsInitialized = true;

			isExe = false;
			timestamp = null;
			version = null;

			if (!module.IsDynamic && module.IsInMemory) {
				var bytes = module.Process.CorProcess.ReadMemory(module.Address, (int)module.Size);
				if (bytes != null) {
					try {
						using (var peImage = new PEImage(bytes))
							InitializeExeFieldsFrom(peImage);
					}
					catch {
					}
				}
			}
			else if (module.IsDynamic || module.IsInMemory) {
				//TODO: Support dynamic modules
			}
			else {
				try {
					using (var peImage = new PEImage(module.Name))
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

			using (var mod = ModuleDefMD.Load(peImage)) {
				var asm = mod.Assembly;
				version = asm == null ? null : asm.Version;
			}
		}
	}
}
