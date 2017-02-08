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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IAdornmentLayerDefinitionService))]
	sealed class AdornmentLayerDefinitionService : IAdornmentLayerDefinitionService {
		readonly Lazy<AdornmentLayerDefinition, IAdornmentLayersMetadata>[] adornmentLayerDefinitions;

		[ImportingConstructor]
		AdornmentLayerDefinitionService([ImportMany] IEnumerable<Lazy<AdornmentLayerDefinition, IAdornmentLayersMetadata>> adornmentLayerDefinitions) {
			this.adornmentLayerDefinitions = Orderer.Order(adornmentLayerDefinitions).ToArray();
		}

		public MetadataAndOrder<IAdornmentLayersMetadata>? GetLayerDefinition(string name) {
			for (int i = 0; i < adornmentLayerDefinitions.Length; i++) {
				var def = adornmentLayerDefinitions[i];
				if (StringComparer.Ordinal.Equals(name, def.Metadata.Name))
					return new MetadataAndOrder<IAdornmentLayersMetadata>(def.Metadata, i);
			}
			return null;
		}
	}
}
