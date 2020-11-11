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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Documents.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Search;

namespace dnSpy.Search {
	/// <summary>
	/// Searches types/members/etc for text. A filter decides which type/member/etc to check.
	/// </summary>
	sealed class FilterSearcher {
		readonly FilterSearcherOptions options;

		public FilterSearcher(FilterSearcherOptions options) => this.options = options;

		bool IsMatch(string? text, object? obj) => options.SearchComparer.IsMatch(text, obj);

		public void SearchAssemblies(IEnumerable<DsDocumentNode> fileNodes) {
			foreach (var fileNode in fileNodes) {
				options.CancellationToken.ThrowIfCancellationRequested();
				if (fileNode is AssemblyDocumentNode)
					SearchAssemblyInternal((AssemblyDocumentNode)fileNode);
				else if (fileNode is ModuleDocumentNode)
					SearchModule(fileNode.Document);
			}
		}

		public void SearchTypes(IEnumerable<SearchTypeInfo> types) {
			foreach (var info in types) {
				options.CancellationToken.ThrowIfCancellationRequested();
				if (info.Type.DeclaringType is null)
					Search(info.Document, info.Type.Namespace, info.Type);
				else
					Search(info.Document, info.Type);
			}
		}

		void CheckCustomAttributes(IDsDocument file, IHasCustomAttribute hca, object? parent) {
			var res = options.Filter.GetResultAttributes(hca);
			if (!res.IsMatch)
				return;
			foreach (var ca in hca.GetCustomAttributes()) {
				options.CancellationToken.ThrowIfCancellationRequested();
				foreach (var o in ca.ConstructorArguments) {
					options.CancellationToken.ThrowIfCancellationRequested();
					if (CheckCA(file, hca, parent, o))
						return;
				}
				foreach (var o in ca.NamedArguments) {
					options.CancellationToken.ThrowIfCancellationRequested();
					if (CheckCA(file, hca, parent, o.Argument))
						return;
				}
			}
		}

		bool CheckCA(IDsDocument file, IHasCustomAttribute hca, object? parent, CAArgument o) {
			var value = o.Value;
			var u = value as UTF8String;
			if (u is not null)
				value = u.String;
			if (!IsMatch(null, value))
				return false;
			options.OnMatch(new SearchResult {
				Context = options.Context,
				Object = hca,
				NameObject = hca,
				ObjectImageReference = GetImageReference(hca),
				LocationObject = parent is string s ? new NamespaceSearchResult(s) : parent,
				LocationImageReference = GetImageReference(parent),
				Document = file,
			});
			return true;
		}

		ImageReference GetImageReference(object? obj) {
			if (obj is ModuleDef)
				return options.DotNetImageService.GetImageReference((ModuleDef)obj);
			if (obj is AssemblyDef)
				return options.DotNetImageService.GetImageReference((AssemblyDef)obj);
			if (obj is TypeDef)
				return options.DotNetImageService.GetImageReference((TypeDef)obj);
			if (obj is MethodDef)
				return options.DotNetImageService.GetImageReference((MethodDef)obj);
			if (obj is FieldDef)
				return options.DotNetImageService.GetImageReference((FieldDef)obj);
			if (obj is PropertyDef)
				return options.DotNetImageService.GetImageReference((PropertyDef)obj);
			if (obj is EventDef)
				return options.DotNetImageService.GetImageReference((EventDef)obj);
			if (obj is ParamDef)
				return options.DotNetImageService.GetImageReferenceParameter();
			if (obj is GenericParam)
				return options.DotNetImageService.GetImageReferenceGenericParameter();
			if (obj is string)
				return options.DotNetImageService.GetNamespaceImageReference();

			return new ImageReference();
		}

		void SearchAssemblyInternal(AssemblyDocumentNode asmNode) {
			if (asmNode is null)
				return;
			var asm = asmNode.Document.AssemblyDef;
			Debug2.Assert(asm is not null);
			if (asm is null)
				return;
			var res = options.Filter.GetResult(asm);
			if (res.FilterType == FilterType.Hide)
				return;
			CheckCustomAttributes(asmNode.Document, asm, null);

			if (res.IsMatch && (IsMatch(asm.FullName, asmNode.Document) || IsMatch(asm.Name, null))) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = asm,
					NameObject = asm,
					ObjectImageReference = options.DotNetImageService.GetImageReference(asmNode.Document.ModuleDef!),
					LocationObject = null,
					LocationImageReference = new ImageReference(),
					Document = asmNode.Document,
				});
			}

			if (asmNode.TreeNode.LazyLoading) {
				options.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
					asmNode.TreeNode.EnsureChildrenLoaded();
				}));
			}
			var modChildren = asmNode.TreeNode.DataChildren.OfType<ModuleDocumentNode>().ToArray();

			foreach (var node in asmNode.TreeNode.DataChildren) {
				options.CancellationToken.ThrowIfCancellationRequested();
				if (node is ModuleDocumentNode modNode)
					SearchModule(modNode.Document);
			}
		}

		void SearchModule(IDsDocument module) {
			if (module is null)
				return;
			var mod = module.ModuleDef;
			if (mod is null) {
				SearchNonNetFile(module);
				return;
			}

			var res = options.Filter.GetResult(mod);
			if (res.FilterType == FilterType.Hide)
				return;
			CheckCustomAttributes(module, mod, mod.Assembly);

			if (res.IsMatch && IsMatch(mod.FullName, module)) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = mod,
					NameObject = mod,
					ObjectImageReference = options.DotNetImageService.GetImageReference(mod),
					LocationObject = mod.Assembly,
					LocationImageReference = mod.Assembly is not null ? options.DotNetImageService.GetImageReference(mod.Assembly.ManifestModule) : new ImageReference(),
					Document = module,
				});
			}

			SearchModAsmReferences(module);
			SearchResources(module);

			foreach (var kv in GetNamespaces(mod)) {
				options.CancellationToken.ThrowIfCancellationRequested();
				Search(module, kv.Key, kv.Value);
			}
		}

		void SearchModAsmReferences(IDsDocument module) {
			var res = options.Filter.GetResult((ReferencesFolderNode?)null);
			if (res.FilterType == FilterType.Hide)
				return;

			foreach (var asmRef in module.ModuleDef!.GetAssemblyRefs()) {
				options.CancellationToken.ThrowIfCancellationRequested();
				res = options.Filter.GetResult(asmRef);
				if (res.FilterType == FilterType.Hide)
					continue;

				if (res.IsMatch && (IsMatch(asmRef.FullName, asmRef) || IsMatch(asmRef.Name, null))) {
					options.OnMatch(new SearchResult {
						Context = options.Context,
						Object = asmRef,
						NameObject = asmRef,
						ObjectImageReference = options.DotNetImageService.GetImageReferenceAssemblyRef(),
						LocationObject = module.ModuleDef,
						LocationImageReference = options.DotNetImageService.GetImageReference(module.ModuleDef),
						Document = module,
					});
				}
			}

			foreach (var modRef in module.ModuleDef.GetModuleRefs()) {
				options.CancellationToken.ThrowIfCancellationRequested();
				res = options.Filter.GetResult(modRef);
				if (res.FilterType == FilterType.Hide)
					continue;

				if (res.IsMatch && IsMatch(modRef.FullName, modRef)) {
					options.OnMatch(new SearchResult {
						Context = options.Context,
						Object = modRef,
						NameObject = modRef,
						ObjectImageReference = options.DotNetImageService.GetImageReferenceModuleRef(),
						LocationObject = module.ModuleDef,
						LocationImageReference = options.DotNetImageService.GetImageReference(module.ModuleDef),
						Document = module,
					});
				}
			}
		}

		void SearchResources(IDsDocument module) {
			var res = options.Filter.GetResult((ResourcesFolderNode?)null);
			if (res.FilterType == FilterType.Hide)
				return;

			res = options.Filter.GetResult((ResourceNode?)null);
			if (res.FilterType == FilterType.Hide)
				return;

			var resNodes = new List<ResourceNode>();
			options.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
				var modNode = options.DocumentTreeView.FindNode(module.ModuleDef);
				if (modNode is null)
					return;
				modNode.TreeNode.EnsureChildrenLoaded();
				var resFolder = modNode.TreeNode.Children.FirstOrDefault(a => a.Data is ResourcesFolderNode);
				if (resFolder is not null) {
					resFolder.EnsureChildrenLoaded();
					resNodes.AddRange(resFolder.DataChildren.OfType<ResourceNode>());
				}
			}));

			foreach (var node in resNodes) {
				options.CancellationToken.ThrowIfCancellationRequested();
				SearchResourceTreeNodes(module, node);
			}
		}

		string? ToString(IResourceNode resource) {
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

		void SearchResourceTreeNodes(IDsDocument module, ResourceNode resTreeNode) {
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
					LocationImageReference = options.DotNetImageService.GetImageReference(module.ModuleDef!),
					Document = module,
				});
			}

			res = options.Filter.GetResult((ResourceElementNode?)null);
			if (res.FilterType == FilterType.Hide)
				return;

			var resNodes = new List<ResourceElementNode>();
			options.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
				resTreeNode.TreeNode.EnsureChildrenLoaded();
				resNodes.AddRange(resTreeNode.TreeNode.DataChildren.OfType<ResourceElementNode>());
			}));

			foreach (var resElNode in resNodes) {
				options.CancellationToken.ThrowIfCancellationRequested();
				SearchResourceElementTreeNode(module, resTreeNode, resElNode);
			}
		}

		void SearchResourceElementTreeNode(IDsDocument module, ResourceNode resTreeNode, ResourceElementNode resElNode) {
			var res = options.Filter.GetResult(resElNode);
			if (res.FilterType == FilterType.Hide)
				return;

			if (res.IsMatch) {
				bool m = IsMatch(resElNode.Name, resElNode) || IsMatch(Uri.UnescapeDataString(resElNode.Name), resElNode);
				if (!m) {
					if (resElNode.ResourceElement.ResourceData is BuiltInResourceData builtin) {
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
						Document = module,
					});
				}
			}
		}

		Dictionary<string, List<TypeDef>> GetNamespaces(ModuleDef module) {
			var ns = new Dictionary<string, List<TypeDef>>(StringComparer.Ordinal);

			foreach (var type in module.Types) {
				if (!ns.TryGetValue(type.Namespace, out var list))
					ns.Add(type.Namespace, list = new List<TypeDef>());
				list.Add(type);
			}

			return ns;
		}

		void SearchNonNetFile(IDsDocument nonNetFile) {
			if (nonNetFile is null)
				return;
			var res = options.Filter.GetResult(nonNetFile);
			if (res.FilterType == FilterType.Hide)
				return;

			if (res.IsMatch && IsMatch(nonNetFile.GetShortName(), nonNetFile)) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = nonNetFile,
					NameObject = nonNetFile,
					ObjectImageReference = options.DotNetImageService.GetImageReference(nonNetFile.PEImage!),
					LocationObject = null,
					LocationImageReference = new ImageReference(),
					Document = nonNetFile,
				});
			}
		}

		void Search(IDsDocument ownerModule, string ns, List<TypeDef> types) {
			var res = options.Filter.GetResult(ns, ownerModule);
			if (res.FilterType == FilterType.Hide)
				return;

			if (res.IsMatch && IsMatch(ns, ns)) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = ns,
					NameObject = new NamespaceSearchResult(ns),
					ObjectImageReference = options.DotNetImageService.GetNamespaceImageReference(),
					LocationObject = ownerModule.ModuleDef,
					LocationImageReference = options.DotNetImageService.GetImageReference(ownerModule.ModuleDef!),
					Document = ownerModule,
				});
			}

			foreach (var type in types) {
				options.CancellationToken.ThrowIfCancellationRequested();
				Search(ownerModule, ns, type);
			}
		}

		void Search(IDsDocument ownerModule, string nsOwner, TypeDef type) {
			CheckCustomAttributes(ownerModule, type, nsOwner);

			var res = options.Filter.GetResult(type);
			if (res.FilterType == FilterType.Hide)
				return;

			if (res.IsMatch && (IsMatch(FixTypeName(type.FullName), type) || IsMatch(FixTypeName(type.Name), type))) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = type,
					NameObject = type,
					ObjectImageReference = options.DotNetImageService.GetImageReference(type),
					LocationObject = new NamespaceSearchResult(nsOwner),
					LocationImageReference = options.DotNetImageService.GetNamespaceImageReference(),
					Document = ownerModule,
				});
			}

			SearchMembers(ownerModule, type);

			foreach (var subType in type.GetTypes()) {
				options.CancellationToken.ThrowIfCancellationRequested();
				Search(ownerModule, subType);
			}
		}

		static string FixTypeName(string name) {
			int i;
			for (i = 0; i < name.Length; i++) {
				var c = name[i];
				if (c == '/' || c == '`')
					break;
			}
			if (i == name.Length)
				return name;
			var sb = new StringBuilder();
			sb.Append(name, 0, i);
			for (; i < name.Length; i++) {
				var c = name[i];
				switch (c) {
				case '/':
					sb.Append('.');
					break;
				case '`':
					// Ignore `1, `2 etc (generic types)
					while (++i < name.Length && char.IsDigit(name[i]))
						;
					break;
				default:
					sb.Append(c);
					break;
				}
			}
			return sb.ToString();
		}

		void Search(IDsDocument ownerModule, TypeDef type) {
			CheckCustomAttributes(ownerModule, type, type.DeclaringType);

			var res = options.Filter.GetResult(type);
			if (res.FilterType == FilterType.Hide)
				return;

			if (res.IsMatch && (IsMatch(FixTypeName(type.FullName), type) || IsMatch(FixTypeName(type.Name), type))) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = type,
					NameObject = type,
					ObjectImageReference = options.DotNetImageService.GetImageReference(type),
					LocationObject = type.DeclaringType,
					LocationImageReference = options.DotNetImageService.GetImageReference(type.DeclaringType),
					Document = ownerModule,
				});
			}

			SearchMembers(ownerModule, type);
		}

		void SearchMembers(IDsDocument ownerModule, TypeDef type) {
			foreach (var method in type.Methods) {
				options.CancellationToken.ThrowIfCancellationRequested();
				Search(ownerModule, type, method);
			}
			foreach (var field in type.Fields) {
				options.CancellationToken.ThrowIfCancellationRequested();
				Search(ownerModule, type, field);
			}
			foreach (var prop in type.Properties) {
				options.CancellationToken.ThrowIfCancellationRequested();
				Search(ownerModule, type, prop);
			}
			foreach (var evt in type.Events) {
				options.CancellationToken.ThrowIfCancellationRequested();
				Search(ownerModule, type, evt);
			}
		}

		bool CheckMatch(MethodDef method) {
			if (IsMatch(method.Name, method))
				return true;
			if (IsMatch(FixTypeName(method.DeclaringType.FullName) + "." + method.Name.String, method) ||
				IsMatch(FixTypeName(method.DeclaringType.FullName) + "::" + method.Name.String, method))
				return true;

			if (method.ImplMap is ImplMap im) {
				if (IsMatch(im.Name, im) || IsMatch(im.Module?.Name, null))
					return true;
			}

			return false;
		}

		void Search(IDsDocument ownerModule, TypeDef type, MethodDef method) {
			var res = options.Filter.GetResult(method);
			if (res.FilterType == FilterType.Hide)
				return;
			CheckCustomAttributes(ownerModule, method, type);

			if (res.IsMatch && CheckMatch(method)) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = method,
					NameObject = method,
					ObjectImageReference = options.DotNetImageService.GetImageReference(method),
					LocationObject = type,
					LocationImageReference = options.DotNetImageService.GetImageReference(type),
					Document = ownerModule,
				});
				return;
			}

			res = options.Filter.GetResultParamDefs(method);
			if (res.FilterType != FilterType.Hide) {
				foreach (var pd in method.ParamDefs) {
					options.CancellationToken.ThrowIfCancellationRequested();
					CheckCustomAttributes(ownerModule, pd, method);
					res = options.Filter.GetResult(method, pd);
					if (res.FilterType == FilterType.Hide)
						continue;
					if (res.IsMatch && IsMatch(pd.Name, pd)) {
						options.OnMatch(new SearchResult {
							Context = options.Context,
							Object = method,
							NameObject = method,
							ObjectImageReference = options.DotNetImageService.GetImageReference(method),
							LocationObject = type,
							LocationImageReference = options.DotNetImageService.GetImageReference(type),
							Document = ownerModule,
						});
						return;
					}
				}
			}

			SearchBody(ownerModule, type, method);
		}

		void SearchBody(IDsDocument ownerModule, TypeDef type, MethodDef method) {
			CilBody body;

			var res = options.Filter.GetResultLocals(method);
			if (res.FilterType != FilterType.Hide) {
				body = method.Body;
				if (body is null)
					return; // Return immediately. All code here depends on a non-null body

				foreach (var local in body.Variables) {
					options.CancellationToken.ThrowIfCancellationRequested();
					res = options.Filter.GetResult(method, local);
					if (res.FilterType == FilterType.Hide)
						continue;
					if (res.IsMatch && IsMatch(local.Name, local)) {
						options.OnMatch(new SearchResult {
							Context = options.Context,
							Object = method,
							NameObject = method,
							ObjectImageReference = options.DotNetImageService.GetImageReference(method),
							LocationObject = type,
							LocationImageReference = options.DotNetImageService.GetImageReference(type),
							Document = ownerModule,
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
			if (body is null)
				return;
			int counter = 0;
			foreach (var instr in body.Instructions) {
				if (counter++ > 1000) {
					options.CancellationToken.ThrowIfCancellationRequested();
					counter = 0;
				}
				object? operand;
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
				if (operand is not null && IsMatch(null, operand)) {
					options.OnMatch(new SearchResult {
						Context = options.Context,
						Object = method,
						NameObject = method,
						ObjectImageReference = options.DotNetImageService.GetImageReference(method),
						LocationObject = type,
						LocationImageReference = options.DotNetImageService.GetImageReference(type),
						Document = ownerModule,
						ObjectInfo = new BodyResult(instr.Offset),
					});
					break;
				}
			}
		}

		bool CheckMatch(FieldDef field) {
			if (IsMatch(field.Name, field))
				return true;
			if (IsMatch(FixTypeName(field.DeclaringType.FullName) + "." + field.Name.String, field) ||
				IsMatch(FixTypeName(field.DeclaringType.FullName) + "::" + field.Name.String, field))
				return true;

			if (field.ImplMap is ImplMap im) {
				if (IsMatch(im.Name, im) || IsMatch(im.Module?.Name, null))
					return true;
			}

			return false;
		}

		void Search(IDsDocument ownerModule, TypeDef type, FieldDef field) {
			var res = options.Filter.GetResult(field);
			if (res.FilterType == FilterType.Hide)
				return;
			CheckCustomAttributes(ownerModule, field, type);

			if (res.IsMatch && CheckMatch(field)) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = field,
					NameObject = field,
					ObjectImageReference = options.DotNetImageService.GetImageReference(field),
					LocationObject = type,
					LocationImageReference = options.DotNetImageService.GetImageReference(type),
					Document = ownerModule,
				});
			}
		}

		bool CheckMatch(PropertyDef prop) {
			if (IsMatch(prop.Name, prop))
				return true;
			if (IsMatch(FixTypeName(prop.DeclaringType.FullName) + "." + prop.Name.String, prop) ||
				IsMatch(FixTypeName(prop.DeclaringType.FullName) + "::" + prop.Name.String, prop))
				return true;

			return false;
		}

		void Search(IDsDocument ownerModule, TypeDef type, PropertyDef prop) {
			var res = options.Filter.GetResult(prop);
			if (res.FilterType == FilterType.Hide)
				return;
			CheckCustomAttributes(ownerModule, prop, type);

			if (res.IsMatch && CheckMatch(prop)) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = prop,
					NameObject = prop,
					ObjectImageReference = options.DotNetImageService.GetImageReference(prop),
					LocationObject = type,
					LocationImageReference = options.DotNetImageService.GetImageReference(type),
					Document = ownerModule,
				});
			}
		}

		bool CheckMatch(EventDef evt) {
			if (IsMatch(evt.Name, evt))
				return true;
			if (IsMatch(FixTypeName(evt.DeclaringType.FullName) + "." + evt.Name.String, evt) ||
				IsMatch(FixTypeName(evt.DeclaringType.FullName) + "::" + evt.Name.String, evt))
				return true;

			return false;
		}

		void Search(IDsDocument ownerModule, TypeDef type, EventDef evt) {
			var res = options.Filter.GetResult(evt);
			if (res.FilterType == FilterType.Hide)
				return;
			CheckCustomAttributes(ownerModule, evt, type);

			if (res.IsMatch && CheckMatch(evt)) {
				options.OnMatch(new SearchResult {
					Context = options.Context,
					Object = evt,
					NameObject = evt,
					ObjectImageReference = options.DotNetImageService.GetImageReference(evt),
					LocationObject = type,
					LocationImageReference = options.DotNetImageService.GetImageReference(type),
					Document = ownerModule,
				});
			}
		}
	}
}
