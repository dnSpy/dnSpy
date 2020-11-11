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
using dnSpy.Contracts.Hex.Intellisense;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Hex.Intellisense {
	[Export(typeof(WpfHexViewCreationListener))]
	[VSTE.TextViewRole(PredefinedHexViewRoles.Editable)]
	[VSTE.TextViewRole(PredefinedHexViewRoles.CanHaveIntellisenseControllers)]
	sealed class HexIntellisenseControllerService : WpfHexViewCreationListener {
		readonly Lazy<HexIntellisenseControllerProvider>[] intellisenseControllerProviders;

		[ImportingConstructor]
		HexIntellisenseControllerService([ImportMany] IEnumerable<Lazy<HexIntellisenseControllerProvider>> intellisenseControllerProviders) => this.intellisenseControllerProviders = intellisenseControllerProviders.ToArray();

		sealed class HexViewState {
			readonly WpfHexView hexView;
			readonly HexIntellisenseController[] intellisenseControllers;

			public HexViewState(WpfHexView hexView, Lazy<HexIntellisenseControllerProvider>[] intellisenseControllerProviders) {
				this.hexView = hexView;
				var list = new List<HexIntellisenseController>(intellisenseControllerProviders.Length);
				foreach (var provider in intellisenseControllerProviders) {
					var controller = provider.Value.TryCreateIntellisenseController(hexView);
					if (controller is not null)
						list.Add(controller);
				}
				intellisenseControllers = list.ToArray();
				if (intellisenseControllers.Length != 0)
					hexView.Closed += HexView_Closed;
			}

			void HexView_Closed(object? sender, EventArgs e) {
				hexView.Closed -= HexView_Closed;
				foreach (var controller in intellisenseControllers)
					controller.Detach(hexView);
			}
		}

		public override void HexViewCreated(WpfHexView hexView) => new HexViewState(hexView, intellisenseControllerProviders);
	}
}
