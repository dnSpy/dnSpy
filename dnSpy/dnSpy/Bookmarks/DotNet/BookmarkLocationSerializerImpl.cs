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
using System.ComponentModel.Composition;
using dnlib.DotNet;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Bookmarks.DotNet;
using dnSpy.Contracts.Debugger.DotNet.Metadata;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.Settings;

namespace dnSpy.Bookmarks.DotNet {
	abstract class DotNetBookmarkLocationSerializer : BookmarkLocationSerializer {
		protected readonly Lazy<DotNetBookmarkLocationFactory> dotNetBookmarkLocationFactory;
		readonly Lazy<DbgMetadataService> dbgMetadataService;

		protected DotNetBookmarkLocationSerializer(Lazy<DotNetBookmarkLocationFactory> dotNetBookmarkLocationFactory, Lazy<DbgMetadataService> dbgMetadataService) {
			this.dotNetBookmarkLocationFactory = dotNetBookmarkLocationFactory;
			this.dbgMetadataService = dbgMetadataService;
		}

		protected abstract void SerializeCore(ISettingsSection section, BookmarkLocation location);

		public override void Serialize(ISettingsSection section, BookmarkLocation location) {
			var iloc = (IDotNetBookmarkLocation)location;
			SerializeCore(section, location);
			section.Attribute("Token", iloc.Token);
			section.Attribute("AssemblyFullName", iloc.Module.AssemblyFullName);
			section.Attribute("ModuleName", iloc.Module.ModuleName);
			if (iloc.Module.IsDynamic)
				section.Attribute("IsDynamic", iloc.Module.IsDynamic);
			if (iloc.Module.IsInMemory)
				section.Attribute("IsInMemory", iloc.Module.IsInMemory);
			if (iloc.Module.ModuleNameOnly)
				section.Attribute("ModuleNameOnly", iloc.Module.ModuleNameOnly);

			if (!iloc.Module.IsInMemory && !iloc.Module.IsDynamic) {
				var s = GetTokenAsString(iloc.Module, iloc.Token);
				if (s != null)
					section.Attribute("TokenString", s);
			}
		}

		public override BookmarkLocation Deserialize(ISettingsSection section) {
			var token = section.Attribute<uint?>("Token");
			var assemblyFullName = section.Attribute<string>("AssemblyFullName");
			var moduleName = section.Attribute<string>("ModuleName");
			var isDynamic = section.Attribute<bool?>("IsDynamic") ?? false;
			var isInMemory = section.Attribute<bool?>("IsInMemory") ?? false;
			var moduleNameOnly = section.Attribute<bool?>("ModuleNameOnly") ?? false;
			if (token == null || assemblyFullName == null || moduleName == null)
				return null;
			var moduleId = new ModuleId(assemblyFullName, moduleName, isDynamic, isInMemory, moduleNameOnly);

			if (!isInMemory && !isDynamic) {
				var s = section.Attribute<string>("TokenString");
				if (!string.IsNullOrEmpty(s) && s != GetTokenAsString(moduleId, token.Value))
					return null;
			}

			return DeserializeCore(section, moduleId, token.Value);
		}

		protected abstract BookmarkLocation DeserializeCore(ISettingsSection section, ModuleId module, uint token);

		string GetTokenAsString(ModuleId moduleId, uint token) {
			var module = dbgMetadataService.Value.TryGetMetadata(moduleId, DbgLoadModuleOptions.AutoLoaded);
			return (module?.ResolveToken(token) as IMemberDef)?.ToString();
		}
	}

	[ExportBookmarkLocationSerializer(PredefinedBookmarkLocationTypes.DotNetBody)]
	sealed class DotNetMethodBodyBookmarkLocationSerializer : DotNetBookmarkLocationSerializer {
		[ImportingConstructor]
		DotNetMethodBodyBookmarkLocationSerializer(Lazy<DotNetBookmarkLocationFactory> dotNetBookmarkLocationFactory, Lazy<DbgMetadataService> dbgMetadataService)
			: base(dotNetBookmarkLocationFactory, dbgMetadataService) {
		}

		protected override void SerializeCore(ISettingsSection section, BookmarkLocation location) {
			var loc = (DotNetMethodBodyBookmarkLocation)location;
			section.Attribute("Offset", loc.Offset);
		}

		protected override BookmarkLocation DeserializeCore(ISettingsSection section, ModuleId module, uint token) {
			var offset = section.Attribute<uint?>("Offset");
			if (offset == null)
				return null;
			return dotNetBookmarkLocationFactory.Value.CreateMethodBodyLocation(module, token, offset.Value);
		}
	}

	[ExportBookmarkLocationSerializer(PredefinedBookmarkLocationTypes.DotNetToken)]
	sealed class DotNetTokenBookmarkLocationSerializer : DotNetBookmarkLocationSerializer {
		[ImportingConstructor]
		DotNetTokenBookmarkLocationSerializer(Lazy<DotNetBookmarkLocationFactory> dotNetBookmarkLocationFactory, Lazy<DbgMetadataService> dbgMetadataService)
			: base(dotNetBookmarkLocationFactory, dbgMetadataService) {
		}

		protected override void SerializeCore(ISettingsSection section, BookmarkLocation location) { }

		protected override BookmarkLocation DeserializeCore(ISettingsSection section, ModuleId module, uint token) =>
			dotNetBookmarkLocationFactory.Value.CreateTokenLocation(module, token);
	}
}
