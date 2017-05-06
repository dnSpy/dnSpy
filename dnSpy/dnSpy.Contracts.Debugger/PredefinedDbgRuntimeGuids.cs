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

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Predefined <see cref="DbgRuntime"/> GUIDs (<see cref="DbgRuntime.Guid"/>)
	/// </summary>
	public static class PredefinedDbgRuntimeGuids {
		/// <summary>
		/// .NET Framework (CorDebug)
		/// </summary>
		public static readonly Guid DotNetFramework = new Guid("CD03ACDD-4F3A-4736-8591-4902B4DCC8C1");

		/// <summary>
		/// .NET Core (CorDebug)
		/// </summary>
		public static readonly Guid DotNetCore = new Guid("E0B4EB52-D1D9-42AB-B130-028CA31CF9F6");
	}
}
