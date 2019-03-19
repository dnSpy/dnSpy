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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.Evaluation.UI;
using dnSpy.Debugger.Evaluation.ViewModel;
using dnSpy.Debugger.Properties;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.ToolWindows.Watch {
	abstract class WatchVariablesWindowValueNodesProviderService {
		public abstract WatchVariablesWindowValueNodesProvider Get(int windowIndex);
	}

	[Export(typeof(WatchVariablesWindowValueNodesProviderService))]
	sealed class WatchVariablesWindowValueNodesProviderServiceImpl : WatchVariablesWindowValueNodesProviderService {
		readonly UIDispatcher uiDispatcher;
		readonly Lazy<DbgObjectIdService> dbgObjectIdService;
		readonly Lazy<WatchWindowExpressionsSettings> watchWindowExpressionsSettings;
		readonly WatchVariablesWindowValueNodesProvider[] providers;

		[ImportingConstructor]
		WatchVariablesWindowValueNodesProviderServiceImpl(UIDispatcher uiDispatcher, Lazy<DbgObjectIdService> dbgObjectIdService, Lazy<WatchWindowExpressionsSettings> watchWindowExpressionsSettings) {
			this.uiDispatcher = uiDispatcher;
			this.dbgObjectIdService = dbgObjectIdService;
			this.watchWindowExpressionsSettings = watchWindowExpressionsSettings;
			providers = new WatchVariablesWindowValueNodesProvider[WatchWindowsHelper.NUMBER_OF_WATCH_WINDOWS];
		}

		public override WatchVariablesWindowValueNodesProvider Get(int windowIndex) {
			uiDispatcher.VerifyAccess();
			if ((uint)windowIndex >= (uint)providers.Length)
				throw new ArgumentOutOfRangeException(nameof(windowIndex));
			var provider = providers[windowIndex];
			if (provider == null) {
				var savedExpressions = watchWindowExpressionsSettings.Value.GetExpressions(windowIndex);
				provider = new WatchVariablesWindowValueNodesProviderImpl(dbgObjectIdService.Value, savedExpressions, expressions => watchWindowExpressionsSettings.Value.SetExpressions(windowIndex, expressions));
				providers[windowIndex] = provider;
			}
			return provider;
		}
	}

	abstract class WatchVariablesWindowValueNodesProvider : VariablesWindowValueNodesProvider {
		public sealed override bool CanAddRemoveExpressions => true;
		public abstract override void DeleteExpressions(string[] ids);
		public abstract override void ClearAllExpressions();
		public abstract override void EditExpression(string id, string expression);
		public abstract override void AddExpressions(string[] expressions);
	}

	sealed class WatchVariablesWindowValueNodesProviderImpl : WatchVariablesWindowValueNodesProvider {
		public override event EventHandler NodesChanged;
		readonly DbgObjectIdService dbgObjectIdService;
		readonly List<ExpressionInfo> expressions;
		readonly Action<string[]> saveExpressions;
		uint nextId;

		sealed class ExpressionInfo {
			public string Id { get; }
			public string Expression { get; set; }
			public bool ForceEval { get; set; }
			public object ExpressionEvaluatorState { get; set; }

			public ExpressionInfo(string id, string expression, bool forceEval) {
				Id = id ?? throw new ArgumentNullException(nameof(id));
				Expression = expression ?? throw new ArgumentNullException(nameof(expression));
				ForceEval = forceEval;
			}
		}

		public WatchVariablesWindowValueNodesProviderImpl(DbgObjectIdService dbgObjectIdService, string[] savedExpressions, Action<string[]> saveExpressions) {
			this.dbgObjectIdService = dbgObjectIdService ?? throw new ArgumentNullException(nameof(dbgObjectIdService));
			expressions = new List<ExpressionInfo>(savedExpressions.Select(a => new ExpressionInfo(GetNextId(), a, forceEval: false)));
			this.saveExpressions = saveExpressions ?? throw new ArgumentNullException(nameof(saveExpressions));
		}

		public override void Initialize(bool enable) {
			ClearEEState();
			if (enable)
				dbgObjectIdService.ObjectIdsChanged += DbgObjectIdService_ObjectIdsChanged;
			else
				dbgObjectIdService.ObjectIdsChanged -= DbgObjectIdService_ObjectIdsChanged;
		}

		void DbgObjectIdService_ObjectIdsChanged(object sender, EventArgs e) => NodesChanged?.Invoke(this, EventArgs.Empty);

		public override void OnIsDebuggingChanged(bool isDebugging) => ClearEEState();

		void ClearEEState() {
			foreach (var info in expressions)
				info.ExpressionEvaluatorState = null;
		}

		// The returned id is unique and also sortable (unless it overflows...)
		string GetNextId() => nextId++.ToString("X8");

		public override DbgValueNodeInfo[] GetDefaultNodes() {
			var res = new DbgValueNodeInfo[expressions.Count];
			for (int i = 0; i < res.Length; i++) {
				var info = expressions[i];
				res[i] = new DbgValueNodeInfo(info.Id, info.Expression, dnSpy_Debugger_Resources.ErrorEvaluatingExpression, causesSideEffects: true);
			}
			return res;
		}

		public override ValueNodesProviderResult GetNodes(DbgEvaluationInfo evalInfo, DbgLanguage language, DbgEvaluationOptions evalOptions, DbgValueNodeEvaluationOptions nodeEvalOptions, DbgValueFormatterOptions nameFormatterOptions) {
			if (expressions.Count == 0)
				return new ValueNodesProviderResult(Array.Empty<DbgValueNodeInfo>(), false);

			var infos = new DbgExpressionEvaluationInfo[expressions.Count];
			Debug.Assert((evalOptions & DbgEvaluationOptions.NoSideEffects) == 0);
			for (int i = 0; i < infos.Length; i++) {
				var info = expressions[i];
				// Root nodes in watch window can always func-eval
				var realEvalOptions = evalOptions & ~DbgEvaluationOptions.NoFuncEval;
				var realNodeEvalOptions = nodeEvalOptions & ~DbgValueNodeEvaluationOptions.NoFuncEval;
				if (info.ForceEval)
					realEvalOptions &= ~DbgEvaluationOptions.NoSideEffects;
				else
					realEvalOptions |= DbgEvaluationOptions.NoSideEffects;
				Debug.Assert(((realEvalOptions & DbgEvaluationOptions.NoFuncEval) != 0) == ((realNodeEvalOptions & DbgValueNodeEvaluationOptions.NoFuncEval) != 0));
				info.ForceEval = false;
				if (info.ExpressionEvaluatorState == null)
					info.ExpressionEvaluatorState = language.ExpressionEvaluator.CreateExpressionEvaluatorState();
				infos[i] = new DbgExpressionEvaluationInfo(info.Expression, realNodeEvalOptions, realEvalOptions, info.ExpressionEvaluatorState);
			}

			var compRes = language.ValueNodeFactory.Create(evalInfo, infos);
			Debug.Assert(compRes.Length == infos.Length);

			var res = new DbgValueNodeInfo[compRes.Length];
			for (int i = 0; i < res.Length; i++) {
				var info = compRes[i];
				if (info.ValueNode.Expression != expressions[i].Expression)
					throw new InvalidOperationException();
				res[i] = new DbgValueNodeInfo(info.ValueNode, expressions[i].Id, info.CausesSideEffects);
			}
			return new ValueNodesProviderResult(res, false);
		}

		public override void DeleteExpressions(string[] ids) {
			int lastIndex = expressions.Count - 1;
			// IDs are also sortable
			foreach (var id in ids.OrderByDescending(a => a, StringComparer.Ordinal)) {
				var (index, info) = FindPrevExpression(lastIndex, id);
				Debug.Assert(info != null);
				Debug.Assert((index < 0) == (info == null));
				if (index < 0)
					continue;
				lastIndex = index;
				expressions.RemoveAt(index);
			}
			OnExpressionsChanged();
		}

		(int index, ExpressionInfo info) FindPrevExpression(int index, string id) {
			if (index >= expressions.Count)
				index = expressions.Count - 1;
			for (; index >= 0; index--) {
				var info = expressions[index];
				if (info.Id == id)
					return (index, info);
			}
			return (-1, null);
		}

		public override void ClearAllExpressions() {
			expressions.Clear();
			OnExpressionsChanged();
		}

		public override void EditExpression(string id, string expression) {
			var info = expressions.First(a => a.Id == id);
			info.Expression = expression;
			info.ForceEval = true;
			OnExpressionsChanged();
		}

		public override void AddExpressions(string[] expressions) {
			for (int i = 0; i < expressions.Length; i++) {
				var expr = expressions[i].Trim();
				if (expr.Length != 0)
					this.expressions.Add(new ExpressionInfo(GetNextId(), expr, forceEval: true));
			}
			OnExpressionsChanged();
		}

		void OnExpressionsChanged() => saveExpressions(expressions.Select(a => a.Expression).ToArray());
	}
}
