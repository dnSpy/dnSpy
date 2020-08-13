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
using System.Runtime.InteropServices;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Attach;
using dnSpy.Debugger.Utilities;

namespace dnSpy.Debugger.Attach {
	sealed class AttachableProcessInfo {
		public int ProcessId { get; }
		public RuntimeId RuntimeId { get; }
		public Guid RuntimeGuid { get; }
		public Guid RuntimeKindGuid { get; }
		public string RuntimeName { get; }
		public string Name { get; }
		public string Title { get; }
		public string Filename { get; }
		public string CommandLine { get; }
		public DbgArchitecture Architecture { get; }
		public DbgOperatingSystem OperatingSystem { get; }

		AttachableProcessInfo(int processId, RuntimeId runtimeId, Guid runtimeGuid, Guid runtimeKindGuid, string runtimeName, string name, string title, string filename, string commandLine, DbgArchitecture architecture, DbgOperatingSystem operatingSystem) {
			ProcessId = processId;
			RuntimeId = runtimeId ?? throw new ArgumentNullException(nameof(runtimeId));
			RuntimeGuid = runtimeGuid;
			RuntimeKindGuid = runtimeKindGuid;
			RuntimeName = runtimeName ?? throw new ArgumentNullException(nameof(runtimeName));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Title = title ?? throw new ArgumentNullException(nameof(title));
			Filename = filename ?? throw new ArgumentNullException(nameof(filename));
			CommandLine = commandLine ?? throw new ArgumentNullException(nameof(commandLine));
			Architecture = architecture;
			OperatingSystem = operatingSystem;
		}

		public static AttachableProcessInfo? Create(ProcessProvider processProvider, AttachProgramOptions options) {
			if (processProvider is null)
				throw new ArgumentNullException(nameof(processProvider));
			if (options is null)
				throw new ArgumentNullException(nameof(options));
			var name = options.Name;
			var title = options.Title;
			var filename = options.Filename;
			var commandLine = options.CommandLine;
			var architecture = options.Architecture;
			var operatingSystem = options.OperatingSystem;
			if (name is null || title is null || filename is null || commandLine is null || architecture is null || operatingSystem is null) {
				var info = GetDefaultProperties(processProvider, options);
				name ??= info.name ?? string.Empty;
				title ??= info.title ?? string.Empty;
				filename ??= info.filename ?? string.Empty;
				commandLine ??= info.commandLine ?? string.Empty;
				architecture ??= info.arch;
				operatingSystem ??= info.operatingSystem;
			}
			if (architecture is null)
				return null;
			if (operatingSystem is null)
				return null;
			return new AttachableProcessInfo(options.ProcessId, options.RuntimeId, options.RuntimeGuid, options.RuntimeKindGuid, options.RuntimeName, name, title, filename, commandLine, architecture ?? DbgArchitecture.X86, operatingSystem ?? DbgOperatingSystem.Windows);
		}

		static (string? name, string? title, string? filename, string? commandLine, DbgArchitecture? arch, DbgOperatingSystem? operatingSystem) GetDefaultProperties(ProcessProvider processProvider, AttachProgramOptions attachProgramOptions) {
			try {
				return GetDefaultPropertiesCore(processProvider, attachProgramOptions);
			}
			catch {
			}
			return default;
		}

		static (string? name, string? title, string? filename, string? commandLine, DbgArchitecture? arch, DbgOperatingSystem? operatingSystem) GetDefaultPropertiesCore(ProcessProvider processProvider, AttachProgramOptions attachProgramOptions) {
			string? name = null, title = null, filename = null, commandLine = null;
			DbgArchitecture? arch = default;
			DbgOperatingSystem? operatingSystem = default;

			var process = processProvider.GetProcess(attachProgramOptions.ProcessId);
			if (!(process is null)) {
				if (attachProgramOptions.CommandLine is null)
					commandLine = Win32CommandLineProvider.TryGetCommandLine(process.Handle);
				if (attachProgramOptions.Title is null)
					title = process.MainWindowTitle;
				if (attachProgramOptions.Architecture is null) {
					int bitness = ProcessUtilities.GetBitness(process.Handle);
					var processArchitecture = RuntimeInformation.ProcessArchitecture;
					switch (processArchitecture) {
					case System.Runtime.InteropServices.Architecture.X86:
					case System.Runtime.InteropServices.Architecture.X64:
						switch (bitness) {
						case 32: arch = DbgArchitecture.X86; break;
						case 64: arch = DbgArchitecture.X64; break;
						}
						break;

					case System.Runtime.InteropServices.Architecture.Arm:
					case System.Runtime.InteropServices.Architecture.Arm64:
						switch (bitness) {
						case 32: arch = DbgArchitecture.Arm; break;
						case 64: arch = DbgArchitecture.Arm64; break;
						}
						break;

					default:
						Debug.Fail($"Unknown arch: {processArchitecture}");
						break;
					}
				}
				if (attachProgramOptions.Name is null)
					name = Path.GetFileName(attachProgramOptions.Filename ?? GetProcessName(process));
				if (attachProgramOptions.Filename is null)
					filename = GetProcessName(process);
			}
			if (attachProgramOptions.OperatingSystem is null) {
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
					operatingSystem = DbgOperatingSystem.Windows;
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
					operatingSystem = DbgOperatingSystem.MacOS;
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
					operatingSystem = DbgOperatingSystem.Linux;
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD")))
					operatingSystem = DbgOperatingSystem.FreeBSD;
				else
					Debug.Fail($"Unknown OS: {RuntimeInformation.OSDescription}");
			}

			return (name, title, filename, commandLine, arch, operatingSystem);
		}

		static string? GetProcessName(Process process) {
			try {
				return process.MainModule?.FileName;
			}
			catch {
			}
			return process.ProcessName;
		}
	}
}
