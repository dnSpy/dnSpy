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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;
using dndbg.Engine;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Images;
using dnSpy.Debugger.CallStack;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.MVVM;
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

	interface ILocalsVM {
		bool IsEnabled { get; set; }
		bool IsVisible { get; set; }
		void RefreshThemeFields();
	}

	[Export, Export(typeof(ILocalsVM)), Export(typeof(ILoadBeforeDebug)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class LocalsVM : ViewModelBase, ILocalsOwner, ILocalsVM, ILoadBeforeDebug {
		public bool IsEnabled {
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

		public bool IsVisible {//TODO: Use this
			get { return isVisible; }
			set { isVisible = value; }
		}
		bool isVisible;

		public SharpTreeNode Root {
			get { return rootNode; }
		}
		readonly SharpTreeNode rootNode;

		public IPrinterContext PrinterContext {
			get { return printerContext; }
		}
		readonly PrinterContext printerContext;

		public bool DebuggerBrowsableAttributesCanHidePropsFields {
			get { return debuggerSettings.DebuggerBrowsableAttributesCanHidePropsFields; }
		}

		public bool CompilerGeneratedAttributesCanHideFields {
			get { return debuggerSettings.CompilerGeneratedAttributesCanHideFields; }
		}

		public bool PropertyEvalAndFunctionCalls {
			get { return debuggerSettings.PropertyEvalAndFunctionCalls; }
		}

		readonly IAskUser askUser;
		readonly IMethodLocalProvider methodLocalProvider;
		readonly Dispatcher dispatcher;
		readonly IDebuggerSettings debuggerSettings;
		readonly IStackFrameManager stackFrameManager;

		public ITheDebugger TheDebugger {
			get { return theDebugger; }
		}
		readonly ITheDebugger theDebugger;

		[ImportingConstructor]
		LocalsVM(IImageManager imageManager, IDebuggerSettings debuggerSettings, ILocalsSettings localsSettings, IMethodLocalProvider methodLocalProvider, IStackFrameManager stackFrameManager, ITheDebugger theDebugger, IAskUser askUser) {
			this.dispatcher = Dispatcher.CurrentDispatcher;
			this.askUser = askUser;
			this.methodLocalProvider = methodLocalProvider;
			this.debuggerSettings = debuggerSettings;
			this.stackFrameManager = stackFrameManager;
			this.theDebugger = theDebugger;

			this.printerContext = new PrinterContext(imageManager) {
				SyntaxHighlight = debuggerSettings.SyntaxHighlightLocals,
				UseHexadecimal = debuggerSettings.UseHexadecimal,
				TypePrinterFlags = TypePrinterFlags.ShowArrayValueSizes,
			};
			this.printerContext.TypePrinterFlags = GetTypePrinterFlags(localsSettings, this.printerContext.TypePrinterFlags);
			this.printerContext.TypePrinterFlags = GetTypePrinterFlags(debuggerSettings, this.printerContext.TypePrinterFlags);

			methodLocalProvider.NewMethodInfoAvailable += MethodLocalProvider_NewMethodInfoAvailable;
			this.rootNode = new SharpTreeNode();
			stackFrameManager.StackFramesUpdated += StackFrameManager_StackFramesUpdated;
			stackFrameManager.PropertyChanged += StackFrameManager_PropertyChanged;
			theDebugger.OnProcessStateChanged += TheDebugger_OnProcessStateChanged;
			theDebugger.ProcessRunning += TheDebugger_ProcessRunning;
			debuggerSettings.PropertyChanged += DebuggerSettings_PropertyChanged;
			localsSettings.PropertyChanged += LocalsSettings_PropertyChanged;
		}

		static void Update(bool b, TypePrinterFlags f, ref TypePrinterFlags flags) {
			if (b)
				flags |= f;
			else
				flags &= ~f;
		}

		TypePrinterFlags GetTypePrinterFlags(ILocalsSettings localsSettings, TypePrinterFlags flags) {
			Update(localsSettings.ShowNamespaces, TypePrinterFlags.ShowNamespaces, ref flags);
			Update(localsSettings.ShowTokens, TypePrinterFlags.ShowTokens, ref flags);
			Update(localsSettings.ShowTypeKeywords, TypePrinterFlags.ShowTypeKeywords, ref flags);
			return flags;
		}

		TypePrinterFlags GetTypePrinterFlags(IDebuggerSettings debuggerSettings, TypePrinterFlags flags) {
			Update(!debuggerSettings.UseHexadecimal, TypePrinterFlags.UseDecimal, ref flags);
			return flags;
		}

		void TheDebugger_ProcessRunning(object sender, EventArgs e) {
			InitializeLocals(LocalInitType.Full);
		}

		void MethodLocalProvider_NewMethodInfoAvailable(object sender, EventArgs e) {
			InitializeLocalAndArgNames();
		}

		void LocalsSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var localsSettings = (ILocalsSettings)sender;
			switch (e.PropertyName) {
			case "ShowNamespaces":
			case "ShowTypeKeywords":
			case "ShowTokens":
				printerContext.TypePrinterFlags = GetTypePrinterFlags(localsSettings, printerContext.TypePrinterFlags);
				RefreshTypeFields();
				break;
			}
		}

		void DebuggerSettings_PropertyChanged(object sender, PropertyChangedEventArgs e) {
			var debuggerSettings = (IDebuggerSettings)sender;
			switch (e.PropertyName) {
			case "UseHexadecimal":
				printerContext.UseHexadecimal = debuggerSettings.UseHexadecimal;
				printerContext.TypePrinterFlags = GetTypePrinterFlags(debuggerSettings, printerContext.TypePrinterFlags);
				RefreshHexFields();
				break;
			case "SyntaxHighlightLocals":
				printerContext.SyntaxHighlight = debuggerSettings.SyntaxHighlightLocals;
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

		void TheDebugger_OnProcessStateChanged(object sender, DebuggerEventArgs e) {
			switch (theDebugger.ProcessState) {
			case DebuggerProcessState.Starting:
				frameInfo = null;
				break;

			case DebuggerProcessState.Continuing:
			case DebuggerProcessState.Running:
				break;

			case DebuggerProcessState.Paused:
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

			public SerializedDnToken? Key {
				get {
					if (ValueContext.Function == null)
						return null;
					var mod = ValueContext.Function.Module;
					if (mod == null)
						return null;
					return new SerializedDnToken(mod.SerializedDnModule, ValueContext.Function.Token);
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
			if (theDebugger.ProcessState != DebuggerProcessState.Continuing && theDebugger.ProcessState != DebuggerProcessState.Running)
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
			if (!IsEnabled || theDebugger.ProcessState != DebuggerProcessState.Paused) {
				ClearAllLocals();
				return;
			}

			if (initType == LocalInitType.Simple) {
				// Property eval has completed, don't do a thing
				return;
			}

			var thread = stackFrameManager.SelectedThread;
			var frame = stackFrameManager.SelectedFrame;
			int frameNo = stackFrameManager.SelectedFrameNumber;
			DnProcess process;
			if (thread == null) {
				process = theDebugger.Debugger.Processes.FirstOrDefault();
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
				args.Add(new LocArgCorValueHolder(theDebugger, true, this, corArgs[i], i));
			for (int i = 0; i < corLocals.Length; i++)
				locals.Add(new LocArgCorValueHolder(theDebugger, false, this, corLocals[i], i));

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
			IILVariable[] decLocals;
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

		public void RefreshThemeFields() {
			foreach (var vm in GetValueVMs())
				vm.RefreshThemeFields();
		}

		void RefreshSyntaxHighlightFields() {
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
			return askUser.AskUser(msg, AskUserButton.YesNo) == MsgBoxButton.Yes;
		}

		DnEval ILocalsOwner.CreateEval(ValueContext context) {
			Debug.Assert(context != null && context.Thread != null);
			if (context == null || context.Thread == null)
				return null;
			if (!debuggerSettings.CanEvaluateToString)
				return null;
			if (!theDebugger.CanEvaluate)
				return null;
			if (theDebugger.EvalDisabled)
				return null;

			return theDebugger.CreateEval(context.Thread.CorThread);
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
			readonly ITheDebugger theDebugger;

			public LocArgCorValueHolder(ITheDebugger theDebugger, bool isArg, LocalsVM locals, CorValue value, int index) {
				this.theDebugger = theDebugger;
				this.isArg = isArg;
				this.locals = locals;
				this.value = value;
				this.index = index;
			}

			public void InvalidateCorValue() {
				theDebugger.DisposeHandle(value);
				value = null;
			}

			CorValue GetNewCorValue() {
				if (!theDebugger.IsDebugging)
					return null;
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
