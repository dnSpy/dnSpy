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
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;
using dnlib.DotNet;

namespace dndbg.Engine {
	public sealed class CorFunction : COMObject<ICorDebugFunction>, IEquatable<CorFunction> {
		/// <summary>
		/// Gets the module or null
		/// </summary>
		public CorModule Module {
			get {
				if (module != null)
					return module;
				ICorDebugModule mod;
				int hr = obj.GetModule(out mod);
				return module = hr < 0 || mod == null ? null : new CorModule(mod);
			}
		}
		CorModule module;

		/// <summary>
		/// Gets the class or null
		/// </summary>
		public CorClass Class {
			get {
				ICorDebugClass cls;
				int hr = obj.GetClass(out cls);
				if (hr >= 0 && cls != null)
					return new CorClass(cls);

				// Here if it's an extern method, eg. it's not IL code, but native code

				var mod = Module;
				Debug.Assert(mod != null);
				var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
				uint tdOwner = 0x02000000 + MDAPI.GetMethodOwnerRid(mdi, Token);
				return mod == null ? null : mod.GetClassFromToken(tdOwner);
			}
		}

		/// <summary>
		/// Gets the token or 0
		/// </summary>
		public uint Token {
			get {
				uint token;
				int hr = obj.GetToken(out token);
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
				int status;
				int hr = func2.GetJMCStatus(out status);
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
				uint ver;
				int hr = obj.GetCurrentVersionNumber(out ver);
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
				uint ver;
				int hr = func2.GetVersionNumber(out ver);
				return hr < 0 ? 0 : ver;
			}
		}

		/// <summary>
		/// Gets the local variables signature token or 0 if none
		/// </summary>
		public uint LocalVarSigToken {
			get {
				uint token;
				int hr = obj.GetLocalVarSigToken(out token);
				return hr < 0 ? 0 : token;
			}
		}

		/// <summary>
		/// Gets the IL code or null
		/// </summary>
		public CorCode ILCode {
			get {
				ICorDebugCode code;
				int hr = obj.GetILCode(out code);
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
				ICorDebugCode code;
				int hr = obj.GetNativeCode(out code);
				return hr < 0 || code == null ? null : new CorCode(code);
			}
		}

		public CorFunction(ICorDebugFunction func, CorModule module = null)
			: base(func) {
			//TODO: ICorDebugFunction2::EnumerateNativeCode
			//TODO: ICorDebugFunction3::GetActiveReJitRequestILCode
		}

		public void GetAttributes(out MethodImplAttributes implAttributes, out MethodAttributes attributes) {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			MDAPI.GetMethodAttributes(mdi, Token, out attributes, out implAttributes);
		}

		public MethodAttributes GetAttributes() {
			MethodImplAttributes implAttributes;
			MethodAttributes attributes;
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			MDAPI.GetMethodAttributes(mdi, Token, out attributes, out implAttributes);
			return attributes;
		}

		/// <summary>
		/// Creates a function breakpoint at the beginning of the function
		/// </summary>
		/// <returns></returns>
		public CorFunctionBreakpoint CreateBreakpoint() {
			ICorDebugFunctionBreakpoint fnbp;
			int hr = obj.CreateBreakpoint(out fnbp);
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
		public List<TokenAndName> GetGenericParameters() {
			var module = Module;
			return MetaDataUtils.GetGenericParameterNames(module == null ? null : module.GetMetaDataInterface<IMetaDataImport>(), Token);
		}

		/// <summary>
		/// Gets type and method generic parameters
		/// </summary>
		/// <param name="typeParams">Updated with type params</param>
		/// <param name="methodParams">Updated with method params</param>
		public void GetGenericParameters(out List<TokenAndName> typeParams, out List<TokenAndName> methodParams) {
			methodParams = GetGenericParameters();
			var cls = Class;
			typeParams = cls == null ? new List<TokenAndName>() : cls.GetGenericParameters();
		}

		public CorOverride[] GetOverrides() {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			var info = MDAPI.GetMethodOverrides(mdi, Token);
			if (info.Length == 0)
				return emptyCorOverrides;
			var res = new CorOverride[info.Length];
			for (int i = 0; i < res.Length; i++)
				res[i] = new CorOverride(mod, info[i]);
			return res;
		}
		static readonly CorOverride[] emptyCorOverrides = new CorOverride[0];

		public string GetName() {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			if (mdi == null)
				return null;
			return MDAPI.GetMethodName(mdi, Token);
		}

		public MethodSig GetMethodSig() {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			if (mdi == null)
				return null;
			return MetaDataUtils.GetMethodSignature(mdi, Token);
		}

		public static bool operator ==(CorFunction a, CorFunction b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorFunction a, CorFunction b) {
			return !(a == b);
		}

		public bool Equals(CorFunction other) {
			return !ReferenceEquals(other, null) &&
				RawObject == other.RawObject;
		}

		public override bool Equals(object obj) {
			return Equals(obj as CorFunction);
		}

		public override int GetHashCode() {
			return RawObject.GetHashCode();
		}

		public T Write<T>(T output, TypePrinterFlags flags) where T : ITypeOutput {
			new TypePrinter(output, flags).Write(this);
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
