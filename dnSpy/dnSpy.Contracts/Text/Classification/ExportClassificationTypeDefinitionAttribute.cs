/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.ComponentModel.Composition;

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Exports a <see cref="ClassificationTypeDefinition"/>
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class ExportClassificationTypeDefinitionAttribute : ExportAttribute {
		/// <summary>
		/// Gets the guid
		/// </summary>
		public string Guid { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="guid">Guid of the classification type</param>
		public ExportClassificationTypeDefinitionAttribute(string guid) {
			Guid = guid;
		}
	}
}
