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
using System.ComponentModel;
using dnSpy.Contracts.Settings;

namespace dnSpy.Hex.HexGroups {
	sealed class OptionsStorage {
		static readonly Guid SETTINGS_GUID = new Guid("8A9E5743-F634-45CE-A841-E2275338DC28");
		const string GroupName = "Group";
		const string GroupNameAttr = "name";
		const string TagName = "Tag";
		const string TagNameAttr = "name";
		const string OptionName = "Option";
		const string OptionNameAttr = "name";
		const string OptionValueAttr = "value";

		struct TagKey : IEquatable<TagKey> {
			readonly string groupName, tag;
			public TagKey(string groupName, string tag) {
				this.groupName = groupName;
				this.tag = tag;
			}
			public bool Equals(TagKey other) => StringComparer.Ordinal.Equals(groupName, other.groupName) && StringComparer.OrdinalIgnoreCase.Equals(tag, other.tag);
			public override bool Equals(object obj) => obj is TagKey && Equals((TagKey)obj);
			public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(groupName) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(tag);
			public override string ToString() => $"({groupName},{tag})";
		}

		struct OptionKey : IEquatable<OptionKey> {
			/*readonly*/ TagKey tagKey;
			readonly string name;
			public OptionKey(TagKey tagKey, string name) {
				this.tagKey = tagKey;
				this.name = name;
			}
			public bool Equals(OptionKey other) => tagKey.Equals(other.tagKey) && StringComparer.Ordinal.Equals(name, other.name);
			public override bool Equals(object obj) => obj is OptionKey && Equals((OptionKey)obj);
			public override int GetHashCode() => tagKey.GetHashCode() ^ StringComparer.Ordinal.GetHashCode(name);
			public override string ToString() => tagKey.ToString() + ": " + name;
		}

		readonly ISettingsSection settingsSection;
		readonly Dictionary<string, ISettingsSection> toGroupSection;
		readonly Dictionary<TagKey, ISettingsSection> toTagSection;
		readonly Dictionary<OptionKey, ISettingsSection> toOptionSection;

		public OptionsStorage(ISettingsService settingsService) {
			if (settingsService == null)
				throw new ArgumentNullException(nameof(settingsService));
			toGroupSection = new Dictionary<string, ISettingsSection>(StringComparer.Ordinal);
			toTagSection = new Dictionary<TagKey, ISettingsSection>();
			toOptionSection = new Dictionary<OptionKey, ISettingsSection>();
			settingsSection = settingsService.GetOrCreateSection(SETTINGS_GUID);

			foreach (var groupSect in settingsSection.SectionsWithName(GroupName)) {
				var groupName = groupSect.Attribute<string>(GroupNameAttr);
				if (groupName == null)
					continue;
				if (toGroupSection.ContainsKey(groupName))
					continue;
				toGroupSection[groupName] = groupSect;

				foreach (var ctSect in groupSect.SectionsWithName(TagName)) {
					var tag = ctSect.Attribute<string>(TagNameAttr);
					if (tag == null)
						continue;
					var key = new TagKey(groupName, tag);
					if (toTagSection.ContainsKey(key))
						continue;
					toTagSection[key] = ctSect;

					foreach (var optSect in ctSect.SectionsWithName(OptionName)) {
						var name = optSect.Attribute<string>(OptionNameAttr);
						if (name == null)
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
			if (groupName == null)
				throw new ArgumentNullException(nameof(groupName));
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			ISettingsSection ctSect;
			if (!toTagSection.TryGetValue(new TagKey(groupName, collection.Tag), out ctSect))
				return;

			var toOption = new Dictionary<string, HexViewGroupOption>(StringComparer.Ordinal);
			foreach (var option in collection.Options)
				toOption[option.OptionId] = option;

			foreach (var sect in ctSect.SectionsWithName(OptionName)) {
				var name = sect.Attribute<string>(OptionNameAttr);
				if (name == null)
					continue;

				var textValue = sect.Attribute<string>(OptionValueAttr);
				if (textValue == null)
					continue;

				HexViewGroupOption option;
				if (!toOption.TryGetValue(name, out option))
					continue;
				if (!option.Definition.CanBeSaved)
					continue;

				object value;
				if (!TryGetValue(option, textValue, out value))
					continue;

				option.Value = value;
			}
		}

		bool TryGetValue(HexViewGroupOption option, string textValue, out object value) {
			var type = option.Definition.Type;
			var c = TypeDescriptor.GetConverter(type);
			try {
				value = c.ConvertFromInvariantString(textValue);
				if (type.IsValueType && value == null)
					return false;
				if (value != null && !type.IsAssignableFrom(value.GetType()))
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

		bool TryGetValueString(HexViewGroupOption option, out string valueString) {
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
			ISettingsSection sect;
			if (toGroupSection.TryGetValue(groupName, out sect))
				return sect;
			sect = settingsSection.CreateSection(GroupName);
			toGroupSection.Add(groupName, sect);
			sect.Attribute(GroupNameAttr, groupName);
			return sect;
		}

		ISettingsSection GetOrCreateTagSection(string groupName, string tag) {
			var key = new TagKey(groupName, tag);
			ISettingsSection sect;
			if (toTagSection.TryGetValue(key, out sect))
				return sect;
			var groupSect = GetOrCreateGroupSection(groupName);
			sect = groupSect.CreateSection(TagName);
			toTagSection.Add(key, sect);
			sect.Attribute(TagNameAttr, tag);
			return sect;
		}

		ISettingsSection GetOrCreateOptionSection(string groupName, HexViewGroupOption option) {
			var key = new OptionKey(new TagKey(groupName, option.Definition.Tag), option.OptionId);
			ISettingsSection sect;
			if (toOptionSection.TryGetValue(key, out sect))
				return sect;
			var ctSect = GetOrCreateTagSection(groupName, option.Definition.Tag);
			sect = ctSect.CreateSection(OptionName);
			toOptionSection.Add(key, sect);
			sect.Attribute(OptionNameAttr, option.OptionId);
			return sect;
		}

		public void Write(string groupName, HexViewGroupOption option) {
			if (groupName == null)
				throw new ArgumentNullException(nameof(groupName));
			if (option == null)
				throw new ArgumentNullException(nameof(option));
			if (!option.Definition.CanBeSaved)
				return;
			var sect = GetOrCreateOptionSection(groupName, option);
			string valueString;
			if (!TryGetValueString(option, out valueString))
				return;
			sect.Attribute(OptionValueAttr, valueString);
		}
	}
}
