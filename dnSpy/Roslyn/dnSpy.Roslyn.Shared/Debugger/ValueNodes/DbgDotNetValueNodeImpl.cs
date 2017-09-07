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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	sealed class DbgDotNetValueNodeImpl : DbgDotNetValueNode {
		public override DmdType ExpectedType { get; }
		public override string ErrorMessage { get; }
		public override DbgDotNetValue Value { get; }
		public override DbgDotNetText Name { get; }
		public override string Expression { get; }
		public override string ImageName { get; }
		public override bool IsReadOnly { get; }
		public override bool CausesSideEffects { get; }
		public override bool? HasChildren => false;//TODO:
		public override ulong ChildCount => 0;//TODO:

		public DbgDotNetValueNodeImpl(DbgDotNetText name, DbgDotNetValue value, string expression, string imageName, bool isReadOnly, bool causesSideEffects, DmdType expectedType, string errorMessage) {
			if (name.Parts == null)
				throw new ArgumentException();
			Name = name;
			Value = value;
			Expression = expression ?? throw new ArgumentNullException(nameof(expression));
			ImageName = imageName ?? throw new ArgumentNullException(nameof(imageName));
			IsReadOnly = isReadOnly;
			CausesSideEffects = causesSideEffects;
			ExpectedType = expectedType;
			ErrorMessage = errorMessage;
		}

		public override DbgDotNetValueNode[] GetChildren(DbgEvaluationContext context, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			return Array.Empty<DbgDotNetValueNode>();//TODO:
		}

		protected override void CloseCore() {
			//TODO: Close Value if not null
		}
	}
}
