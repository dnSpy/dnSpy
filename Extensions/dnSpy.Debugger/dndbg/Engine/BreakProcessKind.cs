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

namespace dndbg.Engine {
	enum BreakProcessKind {
		/// <summary>
		/// Don't break
		/// </summary>
		None,
		/// <summary>
		/// Break at the first CreateProcess event
		/// </summary>
		CreateProcess,
		/// <summary>
		/// Break at the first CreateAppDomain event
		/// </summary>
		CreateAppDomain,
		/// <summary>
		/// Break at the first LoadModule event
		/// </summary>
		LoadModule,
		/// <summary>
		/// Break at the first LoadClass event
		/// </summary>
		LoadClass,
		/// <summary>
		/// Break at the first CreateThread event
		/// </summary>
		CreateThread,
		/// <summary>
		/// Break at the debugged executable's LoadModule event
		/// </summary>
		ExeLoadModule,
		/// <summary>
		/// Break at the debugged executable's first LoadClass event
		/// </summary>
		ExeLoadClass,
		/// <summary>
		/// Break at the module .cctor or entry point if there's no module .cctor
		/// </summary>
		ModuleCctorOrEntryPoint,
		/// <summary>
		/// Break at the entry point
		/// </summary>
		EntryPoint,

		/// <summary>
		/// Always last and shouldn't be used except to count the number of elements in this enum
		/// </summary>
		Last,
	}
}
