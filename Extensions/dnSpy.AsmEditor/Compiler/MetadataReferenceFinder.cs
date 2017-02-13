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
using System.Threading;
using dnlib.DotNet;

namespace dnSpy.AsmEditor.Compiler {
	struct MetadataReferenceFinder {
		readonly ModuleDef module;
		readonly CancellationToken cancellationToken;
		readonly Dictionary<IAssembly, AssemblyDef> assemblies;
		readonly HashSet<IAssembly> checkedContractsAssemblies;

		public MetadataReferenceFinder(ModuleDef module, CancellationToken cancellationToken) {
			this.module = module ?? throw new ArgumentNullException(nameof(module));
			this.cancellationToken = cancellationToken;
			assemblies = new Dictionary<IAssembly, AssemblyDef>(new AssemblyNameComparer(AssemblyNameComparerFlags.All & ~AssemblyNameComparerFlags.Version));
			checkedContractsAssemblies = new HashSet<IAssembly>(AssemblyNameComparer.CompareAll);
		}

		public IEnumerable<ModuleDef> Find(IEnumerable<string> extraAssemblyReferences) {
			Initialize(extraAssemblyReferences);
			yield return module;
			foreach (var asm in assemblies.Values) {
				foreach (var m in asm.Modules) {
					cancellationToken.ThrowIfCancellationRequested();
					yield return m;
				}
			}
		}

		void Initialize(IEnumerable<string> extraAssemblyReferences) {
			foreach (var asm in GetAssemblies(module, extraAssemblyReferences)) {
				cancellationToken.ThrowIfCancellationRequested();
				if (!assemblies.TryGetValue(asm, out var otherAsm))
					assemblies[asm] = asm;
				else if (asm.Version > otherAsm.Version)
					assemblies[asm] = asm;
			}
		}

		IEnumerable<AssemblyDef> GetAssemblies(ModuleDef module, IEnumerable<string> extraAssemblyReferences) {
			var asm = module.Assembly;
			if (asm != null) {
				foreach (var a in GetAssemblies(asm))
					yield return a;
			}

			foreach (var asmRef in GetAssemblyRefs(module, extraAssemblyReferences)) {
				cancellationToken.ThrowIfCancellationRequested();
				asm = module.Context.AssemblyResolver.Resolve(asmRef, module);
				if (asm == null)
					continue;
				foreach (var a in GetAssemblies(asm))
					yield return a;
			}
		}

		IEnumerable<IAssembly> GetAssemblyRefs(ModuleDef module, IEnumerable<string> extraAssemblyReferences) {
			foreach (var a in module.GetAssemblyRefs())
				yield return a;
			foreach (var s in extraAssemblyReferences) {
				var info = new AssemblyNameInfo(s);
				if (info.Version != null)
					yield return info;
			}
		}

		IEnumerable<AssemblyDef> GetAssemblies(AssemblyDef asm) {
			yield return asm;
			foreach (var m in asm.Modules) {
				cancellationToken.ThrowIfCancellationRequested();
				// Also include all contract assemblies since they have type forwarders
				// to eg. mscorlib.
				foreach (var a in GetResolvedContractAssemblies(m))
					yield return a;
			}
		}
		static readonly PublicKeyToken contractsPublicKeyToken = new PublicKeyToken("b03f5f7f11d50a3a");

		IEnumerable<AssemblyDef> GetResolvedContractAssemblies(ModuleDef module) {
			var nonContractAsms = new HashSet<IAssembly>(AssemblyNameComparer.CompareAll);
			var stack = new Stack<AssemblyRef>(module.GetAssemblyRefs());
			while (stack.Count > 0) {
				cancellationToken.ThrowIfCancellationRequested();
				var asmRef = stack.Pop();
				if (!contractsPublicKeyToken.Equals(asmRef.PublicKeyOrToken?.Token))
					continue;
				if (checkedContractsAssemblies.Contains(asmRef))
					continue;
				checkedContractsAssemblies.Add(asmRef);

				var contractsAsm = module.Context.AssemblyResolver.Resolve(asmRef, module);
				if (contractsAsm != null) {
					yield return contractsAsm;
					foreach (var m in contractsAsm.Modules) {
						foreach (var ar in m.GetAssemblyRefs()) {
							cancellationToken.ThrowIfCancellationRequested();
							if (contractsPublicKeyToken.Equals(ar.PublicKeyOrToken))
								stack.Push(ar);
							else
								nonContractAsms.Add(ar);
						}
					}
				}
			}
			foreach (var asmRef in nonContractAsms) {
				cancellationToken.ThrowIfCancellationRequested();
				var asm = module.Context.AssemblyResolver.Resolve(asmRef, module);
				if (asm != null)
					yield return asm;
			}
		}
	}
}
