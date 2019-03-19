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
using dnSpy.Contracts.Hex.Intellisense;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Intellisense {
	abstract class HexIntellisensePresenterFactoryService {
		public abstract HexIntellisensePresenter TryCreateIntellisensePresenter(HexIntellisenseSession session);
	}

	[Export(typeof(HexIntellisensePresenterFactoryService))]
	sealed class HexIntellisensePresenterFactoryServiceImpl : HexIntellisensePresenterFactoryService {
		readonly Lazy<HexIntellisensePresenterProvider, VSUTIL.IOrderable>[] intellisensePresenterProviders;

		[ImportingConstructor]
		HexIntellisensePresenterFactoryServiceImpl([ImportMany] IEnumerable<Lazy<HexIntellisensePresenterProvider, VSUTIL.IOrderable>> intellisensePresenterProviders) => this.intellisensePresenterProviders = VSUTIL.Orderer.Order(intellisensePresenterProviders).ToArray();

		public override HexIntellisensePresenter TryCreateIntellisensePresenter(HexIntellisenseSession session) {
			if (session == null)
				throw new ArgumentNullException(nameof(session));
			foreach (var lz in intellisensePresenterProviders) {
				var presenter = lz.Value.TryCreateIntellisensePresenter(session);
				if (presenter != null)
					return presenter;
			}
			return null;
		}
	}
}
