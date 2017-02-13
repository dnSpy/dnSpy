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
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Hex.HexGroups {
	sealed class HexViewGroupOptionCollection {
		public List<HexViewGroupOption> Options { get; }
		public string SubGroup { get; }

		public HexViewGroupOptionCollection(string subGroup) {
			Options = new List<HexViewGroupOption>();
			SubGroup = subGroup ?? throw new ArgumentNullException(nameof(subGroup));
		}

		public void Add(HexViewGroupOption option) => Options.Add(option);

		public bool HasOption(string optionId) {
			foreach (var option in Options) {
				if (option.OptionId == optionId)
					return true;
			}
			return false;
		}

		public object GetOptionValue(string optionId) {
			foreach (var option in Options) {
				if (option.OptionId == optionId)
					return option.Value;
			}
			throw new ArgumentException($"Invalid optionId: {optionId}", nameof(optionId));
		}

		public void SetOptionValue(string optionId, object value) {
			foreach (var option in Options) {
				if (option.OptionId == optionId) {
					option.Value = value;
					return;
				}
			}
			throw new ArgumentException($"Invalid optionId: {optionId}", nameof(optionId));
		}

		public void InitializeOptions(WpfHexView hexView) {
			foreach (var option in Options) {
				try {
					hexView.Options.SetOptionValue(option.OptionId, option.Value);
				}
				catch (ArgumentException) {
					// Invalid option value
				}
			}
		}
	}
}
