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
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Adds the name of a margin
	/// </summary>
	public sealed class MarginNameAttribute : SingletonBaseMetadataAttribute {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="marginName">Name of margin, eg. <see cref="PredefinedMarginNames.Glyph"/></param>
		public MarginNameAttribute(string marginName) => MarginName = marginName ?? throw new ArgumentNullException(nameof(marginName));

		/// <summary>
		/// Name of margin, eg. <see cref="PredefinedMarginNames.Glyph"/>
		/// </summary>
		public string MarginName { get; }
	}
}
