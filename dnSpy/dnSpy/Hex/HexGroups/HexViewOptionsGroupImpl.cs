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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Settings.HexGroups;
using VSTE = Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Hex.HexGroups {
	sealed class HexViewOptionsGroupImpl : HexViewOptionsGroup {
		public override IEnumerable<WpfHexView> HexViews => hexViews.ToArray();
		public override event EventHandler<HexViewOptionChangedEventArgs> HexViewOptionChanged;

		readonly HexViewOptionsGroupServiceImpl owner;
		readonly List<WpfHexView> hexViews;
		readonly Dictionary<string, HexViewGroupOptionCollection> toOptions;
		readonly OptionsStorage optionsStorage;
		readonly string groupName;

		public HexViewOptionsGroupImpl(HexViewOptionsGroupServiceImpl owner, string groupName, TagOptionDefinition[] defaultOptions, OptionsStorage optionsStorage) {
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			if (groupName == null)
				throw new ArgumentNullException(nameof(groupName));
			if (defaultOptions == null)
				throw new ArgumentNullException(nameof(defaultOptions));
			if (optionsStorage == null)
				throw new ArgumentNullException(nameof(optionsStorage));
			this.owner = owner;
			hexViews = new List<WpfHexView>();
			toOptions = new Dictionary<string, HexViewGroupOptionCollection>(StringComparer.OrdinalIgnoreCase);
			this.groupName = groupName;

			foreach (var option in defaultOptions) {
				Debug.Assert(option.Name != null);
				if (option.Name == null)
					continue;

				var subGroup = option.SubGroup;
				Debug.Assert(subGroup != null);
				if (subGroup == null)
					continue;

				HexViewGroupOptionCollection coll;
				if (!toOptions.TryGetValue(subGroup, out coll))
					toOptions.Add(subGroup, coll = new HexViewGroupOptionCollection(subGroup));
				coll.Add(new HexViewGroupOption(this, option));
			}

			foreach (var coll in toOptions.Values)
				optionsStorage.InitializeOptions(groupName, coll);
			this.optionsStorage = optionsStorage;
		}

		HexViewGroupOptionCollection GetCollection(string tag) {
			if (tag == null)
				tag = string.Empty;

			HexViewGroupOptionCollection coll;
			if (toOptions.TryGetValue(tag, out coll))
				return coll;

			coll = ErrorCollection;
			toOptions.Add(tag, coll);
			return coll;
		}

		HexViewGroupOptionCollection ErrorCollection => errorCollection ?? (errorCollection = new HexViewGroupOptionCollection(Guid.NewGuid().ToString()));
		HexViewGroupOptionCollection errorCollection;

		public override bool HasOption<T>(string tag, VSTE.EditorOptionKey<T> option) => HasOption(tag, option.Name);
		public override bool HasOption(string tag, string optionId) {
			if (tag == null)
				throw new ArgumentNullException(nameof(tag));
			if (optionId == null)
				throw new ArgumentNullException(nameof(optionId));
			return GetCollection(tag).HasOption(optionId);
		}

		public override T GetOptionValue<T>(string tag, VSTE.EditorOptionKey<T> option) => (T)GetOptionValue(tag, option.Name);
		public override object GetOptionValue(string tag, string optionId) {
			if (tag == null)
				throw new ArgumentNullException(nameof(tag));
			if (optionId == null)
				throw new ArgumentNullException(nameof(optionId));
			return GetCollection(tag).GetOptionValue(optionId);
		}

		public override void SetOptionValue<T>(string tag, VSTE.EditorOptionKey<T> option, T value) => SetOptionValue(tag, option.Name, value);
		public override void SetOptionValue(string tag, string optionId, object value) {
			if (tag == null)
				throw new ArgumentNullException(nameof(tag));
			if (optionId == null)
				throw new ArgumentNullException(nameof(optionId));
			GetCollection(tag).SetOptionValue(optionId, value);
		}

		internal void HexViewCreated(WpfHexView hexView) {
			if (hexView == null)
				throw new ArgumentNullException(nameof(hexView));
			Debug.Assert(!hexView.IsClosed);
			if (hexView.IsClosed)
				return;
			hexViews.Add(hexView);
			new HexViewListener(this, hexView);
		}

		sealed class HexViewListener {
			readonly HexViewOptionsGroupImpl owner;
			readonly WpfHexView hexView;

			public HexViewListener(HexViewOptionsGroupImpl owner, WpfHexView hexView) {
				this.owner = owner;
				this.hexView = hexView;
				hexView.Closed += HexView_Closed;
				hexView.Options.OptionChanged += Options_OptionChanged;
				owner.InitializeOptions(hexView);
			}

			void Options_OptionChanged(object sender, VSTE.EditorOptionChangedEventArgs e) {
				if (hexView.IsClosed)
					return;
				owner.OptionChanged(hexView, e);
			}

			void HexView_Closed(object sender, EventArgs e) {
				hexView.Closed -= HexView_Closed;
				hexView.Options.OptionChanged -= Options_OptionChanged;
				owner.Closed(hexView);
			}
		}

		readonly HashSet<HexViewGroupOption> writeOptionHash = new HashSet<HexViewGroupOption>();
		public void OptionChanged(HexViewGroupOption option) {
			if (optionsStorage == null)
				return;
			if (writeOptionHash.Contains(option))
				return;
			try {
				writeOptionHash.Add(option);
				optionsStorage.Write(groupName, option);
				foreach (var hexView in hexViews.ToArray()) {
					if (!StringComparer.OrdinalIgnoreCase.Equals(GetSubGroup(hexView), option.Definition.SubGroup))
						continue;
					try {
						hexView.Options.SetOptionValue(option.OptionId, option.Value);
					}
					catch (ArgumentException) {
						// Invalid option value
					}
				}
				HexViewOptionChanged?.Invoke(this, new HexViewOptionChangedEventArgs(option.Definition.SubGroup, option.Definition.Name));
			}
			finally {
				writeOptionHash.Remove(option);
			}
		}

		string GetSubGroup(WpfHexView hexView) => owner.GetSubGroup(hexView) ?? string.Empty;

		void OptionChanged(WpfHexView hexView, VSTE.EditorOptionChangedEventArgs e) {
			var coll = GetCollection(GetSubGroup(hexView));
			if (!coll.HasOption(e.OptionId))
				return;
			coll.SetOptionValue(e.OptionId, hexView.Options.GetOptionValue(e.OptionId));
		}

		void InitializeOptions(WpfHexView hexView) =>
			GetCollection(GetSubGroup(hexView)).InitializeOptions(hexView);

		void Closed(WpfHexView hexView) {
			Debug.Assert(hexView.IsClosed);
			bool b = hexViews.Remove(hexView);
			Debug.Assert(b);
		}
	}
}
