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
using dnSpy.Contracts.Debugger.Engine;

namespace dnSpy.Debugger.CorDebug.Impl {
	static class ModuleCreator {
		public static DbgEngineModule CreateModule(DbgObjectFactory objectFactory, DbgAppDomain appDomain, DnModule dnModule) {
			ulong address = dnModule.Address;
			uint size = dnModule.Size;
			var imageLayout = CalculateImageLayout(dnModule);
			string name = GetFilename(dnModule.Name);
			string filename = dnModule.Name;
			bool isDynamic = dnModule.IsDynamic;
			bool isInMemory = dnModule.IsInMemory;
			bool isOptimized = CalculateIsOptimized(dnModule);
			int order = dnModule.UniqueId;
			InitializeExeFields(dnModule, filename, imageLayout, out var isExe, out var timestamp, out var version);
			return objectFactory.CreateModule(appDomain, isExe, address, size, imageLayout, name, filename, isDynamic, isInMemory, isOptimized, order, timestamp, version);
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

		static void InitializeExeFields(DnModule dnModule, string filename, DbgImageLayout imageLayout, out bool isExe, out DateTime? timestamp, out string version) {
			isExe = false;
			timestamp = null;
			version = null;

			if (dnModule.IsDynamic) {
				if (dnModule.CorModule.IsManifestModule)
					version = new AssemblyNameInfo(dnModule.Assembly.FullName).Version.ToString();
			}
			else if (dnModule.IsInMemory) {
				Debug.Assert(imageLayout == DbgImageLayout.File, nameof(GetFileVersion) + " assumes file layout");

				var bytes = dnModule.Process.CorProcess.ReadMemory(dnModule.Address, (int)dnModule.Size);
				if (bytes != null) {
					try {
						version = GetFileVersion(bytes);
						using (var peImage = new PEImage(bytes, imageLayout == DbgImageLayout.File ? ImageLayout.File : ImageLayout.Memory, true))
							InitializeExeFieldsFrom(peImage, out isExe, out timestamp, ref version);
					}
					catch {
					}
				}
			}
			else {
				try {
					version = GetFileVersion(filename);
					using (var peImage = new PEImage(filename))
						InitializeExeFieldsFrom(peImage, out isExe, out timestamp, ref version);
				}
				catch {
				}
			}

			if (version == null)
				version = string.Empty;
		}

		static void InitializeExeFieldsFrom(IPEImage peImage, out bool isExe, out DateTime? timestamp, ref string version) {
			isExe = (peImage.ImageNTHeaders.FileHeader.Characteristics & Characteristics.Dll) == 0;
			//TODO: Roslyn sets bit 31 if /deterministic is used (the low 31 bits is not a timestamp)
			timestamp = Epoch.AddSeconds(peImage.ImageNTHeaders.FileHeader.TimeDateStamp);

			if (string.IsNullOrEmpty(version)) {
				using (var mod = ModuleDefMD.Load(peImage))
					version = mod.Assembly?.Version.ToString();
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
			string tempFilename = null;
			try {
				tempFilename = Path.GetTempFileName();
				File.WriteAllBytes(tempFilename, bytes);
				return GetFileVersion(tempFilename);
			}
			catch {
			}
			finally {
				try {
					if (tempFilename != null)
						File.Delete(tempFilename);
				}
				catch { }
			}
			return string.Empty;
		}
	}
}
