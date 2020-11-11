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
using System.Diagnostics;
using System.IO;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.DotNet.MD;
using dnlib.PE;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Engine;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	static class ModuleCreator {
		public static DbgEngineModule CreateModule<T>(DbgEngineImpl engine, DbgObjectFactory objectFactory, DbgAppDomain? appDomain, DnModule dnModule, T data) where T : class {
			ulong address = dnModule.Address;
			uint size = dnModule.Size;
			var imageLayout = CalculateImageLayout(dnModule);
			string name = GetFilename(dnModule.Name);
			string filename = dnModule.Name;
			bool isDynamic = dnModule.IsDynamic;
			bool isInMemory = dnModule.IsInMemory;
			bool? isOptimized = CalculateIsOptimized(dnModule);
			int order = dnModule.UniqueId + 1;// 0-based to 1-based
			InitializeExeFields(dnModule, filename, imageLayout, out var isExe, out var isDll, out var timestamp, out var version, out var assemblySimpleName);

			if (appDomain is null)
				throw new InvalidOperationException("No appdomain");
			var reflectionAppDomain = ((DbgCorDebugInternalAppDomainImpl)appDomain.InternalAppDomain).ReflectionAppDomain;

			var closedListenerCollection = new ClosedListenerCollection();
			var modules = dnModule.Assembly.Modules;
			bool isManifestModule = modules[0] == dnModule;
			var getMetadata = CreateGetMetadataDelegate(engine, objectFactory.Runtime, dnModule, closedListenerCollection, imageLayout);
			var fullyQualifiedName = DmdModule.GetFullyQualifiedName(isInMemory, isDynamic, dnModule.Name);
			DmdAssembly reflectionAssembly;
			DmdModule reflectionModule;
			if (isManifestModule) {
				var assemblyLocation = isInMemory || isDynamic ? string.Empty : dnModule.Name;
				var asmOptions = DmdCreateAssemblyOptions.None;
				if (isInMemory)
					asmOptions |= DmdCreateAssemblyOptions.InMemory;
				if (isDynamic)
					asmOptions |= DmdCreateAssemblyOptions.Dynamic;
				if (isExe)
					asmOptions |= DmdCreateAssemblyOptions.IsEXE;
				else if (isDll)
					asmOptions |= DmdCreateAssemblyOptions.IsDLL;
				var asmInfo = new DmdCreateAssemblyInfo(asmOptions, fullyQualifiedName, assemblyLocation, assemblySimpleName);
				reflectionAssembly = reflectionAppDomain.CreateAssembly(getMetadata, asmInfo);
				reflectionModule = reflectionAssembly.ManifestModule;
			}
			else {
				var manifestModule = engine.TryGetModule(modules[0].CorModule);
				if (manifestModule is null)
					throw new InvalidOperationException();
				reflectionAssembly = ((DbgCorDebugInternalModuleImpl)manifestModule.InternalModule).ReflectionModule!.Assembly;
				reflectionModule = reflectionAppDomain.CreateModule(reflectionAssembly, getMetadata, isInMemory, isDynamic, fullyQualifiedName);
			}

			var internalModule = new DbgCorDebugInternalModuleImpl(reflectionModule, closedListenerCollection);
			return objectFactory.CreateModule(appDomain, internalModule, isExe, address, size, imageLayout, name, filename, isDynamic, isInMemory, isOptimized, order, timestamp, version, engine.GetMessageFlags(), data: data, onCreated: engineModule => internalModule.SetModule(engineModule.Module));
		}

		static Func<DmdLazyMetadataBytes> CreateGetMetadataDelegate(DbgEngineImpl engine, DbgRuntime runtime, DnModule dnModule, ClosedListenerCollection closedListenerCollection, DbgImageLayout imageLayout) {
			if (dnModule.IsDynamic)
				return CreateDynamicGetMetadataDelegate(engine, dnModule);
			else
				return CreateNormalGetMetadataDelegate(engine, runtime, dnModule, closedListenerCollection, imageLayout);
		}

		static Func<DmdLazyMetadataBytes> CreateDynamicGetMetadataDelegate(DbgEngineImpl engine, DnModule dnModule) {
			Debug.Assert(dnModule.IsDynamic);
			var comMetadata = dnModule.CorModule.GetMetaDataInterface<IMetaDataImport2>();
			if (comMetadata is null)
				throw new InvalidOperationException();
			var result = new DmdLazyMetadataBytesCom(comMetadata, engine.GetDynamicModuleHelper(dnModule), engine.DmdDispatcher);
			return () => result;
		}

		static Func<DmdLazyMetadataBytes> CreateNormalGetMetadataDelegate(DbgEngineImpl engine, DbgRuntime runtime, DnModule dnModule, ClosedListenerCollection closedListenerCollection, DbgImageLayout imageLayout) {
			Debug.Assert(!dnModule.IsDynamic);

			return () => {
				var rawMd = engine.RawMetadataService.Create(runtime, imageLayout == DbgImageLayout.File, dnModule.Address, (int)dnModule.Size);
				try {
					closedListenerCollection.Closed += (s, e) => rawMd.Release();
					return new DmdLazyMetadataBytesPtr(rawMd.Address, (uint)rawMd.Size, rawMd.IsFileLayout);
				}
				catch {
					rawMd.Release();
					throw;
				}
			};
		}

		static DbgImageLayout CalculateImageLayout(DnModule dnModule) {
			if (dnModule.IsDynamic)
				return DbgImageLayout.Unknown;
			if (dnModule.IsInMemory)
				return DbgImageLayout.File;
			return DbgImageLayout.Memory;
		}

		static bool? CalculateIsOptimized(DnModule dnModule) {
			switch (dnModule.CachedJITCompilerFlags) {
			case CorDebugJITCompilerFlags.CORDEBUG_JIT_DEFAULT:
				return true;
			case CorDebugJITCompilerFlags.CORDEBUG_JIT_DISABLE_OPTIMIZATION:
			case CorDebugJITCompilerFlags.CORDEBUG_JIT_ENABLE_ENC:
				return false;
			default:
				Debug.Fail($"Unknown JIT compiler flags: {dnModule.CachedJITCompilerFlags}");
				return null;
			}
		}

		static string GetFilename(string s) {
			try {
				return Path.GetFileName(s)!;
			}
			catch {
			}
			return s;
		}

		static void InitializeExeFields(DnModule dnModule, string filename, DbgImageLayout imageLayout, out bool isExe, out bool isDll, out DateTime? timestamp, out string version, out string? assemblySimpleName) {
			isExe = false;
			isDll = false;
			timestamp = null;
			version = string.Empty;
			assemblySimpleName = null;

			if (dnModule.IsDynamic) {
				if (dnModule.CorModule.IsManifestModule)
					version = new AssemblyNameInfo(dnModule.Assembly.FullName).Version.ToString();
			}
			else if (dnModule.IsInMemory) {
				Debug.Assert(imageLayout == DbgImageLayout.File, nameof(GetFileVersion) + " assumes file layout");

				var bytes = dnModule.Process.CorProcess.ReadMemory(dnModule.Address, (int)dnModule.Size);
				if (bytes is not null) {
					try {
						version = GetFileVersion(bytes);
						using (var peImage = new PEImage(bytes, imageLayout == DbgImageLayout.File ? ImageLayout.File : ImageLayout.Memory, true))
							InitializeExeFieldsFrom(peImage, out isExe, out isDll, out timestamp, ref version, out assemblySimpleName);
					}
					catch {
					}
				}
			}
			else {
				try {
					version = GetFileVersion(filename);
					using (var peImage = new PEImage(filename))
						InitializeExeFieldsFrom(peImage, out isExe, out isDll, out timestamp, ref version, out assemblySimpleName);
				}
				catch {
				}
			}
		}

		static void InitializeExeFieldsFrom(IPEImage peImage, out bool isExe, out bool isDll, out DateTime? timestamp, ref string version, out string? assemblySimpleName) {
			isExe = (peImage.ImageNTHeaders.FileHeader.Characteristics & Characteristics.Dll) == 0;
			isDll = !isExe;

			// Roslyn sets bit 31 if /deterministic is used (the low 31 bits is not a timestamp)
			if (peImage.ImageNTHeaders.FileHeader.TimeDateStamp < 0x80000000 && peImage.ImageNTHeaders.FileHeader.TimeDateStamp != 0)
				timestamp = Epoch.AddSeconds(peImage.ImageNTHeaders.FileHeader.TimeDateStamp);
			else
				timestamp = null;

			try {
				if (string.IsNullOrEmpty(version)) {
					using (var mod = ModuleDefMD.Load(peImage)) {
						if (string.IsNullOrEmpty(version))
							version = mod.Assembly?.Version.ToString() ?? string.Empty;
						assemblySimpleName = UTF8String.ToSystemString(mod.Assembly?.Name);
					}
				}
				else {
					using (var md = MetadataFactory.CreateMetadata(peImage)) {
						if (!md.TablesStream.TryReadAssemblyRow(1, out var row))
							assemblySimpleName = null;
						else
							assemblySimpleName = md.StringsStream.Read(row.Name);
					}

				}
			}
			catch {
				assemblySimpleName = null;
			}
		}
		static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		static string GetFileVersion(string filename) {
			if (!File.Exists(filename))
				return string.Empty;
			try {
				var info = FileVersionInfo.GetVersionInfo(filename);
				return info.FileVersion ?? string.Empty;
			}
			catch {
			}
			return string.Empty;
		}

		static string GetFileVersion(byte[] bytes) {
			string? tempFilename = null;
			try {
				tempFilename = Path.GetTempFileName();
				File.WriteAllBytes(tempFilename, bytes);
				return GetFileVersion(tempFilename);
			}
			catch {
			}
			finally {
				try {
					if (tempFilename is not null)
						File.Delete(tempFilename);
				}
				catch { }
			}
			return string.Empty;
		}
	}
}
