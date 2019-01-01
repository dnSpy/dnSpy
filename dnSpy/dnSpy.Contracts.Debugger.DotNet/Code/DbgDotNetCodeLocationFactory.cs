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

using dnSpy.Contracts.Metadata;

namespace dnSpy.Contracts.Debugger.DotNet.Code {
	/// <summary>
	/// Creates <see cref="DbgDotNetCodeLocation"/> instances
	/// </summary>
	public abstract class DbgDotNetCodeLocationFactory {
		/// <summary>
		/// Creates a new <see cref="DbgDotNetCodeLocation"/> instance
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of a method within the module</param>
		/// <param name="offset">IL offset of the location within the method body</param>
		/// <returns></returns>
		public DbgDotNetCodeLocation Create(ModuleId module, uint token, uint offset) =>
			Create(module, token, offset, DbgILOffsetMapping.Exact);

		/// <summary>
		/// Creates a new <see cref="DbgDotNetCodeLocation"/> instance
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of a method within the module</param>
		/// <param name="offset">IL offset of the location within the method body</param>
		/// <param name="ilOffsetMapping">IL offset mapping</param>
		/// <returns></returns>
		public abstract DbgDotNetCodeLocation Create(ModuleId module, uint token, uint offset, DbgILOffsetMapping ilOffsetMapping);
	}
}
