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

namespace dnSpy.Contracts.Hex.Editor {
	/// <summary>
	/// Returns data from all <see cref="HexStructureInfoProvider"/>s
	/// </summary>
	public abstract class HexStructureInfoAggregator {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexStructureInfoAggregator() { }

		/// <summary>
		/// Gets all providers
		/// </summary>
		protected abstract IEnumerable<HexStructureInfoProvider> Providers { get; }

		/// <summary>
		/// Gets all fields
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public IEnumerable<HexStructureInfoProviderAndData<HexStructureField>> GetFields(HexPosition position) {
			foreach (var provider in Providers) {
				foreach (var info in provider.GetFields(position))
					yield return new HexStructureInfoProviderAndData<HexStructureField>(provider, info);
			}
		}

		/// <summary>
		/// Gets all tooltips
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public IEnumerable<HexStructureInfoProviderAndData<object>> GetToolTips(HexPosition position) {
			foreach (var provider in Providers) {
				var toolTip = provider.GetToolTip(position);
				if (toolTip is not null)
					yield return new HexStructureInfoProviderAndData<object>(provider, toolTip);
			}
		}

		/// <summary>
		/// Gets all references
		/// </summary>
		/// <param name="position">Position</param>
		/// <returns></returns>
		public IEnumerable<HexStructureInfoProviderAndData<object>> GetReferences(HexPosition position) {
			foreach (var provider in Providers) {
				var reference = provider.GetReference(position);
				if (reference is not null)
					yield return new HexStructureInfoProviderAndData<object>(provider, reference);
			}
		}
	}
}
