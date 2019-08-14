// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Analyzer.TreeNodes {
	/// <summary>
	/// Base class for analyzer nodes that perform a search.
	/// </summary>
	abstract class SearchNode : AnalyzerTreeNodeData, IAsyncCancellable {
		protected SearchNode() {
		}

		public override void Initialize() => TreeNode.LazyLoading = true;
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.Search;

		public override IEnumerable<TreeNodeData> CreateChildren() {
			Debug2.Assert(asyncFetchChildrenHelper is null);
			asyncFetchChildrenHelper = new AsyncFetchChildrenHelper(this, () => asyncFetchChildrenHelper = null);
			yield break;
		}
		AsyncFetchChildrenHelper? asyncFetchChildrenHelper;

		protected abstract IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct);
		internal IEnumerable<AnalyzerTreeNodeData> FetchChildrenInternal(CancellationToken token) => FetchChildren(token);

		public override void OnIsVisibleChanged() {
			if (!TreeNode.IsVisible && !(asyncFetchChildrenHelper is null) && !asyncFetchChildrenHelper.CompletedSuccessfully) {
				CancelAndClearChildren();
				TreeNode.LazyLoading = true;
			}
 		}

		public override void OnIsExpandedChanged(bool isExpanded) {
			if (!isExpanded && !(asyncFetchChildrenHelper is null) && !asyncFetchChildrenHelper.CompletedSuccessfully) {
				CancelAndClearChildren();
				TreeNode.LazyLoading = true;
			}
		}

		public override bool HandleAssemblyListChanged(IDsDocument[] removedAssemblies, IDsDocument[] addedAssemblies) {
			// only cancel a running analysis if user has manually added/removed assemblies
			bool manualAdd = false;
			foreach (var asm in addedAssemblies) {
				if (!asm.IsAutoLoaded)
					manualAdd = true;
			}
			if (removedAssemblies.Length > 0 || manualAdd) {
				CancelAndClearChildren();
			}
			return true;
		}

		public override bool HandleModelUpdated(IDsDocument[] documents) {
			CancelAndClearChildren();
			return true;
		}

		void CancelAndClearChildren() {
			AnalyzerTreeNodeData.CancelSelfAndChildren(this);
			TreeNode.Children.Clear();
			TreeNode.LazyLoading = true;
		}

		public void Cancel() {
			asyncFetchChildrenHelper?.Cancel();
			asyncFetchChildrenHelper = null;
		}

		internal static bool CanIncludeModule(ModuleDef targetModule, ModuleDef? module) {
			if (module is null)
				return false;
			if (targetModule == module)
				return false;
			if (!(targetModule.Assembly is null) && targetModule.Assembly == module.Assembly)
				return false;
			return true;
		}

		internal static HashSet<string> GetFriendAssemblies(IDsDocumentService documentService, ModuleDef mod, out IDsDocument[] modules) {
			var asm = mod.Assembly;
			Debug2.Assert(!(asm is null));
			var friendAssemblies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var attribute in asm.CustomAttributes.FindAll("System.Runtime.CompilerServices.InternalsVisibleToAttribute")) {
				if (attribute.ConstructorArguments.Count == 0)
					continue;
				string assemblyName = attribute.ConstructorArguments[0].Value as UTF8String;
				if (assemblyName is null)
					continue;
				assemblyName = new AssemblyNameInfo(assemblyName).Name;
				friendAssemblies.Add(assemblyName);
			}
			modules = documentService.GetDocuments().Where(a => CanIncludeModule(mod, a.ModuleDef)).ToArray();
			foreach (var module in modules) {
				Debug2.Assert(!(module.ModuleDef is null));
				var asm2 = module.AssemblyDef;
				if (asm2 is null)
					continue;
				foreach (var attribute in asm2.CustomAttributes.FindAll("System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute")) {
					string assemblyName = attribute.ConstructorArguments[0].Value as UTF8String;
					if (assemblyName is null)
						continue;
					assemblyName = new AssemblyNameInfo(assemblyName).Name;
					if (StringComparer.OrdinalIgnoreCase.Equals(asm.Name.String, assemblyName))
						friendAssemblies.Add(asm2.Name);
				}
			}
			return friendAssemblies;
		}

		internal static void AddTypeEquivalentTypes(IDsDocumentService documentService, TypeDef analyzedType, List<TypeDef> analyzedTypes) {
			Debug.Assert(analyzedTypes.Count == 1 && analyzedTypes[0] == analyzedType);
			if (!TIAHelper.IsTypeDefEquivalent(analyzedType))
				return;
			foreach (var document in documentService.GetDocuments().Where(a => !(a.ModuleDef is null))) {
				foreach (var type in GetTypeEquivalentTypes(document.AssemblyDef, document.ModuleDef, analyzedType)) {
					if (type != analyzedType)
						analyzedTypes.Add(type);
				}
			}
		}

		static IEnumerable<TypeDef> GetTypeEquivalentTypes(AssemblyDef? assembly, ModuleDef? module, TypeDef type) {
			Debug.Assert(TIAHelper.IsTypeDefEquivalent(type));
			var typeRef = new ModuleDefUser("dummy").Import(type);
			foreach (var mod in GetModules(assembly, module)) {
				var otherType = mod.Find(typeRef);
				if (otherType != type && TIAHelper.IsTypeDefEquivalent(otherType) &&
					new SigComparer().Equals(otherType, type) &&
					!new SigComparer(SigComparerOptions.DontCheckTypeEquivalence).Equals(otherType, type)) {
					yield return otherType;
				}
			}
		}

		static IEnumerable<ModuleDef> GetModules(AssemblyDef? assembly, ModuleDef? module) {
			if (!(assembly is null)) {
				foreach (var mod in assembly.Modules)
					yield return mod;
			}
			else {
				if (!(module is null))
					yield return module;
			}
		}

		protected static IEnumerable<(ModuleDef module, ITypeDefOrRef type)> GetTypeEquivalentModulesAndTypes(List<TypeDef> analyzedTypes) {
			foreach (var type in analyzedTypes)
				yield return (type.Module, type);
		}

		internal static IEnumerable<ModuleDef> GetTypeEquivalentModules(List<TypeDef> analyzedTypes) {
			foreach (var type in analyzedTypes)
				yield return type.Module;
		}

		protected static bool CheckEquals(IMemberRef? mr1, IMemberRef? mr2) => Helpers.CheckEquals(mr1, mr2);
	}
}
