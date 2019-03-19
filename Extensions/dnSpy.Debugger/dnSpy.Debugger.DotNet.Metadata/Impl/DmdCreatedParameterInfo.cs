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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdCreatedParameterInfo : DmdParameterInfoBase {
		public override DmdType ParameterType { get; }
		public override string Name => originalParameter.Name;
		public override bool HasDefaultValue => originalParameter.HasDefaultValue;
		public override object RawDefaultValue => originalParameter.RawDefaultValue;
		public override int Position => originalParameter.Position;
		public override DmdParameterAttributes Attributes => originalParameter.Attributes;
		public override DmdMemberInfo Member { get; }
		public override int MetadataToken => originalParameter.MetadataToken;

		readonly DmdParameterInfo originalParameter;

		public DmdCreatedParameterInfo(DmdMemberInfo member, DmdParameterInfo originalParameter, DmdType parameterType) {
			Member = member ?? throw new ArgumentNullException(nameof(member));
			this.originalParameter = originalParameter ?? throw new ArgumentNullException(nameof(originalParameter));
			ParameterType = parameterType ?? throw new ArgumentNullException(nameof(parameterType));
		}

		public override ReadOnlyCollection<DmdCustomAttributeData> GetCustomAttributesData() => originalParameter.GetCustomAttributesData();
	}
}
