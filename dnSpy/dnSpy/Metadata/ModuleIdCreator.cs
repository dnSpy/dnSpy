/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnlib.DotNet;
using dnSpy.Contracts.Metadata;

namespace dnSpy.Metadata {
	[Export(typeof(IModuleIdCreator))]
	sealed class ModuleIdCreator : IModuleIdCreator {
		readonly Lazy<IModuleIdFactoryProvider, IModuleIdFactoryProviderMetadata>[] moduleIdFactoryProviders;
		IModuleIdFactory[] factories;

		[ImportingConstructor]
		ModuleIdCreator([ImportMany] IEnumerable<Lazy<IModuleIdFactoryProvider, IModuleIdFactoryProviderMetadata>> moduleIdFactoryProviders) {
			this.moduleIdFactoryProviders = moduleIdFactoryProviders.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public ModuleId Create(ModuleDef module) {
			if (module == null)
				return new ModuleId();

			if (factories == null) {
				var list = new List<IModuleIdFactory>(moduleIdFactoryProviders.Length);
				foreach (var provider in moduleIdFactoryProviders) {
					var factory = provider.Value.Create();
					if (factory != null)
						list.Add(factory);
				}
				factories = list.ToArray();
			}

			foreach (var factory in factories) {
				var id = factory.Create(module);
				if (id != null)
					return id.Value;
			}

			return ModuleId.CreateFromFile(module);
		}
	}
}
