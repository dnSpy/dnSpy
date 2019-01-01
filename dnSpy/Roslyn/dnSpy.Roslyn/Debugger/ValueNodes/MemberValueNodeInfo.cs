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
using System.Diagnostics;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	readonly struct MemberValueNodeInfoCollection {
		public static readonly MemberValueNodeInfoCollection Empty = new MemberValueNodeInfoCollection(Array.Empty<MemberValueNodeInfo>(), false, false);
		public readonly MemberValueNodeInfo[] Members;
		public readonly bool HasHideRoot;
		public readonly bool HasShowNever;
		public MemberValueNodeInfoCollection(MemberValueNodeInfo[] members, bool hasHideRoot, bool hasShowNever) {
			Members = members;
			HasHideRoot = hasHideRoot;
			HasShowNever = hasShowNever;
		}
	}

	struct MemberValueNodeInfo {
		public readonly DmdMemberInfo Member;

		// It gets initialized later so it can't be readonly
		public DbgDotNetText Name;

		/// <summary>
		/// Most derived class is level 0, and least derived class (<see cref="object"/>) has the highest level
		/// </summary>
		public readonly byte InheritanceLevel;

		MemberValueNodeInfoFlags Flags;
		public bool HasDebuggerBrowsableState_Never => (Flags & MemberValueNodeInfoFlags.DebuggerBrowsableState_Mask) == MemberValueNodeInfoFlags.DebuggerBrowsableState_Never;
		public bool HasDebuggerBrowsableState_RootHidden => (Flags & MemberValueNodeInfoFlags.DebuggerBrowsableState_Mask) == MemberValueNodeInfoFlags.DebuggerBrowsableState_RootHidden;
		public bool IsCompilerGenerated => (Flags & MemberValueNodeInfoFlags.CompilerGeneratedName) != 0;
		public bool IsPublic => (Flags & MemberValueNodeInfoFlags.Public) != 0;
		public bool NeedCast => (Flags & MemberValueNodeInfoFlags.NeedCast) != 0;
		public bool NeedTypeName => (Flags & MemberValueNodeInfoFlags.NeedTypeName) != 0;
		public bool DeprecatedError => (Flags & MemberValueNodeInfoFlags.DeprecatedError) != 0;

		[Flags]
		enum MemberValueNodeInfoFlags : byte {
			None = 0,
			DebuggerBrowsableState_Mask			= 3,
			DebuggerBrowsableState_Default		= 0,
			DebuggerBrowsableState_Never		= 1,
			DebuggerBrowsableState_RootHidden	= 2,
			CompilerGeneratedName				= 4,
			Public								= 8,
			NeedCast							= 0x10,
			NeedTypeName						= 0x20,
			DeprecatedError						= 0x40,
		}

		public MemberValueNodeInfo(DmdMemberInfo member, byte inheritanceLevel) {
			Member = member;
			InheritanceLevel = inheritanceLevel;
			Flags = GetFlags(member);
			// The caller initializes it later
			Name = default;
		}

		public void SetNeedCastAndNeedTypeName() => Flags |= MemberValueNodeInfoFlags.NeedCast | MemberValueNodeInfoFlags.NeedTypeName;

		static MemberValueNodeInfoFlags GetFlags(DmdMemberInfo member) {
			var flags = GetInfoFlags(member);

			// We don't check System.Runtime.CompilerServices.CompilerGeneratedAttribute. This matches VS (Roslyn) behavior.
			if (Microsoft.CodeAnalysis.ExpressionEvaluator.GeneratedMetadataNames.IsCompilerGenerated(member.Name))
				flags |= MemberValueNodeInfoFlags.CompilerGeneratedName;
			var cas = member.CustomAttributes;
			for (int i = 0; i < cas.Count; i++) {
				var ca = cas[i];
				var type = ca.AttributeType;
				switch (type.MetadataName) {
				case nameof(DebuggerBrowsableAttribute):
					if (type.MetadataNamespace == "System.Diagnostics") {
						if (ca.ConstructorArguments.Count == 1) {
							var arg = ca.ConstructorArguments[0];
							if (arg.Value is int) {
								flags &= ~MemberValueNodeInfoFlags.DebuggerBrowsableState_Mask;
								switch ((DebuggerBrowsableState)(int)arg.Value) {
								case DebuggerBrowsableState.Never:
									flags |= MemberValueNodeInfoFlags.DebuggerBrowsableState_Never;
									break;
								case DebuggerBrowsableState.RootHidden:
									flags |= MemberValueNodeInfoFlags.DebuggerBrowsableState_RootHidden;
									break;
								}
							}
						}
					}
					break;

				case nameof(ObsoleteAttribute):
					if (type.MetadataNamespace == "System") {
						if (ca.ConstructorArguments.Count == 2 && ca.ConstructorArguments[0].ArgumentType == member.AppDomain.System_String) {
							var arg = ca.ConstructorArguments[1];
							if (arg.Value is bool && (bool)arg.Value)
								flags |= MemberValueNodeInfoFlags.DeprecatedError;
						}
					}
					break;
				}
			}
			return flags;
		}

		static MemberValueNodeInfoFlags GetInfoFlags(DmdMemberInfo member) {
			Debug.Assert(member.MemberType == DmdMemberTypes.Field || member.MemberType == DmdMemberTypes.Property);
			MemberValueNodeInfoFlags res = MemberValueNodeInfoFlags.None;
			switch (member.MemberType) {
			case DmdMemberTypes.Field:
				var field = ((DmdFieldInfo)member);
				if (field.IsPublic)
					res |= MemberValueNodeInfoFlags.Public;
				else if ((field.IsPrivate || field.IsPrivateScope) && field.DeclaringType != field.ReflectedType)
					res |= MemberValueNodeInfoFlags.NeedCast;
				return res;

			case DmdMemberTypes.Property:
				var prop = (DmdPropertyInfo)member;
				var accessor = prop.GetGetMethod(DmdGetAccessorOptions.All) ?? prop.GetSetMethod(DmdGetAccessorOptions.All);
				if ((object)accessor != null) {
					if (accessor.IsPublic)
						res |= MemberValueNodeInfoFlags.Public;
					else if ((accessor.IsPrivate || accessor.IsPrivateScope) && accessor.DeclaringType != accessor.ReflectedType)
						res |= MemberValueNodeInfoFlags.NeedCast;
				}
				return res;

			default:
				throw new InvalidOperationException();
			}
		}

		public override string ToString() => Member.ToString();
	}
}
