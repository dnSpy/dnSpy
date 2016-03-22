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
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Scripting.Debugger;
using dnSpy.Shared.Scripting;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerMethod : IDebuggerMethod {
		public string Name {
			get {
				if (name != null)
					return name;
				return debugger.Dispatcher.UI(() => {
					if (name != null)
						return name;
					name = func.GetName() ?? string.Empty;
					return name;
				});
			}
		}
		string name;

		public MethodImplAttributes ImplAttributes {
			get { return implAttributes; }
		}
		readonly MethodImplAttributes implAttributes;

		public MethodAttributes Attributes {
			get { return attributes; }
		}
		readonly MethodAttributes attributes;

		public MethodAttributes Access {
			get { return attributes & MethodAttributes.MemberAccessMask; }
		}

		public bool IsCompilerControlled {
			get { return IsPrivateScope; }
		}

		public bool IsPrivateScope {
			get { return (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.PrivateScope; }
		}

		public bool IsPrivate {
			get { return (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private; }
		}

		public bool IsFamilyAndAssembly {
			get { return (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem; }
		}

		public bool IsAssembly {
			get { return (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly; }
		}

		public bool IsFamily {
			get { return (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family; }
		}

		public bool IsFamilyOrAssembly {
			get { return (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem; }
		}

		public bool IsPublic {
			get { return (attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public; }
		}

		public bool IsStatic {
			get { return (attributes & MethodAttributes.Static) != 0; }
		}

		public bool IsFinal {
			get { return (attributes & MethodAttributes.Final) != 0; }
		}

		public bool IsVirtual {
			get { return (attributes & MethodAttributes.Virtual) != 0; }
		}

		public bool IsHideBySig {
			get { return (attributes & MethodAttributes.HideBySig) != 0; }
		}

		public bool IsNewSlot {
			get { return (attributes & MethodAttributes.NewSlot) != 0; }
		}

		public bool IsReuseSlot {
			get { return (attributes & MethodAttributes.NewSlot) == 0; }
		}

		public bool IsCheckAccessOnOverride {
			get { return (attributes & MethodAttributes.CheckAccessOnOverride) != 0; }
		}

		public bool IsAbstract {
			get { return (attributes & MethodAttributes.Abstract) != 0; }
		}

		public bool IsSpecialName {
			get { return (attributes & MethodAttributes.SpecialName) != 0; }
		}

		public bool IsPinvokeImpl {
			get { return (attributes & MethodAttributes.PinvokeImpl) != 0; }
		}

		public bool IsUnmanagedExport {
			get { return (attributes & MethodAttributes.UnmanagedExport) != 0; }
		}

		public bool IsRuntimeSpecialName {
			get { return (attributes & MethodAttributes.RTSpecialName) != 0; }
		}

		public bool HasSecurity {
			get { return (attributes & MethodAttributes.HasSecurity) != 0; }
		}

		public bool IsRequireSecObject {
			get { return (attributes & MethodAttributes.RequireSecObject) != 0; }
		}

		public MethodImplAttributes CodeType {
			get { return implAttributes & MethodImplAttributes.CodeTypeMask; }
		}

		public bool IsIL {
			get { return (implAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.IL; }
		}

		public bool IsNative {
			get { return (implAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Native; }
		}

		public bool IsOPTIL {
			get { return (implAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.OPTIL; }
		}

		public bool IsRuntime {
			get { return (implAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Runtime; }
		}

		public bool IsUnmanaged {
			get { return (implAttributes & MethodImplAttributes.Unmanaged) != 0; }
		}

		public bool IsManaged {
			get { return (implAttributes & MethodImplAttributes.Unmanaged) == 0; }
		}

		public bool IsForwardRef {
			get { return (implAttributes & MethodImplAttributes.ForwardRef) != 0; }
		}

		public bool IsPreserveSig {
			get { return (implAttributes & MethodImplAttributes.PreserveSig) != 0; }
		}

		public bool IsInternalCall {
			get { return (implAttributes & MethodImplAttributes.InternalCall) != 0; }
		}

		public bool IsSynchronized {
			get { return (implAttributes & MethodImplAttributes.Synchronized) != 0; }
		}

		public bool IsNoInlining {
			get { return (implAttributes & MethodImplAttributes.NoInlining) != 0; }
		}

		public bool IsAggressiveInlining {
			get { return (implAttributes & MethodImplAttributes.AggressiveInlining) != 0; }
		}

		public bool IsNoOptimization {
			get { return (implAttributes & MethodImplAttributes.NoOptimization) != 0; }
		}

		public MethodSig MethodSig {
			get {
				if (methodSig != null)
					return methodSig;
				return debugger.Dispatcher.UI(() => {
					if (methodSig != null)
						return methodSig;
					methodSig = func.GetMethodSig();
					if (methodSig == null)
						methodSig = MethodSig.CreateStatic(new CorLibTypes(new ModuleDefUser()).Void);
					return methodSig;
				});
			}
		}
		MethodSig methodSig;

		public IDebuggerClass Class {
			get {
				return debugger.Dispatcher.UI(() => {
					var cls = func.Class;
					return cls == null ? null : new DebuggerClass(debugger, cls);
				});
			}
		}

		public uint CurrentVersionNumber {
			get { return debugger.Dispatcher.UI(() => func.CurrentVersionNumber); }
		}

		public IDebuggerCode ILCode {
			get {
				return debugger.Dispatcher.UI(() => {
					var code = func.ILCode;
					return code == null ? null : new DebuggerCode(debugger, code);
				});
			}
		}

		public bool JustMyCode {
			get { return debugger.Dispatcher.UI(() => func.JustMyCode); }
			set { debugger.Dispatcher.UI(() => func.JustMyCode = value); }
		}

		public uint LocalVarSigToken {
			get { return localVarSigToken; }
		}

		public IDebuggerModule Module {
			get {
				if (module != null)
					return module;
				debugger.Dispatcher.UI(() => {
					if (module == null)
						module = debugger.FindModuleUI(func.Module);
				});
				return module;
			}
		}

		public IDebuggerCode NativeCode {
			get {
				return debugger.Dispatcher.UI(() => {
					var code = func.NativeCode;
					return code == null ? null : new DebuggerCode(debugger, code);
				});
			}
		}

		public uint Token {
			get { return token; }
		}

		public uint VersionNumber {
			get { return debugger.Dispatcher.UI(() => func.VersionNumber); }
		}

		public CorFunction CorFunction {
			get { return func; }
		}
		readonly CorFunction func;

		readonly Debugger debugger;
		readonly int hashCode;
		readonly uint localVarSigToken;
		readonly uint token;
		IDebuggerModule module;

		public DebuggerMethod(Debugger debugger, CorFunction func) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.func = func;
			this.hashCode = func.GetHashCode();
			this.localVarSigToken = func.LocalVarSigToken;
			this.token = func.Token;
			func.GetAttributes(out implAttributes, out attributes);
		}

		public IILBreakpoint CreateBreakpoint(uint offset, Func<IILBreakpoint, bool> cond) {
			return debugger.Dispatcher.UI(() => {
				var mod = func.Module;
				var module = mod == null ? new ModuleName() : mod.SerializedDnModule.ToModuleName();
				return debugger.CreateBreakpoint(module, func.Token, offset, cond);
			});
		}

		public IILBreakpoint CreateBreakpoint(int offset, Func<IILBreakpoint, bool> cond) {
			return CreateBreakpoint((uint)offset, cond);
		}

		public INativeBreakpoint CreateNativeBreakpoint(uint offset, Func<INativeBreakpoint, bool> cond) {
			return debugger.Dispatcher.UI(() => debugger.CreateNativeBreakpoint(NativeCode, offset, cond));
		}

		public INativeBreakpoint CreateNativeBreakpoint(int offset, Func<INativeBreakpoint, bool> cond) {
			return CreateNativeBreakpoint((uint)offset, cond);
		}

		public override bool Equals(object obj) {
			var other = obj as DebuggerMethod;
			return other != null && other.func == func;
		}

		public override int GetHashCode() {
			return hashCode;
		}

		public void Write(ISyntaxHighlightOutput output, TypeFormatFlags flags) {
			debugger.Dispatcher.UI(() => func.Write(new OutputConverter(output), (TypePrinterFlags)flags));
		}

		public string ToString(TypeFormatFlags flags) {
			return debugger.Dispatcher.UI(() => func.ToString((TypePrinterFlags)flags));
		}

		public override string ToString() {
			const TypePrinterFlags flags = TypePrinterFlags.ShowParameterTypes |
				TypePrinterFlags.ShowReturnTypes | TypePrinterFlags.ShowNamespaces |
				TypePrinterFlags.ShowTypeKeywords;
			return debugger.Dispatcher.UI(() => func.ToString(flags));
		}
	}
}
