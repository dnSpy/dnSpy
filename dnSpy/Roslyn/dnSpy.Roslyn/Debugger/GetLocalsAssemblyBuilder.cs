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
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;
using dnSpy.Roslyn.Debugger.ExpressionCompiler;
using Microsoft.CodeAnalysis.ExpressionEvaluator;
using Microsoft.CodeAnalysis.ExpressionEvaluator.DnSpy;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace dnSpy.Roslyn.Debugger {
	struct GetLocalsAssemblyBuilder {
		readonly LanguageExpressionCompiler language;
		readonly MethodDef sourceMethod;
		/*readonly*/ ImmutableArray<string> localVariableNames;
		/*readonly*/ ImmutableArray<string> parameterNames;
		readonly List<DSEELocalAndMethod> localAndMethodBuilder;
		readonly ModuleDefUser? generatedModule;
		readonly TypeDef? getLocalsType;
		int methodNameIndex;
		MethodSig? lastMethodSig;

		const string getLocalsTypeNamespace = "";
		const string getLocalsTypeName = "<>x";
		const string methodNamePrefix = "<>m";

		public GetLocalsAssemblyBuilder(LanguageExpressionCompiler language, MethodDef method, ImmutableArray<string> localVariableNames, ImmutableArray<string> parameterNames) {
			this.language = language;
			sourceMethod = method;
			this.localVariableNames = localVariableNames;
			this.parameterNames = parameterNames;
			methodNameIndex = 0;
			lastMethodSig = null;
			localAndMethodBuilder = new List<DSEELocalAndMethod>();
			if (method.Parameters.Count == 0 && (method.Body?.Variables.Count ?? 0) == 0) {
				generatedModule = null;
				getLocalsType = null;
			}
			else {
				var methodModule = method.Module;
				generatedModule = new ModuleDefUser(Guid.NewGuid().ToString(), Guid.NewGuid(), methodModule.CorLibTypes.AssemblyRef);
				generatedModule.RuntimeVersion = methodModule.RuntimeVersion;
				generatedModule.Machine = methodModule.Machine;
				var asm = new AssemblyDefUser(Guid.NewGuid().ToString());
				asm.Modules.Add(generatedModule);
				getLocalsType = new TypeDefUser(getLocalsTypeNamespace, getLocalsTypeName, generatedModule.CorLibTypes.Object.TypeDefOrRef);
				getLocalsType.Attributes = TypeAttributes.NotPublic | TypeAttributes.AutoLayout | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.SpecialName | TypeAttributes.BeforeFieldInit | TypeAttributes.AnsiClass;
				generatedModule.Types.Add(getLocalsType);
				foreach (var gp in method.DeclaringType.GenericParameters)
					getLocalsType.GenericParameters.Add(Clone(gp));
			}
		}

		GenericParam Clone(GenericParam gp) {
			var clone = new GenericParamUser(gp.Number, gp.Flags, gp.Name);
			foreach (var gpc in gp.GenericParamConstraints)
				clone.GenericParamConstraints.Add(Clone(gpc));
			return clone;
		}

		GenericParamConstraint Clone(GenericParamConstraint gpc) =>
			new GenericParamConstraintUser((ITypeDefOrRef)generatedModule!.Import(gpc.Constraint));

		public byte[] Compile(out DSEELocalAndMethod[] locals, out string typeName, out string? errorMessage) {
			if (generatedModule is null) {
				locals = Array.Empty<DSEELocalAndMethod>();
				typeName = string.Empty;
				errorMessage = null;
				return Array.Empty<byte>();
			}
			Debug2.Assert(!(getLocalsType is null));

			foreach (var p in sourceMethod.Parameters) {
				var name = language.GetVariableName(GetName(p), isThis: p.IsHiddenThisParameter);
				var kind = p.IsHiddenThisParameter ? LocalAndMethodKind.This : LocalAndMethodKind.Parameter;
				var (methodName, flags) = AddMethod(p.Type, p.Index, isLocal: false);
				localAndMethodBuilder.Add(new DSEELocalAndMethod(name, name, methodName, flags, kind, p.Index, Guid.Empty, null));
			}

			var body = sourceMethod.Body;
			if (!(body is null)) {
				foreach (var l in body.Variables) {
					var name = language.GetVariableName(GetName(l), isThis: false);
					const LocalAndMethodKind kind = LocalAndMethodKind.Local;
					var (methodName, flags) = AddMethod(l.Type, l.Index, isLocal: true);
					localAndMethodBuilder.Add(new DSEELocalAndMethod(name, name, methodName, flags, kind, l.Index, Guid.Empty, null));
				}
			}

			var memStream = new MemoryStream();
			var writerOptions = new ModuleWriterOptions(generatedModule);
			generatedModule.Write(memStream, writerOptions);
			locals = localAndMethodBuilder.ToArray();
			typeName = getLocalsType.ReflectionFullName;
			errorMessage = null;
			return memStream.ToArray();
		}

		string GetName(Parameter p) {
			if (p.IsHiddenThisParameter)
				return "this";
			string name;
			if (!parameterNames.IsDefault && (uint)p.Index < (uint)parameterNames.Length) {
				name = parameterNames[p.Index];
				if (!string.IsNullOrEmpty(name))
					return name;
			}
			name = p.Name;
			if (!string.IsNullOrEmpty(name))
				return name;
			return "A_" + p.Index.ToString();
		}

		string GetName(Local l) {
			if (l.Index < localVariableNames.Length)
				return localVariableNames[l.Index];
			return "V_" + l.Index.ToString();
		}

		(string methodName, DkmClrCompilationResultFlags flags) AddMethod(TypeSig type, int index, bool isLocal) {
			Debug2.Assert(!(generatedModule is null));
			Debug2.Assert(!(getLocalsType is null));
			var methodName = methodNamePrefix + methodNameIndex++.ToString();

			var callConv = CallingConvention.Default;
			if (sourceMethod.MethodSig.Generic)
				callConv |= CallingConvention.Generic;
			var methodSig = new MethodSig(callConv, sourceMethod.MethodSig.GenParamCount);
			methodSig.RetType = generatedModule.Import(type.RemovePinnedAndModifiers());
			if (methodSig.RetType.IsByRef)
				methodSig.RetType = methodSig.RetType.Next.RemovePinnedAndModifiers();

			if (!(lastMethodSig is null)) {
				foreach (var p in lastMethodSig.Params)
					methodSig.Params.Add(p);
			}
			else {
				if (sourceMethod.MethodSig.HasThis)
					methodSig.Params.Add(generatedModule.Import(sourceMethod.DeclaringType).ToTypeSig());
				foreach (var p in sourceMethod.MethodSig.Params)
					methodSig.Params.Add(generatedModule.Import(p));
			}

			const MethodImplAttributes methodImplFlags = MethodImplAttributes.IL | MethodImplAttributes.Managed;
			const MethodAttributes methodFlags = MethodAttributes.Assembly | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.ReuseSlot;
			var method = new MethodDefUser(methodName, methodSig, methodImplFlags, methodFlags);
			getLocalsType.Methods.Add(method);

			foreach (var gp in sourceMethod.GenericParameters)
				method.GenericParameters.Add(Clone(gp));

			var body = new CilBody();
			method.Body = body;
			body.InitLocals = true;
			if (sourceMethod.Body is CilBody sourceBody) {
				foreach (var l in sourceBody.Variables)
					body.Variables.Add(new Local(generatedModule.Import(l.Type), l.Name));
			}
			body.Instructions.Add(CreateLoadVariable(method, body.Variables, index, isLocal));
			if (type.RemovePinnedAndModifiers().GetElementType() == ElementType.ByRef)
				body.Instructions.Add(LoadIndirect(type.RemovePinnedAndModifiers()?.Next.RemovePinnedAndModifiers()));
			body.Instructions.Add(Instruction.Create(OpCodes.Ret));

			lastMethodSig = methodSig;
			var flags = DkmClrCompilationResultFlags.None;
			if (methodSig.RetType.RemovePinnedAndModifiers().GetElementType() == ElementType.Boolean)
				flags |= DkmClrCompilationResultFlags.BoolResult;
			return (methodName, flags);
		}

		static Instruction CreateLoadVariable(MethodDef method, IList<Local> locals, int index, bool isLocal) {
			if (isLocal) {
				if (index == 0)
					return Instruction.Create(OpCodes.Ldloc_0);
				if (index == 1)
					return Instruction.Create(OpCodes.Ldloc_1);
				if (index == 2)
					return Instruction.Create(OpCodes.Ldloc_2);
				if (index == 3)
					return Instruction.Create(OpCodes.Ldloc_3);
				if (index <= byte.MaxValue)
					return new Instruction(OpCodes.Ldloc_S, locals[index]);
				return new Instruction(OpCodes.Ldloc, locals[index]);
			}
			else {
				if (index == 0)
					return Instruction.Create(OpCodes.Ldarg_0);
				if (index == 1)
					return Instruction.Create(OpCodes.Ldarg_1);
				if (index == 2)
					return Instruction.Create(OpCodes.Ldarg_2);
				if (index == 3)
					return Instruction.Create(OpCodes.Ldarg_3);
				if (index <= byte.MaxValue)
					return new Instruction(OpCodes.Ldarg_S, method.Parameters[index]);
				return new Instruction(OpCodes.Ldarg, method.Parameters[index]);
			}
		}

		Instruction LoadIndirect(TypeSig? type) {
			Debug2.Assert(!(generatedModule is null));
			switch (type.GetElementType()) {
			case ElementType.Boolean:		return Instruction.Create(OpCodes.Ldind_I1);
			case ElementType.Char:			return Instruction.Create(OpCodes.Ldind_U2);
			case ElementType.I1:			return Instruction.Create(OpCodes.Ldind_I1);
			case ElementType.U1:			return Instruction.Create(OpCodes.Ldind_U1);
			case ElementType.I2:			return Instruction.Create(OpCodes.Ldind_I2);
			case ElementType.U2:			return Instruction.Create(OpCodes.Ldind_U2);
			case ElementType.I4:			return Instruction.Create(OpCodes.Ldind_I4);
			case ElementType.U4:			return Instruction.Create(OpCodes.Ldind_U4);
			case ElementType.I8:			return Instruction.Create(OpCodes.Ldind_I8);
			case ElementType.U8:			return Instruction.Create(OpCodes.Ldind_I8);
			case ElementType.R4:			return Instruction.Create(OpCodes.Ldind_R4);
			case ElementType.R8:			return Instruction.Create(OpCodes.Ldind_R8);
			case ElementType.String:		return Instruction.Create(OpCodes.Ldind_Ref);
			case ElementType.I:				return Instruction.Create(OpCodes.Ldind_I);
			case ElementType.U:				return Instruction.Create(OpCodes.Ldind_I);
			case ElementType.ValueType:		return Instruction.Create(OpCodes.Ldobj, generatedModule.Import(type).ToTypeDefOrRef());
			case ElementType.Class:			return Instruction.Create(OpCodes.Ldind_Ref);
			case ElementType.Array:			return Instruction.Create(OpCodes.Ldind_Ref);
			case ElementType.Object:		return Instruction.Create(OpCodes.Ldind_Ref);
			case ElementType.SZArray:		return Instruction.Create(OpCodes.Ldind_Ref);
			case ElementType.Ptr:			return Instruction.Create(OpCodes.Ldind_I);
			case ElementType.FnPtr:			return Instruction.Create(OpCodes.Ldind_I);
			case ElementType.Var:			return Instruction.Create(OpCodes.Ldobj, generatedModule.Import(type).ToTypeDefOrRef());
			case ElementType.MVar:			return Instruction.Create(OpCodes.Ldobj, generatedModule.Import(type).ToTypeDefOrRef());
			case ElementType.TypedByRef:	return Instruction.Create(OpCodes.Ldobj, generatedModule.Import(type).ToTypeDefOrRef());

			case ElementType.GenericInst:
				var gis = (GenericInstSig)type!;
				if (gis.GenericType.IsValueTypeSig)
					return Instruction.Create(OpCodes.Ldobj, generatedModule.Import(type).ToTypeDefOrRef());
				return Instruction.Create(OpCodes.Ldind_Ref);

			case ElementType.End:
			case ElementType.Void:
			case ElementType.ByRef:
			case ElementType.ValueArray:
			case ElementType.R:
			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.Internal:
			case ElementType.Module:
			case ElementType.Sentinel:
			case ElementType.Pinned:
			default:
				return Instruction.Create(OpCodes.Nop);
			}
		}
	}
}
