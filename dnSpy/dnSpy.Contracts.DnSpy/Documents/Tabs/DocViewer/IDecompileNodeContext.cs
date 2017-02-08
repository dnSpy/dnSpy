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

using System;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.TreeView;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Documents.Tabs.DocViewer {
	/// <summary>
	/// <see cref="IDecompileNode"/> context
	/// </summary>
	public interface IDecompileNodeContext {
		/// <summary>
		/// Output to use
		/// </summary>
		IDecompilerOutput Output { get; }

		/// <summary>
		/// Writes some known documents
		/// </summary>
		IDocumentWriterService DocumentWriterService { get; }

		/// <summary>
		/// Language to use
		/// </summary>
		IDecompiler Decompiler { get; }

		/// <summary>
		/// Gets the decompilation context
		/// </summary>
		DecompilationContext DecompilationContext { get; }

		/// <summary>
		/// Executes <paramref name="func"/> in the UI thread and waits for it to complete, then
		/// returns the result to the caller. This can be used to load the node's
		/// <see cref="ITreeNode.Children"/> property since it can only be loaded in the UI thread.
		/// </summary>
		/// <typeparam name="T">Return type</typeparam>
		/// <param name="func">Delegate to execute</param>
		/// <returns></returns>
		T UIThread<T>(Func<T> func);

		/// <summary>
		/// Sets the content type. See also <see cref="ContentTypeString"/>
		/// </summary>
		IContentType ContentType { get; set; }

		/// <summary>
		/// Sets the content type. See also <see cref="ContentType"/>
		/// </summary>
		string ContentTypeString { get; set; }
	}
}
