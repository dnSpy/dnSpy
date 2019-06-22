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
	[Export(typeof(HexReferenceHandlerService))]
	sealed class HexReferenceHandlerServiceImpl : HexReferenceHandlerService {
		readonly Lazy<HexReferenceHandler, VSUTIL.IOrderable>[] hexReferenceHandlers;
		readonly Lazy<HexReferenceConverter>[] hexReferenceConverters;

		[ImportingConstructor]
		HexReferenceHandlerServiceImpl([ImportMany] IEnumerable<Lazy<HexReferenceHandler, VSUTIL.IOrderable>> hexReferenceHandlers, [ImportMany] IEnumerable<Lazy<HexReferenceConverter>> hexReferenceConverters) {
			this.hexReferenceHandlers = VSUTIL.Orderer.Order(hexReferenceHandlers).ToArray();
			this.hexReferenceConverters = hexReferenceConverters.ToArray();
		}

		public override bool Handle(HexView hexView, object reference, IList<string>? tags) {
			if (hexView is null)
				throw new ArgumentNullException(nameof(hexView));
			if (reference is null)
				throw new ArgumentNullException(nameof(reference));
			object? newRef = ConvertReference(hexView, reference);
			if (newRef is null)
				return false;
			if (tags is null)
				tags = Array.Empty<string>();
			foreach (var lz in hexReferenceHandlers) {
				if (lz.Value.Handle(hexView, newRef, tags))
					return true;
			}
			return false;
		}

		object? ConvertReference(HexView hexView, object reference) {
			foreach (var lz in hexReferenceConverters) {
				var newRef = lz.Value.Convert(hexView, reference);
				if (newRef != reference)
					return newRef;
			}
			return reference;
		}
	}
}
