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
using System.Threading;
using dnlib.DotNet;
using dnSpy.Analyzer.Properties;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class TypeExposedByNode : SearchNode {
		readonly TypeDef analyzedType;
		Guid comGuid;
		bool isComType;

		public TypeExposedByNode(TypeDef analyzedType) => this.analyzedType = analyzedType ?? throw new ArgumentNullException(nameof(analyzedType));

		protected override void Write(ITextColorWriter output, IDecompiler decompiler) =>
			output.Write(BoxedTextColor.Text, dnSpy_Analyzer_Resources.ExposedByTreeNode);

		protected override IEnumerable<AnalyzerTreeNodeData> FetchChildren(CancellationToken ct) {
			bool includeAllModules;
			isComType = ComUtils.IsComType(analyzedType, out comGuid);
			includeAllModules = isComType;
			var options = ScopedWhereUsedAnalyzerOptions.None;
			if (includeAllModules)
				options |= ScopedWhereUsedAnalyzerOptions.IncludeAllModules;
			if (isComType)
				options |= ScopedWhereUsedAnalyzerOptions.ForcePublic;
			var analyzer = new ScopedWhereUsedAnalyzer<AnalyzerTreeNodeData>(Context.DocumentService, analyzedType, FindReferencesInType, options);
			return analyzer.PerformAnalysis(ct);
		}

		IEnumerable<AnalyzerTreeNodeData> FindReferencesInType(TypeDef type) {
			if (analyzedType.IsEnum && new SigComparer().Equals(type, analyzedType))
				yield break;

			foreach (FieldDef field in type.Fields) {
				if (TypeIsExposedBy(field)) {
					yield return new FieldNode(field) { Context = Context };
				}
			}

			foreach (PropertyDef property in type.Properties) {
				if (TypeIsExposedBy(property)) {
					yield return new PropertyNode(property) { Context = Context };
				}
			}

			foreach (EventDef eventDef in type.Events) {
				if (TypeIsExposedBy(eventDef)) {
					yield return new EventNode(eventDef) { Context = Context };
				}
			}

			foreach (MethodDef method in type.Methods) {
				if (TypeIsExposedBy(method)) {
					yield return new MethodNode(method) { Context = Context };
				}
			}
		}

		bool CheckType(IType? type) => CheckType(type, 0);

		const int maxRecursion = 20;
		bool CheckType(IType? type, int recursionCounter) {
			if (recursionCounter > maxRecursion)
				return false;
			if (type is TypeSig ts)
				return CheckType(ts, recursionCounter + 1);
			if (type is TypeSpec typeSpec)
				return CheckType(typeSpec.TypeSig, recursionCounter + 1);
			if (isComType && type.Resolve() is TypeDef td && ComUtils.ComEquals(td, ref comGuid))
				return true;
			return new SigComparer().Equals(analyzedType, type);
		}

		bool CheckType(TypeSig? sig, int recursionCounter) {
			if (recursionCounter > maxRecursion)
				return false;
			if (sig is null)
				return false;
			switch (sig.ElementType) {
			case ElementType.Void:		return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.Void);
			case ElementType.Boolean:	return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.Boolean);
			case ElementType.Char:		return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.Char);
			case ElementType.I1:		return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.SByte);
			case ElementType.U1:		return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.Byte);
			case ElementType.I2:		return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.Int16);
			case ElementType.U2:		return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.UInt16);
			case ElementType.I4:		return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.Int32);
			case ElementType.U4:		return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.UInt32);
			case ElementType.I8:		return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.Int64);
			case ElementType.U8:		return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.UInt64);
			case ElementType.R4:		return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.Single);
			case ElementType.R8:		return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.Double);
			case ElementType.String:	return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.String);
			case ElementType.TypedByRef:return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.TypedReference);
			case ElementType.I:			return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.IntPtr);
			case ElementType.U:			return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.UIntPtr);
			case ElementType.Object:	return new SigComparer().Equals(analyzedType, analyzedType.Module.CorLibTypes.Object);

			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.Array:
			case ElementType.SZArray:
			case ElementType.ValueArray:
			case ElementType.Module:
			case ElementType.Pinned:
				return CheckType(sig.Next, recursionCounter + 1);

			case ElementType.CModReqd:
			case ElementType.CModOpt:
				return CheckType(((ModifierSig)sig).Modifier, recursionCounter + 1) || CheckType(sig.Next, recursionCounter + 1);

			case ElementType.ValueType:
			case ElementType.Class:
				return CheckType(((TypeDefOrRefSig)sig).TypeDefOrRef, recursionCounter + 1);

			case ElementType.GenericInst:
				if (CheckType(((GenericInstSig)sig).GenericType?.TypeDefOrRef, recursionCounter + 1))
					return true;
				foreach (var genericArg in ((GenericInstSig)sig).GenericArguments) {
					if (CheckType(genericArg, recursionCounter + 1))
						return true;
				}
				return false;

			case ElementType.FnPtr:
				return TypeIsExposedBy(((FnPtrSig)sig).MethodSig, recursionCounter + 1);

			case ElementType.End:
			case ElementType.Var:
			case ElementType.R:
			case ElementType.MVar:
			case ElementType.Internal:
			case ElementType.Sentinel:
			default:
				return false;
			}
		}

		bool TypeIsExposedBy(FieldDef field) {
			if (field.IsPrivate)
				return false;

			return CheckType(field.FieldType);
		}

		bool TypeIsExposedBy(PropertyDef property) {
			if (IsPrivate(property))
				return false;

			return TypeIsExposedBy(property.PropertySig);
		}

		bool TypeIsExposedBy(EventDef eventDef) {
			if (IsPrivate(eventDef))
				return false;

			return CheckType(eventDef.EventType);
		}

		bool TypeIsExposedBy(MethodDef method) {
			// if the method has overrides, it is probably an explicit interface member
			// and should be considered part of the public API even though it is marked private.
			if (method.IsPrivate) {
				if (!method.HasOverrides)
					return false;
				var methDecl = method.Overrides[0].MethodDeclaration;
				var typeDef = methDecl?.DeclaringType?.ResolveTypeDef();
				if (typeDef is not null && !typeDef.IsInterface)
					return false;
			}

			// exclude methods with 'semantics'. for example, property getters & setters.
			// HACK: this is a potentially fragile implementation, as the MethodSemantics may be extended to other uses at a later date.
			if (method.SemanticsAttributes != MethodSemanticsAttributes.None)
				return false;

			return TypeIsExposedBy(method.MethodSig);
		}

		bool TypeIsExposedBy(MethodBaseSig? methodSig) => TypeIsExposedBy(methodSig, 0);

		bool TypeIsExposedBy(MethodBaseSig? methodSig, int recursionCounter) {
			if (recursionCounter > maxRecursion)
				return false;
			if (methodSig is null)
				return false;

			if (CheckType(methodSig.RetType))
				return true;
			foreach (var type in methodSig.Params) {
				if (CheckType(type))
					return true;
			}
			return false;
		}

		static bool IsPrivate(PropertyDef property) {
			bool isGetterPublic = (property.GetMethod is not null && !property.GetMethod.IsPrivate);
			bool isSetterPublic = (property.SetMethod is not null && !property.SetMethod.IsPrivate);
			return !(isGetterPublic || isSetterPublic);
		}

		static bool IsPrivate(EventDef eventDef) {
			bool isAdderPublic = (eventDef.AddMethod is not null && !eventDef.AddMethod.IsPrivate);
			bool isRemoverPublic = (eventDef.RemoveMethod is not null && !eventDef.RemoveMethod.IsPrivate);
			bool isInvokerPublic = (eventDef.InvokeMethod is not null && !eventDef.InvokeMethod.IsPrivate);
			return !(isAdderPublic || isRemoverPublic || isInvokerPublic);
		}

		public static bool CanShow(TypeDef type) => !(type.IsAbstract && type.IsSealed);
	}
}
