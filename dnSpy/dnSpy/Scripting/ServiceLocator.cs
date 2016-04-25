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
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Windows.Threading;
using dnSpy.Contracts.Scripting;
using dnSpy.Shared.Scripting;

namespace dnSpy.Scripting {
	[Export, Export(typeof(IServiceLocator)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class ServiceLocator : IServiceLocator {
		readonly Dispatcher dispatcher;

		ServiceLocator() {
			this.dispatcher = Dispatcher.CurrentDispatcher;
		}

		public T Resolve<T>() {
			Debug.Assert(compositionContainer != null);
			if (compositionContainer == null)
				throw new InvalidOperationException();
			return dispatcher.UI(() => compositionContainer.GetExportedValue<T>());
		}

		public T TryResolve<T>() {
			Debug.Assert(compositionContainer != null);
			if (compositionContainer == null)
				throw new InvalidOperationException();
			return dispatcher.UI(() => compositionContainer.GetExportedValueOrDefault<T>());
		}

		public void SetCompositionContainer(CompositionContainer compositionContainer) {
			if (compositionContainer == null)
				throw new ArgumentNullException();
			if (this.compositionContainer != null)
				throw new InvalidOperationException();
			this.compositionContainer = compositionContainer;
		}
		CompositionContainer compositionContainer;
	}
}
