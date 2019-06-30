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
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Intellisense;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Intellisense {
	[Export(typeof(HexQuickInfoBroker))]
	sealed class HexQuickInfoBrokerImpl : HexQuickInfoBroker {
		readonly Lazy<HexIntellisenseSessionStackMapService> intellisenseSessionStackMapService;
		readonly Lazy<HexIntellisensePresenterFactoryService> intellisensePresenterFactoryService;
		readonly Lazy<HexQuickInfoSourceProvider, VSUTIL.IOrderable>[] quickInfoSourceProviders;

		[ImportingConstructor]
		HexQuickInfoBrokerImpl(Lazy<HexIntellisenseSessionStackMapService> intellisenseSessionStackMapService, Lazy<HexIntellisensePresenterFactoryService> intellisensePresenterFactoryService, [ImportMany] IEnumerable<Lazy<HexQuickInfoSourceProvider, VSUTIL.IOrderable>> quickInfoSourceProviders) {
			this.intellisenseSessionStackMapService = intellisenseSessionStackMapService;
			this.intellisensePresenterFactoryService = intellisensePresenterFactoryService;
			this.quickInfoSourceProviders = VSUTIL.Orderer.Order(quickInfoSourceProviders).ToArray();
		}

		public override HexQuickInfoSession? TriggerQuickInfo(HexView hexView) {
			if (hexView is null)
				throw new ArgumentNullException(nameof(hexView));
			var triggerPoint = hexView.Caret.Position.Position.ActivePosition;
			return TriggerQuickInfo(hexView, triggerPoint, trackMouse: false);
		}

		public override HexQuickInfoSession? TriggerQuickInfo(HexView hexView, HexCellPosition triggerPoint, bool trackMouse) {
			if (hexView is null)
				throw new ArgumentNullException(nameof(hexView));
			if (triggerPoint.IsDefault)
				throw new ArgumentException();
			var session = CreateQuickInfoSession(hexView, triggerPoint, trackMouse);
			session.Start();
			return session.IsDismissed ? null : session;
		}

		public override HexQuickInfoSession CreateQuickInfoSession(HexView hexView, HexCellPosition triggerPoint, bool trackMouse) {
			if (hexView is null)
				throw new ArgumentNullException(nameof(hexView));
			if (triggerPoint.IsDefault)
				throw new ArgumentException();
			var stack = intellisenseSessionStackMapService.Value.GetStackForHexView(hexView);
			var session = new HexQuickInfoSessionImpl(hexView, triggerPoint, trackMouse, intellisensePresenterFactoryService.Value, quickInfoSourceProviders);
			stack.PushSession(session);
			return session;
		}

		public override bool IsQuickInfoActive(HexView hexView) {
			if (hexView is null)
				throw new ArgumentNullException(nameof(hexView));
			return GetSessions(hexView).Count != 0;
		}

		public override ReadOnlyCollection<HexQuickInfoSession> GetSessions(HexView hexView) {
			if (hexView is null)
				throw new ArgumentNullException(nameof(hexView));
			var stack = intellisenseSessionStackMapService.Value.GetStackForHexView(hexView);
			return new ReadOnlyCollection<HexQuickInfoSession>(stack.Sessions.OfType<HexQuickInfoSession>().ToArray());
		}
	}
}
