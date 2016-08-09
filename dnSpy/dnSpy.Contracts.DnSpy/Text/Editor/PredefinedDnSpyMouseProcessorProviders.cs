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

using dnSpy.Contracts.Files.Tabs.DocViewer;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Predefined dnSpy <see cref="IMouseProcessorProvider"/>s
	/// </summary>
	public static class PredefinedDnSpyMouseProcessorProviders {
		/// <summary>
		/// <see cref="IDocumentViewer"/> <see cref="IMouseProcessorProvider"/>
		/// </summary>
		public const string DocumentViewerMouseProcessorProvider = "DocumentViewerMouseProcessorProvider";
	}
}
