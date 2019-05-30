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
using dnSpy.Contracts.Hex.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	[Export(typeof(HexStructureInfoAggregatorFactory))]
	sealed class HexStructureInfoAggregatorFactoryImpl : HexStructureInfoAggregatorFactory {
		readonly Lazy<HexStructureInfoProviderFactory, VSUTIL.IOrderable>[] hexStructureInfoProviderFactories;

		[ImportingConstructor]
		HexStructureInfoAggregatorFactoryImpl([ImportMany] IEnumerable<Lazy<HexStructureInfoProviderFactory, VSUTIL.IOrderable>> hexStructureInfoProviderFactories) => this.hexStructureInfoProviderFactories = VSUTIL.Orderer.Order(hexStructureInfoProviderFactories).ToArray();

		public override HexStructureInfoAggregator Create(HexView hexView) {
			if (hexView is null)
				throw new ArgumentNullException(nameof(hexView));
			return hexView.Properties.GetOrCreateSingletonProperty(typeof(HexStructureInfoAggregatorImpl), () => {
				var list = new List<HexStructureInfoProvider>(hexStructureInfoProviderFactories.Length);
				foreach (var lz in hexStructureInfoProviderFactories) {
					var provider = lz.Value.Create(hexView);
					if (!(provider is null))
						list.Add(provider);
				}
				return new HexStructureInfoAggregatorImpl(list.ToArray());
			});
		}
	}
}
