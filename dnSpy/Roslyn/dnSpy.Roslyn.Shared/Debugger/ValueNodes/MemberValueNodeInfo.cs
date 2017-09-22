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
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	struct MemberValueNodeInfo {
		public readonly DmdMemberInfo Member;

		// It gets initialized later so it can't be readonly
		public DbgDotNetText Name;

		/// <summary>
		/// Most derived class is level 0, and least derived class (<see cref="object"/>) has the highest level
		/// </summary>
		public readonly byte InheritanceLevel;

		readonly MemberValueNodeInfoFlags Flags;
		public bool HasDebuggerBrowsableState_Never => (Flags & MemberValueNodeInfoFlags.DebuggerBrowsableState_Mask) == MemberValueNodeInfoFlags.DebuggerBrowsableState_Never;
		public bool HasDebuggerBrowsableState_RootHidden => (Flags & MemberValueNodeInfoFlags.DebuggerBrowsableState_Mask) == MemberValueNodeInfoFlags.DebuggerBrowsableState_RootHidden;
		public bool HasCompilerGeneratedAttribute => (Flags & MemberValueNodeInfoFlags.CompilerGeneratedAttribute) != 0;
		public bool IsPublic => (Flags & MemberValueNodeInfoFlags.Public) != 0;

		[Flags]
		enum MemberValueNodeInfoFlags : byte {
			None = 0,
			DebuggerBrowsableState_Mask			= 3,
			DebuggerBrowsableState_Default		= 0,
			DebuggerBrowsableState_Never		= 1,
			DebuggerBrowsableState_RootHidden	= 2,
			CompilerGeneratedAttribute			= 4,
			Public								= 8,
		}

		public MemberValueNodeInfo(DmdMemberInfo member, byte inheritanceLevel) {
			Member = member;
			InheritanceLevel = inheritanceLevel;
			Flags = GetFlags(member);
			// The caller initializes it later
			Name = default;
		}

		static MemberValueNodeInfoFlags GetFlags(DmdMemberInfo member) {
			var flags = MemberValueNodeInfoFlags.None;
			if (IsPublicInternal(member))
				flags |= MemberValueNodeInfoFlags.Public;
			var cas = member.CustomAttributes;
			for (int i = 0; i < cas.Count; i++) {
				var ca = cas[i];
				var type = ca.AttributeType;
				switch (type.MetadataName) {
				case "CompilerGeneratedAttribute":
					if (type.MetadataNamespace == "System.Runtime.CompilerServices" && member.MemberType == DmdMemberTypes.Field)
						flags |= MemberValueNodeInfoFlags.CompilerGeneratedAttribute;
					break;

				case "DebuggerBrowsableAttribute":
					if (type.MetadataNamespace == "System.Diagnostics") {
						if (ca.ConstructorArguments.Count == 1) {
							var arg = ca.ConstructorArguments[0];
							if (arg.Value is int) {
								flags = flags & ~MemberValueNodeInfoFlags.DebuggerBrowsableState_Mask;
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
				}
			}
			return flags;
		}

		static bool IsPublicInternal(DmdMemberInfo member) {
			Debug.Assert(member.MemberType == DmdMemberTypes.Field || member.MemberType == DmdMemberTypes.Property);
			switch (member.MemberType) {
			case DmdMemberTypes.Field:
				return ((DmdFieldInfo)member).IsPublic;

			case DmdMemberTypes.Property:
				var prop = (DmdPropertyInfo)member;
				var accessor = prop.GetGetMethod(DmdGetAccessorOptions.All) ?? prop.GetSetMethod(DmdGetAccessorOptions.All);
				return accessor?.IsPublic == true;

			default:
				throw new InvalidOperationException();
			}
		}

		public override string ToString() => Member.ToString();
	}
}
