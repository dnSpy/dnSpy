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
using System.Linq;
using dnSpy.Contracts.Debugger.DotNet;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// <see cref="DbgRuntime"/> extension methods
	/// </summary>
	public static class DbgRuntimeDotNetExtensions {
		/// <summary>
		/// Returns true if <paramref name="runtime"/> is a .NET runtime
		/// </summary>
		/// <param name="runtime">Runtime</param>
		/// <returns></returns>
		public static bool IsDotNetRuntime(this DbgRuntime runtime) => runtime.Tags.Any(a => StringComparer.Ordinal.Equals(PredefinedDotNetDbgRuntimeTags.DotNet, a));
	}
}
