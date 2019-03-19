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
using dnSpy.Debugger.DotNet.Mono.Impl;
using dnSpy.Debugger.DotNet.Mono.Impl.Attach;

namespace dnSpy.Debugger.DotNet.Mono.Dialogs.AttachToProcess {
	sealed class UnityAttachProgramOptionsImpl : AttachProgramOptions {
		public override int ProcessId { get; }
		public override RuntimeId RuntimeId { get; }
		public override Guid RuntimeGuid => PredefinedDbgRuntimeGuids.DotNetUnity_Guid;
		public override Guid RuntimeKindGuid => PredefinedDbgRuntimeKindGuids.DotNet_Guid;
		public override string RuntimeName { get; }

		readonly string address;
		readonly ushort port;

		public UnityAttachProgramOptionsImpl(int pid, string address, ushort port, string name) {
			ProcessId = pid;
			RuntimeId = new DotNetMonoRuntimeId() {
				Address = address,
				Port = port,
			};
			RuntimeName = name;
			this.address = address;
			this.port = port;
		}

		public override AttachToProgramOptions GetOptions() => new UnityAttachToProgramOptions {
			Address = address,
			Port = port,
		};
	}
}
