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

using System.Collections.Generic;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Handles references created by <see cref="HexStructureInfoProvider.GetReference(HexPosition)"/>.
	/// Export an instance with a <see cref="VSUTIL.NameAttribute"/>, see <see cref="PredefinedHexReferenceHandlerNames"/>.
	/// </summary>
	public abstract class HexReferenceHandler {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexReferenceHandler() { }

		/// <summary>
		/// Handles a reference
		/// </summary>
		/// <param name="hexView">Hex view</param>
		/// <param name="reference">Reference created by eg. <see cref="HexStructureInfoProvider.GetReference(HexPosition)"/></param>
		/// <param name="tags">Tags, see <see cref="PredefinedHexReferenceHandlerTags"/></param>
		/// <returns></returns>
		public abstract bool Handle(HexView hexView, object reference, IList<string>? tags);
	}
}
