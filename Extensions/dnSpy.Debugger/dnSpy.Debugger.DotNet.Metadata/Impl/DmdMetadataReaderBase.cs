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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdMetadataReaderBase : DmdMetadataReader {
		static DmdMemberInfo TryResolve(DmdMemberInfo member) => member.ResolveMemberNoThrow() ?? member;
		static DmdType TryResolve(DmdType member) => member.ResolveNoThrow() ?? member;
		static DmdFieldInfo TryResolve(DmdFieldInfo member) => member.ResolveNoThrow() ?? member;
		static DmdMethodBase TryResolve(DmdMethodBase member) => member.ResolveMethodBaseNoThrow() ?? member;

		public override DmdMethodBase ResolveMethod(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, bool throwOnError) {
			uint rid = (uint)(metadataToken & 0x00FFFFFF);
			switch ((uint)metadataToken >> 24) {
			case 0x06:
				var method = ResolveMethodDef(rid);
				if ((object)method != null)
					return method;
				break;

			case 0x0A:
				var mr = ResolveMemberRef(rid, genericTypeArguments);
				if ((object)mr != null) {
					if (mr is DmdMethodBase methodRef)
						return TryResolve(methodRef);
					if (throwOnError)
						throw new ArgumentException();
				}
				break;

			case 0x2B:
				var methodSpec = ResolveMethodSpec(rid, genericTypeArguments, genericMethodArguments);
				if ((object)methodSpec != null)
					return TryResolve(methodSpec);
				break;
			}

			if (throwOnError)
				throw new ArgumentOutOfRangeException(nameof(metadataToken));
			return null;
		}

		public override DmdFieldInfo ResolveField(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, bool throwOnError) {
			uint rid = (uint)(metadataToken & 0x00FFFFFF);
			switch ((uint)metadataToken >> 24) {
			case 0x04:
				var field = ResolveFieldDef(rid);
				if ((object)field != null)
					return field;
				break;

			case 0x0A:
				var memberRef = ResolveMemberRef(rid, genericTypeArguments);
				if ((object)memberRef != null) {
					if (memberRef is DmdFieldInfo fieldRef)
						return TryResolve(fieldRef);
					if (throwOnError)
						throw new ArgumentException();
				}
				break;
			}

			if (throwOnError)
				throw new ArgumentOutOfRangeException(nameof(metadataToken));
			return null;
		}

		public override DmdType ResolveType(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, bool throwOnError) {
			uint rid = (uint)(metadataToken & 0x00FFFFFF);
			switch ((uint)metadataToken >> 24) {
			case 0x01:
				var typeRef = ResolveTypeRef(rid);
				if ((object)typeRef != null)
					return TryResolve(typeRef);
				break;

			case 0x02:
				var typeDef = ResolveTypeDef(rid);
				if ((object)typeDef != null)
					return typeDef;
				break;

			case 0x1B:
				var typeSpec = ResolveTypeSpec(rid, genericTypeArguments);
				if ((object)typeSpec != null)
					return TryResolve(typeSpec);
				break;

			case 0x27:
				var exportedType = ResolveExportedType(rid);
				if ((object)exportedType != null)
					return exportedType;// Don't try to resolve it, callers want the actual reference
				break;
			}

			if (throwOnError)
				throw new ArgumentOutOfRangeException(nameof(metadataToken));
			return null;
		}

		public override DmdMemberInfo ResolveMember(int metadataToken, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, bool throwOnError) {
			uint rid = (uint)(metadataToken & 0x00FFFFFF);
			switch ((uint)metadataToken >> 24) {
			case 0x01:
				var typeRef = ResolveTypeRef(rid);
				if ((object)typeRef != null)
					return TryResolve(typeRef);
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
				var memberRef = ResolveMemberRef(rid, genericTypeArguments);
				if ((object)memberRef != null)
					return TryResolve(memberRef);
				break;

			case 0x1B:
				var typeSpec = ResolveTypeSpec(rid, genericTypeArguments);
				if ((object)typeSpec != null)
					return TryResolve(typeSpec);
				break;

			case 0x27:
				var exportedType = ResolveExportedType(rid);
				if ((object)exportedType != null)
					return exportedType;// Don't try to resolve it, callers want the actual reference
				break;

			case 0x2B:
				var methodSpec = ResolveMethodSpec(rid, genericTypeArguments, genericMethodArguments);
				if ((object)methodSpec != null)
					return TryResolve(methodSpec);
				break;
			}

			if (throwOnError)
				throw new ArgumentOutOfRangeException(nameof(metadataToken));
			return null;
		}

		protected abstract DmdType ResolveTypeRef(uint rid);
		protected abstract DmdType ResolveTypeDef(uint rid);
		protected abstract DmdFieldInfo ResolveFieldDef(uint rid);
		protected abstract DmdMethodBase ResolveMethodDef(uint rid);
		protected abstract DmdMemberInfo ResolveMemberRef(uint rid, IList<DmdType> genericTypeArguments);
		protected abstract DmdEventInfo ResolveEventDef(uint rid);
		protected abstract DmdPropertyInfo ResolvePropertyDef(uint rid);
		protected abstract DmdType ResolveTypeSpec(uint rid, IList<DmdType> genericTypeArguments);
		protected abstract DmdTypeRef ResolveExportedType(uint rid);
		protected abstract DmdMethodBase ResolveMethodSpec(uint rid, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments);

		public override byte[] ResolveSignature(int metadataToken) {
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

		public override string ResolveString(int metadataToken) {
			if (((uint)metadataToken >> 24) != 0x70)
				throw new ArgumentOutOfRangeException(nameof(metadataToken));
			uint offset = (uint)metadataToken & 0x00FFFFFF;
			if (offset == 0)
				return string.Empty;
			return ResolveStringCore(offset) ?? throw new ArgumentOutOfRangeException(nameof(offset));
		}

		protected abstract string ResolveStringCore(uint offset);
	}
}
