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
using dnSpy.Contracts.Debugger.CorDebug;
using DE = dndbg.Engine;

namespace dnSpy.Debugger.CorDebug.Impl {
	static class BreakProcessKindHelpers {
		public static DE.BreakProcessKind ToDndbg(this BreakProcessKind kind) {
			switch (kind) {
			case BreakProcessKind.None:						return DE.BreakProcessKind.None;
			case BreakProcessKind.CreateProcess:			return DE.BreakProcessKind.CreateProcess;
			case BreakProcessKind.CreateAppDomain:			return DE.BreakProcessKind.CreateAppDomain;
			case BreakProcessKind.LoadModule:				return DE.BreakProcessKind.LoadModule;
			case BreakProcessKind.LoadClass:				return DE.BreakProcessKind.LoadClass;
			case BreakProcessKind.CreateThread:				return DE.BreakProcessKind.CreateThread;
			case BreakProcessKind.ExeLoadModule:			return DE.BreakProcessKind.ExeLoadModule;
			case BreakProcessKind.ExeLoadClass:				return DE.BreakProcessKind.ExeLoadClass;
			case BreakProcessKind.ModuleCctorOrEntryPoint:	return DE.BreakProcessKind.ModuleCctorOrEntryPoint;
			case BreakProcessKind.EntryPoint:				return DE.BreakProcessKind.EntryPoint;
			default: throw new ArgumentOutOfRangeException(nameof(kind));
			}
		}
	}
}
