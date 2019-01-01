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

using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.MVVM;
using dnSpy.Debugger.DotNet.Mono.Properties;

namespace dnSpy.Debugger.DotNet.Mono.Dialogs.DebugProgram {
	static class BreakProcessKindsUtils {
		public static readonly EnumVM[] BreakProcessKindList = new EnumVM[] {
			new EnumVM(PredefinedBreakKinds.DontBreak, dnSpy_Debugger_DotNet_Mono_Resources.DbgBreak_Dont),
			new EnumVM(PredefinedBreakKinds.CreateProcess, dnSpy_Debugger_DotNet_Mono_Resources.DbgBreak_CreateProcessEvent),
			new EnumVM(PredefinedBreakKinds.EntryPoint, dnSpy_Debugger_DotNet_Mono_Resources.DbgBreak_EntryPoint),
		};
	}
}
