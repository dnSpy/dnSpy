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
using System.Diagnostics;
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;
using dnlib.DotNet;

namespace dndbg.Engine {
	sealed class CorFunction : COMObject<ICorDebugFunction>, IEquatable<CorFunction?> {
		public CorModule? Module {
			get {
				if (!(module is null))
					return module;
				int hr = obj.GetModule(out var mod);
				return module = hr < 0 || mod is null ? null : new CorModule(mod);
			}
		}
		CorModule? module;

		public CorClass? Class {
			get {
				int hr = obj.GetClass(out var cls);
				if (hr >= 0 && !(cls is null))
					return new CorClass(cls);

				// Here if it's an extern method, eg. it's not IL code, but native code

				var mod = Module;
				Debug2.Assert(!(mod is null));
				var mdi = mod?.GetMetaDataInterface<IMetaDataImport>();
				uint tdOwner = 0x02000000 + MDAPI.GetMethodOwnerRid(mdi, Token);
				return mod?.GetClassFromToken(tdOwner);
			}
		}

		public uint Token {
			get {
				int hr = obj.GetToken(out uint token);
				return hr < 0 ? 0 : token;
			}
		}

		public bool JustMyCode {
			get {
				var func2 = obj as ICorDebugFunction2;
				if (func2 is null)
					return false;
				int hr = func2.GetJMCStatus(out int status);
				return hr >= 0 && status != 0;
			}
			set {
				var func2 = obj as ICorDebugFunction2;
				if (func2 is null)
					return;
				int hr = func2.SetJMCStatus(value ? 1 : 0);
			}
		}

		public uint CurrentVersionNumber {
			get {
				int hr = obj.GetCurrentVersionNumber(out uint ver);
				return hr < 0 ? 0 : ver;
			}
		}

		public uint VersionNumber {
			get {
				var func2 = obj as ICorDebugFunction2;
				if (func2 is null)
					return CurrentVersionNumber;
				int hr = func2.GetVersionNumber(out uint ver);
				return hr < 0 ? 0 : ver;
			}
		}

		public uint LocalVarSigToken {
			get {
				int hr = obj.GetLocalVarSigToken(out uint token);
				return hr < 0 ? 0 : token;
			}
		}

		public CorCode? ILCode {
			get {
				int hr = obj.GetILCode(out var code);
				return hr < 0 || code is null ? null : new CorCode(code);
			}
		}

		public CorCode? NativeCode {
			get {
				int hr = obj.GetNativeCode(out var code);
				return hr < 0 || code is null ? null : new CorCode(code);
			}
		}

		public CorFunction(ICorDebugFunction func, CorModule? module = null)
			: base(func) {
		}

		public MethodAttributes GetAttributes() {
			MDAPI.GetMethodAttributes(Module?.GetMetaDataInterface<IMetaDataImport>(), Token, out var attributes, out var implAttributes);
			return attributes;
		}

		public string? GetName() => MDAPI.GetMethodName(Module?.GetMetaDataInterface<IMetaDataImport>(), Token);

		public bool Equals(CorFunction? other) => !(other is null) && RawObject == other.RawObject;
		public override bool Equals(object? obj) => Equals(obj as CorFunction);
		public override int GetHashCode() => RawObject.GetHashCode();
	}
}
