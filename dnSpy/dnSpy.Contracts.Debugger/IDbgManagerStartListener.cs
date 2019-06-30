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

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// All exported classes implementing this interface get created the first time
	/// <see cref="DbgManager.Start(DebugProgramOptions)"/> gets called.
	/// </summary>
	public interface IDbgManagerStartListener {
		/// <summary>
		/// Called the first time <see cref="DbgManager.Start(DebugProgramOptions)"/> gets called.
		/// The code has a chance to hook events and do other initialization before a program
		/// gets debugged.
		/// </summary>
		/// <param name="dbgManager">Debug manager instance</param>
		void OnStart(DbgManager dbgManager);
	}
}
