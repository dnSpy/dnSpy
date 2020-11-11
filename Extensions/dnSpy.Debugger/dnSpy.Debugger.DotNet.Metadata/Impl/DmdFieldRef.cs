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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdFieldRef : DmdFieldInfoBase {
		public override DmdAppDomain AppDomain => declaringTypeRef.AppDomain;
		public override string Name { get; }
		public override DmdType FieldType { get; }
		public override bool IsMetadataReference => true;
		public override DmdType? DeclaringType => __resolvedField_DONT_USE?.DeclaringType ?? declaringTypeRef;
		public override DmdType? ReflectedType => DeclaringType;
		public override int MetadataToken => ResolvedField.MetadataToken;
		public override DmdFieldAttributes Attributes => ResolvedField.Attributes;
		public override uint FieldRVA => ResolvedField.FieldRVA;

		DmdFieldDef ResolvedField => GetResolvedField(throwOnError: true)!;
		DmdFieldDef? GetResolvedField(bool throwOnError) {
			if (__resolvedField_DONT_USE is not null)
				return __resolvedField_DONT_USE;

			DmdFieldDef? newResolvedField = null;
			var declType = declaringTypeRef.Resolve(throwOnError);
			if (declType is not null) {
				var nonGenericInstDeclType = declType.IsGenericType ? declType.GetGenericTypeDefinition() : declType;
				var nonGenericInstDeclTypeField = (DmdFieldDef?)nonGenericInstDeclType?.GetField(Name, rawFieldType, throwOnError: false);
				if (nonGenericInstDeclTypeField is not null) {
					newResolvedField = (object?)nonGenericInstDeclTypeField.DeclaringType == declType ?
						nonGenericInstDeclTypeField :
						(DmdFieldDef?)declType.GetField(nonGenericInstDeclTypeField.Module, nonGenericInstDeclTypeField.MetadataToken);
					Debug2.Assert(newResolvedField is not null);
				}
			}
			if (newResolvedField is not null) {
				Interlocked.CompareExchange(ref __resolvedField_DONT_USE, newResolvedField, null);
				Debug.Assert(DmdMemberInfoEqualityComparer.DefaultMember.Equals(__resolvedField_DONT_USE!.ReflectedType, declaringTypeRef));
				return __resolvedField_DONT_USE;
			}
			if (throwOnError)
				throw new FieldResolveException(this);
			return null;
		}
		volatile DmdFieldDef? __resolvedField_DONT_USE;

		readonly DmdType declaringTypeRef;
		readonly DmdType rawFieldType;

		public DmdFieldRef(DmdType declaringTypeRef, string name, DmdType rawFieldType, DmdType fieldType) {
			this.declaringTypeRef = declaringTypeRef ?? throw new ArgumentNullException(nameof(declaringTypeRef));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			this.rawFieldType = rawFieldType ?? throw new ArgumentNullException(nameof(rawFieldType));
			FieldType = fieldType ?? throw new ArgumentNullException(nameof(fieldType));
		}

		public override DmdFieldInfo? Resolve(bool throwOnError) => GetResolvedField(throwOnError);
		public override object? GetRawConstantValue() => ResolvedField.GetRawConstantValue();
		public override ReadOnlyCollection<DmdCustomAttributeData> GetCustomAttributesData() => ResolvedField.GetCustomAttributesData();
	}
}
