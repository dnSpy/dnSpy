/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
		public string Architecture { get; }

		AttachableProcessInfo(int processId, RuntimeId runtimeId, Guid runtimeGuid, Guid runtimeKindGuid, string runtimeName, string name, string title, string filename, string commandLine, string architecture) {
			ProcessId = processId;
			RuntimeId = runtimeId ?? throw new ArgumentNullException(nameof(runtimeId));
			RuntimeGuid = runtimeGuid;
			RuntimeKindGuid = runtimeKindGuid;
			RuntimeName = runtimeName ?? throw new ArgumentNullException(nameof(runtimeName));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Title = title ?? throw new ArgumentNullException(nameof(title));
			Filename = filename ?? throw new ArgumentNullException(nameof(filename));
			CommandLine = commandLine ?? throw new ArgumentNullException(nameof(commandLine));
			Architecture = architecture ?? throw new ArgumentNullException(nameof(architecture));
		}

		public static AttachableProcessInfo Create(ProcessProvider processProvider, AttachProgramOptions options) {
			if (processProvider == null)
				throw new ArgumentNullException(nameof(processProvider));
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			var name = options.Name;
			var title = options.Title;
			var filename = options.Filename;
			var commandLine = options.CommandLine;
			var architecture = options.Architecture;
			if (name == null || title == null || filename == null || commandLine == null || architecture == null) {
				var info = GetDefaultProperties(processProvider, options);
				name = name ?? info.name ?? string.Empty;
				title = title ?? info.title ?? string.Empty;
				filename = filename ?? info.filename ?? string.Empty;
				commandLine = commandLine ?? info.commandLine ?? string.Empty;
				architecture = architecture ?? info.arch ?? string.Empty;
			}
			return new AttachableProcessInfo(options.ProcessId, options.RuntimeId, options.RuntimeGuid, options.RuntimeKindGuid, options.RuntimeName, name, title, filename, commandLine, architecture);
		}

		static (string name, string title, string filename, string commandLine, string arch) GetDefaultProperties(ProcessProvider processProvider, AttachProgramOptions attachProgramOptions) {
			try {
				return GetDefaultPropertiesCore(processProvider, attachProgramOptions);
			}
			catch {
			}
			return (null, null, null, null, null);
		}

		static (string name, string title, string filename, string commandLine, string arch) GetDefaultPropertiesCore(ProcessProvider processProvider, AttachProgramOptions attachProgramOptions) {
			string name = null, title = null, filename = null, commandLine = null, arch = null;

			var process = processProvider.GetProcess(attachProgramOptions.ProcessId);
			if (process != null) {
				if (attachProgramOptions.CommandLine == null)
					commandLine = Win32CommandLineProvider.TryGetCommandLine(process.Handle);
				if (attachProgramOptions.Title == null)
					title = process.MainWindowTitle;
				if (attachProgramOptions.Architecture == null) {
					switch (ProcessUtilities.GetBitness(process.Handle)) {
					case 32: arch = PredefinedArchitectureNames.X86; break;
					case 64: arch = PredefinedArchitectureNames.X64; break;
					default: arch = "???"; break;
					}
				}
				if (attachProgramOptions.Name == null)
					name = Path.GetFileName(attachProgramOptions.Filename ?? GetProcessName(process));
				if (attachProgramOptions.Filename == null)
					filename = GetProcessName(process);
			}

			return (name, title, filename, commandLine, arch);
		}

		static string GetProcessName(Process process) {
			try {
				return process.MainModule.FileName;
			}
			catch {
			}
			return process.ProcessName;
		}
	}
}
