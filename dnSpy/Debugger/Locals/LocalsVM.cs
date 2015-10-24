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
using System.Windows.Threading;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Debugger.CallStack;
using dnSpy.Files;
using dnSpy.MVVM;
using ICSharpCode.Decompiler.ILAst;
using ICSharpCode.TreeView;

namespace dnSpy.Debugger.Locals {
	enum LocalInitType {
		/// <summary>
		/// Evaluate expressions if necessary
		/// </summary>
		Full,

		/// <summary>
		/// Don't evealuate any expressions
		/// </summary>
		Simple,
	}

	sealed class LocalsVM : ViewModelBase, ILocalsOwner {
		public IAskUser AskUser {
			set { askUser = value; }
		}
		IAskUser askUser;

		internal bool IsEnabled {
			get { return isEnabled; }
			set {
				if (isEnabled != value) {
					// Don't call OnPropertyChanged() since it's only used internally by the View
					isEnabled = value;
					InitializeLocals(LocalInitType.Full);
				}
			}
		}
		bool isEnabled;

		public SharpTreeNode Root {
			get { return rootNode; }
		}
		readonly SharpTreeNode rootNode;

		public TypePrinterFlags TypePrinterFlags {
			get {
				TypePrinterFlags flags = TypePrinterFlags.ShowArrayValueSizes;
				if (LocalsSettings.Instance.ShowNamespaces) flags |= TypePrinterFlags.ShowNamespaces;
				if (LocalsSettings.Instance.ShowTokens) flags |= TypePrinterFlags.ShowTokens;
				if (LocalsSettings.Instance.ShowTypeKeywords) flags |= TypePrinterFlags.ShowTypeKeywords;
				if (!DebuggerSettings.Instance.UseHexadecimal) flags |= TypePrinterFlags.UseDecimal;
				return flags;
			}
		}

		public bool DebuggerBrowsableAttributesCanHidePropsFields {
			get { return DebuggerSettings.Instance.DebuggerBrowsableAttributesCanHidePropsFields; }
		}

		public bool CompilerGeneratedAttributesCanHideFields {
			get { return DebuggerSettings.Instance.CompilerGeneratedAttributesCanHideFields; }
		}

		readonly IMethodLocalProvider methodLocalProvider;
		readonly Dispatcher dispatcher;

		public LocalsVM(Dispatcher dispatcher, IMethodLocalProvider methodLocalProvider) {
			this.dispatcher = dispatcher;
			this.methodLocalProvider = methodLocalProvider;
			methodLocalProvider.NewMethodInfoAvailable += MethodLocalProvider_NewMethodInfoAvailable;
			this.rootNode = new SharpTreeNode();
			StackFrameManager.Instance.StackFramesUpdated += StackFrameManager_StackFramesUpdated;
			StackFrameManager.Instance.PropertyChanged += StackFrameManager_PropertyChanged;
			DebugManager.Instance.OnProcessStateChanged += DebugManager_OnProcessStateChanged;
			DebuggerSettings.Instance.PropertyChanged += DebuggerSettings_PropertyChanged;
			LocalsSettings.Instance.PropertyChanged += LocalsSettings_PropertyChanged;
			DebugManager.Instance.ProcessRunning += DebugManager_ProcessRunning;
		}

		void DebugManager_ProcessRunning(object sender, EventArgs e) {
			InitializeLocals(LocalInitType.Full);
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
			switch (e.PropertyName) {
			case "UseHexadecimal":
				RefreshHexFields();
				break;
			case "SyntaxHighlightLocals":
				RefreshSyntaxHighlightFields();
				break;
			case "PropertyEvalAndFunctionCalls":
			case "DebuggerBrowsableAttributesCanHidePropsFields":
			case "CompilerGeneratedAttributesCanHideFields":
				RecreateLocals();
				break;
			case "UseStringConversionFunction":
				RefreshToStringFields();
				break;
			}
		}

		void DebugManager_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			switch (DebugManager.Instance.ProcessState) {
			case DebuggerProcessState.Starting:
				frameInfo = null;
				break;

			case DebuggerProcessState.Continuing:
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
			public readonly ValueContext ValueContext;

			public SerializedDnSpyToken? Key {
				get {
					if (ValueContext.Function == null)
						return null;
					var mod = ValueContext.Function.Module;
					if (mod == null)
						return null;
					return new SerializedDnSpyToken(mod.SerializedDnModule.ToSerializedDnSpyModule(), ValueContext.Function.Token);
				}
			}

			public FrameInfo(ILocalsOwner localsOwner, DnThread thread, DnProcess process, CorFrame frame, int frameNo) {
				this.ValueContext = new ValueContext(localsOwner, frame, thread, process);
			}

			public bool Equals(FrameInfo other) {
				return ValueContext.Function == other.ValueContext.Function;
			}

			public override bool Equals(object obj) {
				return Equals(obj as FrameInfo);
			}

			public override int GetHashCode() {
				return ValueContext.Function == null ? 0 : ValueContext.Function.GetHashCode();
			}
		}

		void StackFrameManager_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			if (e.PropertyName == "SelectedFrameNumber")
				InitializeLocals(LocalInitType.Full);
		}

		void StackFrameManager_StackFramesUpdated(object sender, StackFramesUpdatedEventArgs e) {
			if (e.Debugger.IsEvaluating)
				return;
			// InitializeLocals() is called when the process has been running for a little while. Speeds up stepping.
			if (DebugManager.Instance.ProcessState != DebuggerProcessState.Continuing && DebugManager.Instance.ProcessState != DebuggerProcessState.Running)
				InitializeLocals(e.Debugger.EvalCompleted ? LocalInitType.Simple : LocalInitType.Full);
		}

		void ClearAllLocals() {
			ClearAndDisposeChildren();
			frameInfo = null;
		}

		void ClearAndDisposeChildren() {
			ValueVM.ClearAndDisposeChildren(rootNode);
		}

		void RecreateLocals() {
			ClearAllLocals();
			InitializeLocals(LocalInitType.Full);
		}

		void InitializeLocals(LocalInitType initType) {
			if (!IsEnabled || DebugManager.Instance.ProcessState != DebuggerProcessState.Stopped) {
				ClearAllLocals();
				return;
			}

			if (initType == LocalInitType.Simple) {
				// Property eval has completed, don't do a thing
				return;
			}

			var thread = StackFrameManager.Instance.SelectedThread;
			var frame = StackFrameManager.Instance.SelectedFrame;
			int frameNo = StackFrameManager.Instance.SelectedFrameNumber;
			DnProcess process;
			if (thread == null) {
				process = DebugManager.Instance.Debugger.Processes.FirstOrDefault();
				thread = process == null ? null : process.Threads.FirstOrDefault();
			}
			else
				process = thread.Process;

			var newFrameInfo = new FrameInfo(this, thread, process, frame, frameNo);
			if (frameInfo == null || !frameInfo.Equals(newFrameInfo))
				ClearAndDisposeChildren();
			frameInfo = newFrameInfo;

			CorValue[] corArgs, corLocals;
			if (frame != null) {
				corArgs = frame.ILArguments.ToArray();
				corLocals = frame.ILLocals.ToArray();
			}
			else
				corArgs = corLocals = new CorValue[0];
			var args = new List<ICorValueHolder>(corArgs.Length);
			var locals = new List<ICorValueHolder>(corLocals.Length);
			for (int i = 0; i < corArgs.Length; i++)
				args.Add(new LocArgCorValueHolder(true, this, corArgs[i], i));
			for (int i = 0; i < corLocals.Length; i++)
				locals.Add(new LocArgCorValueHolder(false, this, corLocals[i], i));

			var exValue = thread == null ? null : thread.CorThread.CurrentException;
			var exValueHolder = exValue == null ? null : new DummyCorValueHolder(exValue);

			int numGenArgs = frameInfo.ValueContext.GenericTypeArguments.Count + frameInfo.ValueContext.GenericMethodArguments.Count;

			if (!CanReuseChildren(exValueHolder, args.Count, locals.Count, numGenArgs))
				ClearAndDisposeChildren();

			if (rootNode.Children.Count == 0) {
				hasInitializedArgNames = false;
				hasInitializedLocalNames = false;
				hasInitializedArgNamesFromMetadata = false;
				hasInitializedLocalNamesFromPdbFile = false;
			}

			List<TypeSig> argTypes;
			List<TypeSig> localTypes;
			if (frame != null)
				frame.GetArgAndLocalTypes(out argTypes, out localTypes);
			else
				argTypes = localTypes = new List<TypeSig>();

			if (rootNode.Children.Count == 0) {
				if (exValueHolder != null)
					rootNode.Children.Add(new CorValueVM(frameInfo.ValueContext, exValueHolder, null, new ExceptionValueType()));
				for (int i = 0; i < args.Count; i++)
					rootNode.Children.Add(new CorValueVM(frameInfo.ValueContext, args[i], Read(argTypes, i), new ArgumentValueType(i)));
				for (int i = 0; i < locals.Count; i++)
					rootNode.Children.Add(new CorValueVM(frameInfo.ValueContext, locals[i], Read(localTypes, i), new LocalValueType(i)));
				if (numGenArgs != 0)
					rootNode.Children.Add(new TypeVariablesValueVM(frameInfo.ValueContext));
			}
			else {
				int index = 0;

				if (exValueHolder != null) {
					if (index < rootNode.Children.Count && NormalValueVM.IsType<ExceptionValueType>(rootNode.Children[index]))
						((CorValueVM)rootNode.Children[index++]).Reinitialize(frameInfo.ValueContext, exValueHolder, null);
					else
						rootNode.Children.Insert(index++, new CorValueVM(frameInfo.ValueContext, exValueHolder, null, new ExceptionValueType()));
				}
				else {
					if (index < rootNode.Children.Count && NormalValueVM.IsType<ExceptionValueType>(rootNode.Children[index]))
						ValueVM.DisposeAndRemoveAt(rootNode, index);
				}

				for (int i = 0; i < args.Count; i++, index++)
					((CorValueVM)rootNode.Children[index]).Reinitialize(frameInfo.ValueContext, args[i], Read(argTypes, i));
				for (int i = 0; i < locals.Count; i++, index++)
					((CorValueVM)rootNode.Children[index]).Reinitialize(frameInfo.ValueContext, locals[i], Read(localTypes, i));
				if (numGenArgs != 0)
					((TypeVariablesValueVM)rootNode.Children[index++]).Reinitialize(frameInfo.ValueContext);
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

		static T Read<T>(IList<T> list, int index) {
			if ((uint)index >= (uint)list.Count)
				return default(T);
			return list[index];
		}

		void InitializeArgNamesFromMetadata() {
			if (hasInitializedArgNamesFromMetadata)
				return;
			hasInitializedArgNamesFromMetadata = true;
			var func = frameInfo.ValueContext.Function;
			if (func == null)
				return;
			MethodAttributes methodAttrs;
			MethodImplAttributes methodImplAttrs;
			var ps = func.GetMDParameters(out methodAttrs, out methodImplAttrs);
			if (ps == null)
				return;

			bool isStatic = (methodAttrs & MethodAttributes.Static) != 0;
			foreach (var vm in rootNode.Children.OfType<NormalValueVM>()) {
				var vt = vm.NormalValueType as ArgumentValueType;
				if (vt == null)
					continue;

				bool isThis = vt.Index == 0 && !isStatic;
				if (isThis)
					vt.InitializeName(string.Empty, isThis);
				else {
					uint index = (uint)vt.Index + (isStatic ? 1U : 0);
					var info = ps.Get(index);

					if (info != null)
						vt.InitializeName(info.Value.Name, isThis);
				}
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
				foreach (var vm in rootNode.Children.OfType<NormalValueVM>()) {
					var vt = vm.NormalValueType as ArgumentValueType;
					if (vt == null)
						continue;
					if ((uint)vt.Index >= parameters.Length)
						continue;
					var p = parameters[vt.Index];
					vt.InitializeName(p.Name, p.IsHiddenThisParameter);
				}
			}
			if (!hasInitializedLocalNames && (locals != null || decLocals != null)) {
				hasInitializedLocalNames = true;
				foreach (var vm in rootNode.Children.OfType<NormalValueVM>()) {
					var vt = vm.NormalValueType as LocalValueType;
					if (vt == null)
						continue;
					var l = locals == null || (uint)vt.Index >= (uint)locals.Length ? null : locals[vt.Index];
					var dl = decLocals == null || (uint)vt.Index >= (uint)decLocals.Length ? null : decLocals[vt.Index];
					string name = dl == null ? null : dl.Name;
					if (name == null && l != null && !string.IsNullOrEmpty(l.Name))
						name = string.Format("[{0}]", l.Name);
					if (string.IsNullOrEmpty(name))
						continue;

					vt.InitializeName(name);
				}
			}
		}

		bool CanReuseChildren(ICorValueHolder ex, int numArgs, int numLocals, int numGenArgs) {
			var children = rootNode.Children;

			int index = 0;
			if (index < children.Count && NormalValueVM.IsType<ExceptionValueType>(children[index]))
				index++;

			if (index + numArgs + numLocals > children.Count)
				return false;
			for (int i = 0; i < numArgs; i++, index++) {
				if (!NormalValueVM.IsType<ArgumentValueType>(children[index]))
					return false;
			}
			for (int i = 0; i < numLocals; i++, index++) {
				if (!NormalValueVM.IsType<LocalValueType>(children[index]))
					return false;
			}

			if (numGenArgs != 0) {
				if (index >= children.Count)
					return false;
				if (!(children[index++] is TypeVariablesValueVM))
					return false;
			}

			return index == children.Count;
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

		void RefreshToStringFields() {
			foreach (var vm in GetValueVMs())
				vm.RefreshToStringFields();
		}

		void ILocalsOwner.Refresh(NormalValueVM vm) {
			if (dispatcher.HasShutdownFinished || dispatcher.HasShutdownStarted)
				return;
			if (callingReread)
				return;
			callingReread = true;
			dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => {
				callingReread = false;
				InitializeLocals(LocalInitType.Full);
			}));
		}
		bool callingReread = false;

		bool ILocalsOwner.AskUser(string msg) {
			Debug.Assert(askUser != null);
			if (askUser == null)
				throw new InvalidOperationException();
			return askUser.AskUser(msg, AskUserButton.YesNo) == MsgBoxButton.OK;
		}

		DnEval ILocalsOwner.CreateEval(ValueContext context) {
			Debug.Assert(context != null && context.Thread != null);
			if (context == null || context.Thread == null)
				return null;
			if (!DebuggerSettings.Instance.CanEvaluateToString)
				return null;
			if (!DebugManager.Instance.CanEvaluate)
				return null;
			if (DebugManager.Instance.EvalDisabled)
				return null;

			return DebugManager.Instance.CreateEval(context.Thread.CorThread);
		}

		sealed class LocArgCorValueHolder : ICorValueHolder {
			public CorValue CorValue {
				get {
					if (value == null || IsNeutered) {
						InvalidateCorValue();
						value = GetNewCorValue();
					}
					return value;
				}
			}
			CorValue value;

			public bool IsNeutered {
				get { return false; }
			}

			readonly bool isArg;
			readonly LocalsVM locals;
			readonly int index;

			public LocArgCorValueHolder(bool isArg, LocalsVM locals, CorValue value, int index) {
				this.isArg = isArg;
				this.locals = locals;
				this.value = value;
				this.index = index;
			}

			public void InvalidateCorValue() {
				DebugManager.Instance.DisposeHandle(value);
				value = null;
			}

			CorValue GetNewCorValue() {
				if (locals.frameInfo == null)
					return null;
				var frame = locals.frameInfo.ValueContext.FrameCouldBeNeutered;
				if (frame == null)
					return null;
				var newValue = isArg ? frame.GetILArgument((uint)index) : frame.GetILLocal((uint)index);
				Debug.Assert(newValue != null && !newValue.IsNeutered);
				return newValue;
			}

			public void Dispose() {
				InvalidateCorValue();
			}
		}
	}
}
