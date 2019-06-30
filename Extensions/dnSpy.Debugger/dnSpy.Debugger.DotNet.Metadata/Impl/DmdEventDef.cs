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
using System.Linq;
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdEventDef : DmdEventInfo {
		sealed private protected override void YouCantDeriveFromThisClass() => throw new InvalidOperationException();

		public sealed override DmdModule Module => DeclaringType!.Module;
		public sealed override DmdType? DeclaringType { get; }
		public sealed override DmdType? ReflectedType { get; }
		public sealed override int MetadataToken => (int)(0x14000000 + rid);

		protected uint Rid => rid;
		readonly uint rid;

		protected DmdEventDef(uint rid, DmdType declaringType, DmdType reflectedType) {
			this.rid = rid;
			DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
			ReflectedType = reflectedType ?? throw new ArgumentNullException(nameof(reflectedType));
		}

		public sealed override DmdMethodInfo[] GetOtherMethods(DmdGetAccessorOptions options) {
			var f = ExtraFields;
			if (f.__otherMethods_DONT_USE is null)
				InitializeEventMethods();
			var otherMethods = f.__otherMethods_DONT_USE!;
			if (otherMethods.Count == 0)
				return Array.Empty<DmdMethodInfo>();
			if ((options & DmdGetAccessorOptions.All) != 0)
				return otherMethods.ToArray();
			var list = new List<DmdMethodInfo>(otherMethods.Count);
			foreach (var method in otherMethods) {
				var accessor = AccessorUtils.FilterAccessor(options, method);
				if (!(accessor is null))
					list.Add(accessor);
			}
			return list.Count == 0 ? Array.Empty<DmdMethodInfo>() : list.ToArray();
		}

		public sealed override DmdMethodInfo? GetAddMethod(DmdGetAccessorOptions options) {
			var f = ExtraFields;
			if (f.__otherMethods_DONT_USE is null)
				InitializeEventMethods();
			return AccessorUtils.FilterAccessor(options, f.__addMethod_DONT_USE!);
		}

		public sealed override DmdMethodInfo? GetRemoveMethod(DmdGetAccessorOptions options) {
			var f = ExtraFields;
			if (f.__otherMethods_DONT_USE is null)
				InitializeEventMethods();
			return AccessorUtils.FilterAccessor(options, f.__removeMethod_DONT_USE!);
		}

		public sealed override DmdMethodInfo? GetRaiseMethod(DmdGetAccessorOptions options) {
			var f = ExtraFields;
			if (f.__otherMethods_DONT_USE is null)
				InitializeEventMethods();
			return AccessorUtils.FilterAccessor(options, f.__raiseMethod_DONT_USE!);
		}

		void InitializeEventMethods() {
			var f = ExtraFields;
			if (!(f.__otherMethods_DONT_USE is null))
				return;
			GetMethods(out var addMethod, out var removeMethod, out var raiseMethod, out var otherMethods);
			lock (LockObject) {
				if (f.__otherMethods_DONT_USE is null) {
					f.__addMethod_DONT_USE = addMethod;
					f.__removeMethod_DONT_USE = removeMethod;
					f.__raiseMethod_DONT_USE = raiseMethod;
					f.__otherMethods_DONT_USE = ReadOnlyCollectionHelpers.Create(otherMethods);
				}
			}
		}
		protected abstract void GetMethods(out DmdMethodInfo? addMethod, out DmdMethodInfo? removeMethod, out DmdMethodInfo? raiseMethod, out DmdMethodInfo[]? otherMethods);

		public sealed override ReadOnlyCollection<DmdCustomAttributeData> GetCustomAttributesData() {
			var f = ExtraFields;
			if (!(f.__customAttributes_DONT_USE is null))
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
			public volatile DmdMethodInfo? __addMethod_DONT_USE;
			public volatile DmdMethodInfo? __removeMethod_DONT_USE;
			public volatile DmdMethodInfo? __raiseMethod_DONT_USE;
			public volatile ReadOnlyCollection<DmdMethodInfo>? __otherMethods_DONT_USE;
			public volatile ReadOnlyCollection<DmdCustomAttributeData>? __customAttributes_DONT_USE;
		}
	}
}
