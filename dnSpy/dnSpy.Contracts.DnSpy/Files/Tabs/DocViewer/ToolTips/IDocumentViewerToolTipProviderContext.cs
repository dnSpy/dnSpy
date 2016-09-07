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

namespace dnSpy.Contracts.Files.Tabs.DocViewer.ToolTips {
	/// <summary>
	/// <see cref="IDocumentViewerToolTipProvider"/> context
	/// </summary>
	public interface IDocumentViewerToolTipProviderContext {
		/// <summary>
		/// Document viewer
		/// </summary>
		IDocumentViewer DocumentViewer { get; }

		/// <summary>
		/// Language to use
		/// </summary>
		IDecompiler Decompiler { get; }

		/// <summary>
		/// Creates a <see cref="ICodeToolTipProvider"/> instance that can be used to create the
		/// code tooltip
		/// </summary>
		/// <returns></returns>
		ICodeToolTipProvider Create();
	}
}
