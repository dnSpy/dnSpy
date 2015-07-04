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
		public ModuleDef Module { get; private set; }
		public IKnownThings KnownThings { get; private set; }

		public Dictionary<ushort, AssemblyInfoRecord> AssemblyIdMap { get; private set; }
		public Dictionary<ushort, AttributeInfoRecord> AttributeIdMap { get; private set; }
		public Dictionary<ushort, StringInfoRecord> StringIdMap { get; private set; }
		public Dictionary<ushort, TypeInfoRecord> TypeIdMap { get; private set; }

		BamlContext(ModuleDef module) {
			Module = module;
			KnownThings = module.IsClr40 ? (IKnownThings)new KnownThingsv4(module) : new KnownThingsv3(module);

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
	}
}