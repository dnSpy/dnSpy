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

namespace dnSpy.Contracts.Debugger.Engine {
	/// <summary>
	/// Message flags
	/// </summary>
	[Flags]
	public enum DbgEngineMessageFlags {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Set if the process should be paused, false if other code gets to decide if it should pause
		/// </summary>
		Pause					= 0x00000001,

		/// <summary>
		/// Set if the process should continue if possible, eg. it's a func-eval and an event occured.
		/// </summary>
		Continue				= 0x00000002,

		/// <summary>
		/// Set if the process is running
		/// </summary>
		Running					= 0x00000004,
	}
}
