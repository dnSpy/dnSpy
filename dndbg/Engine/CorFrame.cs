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
using System.Diagnostics;
using System.Linq;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;
using dnlib.DotNet;

namespace dndbg.Engine {
	public sealed class CorFrame : COMObject<ICorDebugFrame>, IEquatable<CorFrame> {
		/// <summary>
		/// Gets the frame that this frame called or null
		/// </summary>
		public CorFrame Callee {
			get {
				ICorDebugFrame calleeFrame;
				int hr = obj.GetCallee(out calleeFrame);
				return hr < 0 || calleeFrame == null ? null : new CorFrame(calleeFrame);
			}
		}

		/// <summary>
		/// Gets the frame that called this frame or null
		/// </summary>
		public CorFrame Caller {
			get {
				ICorDebugFrame callerFrame;
				int hr = obj.GetCaller(out callerFrame);
				return hr < 0 || callerFrame == null ? null : new CorFrame(callerFrame);
			}
		}

		/// <summary>
		/// Gets its chain
		/// </summary>
		public CorChain Chain {
			get {
				ICorDebugChain chain;
				int hr = obj.GetChain(out chain);
				return hr < 0 || chain == null ? null : new CorChain(chain);
			}
		}

		/// <summary>
		/// true if it has been neutered
		/// </summary>
		public bool IsNeutered {
			get {
				ICorDebugChain chain;
				int hr = obj.GetChain(out chain);
				return hr == CordbgErrors.CORDBG_E_OBJECT_NEUTERED;
			}
		}

		/// <summary>
		/// Gets the token of the method or 0
		/// </summary>
		public uint Token {
			get { return token; }
		}
		readonly uint token;

		/// <summary>
		/// Start address of the stack segment
		/// </summary>
		public ulong StackStart {
			get { return rangeStart; }
		}
		readonly ulong rangeStart;

		/// <summary>
		/// End address of the stack segment
		/// </summary>
		public ulong StackEnd {
			get { return rangeEnd; }
		}
		readonly ulong rangeEnd;

		/// <summary>
		/// true if this is an IL frame (<see cref="ICorDebugILFrame"/>)
		/// </summary>
		public bool IsILFrame {
			get { return obj is ICorDebugILFrame; }
		}

		/// <summary>
		/// true if this is a Native frame (<see cref="ICorDebugNativeFrame"/>). This can be true
		/// even if <see cref="IsILFrame"/> is true (it's a JIT-compiled frame).
		/// </summary>
		public bool IsNativeFrame {
			get { return obj is ICorDebugNativeFrame; }
		}

		/// <summary>
		/// true if it's a JIT-compiled frame (<see cref="IsILFrame"/> and <see cref="IsNativeFrame"/>
		/// are both true).
		/// </summary>
		public bool IsJITCompiledFrame {
			get { return IsILFrame && IsNativeFrame; }
		}

		/// <summary>
		/// true if this is an internal frame (<see cref="ICorDebugInternalFrame"/>)
		/// </summary>
		public bool IsInternalFrame {
			get { return obj is ICorDebugInternalFrame; }
		}

		/// <summary>
		/// true if this is a runtime unwindable frame (<see cref="ICorDebugRuntimeUnwindableFrame"/>)
		/// </summary>
		public bool IsRuntimeUnwindableFrame {
			get { return obj is ICorDebugRuntimeUnwindableFrame; }
		}

		/// <summary>
		/// Gets the IL frame IP. Only valid if <see cref="IsILFrame"/> is true
		/// </summary>
		public ILFrameIP ILFrameIP {
			get {
				var ilf = obj as ICorDebugILFrame;
				if (ilf == null)
					return new ILFrameIP();
				uint offset;
				CorDebugMappingResult mappingResult;
				int hr = ilf.GetIP(out offset, out mappingResult);
				return hr < 0 ? new ILFrameIP() : new ILFrameIP(offset, mappingResult);
			}
		}

		/// <summary>
		/// Gets the native frame IP. Only valid if <see cref="IsNativeFrame"/> is true
		/// </summary>
		public uint NativeFrameIP {
			get {
				var nf = obj as ICorDebugNativeFrame;
				if (nf == null)
					return 0;
				uint offset;
				int hr = nf.GetIP(out offset);
				return hr < 0 ? 0 : offset;
			}
		}

		/// <summary>
		/// Gets the internal frame type or <see cref="CorDebugInternalFrameType.STUBFRAME_NONE"/>
		/// if it's not an internal frame (<see cref="ICorDebugInternalFrame"/>)
		/// </summary>
		public CorDebugInternalFrameType InternalFrameType {
			get {
				var @if = obj as ICorDebugInternalFrame;
				if (@if == null)
					return CorDebugInternalFrameType.STUBFRAME_NONE;
				CorDebugInternalFrameType type;
				int hr = @if.GetFrameType(out type);
				return hr < 0 ? CorDebugInternalFrameType.STUBFRAME_NONE : type;
			}
		}

		/// <summary>
		/// Gets the function or null
		/// </summary>
		public CorFunction Function {
			get {
				ICorDebugFunction func;
				int hr = obj.GetFunction(out func);
				return hr < 0 || func == null ? null : new CorFunction(func);
			}
		}

		/// <summary>
		/// Gets the code or null
		/// </summary>
		public CorCode Code {
			get {
				ICorDebugCode code;
				int hr = obj.GetCode(out code);
				return hr < 0 || code == null ? null : new CorCode(code);
			}
		}

		/// <summary>
		/// Gets all arguments
		/// </summary>
		public IEnumerable<CorValue> ILArguments {
			get {
				var ilf = obj as ICorDebugILFrame;
				if (ilf == null)
					yield break;
				ICorDebugValueEnum valueEnum;
				int hr = ilf.EnumerateArguments(out valueEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					ICorDebugValue value = null;
					uint count;
					hr = valueEnum.Next(1, out value, out count);
					if (hr != 0 || value == null)
						break;
					yield return new CorValue(value);
				}
			}
		}

		/// <summary>
		/// Gets all locals
		/// </summary>
		public IEnumerable<CorValue> ILLocals {
			get {
				var ilf = obj as ICorDebugILFrame;
				if (ilf == null)
					yield break;
				ICorDebugValueEnum valueEnum;
				int hr = ilf.EnumerateLocalVariables(out valueEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					ICorDebugValue value = null;
					uint count;
					hr = valueEnum.Next(1, out value, out count);
					if (hr != 0 || value == null)
						break;
					yield return new CorValue(value);
				}
			}
		}

		/// <summary>
		/// Gets all type and/or method generic parameters. The first returned values are the generic
		/// type params, followed by the generic method params. See also <see cref="GetTypeAndMethodGenericParameters(out List{CorType}, out List{CorType})"/>
		/// </summary>
		public IEnumerable<CorType> TypeParameters {
			get {
				var ilf2 = obj as ICorDebugILFrame2;
				if (ilf2 == null)
					yield break;
				ICorDebugTypeEnum valueEnum;
				int hr = ilf2.EnumerateTypeParameters(out valueEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					ICorDebugType value = null;
					uint count;
					hr = valueEnum.Next(1, out value, out count);
					if (hr != 0 || value == null)
						break;
					yield return new CorType(value);
				}
			}
		}

		/// <summary>
		/// Gets the module of the function or null
		/// </summary>
		public SerializedDnModule? SerializedDnModule {
			get {
				var func = Function;
				if (func == null)
					return null;

				var module = func.Module;
				if (module == null)
					return null;

				return module.SerializedDnModule;
			}
		}

		public CorFrame(ICorDebugFrame frame)
			: base(frame) {
			int hr = frame.GetFunctionToken(out this.token);
			if (hr < 0)
				this.token = 0;

			hr = frame.GetStackRange(out this.rangeStart, out this.rangeEnd);
			if (hr < 0)
				this.rangeStart = this.rangeEnd = 0;

			//TODO: ICorDebugILFrame2, ICorDebugILFrame3
			//TODO: ICorDebugInternalFrame, ICorDebugInternalFrame2
			//TODO: ICorDebugNativeFrame, ICorDebugNativeFrame2
			//TODO: ICorDebugRuntimeUnwindableFrame
		}

		public CorStepper CreateStepper() {
			ICorDebugStepper stepper;
			int hr = obj.CreateStepper(out stepper);
			return hr < 0 || stepper == null ? null : new CorStepper(stepper);
		}

		/// <summary>
		/// Sets a new IL offset. All frames and chains for the current thread will be invalidated
		/// after this call. This method can only be called if <see cref="IsILFrame"/> is true.
		/// </summary>
		/// <param name="ilOffset">New IL offset</param>
		/// <returns></returns>
		public bool SetILFrameIP(uint ilOffset) {
			var ilf = obj as ICorDebugILFrame;
			if (ilf == null)
				return false;
			int hr = ilf.SetIP(ilOffset);
			return hr >= 0;
		}

		/// <summary>
		/// Returns true if it's safe to call <see cref="SetILFrameIP(uint)"/> but it can still be
		/// called if this method fails. This method can only be called if <see cref="IsILFrame"/>
		/// is true.
		/// </summary>
		/// <param name="ilOffset">IL offset</param>
		/// <returns></returns>
		public bool CanSetILFrameIP(uint ilOffset) {
			var ilf = obj as ICorDebugILFrame;
			if (ilf == null)
				return false;
			return ilf.CanSetIP(ilOffset) == 0;
		}

		/// <summary>
		/// Sets a new native offset. All frames and chains for the current thread will be invalidated
		/// after this call. This method can only be called if <see cref="IsNativeFrame"/> is true.
		/// </summary>
		/// <param name="offset">New offset</param>
		/// <returns></returns>
		public bool SetNativeFrameIP(uint offset) {
			var nf = obj as ICorDebugNativeFrame;
			if (nf == null)
				return false;
			int hr = nf.SetIP(offset);
			return hr >= 0;
		}

		/// <summary>
		/// Returns true if it's safe to call <see cref="SetNativeFrameIP(uint)"/> but it can still be
		/// called if this method fails. This method can only be called if <see cref="IsNativeFrame"/>
		/// is true.
		/// </summary>
		/// <param name="offset">Offset</param>
		/// <returns></returns>
		public bool CanSetNativeFrameIP(uint offset) {
			var nf = obj as ICorDebugNativeFrame;
			if (nf == null)
				return false;
			return nf.CanSetIP(offset) == 0;
		}

		/// <summary>
		/// Gets a local variable or null if it's not an <see cref="ICorDebugILFrame"/> or if there
		/// was an error
		/// </summary>
		/// <param name="index">Index of local</param>
		/// <returns></returns>
		public CorValue GetILLocal(uint index) {
			var ilf = obj as ICorDebugILFrame;
			if (ilf == null)
				return null;
			ICorDebugValue value;
			int hr = ilf.GetLocalVariable(index, out value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		/// <summary>
		/// Gets a local variable or null if it's not an <see cref="ICorDebugILFrame"/> or if there
		/// was an error
		/// </summary>
		/// <param name="index">Index of local</param>
		/// <returns></returns>
		public CorValue GetILLocal(int index) {
			return GetILLocal((uint)index);
		}

		/// <summary>
		/// Gets an argument or null if it's not an <see cref="ICorDebugILFrame"/> or if there
		/// was an error
		/// </summary>
		/// <param name="index">Index of argument</param>
		/// <returns></returns>
		public CorValue GetILArgument(uint index) {
			var ilf = obj as ICorDebugILFrame;
			if (ilf == null)
				return null;
			ICorDebugValue value;
			int hr = ilf.GetArgument(index, out value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		/// <summary>
		/// Gets an argument or null if it's not an <see cref="ICorDebugILFrame"/> or if there
		/// was an error
		/// </summary>
		/// <param name="index">Index of argument</param>
		/// <returns></returns>
		public CorValue GetILArgument(int index) {
			return GetILArgument((uint)index);
		}

		/// <summary>
		/// Gets all locals
		/// </summary>
		/// <param name="kind">Kind</param>
		public IEnumerable<CorValue> GetILLocals(ILCodeKind kind) {
			var ilf4 = obj as ICorDebugILFrame4;
			if (ilf4 == null)
				yield break;
			ICorDebugValueEnum valueEnum;
			int hr = ilf4.EnumerateLocalVariablesEx(kind, out valueEnum);
			if (hr < 0)
				yield break;
			for (;;) {
				ICorDebugValue value = null;
				uint count;
				hr = valueEnum.Next(1, out value, out count);
				if (hr != 0 || value == null)
					break;
				yield return new CorValue(value);
			}
		}

		/// <summary>
		/// Gets a local variable or null if it's not an <see cref="ICorDebugILFrame4"/> or if there
		/// was an error
		/// </summary>
		/// <param name="kind">Kind</param>
		/// <param name="index">Index of local</param>
		/// <returns></returns>
		public CorValue GetILLocal(ILCodeKind kind, uint index) {
			var ilf4 = obj as ICorDebugILFrame4;
			if (ilf4 == null)
				return null;
			ICorDebugValue value;
			int hr = ilf4.GetLocalVariableEx(kind, index, out value);
			return hr < 0 || value == null ? null : new CorValue(value);
		}

		/// <summary>
		/// Gets a local variable or null if it's not an <see cref="ICorDebugILFrame4"/> or if there
		/// was an error
		/// </summary>
		/// <param name="kind">Kind</param>
		/// <param name="index">Index of local</param>
		/// <returns></returns>
		public CorValue GetILLocal(ILCodeKind kind, int index) {
			return GetILLocal(kind, (uint)index);
		}

		/// <summary>
		/// Gets the code or null if it's not an <see cref="ICorDebugILFrame4"/> or if there was an
		/// error
		/// </summary>
		/// <param name="kind">Kind</param>
		/// <returns></returns>
		public CorCode GetCode(ILCodeKind kind) {
			var ilf4 = obj as ICorDebugILFrame4;
			if (ilf4 == null)
				return null;
			ICorDebugCode code;
			int hr = ilf4.GetCodeEx(kind, out code);
			return hr < 0 || code == null ? null : new CorCode(code);
		}

		/// <summary>
		/// Splits up <see cref="TypeParameters"/> into type and method generic arguments
		/// </summary>
		/// <param name="typeGenArgs">Gets updated with a list containing all generic type arguments</param>
		/// <param name="methGenArgs">Gets updated with a list containing all generic method arguments</param>
		/// <returns></returns>
		public bool GetTypeAndMethodGenericParameters(out List<CorType> typeGenArgs, out List<CorType> methGenArgs) {
			typeGenArgs = new List<CorType>();
			methGenArgs = new List<CorType>();

			var func = Function;
			if (func == null)
				return false;
			var module = func.Module;
			if (module == null)
				return false;

			var mdi = module.GetMetaDataInterface<IMetaDataImport>();
			var gas = new List<CorType>(TypeParameters);
			var cls = func.Class;
			int typeGenArgsCount = cls == null ? 0 : MetaDataUtils.GetCountGenericParameters(mdi, cls.Token);
			int methGenArgsCount = MetaDataUtils.GetCountGenericParameters(mdi, func.Token);
			Debug.Assert(typeGenArgsCount + methGenArgsCount == gas.Count);
			int j = 0;
			for (int i = 0; j < gas.Count && i < typeGenArgsCount; i++, j++)
				typeGenArgs.Add(gas[j]);
			for (int i = 0; j < gas.Count && i < methGenArgsCount; i++, j++)
				methGenArgs.Add(gas[j]);

			return true;
		}

		/// <summary>
		/// Gets all argument and local types
		/// </summary>
		/// <param name="argTypes">Gets updated with all argument types. If there's a hidden this
		/// parameter, it's the first type. This type can be null. If it's not null, ignore any
		/// <see cref="ClassSig"/> since it might still be a value type</param>
		/// <param name="localTypes">Gets updated with all local types</param>
		/// <returns></returns>
		public bool GetArgAndLocalTypes(out List<TypeSig> argTypes, out List<TypeSig> localTypes) {
			argTypes = new List<TypeSig>();
			localTypes = new List<TypeSig>();

			var func = Function;
			if (func == null)
				return false;
			var module = func.Module;
			if (module == null)
				return false;

			var mdi = module.GetMetaDataInterface<IMetaDataImport>();

			var methodSig = MetaDataUtils.GetMethodSignature(mdi, func.Token);
			if (methodSig != null) {
				if (methodSig.HasThis)
					argTypes.Add(GetThisType(func));
				argTypes.AddRange(methodSig.Params);
				if (methodSig.ParamsAfterSentinel != null)
					argTypes.AddRange(methodSig.ParamsAfterSentinel);
			}

			uint localVarSigTok = func.LocalVarSigToken;
			if ((localVarSigTok & 0x00FFFFFF) != 0) {
				var localSig = MetaDataUtils.ReadStandAloneSig(mdi, localVarSigTok) as LocalSig;
				if (localSig != null)
					localTypes.AddRange(localSig.Locals);
			}

			return true;
		}

		TypeSig GetThisType(CorFunction func) {
			if (func == null)
				return null;
			var funcClass = func.Class;
			var mod = funcClass == null ? null : funcClass.Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			if (mdi == null)
				return null;

			int numTypeGenArgs = MetaDataUtils.GetCountGenericParameters(mdi, funcClass.Token);
			var genTypeArgs = this.TypeParameters.Take(numTypeGenArgs).ToArray();

			var td = DebugSignatureReader.CreateTypeDef(mdi, funcClass.Token);
			// Assume it's a class for now. The code should ignore ClassSig and just use the TypeDef
			var sig = new ClassSig(td);
			if (genTypeArgs.Length == 0)
				return sig;

			var genArgs = new List<TypeSig>(genTypeArgs.Length);
			for (int i = 0; i < genTypeArgs.Length; i++)
				genArgs.Add(new GenericVar(i));
			return new GenericInstSig(sig, genArgs);
		}

		public static bool operator ==(CorFrame a, CorFrame b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorFrame a, CorFrame b) {
			return !(a == b);
		}

		public bool Equals(CorFrame other) {
			return !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) {
			return Equals(obj as CorFrame);
		}

		public override int GetHashCode() {
			return RawObject.GetHashCode();
		}

		public T Write<T>(T output, TypePrinterFlags flags, Func<DnEval> getEval = null) where T : ITypeOutput {
			new TypePrinter(output, flags, getEval).Write(this);
			return output;
		}

		public string ToString(TypePrinterFlags flags) {
			return Write(new StringBuilderTypeOutput(), flags).ToString();
		}

		public override string ToString() {
			return ToString(TypePrinterFlags.Default);
		}
	}
}
