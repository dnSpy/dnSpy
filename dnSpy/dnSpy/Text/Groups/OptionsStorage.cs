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
using System.ComponentModel;
using dnSpy.Contracts.Settings;

namespace dnSpy.Text.Groups {
	sealed class OptionsStorage {
		static readonly Guid SETTINGS_GUID = new Guid("BA10705F-F9A0-4CF6-B3A8-B0E4A76151A6");
		const string GroupName = "Group";
		const string GroupNameAttr = "name";
		const string ContentTypeName = "ContentType";
		const string ContentTypeNameAttr = "name";
		const string OptionName = "Option";
		const string OptionNameAttr = "name";
		const string OptionValueAttr = "value";

		struct ContentTypeKey : IEquatable<ContentTypeKey> {
			readonly string groupName, contentType;
			public ContentTypeKey(string groupName, string contentType) {
				this.groupName = groupName;
				this.contentType = contentType;
			}
			public bool Equals(ContentTypeKey other) => StringComparer.Ordinal.Equals(groupName, other.groupName) && StringComparer.OrdinalIgnoreCase.Equals(contentType, other.contentType);
			public override bool Equals(object obj) => obj is ContentTypeKey && Equals((ContentTypeKey)obj);
			public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(groupName) ^ StringComparer.OrdinalIgnoreCase.GetHashCode(contentType);
			public override string ToString() => $"({groupName},{contentType})";
		}

		struct OptionKey : IEquatable<OptionKey> {
			/*readonly*/ ContentTypeKey contentTypeKey;
			readonly string name;
			public OptionKey(ContentTypeKey contentTypeKey, string name) {
				this.contentTypeKey = contentTypeKey;
				this.name = name;
			}
			public bool Equals(OptionKey other) => contentTypeKey.Equals(other.contentTypeKey) && StringComparer.Ordinal.Equals(name, other.name);
			public override bool Equals(object obj) => obj is OptionKey && Equals((OptionKey)obj);
			public override int GetHashCode() => contentTypeKey.GetHashCode() ^ StringComparer.Ordinal.GetHashCode(name);
			public override string ToString() => contentTypeKey.ToString() + ": " + name;
		}

		readonly ISettingsSection settingsSection;
		readonly Dictionary<string, ISettingsSection> toGroupSection;
		readonly Dictionary<ContentTypeKey, ISettingsSection> toContentTypeSection;
		readonly Dictionary<OptionKey, ISettingsSection> toOptionSection;

		public OptionsStorage(ISettingsService settingsService) {
			if (settingsService == null)
				throw new ArgumentNullException(nameof(settingsService));
			toGroupSection = new Dictionary<string, ISettingsSection>(StringComparer.Ordinal);
			toContentTypeSection = new Dictionary<ContentTypeKey, ISettingsSection>();
			toOptionSection = new Dictionary<OptionKey, ISettingsSection>();
			settingsSection = settingsService.GetOrCreateSection(SETTINGS_GUID);

			foreach (var groupSect in settingsSection.SectionsWithName(GroupName)) {
				var groupName = groupSect.Attribute<string>(GroupNameAttr);
				if (groupName == null)
					continue;
				if (toGroupSection.ContainsKey(groupName))
					continue;
				toGroupSection[groupName] = groupSect;

				foreach (var ctSect in groupSect.SectionsWithName(ContentTypeName)) {
					var contentType = ctSect.Attribute<string>(ContentTypeNameAttr);
					if (contentType == null)
						continue;
					var key = new ContentTypeKey(groupName, contentType);
					if (toContentTypeSection.ContainsKey(key))
						continue;
					toContentTypeSection[key] = ctSect;

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

		public void InitializeOptions(string groupName, TextViewGroupOptionCollection collection) {
			if (groupName == null)
				throw new ArgumentNullException(nameof(groupName));
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			ISettingsSection ctSect;
			if (!toContentTypeSection.TryGetValue(new ContentTypeKey(groupName, collection.ContentType.TypeName), out ctSect))
				return;

			var toOption = new Dictionary<string, TextViewGroupOption>(StringComparer.Ordinal);
			foreach (var option in collection.Options)
				toOption[option.OptionId] = option;

			foreach (var sect in ctSect.SectionsWithName(OptionName)) {
				var name = sect.Attribute<string>(OptionNameAttr);
				if (name == null)
					continue;

				var textValue = sect.Attribute<string>(OptionValueAttr);
				if (textValue == null)
					continue;

				TextViewGroupOption option;
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

		bool TryGetValue(TextViewGroupOption option, string textValue, out object value) {
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

		bool TryGetValueString(TextViewGroupOption option, out string valueString) {
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

		ISettingsSection GetOrCreateContentTypeSection(string groupName, string contentType) {
			var key = new ContentTypeKey(groupName, contentType);
			ISettingsSection sect;
			if (toContentTypeSection.TryGetValue(key, out sect))
				return sect;
			var groupSect = GetOrCreateGroupSection(groupName);
			sect = groupSect.CreateSection(ContentTypeName);
			toContentTypeSection.Add(key, sect);
			sect.Attribute(ContentTypeNameAttr, contentType);
			return sect;
		}

		ISettingsSection GetOrCreateOptionSection(string groupName, TextViewGroupOption option) {
			var key = new OptionKey(new ContentTypeKey(groupName, option.Definition.ContentType), option.OptionId);
			ISettingsSection sect;
			if (toOptionSection.TryGetValue(key, out sect))
				return sect;
			var ctSect = GetOrCreateContentTypeSection(groupName, option.Definition.ContentType);
			sect = ctSect.CreateSection(OptionName);
			toOptionSection.Add(key, sect);
			sect.Attribute(OptionNameAttr, option.OptionId);
			return sect;
		}

		public void Write(string groupName, TextViewGroupOption option) {
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
