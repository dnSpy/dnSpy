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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;
using dnlib.DotNet;

namespace dndbg.Engine {
	sealed class CorFunction : COMObject<ICorDebugFunction>, IEquatable<CorFunction> {
		/// <summary>
		/// Gets the module or null
		/// </summary>
		public CorModule Module {
			get {
				if (module != null)
					return module;
				int hr = obj.GetModule(out var mod);
				return module = hr < 0 || mod == null ? null : new CorModule(mod);
			}
		}
		CorModule module;

		/// <summary>
		/// Gets the class or null
		/// </summary>
		public CorClass Class {
			get {
				int hr = obj.GetClass(out var cls);
				if (hr >= 0 && cls != null)
					return new CorClass(cls);

				// Here if it's an extern method, eg. it's not IL code, but native code

				var mod = Module;
				Debug.Assert(mod != null);
				var mdi = mod?.GetMetaDataInterface<IMetaDataImport>();
				uint tdOwner = 0x02000000 + MDAPI.GetMethodOwnerRid(mdi, Token);
				return mod?.GetClassFromToken(tdOwner);
			}
		}

		/// <summary>
		/// Gets the token or 0
		/// </summary>
		public uint Token {
			get {
				int hr = obj.GetToken(out uint token);
				return hr < 0 ? 0 : token;
			}
		}

		/// <summary>
		/// Gets/sets JMC (just my code) flag
		/// </summary>
		public bool JustMyCode {
			get {
				var func2 = obj as ICorDebugFunction2;
				if (func2 == null)
					return false;
				int hr = func2.GetJMCStatus(out int status);
				return hr >= 0 && status != 0;
			}
			set {
				var func2 = obj as ICorDebugFunction2;
				if (func2 == null)
					return;
				int hr = func2.SetJMCStatus(value ? 1 : 0);
			}
		}

		/// <summary>
		/// Gets EnC (edit and continue) version number of the latest edit, and might be greater
		/// than this function's version number. See <see cref="VersionNumber"/>.
		/// </summary>
		public uint CurrentVersionNumber {
			get {
				int hr = obj.GetCurrentVersionNumber(out uint ver);
				return hr < 0 ? 0 : ver;
			}
		}

		/// <summary>
		/// Gets the EnC (edit and continue) version number of this function
		/// </summary>
		public uint VersionNumber {
			get {
				var func2 = obj as ICorDebugFunction2;
				if (func2 == null)
					return CurrentVersionNumber;
				int hr = func2.GetVersionNumber(out uint ver);
				return hr < 0 ? 0 : ver;
			}
		}

		/// <summary>
		/// Gets the local variables signature token or 0 if none
		/// </summary>
		public uint LocalVarSigToken {
			get {
				int hr = obj.GetLocalVarSigToken(out uint token);
				return hr < 0 ? 0 : token;
			}
		}

		/// <summary>
		/// Gets the IL code or null
		/// </summary>
		public CorCode ILCode {
			get {
				int hr = obj.GetILCode(out var code);
				return hr < 0 || code == null ? null : new CorCode(code);
			}
		}

		/// <summary>
		/// Gets the native code or null. If it's a generic method that's been JITed more than once,
		/// the returned code could be any one of the JITed codes.
		/// </summary>
		/// <remarks><c>EnumerateNativeCode()</c> should be called but that method hasn't been
		/// implemented by the CLR debugger yet.</remarks>
		public CorCode NativeCode {
			get {
				int hr = obj.GetNativeCode(out var code);
				return hr < 0 || code == null ? null : new CorCode(code);
			}
		}

		public CorFunction(ICorDebugFunction func, CorModule module = null)
			: base(func) {
			//TODO: ICorDebugFunction2::EnumerateNativeCode
			//TODO: ICorDebugFunction3::GetActiveReJitRequestILCode
		}

		public void GetAttributes(out MethodImplAttributes implAttributes, out MethodAttributes attributes) =>
			MDAPI.GetMethodAttributes(Module?.GetMetaDataInterface<IMetaDataImport>(), Token, out attributes, out implAttributes);

		public MethodAttributes GetAttributes() {
			MDAPI.GetMethodAttributes(Module?.GetMetaDataInterface<IMetaDataImport>(), Token, out var attributes, out var implAttributes);
			return attributes;
		}

		/// <summary>
		/// Creates a function breakpoint at the beginning of the function
		/// </summary>
		/// <returns></returns>
		public CorFunctionBreakpoint CreateBreakpoint() {
			int hr = obj.CreateBreakpoint(out var fnbp);
			return hr < 0 || fnbp == null ? null : new CorFunctionBreakpoint(fnbp);
		}

		/// <summary>
		/// Gets method parameters and method flags
		/// </summary>
		/// <param name="methodAttrs">Method attributes</param>
		/// <param name="methodImplAttrs">Method implementation attributes</param>
		/// <returns></returns>
		public MDParameters GetMDParameters(out MethodAttributes methodAttrs, out MethodImplAttributes methodImplAttrs) {
			methodAttrs = 0;
			methodImplAttrs = 0;
			var mod = Module;
			if (mod == null)
				return null;
			var mdi = mod.GetMetaDataInterface<IMetaDataImport>();
			if (mdi == null)
				return null;
			if (!MDAPI.GetMethodAttributes(mdi, Token, out methodAttrs, out methodImplAttrs))
				return null;
			var name = MDAPI.GetMethodName(mdi, Token);
			if (name == null)
				return null;
			return MetaDataUtils.GetParameters(mdi, Token);
		}

		/// <summary>
		/// Gets method generic parameters
		/// </summary>
		/// <returns></returns>
		public List<TokenAndName> GetGenericParameters() =>
			MetaDataUtils.GetGenericParameterNames(Module?.GetMetaDataInterface<IMetaDataImport>(), Token);

		/// <summary>
		/// Gets type and method generic parameters
		/// </summary>
		/// <param name="typeParams">Updated with type params</param>
		/// <param name="methodParams">Updated with method params</param>
		public void GetGenericParameters(out List<TokenAndName> typeParams, out List<TokenAndName> methodParams) {
			methodParams = GetGenericParameters();
			typeParams = Class?.GetGenericParameters() ?? new List<TokenAndName>();
		}

		public CorOverride[] GetOverrides() {
			var mod = Module;
			var info = MDAPI.GetMethodOverrides(mod?.GetMetaDataInterface<IMetaDataImport>(), Token);
			if (info.Length == 0)
				return Array.Empty<CorOverride>();
			var res = new CorOverride[info.Length];
			for (int i = 0; i < res.Length; i++)
				res[i] = new CorOverride(mod, info[i]);
			return res;
		}

		public string GetName() => MDAPI.GetMethodName(Module?.GetMetaDataInterface<IMetaDataImport>(), Token);
		public MethodSig GetMethodSig() => MetaDataUtils.GetMethodSignature(Module?.GetMetaDataInterface<IMetaDataImport>(), Token);

		public static bool operator ==(CorFunction a, CorFunction b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorFunction a, CorFunction b) => !(a == b);
		public bool Equals(CorFunction other) => !ReferenceEquals(other, null) && RawObject == other.RawObject;
		public override bool Equals(object obj) => Equals(obj as CorFunction);
		public override int GetHashCode() => RawObject.GetHashCode();

		public T Write<T>(T output, TypePrinterFlags flags) where T : ITypeOutput {
			new TypePrinter(output, flags).Write(this);
			return output;
		}

		public string ToString(TypePrinterFlags flags) => Write(new StringBuilderTypeOutput(), flags).ToString();
		public override string ToString() => ToString(TypePrinterFlags.Default);
	}
}
