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
	abstract class DbgDynamicModuleProviderService {
		public abstract DbgDynamicModuleProvider Create(DbgRuntime runtime);
	}

	[Export(typeof(DbgDynamicModuleProviderService))]
	sealed class DbgDynamicModuleProviderServiceImpl : DbgDynamicModuleProviderService {
		readonly Lazy<DbgDynamicModuleProviderFactory>[] dbgDynamicModuleProviderFactories;

		[ImportingConstructor]
		DbgDynamicModuleProviderServiceImpl([ImportMany] IEnumerable<Lazy<DbgDynamicModuleProviderFactory>> dbgDynamicModuleProviderFactories) =>
			this.dbgDynamicModuleProviderFactories = dbgDynamicModuleProviderFactories.ToArray();

		public override DbgDynamicModuleProvider Create(DbgRuntime runtime) {
			if (runtime == null)
				throw new ArgumentNullException(nameof(runtime));
			foreach (var lz in dbgDynamicModuleProviderFactories) {
				var provider = lz.Value.Create(runtime);
				if (provider != null)
					return provider;
			}
			return null;
		}
	}
}
