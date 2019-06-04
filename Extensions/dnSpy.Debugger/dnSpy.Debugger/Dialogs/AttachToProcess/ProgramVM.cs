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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Attach;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Debugger.Attach;
using dnSpy.Debugger.UI;
using dnSpy.Debugger.Utilities;

namespace dnSpy.Debugger.Dialogs.AttachToProcess {
	sealed class ProgramVM : ViewModelBase {
		public AttachProgramOptions AttachProgramOptions { get; }

		public int Id => attachableProcessInfo.ProcessId;
		public string RuntimeName => attachableProcessInfo.RuntimeName;
		public string Name => attachableProcessInfo.Name;
		public string Title => attachableProcessInfo.Title;
		public string Filename => attachableProcessInfo.Filename;
		public string CommandLine => attachableProcessInfo.CommandLine;
		public string Architecture => GetArchitectureString(attachableProcessInfo.Architecture);

		public IAttachToProcessContext Context { get; }
		public object ProcessObject => new FormatterObject<ProgramVM>(this, PredefinedTextClassifierTags.AttachToProcessWindowProcess);
		public object IdObject => new FormatterObject<ProgramVM>(this, PredefinedTextClassifierTags.AttachToProcessWindowPid);
		public object TitleObject => new FormatterObject<ProgramVM>(this, PredefinedTextClassifierTags.AttachToProcessWindowTitle);
		public object RuntimeNameObject => new FormatterObject<ProgramVM>(this, PredefinedTextClassifierTags.AttachToProcessWindowType);
		public object ArchitectureObject => new FormatterObject<ProgramVM>(this, PredefinedTextClassifierTags.AttachToProcessWindowMachine);
		public object PathObject => new FormatterObject<ProgramVM>(this, PredefinedTextClassifierTags.AttachToProcessWindowFullPath);
		public object CommandLineObject => new FormatterObject<ProgramVM>(this, PredefinedTextClassifierTags.AttachToProcessWindowCommandLine);

		readonly AttachableProcessInfo attachableProcessInfo;

		public static ProgramVM? Create(ProcessProvider processProvider, AttachProgramOptions attachProgramOptions, IAttachToProcessContext context) {
			if (processProvider is null)
				throw new ArgumentNullException(nameof(processProvider));
			var attachableProcessInfo = AttachableProcessInfo.Create(processProvider, attachProgramOptions);
			if (attachableProcessInfo is null)
				return null;
			return new ProgramVM(attachableProcessInfo, attachProgramOptions, context);
		}

		ProgramVM(AttachableProcessInfo attachableProcessInfo, AttachProgramOptions attachProgramOptions, IAttachToProcessContext context) {
			AttachProgramOptions = attachProgramOptions ?? throw new ArgumentNullException(nameof(attachProgramOptions));
			this.attachableProcessInfo = attachableProcessInfo ?? throw new ArgumentNullException(nameof(attachableProcessInfo));
			Context = context ?? throw new ArgumentNullException(nameof(context));
		}

		static string GetArchitectureString(DbgArchitecture architecture) {
			switch (architecture) {
			case DbgArchitecture.X86:	return "x86";
			case DbgArchitecture.X64:	return "x64";
			case DbgArchitecture.Arm:	return "ARM";
			case DbgArchitecture.Arm64:	return "ARM64";
			default:
				Debug.Fail($"Unknown architecture: {architecture}");
				return "???";
			}
		}
	}
}
