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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using dnSpy.Contracts.Documents;
using dnSpy.UI;

namespace dnSpy.Documents {
	[Export(typeof(ReferenceNavigatorService))]
	sealed class ReferenceNavigatorServiceImpl : ReferenceNavigatorService {
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<ReferenceNavigator, IReferenceNavigatorMetadata>[] referenceNavigators;
		readonly Lazy<ReferenceConverter, IReferenceConverterMetadata>[] referenceConverters;

		[ImportingConstructor]
		ReferenceNavigatorServiceImpl(UIDispatcher uiDispatcher, [ImportMany] IEnumerable<Lazy<ReferenceNavigator, IReferenceNavigatorMetadata>> referenceNavigators, [ImportMany] IEnumerable<Lazy<ReferenceConverter, IReferenceConverterMetadata>> referenceConverters) {
			this.uiDispatcher = uiDispatcher;
			this.referenceNavigators = referenceNavigators.OrderBy(a => a.Metadata.Order).ToArray();
			this.referenceConverters = referenceConverters.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public override void GoTo(object reference, object[] options) => uiDispatcher.UI(() => GoToCore(reference, options));
		void GoToCore(object reference, object[] options) {
			uiDispatcher.VerifyAccess();
			reference = Convert(reference);
			if (reference == null)
				return;
			var roOptions = options == null || options.Length == 0 ? emptyOptions : new ReadOnlyCollection<object>(options);
			foreach (var lz in referenceNavigators) {
				if (lz.Value.GoTo(reference, roOptions))
					break;
			}
		}
		static readonly ReadOnlyCollection<object> emptyOptions = new ReadOnlyCollection<object>(Array.Empty<object>());

		object Convert(object reference) {
			uiDispatcher.VerifyAccess();
			foreach (var lz in referenceConverters) {
				if (reference == null)
					break;
				lz.Value.Convert(ref reference);
			}
			return reference;
		}
	}
}
