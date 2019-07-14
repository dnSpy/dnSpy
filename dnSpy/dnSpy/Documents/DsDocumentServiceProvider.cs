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
using System.ComponentModel.Composition;
using dnSpy.Contracts.DnSpy.Metadata;
using dnSpy.Contracts.Documents;

namespace dnSpy.Documents {
	[Export(typeof(IDsDocumentServiceProvider))]
	sealed class DsDocumentServiceProvider : IDsDocumentServiceProvider {
		readonly IDsDocumentServiceSettings documentServiceSettings;
		readonly IDsDocumentProvider[] documentProviders;
		readonly Lazy<IRuntimeAssemblyResolver, IRuntimeAssemblyResolverMetadata>[] runtimeAsmResolvers;

		[ImportingConstructor]
		DsDocumentServiceProvider(IDsDocumentServiceSettings documentServiceSettings, [ImportMany] IDsDocumentProvider[] documentProviders, [ImportMany] Lazy<IRuntimeAssemblyResolver, IRuntimeAssemblyResolverMetadata>[] runtimeAsmResolvers) {
			this.documentServiceSettings = documentServiceSettings;
			this.documentProviders = documentProviders;
			this.runtimeAsmResolvers = runtimeAsmResolvers;
		}

		public IDsDocumentService Create() => new DsDocumentService(documentServiceSettings, documentProviders, runtimeAsmResolvers);
	}
}
