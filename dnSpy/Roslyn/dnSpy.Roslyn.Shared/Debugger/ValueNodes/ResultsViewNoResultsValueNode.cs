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
using System.Globalization;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;
using dnSpy.Roslyn.Shared.Properties;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	sealed class ResultsViewNoResultsValueNode : DbgDotNetValueNode {
		public override DmdType ExpectedType => null;
		public override DmdType ActualType => null;
		public override string ErrorMessage => null;
		public override DbgDotNetValue Value => null;
		public override DbgDotNetText Name => emptyPropertyName;
		public override string Expression { get; }
		public override string ImageName => PredefinedDbgValueNodeImageNames.Property;
		public override bool IsReadOnly => true;
		public override bool CausesSideEffects => false;
		public override bool? HasChildren => false;

		static readonly DbgDotNetText noResultsName = new DbgDotNetText(new DbgDotNetTextPart(BoxedTextColor.Text, dnSpy_Roslyn_Shared_Resources.DebuggerVarsWindow_ResultsView_NoResults));
		static readonly DbgDotNetText emptyPropertyName = new DbgDotNetText(new DbgDotNetTextPart(BoxedTextColor.InstanceProperty, dnSpy_Roslyn_Shared_Resources.DebuggerVarsWindow_Empty_PropertyName));

		public ResultsViewNoResultsValueNode(string expression) => Expression = expression;

		public override bool FormatValue(DbgEvaluationContext context, DbgStackFrame frame, ITextColorWriter output, CultureInfo cultureInfo, CancellationToken cancellationToken) {
			noResultsName.WriteTo(output);
			return true;
		}

		public override ulong GetChildCount(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) => 0;
		public override DbgDotNetValueNode[] GetChildren(DbgEvaluationContext context, DbgStackFrame frame, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) => Array.Empty<DbgDotNetValueNode>();
		protected override void CloseCore(DbgDispatcher dispatcher) { }
	}
}
