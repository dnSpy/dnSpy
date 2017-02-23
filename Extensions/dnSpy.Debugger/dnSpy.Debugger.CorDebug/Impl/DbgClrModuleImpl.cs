/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Diagnostics;
using System.IO;
using dndbg.COM.CorDebug;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet;

namespace dnSpy.Debugger.CorDebug.Impl {
	sealed class DbgClrModuleImpl : DbgClrModule {
		readonly object lockObj;

		public override DbgClrAssembly Assembly => throw new NotImplementedException();//TODO:
		public override DbgRuntime Runtime { get; }
		public override ulong Address { get; }
		public override uint Size { get; }
		public override DbgImageLayout ImageLayout { get; }
		public override string Name { get; }
		public override string Filename { get; }
		public override string RealFilename { get; }
		public override bool IsDynamic { get; }
		public override bool IsInMemory { get; }
		public override bool? IsOptimized { get; }
		public override int Order { get; }

		public override bool IsExe {
			get {
				InitializeExeFields();
				return isExe;
			}
		}
		bool isExe;

		public override DateTime? Timestamp {
			get {
				InitializeExeFields();
				return timestamp;
			}
		}
		DateTime? timestamp;

		public override string Version {
			get {
				InitializeExeFields();
				return version;
			}
		}
		string version;

		internal DnModule DnModule { get; }

		readonly bool isManifestModule;

		public DbgClrModuleImpl(DbgRuntime runtime, DnModule dnModule) {
			lockObj = new object();
			Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			DnModule = dnModule ?? throw new ArgumentNullException(nameof(dnModule));
			Address = dnModule.Address;
			Size = dnModule.Size;
			ImageLayout = CalculateImageLayout(dnModule);
			Name = GetFilename(dnModule.Name);
			Filename = dnModule.Name;
			RealFilename = dnModule.Name;//TODO: Find real filename
			IsDynamic = dnModule.IsDynamic;
			IsInMemory = dnModule.IsInMemory;
			IsOptimized = CalculateIsOptimized(dnModule);
			Order = dnModule.UniqueId;
			isManifestModule = dnModule.CorModule.IsManifestModule;
		}

		static DbgImageLayout CalculateImageLayout(DnModule dnModule) {
			if (dnModule.IsDynamic)
				return DbgImageLayout.Unknown;
			if (dnModule.IsInMemory)
				return DbgImageLayout.File;
			return DbgImageLayout.Memory;
		}

		static bool CalculateIsOptimized(DnModule dnModule) {
			switch (dnModule.CachedJITCompilerFlags) {
			case CorDebugJITCompilerFlags.CORDEBUG_JIT_DEFAULT:
				return true;
			case CorDebugJITCompilerFlags.CORDEBUG_JIT_DISABLE_OPTIMIZATION:
			case CorDebugJITCompilerFlags.CORDEBUG_JIT_ENABLE_ENC:
				return false;
			default:
				Debug.Fail($"Unknown JIT compiler flags: {dnModule.CachedJITCompilerFlags}");
				return false;
			}
		}

		static string GetFilename(string s) {
			try {
				return Path.GetFileName(s);
			}
			catch {
			}
			return s;
		}

		void InitializeExeFields() {
			if (exeFieldsInitialized)
				return;
			lock (lockObj) {
				if (exeFieldsInitialized)
					return;

				isExe = false;
				timestamp = null;
				version = null;

				if (!IsDynamic && IsInMemory) {
					var bytes = DnModule.Process.CorProcess.ReadMemory(Address, (int)Size);
					if (bytes != null) {
						try {
							using (var peImage = new PEImage(bytes))
								InitializeExeFieldsFrom(peImage);
						}
						catch {
						}
					}
				}
				else if (IsDynamic || IsInMemory) {
					if (isManifestModule)
						version = new AssemblyNameInfo(DnModule.Assembly.FullName).Version.ToString();
				}
				else {
					try {
						using (var peImage = new PEImage(Filename))
							InitializeExeFieldsFrom(peImage);
					}
					catch {
					}
				}

				if (version == null)
					version = string.Empty;

				exeFieldsInitialized = true;
			}
		}
		bool exeFieldsInitialized;

		void InitializeExeFieldsFrom(IPEImage peImage) {
			isExe = (peImage.ImageNTHeaders.FileHeader.Characteristics & Characteristics.Dll) == 0;
			//TODO: Roslyn sets bit 31 if /deterministic is used (the low 31 bits is not a timestamp)
			timestamp = Epoch.AddSeconds(peImage.ImageNTHeaders.FileHeader.TimeDateStamp);

			using (var mod = ModuleDefMD.Load(peImage))
				version = mod.Assembly?.Version.ToString();
		}
		static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		protected override void CloseCore() { }
	}
}
