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
using dndbg.Engine;
using dnlib.DotNet;
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Scripting.Debugger;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerMethod : IDebuggerMethod {
		public string Name {
			get {
				if (name != null)
					return name;
				return debugger.Dispatcher.UI(() => {
					if (name != null)
						return name;
					name = this.CorFunction.GetName() ?? string.Empty;
					return name;
				});
			}
		}
		string name;

		public MethodImplAttributes ImplAttributes => implAttributes;
		readonly MethodImplAttributes implAttributes;

		public MethodAttributes Attributes => attributes;
		readonly MethodAttributes attributes;

		public MethodAttributes Access => attributes & MethodAttributes.MemberAccessMask;
		public bool IsCompilerControlled => IsPrivateScope;
		public bool IsPrivateScope => (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.PrivateScope;
		public bool IsPrivate => (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private;
		public bool IsFamilyAndAssembly => (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem;
		public bool IsAssembly => (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly;
		public bool IsFamily => (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family;
		public bool IsFamilyOrAssembly => (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem;
		public bool IsPublic => (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public;
		public bool IsStatic => (attributes & MethodAttributes.Static) != 0;
		public bool IsFinal => (attributes & MethodAttributes.Final) != 0;
		public bool IsVirtual => (attributes & MethodAttributes.Virtual) != 0;
		public bool IsHideBySig => (attributes & MethodAttributes.HideBySig) != 0;
		public bool IsNewSlot => (attributes & MethodAttributes.NewSlot) != 0;
		public bool IsReuseSlot => (attributes & MethodAttributes.NewSlot) == 0;
		public bool IsCheckAccessOnOverride => (attributes & MethodAttributes.CheckAccessOnOverride) != 0;
		public bool IsAbstract => (attributes & MethodAttributes.Abstract) != 0;
		public bool IsSpecialName => (attributes & MethodAttributes.SpecialName) != 0;
		public bool IsPinvokeImpl => (attributes & MethodAttributes.PinvokeImpl) != 0;
		public bool IsUnmanagedExport => (attributes & MethodAttributes.UnmanagedExport) != 0;
		public bool IsRuntimeSpecialName => (attributes & MethodAttributes.RTSpecialName) != 0;
		public bool HasSecurity => (attributes & MethodAttributes.HasSecurity) != 0;
		public bool IsRequireSecObject => (attributes & MethodAttributes.RequireSecObject) != 0;
		public MethodImplAttributes CodeType => implAttributes & MethodImplAttributes.CodeTypeMask;
		public bool IsIL => (implAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.IL;
		public bool IsNative => (implAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Native;
		public bool IsOPTIL => (implAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.OPTIL;
		public bool IsRuntime => (implAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Runtime;
		public bool IsUnmanaged => (implAttributes & MethodImplAttributes.Unmanaged) != 0;
		public bool IsManaged => (implAttributes & MethodImplAttributes.Unmanaged) == 0;
		public bool IsForwardRef => (implAttributes & MethodImplAttributes.ForwardRef) != 0;
		public bool IsPreserveSig => (implAttributes & MethodImplAttributes.PreserveSig) != 0;
		public bool IsInternalCall => (implAttributes & MethodImplAttributes.InternalCall) != 0;
		public bool IsSynchronized => (implAttributes & MethodImplAttributes.Synchronized) != 0;
		public bool IsNoInlining => (implAttributes & MethodImplAttributes.NoInlining) != 0;
		public bool IsAggressiveInlining => (implAttributes & MethodImplAttributes.AggressiveInlining) != 0;
		public bool IsNoOptimization => (implAttributes & MethodImplAttributes.NoOptimization) != 0;

		public MethodSig MethodSig {
			get {
				if (methodSig != null)
					return methodSig;
				return debugger.Dispatcher.UI(() => {
					if (methodSig != null)
						return methodSig;
					methodSig = this.CorFunction.GetMethodSig();
					if (methodSig == null)
						methodSig = MethodSig.CreateStatic(new CorLibTypes(new ModuleDefUser()).Void);
					return methodSig;
				});
			}
		}
		MethodSig methodSig;

		public IDebuggerClass Class => debugger.Dispatcher.UI(() => {
			var cls = this.CorFunction.Class;
			return cls == null ? null : new DebuggerClass(debugger, cls);
		});

		public uint CurrentVersionNumber => debugger.Dispatcher.UI(() => this.CorFunction.CurrentVersionNumber);

		public IDebuggerCode ILCode => debugger.Dispatcher.UI(() => {
			var code = this.CorFunction.ILCode;
			return code == null ? null : new DebuggerCode(debugger, code);
		});

		public bool JustMyCode {
			get { return debugger.Dispatcher.UI(() => CorFunction.JustMyCode); }
			set { debugger.Dispatcher.UI(() => CorFunction.JustMyCode = value); }
		}

		public uint LocalVarSigToken { get; }

		public IDebuggerModule Module {
			get {
				if (module != null)
					return module;
				debugger.Dispatcher.UI(() => {
					if (module == null)
						module = debugger.FindModuleUI(this.CorFunction.Module);
				});
				return module;
			}
		}

		public IDebuggerCode NativeCode => debugger.Dispatcher.UI(() => {
			var code = this.CorFunction.NativeCode;
			return code == null ? null : new DebuggerCode(debugger, code);
		});

		public uint Token { get; }
		public uint VersionNumber => debugger.Dispatcher.UI(() => this.CorFunction.VersionNumber);
		public CorFunction CorFunction { get; }

		readonly Debugger debugger;
		readonly int hashCode;
		IDebuggerModule module;

		public DebuggerMethod(Debugger debugger, CorFunction func) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.CorFunction = func;
			this.hashCode = func.GetHashCode();
			this.LocalVarSigToken = func.LocalVarSigToken;
			this.Token = func.Token;
			func.GetAttributes(out implAttributes, out attributes);
		}

		public IILBreakpoint CreateBreakpoint(uint offset, Func<IILBreakpoint, bool> cond) => debugger.Dispatcher.UI(() => {
			var mod = this.CorFunction.Module;
			var module = mod == null ? new ModuleName() : Utils.ToModuleName(mod.SerializedDnModule);
			return debugger.CreateBreakpoint(module, this.CorFunction.Token, offset, cond);
		});

		public IILBreakpoint CreateBreakpoint(int offset, Func<IILBreakpoint, bool> cond) =>
			CreateBreakpoint((uint)offset, cond);
		public INativeBreakpoint CreateNativeBreakpoint(uint offset, Func<INativeBreakpoint, bool> cond) =>
			debugger.Dispatcher.UI(() => debugger.CreateNativeBreakpoint(NativeCode, offset, cond));
		public INativeBreakpoint CreateNativeBreakpoint(int offset, Func<INativeBreakpoint, bool> cond) =>
			CreateNativeBreakpoint((uint)offset, cond);

		public override bool Equals(object obj) => (obj as DebuggerMethod)?.CorFunction == CorFunction;
		public override int GetHashCode() => hashCode;
		const TypePrinterFlags DEFAULT_FLAGS = TypePrinterFlags.ShowParameterTypes |
			TypePrinterFlags.ShowReturnTypes | TypePrinterFlags.ShowNamespaces |
			TypePrinterFlags.ShowTypeKeywords;
		public void WriteTo(IOutputWriter output) => Write(output, (TypeFormatFlags)DEFAULT_FLAGS);
		public void Write(IOutputWriter output, TypeFormatFlags flags) =>
			debugger.Dispatcher.UI(() => CorFunction.Write(new OutputWriterConverter(output), (TypePrinterFlags)flags));
		public string ToString(TypeFormatFlags flags) => debugger.Dispatcher.UI(() => this.CorFunction.ToString((TypePrinterFlags)flags));
		public override string ToString() => debugger.Dispatcher.UI(() => this.CorFunction.ToString(DEFAULT_FLAGS));
	}
}
