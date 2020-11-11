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

namespace dnSpy.Contracts.Debugger.DotNet {
	/// <summary>
	/// Predefined <see cref="DbgRuntime"/> tags (<see cref="DbgRuntime.Tags"/>)
	/// </summary>
	public static class PredefinedDotNetDbgRuntimeTags {
		/// <summary>
		/// .NET runtime (any)
		/// </summary>
		public const string DotNetBase = nameof(DotNetBase);

		/// <summary>
		/// .NET Framework runtime
		/// </summary>
		public const string DotNetFramework = nameof(DotNetFramework);

		/// <summary>
		/// .NET runtime
		/// </summary>
		public const string DotNet = nameof(DotNet);

		/// <summary>
		/// .NET Mono runtime
		/// </summary>
		public const string DotNetMono = nameof(DotNetMono);

		/// <summary>
		/// .NET Unity runtime
		/// </summary>
		public const string DotNetUnity = nameof(DotNetUnity);
	}
}
