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
using System.Collections.Generic;
using System.ComponentModel;
using dnSpy.Contracts.Settings;

namespace dnSpy.Hex.HexGroups {
	sealed class OptionsStorage {
		static readonly Guid SETTINGS_GUID = new Guid("8A9E5743-F634-45CE-A841-E2275338DC28");
		const string GroupName = "Group";
		const string GroupNameAttr = "name";
		const string SubGroupName = "SubGroup";
		const string SubGroupNameAttr = "name";
		const string OptionName = "Option";
		const string OptionNameAttr = "name";
		const string OptionValueAttr = "value";

		readonly struct SubGroupKey : IEquatable<SubGroupKey> {
			readonly string groupName, subGroup;
			public SubGroupKey(string groupName, string subGroup) {
				this.groupName = groupName;
				this.subGroup = subGroup;
			}
			public bool Equals(SubGroupKey other) => StringComparer.Ordinal.Equals(groupName, other.groupName) && StringComparer.OrdinalIgnoreCase.Equals(subGroup, other.subGroup);
			public override bool Equals(object? obj) => obj is SubGroupKey && Equals((SubGroupKey)obj);
			public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(groupName) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(subGroup);
			public override string ToString() => $"({groupName},{subGroup})";
		}

		readonly struct OptionKey : IEquatable<OptionKey> {
			readonly SubGroupKey subGroupKey;
			readonly string name;
			public OptionKey(SubGroupKey subGroupKey, string name) {
				this.subGroupKey = subGroupKey;
				this.name = name;
			}
			public bool Equals(OptionKey other) => subGroupKey.Equals(other.subGroupKey) && StringComparer.Ordinal.Equals(name, other.name);
			public override bool Equals(object? obj) => obj is OptionKey && Equals((OptionKey)obj);
			public override int GetHashCode() => subGroupKey.GetHashCode() ^ StringComparer.Ordinal.GetHashCode(name);
			public override string ToString() => subGroupKey.ToString() + ": " + name;
		}

		readonly ISettingsSection settingsSection;
		readonly Dictionary<string, ISettingsSection> toGroupSection;
		readonly Dictionary<SubGroupKey, ISettingsSection> toSubGroupSection;
		readonly Dictionary<OptionKey, ISettingsSection> toOptionSection;

		public OptionsStorage(ISettingsService settingsService) {
			if (settingsService is null)
				throw new ArgumentNullException(nameof(settingsService));
			toGroupSection = new Dictionary<string, ISettingsSection>(StringComparer.Ordinal);
			toSubGroupSection = new Dictionary<SubGroupKey, ISettingsSection>();
			toOptionSection = new Dictionary<OptionKey, ISettingsSection>();
			settingsSection = settingsService.GetOrCreateSection(SETTINGS_GUID);

			foreach (var groupSect in settingsSection.SectionsWithName(GroupName)) {
				var groupName = groupSect.Attribute<string>(GroupNameAttr);
				if (groupName is null)
					continue;
				if (toGroupSection.ContainsKey(groupName))
					continue;
				toGroupSection[groupName] = groupSect;

				foreach (var ctSect in groupSect.SectionsWithName(SubGroupName)) {
					var subGroup = ctSect.Attribute<string>(SubGroupNameAttr);
					if (subGroup is null)
						continue;
					var key = new SubGroupKey(groupName, subGroup);
					if (toSubGroupSection.ContainsKey(key))
						continue;
					toSubGroupSection[key] = ctSect;

					foreach (var optSect in ctSect.SectionsWithName(OptionName)) {
						var name = optSect.Attribute<string>(OptionNameAttr);
						if (name is null)
							continue;
						var optKey = new OptionKey(key, name);
						if (toOptionSection.ContainsKey(optKey))
							continue;
						toOptionSection[optKey] = optSect;
					}
				}
			}
		}

		public void InitializeOptions(string groupName, HexViewGroupOptionCollection collection) {
			if (groupName is null)
				throw new ArgumentNullException(nameof(groupName));
			if (collection is null)
				throw new ArgumentNullException(nameof(collection));

			if (!toSubGroupSection.TryGetValue(new SubGroupKey(groupName, collection.SubGroup), out var ctSect))
				return;

			var toOption = new Dictionary<string, HexViewGroupOption>(StringComparer.Ordinal);
			foreach (var option in collection.Options)
				toOption[option.OptionId] = option;

			foreach (var sect in ctSect.SectionsWithName(OptionName)) {
				var name = sect.Attribute<string>(OptionNameAttr);
				if (name is null)
					continue;

				var textValue = sect.Attribute<string>(OptionValueAttr);
				if (textValue is null)
					continue;

				if (!toOption.TryGetValue(name, out var option))
					continue;
				if (!option.Definition.CanBeSaved)
					continue;

				if (!TryGetValue(option, textValue, out var value))
					continue;

				option.Value = value;
			}
		}

		bool TryGetValue(HexViewGroupOption option, string textValue, out object? value) {
			var type = option.Definition.Type;
			var c = TypeDescriptor.GetConverter(type);
			try {
				value = c.ConvertFromInvariantString(textValue);
				if (type.IsValueType && value is null)
					return false;
				if (value is not null && !type.IsAssignableFrom(value.GetType()))
					return false;
				return true;
			}
			catch (FormatException) {
			}
			catch (NotSupportedException) {
			}
			value = null;
			return false;
		}

		bool TryGetValueString(HexViewGroupOption option, out string? valueString) {
			if (!option.Definition.CanBeSaved) {
				valueString = null;
				return false;
			}
			var type = option.Definition.Type;
			try {
				var c = TypeDescriptor.GetConverter(type);
				valueString = c.ConvertToInvariantString(option.Value);
				return true;
			}
			catch (FormatException) {
			}
			catch (NotSupportedException) {
			}
			valueString = null;
			return false;
		}

		ISettingsSection GetOrCreateGroupSection(string groupName) {
			if (toGroupSection.TryGetValue(groupName, out var sect))
				return sect;
			sect = settingsSection.CreateSection(GroupName);
			toGroupSection.Add(groupName, sect);
			sect.Attribute(GroupNameAttr, groupName);
			return sect;
		}

		ISettingsSection GetOrCreateSubGroupSection(string groupName, string subGroup) {
			var key = new SubGroupKey(groupName, subGroup);
			if (toSubGroupSection.TryGetValue(key, out var sect))
				return sect;
			var groupSect = GetOrCreateGroupSection(groupName);
			sect = groupSect.CreateSection(SubGroupName);
			toSubGroupSection.Add(key, sect);
			sect.Attribute(SubGroupNameAttr, subGroup);
			return sect;
		}

		ISettingsSection GetOrCreateOptionSection(string groupName, HexViewGroupOption option) {
			var key = new OptionKey(new SubGroupKey(groupName, option.Definition.SubGroup), option.OptionId);
			if (toOptionSection.TryGetValue(key, out var sect))
				return sect;
			var ctSect = GetOrCreateSubGroupSection(groupName, option.Definition.SubGroup);
			sect = ctSect.CreateSection(OptionName);
			toOptionSection.Add(key, sect);
			sect.Attribute(OptionNameAttr, option.OptionId);
			return sect;
		}

		public void Write(string groupName, HexViewGroupOption option) {
			if (groupName is null)
				throw new ArgumentNullException(nameof(groupName));
			if (option is null)
				throw new ArgumentNullException(nameof(option));
			if (!option.Definition.CanBeSaved)
				return;
			var sect = GetOrCreateOptionSection(groupName, option);
			if (!TryGetValueString(option, out var valueString))
				return;
			sect.Attribute(OptionValueAttr, valueString);
		}
	}
}
