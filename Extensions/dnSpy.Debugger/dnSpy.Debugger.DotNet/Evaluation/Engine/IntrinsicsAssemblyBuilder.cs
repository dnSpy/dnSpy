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
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	readonly struct IntrinsicsAssemblyBuilder {
		readonly ModuleDef module;
		readonly TypeDef intrinsicsType;
		readonly ICorLibTypes corlibTypes;
		readonly TypeSig exceptionTypeSig;
		readonly TypeSig typeTypeSig;
		readonly TypeSig guidTypeSig;

		public IntrinsicsAssemblyBuilder(string corlibAssemblyFullName, string imageRuntimeVersion) {
			var corlibRef = new AssemblyRefUser(new AssemblyNameInfo(corlibAssemblyFullName));
			module = new ModuleDefUser(Guid.NewGuid().ToString(), Guid.NewGuid(), corlibRef);
			module.RuntimeVersion = imageRuntimeVersion;
			module.Kind = ModuleKind.Dll;
			var asm = new AssemblyDefUser(Guid.NewGuid().ToString());
			asm.Modules.Add(module);
			intrinsicsType = new TypeDefUser(ExpressionCompilerConstants.IntrinsicAssemblyNamespace, ExpressionCompilerConstants.IntrinsicAssemblyTypeName, module.CorLibTypes.Object.TypeDefOrRef);
			intrinsicsType.Attributes = TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.Abstract | TypeAttributes.Sealed;
			module.Types.Add(intrinsicsType);
			corlibTypes = module.CorLibTypes;
			exceptionTypeSig = new ClassSig(corlibTypes.GetTypeRef(nameof(System), nameof(Exception)));
			typeTypeSig = new ClassSig(corlibTypes.GetTypeRef(nameof(System), nameof(Type)));
			guidTypeSig = new ValueTypeSig(corlibTypes.GetTypeRef(nameof(System), nameof(Guid)));
		}

		public (byte[] assemblyBytes, string assemblySimpleName) Create() {
			intrinsicsType.Methods.Add(CreateGetObjectAtAddress());
			intrinsicsType.Methods.Add(CreateGetException());
			intrinsicsType.Methods.Add(CreateGetStowedException());
			intrinsicsType.Methods.Add(CreateGetReturnValue());
			intrinsicsType.Methods.Add(CreateCreateVariable());
			intrinsicsType.Methods.Add(CreateGetObjectByAlias());
			intrinsicsType.Methods.Add(CreateGetVariableAddress());

			var memStream = new MemoryStream();
			var writerOptions = new ModuleWriterOptions(module);
			module.Write(memStream, writerOptions);
			return (memStream.ToArray(), module.Assembly.Name);
		}

		const MethodImplAttributes methodImplAttributes = MethodImplAttributes.IL | MethodImplAttributes.Managed;
		const MethodAttributes methodAttributes = MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.ReuseSlot;

		CilBody CreateBody() {
			var body = new CilBody();
			body.Instructions.Add(Instruction.Create(OpCodes.Ldnull));
			body.Instructions.Add(Instruction.Create(OpCodes.Throw));
			return body;
		}

		MethodDef CreateGetObjectAtAddress() {
			var sig = MethodSig.CreateStatic(corlibTypes.Object, corlibTypes.UInt64);
			var method = new MethodDefUser(ExpressionCompilerConstants.GetObjectAtAddressMethodName, sig, methodImplAttributes, methodAttributes);
			method.ParamDefs.Add(new ParamDefUser("address", 1));
			method.Body = CreateBody();
			return method;
		}

		MethodDef CreateGetException() {
			var sig = MethodSig.CreateStatic(exceptionTypeSig);
			var method = new MethodDefUser(ExpressionCompilerConstants.GetExceptionMethodName, sig, methodImplAttributes, methodAttributes);
			method.Body = CreateBody();
			return method;
		}

		MethodDef CreateGetStowedException() {
			var sig = MethodSig.CreateStatic(exceptionTypeSig);
			var method = new MethodDefUser(ExpressionCompilerConstants.GetStowedExceptionMethodName, sig, methodImplAttributes, methodAttributes);
			method.Body = CreateBody();
			return method;
		}

		MethodDef CreateGetReturnValue() {
			var sig = MethodSig.CreateStatic(corlibTypes.Object, corlibTypes.Int32);
			var method = new MethodDefUser(ExpressionCompilerConstants.GetReturnValueMethodName, sig, methodImplAttributes, methodAttributes);
			method.ParamDefs.Add(new ParamDefUser("index", 1));
			method.Body = CreateBody();
			return method;
		}

		MethodDef CreateCreateVariable() {
			var sig = MethodSig.CreateStatic(corlibTypes.Void, typeTypeSig, corlibTypes.String, guidTypeSig, new SZArraySig(corlibTypes.Byte));
			var method = new MethodDefUser(ExpressionCompilerConstants.CreateVariableMethodName, sig, methodImplAttributes, methodAttributes);
			method.ParamDefs.Add(new ParamDefUser("type", 1));
			method.ParamDefs.Add(new ParamDefUser("name", 2));
			method.ParamDefs.Add(new ParamDefUser("customTypeInfoPayloadTypeId", 3));
			method.ParamDefs.Add(new ParamDefUser("customTypeInfoPayload", 4));
			method.Body = CreateBody();
			return method;
		}

		MethodDef CreateGetObjectByAlias() {
			var sig = MethodSig.CreateStatic(corlibTypes.Object, corlibTypes.String);
			var method = new MethodDefUser(ExpressionCompilerConstants.GetVariableValueMethodName, sig, methodImplAttributes, methodAttributes);
			method.ParamDefs.Add(new ParamDefUser("name", 1));
			method.Body = CreateBody();
			return method;
		}

		MethodDef CreateGetVariableAddress() {
			var method = new MethodDefUser(ExpressionCompilerConstants.GetVariableAddressMethodName, null, methodImplAttributes, methodAttributes);
			var sig = MethodSig.CreateStaticGeneric(1, new PtrSig(new GenericMVar(0, method)), corlibTypes.String);
			method.MethodSig = sig;
			method.GenericParameters.Add(new GenericParamUser(0, GenericParamAttributes.NonVariant, "T"));
			method.ParamDefs.Add(new ParamDefUser("name", 1));
			method.Body = CreateBody();
			return method;
		}
	}
}
