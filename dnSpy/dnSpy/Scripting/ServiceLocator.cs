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
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using dnSpy.Contracts.Scripting;
using Microsoft.VisualStudio.Composition;

namespace dnSpy.Scripting {
	[Export, Export(typeof(IServiceLocator))]
	sealed class ServiceLocator : IServiceLocator {
		Dispatcher dispatcher;

		public T Resolve<T>() {
			Debug.Assert(exportProvider != null);
			if (exportProvider == null)
				throw new InvalidOperationException();
			return dispatcher.UI(() => exportProvider.GetExportedValue<T>());
		}

		public T TryResolve<T>() {
			Debug.Assert(exportProvider != null);
			if (exportProvider == null)
				throw new InvalidOperationException();
			return dispatcher.UI(() => {
				// VS-MEF doesn't have GetExportedValueOrDefault()
				var res = exportProvider.GetExports<T, IDictionary<string, object>>(null).SingleOrDefault();
				if (res == null)
					return default;
				return res.Value;
			});
		}

		public void SetExportProvider(Dispatcher dispatcher, ExportProvider exportProvider) {
			this.dispatcher = dispatcher;
			if (this.exportProvider != null)
				throw new InvalidOperationException();
			this.exportProvider = exportProvider ?? throw new ArgumentNullException(nameof(exportProvider));
		}
		ExportProvider exportProvider;
	}
}
