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
using System.Resources;
using dnlib.DotNet;
using dnSpy.Languages.Properties;

namespace dnSpy.Languages.MSBuild {
	sealed class ResXProjectFile : ProjectFile {
		public override string Description => Languages_Resources.MSBuild_CreateResXFile;
		public override BuildAction BuildAction => BuildAction.EmbeddedResource;
		public override string Filename => filename;
		readonly string filename;

		public string TypeFullName { get; }
		public bool IsSatelliteFile { get; set; }

		readonly EmbeddedResource embeddedResource;
		readonly ModuleDef module;
		readonly Dictionary<IAssembly, IAssembly> newToOldAsm;

		public ResXProjectFile(ModuleDef module, string filename, string typeFullName, EmbeddedResource er) {
			this.module = module;
			this.filename = filename;
			this.TypeFullName = typeFullName;
			this.embeddedResource = er;

			this.newToOldAsm = new Dictionary<IAssembly, IAssembly>(new AssemblyNameComparer(AssemblyNameComparerFlags.All & ~AssemblyNameComparerFlags.Version));
			foreach (var asmRef in module.GetAssemblyRefs())
				this.newToOldAsm[asmRef] = asmRef;
		}

		public override void Create(DecompileContext ctx) {
			var list = ReadResourceEntries(ctx);

			using (var writer = new ResXResourceWriter(Filename, TypeNameConverter)) {
				foreach (var t in list) {
					ctx.CancellationToken.ThrowIfCancellationRequested();
					writer.AddResource(t);
				}
			}
		}

		string TypeNameConverter(Type type) {
			var newAsm = new AssemblyNameInfo(type.Assembly.GetName());
			IAssembly oldAsm;
			if (!newToOldAsm.TryGetValue(newAsm, out oldAsm))
				return type.AssemblyQualifiedName;
			if (type.IsGenericType)
				return type.AssemblyQualifiedName;
			if (AssemblyNameComparer.CompareAll.Equals(oldAsm, newAsm))
				return type.AssemblyQualifiedName;
			return string.Format("{0}, {1}", type.FullName, oldAsm.FullName);
		}

		List<ResXDataNode> ReadResourceEntries(DecompileContext ctx) {
			var list = new List<ResXDataNode>();
			int errors = 0;
			try {
				using (var reader = new ResourceReader(embeddedResource.GetResourceStream())) {
					var iter = reader.GetEnumerator();
					while (iter.MoveNext()) {
						ctx.CancellationToken.ThrowIfCancellationRequested();
						string key = null;
						try {
							key = iter.Key as string;
							if (key == null)
								continue;
							//TODO: Some resources, like images, should be saved as separate files. Use ResXFileRef.
							//		Don't do it if it's a satellite assembly.
							list.Add(new ResXDataNode(key, iter.Value, TypeNameConverter));
						}
						catch (Exception ex) {
							if (errors++ < 30)
								ctx.Logger.Error(string.Format("Could not add resource '{0}', Message: {1}", key, ex.Message));
						}
					}
				}
			}
			catch (Exception ex) {
				ctx.Logger.Error(string.Format("Could not read resources from {0}, Message: {1}", embeddedResource.Name, ex.Message));
			}
			return list;
		}
	}
}
