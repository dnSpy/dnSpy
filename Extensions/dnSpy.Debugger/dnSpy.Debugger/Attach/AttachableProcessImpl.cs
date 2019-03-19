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
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Attach;

namespace dnSpy.Debugger.Attach {
	sealed class AttachableProcessImpl : AttachableProcess {
		public override int ProcessId => attachableProcessInfo.ProcessId;
		public override RuntimeId RuntimeId => attachableProcessInfo.RuntimeId;
		public override Guid RuntimeGuid => attachableProcessInfo.RuntimeGuid;
		public override Guid RuntimeKindGuid => attachableProcessInfo.RuntimeKindGuid;
		public override string RuntimeName => attachableProcessInfo.RuntimeName;
		public override string Name => attachableProcessInfo.Name;
		public override string Title => attachableProcessInfo.Title;
		public override string Filename => attachableProcessInfo.Filename;
		public override DbgArchitecture Architecture => attachableProcessInfo.Architecture;
		public override DbgOperatingSystem OperatingSystem => attachableProcessInfo.OperatingSystem;

		readonly DbgManager dbgManager;
		readonly AttachProgramOptions attachProgramOptions;
		readonly AttachableProcessInfo attachableProcessInfo;

		public AttachableProcessImpl(DbgManager dbgManager, AttachProgramOptions attachProgramOptions, AttachableProcessInfo attachableProcessInfo) {
			this.dbgManager = dbgManager ?? throw new ArgumentNullException(nameof(dbgManager));
			this.attachProgramOptions = attachProgramOptions ?? throw new ArgumentNullException(nameof(attachProgramOptions));
			this.attachableProcessInfo = attachableProcessInfo ?? throw new ArgumentNullException(nameof(attachableProcessInfo));
		}

		public override AttachToProgramOptions GetOptions() => attachProgramOptions.GetOptions();
		public override void Attach() => dbgManager.Start(GetOptions());
	}
}
