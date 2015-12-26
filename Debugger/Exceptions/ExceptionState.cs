/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

namespace dnSpy.Debugger.Exceptions {
	[Flags]
	enum ExceptionState : uint {	// see msdbg.h or https://msdn.microsoft.com/en-us/library/vstudio/bb146192%28v=vs.140%29.aspx
		EXCEPTION_NONE						= 0x0000,
		EXCEPTION_STOP_FIRST_CHANCE			= 0x0001,
		EXCEPTION_STOP_SECOND_CHANCE		= 0x0002,
		EXCEPTION_STOP_USER_FIRST_CHANCE	= 0x0010,
		EXCEPTION_STOP_USER_UNCAUGHT		= 0x0020,
		EXCEPTION_STOP_ALL					= 0x00FF,
		EXCEPTION_CANNOT_BE_CONTINUED		= 0x0100,

		// These are for exception types only
		EXCEPTION_CODE_SUPPORTED			= 0x1000,
		EXCEPTION_CODE_DISPLAY_IN_HEX		= 0x2000,
		EXCEPTION_JUST_MY_CODE_SUPPORTED	= 0x4000,
		EXCEPTION_MANAGED_DEBUG_ASSISTANT	= 0x8000,

		// These are no longer used
		EXCEPTION_STOP_FIRST_CHANCE_USE_PARENT = 0x0004,
		EXCEPTION_STOP_SECOND_CHANCE_USE_PARENT = 0x0008,
		EXCEPTION_STOP_USER_FIRST_CHANCE_USE_PARENT = 0x0040,
		EXCEPTION_STOP_USER_UNCAUGHT_USE_PARENT = 0x0080,
	}
}
