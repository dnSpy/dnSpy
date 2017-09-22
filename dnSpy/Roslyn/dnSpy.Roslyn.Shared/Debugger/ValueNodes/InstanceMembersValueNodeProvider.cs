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

using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	sealed class InstanceMembersValueNodeProvider : MembersValueNodeProvider {
		public override string ImageName => imageName;

		readonly DbgDotNetValue value;
		readonly string imageName;

		public InstanceMembersValueNodeProvider(LanguageValueNodeFactory valueNodeFactory, DbgDotNetText name, string expression, DbgDotNetValue value, MemberValueNodeInfoCollection membersCollection, DbgValueNodeEvaluationOptions evalOptions, string imageName)
			: base(valueNodeFactory, name, expression, membersCollection, evalOptions) {
			this.value = value;
			this.imageName = imageName;
		}

		protected override (DbgDotNetValueNode node, bool canHide) CreateValueNode(DbgEvaluationContext context, DbgStackFrame frame, int index, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) =>
			CreateValueNode(context, frame, value, index, options, cancellationToken);
	}
}
