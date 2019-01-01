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
using dnSpy.Text;
using dnSpy.Text.MEF;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Language.Intellisense {
	interface IIntellisensePresenterFactoryService {
		IIntellisensePresenter TryCreateIntellisensePresenter(IIntellisenseSession session);
	}

	[Export(typeof(IIntellisensePresenterFactoryService))]
	sealed class IntellisensePresenterFactoryService : IIntellisensePresenterFactoryService {
		readonly Lazy<IIntellisensePresenterProvider, IOrderableContentTypeMetadata>[] intellisensePresenterProviders;

		[ImportingConstructor]
		IntellisensePresenterFactoryService([ImportMany] IEnumerable<Lazy<IIntellisensePresenterProvider, IOrderableContentTypeMetadata>> intellisensePresenterProviders) => this.intellisensePresenterProviders = Orderer.Order(intellisensePresenterProviders).ToArray();

		public IIntellisensePresenter TryCreateIntellisensePresenter(IIntellisenseSession session) {
			if (session == null)
				throw new ArgumentNullException(nameof(session));
			var contentTypes = session.TextView.BufferGraph.GetTextBuffers(a => session.GetTriggerPoint(a) != null).Select(a => a.ContentType).ToArray();
			foreach (var lz in intellisensePresenterProviders) {
				foreach (var contentType in contentTypes) {
					if (!contentType.IsOfAnyType(lz.Metadata.ContentTypes))
						continue;
					var presenter = lz.Value.TryCreateIntellisensePresenter(session);
					if (presenter != null)
						return presenter;
				}
			}
			return null;
		}
	}
}
