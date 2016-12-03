/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

using dndbg.COM.CorDebug;

namespace dndbg.Engine {
	public struct CorExceptionObjectStackFrame {
		public CorModule Module { get; }
		public ulong IP { get; }
		public uint Token { get; }
		public bool IsLastForeignExceptionFrame { get; }

		public CorExceptionObjectStackFrame(CorDebugExceptionObjectStackFrame other) {
			Module = other.pModule == null ? null : new CorModule(other.pModule);
			IP = other.ip;
			Token = other.methodDef;
			IsLastForeignExceptionFrame = other.isLastForeignExceptionFrame;
		}
	}
}
