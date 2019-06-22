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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	sealed class InstanceMembersValueNodeProvider : MembersValueNodeProvider {
		public override string ImageName => imageName;

		readonly bool addParens;
		readonly DmdType slotType;
		readonly DbgDotNetValue value;
		readonly string imageName;

		public InstanceMembersValueNodeProvider(LanguageValueNodeFactory valueNodeFactory, DbgDotNetText name, string expression, bool addParens, DmdType slotType, DbgDotNetValue value, MemberValueNodeInfoCollection membersCollection, DbgValueNodeEvaluationOptions evalOptions, string imageName)
			: base(valueNodeFactory, name, expression, membersCollection, evalOptions) {
			this.addParens = addParens;
			this.slotType = slotType;
			this.value = value;
			this.imageName = imageName;
		}

		protected override (DbgDotNetValueNode node, bool canHide) CreateValueNode(DbgEvaluationInfo evalInfo, int index, DbgValueNodeEvaluationOptions options, ReadOnlyCollection<string>? formatSpecifiers) =>
			CreateValueNode(evalInfo, addParens, slotType, value, index, options, Expression, formatSpecifiers);
	}
}
