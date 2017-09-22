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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	abstract class MembersValueNodeProvider : DbgDotNetValueNodeProvider {
		public sealed override DbgDotNetText Name { get; }
		public sealed override string Expression { get; }
		public sealed override bool? HasChildren => membersCollection.Members.Length > 0;

		protected readonly LanguageValueNodeFactory valueNodeFactory;
		/*readonly*/ protected MemberValueNodeInfoCollection membersCollection;
		protected readonly DbgValueNodeEvaluationOptions evalOptions;
		ChildNodeProviderInfo[] childNodeProviderInfos;
		bool hasInitialized;
		DbgManager dbgManager;

		protected struct ChildNodeProviderInfo {
			public readonly ulong StartIndex;
			// Not inclusive
			public readonly ulong EndIndex;
			public readonly DbgDotNetValueNode ValueNode;
			public ChildNodeProviderInfo(ulong startIndex, ulong endIndex, DbgDotNetValueNode valueNode) {
				StartIndex = startIndex;
				EndIndex = endIndex;
				ValueNode = valueNode;
			}
			public ChildNodeProviderInfo(ulong startIndex, ulong endIndex) {
				StartIndex = startIndex;
				EndIndex = endIndex;
				ValueNode = null;
			}
		}

		static class Cache {
			static List<ChildNodeProviderInfo> providerInfoList;
			public static List<ChildNodeProviderInfo> AllocProviderList() => Interlocked.Exchange(ref providerInfoList, null) ?? new List<ChildNodeProviderInfo>();
			public static ChildNodeProviderInfo[] FreeAndToArray(ref List<ChildNodeProviderInfo> list) {
				var res = list.ToArray();
				list.Clear();
				providerInfoList = list;
				return res;
			}
		}

		protected MembersValueNodeProvider(LanguageValueNodeFactory valueNodeFactory, DbgDotNetText name, string expression, MemberValueNodeInfoCollection membersCollection, DbgValueNodeEvaluationOptions evalOptions) {
			this.valueNodeFactory = valueNodeFactory;
			Name = name;
			Expression = expression;
			this.membersCollection = membersCollection;
			this.evalOptions = evalOptions;
		}

		public sealed override ulong GetChildCount(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			if (!hasInitialized)
				Initialize(context, frame, cancellationToken);
			return childNodeProviderInfos[childNodeProviderInfos.Length - 1].EndIndex;
		}

		void Initialize(DbgEvaluationContext context, DbgStackFrame frame, CancellationToken cancellationToken) {
			if (hasInitialized)
				return;
			dbgManager = context.Process.DbgManager;
			if ((evalOptions & DbgValueNodeEvaluationOptions.NoHideRoots) != 0 || !membersCollection.HasHideRoot || membersCollection.Members.Length == 0)
				childNodeProviderInfos = new ChildNodeProviderInfo[] { new ChildNodeProviderInfo(0, (uint)membersCollection.Members.Length) };
			else {
				DbgDotNetValueNode valueNode = null;
				var list = Cache.AllocProviderList();
				try {
					var members = membersCollection.Members;
					int membersBaseIndex = 0;
					ulong baseIndex = 0;
					int i;
					for (i = 0; i < members.Length; i++) {
						if (!members[i].HasDebuggerBrowsableState_RootHidden)
							continue;
						if (membersBaseIndex != i) {
							list.Add(new ChildNodeProviderInfo(baseIndex, baseIndex + (uint)(i - membersBaseIndex)));
							membersBaseIndex = i;
							baseIndex += (uint)(i - membersBaseIndex);
						}

						valueNode = CreateValueNode(context, frame, membersBaseIndex, evalOptions, cancellationToken);
						ulong childCount = valueNode.GetChildCount(context, frame, cancellationToken);
						list.Add(new ChildNodeProviderInfo(baseIndex, baseIndex + childCount, valueNode));

						membersBaseIndex++;
						baseIndex += childCount;
						valueNode = null;
					}
					if (membersBaseIndex != i)
						list.Add(new ChildNodeProviderInfo(baseIndex, baseIndex + (uint)(i - membersBaseIndex)));
					if (list.Count == 0)
						list.Add(new ChildNodeProviderInfo(0, 0));
					childNodeProviderInfos = Cache.FreeAndToArray(ref list);
				}
				catch {
					if (valueNode != null)
						dbgManager.Close(valueNode);
					dbgManager.Close(list.Select(a => a.ValueNode));
					throw;
				}
			}
			hasInitialized = true;
		}

		protected abstract DbgDotNetValueNode CreateValueNode(DbgEvaluationContext context, DbgStackFrame frame, int index, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken);

		int lastProviderIndex;
		int GetProviderIndex(ulong childIndex) {
			var infos = childNodeProviderInfos;
			ref var last = ref infos[lastProviderIndex];
			if (last.StartIndex <= childIndex && childIndex < last.EndIndex)
				return lastProviderIndex;

			int lo = 0, hi = infos.Length - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				ref var info = ref infos[index];
				if (childIndex < info.StartIndex)
					hi = index - 1;
				else if (childIndex >= info.EndIndex)
					lo = index + 1;
				else
					return lastProviderIndex = index;
			}
			return lastProviderIndex = lo;
		}

		public sealed override DbgDotNetValueNode[] GetChildren(LanguageValueNodeFactory valueNodeFactory, DbgEvaluationContext context, DbgStackFrame frame, ulong index, int count, DbgValueNodeEvaluationOptions options, CancellationToken cancellationToken) {
			Debug.Assert(this.valueNodeFactory == valueNodeFactory);
			if (!hasInitialized)
				Initialize(context, frame, cancellationToken);
			DbgDotNetValueNode[] children = null;
			var res = count == 0 ? Array.Empty<DbgDotNetValueNode>() : new DbgDotNetValueNode[count];
			try {
				int providerIndex = GetProviderIndex(index);
				var members = membersCollection.Members;
				for (int i = 0; i < res.Length && providerIndex < childNodeProviderInfos.Length; providerIndex++) {
					cancellationToken.ThrowIfCancellationRequested();
					ref var providerInfo = ref childNodeProviderInfos[providerIndex];
					if (providerInfo.ValueNode != null) {
						ulong childCount = providerInfo.EndIndex - providerInfo.StartIndex;
						int maxChildren = (int)Math.Min(childCount, (uint)(count - i));
						children = providerInfo.ValueNode.GetChildren(context, frame, index + (uint)i, maxChildren, options, cancellationToken);
						for (int j = 0; j < children.Length; j++)
							res[i++] = children[j];
						children = null;
					}
					else {
						while (index + (uint)i < providerInfo.EndIndex && i < res.Length) {
							cancellationToken.ThrowIfCancellationRequested();
							res[i] = CreateValueNode(context, frame, (int)index + i, options, cancellationToken);
							i++;
						}
					}
				}
			}
			catch {
				if (children != null)
					context.Process.DbgManager.Close(children);
				context.Process.DbgManager.Close(res.Where(a => a != null));
				throw;
			}
			return res;
		}

		public sealed override void Dispose() {
			Debug.Assert(childNodeProviderInfos == null || dbgManager != null);
			if (childNodeProviderInfos != null) {
				Debug.Assert(childNodeProviderInfos.Length >= 1);
				if (childNodeProviderInfos.Length > 1 || childNodeProviderInfos[0].ValueNode != null)
					dbgManager?.Close(childNodeProviderInfos.Select(a => a.ValueNode).Where(a => a != null));
			}
		}
	}
}
