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
using System.Collections.ObjectModel;
using System.Linq;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdEventDef : DmdEventInfo {
		internal sealed override void YouCantDeriveFromThisClass() => throw new InvalidOperationException();

		public sealed override DmdModule Module => DeclaringType.Module;
		public sealed override DmdType DeclaringType { get; }
		public sealed override DmdType ReflectedType { get; }
		public sealed override int MetadataToken => (int)(0x14000000 + rid);

		protected uint Rid => rid;
		readonly uint rid;

		protected DmdEventDef(uint rid, DmdTypeDef declaringType, DmdType reflectedType) {
			this.rid = rid;
			DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
			ReflectedType = reflectedType ?? throw new ArgumentNullException(nameof(reflectedType));
		}

		public sealed override DmdMethodInfo[] GetOtherMethods(bool nonPublic) {
			if (__otherMethods_DONT_USE == null)
				InitializeEventMethods();
			var otherMethods = __otherMethods_DONT_USE;
			if (otherMethods.Count == 0)
				return Array.Empty<DmdMethodInfo>();
			if (nonPublic)
				return otherMethods.ToArray();
			var list = new List<DmdMethodInfo>(otherMethods.Count);
			foreach (var method in otherMethods) {
				if (method.IsPublic)
					list.Add(method);
			}
			return list.Count == 0 ? Array.Empty<DmdMethodInfo>() : list.ToArray();
		}

		public sealed override DmdMethodInfo GetAddMethod(bool nonPublic) {
			if (__otherMethods_DONT_USE == null)
				InitializeEventMethods();
			if (nonPublic)
				return __addMethod_DONT_USE;
			return __addMethod_DONT_USE?.IsPublic == true ? __addMethod_DONT_USE : null;
		}

		public sealed override DmdMethodInfo GetRemoveMethod(bool nonPublic) {
			if (__otherMethods_DONT_USE == null)
				InitializeEventMethods();
			if (nonPublic)
				return __removeMethod_DONT_USE;
			return __removeMethod_DONT_USE?.IsPublic == true ? __removeMethod_DONT_USE : null;
		}

		public sealed override DmdMethodInfo GetRaiseMethod(bool nonPublic) {
			if (__otherMethods_DONT_USE == null)
				InitializeEventMethods();
			if (nonPublic)
				return __raiseMethod_DONT_USE;
			return __raiseMethod_DONT_USE?.IsPublic == true ? __raiseMethod_DONT_USE : null;
		}

		void InitializeEventMethods() {
			if (__otherMethods_DONT_USE != null)
				return;
			lock (LockObject) {
				if (__otherMethods_DONT_USE != null)
					return;
				GetMethods(out var addMethod, out var removeMethod, out var raiseMethod, out var otherMethods);
				__addMethod_DONT_USE = addMethod;
				__removeMethod_DONT_USE = removeMethod;
				__raiseMethod_DONT_USE = raiseMethod;
				__otherMethods_DONT_USE = ReadOnlyCollectionHelpers.Create(otherMethods);
			}
		}
		DmdMethodInfo __addMethod_DONT_USE;
		DmdMethodInfo __removeMethod_DONT_USE;
		DmdMethodInfo __raiseMethod_DONT_USE;
		ReadOnlyCollection<DmdMethodInfo> __otherMethods_DONT_USE;
		protected abstract void GetMethods(out DmdMethodInfo addMethod, out DmdMethodInfo removeMethod, out DmdMethodInfo raiseMethod, out DmdMethodInfo[] otherMethods);

		public sealed override IList<DmdCustomAttributeData> GetCustomAttributesData() {
			if (__customAttributes_DONT_USE != null)
				return __customAttributes_DONT_USE;
			lock (LockObject) {
				if (__customAttributes_DONT_USE != null)
					return __customAttributes_DONT_USE;
				var info = CreateCustomAttributes();
				__customAttributes_DONT_USE = CustomAttributesHelper.AddPseudoCustomAttributes(this, info);
				return __customAttributes_DONT_USE;
			}
		}
		ReadOnlyCollection<DmdCustomAttributeData> __customAttributes_DONT_USE;

		protected abstract DmdCustomAttributeData[] CreateCustomAttributes();
	}
}
