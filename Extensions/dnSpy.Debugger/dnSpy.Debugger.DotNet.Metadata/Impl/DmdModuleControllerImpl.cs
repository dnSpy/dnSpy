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
	sealed class DmdModuleControllerImpl : DmdModuleController {
		public override DmdModule Module => module;

		readonly DmdAssemblyImpl assembly;
		readonly DmdModuleImpl module;

		public DmdModuleControllerImpl(DmdAssemblyImpl assembly, DmdMetadataReader metadataReader, bool isInMemory, bool isDynamic, string fullyQualifiedName) {
			if (metadataReader == null)
				throw new ArgumentNullException(nameof(metadataReader));
			if (fullyQualifiedName == null)
				throw new ArgumentNullException(nameof(fullyQualifiedName));
			this.assembly = assembly ?? throw new ArgumentNullException(nameof(assembly));
			module = new DmdModuleImpl(assembly, metadataReader, isInMemory, isDynamic, fullyQualifiedName);
			assembly.Add(module);
		}

		public override void Remove() => assembly.Remove(module);
		public override void SetScopeName(string scopeName) =>
			module.SetScopeName(scopeName ?? throw new ArgumentNullException(nameof(scopeName)));
	}
}
