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
using Mono.Cecil;
using Mono.Cecil.Cil;
using ICSharpCode.NRefactory.Utils;
using System.Collections.Concurrent;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	internal sealed class AnalyzedAttributeAppliedToTreeNode : AnalyzerSearchTreeNode
	{
		private readonly TypeDefinition analyzedType;
		private readonly string attributeName;

		private AttributeTargets usage = AttributeTargets.All;
		private bool allowMutiple;
		private bool inherited = true;
		private ConcurrentDictionary<MethodDefinition, int> foundMethods;

		public static bool CanShow(TypeDefinition type)
		{
			return type.IsClass && type.IsCustomAttribute();
		}

		public AnalyzedAttributeAppliedToTreeNode(TypeDefinition analyzedType)
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
					TypeReference t = ca.AttributeType;
					if (t.Name == "AttributeUsageAttribute" && t.Namespace == "System") {
						this.usage = (AttributeTargets)ca.ConstructorArguments[0].Value;
						if (ca.ConstructorArguments.Count > 1) {
							this.allowMutiple = (bool)ca.ConstructorArguments[1].Value;
							this.inherited = (bool)ca.ConstructorArguments[2].Value;
						}
						if (ca.HasProperties) {
							foreach (var namedArgument in ca.Properties) {
								switch (namedArgument.Name) {
									case "AllowMultiple":
										this.allowMutiple = (bool)namedArgument.Argument.Value;
										break;
									case "Inherited":
										this.inherited = (bool)namedArgument.Argument.Value;
										break;
								}
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
			foundMethods = new ConcurrentDictionary<MethodDefinition, int>();

			//get the assemblies to search
			var currentAssembly = analyzedType.Module.Assembly;
			var assemblies = analyzedType.IsPublic ? GetReferencingAssemblies(currentAssembly, ct) : GetAssemblyAndAnyFriends(currentAssembly, ct);

			var results = assemblies.AsParallel().WithCancellation(ct).SelectMany(a => FindReferencesInAssembly(a.Item1.MainModule, a.Item2, ct));

			foreach (var result in results.OrderBy(n => n.Text)) {
				yield return result;
			}

			foundMethods = null;
		}

		#region standard custom attributes

		private IEnumerable<AnalyzerTreeNode> FindReferencesInAssembly(ModuleDefinition module, TypeReference tr, CancellationToken ct)
		{
			//since we do not display modules as separate entities, coalesce the assembly and module searches
			bool foundInAssyOrModule = false;

			if ((usage & AttributeTargets.Assembly) != 0) {
				AssemblyDefinition asm = module.Assembly;
				if (asm != null && asm.HasCustomAttributes) {
					foreach (var attribute in asm.CustomAttributes) {
						if (attribute.AttributeType == tr) {
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
							if (attribute.AttributeType == tr) {
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

			foreach (TypeDefinition type in TreeTraversal.PreOrder(module.Types, t => t.NestedTypes).OrderBy(t => t.FullName)) {
				ct.ThrowIfCancellationRequested();
				foreach (var result in FindReferencesWithinInType(type, tr)) {
					ct.ThrowIfCancellationRequested();
					yield return result;
				}
			}
		}

		private IEnumerable<AnalyzerTreeNode> FindReferencesWithinInType(TypeDefinition type, TypeReference attrTypeRef)
		{

			bool searchRequired = (type.IsClass && usage.HasFlag(AttributeTargets.Class))
				|| (type.IsEnum && usage.HasFlag(AttributeTargets.Enum))
				|| (type.IsInterface && usage.HasFlag(AttributeTargets.Interface))
				|| (type.IsValueType && usage.HasFlag(AttributeTargets.Struct));
			if (searchRequired) {
				if (type.HasCustomAttributes) {
					foreach (var attribute in type.CustomAttributes) {
						if (attribute.AttributeType == attrTypeRef) {
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
							if (attribute.AttributeType == attrTypeRef) {
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
							if (attribute.AttributeType == attrTypeRef) {
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
							if (attribute.AttributeType == attrTypeRef) {
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
							if (attribute.AttributeType == attrTypeRef) {
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
								if (attribute.AttributeType == attrTypeRef) {
									found = true;
									break;
								}
							}
						}
					}
					if (!found &&
						((usage & AttributeTargets.ReturnValue) != 0) &&
						method.MethodReturnType.HasCustomAttributes) {
						foreach (var attribute in method.MethodReturnType.CustomAttributes) {
							if (attribute.AttributeType == attrTypeRef) {
								found = true;
								break;
							}
						}
					}

					if (!found &&
						((usage & AttributeTargets.Parameter) != 0) &&
						method.HasParameters) {
						foreach (var parameter in method.Parameters) {
							foreach (var attribute in parameter.CustomAttributes) {
								if (attribute.AttributeType == attrTypeRef) {
									found = true;
									break;
								}
							}
						}
					}

					if (found) {
						MethodDefinition codeLocation = this.Language.GetOriginalCodeLocation(method) as MethodDefinition;
						if (codeLocation != null && !HasAlreadyBeenFound(codeLocation)) {
							var node = new AnalyzedMethodTreeNode(codeLocation);
							node.Language = this.Language;
							yield return node;
						}
					}
				}
			}
		}

		private bool HasAlreadyBeenFound(MethodDefinition method)
		{
			return !foundMethods.TryAdd(method, 0);
		}

		#endregion

		#region search scope

		private IEnumerable<Tuple<AssemblyDefinition, TypeReference>> GetReferencingAssemblies(AssemblyDefinition asm, CancellationToken ct)
		{
			yield return new Tuple<AssemblyDefinition, TypeReference>(asm, this.analyzedType);

			string requiredAssemblyFullName = asm.FullName;

			IEnumerable<LoadedAssembly> assemblies = MainWindow.Instance.CurrentAssemblyList.GetAssemblies().Where(assy => assy.AssemblyDefinition != null);

			foreach (var assembly in assemblies) {
				ct.ThrowIfCancellationRequested();
				bool found = false;
				foreach (var reference in assembly.AssemblyDefinition.MainModule.AssemblyReferences) {
					if (requiredAssemblyFullName == reference.FullName) {
						found = true;
						break;
					}
				}
				if (found) {
					var typeref = GetScopeTypeReferenceInAssembly(assembly.AssemblyDefinition);
					if (typeref != null)
						yield return new Tuple<AssemblyDefinition, TypeReference>(assembly.AssemblyDefinition, typeref);
				}
			}
		}

		private IEnumerable<Tuple<AssemblyDefinition, TypeReference>> GetAssemblyAndAnyFriends(AssemblyDefinition asm, CancellationToken ct)
		{
			yield return new Tuple<AssemblyDefinition, TypeReference>(asm, analyzedType);

			if (asm.HasCustomAttributes) {
				var attributes = asm.CustomAttributes
					.Where(attr => attr.AttributeType.FullName == "System.Runtime.CompilerServices.InternalsVisibleToAttribute");
				var friendAssemblies = new HashSet<string>();
				foreach (var attribute in attributes) {
					string assemblyName = attribute.ConstructorArguments[0].Value as string;
					assemblyName = assemblyName.Split(',')[0]; // strip off any public key info
					friendAssemblies.Add(assemblyName);
				}

				if (friendAssemblies.Count > 0) {
					IEnumerable<LoadedAssembly> assemblies = MainWindow.Instance.CurrentAssemblyList.GetAssemblies();

					foreach (var assembly in assemblies) {
						ct.ThrowIfCancellationRequested();
						if (friendAssemblies.Contains(assembly.ShortName)) {
							var typeref = GetScopeTypeReferenceInAssembly(assembly.AssemblyDefinition);
							if (typeref != null) {
								yield return new Tuple<AssemblyDefinition, TypeReference>(assembly.AssemblyDefinition, typeref);
							}
						}
					}
				}
			}
		}

		private TypeReference GetScopeTypeReferenceInAssembly(AssemblyDefinition asm)
		{
			foreach (var typeref in asm.MainModule.GetTypeReferences()) {
				if (typeref.Name == analyzedType.Name && typeref.Namespace == analyzedType.Namespace) {
					return typeref;
				}
			}
			return null;
		}

		#endregion
	}
	internal static class ExtensionMethods
	{
		public static bool HasCustomAttribute(this MemberReference member, string attributeTypeName)
		{
			return false;
		}
	}
}