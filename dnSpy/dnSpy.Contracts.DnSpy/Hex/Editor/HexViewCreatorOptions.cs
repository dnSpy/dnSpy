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
using System.Collections.Generic;
using dnSpy.Contracts.Menus;

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// <see cref="WpfHexView"/> creator options
	/// </summary>
	public class HexViewCreatorOptions {
		/// <summary>
		/// Guid of context menu or null
		/// </summary>
		public Guid? MenuGuid { get; set; }

		/// <summary>
		/// Creates <see cref="GuidObject"/>s, can be null
		/// </summary>
		public Func<GuidObjectsProviderArgs, IEnumerable<GuidObject>> CreateGuidObjects { get; set; }

		/// <summary>
		/// Clones this
		/// </summary>
		/// <returns></returns>
		public HexViewCreatorOptions Clone() => CopyTo(new HexViewCreatorOptions());

		/// <summary>
		/// Constructor
		/// </summary>
		public HexViewCreatorOptions() {
		}

		/// <summary>
		/// Copy this to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public HexViewCreatorOptions CopyTo(HexViewCreatorOptions other) {
			if (other == null)
				throw new ArgumentNullException(nameof(other));
			other.MenuGuid = MenuGuid;
			other.CreateGuidObjects = CreateGuidObjects;
			return other;
		}
	}
}
