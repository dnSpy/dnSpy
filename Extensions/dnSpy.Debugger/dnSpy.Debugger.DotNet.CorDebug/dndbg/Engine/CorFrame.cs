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
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;

namespace dndbg.Engine {
	sealed class CorFrame : COMObject<ICorDebugFrame>, IEquatable<CorFrame?> {
		public CorFrame? Callee {
			get {
				int hr = obj.GetCallee(out var calleeFrame);
				return hr < 0 || calleeFrame is null ? null : new CorFrame(calleeFrame);
			}
		}

		public CorFrame? Caller {
			get {
				int hr = obj.GetCaller(out var callerFrame);
				return hr < 0 || callerFrame is null ? null : new CorFrame(callerFrame);
			}
		}

		public CorChain? Chain {
			get {
				int hr = obj.GetChain(out var chain);
				return hr < 0 || chain is null ? null : new CorChain(chain);
			}
		}

		public bool IsNeutered {
			get {
				int hr = obj.GetChain(out var chain);
				return hr == CordbgErrors.CORDBG_E_OBJECT_NEUTERED;
			}
		}

		public uint Token => token;
		readonly uint token;

		public ulong StackStart => rangeStart;
		readonly ulong rangeStart;

		public ulong StackEnd => rangeEnd;
		readonly ulong rangeEnd;

		public bool IsILFrame => obj is ICorDebugILFrame;
		public bool IsNativeFrame => obj is ICorDebugNativeFrame;
		public bool IsJITCompiledFrame => IsILFrame && IsNativeFrame;
		public bool IsInternalFrame => obj is ICorDebugInternalFrame;
		public bool IsRuntimeUnwindableFrame => obj is ICorDebugRuntimeUnwindableFrame;

		public ILFrameIP ILFrameIP {
			get {
				var ilf = obj as ICorDebugILFrame;
				if (ilf is null)
					return new ILFrameIP();
				int hr = ilf.GetIP(out uint offset, out var mappingResult);
				return hr < 0 ? new ILFrameIP() : new ILFrameIP(offset, mappingResult);
			}
		}

		public uint NativeFrameIP {
			get {
				var nf = obj as ICorDebugNativeFrame;
				if (nf is null)
					return 0;
				int hr = nf.GetIP(out uint offset);
				return hr < 0 ? 0 : offset;
			}
		}

		public CorDebugInternalFrameType InternalFrameType {
			get {
				var @if = obj as ICorDebugInternalFrame;
				if (@if is null)
					return CorDebugInternalFrameType.STUBFRAME_NONE;
				int hr = @if.GetFrameType(out var type);
				return hr < 0 ? CorDebugInternalFrameType.STUBFRAME_NONE : type;
			}
		}

		public CorFunction? Function {
			get {
				int hr = obj.GetFunction(out var func);
				return hr < 0 || func is null ? null : new CorFunction(func);
			}
		}

		public CorCode? Code {
			get {
				int hr = obj.GetCode(out var code);
				return hr < 0 || code is null ? null : new CorCode(code);
			}
		}

		public IEnumerable<CorType> TypeParameters {
			get {
				var ilf2 = obj as ICorDebugILFrame2;
				if (ilf2 is null)
					yield break;
				int hr = ilf2.EnumerateTypeParameters(out var valueEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					hr = valueEnum.Next(1, out var value, out uint count);
					if (hr != 0 || value is null)
						break;
					yield return new CorType(value);
				}
			}
		}

		public CorFrame(ICorDebugFrame frame)
			: base(frame) {
			int hr = frame.GetFunctionToken(out token);
			if (hr < 0)
				token = 0;

			hr = frame.GetStackRange(out rangeStart, out rangeEnd);
			if (hr < 0)
				rangeStart = rangeEnd = 0;
		}

		public CorStepper? CreateStepper() {
			int hr = obj.CreateStepper(out var stepper);
			return hr < 0 || stepper is null ? null : new CorStepper(stepper);
		}

		public bool SetILFrameIP(uint ilOffset) {
			var ilf = obj as ICorDebugILFrame;
			if (ilf is null)
				return false;
			int hr = ilf.SetIP(ilOffset);
			return hr >= 0;
		}

		public bool CanSetILFrameIP(uint ilOffset) {
			var ilf = obj as ICorDebugILFrame;
			if (ilf is null)
				return false;
			return ilf.CanSetIP(ilOffset) == 0;
		}

		public bool SetNativeFrameIP(uint offset) {
			var nf = obj as ICorDebugNativeFrame;
			if (nf is null)
				return false;
			int hr = nf.SetIP(offset);
			return hr >= 0;
		}

		public bool CanSetNativeFrameIP(uint offset) {
			var nf = obj as ICorDebugNativeFrame;
			if (nf is null)
				return false;
			return nf.CanSetIP(offset) == 0;
		}

		public CorValue? GetILLocal(uint index, out int hr) {
			var ilf = obj as ICorDebugILFrame;
			if (ilf is null) {
				hr = -1;
				return null;
			}
			hr = ilf.GetLocalVariable(index, out var value);
			return hr < 0 || value is null ? null : new CorValue(value);
		}

		public CorValue? GetILArgument(uint index, out int hr) {
			var ilf = obj as ICorDebugILFrame;
			if (ilf is null) {
				hr = -1;
				return null;
			}
			hr = ilf.GetArgument(index, out var value);
			return hr < 0 || value is null ? null : new CorValue(value);
		}

		public CorValue? GetILLocal(ILCodeKind kind, uint index) {
			var ilf4 = obj as ICorDebugILFrame4;
			if (ilf4 is null)
				return null;
			int hr = ilf4.GetLocalVariableEx(kind, index, out var value);
			return hr < 0 || value is null ? null : new CorValue(value);
		}

		public CorValue? GetILLocal(ILCodeKind kind, int index) => GetILLocal(kind, (uint)index);

		public CorCode? GetCode(ILCodeKind kind) {
			var ilf4 = obj as ICorDebugILFrame4;
			if (ilf4 is null)
				return null;
			int hr = ilf4.GetCodeEx(kind, out var code);
			return hr < 0 || code is null ? null : new CorCode(code);
		}

		public bool GetTypeAndMethodGenericParameters(out CorType[] typeGenArgs, out CorType[] methGenArgs) {
			var func = Function;
			var module = func?.Module;
			if (module is null) {
				typeGenArgs = Array.Empty<CorType>();
				methGenArgs = Array.Empty<CorType>();
				return false;
			}
			Debug2.Assert(func is not null);

			var mdi = module.GetMetaDataInterface<IMetaDataImport>();
			var gas = new List<CorType>(TypeParameters);
			var cls = func.Class;
			int typeGenArgsCount = cls is null ? 0 : GetCountGenericParameters(mdi, cls.Token);
			int methGenArgsCount = GetCountGenericParameters(mdi, func.Token);
			Debug.Assert(typeGenArgsCount + methGenArgsCount == gas.Count);
			typeGenArgs = new CorType[typeGenArgsCount];
			methGenArgs = new CorType[methGenArgsCount];
			int j = 0;
			for (int i = 0; j < gas.Count && i < typeGenArgs.Length; i++, j++)
				typeGenArgs[i] = gas[j];
			for (int i = 0; j < gas.Count && i < methGenArgs.Length; i++, j++)
				methGenArgs[i] = gas[j];

			return true;
		}

		static int GetCountGenericParameters(IMetaDataImport? mdi, uint token) => MDAPI.GetGenericParamTokens(mdi as IMetaDataImport2, token).Length;

		public CorValue? GetReturnValueForILOffset(uint offset) {
			var ilf3 = obj as ICorDebugILFrame3;
			if (ilf3 is null)
				return null;
			int hr = ilf3.GetReturnValueForILOffset(offset, out var value);
			return hr < 0 || value is null ? null : new CorValue(value);
		}

		public bool Equals(CorFrame? other) => other is not null && RawObject == other.RawObject;
		public override bool Equals(object? obj) => Equals(obj as CorFrame);
		public override int GetHashCode() => RawObject.GetHashCode();
	}
}
