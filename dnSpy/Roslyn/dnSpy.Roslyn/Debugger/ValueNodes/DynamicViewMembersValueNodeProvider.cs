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
	sealed class DynamicViewMembersValueNodeProvider : MembersValueNodeProvider {
		public override string ImageName => PredefinedDbgValueNodeImageNames.DynamicView;
		public override DbgDotNetText ValueText => valueText;

		static readonly DbgDotNetText valueText = new DbgDotNetText(new DbgDotNetTextPart(DbgTextColor.Text, dnSpy_Roslyn_Resources.DebuggerVarsWindow_ExpandDynamicViewMessage));
		static readonly DbgDotNetText dynamicViewName = new DbgDotNetText(new DbgDotNetTextPart(DbgTextColor.Text, dnSpy_Roslyn_Resources.DebuggerVarsWindow_DynamicView));

		readonly DbgDotNetValueNodeProviderFactory valueNodeProviderFactory;
		readonly DbgDotNetValue instanceValue;
		readonly DmdType expectedType;
		readonly string valueExpression;
		readonly DmdAppDomain appDomain;
		string dynamicViewProxyExpression;
		DbgDotNetValue getDynamicViewValue;

		public DynamicViewMembersValueNodeProvider(DbgDotNetValueNodeProviderFactory valueNodeProviderFactory, LanguageValueNodeFactory valueNodeFactory, DbgDotNetValue instanceValue, DmdType expectedType, string valueExpression, DmdAppDomain appDomain, DbgValueNodeEvaluationOptions evalOptions)
			: base(valueNodeFactory, dynamicViewName, valueExpression + ", " + PredefinedFormatSpecifiers.DynamicView, default, evalOptions) {
			this.valueNodeProviderFactory = valueNodeProviderFactory;
			this.instanceValue = instanceValue;
			this.expectedType = expectedType;
			this.valueExpression = valueExpression;
			this.appDomain = appDomain;
			dynamicViewProxyExpression = string.Empty;
		}

		sealed class ForceLoadAssemblyState {
			public volatile int Counter;
		}

		protected override string InitializeCore(DbgEvaluationInfo evalInfo) {
			if ((evalOptions & DbgValueNodeEvaluationOptions.NoFuncEval) != 0)
				return PredefinedEvaluationErrorMessages.FuncEvalDisabled;

			var proxyCtor = DynamicMetaObjectProviderDebugViewHelper.GetDynamicMetaObjectProviderDebugViewConstructor(appDomain);
			if ((object)proxyCtor == null) {
				var loadState = appDomain.GetOrCreateData<ForceLoadAssemblyState>();
				if (Interlocked.Exchange(ref loadState.Counter, 1) == 0) {
					var loader = new ReflectionAssemblyLoader(evalInfo, appDomain);
					if (loader.TryLoadAssembly(GetRequiredAssemblyFullName(evalInfo.Runtime)))
						proxyCtor = DynamicMetaObjectProviderDebugViewHelper.GetDynamicMetaObjectProviderDebugViewConstructor(appDomain);
				}
				if ((object)proxyCtor == null) {
					var asmFilename = GetRequiredAssemblyFilename(evalInfo.Runtime);
					var asm = appDomain.GetAssembly(Path.GetFileNameWithoutExtension(asmFilename));
					if (asm == null)
						return string.Format(dnSpy_Roslyn_Resources.DynamicViewAssemblyNotLoaded, asmFilename);
					return string.Format(dnSpy_Roslyn_Resources.TypeDoesNotExistInAssembly, DynamicMetaObjectProviderDebugViewHelper.GetDebugViewTypeDisplayName(), asmFilename);
				}
			}

			var runtime = evalInfo.Runtime.GetDotNetRuntime();
			var proxyTypeResult = runtime.CreateInstance(evalInfo, proxyCtor, new[] { instanceValue }, DbgDotNetInvokeOptions.None);
			if (proxyTypeResult.HasError)
				return proxyTypeResult.ErrorMessage;

			dynamicViewProxyExpression = valueNodeProviderFactory.GetNewObjectExpression(proxyCtor, valueExpression, expectedType);
			getDynamicViewValue = proxyTypeResult.Value;
			valueNodeProviderFactory.GetMemberCollections(getDynamicViewValue.Type, evalOptions, out membersCollection, out _);
			return null;
		}

		// DNF 4.0:  "Microsoft.CSharp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
		// DNC 1.0+: "Microsoft.CSharp, Version=4.0.X.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
		string GetRequiredAssemblyFullName(DbgRuntime runtime) =>
			"Microsoft.CSharp, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

		string GetRequiredAssemblyFilename(DbgRuntime runtime) =>
			"Microsoft.CSharp.dll";

		protected override (DbgDotNetValueNode node, bool canHide) CreateValueNode(DbgEvaluationInfo evalInfo, int index, DbgValueNodeEvaluationOptions options, ReadOnlyCollection<string> formatSpecifiers) =>
			CreateValueNode(evalInfo, false, getDynamicViewValue.Type, getDynamicViewValue, index, options, dynamicViewProxyExpression, formatSpecifiers);

		protected override (DbgDotNetValueNode node, bool canHide) TryCreateInstanceValueNode(DbgEvaluationInfo evalInfo, DbgDotNetValueResult valueResult) {
			var noResultsNode = DebugViewNoResultsValueNode.TryCreate(evalInfo, Expression, valueResult);
			if (noResultsNode != null) {
				valueResult.Value?.Dispose();
				return (noResultsNode, false);
			}
			return (null, false);
		}

		protected override void DisposeCore() => getDynamicViewValue?.Dispose();
	}
}
