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
		DbgDotNetValue getResultsViewValue;

		public ResultsViewMembersValueNodeProvider(DbgDotNetValueNodeProviderFactory valueNodeProviderFactory, LanguageValueNodeFactory valueNodeFactory, DmdType enumerableType, DbgDotNetValue instanceValue, string expression, DbgValueNodeEvaluationOptions evalOptions)
			: base(valueNodeFactory, resultsViewName, expression + ", results", default, evalOptions) {
			this.valueNodeProviderFactory = valueNodeProviderFactory;
			this.enumerableType = enumerableType;
			this.instanceValue = instanceValue;
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
				if (Interlocked.Increment(ref loadState.Counter) == 1) {
					//TODO: Try to force load the assembly
					//		.NET Framework:
					//			System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
					//			System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
					//		.NET Core:
					//			v1: System.Linq, Version=4.1.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
					//			v2: System.Linq, Version=4.2.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
				}
				return string.Format(dnSpy_Roslyn_Shared_Resources.SystemCoreDllNotLoaded, GetRequiredAssemblyFileName(enumerableType.AppDomain));
			}

			var runtime = context.Runtime.GetDotNetRuntime();
			var proxyTypeResult = runtime.CreateInstance(context, frame, proxyCtor, new[] { instanceValue }, cancellationToken);
			if (proxyTypeResult.HasError)
				return proxyTypeResult.ErrorMessage;

			getResultsViewValue = proxyTypeResult.Value;
			valueNodeProviderFactory.GetMemberCollections(getResultsViewValue.Type, evalOptions, out membersCollection, out _);
			return null;
		}

		static string GetRequiredAssemblyFileName(DmdAppDomain appDomain) {
			// Check if it's .NET Core
			if (StringComparer.OrdinalIgnoreCase.Equals(appDomain.CorLib.GetName().Name, "System.Private.CoreLib"))
				return "System.Linq.dll";
			return "System.Core.dll";
		}

		protected override (DbgDotNetValueNode node, bool canHide) CreateValueNode(DbgEvaluationContext context, DbgStackFrame frame, int index, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) =>
			CreateValueNode(context, frame, getResultsViewValue, index, options, cancellationToken);

		protected override (DbgDotNetValueNode node, bool canHide) TryCreateInstanceValueNode(DbgDotNetValueResult valueResult) {
			if (!valueResult.ValueIsException)
				return (null, false);
			if (valueResult.Value.Type != valueResult.Value.Type.AppDomain.GetWellKnownType(DmdWellKnownType.System_Linq_SystemCore_EnumerableDebugViewEmptyException, isOptional: true))
				return (null, false);
			valueResult.Value?.Dispose();
			return (new ResultsViewNoResultsValueNode(Expression), false);
		}

		protected override void DisposeCore() => getResultsViewValue?.Dispose();
	}
}
