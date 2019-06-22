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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	sealed class AggregateValueNodeProvider : DbgDotNetValueNodeProvider {
		public override DbgDotNetText Name => providers[0].Name;
		public override string Expression => providers[0].Expression;
		public override string ImageName => providers[0].ImageName;
		public override bool? HasChildren => true;

		readonly DbgDotNetValueNodeProvider[] providers;

		public AggregateValueNodeProvider(DbgDotNetValueNodeProvider[] providers) {
			Debug.Assert(providers.Length > 1);
			this.providers = providers;
		}

		public override ulong GetChildCount(DbgEvaluationInfo evalInfo) =>
			providers[0].GetChildCount(evalInfo) + (uint)(providers.Length - 1);

		public override DbgDotNetValueNode[] GetChildren(LanguageValueNodeFactory valueNodeFactory, DbgEvaluationInfo evalInfo, ulong index, int count, DbgValueNodeEvaluationOptions options, ReadOnlyCollection<string>? formatSpecifiers) {
			if (count == 0)
				return Array.Empty<DbgDotNetValueNode>();

			var first = providers[0];
			ulong childCount = first.GetChildCount(evalInfo);
			if (index + (uint)count <= childCount)
				return first.GetChildren(valueNodeFactory, evalInfo, index, count, options, formatSpecifiers);

			var res = new DbgDotNetValueNode[count];
			try {
				int w = 0;
				if (index < childCount) {
					var tmp = first.GetChildren(valueNodeFactory, evalInfo, index, (int)(childCount - index), options, formatSpecifiers);
					Array.Copy(tmp, res, tmp.Length);
					w += tmp.Length;
				}
				for (int i = (int)(index - childCount) + 1; i < providers.Length && w < count; i++) {
					evalInfo.CancellationToken.ThrowIfCancellationRequested();
					var provider = providers[i];
					res[w++] = valueNodeFactory.Create(evalInfo, provider.Name, provider, formatSpecifiers, options, provider.Expression, provider.ImageName, provider.ValueText);
				}
				if (w != res.Length)
					throw new InvalidOperationException();
				return res;
			}
			catch {
				evalInfo.Context.Runtime.Process.DbgManager.Close(res.Where(a => !(a is null)));
				throw;
			}
		}

		public override void Dispose() {
			foreach (var p in providers)
				p.Dispose();
		}
	}
}
