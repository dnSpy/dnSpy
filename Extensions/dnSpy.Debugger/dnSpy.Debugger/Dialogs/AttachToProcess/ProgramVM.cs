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
using System.IO;
using dnSpy.Contracts.Debugger.Attach;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.UI;
using dnSpy.Debugger.Utilities;

namespace dnSpy.Debugger.Dialogs.AttachToProcess {
	sealed class ProgramVM : ViewModelBase {
		public AttachProgramOptions AttachProgramOptions { get; }

		public int Id => AttachProgramOptions.ProcessId;
		public string RuntimeName => AttachProgramOptions.RuntimeName;
		public string Name { get; }
		public string Title { get; }
		public string Filename { get; }
		public string Architecture { get; }

		public IAttachToProcessContext Context { get; }
		public object ProcessObject => new FormatterObject<ProgramVM>(this, PredefinedTextClassifierTags.AttachToProcessWindowProcess);
		public object IdObject => new FormatterObject<ProgramVM>(this, PredefinedTextClassifierTags.AttachToProcessWindowPid);
		public object TitleObject => new FormatterObject<ProgramVM>(this, PredefinedTextClassifierTags.AttachToProcessWindowTitle);
		public object RuntimeNameObject => new FormatterObject<ProgramVM>(this, PredefinedTextClassifierTags.AttachToProcessWindowType);
		public object ArchitectureObject => new FormatterObject<ProgramVM>(this, PredefinedTextClassifierTags.AttachToProcessWindowMachine);
		public object PathObject => new FormatterObject<ProgramVM>(this, PredefinedTextClassifierTags.AttachToProcessWindowFullPath);

		public ProgramVM(ProcessProvider processProvider, AttachProgramOptions attachProgramOptions, IAttachToProcessContext context) {
			if (processProvider == null)
				throw new ArgumentNullException(nameof(processProvider));
			AttachProgramOptions = attachProgramOptions ?? throw new ArgumentNullException(nameof(attachProgramOptions));
			Name = attachProgramOptions.Name;
			Title = attachProgramOptions.Title;
			Filename = attachProgramOptions.Filename;
			Architecture = attachProgramOptions.Architecture;
			Context = context ?? throw new ArgumentNullException(nameof(context));

			if (Name == null || Title == null || Filename == null || Architecture == null) {
				var info = GetDefaultProperties(processProvider, attachProgramOptions);
				Name = Name ?? info.name ?? string.Empty;
				Title = Title ?? info.title ?? string.Empty;
				Filename = Filename ?? info.filename ?? string.Empty;
				Architecture = Architecture ?? info.arch ?? string.Empty;
			}
		}

		static (string name, string title, string filename, string arch) GetDefaultProperties(ProcessProvider processProvider, AttachProgramOptions attachProgramOptions) {
			try {
				return GetDefaultPropertiesCore(processProvider, attachProgramOptions);
			}
			catch {
			}
			return (null, null, null, null);
		}

		static (string name, string title, string filename, string arch) GetDefaultPropertiesCore(ProcessProvider processProvider, AttachProgramOptions attachProgramOptions) {
			string name = null, title = null, filename = null, arch = null;

			var process = processProvider.GetProcess(attachProgramOptions.ProcessId);
			if (process != null) {
				if (attachProgramOptions.Name == null)
					name = Path.GetFileName(attachProgramOptions.Filename ?? process.MainModule.FileName);
				if (attachProgramOptions.Filename == null)
					filename = process.MainModule.FileName;
				if (attachProgramOptions.Title == null)
					title = process.MainWindowTitle;
				if (attachProgramOptions.Architecture == null) {
					switch (ProcessUtilities.GetBitness(process.Handle)) {
					case 32: arch = PredefinedArchitectureNames.X86; break;
					case 64: arch = PredefinedArchitectureNames.X64; break;
					default: arch = "???"; break;
					}
				}
			}

			return (name, title, filename, arch);
		}
	}
}
