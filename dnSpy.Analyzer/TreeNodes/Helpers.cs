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

using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace dnSpy.Analyzer.TreeNodes {
	static class Helpers {
		public static bool IsReferencedBy(TypeDef type, ITypeDefOrRef typeRef) {
			return IsReferencedBy(type, typeRef, 0);
		}

		static bool IsReferencedBy(TypeDef type, ITypeDefOrRef typeRef, int depth) {
			if (depth >= 30)
				return false;
			// TODO: move it to a better place after adding support for more cases.
			if (type == null)
				return false;
			if (typeRef == null)
				return false;

			if (type == typeRef)
				return true;
			if (type.Name != typeRef.Name)
				return false;
			if (type.Namespace != typeRef.Namespace)
				return false;

			if (type.DeclaringType != null || typeRef.DeclaringType != null) {
				if (type.DeclaringType == null || typeRef.DeclaringType == null)
					return false;
				if (!IsReferencedBy(type.DeclaringType, typeRef.DeclaringType, depth + 1))
					return false;
			}

			return true;
		}

		public static IMemberRef GetOriginalCodeLocation(IMemberRef member) {
			if (member is MethodDef)
				return GetOriginalCodeLocation((MethodDef)member);
			return member;
		}

		static MethodDef GetOriginalCodeLocation(MethodDef method) {
			if (IsCompilerGenerated(method)) {
				return FindMethodUsageInType(method.DeclaringType, method) ?? method;
			}

			var typeUsage = GetOriginalCodeLocation(method.DeclaringType);

			return typeUsage ?? method;
		}

		/// <summary>
		/// Given a compiler-generated type, returns the method where that type is used.
		/// Used to detect the 'parent method' for a lambda/iterator/async state machine.
		/// </summary>
		static MethodDef GetOriginalCodeLocation(TypeDef type) {
			if (type != null && type.DeclaringType != null && IsCompilerGenerated(type)) {
				if (type.IsValueType) {
					// Value types might not have any constructor; but they must be stored in a local var
					// because 'initobj' (or 'call .ctor') expects a managed ref.
					return FindVariableOfTypeUsageInType(type.DeclaringType, type);
				}
				else {
					MethodDef constructor = GetTypeConstructor(type);
					if (constructor == null)
						return null;
					return FindMethodUsageInType(type.DeclaringType, constructor);
				}
			}
			return null;
		}

		static MethodDef GetTypeConstructor(TypeDef type) {
			return type.FindConstructors().FirstOrDefault();
		}

		static MethodDef FindMethodUsageInType(TypeDef type, MethodDef analyzedMethod) {
			string name = analyzedMethod.Name;
			foreach (MethodDef method in type.Methods) {
				bool found = false;
				if (!method.HasBody)
					continue;
				foreach (Instruction instr in method.Body.Instructions) {
					IMethod mr = instr.Operand as IMethod;
					if (mr != null && !mr.IsField && mr.Name == name &&
						IsReferencedBy(analyzedMethod.DeclaringType, mr.DeclaringType) &&
						mr.ResolveMethodDef() == analyzedMethod) {
						found = true;
						break;
					}
				}

				if (found)
					return method;
			}
			return null;
		}

		static MethodDef FindVariableOfTypeUsageInType(TypeDef type, TypeDef variableType) {
			foreach (MethodDef method in type.Methods) {
				bool found = false;
				if (!method.HasBody)
					continue;
				foreach (var v in method.Body.Variables) {
					if (ResolveWithinSameModule(v.Type.ToTypeDefOrRef()) == variableType) {
						found = true;
						break;
					}
				}

				if (found)
					return method;
			}
			return null;
		}

		static TypeDef ResolveWithinSameModule(ITypeDefOrRef type) {
			if (type != null && type.Scope == type.Module)
				return type.ResolveTypeDef();
			return null;
		}

		static bool IsCompilerGenerated(this IHasCustomAttribute hca) {
			return hca != null && hca.CustomAttributes.IsDefined("System.Runtime.CompilerServices.CompilerGeneratedAttribute");
		}
	}
}
