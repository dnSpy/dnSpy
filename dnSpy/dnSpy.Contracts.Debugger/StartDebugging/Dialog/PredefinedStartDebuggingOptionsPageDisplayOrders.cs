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

namespace dnSpy.Contracts.Debugger.StartDebugging.Dialog {
	/// <summary>
	/// Predefined options page display order constants
	/// </summary>
	public static class PredefinedStartDebuggingOptionsPageDisplayOrders {
		/// <summary>
		/// .NET Framework debug engine
		/// </summary>
		public static readonly double DotNetFramework = 100000;

		/// <summary>
		/// .NET Core debug engine
		/// </summary>
		public static readonly double DotNetCore = 101000;

		/// <summary>
		/// Unity debug engine (start executable)
		/// </summary>
		public static readonly double DotNetUnity = 102000;

		/// <summary>
		/// Unity debug engine (connect to a waiting executable)
		/// </summary>
		public static readonly double DotNetUnityConnect = 103000;

		/// <summary>
		/// Mono debug engine (start executable)
		/// </summary>
		public static readonly double DotNetMono = 110000;

		/// <summary>
		/// Mono debug engine (connect to a waiting executable)
		/// </summary>
		public static readonly double DotNetMonoConnect = 111000;
	}
}
