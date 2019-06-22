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
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Roslyn.Properties;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	sealed class ResultsViewMembersValueNodeProvider : MembersValueNodeProvider {
		public override string ImageName => PredefinedDbgValueNodeImageNames.ResultsView;
		public override DbgDotNetText ValueText => valueText;

		static readonly DbgDotNetText valueText = new DbgDotNetText(new DbgDotNetTextPart(DbgTextColor.Text, dnSpy_Roslyn_Resources.DebuggerVarsWindow_ExpandResultsViewMessage));
		static readonly DbgDotNetText resultsViewName = new DbgDotNetText(new DbgDotNetTextPart(DbgTextColor.Text, dnSpy_Roslyn_Resources.DebuggerVarsWindow_ResultsView));

		readonly DbgDotNetValueNodeProviderFactory valueNodeProviderFactory;
		readonly DmdType enumerableType;
		readonly DbgDotNetValue instanceValue;
		readonly DmdType expectedType;
		readonly string valueExpression;
		string resultsViewProxyExpression;
		DbgDotNetValue? getResultsViewValue;

		public ResultsViewMembersValueNodeProvider(DbgDotNetValueNodeProviderFactory valueNodeProviderFactory, LanguageValueNodeFactory valueNodeFactory, DmdType enumerableType, DbgDotNetValue instanceValue, DmdType expectedType, string valueExpression, DbgValueNodeEvaluationOptions evalOptions)
			: base(valueNodeFactory, resultsViewName, valueExpression + ", " + PredefinedFormatSpecifiers.ResultsView, default, evalOptions) {
			this.valueNodeProviderFactory = valueNodeProviderFactory;
			this.enumerableType = enumerableType;
			this.instanceValue = instanceValue;
			this.expectedType = expectedType;
			this.valueExpression = valueExpression;
			resultsViewProxyExpression = string.Empty;
		}

		sealed class ForceLoadAssemblyState {
			public volatile int Counter;
		}

		protected override string? InitializeCore(DbgEvaluationInfo evalInfo) {
			if ((evalOptions & DbgValueNodeEvaluationOptions.NoFuncEval) != 0)
				return PredefinedEvaluationErrorMessages.FuncEvalDisabled;

			var errorMessage = InitializeEnumerableDebugView(evalInfo);
			if (!(errorMessage is null)) {
				if (InitializeListDebugView(evalInfo))
					errorMessage = null;
			}

			return errorMessage;
		}

		bool InitializeListDebugView(DbgEvaluationInfo evalInfo) {
			var info = EnumerableDebugViewHelper.GetListEnumerableMethods(instanceValue.Type, enumerableType);
			if (info.ctor is null)
				return false;

			DbgDotNetValueResult collTypeResult = default;
			DbgDotNetValueResult toArrayResult = default;
			bool error = true;
			try {
				var runtime = evalInfo.Runtime.GetDotNetRuntime();

				collTypeResult = runtime.CreateInstance(evalInfo, info.ctor, new[] { instanceValue }, DbgDotNetInvokeOptions.None);
				if (!collTypeResult.IsNormalResult)
					return false;
				var expr = valueNodeProviderFactory.GetNewObjectExpression(info.ctor, valueExpression, expectedType);

				toArrayResult = runtime.Call(evalInfo, collTypeResult.Value, info.toArrayMethod, Array.Empty<object>(), DbgDotNetInvokeOptions.None);
				if (toArrayResult.HasError)
					return false;
				expr = valueNodeProviderFactory.GetCallExpression(info.toArrayMethod, expr);

				var result = valueNodeProviderFactory.Create(evalInfo, false, toArrayResult.Value!.Type, new DbgDotNetValueNodeInfo(toArrayResult.Value, expr), evalOptions);
				if (result.Provider is null)
					return false;
				realProvider = result.Provider;
				error = false;
				return true;
			}
			finally {
				collTypeResult.Value?.Dispose();
				if (error)
					toArrayResult.Value?.Dispose();
			}
		}

		string? InitializeEnumerableDebugView(DbgEvaluationInfo evalInfo) {
			var proxyCtor = EnumerableDebugViewHelper.GetEnumerableDebugViewConstructor(enumerableType);
			if (proxyCtor is null) {
				var loadState = enumerableType.AppDomain.GetOrCreateData<ForceLoadAssemblyState>();
				if (Interlocked.Exchange(ref loadState.Counter, 1) == 0) {
					var loader = new ReflectionAssemblyLoader(evalInfo, enumerableType.AppDomain);
					if (loader.TryLoadAssembly(GetRequiredAssemblyFullName(evalInfo.Runtime)))
						proxyCtor = EnumerableDebugViewHelper.GetEnumerableDebugViewConstructor(enumerableType);
				}
				if (proxyCtor is null) {
					var asmFilename = GetRequiredAssemblyFilename(evalInfo.Runtime);
					var asm = enumerableType.AppDomain.GetAssembly(Path.GetFileNameWithoutExtension(asmFilename));
					if (asm is null)
						return string.Format(dnSpy_Roslyn_Resources.SystemCoreDllNotLoaded, asmFilename);
					return string.Format(dnSpy_Roslyn_Resources.TypeDoesNotExistInAssembly, EnumerableDebugViewHelper.GetDebugViewTypeDisplayName(enumerableType), asmFilename);
				}
			}

			var runtime = evalInfo.Runtime.GetDotNetRuntime();
			var proxyTypeResult = runtime.CreateInstance(evalInfo, proxyCtor, new[] { instanceValue }, DbgDotNetInvokeOptions.None);
			if (proxyTypeResult.HasError)
				return proxyTypeResult.ErrorMessage;

			resultsViewProxyExpression = valueNodeProviderFactory.GetNewObjectExpression(proxyCtor, valueExpression, expectedType);
			getResultsViewValue = proxyTypeResult.Value;
			valueNodeProviderFactory.GetMemberCollections(getResultsViewValue!.Type, evalOptions, out membersCollection, out _);
			return null;
		}

		enum ClrVersion {
			CLR2,
			CLR4,
			CoreCLR,
		}

		ClrVersion GetClrVersion(DbgRuntime runtime) {
			if (runtime.Guid == PredefinedDbgRuntimeGuids.DotNetCore_Guid)
				return ClrVersion.CoreCLR;
			if (enumerableType.AppDomain.CorLib?.GetName().Version == new Version(2, 0, 0, 0))
				return ClrVersion.CLR2;
			return ClrVersion.CLR4;
		}

		string GetRequiredAssemblyFullName(DbgRuntime runtime) {
			switch (GetClrVersion(runtime)) {
			case ClrVersion.CLR2:		return "System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
			case ClrVersion.CLR4:		return "System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
			case ClrVersion.CoreCLR:	return "System.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
			default:					throw new InvalidOperationException();
			}
		}

		string GetRequiredAssemblyFilename(DbgRuntime runtime) {
			switch (GetClrVersion(runtime)) {
			case ClrVersion.CLR2:
			case ClrVersion.CLR4:		return "System.Core.dll";
			case ClrVersion.CoreCLR:	return "System.Linq.dll";
			default:					throw new InvalidOperationException();
			}
		}

		protected override (DbgDotNetValueNode node, bool canHide) CreateValueNode(DbgEvaluationInfo evalInfo, int index, DbgValueNodeEvaluationOptions options, ReadOnlyCollection<string>? formatSpecifiers) =>
			CreateValueNode(evalInfo, false, getResultsViewValue!.Type, getResultsViewValue, index, options, resultsViewProxyExpression, formatSpecifiers);

		protected override (DbgDotNetValueNode? node, bool canHide) TryCreateInstanceValueNode(DbgEvaluationInfo evalInfo, DbgDotNetValueResult valueResult) {
			var noResultsNode = DebugViewNoResultsValueNode.TryCreate(evalInfo, Expression, valueResult);
			if (!(noResultsNode is null)) {
				valueResult.Value?.Dispose();
				return (noResultsNode, false);
			}
			return (null, false);
		}

		protected override void DisposeCore() => getResultsViewValue?.Dispose();
	}
}
