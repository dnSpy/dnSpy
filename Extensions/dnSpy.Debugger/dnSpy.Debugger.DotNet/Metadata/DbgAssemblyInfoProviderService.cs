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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Metadata {
	abstract class DbgAssemblyInfoProviderService {
		public abstract DbgAssemblyInfoProvider? Create(DbgRuntime runtime);
	}

	[Export(typeof(DbgAssemblyInfoProviderService))]
	sealed class DbgAssemblyInfoProviderServiceImpl : DbgAssemblyInfoProviderService {
		readonly Lazy<DbgAssemblyInfoProviderFactory>[] dbgAssemblyInfoProviderFactories;

		[ImportingConstructor]
		DbgAssemblyInfoProviderServiceImpl([ImportMany] IEnumerable<Lazy<DbgAssemblyInfoProviderFactory>> dbgAssemblyInfoProviderFactories) =>
			this.dbgAssemblyInfoProviderFactories = dbgAssemblyInfoProviderFactories.ToArray();

		public override DbgAssemblyInfoProvider? Create(DbgRuntime runtime) {
			if (runtime is null)
				throw new ArgumentNullException(nameof(runtime));
			foreach (var lz in dbgAssemblyInfoProviderFactories) {
				var provider = lz.Value.Create(runtime);
				if (provider is not null)
					return provider;
			}
			return null;
		}
	}
}
