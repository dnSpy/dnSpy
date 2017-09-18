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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	abstract class DbgDotNetValueNodeProviderFactory {
		[Flags]
		enum TypeStateFlags {
			None = 0,
			AddResultsView = 1,
			IsNullable = 2,
			IsTupleType = 4,
		}

		sealed class TypeState {
			public readonly DmdType Type;
			public readonly TypeStateFlags Flags;
			public readonly string Expression;
			public readonly bool HasNoChildren;
			public readonly MemberValueNodeInfo[] InstanceMembers;
			public readonly MemberValueNodeInfo[] StaticMembers;

			public bool AddResultsView => (Flags & TypeStateFlags.AddResultsView) != 0;
			public bool IsNullable => (Flags & TypeStateFlags.IsNullable) != 0;
			public bool IsTupleType => (Flags & TypeStateFlags.IsTupleType) != 0;

			public DbgValueNodeEvaluationOptions CachedEvalOptions;
			public MemberValueNodeInfo[] CachedInstanceMembers;
			public MemberValueNodeInfo[] CachedStaticMembers;

			public TypeState(DmdType type, string expression) {
				Type = type;
				Flags = GetFlags(type);
				Expression = expression;
				HasNoChildren = true;
				InstanceMembers = Array.Empty<MemberValueNodeInfo>();
				StaticMembers = Array.Empty<MemberValueNodeInfo>();
			}

			public TypeState(DmdType type, string expression, MemberValueNodeInfo[] instanceMembers, MemberValueNodeInfo[] staticMembers) {
				Type = type;
				Flags = GetFlags(type);
				Expression = expression;
				HasNoChildren = false;
				InstanceMembers = instanceMembers;
				StaticMembers = staticMembers;
			}

			static TypeStateFlags GetFlags(DmdType type) {
				var res = TypeStateFlags.None;
				if (type.AppDomain.System_Collections_IEnumerable.IsAssignableFrom(type))
					res |= TypeStateFlags.AddResultsView;
				if (type.IsNullable)
					res |= TypeStateFlags.IsNullable;
				if (Formatters.TypeFormatterUtils.IsTupleType(type))
					res |= TypeStateFlags.IsTupleType;
				return res;
			}
		}

		/// <summary>
		/// Returns true if <paramref name="type"/> is a primitive type that doesn't show any members,
		/// eg. integers, booleans, floating point numbers, strings
		/// </summary>
		/// <param name="type">Type to check</param>
		/// <returns></returns>
		protected abstract bool HasNoChildren(DmdType type);

		protected abstract DbgDotNetText InstanceMembersName { get; }
		protected abstract DbgDotNetText StaticMembersName { get; }
		protected abstract void FormatTypeName(ITextColorWriter output, DmdType type);
		protected abstract void FormatFieldName(ITextColorWriter output, DmdFieldInfo field);
		protected abstract void FormatPropertyName(ITextColorWriter output, DmdPropertyInfo property);
		public abstract void FormatArrayName(ITextColorWriter output, int index);
		public abstract void FormatArrayName(ITextColorWriter output, int[] indexes);

		public DbgDotNetValueNodeProvider Create(DbgDotNetValue value, string expression, bool isReadOnly, DbgValueNodeEvaluationOptions options) {
			if (value == null)
				return null;
			var type = value.Type;
			if (type.IsByRef)
				type = type.GetElementType();
			var state = GetOrCreateTypeState(type);
			return CreateCore(state, new DbgDotNetInstanceValueInfo(expression, value.Type, value, isReadOnly), options);
		}

		TypeState GetOrCreateTypeState(DmdType type) {
			var state = StateWithKey<TypeState>.TryGet(type, this);
			if (state != null)
				return state;
			return CreateTypeState(type);

			TypeState CreateTypeState(DmdType type2) {
				var state2 = CreateTypeStateCore(type2);
				return StateWithKey<TypeState>.GetOrCreate(type2, this, () => state2);
			}
		}

		sealed class MemberValueNodeInfoEqualityComparer : IComparer<MemberValueNodeInfo> {
			public static readonly MemberValueNodeInfoEqualityComparer Instance = new MemberValueNodeInfoEqualityComparer();
			MemberValueNodeInfoEqualityComparer() { }

			public int Compare(MemberValueNodeInfo x, MemberValueNodeInfo y) {
				int c = StringComparer.Ordinal.Compare(x.Member.Name, y.Member.Name);
				if (c != 0)
					return c;

				c = GetOrder(x.Member.MemberType) - GetOrder(y.Member.MemberType);
				if (c != 0)
					return c;

				c = y.InheritanceLevel.CompareTo(x.InheritanceLevel);
				if (c != 0)
					return c;
				return x.Member.MetadataToken - y.Member.MetadataToken;
			}

			static int GetOrder(DmdMemberTypes memberType) {
				if (memberType == DmdMemberTypes.Field)
					return 0;
				if (memberType == DmdMemberTypes.Property)
					return 1;
				throw new InvalidOperationException();
			}
		}

		string GetTypeExpression(DmdType type) {
			var output = new StringBuilderTextColorOutput();
			FormatTypeName(output, type);
			return output.ToString();
		}

		TypeState CreateTypeStateCore(DmdType type) {
			var typeExpression = GetTypeExpression(type);
			if (HasNoChildren(type) || type.IsFunctionPointer)
				return new TypeState(type, typeExpression);

			MemberValueNodeInfo[] instanceMembers, staticMembers;

			Debug.Assert(!type.IsByRef);
			if (type.TypeSignatureKind == DmdTypeSignatureKind.Type || type.TypeSignatureKind == DmdTypeSignatureKind.GenericInstance) {
				var instanceMembersList = new List<MemberValueNodeInfo>();
				var staticMembersList = new List<MemberValueNodeInfo>();

				byte inheritanceLevel;
				DmdType currentType;

				inheritanceLevel = 0;
				currentType = type;
				foreach (var field in type.Fields) {
					var declType = field.DeclaringType;
					while (declType != currentType) {
						Debug.Assert((object)currentType.BaseType != null);
						currentType = currentType.BaseType;
						if (inheritanceLevel != byte.MaxValue)
							inheritanceLevel++;
					}
					if (field.IsStatic)
						staticMembersList.Add(new MemberValueNodeInfo(field, inheritanceLevel));
					else
						instanceMembersList.Add(new MemberValueNodeInfo(field, inheritanceLevel));
				}

				inheritanceLevel = 0;
				currentType = type;
				foreach (var property in type.Properties) {
					if (property.GetMethodSignature().GetParameterTypes().Count != 0)
						continue;
					var declType = property.DeclaringType;
					while (declType != currentType) {
						Debug.Assert((object)currentType.BaseType != null);
						currentType = currentType.BaseType;
						if (inheritanceLevel != byte.MaxValue)
							inheritanceLevel++;
					}
					var getter = property.GetGetMethod(DmdGetAccessorOptions.All);
					if ((object)getter == null || getter.GetMethodSignature().GetParameterTypes().Count != 0)
						continue;
					if (getter.IsStatic)
						staticMembersList.Add(new MemberValueNodeInfo(property, inheritanceLevel));
					else
						instanceMembersList.Add(new MemberValueNodeInfo(property, inheritanceLevel));
				}

				instanceMembers = instanceMembersList.Count == 0 ? Array.Empty<MemberValueNodeInfo>() : instanceMembersList.ToArray();
				staticMembers = staticMembersList.Count == 0 ? Array.Empty<MemberValueNodeInfo>() : staticMembersList.ToArray();

				Array.Sort(instanceMembers, MemberValueNodeInfoEqualityComparer.Instance);
				Array.Sort(staticMembers, MemberValueNodeInfoEqualityComparer.Instance);
				var output = new DbgDotNetTextOutput();
				UpdateNames(instanceMembers, output);
				UpdateNames(staticMembers, output);
			}
			else
				staticMembers = instanceMembers = Array.Empty<MemberValueNodeInfo>();

			return new TypeState(type, typeExpression, instanceMembers, staticMembers);
		}

		void UpdateNames(MemberValueNodeInfo[] infos, DbgDotNetTextOutput output) {
			if (infos.Length == 0)
				return;

			string prevName = null;
			for (int i = 0; i < infos.Length; i++) {
				ref var info = ref infos[i];
				var member = info.Member;
				if (member.Name == prevName) {
					ref var prev = ref infos[i - 1];
					FormatName(output, prev.Member);
					output.Write(BoxedTextColor.Text, " ");
					output.Write(BoxedTextColor.Punctuation, "(");
					FormatTypeName(output, prev.Member.DeclaringType);
					output.Write(BoxedTextColor.Punctuation, ")");
					prev.Name = output.CreateAndReset();
				}
				FormatName(output, member);
				info.Name = output.CreateAndReset();

				prevName = member.Name;
			}
		}

		void FormatName(DbgDotNetTextOutput output, DmdMemberInfo member) {
			if (member.MemberType == DmdMemberTypes.Field)
				FormatFieldName(output, (DmdFieldInfo)member);
			else {
				Debug.Assert(member.MemberType == DmdMemberTypes.Property);
				FormatPropertyName(output, (DmdPropertyInfo)member);
			}
		}

		DbgDotNetValueNodeProvider CreateCore(TypeState state, DbgDotNetInstanceValueInfo valueInfo, DbgValueNodeEvaluationOptions evalOptions) {
			if (state.HasNoChildren)
				return null;

			if (state.IsNullable) {
				//TODO: check if null
			}

			if (state.IsTupleType) {
				//TODO: value tuples
			}

			if (state.Type.IsArray && !valueInfo.Value.IsNullReference)
				return new ArrayValueNodeProvider(this, valueInfo);

			if ((evalOptions & DbgValueNodeEvaluationOptions.RawView) == 0) {
				//TODO: Check if type or base type has System.Diagnostics.DebuggerTypeProxyAttribute
			}

			MemberValueNodeInfo[] instanceMembersInfos;
			MemberValueNodeInfo[] staticMembersInfos;
			lock (state) {
				if (state.CachedEvalOptions != evalOptions || state.CachedInstanceMembers == null) {
					state.CachedEvalOptions = evalOptions;
					state.CachedInstanceMembers = Filter(state.InstanceMembers, evalOptions);
					state.CachedStaticMembers = Filter(state.StaticMembers, evalOptions);
				}
				instanceMembersInfos = state.CachedInstanceMembers;
				staticMembersInfos = state.CachedStaticMembers;
			}

			var providers = new List<DbgDotNetValueNodeProvider>(2);

			if (valueInfo.Value.IsNullReference)
				instanceMembersInfos = Array.Empty<MemberValueNodeInfo>();
			providers.Add(new InstanceMembersValueNodeProvider(InstanceMembersName, state.Expression, valueInfo, instanceMembersInfos));

			if (staticMembersInfos.Length != 0)
				providers.Add(new StaticMembersValueNodeProvider(StaticMembersName, state.Expression, valueInfo, staticMembersInfos));

			//TODO: dynamic types
			//TODO: non-void and non-null pointers (derefence and show members)
			//TODO: raw view

			if (state.AddResultsView) {
				//TODO: Add "Results View"
			}

			return DbgDotNetValueNodeProvider.Create(providers);
		}

		MemberValueNodeInfo[] Filter(MemberValueNodeInfo[] infos, DbgValueNodeEvaluationOptions evalOptions) {
			bool hideCompilerGeneratedMembers = (evalOptions & DbgValueNodeEvaluationOptions.HideCompilerGeneratedMembers) != 0;
			bool respectHideMemberAttributes = (evalOptions & DbgValueNodeEvaluationOptions.RespectHideMemberAttributes) != 0;
			if (!hideCompilerGeneratedMembers && !respectHideMemberAttributes)
				return infos;
			return infos.Where(a => {
				Debug.Assert(a.Member.MemberType == DmdMemberTypes.Field || a.Member.MemberType == DmdMemberTypes.Property);
				if (respectHideMemberAttributes && a.HasDebuggerBrowsableState_Never)
					return false;
				if (hideCompilerGeneratedMembers && a.Member.MemberType == DmdMemberTypes.Field && a.HasCompilerGeneratedAttribute)
					return false;
				return true;
			}).ToArray();
		}
	}
}
