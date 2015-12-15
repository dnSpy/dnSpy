/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Shared.UI.Search;

namespace dnSpy.Search {
	/// <summary>
	/// Searches types/members/etc for text. A filter decides which type/member/etc to check.
	/// </summary>
	sealed class FilterSearcher {
		readonly FilterSearcherOptions options;

		public FilterSearcher(FilterSearcherOptions options) {
			this.options = options;
		}

		bool IsMatch(string text, object obj) {
			return options.SearchComparer.IsMatch(text, obj);
		}

		public void SearchAssemblies(IEnumerable<IDnSpyFileNode> fileNodes) {
			foreach (var fileNode in fileNodes) {
				options.CancellationToken.ThrowIfCancellationRequested();
				if (fileNode is IAssemblyFileNode)
					SearchAssemblyInternal((IAssemblyFileNode)fileNode);
				else if (fileNode is IModuleFileNode)
					SearchModule(fileNode.DnSpyFile);
			}
		}

		void SearchAssemblyInternal(IAssemblyFileNode asmNode) {
			if (asmNode == null)
				return;
			var asm = asmNode.DnSpyFile.AssemblyDef;
			Debug.Assert(asm != null);
			if (asm == null)
				return;
			var res = options.Filter.GetResult(asm);
			if (res.FilterType == FilterType.Hide)
				return;

			if (res.IsMatch && IsMatch(asm.FullName, asmNode.DnSpyFile)) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = asm,
					NameObject = asm,
					ObjectImageReference = options.DotNetImageManager.GetImageReference(asmNode.DnSpyFile.ModuleDef),
					LocationObject = null,
					LocationImageReference = new ImageReference(),
					DnSpyFile = asmNode.DnSpyFile,
				});
			}

			if (asmNode.TreeNode.LazyLoading) {
				options.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
					asmNode.TreeNode.EnsureChildrenLoaded();
				}));
			}
			var modChildren = asmNode.TreeNode.DataChildren.OfType<IModuleFileNode>().ToArray();

			foreach (var node in asmNode.TreeNode.DataChildren) {
				options.CancellationToken.ThrowIfCancellationRequested();
				var modNode = node as IModuleFileNode;
				if (modNode != null)
					SearchModule(modNode.DnSpyFile);
			}
		}

		void SearchModule(IDnSpyFile module) {
			if (module == null)
				return;
			var mod = module.ModuleDef;
			if (mod == null) {
				SearchNonNetFile(module);
				return;
			}

			var res = options.Filter.GetResult(mod);
			if (res.FilterType == FilterType.Hide)
				return;

			if (res.IsMatch && IsMatch(mod.FullName, module)) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = mod,
					NameObject = mod,
					ObjectImageReference = options.DotNetImageManager.GetImageReference(mod),
					LocationObject = mod.Assembly != null ? mod.Assembly : null,
					LocationImageReference = mod.Assembly != null ? options.DotNetImageManager.GetImageReference(mod.Assembly.ManifestModule) : new ImageReference(),
					DnSpyFile = module,
				});
			}

			SearchModAsmReferences(module);
			SearchResources(module);

			foreach (var kv in GetNamespaces(mod)) {
				options.CancellationToken.ThrowIfCancellationRequested();
				Search(module, kv.Key, kv.Value);
			}
		}

		void SearchModAsmReferences(IDnSpyFile module) {
			var res = options.Filter.GetResult((IReferencesFolderNode)null);
			if (res.FilterType == FilterType.Hide)
				return;

			foreach (var asmRef in module.ModuleDef.GetAssemblyRefs()) {
				res = options.Filter.GetResult(asmRef);
				if (res.FilterType == FilterType.Hide)
					continue;

				if (res.IsMatch && IsMatch(asmRef.FullName, asmRef)) {
					options.OnMatch(new SearchResult {
						Context = options.Context,
						Object = asmRef,
						NameObject = asmRef,
						ObjectImageReference = options.DotNetImageManager.GetImageReferenceAssemblyRef(),
						LocationObject = module.ModuleDef,
						LocationImageReference = options.DotNetImageManager.GetImageReference(module.ModuleDef),
						DnSpyFile = module,
					});
				}
			}

			foreach (var modRef in module.ModuleDef.GetModuleRefs()) {
				res = options.Filter.GetResult(modRef);
				if (res.FilterType == FilterType.Hide)
					continue;

				if (res.IsMatch && IsMatch(modRef.FullName, modRef)) {
					options.OnMatch(new SearchResult {
						Context = options.Context,
						Object = modRef,
						NameObject = modRef,
						ObjectImageReference = options.DotNetImageManager.GetImageReferenceModuleRef(),
						LocationObject = module.ModuleDef,
						LocationImageReference = options.DotNetImageManager.GetImageReference(module.ModuleDef),
						DnSpyFile = module,
					});
				}
			}
		}

		void SearchResources(IDnSpyFile module) {
			var res = options.Filter.GetResult((IResourcesFolderNode)null);
			if (res.FilterType == FilterType.Hide)
				return;

			res = options.Filter.GetResult((IResourceNode)null);
			if (res.FilterType == FilterType.Hide)
				return;

			var resNodes = new List<IResourceNode>();
			options.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
				var modNode = options.FileTreeView.FindNode(module.ModuleDef);
				if (modNode == null)
					return;
				modNode.TreeNode.EnsureChildrenLoaded();
				var resFolder = modNode.TreeNode.Children.FirstOrDefault(a => a.Data is IResourcesFolderNode);
				if (resFolder != null) {
					resFolder.EnsureChildrenLoaded();
					resNodes.AddRange(resFolder.DataChildren.OfType<IResourceNode>());
				}
			}));

			foreach (var node in resNodes)
				SearchResourceTreeNodes(module, node);
		}

		string ToString(IResourceDataProvider resource) {
			try {
				return resource.ToString(options.CancellationToken, options.SearchDecompiledData);
			}
			catch (OperationCanceledException) {
				throw;
			}
			catch {
			}
			return string.Empty;
		}

		void SearchResourceTreeNodes(IDnSpyFile module, IResourceNode resTreeNode) {
			var res = options.Filter.GetResult(resTreeNode);
			if (res.FilterType == FilterType.Hide)
				return;

			if (res.IsMatch && (IsMatch(resTreeNode.Name, resTreeNode) || IsMatch(ToString(resTreeNode), null))) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = resTreeNode,
					NameObject = resTreeNode,
					ObjectImageReference = resTreeNode.Icon,
					LocationObject = module.ModuleDef,
					LocationImageReference = options.DotNetImageManager.GetImageReference(module.ModuleDef),
					DnSpyFile = module,
				});
			}

			res = options.Filter.GetResult((IResourceElementNode)null);
			if (res.FilterType == FilterType.Hide)
				return;

			var resNodes = new List<IResourceElementNode>();
			options.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
				resTreeNode.TreeNode.EnsureChildrenLoaded();
				resNodes.AddRange(resTreeNode.TreeNode.DataChildren.OfType<IResourceElementNode>());
			}));

			foreach (var resElNode in resNodes)
				SearchResourceElementTreeNode(module, resTreeNode, resElNode);
		}

		void SearchResourceElementTreeNode(IDnSpyFile module, IResourceNode resTreeNode, IResourceElementNode resElNode) {
			var res = options.Filter.GetResult(resElNode);
			if (res.FilterType == FilterType.Hide)
				return;

			if (res.IsMatch) {
				bool m = IsMatch(resElNode.Name, resElNode);
				if (!m) {
					var builtin = resElNode.ResourceElement.ResourceData as BuiltInResourceData;
					if (builtin != null) {
						var val = builtin.Data;
						if (builtin.Code == ResourceTypeCode.TimeSpan)
							val = ((TimeSpan)val).Ticks;
						m = IsMatch(val as string, val);
					}
				}
				if (!m)
					m = IsMatch(ToString(resElNode), null);
				if (m) {
					options.OnMatch(new SearchResult {
						Context = options.Context,
						Object = resElNode,
						NameObject = resElNode,
						ObjectImageReference = resElNode.Icon,
						LocationObject = resTreeNode,
						LocationImageReference = resTreeNode.Icon,
						DnSpyFile = module,
					});
				}
			}
		}

		Dictionary<string, List<TypeDef>> GetNamespaces(ModuleDef module) {
			var ns = new Dictionary<string, List<TypeDef>>(StringComparer.Ordinal);

			foreach (var type in module.Types) {
				List<TypeDef> list;
				if (!ns.TryGetValue(type.Namespace, out list))
					ns.Add(type.Namespace, list = new List<TypeDef>());
				list.Add(type);
			}

			return ns;
		}

		void SearchNonNetFile(IDnSpyFile nonNetFile) {
			if (nonNetFile == null)
				return;
			var res = options.Filter.GetResult(nonNetFile);
			if (res.FilterType == FilterType.Hide)
				return;

			if (res.IsMatch && IsMatch(nonNetFile.GetShortName(), nonNetFile)) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = nonNetFile,
					NameObject = nonNetFile,
					ObjectImageReference = options.DotNetImageManager.GetImageReference(nonNetFile.PEImage),
					LocationObject = null,
					LocationImageReference = new ImageReference(),
					DnSpyFile = nonNetFile,
				});
			}
		}

		void Search(IDnSpyFile ownerModule, string ns, List<TypeDef> types) {
			var res = options.Filter.GetResult(ns, ownerModule);
			if (res.FilterType == FilterType.Hide)
				return;

			if (res.IsMatch && IsMatch(ns, ns)) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = ns,
					NameObject = new NamespaceSearchResult(ns),
					ObjectImageReference = options.DotNetImageManager.GetNamespaceImageReference(),
					LocationObject = ownerModule.ModuleDef,
					LocationImageReference = options.DotNetImageManager.GetImageReference(ownerModule.ModuleDef),
					DnSpyFile = ownerModule,
				});
			}

			foreach (var type in types) {
				options.CancellationToken.ThrowIfCancellationRequested();
				Search(ownerModule, ns, type);
			}
		}

		void Search(IDnSpyFile ownerModule, string nsOwner, TypeDef type) {
			var res = options.Filter.GetResult(type);
			if (res.FilterType == FilterType.Hide)
				return;

			if (res.IsMatch && (IsMatch(type.FullName, type) || IsMatch(type.Name, type))) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = type,
					NameObject = type,
					ObjectImageReference = options.DotNetImageManager.GetImageReference(type),
					LocationObject = new NamespaceSearchResult(nsOwner),
					LocationImageReference = options.DotNetImageManager.GetNamespaceImageReference(),
					DnSpyFile = ownerModule,
				});
			}

			SearchMembers(ownerModule, type);

			foreach (var subType in type.GetTypes()) {
				options.CancellationToken.ThrowIfCancellationRequested();
				Search(ownerModule, subType);
			}
		}

		void Search(IDnSpyFile ownerModule, TypeDef type) {
			var res = options.Filter.GetResult(type);
			if (res.FilterType == FilterType.Hide)
				return;

			if (res.IsMatch && (IsMatch(type.FullName, type) || IsMatch(type.Name, type))) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = type,
					NameObject = type,
					ObjectImageReference = options.DotNetImageManager.GetImageReference(type),
					LocationObject = type.DeclaringType,
					LocationImageReference = options.DotNetImageManager.GetImageReference(type.DeclaringType),
					DnSpyFile = ownerModule,
				});
			}

			SearchMembers(ownerModule, type);
		}

		void SearchMembers(IDnSpyFile ownerModule, TypeDef type) {
			foreach (var method in type.Methods)
				Search(ownerModule, type, method);
			options.CancellationToken.ThrowIfCancellationRequested();
			foreach (var field in type.Fields)
				Search(ownerModule, type, field);
			options.CancellationToken.ThrowIfCancellationRequested();
			foreach (var prop in type.Properties)
				Search(ownerModule, type, prop);
			options.CancellationToken.ThrowIfCancellationRequested();
			foreach (var evt in type.Events)
				Search(ownerModule, type, evt);
		}

		void Search(IDnSpyFile ownerModule, TypeDef type, MethodDef method) {
			var res = options.Filter.GetResult(method);
			if (res.FilterType == FilterType.Hide)
				return;

			ImplMap im;
			if (res.IsMatch && (IsMatch(method.Name, method) || ((im = method.ImplMap) != null && (IsMatch(im.Name, im) || IsMatch(im.Module == null ? null : im.Module.Name, null))))) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = method,
					NameObject = method,
					ObjectImageReference = options.DotNetImageManager.GetImageReference(method),
					LocationObject = type,
					LocationImageReference = options.DotNetImageManager.GetImageReference(type),
					DnSpyFile = ownerModule,
				});
				return;
			}

			res = options.Filter.GetResultParamDefs(method);
			if (res.FilterType != FilterType.Hide) {
				foreach (var pd in method.ParamDefs) {
					res = options.Filter.GetResult(method, pd);
					if (res.FilterType == FilterType.Hide)
						continue;
					if (res.IsMatch && IsMatch(pd.Name, pd)) {
						options.OnMatch(new SearchResult {
							Context = options.Context,
							Object = method,
							NameObject = method,
							ObjectImageReference = options.DotNetImageManager.GetImageReference(method),
							LocationObject = type,
							LocationImageReference = options.DotNetImageManager.GetImageReference(type),
							DnSpyFile = ownerModule,
						});
						return;
					}
				}
			}

			SearchBody(ownerModule, type, method);
		}

		void SearchBody(IDnSpyFile ownerModule, TypeDef type, MethodDef method) {
			bool loadedBody;
			SearchBody(ownerModule, type, method, out loadedBody);
			/*TODO:
			if (loadedBody)
				FreeMethodBody(method);
			*/
		}

		void SearchBody(IDnSpyFile ownerModule, TypeDef type, MethodDef method, out bool loadedBody) {
			loadedBody = false;
			CilBody body;

			var res = options.Filter.GetResultLocals(method);
			if (res.FilterType != FilterType.Hide) {
				body = method.Body;
				if (body == null)
					return; // Return immediately. All code here depends on a non-null body
				loadedBody = true;

				foreach (var local in body.Variables) {
					res = options.Filter.GetResult(method, local);
					if (res.FilterType == FilterType.Hide)
						continue;
					if (res.IsMatch && IsMatch(local.Name, local)) {
						options.OnMatch(new SearchResult {
							Context = options.Context,
							Object = method,
							NameObject = method,
							ObjectImageReference = options.DotNetImageManager.GetImageReference(method),
							LocationObject = type,
							LocationImageReference = options.DotNetImageManager.GetImageReference(type),
							DnSpyFile = ownerModule,
						});
						return;
					}
				}
			}

			res = options.Filter.GetResultBody(method);
			if (res.FilterType == FilterType.Hide)
				return;
			if (!res.IsMatch)
				return;

			body = method.Body;
			if (body == null)
				return; // Return immediately. All code here depends on a non-null body
			loadedBody = true;
			foreach (var instr in body.Instructions) {
				object operand;
				// Only check numbers and strings. Don't pass in any type of operand to IsMatch()
				switch (instr.OpCode.Code) {
				case Code.Ldc_I4_M1: operand = -1; break;
				case Code.Ldc_I4_0: operand = 0; break;
				case Code.Ldc_I4_1: operand = 1; break;
				case Code.Ldc_I4_2: operand = 2; break;
				case Code.Ldc_I4_3: operand = 3; break;
				case Code.Ldc_I4_4: operand = 4; break;
				case Code.Ldc_I4_5: operand = 5; break;
				case Code.Ldc_I4_6: operand = 6; break;
				case Code.Ldc_I4_7: operand = 7; break;
				case Code.Ldc_I4_8: operand = 8; break;
				case Code.Ldc_I4:
				case Code.Ldc_I4_S:
				case Code.Ldc_R4:
				case Code.Ldc_R8:
				case Code.Ldstr: operand = instr.Operand; break;
				default: operand = null; break;
				}
				if (operand != null && IsMatch(null, operand)) {
					options.OnMatch(new SearchResult {
						Context = options.Context,
						Object = method,
						NameObject = method,
						ObjectImageReference = options.DotNetImageManager.GetImageReference(method),
						LocationObject = type,
						LocationImageReference = options.DotNetImageManager.GetImageReference(type),
						DnSpyFile = ownerModule,
					});
					break;
				}
			}
		}

		void Search(IDnSpyFile ownerModule, TypeDef type, FieldDef field) {
			var res = options.Filter.GetResult(field);
			if (res.FilterType == FilterType.Hide)
				return;

			ImplMap im;
			if (res.IsMatch && (IsMatch(field.Name, field) || ((im = field.ImplMap) != null && (IsMatch(im.Name, im) || IsMatch(im.Module == null ? null : im.Module.Name, null))))) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = field,
					NameObject = field,
					ObjectImageReference = options.DotNetImageManager.GetImageReference(field),
					LocationObject = type,
					LocationImageReference = options.DotNetImageManager.GetImageReference(type),
					DnSpyFile = ownerModule,
				});
			}
		}

		void Search(IDnSpyFile ownerModule, TypeDef type, PropertyDef prop) {
			var res = options.Filter.GetResult(prop);
			if (res.FilterType == FilterType.Hide)
				return;

			if (res.IsMatch && IsMatch(prop.Name, prop)) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = prop,
					NameObject = prop,
					ObjectImageReference = options.DotNetImageManager.GetImageReference(prop),
					LocationObject = type,
					LocationImageReference = options.DotNetImageManager.GetImageReference(type),
					DnSpyFile = ownerModule,
				});
			}
		}

		void Search(IDnSpyFile ownerModule, TypeDef type, EventDef evt) {
			var res = options.Filter.GetResult(evt);
			if (res.FilterType == FilterType.Hide)
				return;

			if (res.IsMatch && IsMatch(evt.Name, evt)) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = evt,
					NameObject = evt,
					ObjectImageReference = options.DotNetImageManager.GetImageReference(evt),
					LocationObject = type,
					LocationImageReference = options.DotNetImageManager.GetImageReference(type),
					DnSpyFile = ownerModule,
				});
			}
		}
	}
}
