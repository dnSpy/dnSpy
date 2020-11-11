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
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using dnlib.DotNet;
using dnSpy.Decompiler.Properties;

namespace dnSpy.Decompiler.MSBuild {
	sealed class SettingsProjectFile : ProjectFile {
		public override string Description => string.Format(dnSpy_Decompiler_Resources.MSBuild_CreateSettingsFile, Path.GetFileName(filename));
		public override BuildAction BuildAction => BuildAction.None;
		public override string Filename => filename;
		readonly string filename;
		readonly TypeDef type;

		public SettingsProjectFile(TypeDef type, string filename) {
			this.filename = filename;
			this.type = type;
		}

		sealed class Setting {
			public string? Name { get; set; }
			public string? Description { get; set; }
			public string? Provider { get; set; }
			public bool Roaming { get; set; }
			public bool GenerateDefaultValueInCode { get; set; }
			public string? Type { get; set; }
			public string? Scope { get; set; }
			public Value? Value { get; set; }
			public Value? DesignTimeValue { get; set; }
			public Setting() => GenerateDefaultValueInCode = true;
		}
		sealed class Value {
			public string? Profile { get; set; }
			public string? Text { get; set; }
		}

		const string DEFAULT_PROFILE = "(Default)";
		public override void Create(DecompileContext ctx) {
			var settings = new XmlWriterSettings {
				Encoding = Encoding.UTF8,
				Indent = true,
			};
			using (var writer = XmlWriter.Create(filename, settings)) {
				writer.WriteProcessingInstruction("xml", "version='1.0' encoding='utf-8'");
				writer.WriteStartElement("SettingsFile", "http://schemas.microsoft.com/VisualStudio/2004/01/settings");
				writer.WriteAttributeString("CurrentProfile", DEFAULT_PROFILE);
				writer.WriteAttributeString("GeneratedClassNamespace", type.ReflectionNamespace);
				writer.WriteAttributeString("GeneratedClassName", type.ReflectionName);

				writer.WriteStartElement("Profiles");
				writer.WriteEndElement();

				writer.WriteStartElement("Settings");
				foreach (var setting in FindSettings()) {
					writer.WriteStartElement("Setting");
					writer.WriteAttributeString("Name", setting.Name);
					if (!string.IsNullOrEmpty(setting.Description))
						writer.WriteAttributeString("Description", setting.Description);
					if (!string.IsNullOrEmpty(setting.Provider))
						writer.WriteAttributeString("Provider", setting.Provider);
					if (setting.Roaming)
						writer.WriteAttributeString("Roaming", "true");
					if (!setting.GenerateDefaultValueInCode)
						writer.WriteAttributeString("GenerateDefaultValueInCode", "false");
					writer.WriteAttributeString("Type", setting.Type);
					writer.WriteAttributeString("Scope", setting.Scope);
					if (setting.DesignTimeValue is not null) {
						writer.WriteStartElement("DesignTimeValue");
						writer.WriteAttributeString("Profile", setting.DesignTimeValue.Profile);
						writer.WriteString(setting.DesignTimeValue.Text);
						writer.WriteEndElement();
					}
					writer.WriteStartElement("Value");
					writer.WriteAttributeString("Profile", setting.Value?.Profile ?? "???");
					writer.WriteString(setting.Value?.Text ?? "???");
					writer.WriteEndElement();
					writer.WriteEndElement();
				}
				writer.WriteEndElement();

				writer.WriteEndElement();
				writer.WriteEndDocument();
			}
		}

		IEnumerable<Setting> FindSettings() {
			foreach (var prop in type.Properties) {
				var propType = prop.PropertySig.GetRetType().RemovePinnedAndModifiers();
				if (propType is null)
					continue;
				string settingsType = propType.ReflectionFullName;

				var ca = prop.CustomAttributes.Find("System.Configuration.DefaultSettingValueAttribute");
				if (ca is null || ca.ConstructorArguments.Count != 1)
					continue;
				var arg = ca.ConstructorArguments[0];
				if (arg.Type.RemovePinnedAndModifiers().GetElementType() != ElementType.String)
					continue;
				string defaultValue = arg.Value as UTF8String;
				if (defaultValue is null)
					continue;
				bool generateDefaultValueInCode = true;

				bool hasUserScopedAttr = prop.CustomAttributes.IsDefined("System.Configuration.UserScopedSettingAttribute");
				bool hasAppScopedAttr = prop.CustomAttributes.IsDefined("System.Configuration.ApplicationScopedSettingAttribute");
				if (!hasUserScopedAttr && !hasAppScopedAttr)
					continue;

				bool roaming = false;
				ca = prop.CustomAttributes.Find("System.Configuration.SettingsManageabilityAttribute");
				if (ca is not null && ca.ConstructorArguments.Count == 1) {
					arg = ca.ConstructorArguments[0];
					var argType = arg.Type.RemovePinnedAndModifiers();
					if (argType is not null && argType.ReflectionFullName == "System.Configuration.SettingsManageability") {
						var v = arg.Value as int?;
						if (v is not null) {
							switch ((SettingsManageability)v.Value) {
							case SettingsManageability.Roaming:
								roaming = true;
								break;
							}
						}
					}
				}

				var setting = new Setting();

				ca = prop.CustomAttributes.Find("System.Configuration.SpecialSettingAttribute");
				if (ca is not null && ca.ConstructorArguments.Count == 1) {
					arg = ca.ConstructorArguments[0];
					var argType = arg.Type.RemovePinnedAndModifiers();
					if (argType is not null && argType.ReflectionFullName == "System.Configuration.SpecialSetting") {
						var v = arg.Value as int?;
						if (v is not null) {
							switch ((SpecialSetting)v.Value) {
							case SpecialSetting.ConnectionString:
								settingsType = "(Connection string)";
								var designTimeValue = GetConnectionStringDesignTimeValue(prop);
								if (designTimeValue is not null) {
									setting.DesignTimeValue = new Value {
										Profile = DEFAULT_PROFILE,
										Text = designTimeValue,
									};
								}
								break;
							case SpecialSetting.WebServiceUrl:
								settingsType = "(Web Service URL)";
								break;
							}
						}
					}
				}

				string? provider = null;
				ca = prop.CustomAttributes.Find("System.Configuration.SettingsProviderAttribute");
				if (ca is not null && ca.ConstructorArguments.Count == 1) {
					arg = ca.ConstructorArguments[0];
					var argType = arg.Type.RemovePinnedAndModifiers();
					if (argType.GetElementType() == ElementType.String)
						provider = arg.Value as UTF8String;
					else if (argType is not null && argType.FullName == "System.Type") {
						if (arg.Value is TypeDefOrRefSig t && t.TypeDefOrRef is not null)
							provider = t.TypeDefOrRef.ReflectionFullName;
					}
				}

				string? description = null;
				ca = prop.CustomAttributes.Find("System.Configuration.SettingsDescriptionAttribute");
				if (ca is not null && ca.ConstructorArguments.Count == 1) {
					arg = ca.ConstructorArguments[0];
					var argType = arg.Type.RemovePinnedAndModifiers();
					if (argType.GetElementType() == ElementType.String)
						description = arg.Value as UTF8String;
				}

				setting.Name = prop.Name;
				setting.Description = description;
				setting.Provider = provider;
				setting.Roaming = roaming;
				setting.GenerateDefaultValueInCode = generateDefaultValueInCode;
				setting.Type = settingsType;
				setting.Scope = hasUserScopedAttr ? "User" : "Application";

				setting.Value = new Value {
					Profile = DEFAULT_PROFILE,
					Text = defaultValue,
				};
				yield return setting;
			}
		}

		string? GetConnectionStringDesignTimeValue(PropertyDef prop) {
			if (toConnectionStringInfo is null)
				InitializeConnectionStringDesignTimeValues();
			Debug2.Assert(toConnectionStringInfo is not null);
			if (!toConnectionStringInfo.TryGetValue(prop.Name, out var info))
				return null;

			return string.Format(connectionIdStringFormat, EscapeXmlString(info.String), EscapeXmlString(info.ProviderName));
		}
		Dictionary<string, ConnectionStringInfo>? toConnectionStringInfo;
		static readonly string connectionIdStringFormat = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<SerializableConnectionString xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <ConnectionString>{0}</ConnectionString>\r\n  <ProviderName>{1}</ProviderName>\r\n</SerializableConnectionString>";

		static string EscapeXmlString(string? s) {
			var el = new XmlDocument().CreateElement("a");
			el.InnerText = s ?? string.Empty;
			return el.InnerXml;
		}

		sealed class ConnectionStringInfo {
			public string? String { get; set; }
			public string? ProviderName { get; set; }
		}

		void InitializeConnectionStringDesignTimeValues() {
			Debug2.Assert(toConnectionStringInfo is null);
			if (toConnectionStringInfo is not null)
				return;
			toConnectionStringInfo = new Dictionary<string, ConnectionStringInfo>(StringComparer.Ordinal);

			var configFile = type.Module.Location + ".config";
			if (!File.Exists(configFile))
				return;

			try {
				var doc = XDocument.Load(configFile, LoadOptions.None);
				var prefix = type.ReflectionFullName + ".";
				foreach (var e in doc.XPathSelectElements("/configuration/connectionStrings/add")) {
					var name = (string?)e.Attribute("name");
					if (name is null || !name.StartsWith(prefix, StringComparison.Ordinal))
						continue;

					var connectionString = (string?)e.Attribute("connectionString");
					var providerName = (string?)e.Attribute("providerName");
					if (connectionString is null || providerName is null)
						continue;

					var info = new ConnectionStringInfo {
						String = connectionString,
						ProviderName = providerName,
					};

					var propName = name.Substring(prefix.Length);
					if (!toConnectionStringInfo.ContainsKey(propName))
						toConnectionStringInfo[propName] = info;
				}
			}
			catch {
			}
		}
	}
}
