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
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Debugger.Exceptions;

namespace dnSpy.Debugger.Exceptions {
	[Export(typeof(DefaultExceptionDefinitionsProvider))]
	sealed class DefaultExceptionDefinitionsProvider {
		public ReadOnlyCollection<DbgExceptionCategoryDefinition> CategoryDefinitions { get; }
		public ReadOnlyCollection<DbgExceptionDefinition> Definitions { get; }

		[ImportingConstructor]
		DefaultExceptionDefinitionsProvider([ImportMany] IEnumerable<Lazy<DbgExceptionDefinitionProvider, IDbgExceptionDefinitionProviderMetadata>> dbgExceptionDefinitionProviders) {
			var providers = dbgExceptionDefinitionProviders.OrderBy(a => a.Metadata.Order).ToArray();

			var xmlFiles = new List<string>();
			foreach (var p in providers) {
				foreach (var file in p.Value.GetExceptionFilenames()) {
					string filename;
					if (Path.IsPathRooted(file))
						filename = file;
					else
						filename = Path.Combine(Path.GetDirectoryName(p.Value.GetType().Assembly.Location)!, file);
					if (!File.Exists(filename))
						continue;
					xmlFiles.Add(filename);
				}
			}
			var debugDir = Path.Combine(AppDirectories.BinDirectory, "debug");
			xmlFiles.AddRange(Directory.GetFiles(debugDir, "*.ex.xml").OrderBy(a => a, StringComparer.OrdinalIgnoreCase));
			var reader = new ExceptionsFileReader();
			foreach (var file in xmlFiles.Distinct(StringComparer.OrdinalIgnoreCase))
				reader.Read(file);

			var categoryDefs = new Dictionary<string, DbgExceptionCategoryDefinition>(StringComparer.Ordinal);
			foreach (var p in providers) {
				foreach (var def in p.Value.CreateCategories()) {
					if (!categoryDefs.ContainsKey(def.Name))
						categoryDefs.Add(def.Name, def);
				}
			}
			// Categories from files have lower priority than anything from CreateCategories()
			foreach (var def in reader.CategoryDefinitions) {
				if (!categoryDefs.ContainsKey(def.Name))
					categoryDefs.Add(def.Name, def);
			}
			CategoryDefinitions = new ReadOnlyCollection<DbgExceptionCategoryDefinition>(categoryDefs.Select(a => a.Value).ToArray());

			var defs = new Dictionary<DbgExceptionId, DbgExceptionDefinition>();
			foreach (var p in providers) {
				foreach (var def in p.Value.Create()) {
					bool b = categoryDefs.ContainsKey(def.Id.Category);
					Debug.Assert(b);
					if (!b)
						continue;
					if (!defs.ContainsKey(def.Id))
						defs.Add(def.Id, def);
				}
			}
			// Exceptions from files have lower priority than anything from Create()
			foreach (var def in reader.ExceptionDefinitions) {
				bool b = categoryDefs.ContainsKey(def.Id.Category);
				Debug.Assert(b);
				if (!b)
					continue;
				if (!defs.ContainsKey(def.Id))
					defs.Add(def.Id, def);
			}
			foreach (var category in CategoryDefinitions) {
				var id = new DbgExceptionId(category.Name);
				if (!defs.ContainsKey(id))
					defs.Add(id, new DbgExceptionDefinition(id, DbgExceptionDefinitionFlags.None));
			}
			Definitions = new ReadOnlyCollection<DbgExceptionDefinition>(defs.Select(a => a.Value).ToArray());
		}
	}
}
