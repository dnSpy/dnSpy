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

using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Editor options factory service
	/// </summary>
	public abstract class HexEditorOptionsFactoryService {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexEditorOptionsFactoryService() { }

		/// <summary>
		/// Gets the global options
		/// </summary>
		public abstract IEditorOptions GlobalOptions { get; }

		/// <summary>
		/// Creates options
		/// </summary>
		/// <returns></returns>
		public abstract IEditorOptions CreateOptions();

		/// <summary>
		/// Creates or returns existing options
		/// </summary>
		/// <param name="scope">Owner</param>
		/// <returns></returns>
		public abstract IEditorOptions GetOptions(IPropertyOwner scope);
	}
}
