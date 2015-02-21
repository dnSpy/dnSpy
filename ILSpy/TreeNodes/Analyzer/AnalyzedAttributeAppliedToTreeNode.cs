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
using System.Linq;
using System.Threading;
using ICSharpCode.Decompiler.Ast;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using ICSharpCode.NRefactory.Utils;
using System.Collections.Concurrent;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal sealed class AnalyzedAttributeAppliedToTreeNode : AnalyzerSearchTreeNode
	{
		private readonly TypeDef analyzedType;
		private readonly string attributeName;

		private AttributeTargets usage = AttributeTargets.All;
		private bool allowMutiple;
		private bool inherited = true;
		private ConcurrentDictionary<MethodDef, int> foundMethods;

		public static bool CanShow(TypeDef type)
		{
			return type.IsClass && type.IsCustomAttribute();
		}

		public AnalyzedAttributeAppliedToTreeNode(TypeDef analyzedType)
		{
			if (analyzedType == null)
				throw new ArgumentNullException("analyzedType");

			this.analyzedType = analyzedType;
			attributeName = this.analyzedType.FullName;
			GetAttributeUsage();
		}

		private void GetAttributeUsage()
		{
			if (analyzedType.HasCustomAttributes) {
				foreach (CustomAttribute ca in analyzedType.CustomAttributes) {
					ITypeDefOrRef t = ca.AttributeType;
					if (t != null && t.Name == "AttributeUsageAttribute" && t.Namespace == "System" &&
						ca.ConstructorArguments.Count > 0 &&
						ca.ConstructorArguments[0].Value is int) {
						this.usage = (AttributeTargets)ca.ConstructorArguments[0].Value;
						if (ca.ConstructorArguments.Count > 2) {
							if (ca.ConstructorArguments[1].Value is bool)
								this.allowMutiple = (bool)ca.ConstructorArguments[1].Value;
							if (ca.ConstructorArguments[2].Value is bool)
								this.inherited = (bool)ca.ConstructorArguments[2].Value;
						}
						foreach (var namedArgument in ca.Properties) {
							switch (namedArgument.Name) {
								case "AllowMultiple":
									if (namedArgument.Argument.Value is bool)
										this.allowMutiple = (bool)namedArgument.Argument.Value;
									break;
								case "Inherited":
									if (namedArgument.Argument.Value is bool)
										this.inherited = (bool)namedArgument.Argument.Value;
									break;
							}
						}
					}
				}
			}
		}

		public override object Text
		{
			get { return "Applied To"; }
		}

		protected override IEnumerable<AnalyzerTreeNode> FetchChildren(CancellationToken ct)
		{
			foundMethods = new ConcurrentDictionary<MethodDef, int>();

			//get the assemblies to search
			var currentAssembly = analyzedType.Module.Assembly;
			var assemblies = analyzedType.IsPublic ? GetReferencingAssemblies(currentAssembly, ct) : GetAssemblyAndAnyFriends(currentAssembly, ct);

			var results = assemblies.AsParallel().WithCancellation(ct).SelectMany(a => FindReferencesInAssembly(a.Item1.Modules, a.Item2, ct));

			foreach (var result in results.OrderBy(n => n.Text)) {
				yield return result;
			}

			foundMethods = null;
		}

		#region standard custom attributes

		private IEnumerable<AnalyzerTreeNode> FindReferencesInAssembly(IEnumerable<ModuleDef> modules, ITypeDefOrRef tr, CancellationToken ct)
		{
			foreach (var module in modules) {
				//since we do not display modules as separate entities, coalesce the assembly and module searches
				bool foundInAssyOrModule = false;

				if ((usage & AttributeTargets.Assembly) != 0) {
					AssemblyDef asm = module.Assembly;
					if (asm != null && asm.HasCustomAttributes) {
						foreach (var attribute in asm.CustomAttributes) {
							if (new SigComparer().Equals(attribute.AttributeType, tr)) {
								foundInAssyOrModule = true;
								break;
							}
						}
					}
				}

				if (!foundInAssyOrModule) {
					ct.ThrowIfCancellationRequested();

					//search module
					if ((usage & AttributeTargets.Module) != 0) {
						if (module.HasCustomAttributes) {
							foreach (var attribute in module.CustomAttributes) {
								if (new SigComparer().Equals(attribute.AttributeType, tr)) {
									foundInAssyOrModule = true;
									break;
								}
							}
						}
					}

				}

				if (foundInAssyOrModule) {
					yield return new AnalyzedAssemblyTreeNode(module);
				}

				ct.ThrowIfCancellationRequested();

				foreach (TypeDef type in TreeTraversal.PreOrder(module.Types, t => t.NestedTypes).OrderBy(t => t.FullName)) {
					ct.ThrowIfCancellationRequested();
					foreach (var result in FindReferencesWithinInType(type, tr)) {
						ct.ThrowIfCancellationRequested();
						yield return result;
					}
				}
			}
		}

		private IEnumerable<AnalyzerTreeNode> FindReferencesWithinInType(TypeDef type, ITypeDefOrRef attrTypeRef)
		{

			bool searchRequired = (type.IsClass && usage.HasFlag(AttributeTargets.Class))
				|| (type.IsEnum && usage.HasFlag(AttributeTargets.Enum))
				|| (type.IsInterface && usage.HasFlag(AttributeTargets.Interface))
				|| (Decompiler.DnlibExtensions.IsValueType(type) && usage.HasFlag(AttributeTargets.Struct));
			if (searchRequired) {
				if (type.HasCustomAttributes) {
					foreach (var attribute in type.CustomAttributes) {
						if (new SigComparer().Equals(attribute.AttributeType, attrTypeRef)) {
							var node = new AnalyzedTypeTreeNode(type);
							node.Language = this.Language;
							yield return node;
							break;
						}
					}
				}
			}

			if ((this.usage & AttributeTargets.GenericParameter) != 0 && type.HasGenericParameters) {
				foreach (var parameter in type.GenericParameters) {
					if (parameter.HasCustomAttributes) {
						foreach (var attribute in parameter.CustomAttributes) {
							if (new SigComparer().Equals(attribute.AttributeType, attrTypeRef)) {
								var node = new AnalyzedTypeTreeNode(type);
								node.Language = this.Language;
								yield return node;
								break;
							}
						}
					}
				}
			}

			if ((this.usage & AttributeTargets.Field) != 0 && type.HasFields) {
				foreach (var field in type.Fields) {
					if (field.HasCustomAttributes) {
						foreach (var attribute in field.CustomAttributes) {
							if (new SigComparer().Equals(attribute.AttributeType, attrTypeRef)) {
								var node = new AnalyzedFieldTreeNode(field);
								node.Language = this.Language;
								yield return node;
								break;
							}
						}
					}
				}
			}

			if (((usage & AttributeTargets.Property) != 0) && type.HasProperties) {
				foreach (var property in type.Properties) {
					if (property.HasCustomAttributes) {
						foreach (var attribute in property.CustomAttributes) {
							if (new SigComparer().Equals(attribute.AttributeType, attrTypeRef)) {
								var node = new AnalyzedPropertyTreeNode(property);
								node.Language = this.Language;
								yield return node;
								break;
							}
						}
					}
				}
			}
			if (((usage & AttributeTargets.Event) != 0) && type.HasEvents) {
				foreach (var _event in type.Events) {
					if (_event.HasCustomAttributes) {
						foreach (var attribute in _event.CustomAttributes) {
							if (new SigComparer().Equals(attribute.AttributeType, attrTypeRef)) {
								var node = new AnalyzedEventTreeNode(_event);
								node.Language = this.Language;
								yield return node;
								break;
							}
						}
					}
				}
			}

			if (type.HasMethods) {
				foreach (var method in type.Methods) {
					bool found = false;
					if ((usage & (AttributeTargets.Method | AttributeTargets.Constructor)) != 0) {
						if (method.HasCustomAttributes) {
							foreach (var attribute in method.CustomAttributes) {
								if (new SigComparer().Equals(attribute.AttributeType, attrTypeRef)) {
									found = true;
									break;
								}
							}
						}
					}
					if (!found &&
						((usage & AttributeTargets.ReturnValue) != 0) &&
						method.Parameters.ReturnParameter.HasParamDef &&
						method.Parameters.ReturnParameter.ParamDef.HasCustomAttributes) {
						foreach (var attribute in method.Parameters.ReturnParameter.ParamDef.CustomAttributes) {
							if (new SigComparer().Equals(attribute.AttributeType, attrTypeRef)) {
								found = true;
								break;
							}
						}
					}

					if (!found &&
						((usage & AttributeTargets.Parameter) != 0) &&
						method.Parameters.Count > 0) {
						foreach (var parameter in method.Parameters.Where(param => param.HasParamDef)) {
							if (parameter.IsHiddenThisParameter)
								continue;
							foreach (var attribute in parameter.ParamDef.CustomAttributes) {
								if (new SigComparer().Equals(attribute.AttributeType, attrTypeRef)) {
									found = true;
									break;
								}
							}
						}
					}

					if (found) {
						MethodDef codeLocation = this.Language.GetOriginalCodeLocation(method) as MethodDef;
						if (codeLocation != null && !HasAlreadyBeenFound(codeLocation)) {
							var node = new AnalyzedMethodTreeNode(codeLocation);
							node.Language = this.Language;
							yield return node;
						}
					}
				}
			}
		}

		private bool HasAlreadyBeenFound(MethodDef method)
		{
			return !foundMethods.TryAdd(method, 0);
		}

		#endregion

		#region search scope

		private IEnumerable<Tuple<AssemblyDef, ITypeDefOrRef>> GetReferencingAssemblies(AssemblyDef asm, CancellationToken ct)
		{
			if (asm == null)
				yield break;
			yield return new Tuple<AssemblyDef, ITypeDefOrRef>(asm, this.analyzedType);

			IEnumerable<LoadedAssembly> assemblies = MainWindow.Instance.CurrentAssemblyList.GetAssemblies().Where(assy => assy.AssemblyDefinition != null);

			foreach (var assembly in assemblies) {
				ct.ThrowIfCancellationRequested();
				bool found = false;
				foreach (var reference in assembly.AssemblyDefinition.Modules.OfType<ModuleDefMD>().SelectMany(module => module.GetAssemblyRefs())) {
					if (AssemblyNameComparer.CompareAll.CompareTo(asm, reference) == 0) {
						found = true;
						break;
					}
				}
				if (found) {
					var typeref = GetScopeTypeReferenceInAssembly(assembly.AssemblyDefinition);
					if (typeref != null)
						yield return new Tuple<AssemblyDef, ITypeDefOrRef>(assembly.AssemblyDefinition, typeref);
				}
			}
		}

		private IEnumerable<Tuple<AssemblyDef, ITypeDefOrRef>> GetAssemblyAndAnyFriends(AssemblyDef asm, CancellationToken ct)
		{
			if (asm == null)
				yield break;
			yield return new Tuple<AssemblyDef, ITypeDefOrRef>(asm, analyzedType);

			if (asm.HasCustomAttributes) {
				var attributes = asm.CustomAttributes
					.Where(attr => attr.TypeFullName == "System.Runtime.CompilerServices.InternalsVisibleToAttribute");
				var friendAssemblies = new HashSet<string>();
				foreach (var attribute in attributes) {
					if (attribute.ConstructorArguments.Count == 0)
						continue;
					string assemblyName = attribute.ConstructorArguments[0].Value as UTF8String;
					if (assemblyName == null)
						continue;
					assemblyName = assemblyName.Split(',')[0]; // strip off any public key info
					friendAssemblies.Add(assemblyName);
				}

				if (friendAssemblies.Count > 0) {
					IEnumerable<LoadedAssembly> assemblies = MainWindow.Instance.CurrentAssemblyList.GetAssemblies().Where(assy => assy.AssemblyDefinition != null);

					foreach (var assembly in assemblies) {
						ct.ThrowIfCancellationRequested();
						if (friendAssemblies.Contains(assembly.AssemblyDefinition.Name)) {
							var typeref = GetScopeTypeReferenceInAssembly(assembly.AssemblyDefinition);
							if (typeref != null) {
								yield return new Tuple<AssemblyDef, ITypeDefOrRef>(assembly.AssemblyDefinition, typeref);
							}
						}
					}
				}
			}
		}

		private ITypeDefOrRef GetScopeTypeReferenceInAssembly(AssemblyDef asm)
		{
			foreach (var tmp in asm.Modules) {
				var mod = tmp as ModuleDefMD;
				if (mod == null)
					continue;
				foreach (var typeref in mod.GetTypeRefs()) {
					if (new SigComparer().Equals(analyzedType, typeref))
						return typeref;
				}
			}
			return null;
		}

		#endregion
	}
	internal static class ExtensionMethods
	{
		public static bool HasCustomAttribute(this IMemberRef member, string attributeTypeName)
		{
			return false;
		}
	}
}