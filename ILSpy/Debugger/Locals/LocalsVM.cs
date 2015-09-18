/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Debugger.CallStack;
using dnSpy.MVVM;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.TreeView;

namespace dnSpy.Debugger.Locals {
	sealed class LocalsVM : ViewModelBase {
		internal bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					// Don't call OnPropertyChanged() since it's only used internally by the View
					isEnabled = value;
					InitializeLocals();
				}
			}
		}
		bool isEnabled;

		public SharpTreeNode Root {
			get { return rootNode; }
		}
		readonly SharpTreeNode rootNode;

		public static TypePrinterFlags TypePrinterFlags {
			get {
				TypePrinterFlags flags = TypePrinterFlags.ShowArrayValueSizes;
				if (LocalsSettings.Instance.ShowNamespaces) flags |= TypePrinterFlags.ShowNamespaces;
				if (LocalsSettings.Instance.ShowTokens) flags |= TypePrinterFlags.ShowTokens;
				if (LocalsSettings.Instance.ShowTypeKeywords) flags |= TypePrinterFlags.ShowTypeKeywords;
				if (!DebuggerSettings.Instance.UseHexadecimal) flags |= TypePrinterFlags.UseDecimal;
				return flags;
			}
		}

		readonly IMethodLocalProvider methodLocalProvider;

		public LocalsVM(IMethodLocalProvider methodLocalProvider) {
			this.methodLocalProvider = methodLocalProvider;
			methodLocalProvider.NewMethodInfoAvailable += MethodLocalProvider_NewMethodInfoAvailable;
			this.rootNode = new SharpTreeNode();
			StackFrameManager.Instance.StackFramesUpdated += StackFrameManager_StackFramesUpdated;
			StackFrameManager.Instance.PropertyChanged += StackFrameManager_PropertyChanged;
			DebugManager.Instance.OnProcessStateChanged += DebugManager_OnProcessStateChanged;
			DebuggerSettings.Instance.PropertyChanged += DebuggerSettings_PropertyChanged;
			LocalsSettings.Instance.PropertyChanged += LocalsSettings_PropertyChanged;
		}

		void MethodLocalProvider_NewMethodInfoAvailable(object sender, EventArgs e) {
			InitializeLocalAndArgNames();
		}

		private void LocalsSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			switch (e.PropertyName) {
			case "ShowNamespaces":
			case "ShowTypeKeywords":
			case "ShowTokens":
				RefreshTypeFields();
				break;
			}
		}

		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "UseHexadecimal")
				RefreshHexFields();
		}

		void DebugManager_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			switch (DebugManager.Instance.ProcessState) {
			case DebuggerProcessState.Starting:
				frameInfo = null;
				break;

			case DebuggerProcessState.Running:
				break;

			case DebuggerProcessState.Stopped:
				// Handled in StackFrameManager_StackFramesUpdated
				break;

			case DebuggerProcessState.Terminated:
				frameInfo = null;
				break;
			}
		}
		FrameInfo frameInfo = null;

		sealed class FrameInfo : IEquatable<FrameInfo> {
			public readonly CorFunction Function;

			public MethodKey? Key {
				get {
					if (Function == null)
						return null;
					var mod = Function.Module;
					if (mod == null)
						return null;
					return MethodKey.Create(Function.Token, mod.SerializedDnModule);
				}
			}

			public FrameInfo(CorFrame frame, int frameNo) {
				this.Function = frame.Function;
			}

			public bool Equals(FrameInfo other) {
				return Function == other.Function;
			}

			public override bool Equals(object obj) {
				return Equals(obj as FrameInfo);
			}

			public override int GetHashCode() {
				return Function == null ? 0 : Function.GetHashCode();
			}
		}

		void StackFrameManager_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "SelectedFrameNumber")
				InitializeLocals();
		}

		void StackFrameManager_StackFramesUpdated(object sender, StackFramesUpdatedEventArgs e) {
			if (e.Debugger.IsEvaluating)
				return;
			// InitializeStackFrames() is called by LocalsControlCreator when the process has been
			// running for a little while. Speeds up stepping.
			if (DebugManager.Instance.ProcessState != DebuggerProcessState.Running)
				InitializeLocals();
		}

		internal void InitializeLocals() {
			if (!IsEnabled || DebugManager.Instance.ProcessState != DebuggerProcessState.Stopped) {
				frameInfo = null;
				rootNode.Children.Clear();
				return;
			}

			var frame = StackFrameManager.Instance.SelectedFrame;
			int frameNo = StackFrameManager.Instance.SelectedFrameNumber;
			if (frame == null) {
				frameInfo = null;
				rootNode.Children.Clear();
				return;
			}

			var newFrameInfo = new FrameInfo(frame, frameNo);
			if (frameInfo == null || !frameInfo.Equals(newFrameInfo))
				rootNode.Children.Clear();

			var args = new List<CorValueHolder>();
			var locals = new List<CorValueHolder>();
			int index = 0;
			foreach (var arg in frame.ILArguments) {
				int indexTmp = index;
				args.Add(new CorValueHolder(arg, () => GetILArgument(indexTmp), GetDebugger));
				index++;
			}
			index = 0;
			foreach (var local in frame.ILLocals) {
				int indexTmp = index;
				locals.Add(new CorValueHolder(local, () => GetILLocal(indexTmp), GetDebugger));
				index++;
			}

			if (!CanReuseChildren(args.Count, locals.Count))
				rootNode.Children.Clear();

			if (rootNode.Children.Count == 0) {
				hasInitializedArgNames = false;
				hasInitializedLocalNames = false;
				hasInitializedArgNamesFromMetadata = false;
				hasInitializedLocalNamesFromPdbFile = false;
			}

			valueContext = new ValueContext(frame);
			frameInfo = newFrameInfo;

			List<TypeSig> argTypes;
			List<TypeSig> localTypes;
			frame.GetArgAndLocalTypes(out argTypes, out localTypes);

			if (rootNode.Children.Count == 0) {
				for (int i = 0; i < args.Count; i++)
					rootNode.Children.Add(new ArgumentValueVM(valueContext, args[i], Read(argTypes, i), i));
				for (int i = 0; i < locals.Count; i++)
					rootNode.Children.Add(new LocalValueVM(valueContext, locals[i], Read(localTypes, i), i));
			}
			else {
				for (int i = 0; i < args.Count; i++)
					((ValueVM)rootNode.Children[i]).Reinitialize(valueContext, args[i], Read(argTypes, i));
				for (int i = 0; i < locals.Count; i++)
					((ValueVM)rootNode.Children[args.Count + i]).Reinitialize(valueContext, locals[i], Read(localTypes, i));
			}

			InitializeLocalAndArgNames();
			if (!hasInitializedArgNames && !hasInitializedArgNamesFromMetadata)
				InitializeArgNamesFromMetadata();
			if (!hasInitializedLocalNames && !hasInitializedLocalNamesFromPdbFile)
				InitializeLocalNamesFromPdbFile();
		}
		bool hasInitializedArgNames;
		bool hasInitializedLocalNames;
		bool hasInitializedArgNamesFromMetadata;
		bool hasInitializedLocalNamesFromPdbFile;
		ValueContext valueContext;

		static DnDebugger GetDebugger() {
			return DebugManager.Instance.Debugger;
		}

		CorValue GetILArgument(int index) {
			Debug.Fail("NYI. Get the correct new IL Frame");//TODO:
			return valueContext.Frame.GetILArgument((uint)index);
		}

		CorValue GetILLocal(int index) {
			Debug.Fail("NYI. Get the correct new IL Frame");//TODO:
			return valueContext.Frame.GetILLocal((uint)index);
		}

		static T Read<T>(IList<T> list, int index) {
			if ((uint)index >= (uint)list.Count)
				return default(T);
			return list[index];
		}

		void InitializeArgNamesFromMetadata() {
			if (hasInitializedArgNamesFromMetadata)
				return;
			hasInitializedArgNamesFromMetadata = true;
			var func = frameInfo.Function;
			if (func == null)
				return;
			MethodAttributes methodAttrs;
			MethodImplAttributes methodImplAttrs;
			var ps = func.GetMDParameters(out methodAttrs, out methodImplAttrs);
			if (ps == null)
				return;

			foreach (var vm in rootNode.Children.OfType<ArgumentValueVM>()) {
				var info = ps.Get((uint)vm.Index);

				bool isThis = vm.Index == 0 && (methodAttrs & MethodAttributes.Static) == 0;
				if (isThis)
					vm.InitializeName(string.Empty, isThis);
				else if (info != null)
					vm.InitializeName(info.Value.Name, isThis);
			}
		}

		void InitializeLocalNamesFromPdbFile() {
			if (hasInitializedLocalNamesFromPdbFile)
				return;
			hasInitializedLocalNamesFromPdbFile = true;
			//TODO:
		}

		void InitializeLocalAndArgNames() {
			if (frameInfo == null)
				return;
			if (hasInitializedLocalNames && hasInitializedArgNames)
				return;

			var key = frameInfo.Key;
			if (key == null)
				return;

			Parameter[] parameters;
			Local[] locals;
			ILVariable[] decLocals;
			methodLocalProvider.GetMethodInfo(key.Value, out parameters, out locals, out decLocals);
			if (!hasInitializedArgNames && parameters != null) {
				hasInitializedArgNames = true;
				foreach (var vm in rootNode.Children.OfType<ArgumentValueVM>()) {
					if ((uint)vm.Index >= parameters.Length)
						continue;
					var p = parameters[vm.Index];
					vm.InitializeName(p.Name, p.IsHiddenThisParameter);
				}
			}
			if (!hasInitializedLocalNames && (locals != null || decLocals != null)) {
				hasInitializedLocalNames = true;
				foreach (var vm in rootNode.Children.OfType<LocalValueVM>()) {
					var l = locals == null || (uint)vm.Index >= (uint)locals.Length ? null : locals[vm.Index];
					var dl = decLocals == null || (uint)vm.Index >= (uint)decLocals.Length ? null : decLocals[vm.Index];
					string name = dl == null ? null : dl.Name;
					if (name == null && l != null && !string.IsNullOrEmpty(l.Name))
						name = string.Format("[{0}]", l.Name);
					if (string.IsNullOrEmpty(name))
						continue;

					vm.InitializeName(name);
				}
			}
		}

		bool CanReuseChildren(int numArgs, int numLocals) {
			var children = rootNode.Children;
			if (children.Count != numArgs + numLocals)
				return false;

			for (int i = 0; i < numArgs; i++) {
				if (!(children[i] is ArgumentValueVM))
					return false;
			}

			for (int i = 0; i < numLocals; i++) {
				if (!(children[numArgs + i] is LocalValueVM))
					return false;
			}

			return true;
		}

		IEnumerable<ValueVM> GetValueVMs() {
			return rootNode.Descendants().Cast<ValueVM>();
		}

		void RefreshTypeFields() {
			foreach (var vm in GetValueVMs())
				vm.RefreshTypeFields();
		}

		void RefreshHexFields() {
			foreach (var vm in GetValueVMs())
				vm.RefreshHexFields();
		}

		internal void RefreshThemeFields() {
			foreach (var vm in GetValueVMs())
				vm.RefreshThemeFields();
		}

		internal void RefreshSyntaxHighlightFields() {
			foreach (var vm in GetValueVMs())
				vm.RefreshSyntaxHighlightFields();
		}
	}
}
