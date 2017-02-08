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

using System.Windows;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Properties;

namespace dnSpy.Documents.Tabs {
	sealed class NullDocumentTabContent : DocumentTabContent {
		public override DocumentTabContent Clone() => new NullDocumentTabContent();
		public override DocumentTabUIContext CreateUIContext(IDocumentTabUIContextLocator locator) =>
			locator.Get(typeof(NullDocumentTabUIContext), () => new NullDocumentTabUIContext());
		public override string Title => dnSpy_Resources.EmptyTabTitle;
	}

	sealed class NullDocumentTabUIContext : DocumentTabUIContext {
		public override IInputElement FocusedElement => null;
		public override object UIObject => string.Empty;
		public override FrameworkElement ZoomElement => null;
	}
}
