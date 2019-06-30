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
using dndbg.COM.CorDebug;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.CorDebug;
using dnSpy.Debugger.DotNet.CorDebug.Properties;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl {
	static class DnThreadUtils {
		static readonly (CorDebugUserState flag, DbgStateInfo stateInfo)[] userStates = new (CorDebugUserState, DbgStateInfo)[] {
			(CorDebugUserState.USER_STOP_REQUESTED, new DbgStateInfo(CorThreadUserStates.StopRequested, dnSpy_Debugger_DotNet_CorDebug_Resources.Thread_UserState_StopRequested)),
			(CorDebugUserState.USER_SUSPEND_REQUESTED, new DbgStateInfo(CorThreadUserStates.SuspendRequested, dnSpy_Debugger_DotNet_CorDebug_Resources.Thread_UserState_SuspendRequested)),
			(CorDebugUserState.USER_BACKGROUND, new DbgStateInfo(CorThreadUserStates.Background, dnSpy_Debugger_DotNet_CorDebug_Resources.Thread_UserState_Background)),
			(CorDebugUserState.USER_UNSTARTED, new DbgStateInfo(CorThreadUserStates.Unstarted, dnSpy_Debugger_DotNet_CorDebug_Resources.Thread_UserState_Unstarted)),
			(CorDebugUserState.USER_STOPPED, new DbgStateInfo(CorThreadUserStates.Stopped, dnSpy_Debugger_DotNet_CorDebug_Resources.Thread_UserState_Stopped)),
			(CorDebugUserState.USER_WAIT_SLEEP_JOIN, new DbgStateInfo(CorThreadUserStates.WaitSleepJoin, dnSpy_Debugger_DotNet_CorDebug_Resources.Thread_UserState_WaitSleepJoin)),
			(CorDebugUserState.USER_SUSPENDED, new DbgStateInfo(CorThreadUserStates.Suspended, dnSpy_Debugger_DotNet_CorDebug_Resources.Thread_UserState_Suspended)),
			(CorDebugUserState.USER_UNSAFE_POINT, new DbgStateInfo(CorThreadUserStates.UnsafePoint, dnSpy_Debugger_DotNet_CorDebug_Resources.Thread_UserState_UnsafePoint)),
			(CorDebugUserState.USER_THREADPOOL, new DbgStateInfo(CorThreadUserStates.ThreadPool, dnSpy_Debugger_DotNet_CorDebug_Resources.Thread_UserState_ThreadPool)),
		};
		static readonly ReadOnlyCollection<DbgStateInfo> emptyState = new ReadOnlyCollection<DbgStateInfo>(Array.Empty<DbgStateInfo>());

		public static ReadOnlyCollection<DbgStateInfo> GetState(CorDebugUserState state) {
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
