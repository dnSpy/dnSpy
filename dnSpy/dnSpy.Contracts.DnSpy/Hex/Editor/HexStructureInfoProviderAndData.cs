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

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Contains a <see cref="HexStructureInfoProvider"/> and data
	/// </summary>
	/// <typeparam name="TValue">Type of value</typeparam>
	public readonly struct HexStructureInfoProviderAndData<TValue> {
		/// <summary>
		/// true if this is a default instance that hasn't been initialized
		/// </summary>
		public bool IsDefault => Provider == null;

		/// <summary>
		/// Gets the provider
		/// </summary>
		public HexStructureInfoProvider Provider { get; }

		/// <summary>
		/// Gets the value
		/// </summary>
		public TValue Value { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="provider">Provider</param>
		/// <param name="value">Value</param>
		public HexStructureInfoProviderAndData(HexStructureInfoProvider provider, TValue value) {
			Provider = provider ?? throw new ArgumentNullException(nameof(provider));
			Value = value;
		}
	}
}
