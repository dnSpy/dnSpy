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

using dndbg.Engine;

namespace dnSpy.Debugger.CallStack {
	static class FrameUtils {
		public static bool GoTo(CorFrame frame) {
			if (GoToIL(frame))
				return true;

			//TODO: eg. native frame or internal frame

			return false;
		}

		public static bool CanGoToIL(CorFrame frame) {
			if (frame == null)
				return false;
			if (!frame.IsILFrame)
				return false;
			if (!frame.ILFrameIP.IsExact && !frame.ILFrameIP.IsApproximate)
				return false;
			if (frame.Token == 0)
				return false;

			return true;
		}

		public static bool GoToIL(CorFrame frame) {
			if (!CanGoToIL(frame))
				return false;

			var serAsm = frame.GetSerializedDnModuleWithAssembly();
			if (serAsm == null)
				return false;

			return DebugUtils.GoToIL(serAsm.Value, frame.Token, frame.ILFrameIP.Offset);
		}

		public static bool CanGoToDisasm(CorFrame frame) {
			if (frame == null)
				return false;
			if (!frame.IsNativeFrame)
				return false;

			return false;//TODO:
		}

		public static bool GoToDisasm(CorFrame frame) {
			if (!CanGoToDisasm(frame))
				return false;

			return false;//TODO:
		}
	}
}
