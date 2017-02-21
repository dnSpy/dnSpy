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

namespace dnSpy.Contracts.Debugger.UI {
	/// <summary>
	/// Predefined generic debug engine guids
	/// </summary>
	public static class PredefinedGenericDebugEngineGuids {
		/// <summary>
		/// .NET Framework or compatible framework (eg. Mono)
		/// </summary>
		public static readonly Guid DotNetFramework = new Guid("0F99555D-5523-4AAE-BD4C-0451B9D50126");

		/// <summary>
		/// .NET Core
		/// </summary>
		public static readonly Guid DotNetCore = new Guid("7D294510-4730-433B-85C1-61EC0B4F6C3C");
	}
}
