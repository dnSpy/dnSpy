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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	sealed class AggregateValueNodeProvider : DbgDotNetValueNodeProvider {
		public override DbgDotNetText Name => providers[0].Name;
		public override string Expression => providers[0].Expression;
		public override string ImageName => providers[0].ImageName;
		public override bool? HasChildren => true;
		public override ulong ChildCount => providers[0].ChildCount + (uint)(providers.Length - 1);

		readonly DbgDotNetValueNodeProvider[] providers;

		public AggregateValueNodeProvider(DbgDotNetValueNodeProvider[] providers) {
			Debug.Assert(providers.Length > 1);
			this.providers = providers;
		}

		public override DbgDotNetValueNode[] GetChildren(LanguageValueNodeFactory valueNodeFactory, DbgEvaluationContext context, DbgStackFrame frame, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			if (count == 0)
				return Array.Empty<DbgDotNetValueNode>();

			var first = providers[0];
			if (index + (uint)count <= first.ChildCount)
				return first.GetChildren(valueNodeFactory, context, frame, index, count, options, cancellationToken);

			var res = new DbgDotNetValueNode[count];
			try {
				int w = 0;
				if (index < first.ChildCount) {
					var tmp = first.GetChildren(valueNodeFactory, context, frame, index, (int)(first.ChildCount - index), options, cancellationToken);
					Array.Copy(tmp, res, tmp.Length);
					w += tmp.Length;
				}
				for (int i = (int)(index - first.ChildCount) + 1; i < providers.Length && w < count; i++) {
					var provider = providers[i];
					res[w++] = valueNodeFactory.Create(context, provider.Name, provider, options, provider.Expression, provider.ImageName);
				}
				if (w != res.Length)
					throw new InvalidOperationException();
				return res;
			}
			catch {
				context.Runtime.Process.DbgManager.Close(res.Where(a => a != null));
				throw;
			}
		}

		public override void Dispose() {
			foreach (var p in providers)
				p.Dispose();
		}
	}
}
