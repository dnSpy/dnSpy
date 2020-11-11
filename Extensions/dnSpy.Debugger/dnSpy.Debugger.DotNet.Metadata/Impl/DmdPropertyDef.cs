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
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdPropertyDef : DmdPropertyInfo {
		sealed private protected override void YouCantDeriveFromThisClass() => throw new InvalidOperationException();

		public sealed override DmdModule Module => DeclaringType!.Module;
		public sealed override DmdType? DeclaringType { get; }
		public sealed override DmdType? ReflectedType { get; }
		public sealed override int MetadataToken => (int)(0x17000000 + rid);
		public sealed override DmdType PropertyType => GetMethodSignature().ReturnType;

		protected uint Rid => rid;
		readonly uint rid;

		protected DmdPropertyDef(uint rid, DmdType declaringType, DmdType reflectedType) {
			this.rid = rid;
			DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
			ReflectedType = reflectedType ?? throw new ArgumentNullException(nameof(reflectedType));
		}

		public sealed override DmdMethodInfo[] GetAccessors(DmdGetAccessorOptions options) {
			var f = ExtraFields;
			if (f.__otherMethods_DONT_USE is null)
				InitializePropertyMethods();
			var list = new List<DmdMethodInfo>();
			var accessor = AccessorUtils.FilterAccessor(options, f.__getMethod_DONT_USE!);
			if (accessor is not null)
				list.Add(accessor);
			accessor = AccessorUtils.FilterAccessor(options, f.__setMethod_DONT_USE!);
			if (accessor is not null)
				list.Add(accessor);
			foreach (var method in f.__otherMethods_DONT_USE!) {
				accessor = AccessorUtils.FilterAccessor(options, method);
				if (accessor is not null)
					list.Add(accessor);
			}
			return list.ToArray();
		}

		public sealed override DmdMethodInfo? GetGetMethod(DmdGetAccessorOptions options) {
			var f = ExtraFields;
			if (f.__otherMethods_DONT_USE is null)
				InitializePropertyMethods();
			return AccessorUtils.FilterAccessor(options, f.__getMethod_DONT_USE!);
		}

		public sealed override DmdMethodInfo? GetSetMethod(DmdGetAccessorOptions options) {
			var f = ExtraFields;
			if (f.__otherMethods_DONT_USE is null)
				InitializePropertyMethods();
			return AccessorUtils.FilterAccessor(options, f.__setMethod_DONT_USE!);
		}

		void InitializePropertyMethods() {
			var f = ExtraFields;
			if (f.__otherMethods_DONT_USE is not null)
				return;
			GetMethods(out var getMethod, out var setMethod, out var otherMethods);
			lock (LockObject) {
				if (f.__otherMethods_DONT_USE is null) {
					f.__getMethod_DONT_USE = getMethod;
					f.__setMethod_DONT_USE = setMethod;
					f.__otherMethods_DONT_USE = ReadOnlyCollectionHelpers.Create(otherMethods);
				}
			}
		}
		protected abstract void GetMethods(out DmdMethodInfo? getMethod, out DmdMethodInfo? setMethod, out DmdMethodInfo[]? otherMethods);

		public sealed override ReadOnlyCollection<DmdParameterInfo> GetIndexParameters() {
			var f = ExtraFields;
			if (f.__indexParameters_DONT_USE is null)
				InitializeIndexParameters();
			return f.__indexParameters_DONT_USE!;
		}

		void InitializeIndexParameters() {
			var f = ExtraFields;
			if (f.__indexParameters_DONT_USE is not null)
				return;
			var info = CreateIndexParameters();
			Interlocked.CompareExchange(ref f.__indexParameters_DONT_USE, ReadOnlyCollectionHelpers.Create(info), null);
		}

		DmdParameterInfo[] CreateIndexParameters() {
			if (GetGetMethod(DmdGetAccessorOptions.All) is DmdMethodInfo getMethod) {
				var ps = getMethod.GetParameters();
				var res = new DmdParameterInfo[ps.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DmdPropertyParameter(this, ps[i]);
				return res;
			}
			else if (GetSetMethod(DmdGetAccessorOptions.All) is DmdMethodInfo setMethod) {
				var ps = setMethod.GetParameters();
				if (ps.Count == 0)
					return Array.Empty<DmdParameterInfo>();
				var res = new DmdParameterInfo[ps.Count - 1];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DmdPropertyParameter(this, ps[i]);
				return res;
			}
			else {
				var types = GetMethodSignature().GetParameterTypes();
				var res = new DmdParameterInfo[types.Count];
				for (int i = 0; i < res.Length; i++)
					res[i] = new DmdCreatedParameterDef(this, i, types[i]);
				return res;
			}
		}

		public sealed override ReadOnlyCollection<DmdCustomAttributeData> GetCustomAttributesData() {
			var f = ExtraFields;
			if (f.__customAttributes_DONT_USE is not null)
				return f.__customAttributes_DONT_USE;
			var info = CreateCustomAttributes();
			var newCAs = CustomAttributesHelper.AddPseudoCustomAttributes(this, info);
			Interlocked.CompareExchange(ref f.__customAttributes_DONT_USE, newCAs, null);
			return f.__customAttributes_DONT_USE!;
		}

		protected abstract DmdCustomAttributeData[] CreateCustomAttributes();

		ExtraFieldsImpl ExtraFields {
			get {
				if (__extraFields_DONT_USE is ExtraFieldsImpl f)
					return f;
				Interlocked.CompareExchange(ref __extraFields_DONT_USE, new ExtraFieldsImpl(), null);
				return __extraFields_DONT_USE!;
			}
		}
		volatile ExtraFieldsImpl? __extraFields_DONT_USE;

		// Most of the fields aren't used so we alloc them when needed
		sealed class ExtraFieldsImpl {
			public volatile ReadOnlyCollection<DmdParameterInfo>? __indexParameters_DONT_USE;
			public volatile DmdMethodInfo? __getMethod_DONT_USE;
			public volatile DmdMethodInfo? __setMethod_DONT_USE;
			public volatile ReadOnlyCollection<DmdMethodInfo>? __otherMethods_DONT_USE;
			public volatile ReadOnlyCollection<DmdCustomAttributeData>? __customAttributes_DONT_USE;
		}
	}
}
