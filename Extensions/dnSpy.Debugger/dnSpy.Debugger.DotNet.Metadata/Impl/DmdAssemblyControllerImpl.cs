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

using System;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdAssemblyControllerImpl : DmdAssemblyController {
		public override DmdModuleController ModuleController => moduleController;
		public override DmdAssembly Assembly => assembly;

		readonly DmdAppDomainImpl appDomain;
		readonly DmdAssemblyImpl assembly;
		readonly DmdModuleControllerImpl moduleController;

		public DmdAssemblyControllerImpl(DmdAppDomainImpl appDomain, DmdLazyMetadataReader metadataReader, bool isInMemory, bool isDynamic, string fullyQualifiedName, string assemblyLocation) {
			if (appDomain == null)
				throw new ArgumentNullException(nameof(appDomain));
			if (metadataReader == null)
				throw new ArgumentNullException(nameof(metadataReader));
			if (fullyQualifiedName == null)
				throw new ArgumentNullException(nameof(fullyQualifiedName));
			if (assemblyLocation == null)
				throw new ArgumentNullException(nameof(assemblyLocation));
			this.appDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));
			assembly = new DmdAssemblyImpl(appDomain, metadataReader, assemblyLocation);
			moduleController = new DmdModuleControllerImpl(assembly, metadataReader, isInMemory, isDynamic, fullyQualifiedName);
			metadataReader.SetModule(moduleController.ModuleImpl);
			appDomain.Add(assembly);
		}

		public override void Remove() => appDomain.Remove(assembly);
	}
}
