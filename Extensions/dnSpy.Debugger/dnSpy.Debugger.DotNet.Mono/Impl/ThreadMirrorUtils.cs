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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Mono;
using dnSpy.Debugger.DotNet.Mono.Properties;

namespace dnSpy.Debugger.DotNet.Mono.Impl {
	static class ThreadMirrorUtils {
		static readonly (ThreadState flag, DbgStateInfo stateInfo)[] userStates = new (ThreadState, DbgStateInfo)[] {
			(ThreadState.StopRequested, new DbgStateInfo(ThreadStates.StopRequested, dnSpy_Debugger_DotNet_Mono_Resources.Thread_UserState_StopRequested)),
			(ThreadState.SuspendRequested, new DbgStateInfo(ThreadStates.SuspendRequested, dnSpy_Debugger_DotNet_Mono_Resources.Thread_UserState_SuspendRequested)),
			(ThreadState.Background, new DbgStateInfo(ThreadStates.Background, dnSpy_Debugger_DotNet_Mono_Resources.Thread_UserState_Background)),
			(ThreadState.Unstarted, new DbgStateInfo(ThreadStates.Unstarted, dnSpy_Debugger_DotNet_Mono_Resources.Thread_UserState_Unstarted)),
			(ThreadState.Stopped, new DbgStateInfo(ThreadStates.Stopped, dnSpy_Debugger_DotNet_Mono_Resources.Thread_UserState_Stopped)),
			(ThreadState.WaitSleepJoin, new DbgStateInfo(ThreadStates.WaitSleepJoin, dnSpy_Debugger_DotNet_Mono_Resources.Thread_UserState_WaitSleepJoin)),
			(ThreadState.Suspended, new DbgStateInfo(ThreadStates.Suspended, dnSpy_Debugger_DotNet_Mono_Resources.Thread_UserState_Suspended)),
			(ThreadState.AbortRequested, new DbgStateInfo(ThreadStates.AbortRequested, dnSpy_Debugger_DotNet_Mono_Resources.Thread_UserState_AbortRequested)),
			(ThreadState.Aborted, new DbgStateInfo(ThreadStates.Aborted, dnSpy_Debugger_DotNet_Mono_Resources.Thread_UserState_Aborted)),
		};
		static readonly ReadOnlyCollection<DbgStateInfo> emptyState = new ReadOnlyCollection<DbgStateInfo>(Array.Empty<DbgStateInfo>());

		public static ReadOnlyCollection<DbgStateInfo> GetState(ThreadState state) {
			if (state == 0)
				return emptyState;

			var list = new List<DbgStateInfo>();
			foreach (var info in userStates) {
				if ((state & info.flag) != 0) {
					state &= ~info.flag;
					list.Add(info.stateInfo);
				}
			}
			if (state != 0)
				list.Add(new DbgStateInfo("0x" + ((int)state).ToString("X")));

			return new ReadOnlyCollection<DbgStateInfo>(list);
		}
	}
}
