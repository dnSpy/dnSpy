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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdMetadataReaderBase : DmdMetadataReader {
		protected const bool resolveTypes = true;

		static DmdMemberInfo TryResolve(DmdMemberInfo member, DmdResolveOptions options) => (options & DmdResolveOptions.NoTryResolveRefs) != 0 ? member : member.ResolveMemberNoThrow() ?? member;
		static DmdType TryResolve(DmdType member, DmdResolveOptions options) => (options & DmdResolveOptions.NoTryResolveRefs) != 0 ? member : member.ResolveNoThrow() ?? member;
		static DmdFieldInfo TryResolve(DmdFieldInfo member, DmdResolveOptions options) => (options & DmdResolveOptions.NoTryResolveRefs) != 0 ? member : member.ResolveNoThrow() ?? member;
		static DmdMethodBase TryResolve(DmdMethodBase member, DmdResolveOptions options) => (options & DmdResolveOptions.NoTryResolveRefs) != 0 ? member : member.ResolveMethodBaseNoThrow() ?? member;

		public sealed override DmdMethodBase ResolveMethod(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) {
			uint rid = (uint)(metadataToken & 0x00FFFFFF);
			switch ((uint)metadataToken >> 24) {
			case 0x06:
				var method = ResolveMethodDef(rid);
				if ((object)method != null)
					return method;
				break;

			case 0x0A:
				var mr = ResolveMemberRef(rid, genericTypeArguments, genericMethodArguments);
				if ((object)mr != null) {
					if (mr is DmdMethodBase methodRef)
						return TryResolve(methodRef, options);
					if ((options & DmdResolveOptions.ThrowOnError) != 0)
						throw new ArgumentException();
				}
				break;

			case 0x2B:
				var methodSpec = ResolveMethodSpec(rid, genericTypeArguments, genericMethodArguments);
				if ((object)methodSpec != null)
					return TryResolve(methodSpec, options);
				break;
			}

			if ((options & DmdResolveOptions.ThrowOnError) != 0)
				throw new ArgumentOutOfRangeException(nameof(metadataToken));
			return null;
		}

		public sealed override DmdFieldInfo ResolveField(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) {
			uint rid = (uint)(metadataToken & 0x00FFFFFF);
			switch ((uint)metadataToken >> 24) {
			case 0x04:
				var field = ResolveFieldDef(rid);
				if ((object)field != null)
					return field;
				break;

			case 0x0A:
				var memberRef = ResolveMemberRef(rid, genericTypeArguments, genericMethodArguments);
				if ((object)memberRef != null) {
					if (memberRef is DmdFieldInfo fieldRef)
						return TryResolve(fieldRef, options);
					if ((options & DmdResolveOptions.ThrowOnError) != 0)
						throw new ArgumentException();
				}
				break;
			}

			if ((options & DmdResolveOptions.ThrowOnError) != 0)
				throw new ArgumentOutOfRangeException(nameof(metadataToken));
			return null;
		}

		public sealed override DmdType ResolveType(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) {
			uint rid = (uint)(metadataToken & 0x00FFFFFF);
			switch ((uint)metadataToken >> 24) {
			case 0x01:
				var typeRef = ResolveTypeRef(rid);
				if ((object)typeRef != null)
					return TryResolve(typeRef, options);
				break;

			case 0x02:
				var typeDef = ResolveTypeDef(rid);
				if ((object)typeDef != null)
					return typeDef;
				break;

			case 0x1B:
				var typeSpec = ResolveTypeSpec(rid, genericTypeArguments, genericMethodArguments);
				if ((object)typeSpec != null)
					return TryResolve(typeSpec, options);
				break;

			case 0x27:
				var exportedType = ResolveExportedType(rid);
				if ((object)exportedType != null)
					return exportedType;// Don't try to resolve it, callers want the actual reference
				break;
			}

			if ((options & DmdResolveOptions.ThrowOnError) != 0)
				throw new ArgumentOutOfRangeException(nameof(metadataToken));
			return null;
		}

		public sealed override DmdMemberInfo ResolveMember(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) {
			uint rid = (uint)(metadataToken & 0x00FFFFFF);
			switch ((uint)metadataToken >> 24) {
			case 0x01:
				var typeRef = ResolveTypeRef(rid);
				if ((object)typeRef != null)
					return TryResolve(typeRef, options);
				break;

			case 0x02:
				var typeDef = ResolveTypeDef(rid);
				if ((object)typeDef != null)
					return typeDef;
				break;

			case 0x04:
				var field = ResolveFieldDef(rid);
				if ((object)field != null)
					return field;
				break;

			case 0x06:
				var method = ResolveMethodDef(rid);
				if ((object)method != null)
					return method;
				break;

			case 0x0A:
				var memberRef = ResolveMemberRef(rid, genericTypeArguments, genericMethodArguments);
				if ((object)memberRef != null)
					return TryResolve(memberRef, options);
				break;

			case 0x1B:
				var typeSpec = ResolveTypeSpec(rid, genericTypeArguments, genericMethodArguments);
				if ((object)typeSpec != null)
					return TryResolve(typeSpec, options);
				break;

			case 0x27:
				var exportedType = ResolveExportedType(rid);
				if ((object)exportedType != null)
					return exportedType;// Don't try to resolve it, callers want the actual reference
				break;

			case 0x2B:
				var methodSpec = ResolveMethodSpec(rid, genericTypeArguments, genericMethodArguments);
				if ((object)methodSpec != null)
					return TryResolve(methodSpec, options);
				break;
			}

			if ((options & DmdResolveOptions.ThrowOnError) != 0)
				throw new ArgumentOutOfRangeException(nameof(metadataToken));
			return null;
		}

		public sealed override DmdMethodSignature ResolveMethodSignature(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, DmdResolveOptions options) {
			uint rid = (uint)(metadataToken & 0x00FFFFFF);
			switch ((uint)metadataToken >> 24) {
			case 0x11:
				var methodSig = ResolveMethodSignature(rid, genericTypeArguments, genericMethodArguments);
				if ((object)methodSig != null)
					return methodSig;
				break;
			}

			if ((options & DmdResolveOptions.ThrowOnError) != 0)
				throw new ArgumentOutOfRangeException(nameof(metadataToken));
			return null;
		}

		protected abstract DmdTypeRef ResolveTypeRef(uint rid);
		protected abstract DmdTypeDef ResolveTypeDef(uint rid);
		protected abstract DmdFieldDef ResolveFieldDef(uint rid);
		protected abstract DmdMethodBase ResolveMethodDef(uint rid);
		protected abstract DmdMemberInfo ResolveMemberRef(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments);
		protected abstract DmdEventDef ResolveEventDef(uint rid);
		protected abstract DmdPropertyDef ResolvePropertyDef(uint rid);
		protected abstract DmdType ResolveTypeSpec(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments);
		protected abstract DmdTypeRef ResolveExportedType(uint rid);
		protected abstract DmdMethodBase ResolveMethodSpec(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments);
		protected abstract DmdMethodSignature ResolveMethodSignature(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments);

		public sealed override byte[] ResolveSignature(int metadataToken) {
			byte[] res;
			uint rid = (uint)(metadataToken & 0x00FFFFFF);
			switch ((uint)metadataToken >> 24) {
			case 0x04: res = ResolveFieldSignature(rid); break;
			case 0x06: res = ResolveMethodSignature(rid); break;
			case 0x0A: res = ResolveMemberRefSignature(rid); break;
			case 0x11: res = ResolveStandAloneSigSignature(rid); break;
			case 0x1B: res = ResolveTypeSpecSignature(rid); break;
			case 0x2B: res = ResolveMethodSpecSignature(rid); break;
			default: res = null; break;
			}
			return res ?? throw new ArgumentOutOfRangeException(nameof(metadataToken));
		}

		protected abstract byte[] ResolveFieldSignature(uint rid);
		protected abstract byte[] ResolveMethodSignature(uint rid);
		protected abstract byte[] ResolveMemberRefSignature(uint rid);
		protected abstract byte[] ResolveStandAloneSigSignature(uint rid);
		protected abstract byte[] ResolveTypeSpecSignature(uint rid);
		protected abstract byte[] ResolveMethodSpecSignature(uint rid);

		public sealed override string ResolveString(int metadataToken) {
			if (((uint)metadataToken >> 24) != 0x70)
				throw new ArgumentOutOfRangeException(nameof(metadataToken));
			uint offset = (uint)metadataToken & 0x00FFFFFF;
			if (offset == 0)
				return string.Empty;
			return ResolveStringCore(offset) ?? throw new ArgumentOutOfRangeException(nameof(offset));
		}

		protected abstract string ResolveStringCore(uint offset);

		public sealed override DmdCustomAttributeData[] ReadCustomAttributes(int metadataToken) {
			uint rid = (uint)(metadataToken & 0x00FFFFFF);
			switch ((uint)metadataToken >> 24) {
			case 0x00: return ReadModuleCustomAttributes(rid);
			case 0x02: return ReadTypeDefCustomAttributes(rid);
			case 0x04: return ReadFieldCustomAttributes(rid);
			case 0x06: return ReadMethodCustomAttributes(rid);
			case 0x08: return ReadParamCustomAttributes(rid);
			case 0x14: return ReadEventCustomAttributes(rid);
			case 0x17: return ReadPropertyCustomAttributes(rid);
			case 0x20: return ReadAssemblyCustomAttributes(rid);
			default: throw new ArgumentOutOfRangeException(nameof(metadataToken));
			}
		}

		protected abstract DmdCustomAttributeData[] ReadAssemblyCustomAttributes(uint rid);
		protected abstract DmdCustomAttributeData[] ReadModuleCustomAttributes(uint rid);
		protected abstract DmdCustomAttributeData[] ReadTypeDefCustomAttributes(uint rid);
		protected abstract DmdCustomAttributeData[] ReadFieldCustomAttributes(uint rid);
		protected abstract DmdCustomAttributeData[] ReadMethodCustomAttributes(uint rid);
		protected abstract DmdCustomAttributeData[] ReadParamCustomAttributes(uint rid);
		protected abstract DmdCustomAttributeData[] ReadEventCustomAttributes(uint rid);
		protected abstract DmdCustomAttributeData[] ReadPropertyCustomAttributes(uint rid);

		public sealed override DmdCustomAttributeData[] ReadSecurityAttributes(int metadataToken) {
			uint rid = (uint)(metadataToken & 0x00FFFFFF);
			switch ((uint)metadataToken >> 24) {
			case 0x02: return ReadTypeDefSecurityAttributes(rid);
			case 0x06: return ReadMethodSecurityAttributes(rid);
			case 0x20: return ReadAssemblySecurityAttributes(rid);
			default: throw new ArgumentOutOfRangeException(nameof(metadataToken));
			}
		}

		protected abstract DmdCustomAttributeData[] ReadAssemblySecurityAttributes(uint rid);
		protected abstract DmdCustomAttributeData[] ReadTypeDefSecurityAttributes(uint rid);
		protected abstract DmdCustomAttributeData[] ReadMethodSecurityAttributes(uint rid);

		public override event EventHandler<DmdTypesUpdatedEventArgs> TypesUpdated { add { } remove { } }
	}
}
