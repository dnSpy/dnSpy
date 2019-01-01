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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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
	abstract class MembersValueNodeProvider : DbgDotNetValueNodeProvider {
		public sealed override DbgDotNetText Name { get; }
		public sealed override string Expression { get; }
		public sealed override bool? HasChildren => realProvider?.HasChildren ?? ((membersCollection.Members?.Length ?? 1) > 0);

		protected readonly LanguageValueNodeFactory valueNodeFactory;
		protected MemberValueNodeInfoCollection membersCollection;
		protected readonly DbgValueNodeEvaluationOptions evalOptions;
		ChildNodeProviderInfo[] childNodeProviderInfos;
		bool hasInitialized;
		DbgManager dbgManager;
		string errorMessage;
		protected DbgDotNetValueNodeProvider realProvider;

		protected readonly struct ChildNodeProviderInfo {
			public readonly ulong StartIndex;
			// Not inclusive
			public readonly ulong EndIndex;
			public readonly uint BaseIndex;
			public readonly DbgDotNetValueNode ValueNode;
			public readonly bool CanHide;
			public ChildNodeProviderInfo(ulong startIndex, ulong endIndex, DbgDotNetValueNode valueNode, bool canHide) {
				StartIndex = startIndex;
				EndIndex = endIndex;
				BaseIndex = 0;
				ValueNode = valueNode;
				CanHide = canHide;
			}
			public ChildNodeProviderInfo(ulong startIndex, ulong endIndex, uint baseIndex) {
				StartIndex = startIndex;
				EndIndex = endIndex;
				BaseIndex = baseIndex;
				ValueNode = null;
				CanHide = false;
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

		public sealed override ulong GetChildCount(DbgEvaluationInfo evalInfo) {
			if (!hasInitialized)
				Initialize(evalInfo);
			if (realProvider != null)
				return realProvider.GetChildCount(evalInfo);
			return childNodeProviderInfos[childNodeProviderInfos.Length - 1].EndIndex;
		}

		protected virtual string InitializeCore(DbgEvaluationInfo evalInfo) => null;

		void Initialize(DbgEvaluationInfo evalInfo) {
			if (hasInitialized)
				return;
			errorMessage = InitializeCore(evalInfo);
			if (realProvider != null)
				return;
			Debug.Assert(errorMessage != null || membersCollection.Members != null);
			if (errorMessage == null && membersCollection.Members == null)
				errorMessage = PredefinedEvaluationErrorMessages.InternalDebuggerError;
			dbgManager = evalInfo.Runtime.Process.DbgManager;
			if (errorMessage != null)
				childNodeProviderInfos = new ChildNodeProviderInfo[] { new ChildNodeProviderInfo(0, 1, 0) };
			else if ((evalOptions & DbgValueNodeEvaluationOptions.NoHideRoots) != 0 || !membersCollection.HasHideRoot || (evalOptions & DbgValueNodeEvaluationOptions.RawView) != 0 || membersCollection.Members.Length == 0)
				childNodeProviderInfos = new ChildNodeProviderInfo[] { new ChildNodeProviderInfo(0, (uint)membersCollection.Members.Length, 0) };
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
							list.Add(new ChildNodeProviderInfo(baseIndex, baseIndex + (uint)(i - membersBaseIndex), (uint)membersBaseIndex));
							baseIndex += (uint)(i - membersBaseIndex);
							membersBaseIndex = i;
						}

						evalInfo.CancellationToken.ThrowIfCancellationRequested();
						// Format specifiers get updated in GetChildren()
						var info = CreateValueNode(evalInfo, membersBaseIndex, evalOptions, formatSpecifiers: null);
						valueNode = info.node;
						ulong childCount = info.canHide ? valueNode.GetChildCount(evalInfo) : 1;
						list.Add(new ChildNodeProviderInfo(baseIndex, baseIndex + childCount, valueNode, info.canHide));

						membersBaseIndex++;
						baseIndex += childCount;
						valueNode = null;
					}
					if (membersBaseIndex != i)
						list.Add(new ChildNodeProviderInfo(baseIndex, baseIndex + (uint)(i - membersBaseIndex), (uint)membersBaseIndex));
					if (list.Count == 0)
						list.Add(new ChildNodeProviderInfo(0, 0, 0));
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

		protected abstract (DbgDotNetValueNode node, bool canHide) CreateValueNode(DbgEvaluationInfo evalInfo, int index, DbgValueNodeEvaluationOptions options, ReadOnlyCollection<string> formatSpecifiers);

		protected (DbgDotNetValueNode node, bool canHide) CreateValueNode(DbgEvaluationInfo evalInfo, bool addParens, DmdType slotType, DbgDotNetValue value, int index, DbgValueNodeEvaluationOptions options, string baseExpression, ReadOnlyCollection<string> formatSpecifiers) {
			var runtime = evalInfo.Runtime.GetDotNetRuntime();
			if ((evalOptions & DbgValueNodeEvaluationOptions.RawView) != 0)
				options |= DbgValueNodeEvaluationOptions.RawView;
			DbgDotNetValueResult valueResult = default;
			try {
				ref var info = ref membersCollection.Members[index];
				string expression, imageName;
				bool isReadOnly;
				DmdType expectedType;
				var castType = info.NeedCast || NeedCast(slotType, GetMemberDeclaringType(info.Member)) ? info.Member.DeclaringType : null;
				switch (info.Member.MemberType) {
				case DmdMemberTypes.Field:
					var field = (DmdFieldInfo)info.Member;
					expression = valueNodeFactory.GetFieldExpression(baseExpression, field.Name, castType, addParens);
					expectedType = field.FieldType;
					imageName = ImageNameUtils.GetImageName(field);
					valueResult = runtime.LoadField(evalInfo, value, field);
					// We should be able to change read only fields (we're a debugger), but since the
					// compiler will complain, we have to prevent the user from editing the value.
					isReadOnly = field.IsInitOnly;
					break;

				case DmdMemberTypes.Property:
					var property = (DmdPropertyInfo)info.Member;
					expression = valueNodeFactory.GetPropertyExpression(baseExpression, property.Name, castType, addParens);
					expectedType = property.PropertyType;
					imageName = ImageNameUtils.GetImageName(property);
					if ((options & DbgValueNodeEvaluationOptions.NoFuncEval) != 0) {
						isReadOnly = true;
						valueResult = DbgDotNetValueResult.CreateError(PredefinedEvaluationErrorMessages.FuncEvalDisabled);
					}
					else {
						var getter = property.GetGetMethod(DmdGetAccessorOptions.All) ?? throw new InvalidOperationException();
						valueResult = runtime.Call(evalInfo, value, getter, Array.Empty<object>(), DbgDotNetInvokeOptions.None);
						isReadOnly = (object)property.GetSetMethod(DmdGetAccessorOptions.All) == null;
					}
					break;

				default:
					throw new InvalidOperationException();
				}

				DbgDotNetValueNode newNode;
				bool canHide = true;
				var customInfo = TryCreateInstanceValueNode(evalInfo, valueResult);
				if (customInfo.node != null)
					(newNode, canHide) = customInfo;
				else if (valueResult.HasError) {
					newNode = valueNodeFactory.CreateError(evalInfo, info.Name, valueResult.ErrorMessage, expression, false);
					canHide = false;
				}
				else if (valueResult.ValueIsException)
					newNode = valueNodeFactory.Create(evalInfo, info.Name, valueResult.Value, formatSpecifiers, options, expression, PredefinedDbgValueNodeImageNames.Error, true, false, expectedType, false);
				else
					newNode = valueNodeFactory.Create(evalInfo, info.Name, valueResult.Value, formatSpecifiers, options, expression, imageName, isReadOnly, false, expectedType, false);

				valueResult = default;
				return (newNode, canHide);
			}
			catch {
				valueResult.Value?.Dispose();
				throw;
			}
		}

		static DmdType GetMemberDeclaringType(DmdMemberInfo member) {
			switch (member.MemberType) {
			case DmdMemberTypes.Field:
				return member.DeclaringType;

			case DmdMemberTypes.Property:
				var property = (DmdPropertyInfo)member;
				var accessor = property.GetGetMethod(DmdGetAccessorOptions.All) ?? property.GetSetMethod(DmdGetAccessorOptions.All);
				if ((object)accessor == null)
					return member.DeclaringType;
				accessor = accessor.GetBaseDefinition();
				return accessor.DeclaringType;

			default:
				throw new InvalidOperationException();
			}
		}

		protected virtual (DbgDotNetValueNode node, bool canHide) TryCreateInstanceValueNode(DbgEvaluationInfo evalInfo, DbgDotNetValueResult valueResult) => (null, false);

		int lastProviderIndex;
		int GetProviderIndex(ulong childIndex) {
			var infos = childNodeProviderInfos;
			ref readonly var last = ref infos[lastProviderIndex];
			if (last.StartIndex <= childIndex && childIndex < last.EndIndex)
				return lastProviderIndex;

			int lo = 0, hi = infos.Length - 1;
			while (lo <= hi) {
				int index = (lo + hi) / 2;

				ref readonly var info = ref infos[index];
				if (childIndex < info.StartIndex)
					hi = index - 1;
				else if (childIndex >= info.EndIndex)
					lo = index + 1;
				else
					return lastProviderIndex = index;
			}
			return lastProviderIndex = lo;
		}

		public sealed override DbgDotNetValueNode[] GetChildren(LanguageValueNodeFactory valueNodeFactory, DbgEvaluationInfo evalInfo, ulong index, int count, DbgValueNodeEvaluationOptions options, ReadOnlyCollection<string> formatSpecifiers) {
			Debug.Assert(this.valueNodeFactory == valueNodeFactory);
			if (!hasInitialized)
				Initialize(evalInfo);
			if (realProvider != null)
				return realProvider.GetChildren(valueNodeFactory, evalInfo, index, count, options, formatSpecifiers);
			DbgDotNetValueNode[] children = null;
			var res = count == 0 ? Array.Empty<DbgDotNetValueNode>() : new DbgDotNetValueNode[count];
			try {
				int providerIndex = GetProviderIndex(index);
				var members = membersCollection.Members;
				for (int i = 0; i < res.Length && providerIndex < childNodeProviderInfos.Length; providerIndex++) {
					evalInfo.CancellationToken.ThrowIfCancellationRequested();
					ref readonly var providerInfo = ref childNodeProviderInfos[providerIndex];
					if (providerInfo.ValueNode != null) {
						UpdateFormatSpecifiers(providerInfo.ValueNode, formatSpecifiers);
						if (providerInfo.CanHide) {
							ulong childCount = providerInfo.EndIndex - providerInfo.StartIndex;
							int maxChildren = (int)Math.Min(childCount, (uint)(count - i));
							children = providerInfo.ValueNode.GetChildren(evalInfo, index + (uint)i - providerInfo.StartIndex, maxChildren, options);
							for (int j = 0; j < children.Length; j++) {
								var child = children[j];
								UpdateFormatSpecifiers(child, formatSpecifiers);
								res[i++] = child;
							}
							children = null;
						}
						else
							res[i++] = providerInfo.ValueNode;
					}
					else {
						while (index + (uint)i < providerInfo.EndIndex && i < res.Length) {
							evalInfo.CancellationToken.ThrowIfCancellationRequested();
							res[i] = errorMessage != null ?
								valueNodeFactory.CreateError(evalInfo, errorPropertyName, errorMessage, Expression, false) :
								CreateValueNode(evalInfo, (int)(index + (uint)i - providerInfo.StartIndex + providerInfo.BaseIndex), options, formatSpecifiers).node;
							i++;
						}
					}
				}
			}
			catch {
				if (children != null)
					evalInfo.Runtime.Process.DbgManager.Close(children);
				evalInfo.Runtime.Process.DbgManager.Close(res.Where(a => a != null));
				throw;
			}
			return res;
		}
		static readonly DbgDotNetText errorPropertyName = new DbgDotNetText(new DbgDotNetTextPart(DbgTextColor.InstanceProperty, dnSpy_Roslyn_Resources.DebuggerVarsWindow_Error_PropertyName));

		void UpdateFormatSpecifiers(DbgDotNetValueNode valueNode, ReadOnlyCollection<string> formatSpecifiers) =>
			(valueNode as DbgDotNetValueNodeImpl)?.SetFormatSpecifiers(formatSpecifiers);

		protected virtual void DisposeCore() { }

		public sealed override void Dispose() {
			Debug.Assert(childNodeProviderInfos == null || dbgManager != null);
			DisposeCore();
			realProvider?.Dispose();
			if (childNodeProviderInfos != null) {
				Debug.Assert(childNodeProviderInfos.Length >= 1);
				if (childNodeProviderInfos.Length > 1 || childNodeProviderInfos[0].ValueNode != null)
					dbgManager?.Close(childNodeProviderInfos.Select(a => a.ValueNode).Where(a => a != null));
			}
		}
	}
}
