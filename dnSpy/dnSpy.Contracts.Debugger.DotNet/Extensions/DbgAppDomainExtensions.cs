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

using dnSpy.Contracts.Debugger.DotNet;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Extension methods
	/// </summary>
	public static class DbgAppDomainExtensions {
		/// <summary>
		/// Gets the reflection app domain or null if this isn't a managed app domain
		/// </summary>
		/// <param name="appDomain">Debugger app domain</param>
		/// <returns></returns>
		public static DmdAppDomain? GetReflectionAppDomain(this DbgAppDomain appDomain) => (appDomain.InternalAppDomain as DbgDotNetInternalAppDomain)?.ReflectionAppDomain;

		/// <summary>
		/// Gets the internal .NET app domain or null if it's not a managed app domain
		/// </summary>
		/// <param name="appDomain"></param>
		/// <returns></returns>
		public static DbgDotNetInternalAppDomain? GetDotNetInternalAppDomain(this DbgAppDomain appDomain) => appDomain.InternalAppDomain as DbgDotNetInternalAppDomain;
	}
}
