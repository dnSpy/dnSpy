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
	sealed class DmdPropertyParameter : DmdParameterInfoBase {
		public override DmdType ParameterType => parameter.ParameterType;
		public override string? Name => parameter.Name;
		public override bool HasDefaultValue => parameter.HasDefaultValue;
		public override object? RawDefaultValue => parameter.RawDefaultValue;
		public override int Position => parameter.Position;
		public override DmdParameterAttributes Attributes => parameter.Attributes;
		public override DmdMemberInfo Member { get; }
		public override int MetadataToken => parameter.MetadataToken;

		readonly DmdParameterInfo parameter;

		public DmdPropertyParameter(DmdPropertyDef property, DmdParameterInfo parameter) {
			Member = property ?? throw new ArgumentNullException(nameof(property));
			this.parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
		}

		public override ReadOnlyCollection<DmdCustomAttributeData> GetCustomAttributesData() => parameter.GetCustomAttributesData();
	}
}
