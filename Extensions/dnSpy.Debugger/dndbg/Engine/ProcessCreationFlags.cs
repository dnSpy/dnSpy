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

namespace dndbg.Engine {
	[Flags]
	public enum ProcessCreationFlags : uint {
		DEBUG_PROCESS = 0x00000001,
		DEBUG_ONLY_THIS_PROCESS = 0x00000002,
		CREATE_SUSPENDED = 0x00000004,
		DETACHED_PROCESS = 0x00000008,

		CREATE_NEW_CONSOLE = 0x00000010,
		NORMAL_PRIORITY_CLASS = 0x00000020,
		IDLE_PRIORITY_CLASS = 0x00000040,
		HIGH_PRIORITY_CLASS = 0x00000080,

		REALTIME_PRIORITY_CLASS = 0x00000100,
		CREATE_NEW_PROCESS_GROUP = 0x00000200,
		CREATE_UNICODE_ENVIRONMENT = 0x00000400,
		CREATE_SEPARATE_WOW_VDM = 0x00000800,

		CREATE_SHARED_WOW_VDM = 0x00001000,
		CREATE_FORCEDOS = 0x00002000,
		BELOW_NORMAL_PRIORITY_CLASS = 0x00004000,
		ABOVE_NORMAL_PRIORITY_CLASS = 0x00008000,

		INHERIT_PARENT_AFFINITY = 0x00010000,
		INHERIT_CALLER_PRIORITY = 0x00020000,
		CREATE_PROTECTED_PROCESS = 0x00040000,
		EXTENDED_STARTUPINFO_PRESENT = 0x00080000,

		PROCESS_MODE_BACKGROUND_BEGIN = 0x00100000,
		PROCESS_MODE_BACKGROUND_END = 0x00200000,

		CREATE_BREAKAWAY_FROM_JOB = 0x01000000,
		CREATE_PRESERVE_CODE_AUTHZ_LEVEL = 0x02000000,
		CREATE_DEFAULT_ERROR_MODE = 0x04000000,
		CREATE_NO_WINDOW = 0x08000000,

		PROFILE_USER = 0x10000000,
		PROFILE_KERNEL = 0x20000000,
		PROFILE_SERVER = 0x40000000,
		CREATE_IGNORE_SYSTEM_DEFAULT = 0x80000000,
	}
}
