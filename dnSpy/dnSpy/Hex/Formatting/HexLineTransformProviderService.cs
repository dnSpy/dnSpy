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
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Formatting;
using dnSpy.Hex.MEF;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSTF = Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Hex.Formatting {
	abstract class HexLineTransformProvider : HexLineTransformSource {
	}

	abstract class HexLineTransformProviderService {
		public abstract HexLineTransformProvider Create(WpfHexView hexView, bool removeExtraTextLineVerticalPixels);
	}

	[Export(typeof(HexLineTransformProviderService))]
	sealed class HexLineTransformProviderServiceImpl : HexLineTransformProviderService {
		readonly Lazy<HexLineTransformSourceProvider, ITextViewRoleMetadata>[] lineTransformSourceProviders;

		[ImportingConstructor]
		HexLineTransformProviderServiceImpl([ImportMany] IEnumerable<Lazy<HexLineTransformSourceProvider, ITextViewRoleMetadata>> lineTransformSourceProviders) => this.lineTransformSourceProviders = lineTransformSourceProviders.ToArray();

		public override HexLineTransformProvider Create(WpfHexView hexView, bool removeExtraTextLineVerticalPixels) {
			var list = new List<HexLineTransformSource>();
			foreach (var lz in lineTransformSourceProviders) {
				if (!hexView.Roles.ContainsAny(lz.Metadata.TextViewRoles))
					continue;
				var source = lz.Value.Create(hexView);
				if (source != null)
					list.Add(source);
			}
			return new HexLineTransformProviderImpl(list.ToArray(), removeExtraTextLineVerticalPixels);
		}

		sealed class HexLineTransformProviderImpl : HexLineTransformProvider {
			readonly HexLineTransformSource[] lineTransformSources;
			readonly bool removeExtraTextLineVerticalPixels;

			public HexLineTransformProviderImpl(HexLineTransformSource[] lineTransformSources, bool removeExtraTextLineVerticalPixels) {
				this.lineTransformSources = lineTransformSources ?? throw new ArgumentNullException(nameof(lineTransformSources));
				this.removeExtraTextLineVerticalPixels = removeExtraTextLineVerticalPixels;
			}

			public override VSTF.LineTransform GetLineTransform(HexViewLine line, double yPosition, VSTE.ViewRelativePosition placement) {
				var transform = removeExtraTextLineVerticalPixels ?
					new VSTF.LineTransform(0, 0, line.DefaultLineTransform.VerticalScale, line.DefaultLineTransform.Right) :
					line.DefaultLineTransform;
				foreach (var source in lineTransformSources)
					transform = VSTF.LineTransform.Combine(transform, source.GetLineTransform(line, yPosition, placement));
				return transform;
			}
		}
	}
}
