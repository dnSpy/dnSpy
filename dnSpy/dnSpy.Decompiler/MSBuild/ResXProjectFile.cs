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
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using dnlib.DotNet;
using dnSpy.Decompiler.Properties;

namespace dnSpy.Decompiler.MSBuild {
	sealed class ResXProjectFile : ProjectFile {
		static ResXProjectFile() {
			// Mono doesn't support the constructors that we need

			Type[] paramTypes;
			ConstructorInfo? ctorInfo;

			paramTypes = new Type[] { typeof(string), typeof(Func<Type, string>) };
			ctorInfo = typeof(ResXResourceWriter).GetConstructor(paramTypes);
			if (!(ctorInfo is null)) {
				var dynMethod = new DynamicMethod("ResXResourceWriter-ctor", typeof(ResXResourceWriter), paramTypes);
				var ilg = dynMethod.GetILGenerator();
				ilg.Emit(OpCodes.Ldarg_0);
				ilg.Emit(OpCodes.Ldarg_1);
				ilg.Emit(OpCodes.Newobj, ctorInfo);
				ilg.Emit(OpCodes.Ret);
				delegateResXResourceWriterConstructor = (Func<string, Func<Type, string>, ResXResourceWriter>)dynMethod.CreateDelegate(typeof(Func<string, Func<Type, string>, ResXResourceWriter>));
			}

			paramTypes = new Type[] { typeof(string), typeof(object), typeof(Func<Type, string>) };
			ctorInfo = typeof(ResXDataNode).GetConstructor(paramTypes);
			if (!(ctorInfo is null)) {
				var dynMethod = new DynamicMethod("ResXDataNode-ctor", typeof(ResXDataNode), paramTypes);
				var ilg = dynMethod.GetILGenerator();
				ilg.Emit(OpCodes.Ldarg_0);
				ilg.Emit(OpCodes.Ldarg_1);
				ilg.Emit(OpCodes.Ldarg_2);
				ilg.Emit(OpCodes.Newobj, ctorInfo);
				ilg.Emit(OpCodes.Ret);
				delegateResXDataNodeConstructor = (Func<string, object?, Func<Type, string>, ResXDataNode>)dynMethod.CreateDelegate(typeof(Func<string, object?, Func<Type, string>, ResXDataNode>));
			}
		}
		static readonly Func<string, Func<Type, string>, ResXResourceWriter>? delegateResXResourceWriterConstructor;
		static readonly Func<string, object?, Func<Type, string>, ResXDataNode>? delegateResXDataNodeConstructor;

		public override string Description => dnSpy_Decompiler_Resources.MSBuild_CreateResXFile;
		public override BuildAction BuildAction => BuildAction.EmbeddedResource;
		public override string Filename => filename;
		readonly string filename;

		public string TypeFullName { get; }
		public bool IsSatelliteFile { get; set; }

		readonly EmbeddedResource embeddedResource;
		readonly Dictionary<IAssembly, IAssembly> newToOldAsm;

		public ResXProjectFile(ModuleDef module, string filename, string typeFullName, EmbeddedResource er) {
			this.filename = filename;
			TypeFullName = typeFullName;
			embeddedResource = er;

			newToOldAsm = new Dictionary<IAssembly, IAssembly>(new AssemblyNameComparer(AssemblyNameComparerFlags.All & ~AssemblyNameComparerFlags.Version));
			foreach (var asmRef in module.GetAssemblyRefs())
				newToOldAsm[asmRef] = asmRef;
		}

		public override void Create(DecompileContext ctx) {
			var list = ReadResourceEntries(ctx);

			using (var writer = delegateResXResourceWriterConstructor?.Invoke(Filename, TypeNameConverter) ?? new ResXResourceWriter(Filename)) {
				foreach (var t in list) {
					ctx.CancellationToken.ThrowIfCancellationRequested();
					writer.AddResource(t);
				}
			}
		}

		string TypeNameConverter(Type type) {
			var newAsm = new AssemblyNameInfo(type.Assembly.GetName());
			if (!newToOldAsm.TryGetValue(newAsm, out var oldAsm))
				return type.AssemblyQualifiedName ?? throw new ArgumentException();
			if (type.IsGenericType)
				return type.AssemblyQualifiedName ?? throw new ArgumentException();
			if (AssemblyNameComparer.CompareAll.Equals(oldAsm, newAsm))
				return type.AssemblyQualifiedName ?? throw new ArgumentException();
			return $"{type.FullName}, {oldAsm.FullName}";
		}

		List<ResXDataNode> ReadResourceEntries(DecompileContext ctx) {
			var list = new List<ResXDataNode>();
			int errors = 0;
			try {
				using (var reader = new ResourceReader(embeddedResource.CreateReader().AsStream())) {
					var iter = reader.GetEnumerator();
					while (iter.MoveNext()) {
						ctx.CancellationToken.ThrowIfCancellationRequested();
						string? key = null;
						try {
							key = iter.Key as string;
							if (key is null)
								continue;
							var value = iter.Value;
							// ResXDataNode ctor checks if the input is serializable, which this stream isn't.
							// We have no choice but to create a new stream.
							if (value is Stream && !value.GetType().IsSerializable) {
								var stream = (Stream)value;
								var data = new byte[stream.Length];
								if (stream.Read(data, 0, data.Length) != data.Length)
									throw new IOException("Could not read all bytes");
								value = new MemoryStream(data);
							}
							//TODO: Some resources, like images, should be saved as separate files. Use ResXFileRef.
							//		Don't do it if it's a satellite assembly.
							list.Add(delegateResXDataNodeConstructor?.Invoke(key, value, TypeNameConverter) ?? new ResXDataNode(key, value));
						}
						catch (Exception ex) {
							if (errors++ < 30)
								ctx.Logger.Error($"Could not add resource '{key}', Message: {ex.Message}");
						}
					}
				}
			}
			catch (Exception ex) {
				ctx.Logger.Error($"Could not read resources from {embeddedResource.Name}, Message: {ex.Message}");
			}
			return list;
		}
	}
}
