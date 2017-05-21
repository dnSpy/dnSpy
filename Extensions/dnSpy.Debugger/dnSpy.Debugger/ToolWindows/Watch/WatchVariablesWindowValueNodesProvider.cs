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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger.CallStack;
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
		readonly Lazy<WatchWindowExpressionsSettings> watchWindowExpressionsSettings;
		readonly WatchVariablesWindowValueNodesProvider[] providers;

		[ImportingConstructor]
		WatchVariablesWindowValueNodesProviderServiceImpl(UIDispatcher uiDispatcher, Lazy<WatchWindowExpressionsSettings> watchWindowExpressionsSettings) {
			this.uiDispatcher = uiDispatcher;
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
				provider = new WatchVariablesWindowValueNodesProviderImpl(savedExpressions, expressions => watchWindowExpressionsSettings.Value.SetExpressions(windowIndex, expressions));
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
		readonly List<ExpressionInfo> expressions;
		readonly Action<string[]> saveExpressions;
		uint nextId;

		sealed class ExpressionInfo {
			public string Id { get; }
			public string Expression { get; set; }
			public bool ForceEval { get; set; }

			public ExpressionInfo(string id, string expression, bool forceEval) {
				Id = id ?? throw new ArgumentNullException(nameof(id));
				Expression = expression ?? throw new ArgumentNullException(nameof(expression));
				ForceEval = forceEval;
			}
		}

		public WatchVariablesWindowValueNodesProviderImpl(string[] savedExpressions, Action<string[]> saveExpressions) {
			expressions = new List<ExpressionInfo>(savedExpressions.Select(a => new ExpressionInfo(GetNextId(), a, forceEval: false)));
			this.saveExpressions = saveExpressions ?? throw new ArgumentNullException(nameof(saveExpressions));
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

		public override DbgValueNodeInfo[] GetNodes(DbgLanguage language, DbgStackFrame frame, DbgEvaluationOptions options) {
			var res = new DbgValueNodeInfo[expressions.Count];
			Debug.Assert((options & DbgEvaluationOptions.NoSideEffects) == 0);
			for (int i = 0; i < res.Length; i++) {
				var info = expressions[i];
				var evalOptions = options;
				if (info.ForceEval)
					evalOptions = options & ~DbgEvaluationOptions.NoSideEffects;
				else
					evalOptions = options | DbgEvaluationOptions.NoSideEffects;
				info.ForceEval = false;
				var result = language.ValueNodeFactory.Create(frame, info.Expression, evalOptions);
				if (result.Error is string error)
					res[i] = new DbgValueNodeInfo(info.Id, info.Expression, error, result.CausesSideEffects);
				else {
					if (result.ValueNode.Expression != info.Expression)
						throw new InvalidOperationException();
					res[i] = new DbgValueNodeInfo(result.ValueNode, info.Id);
				}
			}
			return res;
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
