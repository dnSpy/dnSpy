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

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Contains a runtime and allows creating new app domains
	/// </summary>
	public abstract class DmdRuntimeController {
		/// <summary>
		/// Gets the runtime
		/// </summary>
		public abstract DmdRuntime Runtime { get; }

		/// <summary>
		/// Creates an app domain
		/// </summary>
		/// <param name="id">AppDomain id, must be a unique identifier</param>
		/// <returns></returns>
		public abstract DmdAppDomainController CreateAppDomain(int id);
	}
}
