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

using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.PE;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Extensions
	/// </summary>
	public static class Extensions {
		/// <summary>
		/// Checks whether a custom attribute exists
		/// </summary>
		/// <param name="provider">Custom attribute provider</param>
		/// <param name="namespace">Namespace of custom attribute</param>
		/// <param name="name">Name of custom attribute</param>
		/// <returns></returns>
		public static bool IsDefined(this IHasCustomAttribute provider, UTF8String @namespace, UTF8String name) {
			if (provider == null || provider.CustomAttributes.Count == 0)
				return false;
			foreach (var ca in provider.CustomAttributes) {
				if (ca.AttributeType is TypeRef tr) {
					if (tr.Namespace == @namespace && tr.Name == name)
						return true;
					continue;
				}

				if (ca.AttributeType is TypeDef td) {
					if (td.Namespace == @namespace && td.Name == name)
						return true;
					continue;
				}
			}
			return false;
		}

		/// <summary>
		/// Gets the RVA and file offset of a member definition. Returns false if the RVA and
		/// file offsets aren't known or if there's no RVA (eg. there's no method body)
		/// </summary>
		/// <param name="member">Member</param>
		/// <param name="rva">Updated with the RVA</param>
		/// <param name="fileOffset">Updated with the file offset</param>
		/// <returns></returns>
		public static bool GetRVA(this IMemberDef member, out uint rva, out long fileOffset) {
			rva = 0;
			fileOffset = 0;

			if (member is MethodDef)
				rva = (uint)(member as MethodDef).RVA;
			else if (member is FieldDef)
				rva = (uint)(member as FieldDef).RVA;
			if (rva == 0)
				return false;

			var fo = member.Module.ToFileOffset(rva);
			if (fo == null)
				return false;
			fileOffset = fo.Value;
			return true;
		}

		/// <summary>
		/// Converts an RVA to a file offset
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="rva">RVA</param>
		/// <returns></returns>
		public static long? ToFileOffset(this ModuleDef module, uint rva) {
			var m = module as ModuleDefMD;//TODO: Support CorModuleDef
			if (m == null)
				return null;
			return (long)m.MetaData.PEImage.ToFileOffset((RVA)rva);
		}

		/// <summary>
		/// Gets the size of the code in the method body
		/// </summary>
		/// <param name="body">Method body, can be null</param>
		/// <returns></returns>
		public static int GetCodeSize(this CilBody body) {
			if (body == null || body.Instructions.Count == 0)
				return 0;
			var instr = body.Instructions[body.Instructions.Count - 1];
			return (int)instr.Offset + instr.GetSize();
		}

		/// <summary>
		/// Gets all parameters
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		public static IList<Parameter> GetParameters(this IMethod method) {
			if (method == null || method.MethodSig == null)
				return new List<Parameter>();

			if (method is MethodDef md)
				return md.Parameters;

			var list = new List<Parameter>();
			int paramIndex = 0, methodSigIndex = 0;
			if (method.MethodSig.HasThis)
				list.Add(new Parameter(paramIndex++, Parameter.HIDDEN_THIS_METHOD_SIG_INDEX, method.DeclaringType.ToTypeSig()));
			foreach (var type in method.MethodSig.GetParams())
				list.Add(new Parameter(paramIndex++, methodSigIndex++, type));
			return list;
		}

		static IEnumerable<MethodDef> GetAllMethods(this PropertyDef p) {
			foreach (var m in p.GetMethods) yield return m;
			foreach (var m in p.SetMethods) yield return m;
			foreach (var m in p.OtherMethods) yield return m;
		}

		static IEnumerable<MethodDef> GetAllMethods(this EventDef e) {
			if (e.AddMethod != null)
				yield return e.AddMethod;
			if (e.InvokeMethod != null)
				yield return e.InvokeMethod;
			if (e.RemoveMethod != null)
				yield return e.RemoveMethod;
			foreach (var m in e.OtherMethods)
				yield return m;
		}

		/// <summary>
		/// Gets all methods that are part of properties or events
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		public static HashSet<MethodDef> GetPropertyAndEventMethods(this TypeDef type) {
			var hash = new HashSet<MethodDef>();
			foreach (var p in type.Properties) {
				foreach (var m in p.GetAllMethods())
					hash.Add(m);
			}
			foreach (var e in type.Events) {
				foreach (var m in e.GetAllMethods())
					hash.Add(m);
			}
			hash.Remove(null);
			return hash;
		}

		/// <summary>
		/// Checks whether <paramref name="property"/> is an indexer property
		/// </summary>
		/// <param name="property">Property to check</param>
		/// <returns></returns>
		public static bool IsIndexer(this PropertyDef property) {
			if (property == null || property.PropertySig.GetParamCount() == 0)
				return false;

			var accessor = property.GetMethod ?? property.SetMethod;
			var basePropDef = property;
			if (accessor != null && accessor.HasOverrides) {
				var baseAccessor = accessor.Overrides.First().MethodDeclaration.ResolveMethodDef();
				if (baseAccessor != null) {
					foreach (var baseProp in baseAccessor.DeclaringType.Properties) {
						if (baseProp.GetMethod == baseAccessor || baseProp.SetMethod == baseAccessor) {
							basePropDef = baseProp;
							break;
						}
					}
				}
				else
					return false;
			}
			var defaultMemberName = GetDefaultMemberName(basePropDef.DeclaringType);
			if (defaultMemberName == basePropDef.Name)
				return true;

			return false;
		}

		static string GetDefaultMemberName(TypeDef type) {
			if (type == null)
				return null;
			foreach (var ca in type.CustomAttributes.FindAll("System.Reflection.DefaultMemberAttribute")) {
				if (ca.Constructor != null && ca.Constructor.FullName == @"System.Void System.Reflection.DefaultMemberAttribute::.ctor(System.String)" &&
					ca.ConstructorArguments.Count == 1 &&
					ca.ConstructorArguments[0].Value is UTF8String) {
					return (UTF8String)ca.ConstructorArguments[0].Value;
				}
			}
			return null;
		}

		/// <summary>
		/// Resolves a type
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		public static TypeDef Resolve(this IType type) => type?.ScopeType.ResolveTypeDef();

		/// <summary>
		/// Returns true if the fields can be sorted and false if the original metadata order must be used
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		public static bool CanSortFields(this TypeDef type) => type.IsAutoLayout;

		/// <summary>
		/// Returns true if the methods can be sorted and false if the original metadata order must be used
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		public static bool CanSortMethods(this TypeDef type) => !(type.IsInterface && type.IsImport);

		/// <summary>
		/// Gets all methods, properties and events. They're returned in the original metadata order.
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		public static IEnumerable<IMemberDef> GetNonSortedMethodsPropertiesEvents(this TypeDef type) {
			var hash = new HashSet<MethodDef>();
			var defs = new List<(IMemberDef def, List<MethodDef> list)>();
			foreach (var p in type.Properties) {
				var methods = new List<MethodDef>(p.GetAllMethods());
				foreach (var m in methods)
					hash.Add(m);
				methods.Sort((a, b) => a.MDToken.Raw.CompareTo(b.MDToken.Raw));
				defs.Add((p, methods));
			}
			foreach (var e in type.Events) {
				var methods = new List<MethodDef>(e.GetAllMethods());
				foreach (var m in methods)
					hash.Add(m);
				methods.Sort((a, b) => a.MDToken.Raw.CompareTo(b.MDToken.Raw));
				defs.Add((e, methods));
			}
			foreach (var m in type.Methods) {
				if (hash.Contains(m))
					continue;
				defs.Add((m, new List<MethodDef> { m }));
			}
			defs.Sort((a, b) => {
				if (a.list.Count == 0 || b.list.Count == 0)
					return b.list.Count.CompareTo(a.list.Count);
				return a.list[0].MDToken.Raw.CompareTo(b.list[0].MDToken.Raw);
			});
			return defs.Select(a => a.def);
		}
	}
}
