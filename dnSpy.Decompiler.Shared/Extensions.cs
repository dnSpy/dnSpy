/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.IO;
using dnlib.PE;

namespace dnSpy.Decompiler.Shared {
	public static class Extensions {
		public static bool IsDefined(this IHasCustomAttribute provider, UTF8String ns, UTF8String name) {
			if (provider == null || provider.CustomAttributes.Count == 0)
				return false;
			foreach (var ca in provider.CustomAttributes) {
				var tr = ca.AttributeType as TypeRef;
				if (tr != null) {
					if (tr.Namespace == ns && tr.Name == name)
						return true;
					continue;
				}

				var td = ca.AttributeType as TypeDef;
				if (td != null) {
					if (td.Namespace == ns && td.Name == name)
						return true;
					continue;
				}
			}
			return false;
		}

		public static CustomAttribute Find(this IHasCustomAttribute provider, UTF8String ns, UTF8String name) {
			if (provider == null || provider.CustomAttributes.Count == 0)
				return null;
			foreach (var ca in provider.CustomAttributes) {
				var tr = ca.AttributeType as TypeRef;
				if (tr != null) {
					if (tr.Namespace == ns && tr.Name == name)
						return ca;
					continue;
				}

				var td = ca.AttributeType as TypeDef;
				if (td != null) {
					if (td.Namespace == ns && td.Name == name)
						return ca;
					continue;
				}
			}
			return null;
		}

		public static bool GetRVA(this IMemberDef member, out uint rva, out long fileOffset) {
			rva = 0;
			fileOffset = 0;

			if (member is MethodDef)
				rva = (uint)(member as MethodDef).RVA;
			else if (member is FieldDef)
				rva = (uint)(member as FieldDef).RVA;
			if (rva == 0)
				return false;

			fileOffset = member.Module.ToFileOffset(rva);
			return true;
		}

		public static IImageStream GetImageStream(this ModuleDef module, uint rva) {
			var m = module as ModuleDefMD;//TODO: Support CorModuleDef
			if (m == null)
				return null;

			return m.MetaData.PEImage.CreateStream((RVA)rva);
		}

		public static long ToFileOffset(this ModuleDef module, uint rva) {
			var m = module as ModuleDefMD;//TODO: Support CorModuleDef
			if (m == null)
				return (uint)rva;
			return (long)m.MetaData.PEImage.ToFileOffset((RVA)rva);
		}

		public static int GetCodeSize(this CilBody body) {
			if (body == null || body.Instructions.Count == 0)
				return 0;
			var instr = body.Instructions[body.Instructions.Count - 1];
			return (int)instr.Offset + instr.GetSize();
		}

		public static IList<Parameter> GetParameters(this IMethod method) {
			if (method == null || method.MethodSig == null)
				return new List<Parameter>();

			var md = method as MethodDef;
			if (md != null)
				return md.Parameters;

			var list = new List<Parameter>();
			int paramIndex = 0, methodSigIndex = 0;
			if (method.MethodSig.HasThis)
				list.Add(new Parameter(paramIndex++, Parameter.HIDDEN_THIS_METHOD_SIG_INDEX, method.DeclaringType.ToTypeSig()));
			foreach (var type in method.MethodSig.GetParams())
				list.Add(new Parameter(paramIndex++, methodSigIndex++, type));
			return list;
		}

		public static IEnumerable<MethodDef> GetAllMethods(this PropertyDef p) {
			foreach (var m in p.GetMethods) yield return m;
			foreach (var m in p.SetMethods) yield return m;
			foreach (var m in p.OtherMethods) yield return m;
		}

		public static IEnumerable<MethodDef> GetAllMethods(this EventDef e) {
			if (e.AddMethod != null)
				yield return e.AddMethod;
			if (e.InvokeMethod != null)
				yield return e.InvokeMethod;
			if (e.RemoveMethod != null)
				yield return e.RemoveMethod;
			foreach (var m in e.OtherMethods)
				yield return m;
		}

		public static HashSet<MethodDef> GetPropEventMethods(this TypeDef type) {
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

		public static bool IsIndexer(this PropertyDef prop) {
			if (prop == null || prop.PropertySig.GetParamCount() == 0)
				return false;

			var accessor = prop.GetMethod ?? prop.SetMethod;
			var basePropDef = prop;
			if (accessor != null && accessor.HasOverrides) {
				var baseAccessor = accessor.Overrides.First().MethodDeclaration.ResolveMethodDef();
				if (baseAccessor != null) {
					foreach (PropertyDef baseProp in baseAccessor.DeclaringType.Properties) {
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

		public static TypeDef Resolve(this IType type) {
			return type == null ? null : type.ScopeType.ResolveTypeDef();
		}

		public static bool CanSortFields(this TypeDef type) {
			return type.IsAutoLayout;
		}

		public static bool CanSortMethods(this TypeDef type) {
			return !(type.IsInterface && type.IsImport);
		}

		public static IEnumerable<IMemberDef> GetNonSortedMethodsPropsEvents(this TypeDef type) {
			var hash = new HashSet<MethodDef>();
			var defs = new List<Tuple<IMemberDef, List<MethodDef>>>();
			foreach (var p in type.Properties) {
				var methods = new List<MethodDef>(p.GetAllMethods());
				foreach (var m in methods)
					hash.Add(m);
				methods.Sort((a, b) => a.MDToken.Raw.CompareTo(b.MDToken.Raw));
				defs.Add(Tuple.Create((IMemberDef)p, methods));
			}
			foreach (var e in type.Events) {
				var methods = new List<MethodDef>(e.GetAllMethods());
				foreach (var m in methods)
					hash.Add(m);
				methods.Sort((a, b) => a.MDToken.Raw.CompareTo(b.MDToken.Raw));
				defs.Add(Tuple.Create((IMemberDef)e, methods));
			}
			foreach (var m in type.Methods) {
				if (hash.Contains(m))
					continue;
				defs.Add(Tuple.Create((IMemberDef)m, new List<MethodDef> { m }));
			}
			defs.Sort((a, b) => {
				if (a.Item2.Count == 0 || b.Item2.Count == 0)
					return b.Item2.Count.CompareTo(a.Item2.Count);
				return a.Item2[0].MDToken.Raw.CompareTo(b.Item2[0].MDToken.Raw);
			});
			return defs.Select(a => a.Item1);
		}
	}
}
