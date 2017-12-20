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
using System.IO;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Engine.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Roslyn.Shared.Properties;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	sealed class ResultsViewMembersValueNodeProvider : MembersValueNodeProvider {
		public override string ImageName => PredefinedDbgValueNodeImageNames.ResultsView;
		public override DbgDotNetText ValueText => valueText;

		static readonly DbgDotNetText valueText = new DbgDotNetText(new DbgDotNetTextPart(BoxedTextColor.Text, dnSpy_Roslyn_Shared_Resources.DebuggerVarsWindow_ExpandResultsViewMessage));
		static readonly DbgDotNetText resultsViewName = new DbgDotNetText(new DbgDotNetTextPart(BoxedTextColor.Text, dnSpy_Roslyn_Shared_Resources.DebuggerVarsWindow_ResultsView));

		readonly DbgDotNetValueNodeProviderFactory valueNodeProviderFactory;
		readonly DmdType enumerableType;
		readonly DbgDotNetValue instanceValue;
		readonly string valueExpression;
		string resultsViewProxyExpression;
		DbgDotNetValue getResultsViewValue;

		public ResultsViewMembersValueNodeProvider(DbgDotNetValueNodeProviderFactory valueNodeProviderFactory, LanguageValueNodeFactory valueNodeFactory, DmdType enumerableType, DbgDotNetValue instanceValue, string valueExpression, DbgValueNodeEvaluationOptions evalOptions)
			: base(valueNodeFactory, resultsViewName, valueExpression + ", results", default, evalOptions) {
			this.valueNodeProviderFactory = valueNodeProviderFactory;
			this.enumerableType = enumerableType;
			this.instanceValue = instanceValue;
			this.valueExpression = valueExpression;
			resultsViewProxyExpression = string.Empty;
		}

		sealed class ForceLoadAssemblyState {
			public volatile int Counter;
		}

		protected override string InitializeCore(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			if ((evalOptions & DbgValueNodeEvaluationOptions.NoFuncEval) != 0)
				return PredefinedEvaluationErrorMessages.FuncEvalDisabled;

			var proxyCtor = EnumerableDebugViewHelper.GetEnumerableDebugViewConstructor(enumerableType);
			if ((object)proxyCtor == null) {
				var loadState = enumerableType.AppDomain.GetOrCreateData<ForceLoadAssemblyState>();
				if (Interlocked.Exchange(ref loadState.Counter, 1) == 0) {
					var loader = new ReflectionAssemblyLoader(context, frame, enumerableType.AppDomain, cancellationToken);
					if (loader.TryLoadAssembly(GetRequiredAssemblyFullName(context.Runtime)))
						proxyCtor = EnumerableDebugViewHelper.GetEnumerableDebugViewConstructor(enumerableType);
				}
				if ((object)proxyCtor == null) {
					var asmFilename = GetRequiredAssemblyFilename(context.Runtime);
					var asm = enumerableType.AppDomain.GetAssembly(Path.GetFileNameWithoutExtension(asmFilename));
					if (asm == null)
						return string.Format(dnSpy_Roslyn_Shared_Resources.SystemCoreDllNotLoaded, asmFilename);
					return string.Format(dnSpy_Roslyn_Shared_Resources.TypeDoesNotExistInAssembly, EnumerableDebugViewHelper.GetDebugViewTypeDisplayName(enumerableType), asmFilename);
				}
			}

			var runtime = context.Runtime.GetDotNetRuntime();
			var proxyTypeResult = runtime.CreateInstance(context, frame, proxyCtor, new[] { instanceValue }, DbgDotNetInvokeOptions.None, cancellationToken);
			if (proxyTypeResult.HasError)
				return proxyTypeResult.ErrorMessage;

			resultsViewProxyExpression = valueNodeProviderFactory.GetNewObjectExpression(proxyCtor, valueExpression);
			getResultsViewValue = proxyTypeResult.Value;
			valueNodeProviderFactory.GetMemberCollections(getResultsViewValue.Type, evalOptions, out membersCollection, out _);
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
			if (enumerableType.AppDomain.CorLib.GetName().Version == new Version(2, 0, 0, 0))
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

		protected override (DbgDotNetValueNode node, bool canHide) CreateValueNode(DbgEvaluationContext context, DbgStackFrame frame, int index, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) =>
			CreateValueNode(context, frame, false, getResultsViewValue.Type, getResultsViewValue, index, options, resultsViewProxyExpression, cancellationToken);

		protected override (DbgDotNetValueNode node, bool canHide) TryCreateInstanceValueNode(DbgEvaluationContext context, DbgStackFrame frame, DbgDotNetValueResult valueResult, CancellationToken cancellationToken) {
			var noResultsNode = DebugViewNoResultsValueNode.TryCreate(context, frame, Expression, valueResult, cancellationToken);
			if (noResultsNode != null) {
				valueResult.Value?.Dispose();
				return (noResultsNode, false);
			}
			return (null, false);
		}

		protected override void DisposeCore() => getResultsViewValue?.Dispose();
	}
}
