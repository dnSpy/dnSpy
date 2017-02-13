/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System.Collections.Generic;
using System.Threading;
using dnlib.DotNet;

namespace dnSpy.BamlDecompiler.Baml {
	internal class BamlContext {
		public ModuleDef Module { get; }
		public KnownThings KnownThings { get; }

		Dictionary<ushort, IAssembly> assemblyMap = new Dictionary<ushort, IAssembly>();

		public Dictionary<ushort, AssemblyInfoRecord> AssemblyIdMap { get; }
		public Dictionary<ushort, AttributeInfoRecord> AttributeIdMap { get; }
		public Dictionary<ushort, StringInfoRecord> StringIdMap { get; }
		public Dictionary<ushort, TypeInfoRecord> TypeIdMap { get; }

		BamlContext(ModuleDef module) {
			Module = module;
			KnownThings = new KnownThings(module);

			AssemblyIdMap = new Dictionary<ushort, AssemblyInfoRecord>();
			AttributeIdMap = new Dictionary<ushort, AttributeInfoRecord>();
			StringIdMap = new Dictionary<ushort, StringInfoRecord>();
			TypeIdMap = new Dictionary<ushort, TypeInfoRecord>();
		}

		public static BamlContext ConstructContext(ModuleDef module, BamlDocument document, CancellationToken token) {
			var ctx = new BamlContext(module);

			foreach (var record in document) {
				token.ThrowIfCancellationRequested();

				if (record is AssemblyInfoRecord) {
					var assemblyInfo = (AssemblyInfoRecord)record;
					if (assemblyInfo.AssemblyId == ctx.AssemblyIdMap.Count)
						ctx.AssemblyIdMap.Add(assemblyInfo.AssemblyId, assemblyInfo);
				}
				else if (record is AttributeInfoRecord) {
					var attrInfo = (AttributeInfoRecord)record;
					if (attrInfo.AttributeId == ctx.AttributeIdMap.Count)
						ctx.AttributeIdMap.Add(attrInfo.AttributeId, attrInfo);
				}
				else if (record is StringInfoRecord) {
					var strInfo = (StringInfoRecord)record;
					if (strInfo.StringId == ctx.StringIdMap.Count)
						ctx.StringIdMap.Add(strInfo.StringId, strInfo);
				}
				else if (record is TypeInfoRecord) {
					var typeInfo = (TypeInfoRecord)record;
					if (typeInfo.TypeId == ctx.TypeIdMap.Count)
						ctx.TypeIdMap.Add(typeInfo.TypeId, typeInfo);
				}
			}

			return ctx;
		}

		public IAssembly ResolveAssembly(ushort id) {
			id &= 0xfff;
			if (!assemblyMap.TryGetValue(id, out var assembly)) {
				if (AssemblyIdMap.TryGetValue(id, out var assemblyRec)) {
					var assemblyName = new AssemblyNameInfo(assemblyRec.AssemblyFullName);

					if (assemblyName.Name == Module.Assembly.Name)
						assembly = Module.Assembly;
					else
						assembly = Module.Context.AssemblyResolver.Resolve(assemblyName, Module);

					if (assembly == null)
						assembly = assemblyName;
				}
				else
					assembly = null;

				assemblyMap[id] = assembly;
			}
			return assembly;
		}
	}
}