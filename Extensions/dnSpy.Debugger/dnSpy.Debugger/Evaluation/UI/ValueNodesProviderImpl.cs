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
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.Evaluation.ViewModel;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.Evaluation.UI {
	sealed class ValueNodesProviderImpl : ValueNodesProvider {
		public override event EventHandler? NodesChanged;
		public override event EventHandler? IsReadOnlyChanged;
		public override bool IsReadOnly => isReadOnly;
		public override event EventHandler? LanguageChanged;
		public override DbgLanguage? Language => language;
		bool isReadOnly;
		bool isOpen;
		DbgLanguage? language;
		readonly EvalContextInfo evalContextInfo;

		sealed class EvalContextInfo {
			public DbgEvaluationInfo? EvalInfo {
				get => __evalInfo_DONT_USE;
				set {
					__evalInfo_DONT_USE?.Context.Close();
					__evalInfo_DONT_USE = value;
				}
			}
			DbgEvaluationInfo? __evalInfo_DONT_USE;

			public DbgLanguage? Language;

			public void Clear() {
				EvalInfo = null;
				Language = null;
			}
		}

		readonly VariablesWindowValueNodesProvider variablesWindowValueNodesProvider;
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<DbgManager> dbgManager;
		readonly Lazy<DbgLanguageService> dbgLanguageService;
		readonly Lazy<DbgCallStackService> dbgCallStackService;

		public ValueNodesProviderImpl(VariablesWindowValueNodesProvider variablesWindowValueNodesProvider, UIDispatcher uiDispatcher, Lazy<DbgManager> dbgManager, Lazy<DbgLanguageService> dbgLanguageService, Lazy<DbgCallStackService> dbgCallStackService) {
			this.variablesWindowValueNodesProvider = variablesWindowValueNodesProvider ?? throw new ArgumentNullException(nameof(variablesWindowValueNodesProvider));
			this.uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
			this.dbgManager = dbgManager ?? throw new ArgumentNullException(nameof(dbgManager));
			this.dbgLanguageService = dbgLanguageService ?? throw new ArgumentNullException(nameof(dbgLanguageService));
			this.dbgCallStackService = dbgCallStackService ?? throw new ArgumentNullException(nameof(dbgCallStackService));
			evalContextInfo = new EvalContextInfo();
		}

		void UI(Action callback) => uiDispatcher.UI(callback);

		void DbgThread(Action callback) =>
			dbgManager.Value.Dispatcher.BeginInvoke(callback);

		public void Initialize_UI(bool enable) {
			uiDispatcher.VerifyAccess();
			isOpen = enable;
			evalContextInfo.Clear();
			variablesWindowValueNodesProvider.Initialize(enable);
			if (enable)
				variablesWindowValueNodesProvider.NodesChanged += VariablesWindowValueNodesProvider_NodesChanged;
			else
				variablesWindowValueNodesProvider.NodesChanged -= VariablesWindowValueNodesProvider_NodesChanged;
			RefreshNodes_UI();
			DbgThread(() => InitializeDebugger_DbgThread(enable));
		}

		void InitializeDebugger_DbgThread(bool enable) {
			dbgManager.Value.Dispatcher.VerifyAccess();
			if (enable) {
				dbgLanguageService.Value.LanguageChanged += DbgLanguageService_LanguageChanged;
				dbgCallStackService.Value.FramesChanged += DbgCallStackService_FramesChanged;
				dbgManager.Value.IsDebuggingChanged += DbgManager_IsDebuggingChanged;
			}
			else {
				dbgLanguageService.Value.LanguageChanged -= DbgLanguageService_LanguageChanged;
				dbgCallStackService.Value.FramesChanged -= DbgCallStackService_FramesChanged;
				dbgManager.Value.IsDebuggingChanged -= DbgManager_IsDebuggingChanged;
			}
			CallOnIsDebuggingChanged(dbgManager.Value.IsDebugging);
		}

		void DbgManager_IsDebuggingChanged(object? sender, EventArgs e) => CallOnIsDebuggingChanged(dbgManager.Value.IsDebugging);

		void CallOnIsDebuggingChanged(bool isDebugging) => UI(() => variablesWindowValueNodesProvider.OnIsDebuggingChanged(isDebugging));

		void DbgLanguageService_LanguageChanged(object? sender, DbgLanguageChangedEventArgs e) {
			var thread = dbgManager.Value.CurrentThread.Current;
			if (thread is null || thread.Runtime.RuntimeKindGuid != e.RuntimeKindGuid)
				return;
			UI(() => RefreshNodes_UI());
		}

		void DbgCallStackService_FramesChanged(object? sender, FramesChangedEventArgs e) =>
			UI(() => RefreshNodes_UI());

		void VariablesWindowValueNodesProvider_NodesChanged(object? sender, EventArgs e) =>
			UI(() => RefreshNodes_UI());

		void RefreshNodes_UI() {
			uiDispatcher.VerifyAccess();
			var info = TryGetLanguage();
			if (info.language != language) {
				language = info.language;
				LanguageChanged?.Invoke(this, EventArgs.Empty);
			}
			bool newIsReadOnly = info.frame is null;
			NodesChanged?.Invoke(this, EventArgs.Empty);
			SetIsReadOnly_UI(newIsReadOnly);
		}

		(DbgLanguage? language, DbgStackFrame? frame) TryGetLanguage() {
			if (!isOpen)
				return (null, null);
			var frame = dbgCallStackService.Value.ActiveFrame;
			if (frame is null)
				return (null, null);
			var language = dbgLanguageService.Value.GetCurrentLanguage(frame.Runtime.RuntimeKindGuid);
			return (language, frame);
		}

		public override GetNodesResult GetNodes(DbgEvaluationOptions evalOptions, DbgValueNodeEvaluationOptions nodeEvalOptions, DbgValueFormatterOptions nameFormatterOptions) {
			uiDispatcher.VerifyAccess();
			var info = TryGetLanguage();
			if (info.frame is null)
				return new GetNodesResult(variablesWindowValueNodesProvider.GetDefaultNodes(), frameClosed: false, recreateAllNodes: false);
			Debug2.Assert(!(info.language is null));
			var evalInfo = TryGetEvaluationInfo(info);
			if (evalInfo is null)
				return new GetNodesResult(variablesWindowValueNodesProvider.GetDefaultNodes(), info.frame.IsClosed, recreateAllNodes: false);
			Debug.Assert(evalInfo.Frame == info.frame);
			try {
				var nodesInfo = variablesWindowValueNodesProvider.GetNodes(evalInfo, info.language, evalOptions, nodeEvalOptions, nameFormatterOptions);
				return new GetNodesResult(nodesInfo.Nodes, info.frame.IsClosed, nodesInfo.RecreateAllNodes);
			}
			catch {
				// A TaskCanceledException gets thrown if the runtime's Dispatcher has shut down.
				// If the runtime is closed, ignore the exception, we're not debugging it anymore.
				if (evalInfo.Runtime.IsClosed)
					return new GetNodesResult(variablesWindowValueNodesProvider.GetDefaultNodes(), info.frame.IsClosed, recreateAllNodes: false);
				throw;
			}
		}

		void SetIsReadOnly_UI(bool newIsReadOnly) {
			uiDispatcher.VerifyAccess();
			if (isReadOnly == newIsReadOnly)
				return;
			isReadOnly = newIsReadOnly;
			IsReadOnlyChanged?.Invoke(this, EventArgs.Empty);
		}

		public override bool CanAddRemoveExpressions => variablesWindowValueNodesProvider.CanAddRemoveExpressions;

		public override void DeleteExpressions(string[] ids) {
			if (!CanAddRemoveExpressions)
				throw new InvalidOperationException();
			variablesWindowValueNodesProvider.DeleteExpressions(ids);
		}

		public override void ClearAllExpressions() {
			if (!CanAddRemoveExpressions)
				throw new InvalidOperationException();
			variablesWindowValueNodesProvider.ClearAllExpressions();
		}

		public override void EditExpression(string? id, string expression) {
			if (!CanAddRemoveExpressions)
				throw new InvalidOperationException();
			variablesWindowValueNodesProvider.EditExpression(id, expression);
		}

		public override void AddExpressions(string[] expressions) {
			if (!CanAddRemoveExpressions)
				throw new InvalidOperationException();
			variablesWindowValueNodesProvider.AddExpressions(expressions);
		}

		public override DbgEvaluationInfo? TryGetEvaluationInfo() => TryGetEvaluationInfo(TryGetLanguage());

		DbgEvaluationInfo? TryGetEvaluationInfo((DbgLanguage? language, DbgStackFrame? frame) info) {
			if (!(evalContextInfo.EvalInfo is null) && evalContextInfo.Language == info.language && evalContextInfo.EvalInfo.Frame == info.frame)
				return evalContextInfo.EvalInfo;

			evalContextInfo.Language = info.language;
			if (!(info.frame is null)) {
				Debug2.Assert(!(info.language is null));
				//TODO: Show a cancel button if the decompiler takes too long to decompile the method
				var cancellationToken = CancellationToken.None;
				var context = info.language.CreateContext(info.frame, cancellationToken: cancellationToken);
				evalContextInfo.EvalInfo = new DbgEvaluationInfo(context, info.frame, cancellationToken);
			}
			else
				evalContextInfo.EvalInfo = null;
			return evalContextInfo.EvalInfo;
		}

		public override DbgStackFrame? TryGetFrame() => TryGetLanguage().frame;

		public override void RefreshAllNodes() {
			uiDispatcher.VerifyAccess();
			// Clear language so the context is recreated
			evalContextInfo.Language = null;
			RefreshNodes_UI();
		}
	}
}
