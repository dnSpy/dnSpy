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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	abstract class DbgDotNetValueNodeProvider {
		public abstract DbgDotNetText Name { get; }
		public abstract string Expression { get; }
		public abstract string ImageName { get; }
		public virtual DbgDotNetText ValueText => default;

		public abstract bool? HasChildren { get; }
		public abstract ulong GetChildCount(DbgEvaluationInfo evalInfo);
		public abstract DbgDotNetValueNode[] GetChildren(LanguageValueNodeFactory valueNodeFactory, DbgEvaluationInfo evalInfo, ulong index, int count, DbgValueNodeEvaluationOptions options, ReadOnlyCollection<string> formatSpecifiers);

		public abstract void Dispose();

		public static DbgDotNetValueNodeProvider Create(List<DbgDotNetValueNodeProvider> providers) {
			if (providers.Count == 0)
				return null;
			if (providers.Count == 1)
				return providers[0];
			return new AggregateValueNodeProvider(providers.ToArray());
		}

		protected static bool NeedCast(DmdType slotType, DmdType memberDeclaringType) {
			if (slotType.IsInterface)
				return true;
			else
				return !slotType.CanCastTo(memberDeclaringType);
		}
	}
}
