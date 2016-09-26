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

using dnSpy.Contracts.Decompiler;

namespace dnSpy.Contracts.Documents.Tabs.DocViewer {
	/// <summary>
	/// <see cref="IDocumentViewerListener"/> constants
	/// </summary>
	public static class DocumentViewerListenerConstants {
		/// <summary>Default order</summary>
		public const double ORDER_DEFAULT = double.MaxValue;

		/// <summary>Glyph text marker service</summary>
		public const double ORDER_GLYPHTEXTMARKERSERVICE = 1000;

		/// <summary>create <see cref="IMethodDebugService"/></summary>
		public const double ORDER_METHODDEBUGSERVICE = 2000;

		/// <summary>Debugger: locals (<c>MethodLocalProvider</c>)</summary>
		public const double ORDER_DEBUGGER_METHODLOCALPROVIDER = ORDER_METHODDEBUGSERVICE + 1000;

		/// <summary>Brace pair service</summary>
		public const double ORDER_BRACEPAIRSERVICE = 4000;

		/// <summary>Structure visualizer service</summary>
		public const double ORDER_STRUCTUREVISUALIZERSERVICE = 5000;

		/// <summary>Line separator service</summary>
		public const double ORDER_LINESEPARATORSERVICE = 6000;
	}
}
