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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// A .NET runtime
	/// </summary>
	public abstract class DmdRuntime : DmdObject {
		/// <summary>
		/// Dummy abstract method to make sure no-one outside this assembly can create their own <see cref="DmdRuntime"/>
		/// </summary>
		private protected abstract void YouCantDeriveFromThisClass();

		/// <summary>
		/// Gets the size of a pointer in bytes
		/// </summary>
		public abstract int PointerSize { get; }

		/// <summary>
		/// Gets the machine
		/// </summary>
		public abstract DmdImageFileMachine Machine { get; }

		/// <summary>
		/// Gets all AppDomains
		/// </summary>
		/// <returns></returns>
		public abstract DmdAppDomain[] GetAppDomains();

		/// <summary>
		/// Returns an AppDomain or null if it doesn't exist
		/// </summary>
		/// <param name="id">AppDomain id</param>
		/// <returns></returns>
		public abstract DmdAppDomain GetAppDomain(int id);

		/// <summary>
		/// Creates an AppDomain
		/// </summary>
		/// <param name="id">AppDomain id, must be a unique identifier</param>
		/// <returns></returns>
		public abstract DmdAppDomain CreateAppDomain(int id);

		/// <summary>
		/// Removes an AppDomain
		/// </summary>
		/// <param name="appDomain">AppDomain to remove</param>
		public abstract void Remove(DmdAppDomain appDomain);
	}
}
