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

using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Files.Tabs;

namespace dnSpy.Debugger.CallStack {
	static class FrameUtils {
		public static bool GoTo(IFileTabManager fileTabManager, IModuleLoader moduleLoader, CorFrame frame, bool newTab) {
			if (GoToIL(fileTabManager, moduleLoader, frame, newTab))
				return true;

			//TODO: eg. native frame or internal frame

			return false;
		}

		public static bool CanGoToIL(CorFrame frame) {
			if (frame == null)
				return false;
			if (!frame.IsILFrame)
				return false;
			var ip = frame.ILFrameIP;
			if (!ip.IsExact && !ip.IsApproximate && !ip.IsProlog && !ip.IsEpilog)
				return false;
			if (frame.Token == 0)
				return false;

			return true;
		}

		public static bool GoToIL(IFileTabManager fileTabManager, IModuleLoader moduleLoader, CorFrame frame, bool newTab) {
			if (!CanGoToIL(frame))
				return false;

			var func = frame.Function;
			if (func == null)
				return false;

			return DebugUtils.GoToIL(fileTabManager, moduleLoader.LoadModule(func.Module, true), frame.Token, frame.GetILOffset(moduleLoader), newTab);
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

		public static uint GetILOffset(this CorFrame frame, IModuleLoader moduleLoader) {
			var ip = frame.ILFrameIP;
			if (ip.IsExact || ip.IsApproximate)
				return ip.Offset;
			if (ip.IsProlog)
				return 0;

			if (ip.IsEpilog) {
				var func = frame.Function;
				var file = func == null ? null : moduleLoader.LoadModule(func.Module, true);
				var mod = file == null ? null : file.ModuleDef;
				var md = mod == null ? null : mod.ResolveToken(frame.Token) as MethodDef;
				if (md != null && md.Body != null && md.Body.Instructions.Count > 0)
					return md.Body.Instructions[md.Body.Instructions.Count - 1].Offset;
			}

			return uint.MaxValue;
		}
	}
}
