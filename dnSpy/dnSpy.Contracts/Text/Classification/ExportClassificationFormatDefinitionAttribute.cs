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
	/// <summary>Metadata</summary>
	public interface IClassificationFormatDefinitionMetadata {
		/// <summary>See <see cref="ExportClassificationFormatDefinitionAttribute.Order"/></summary>
		double Order { get; }
		/// <summary>See <see cref="ExportClassificationFormatDefinitionAttribute.ClassificationTypeName"/></summary>
		string ClassificationTypeName { get; }
		/// <summary>See <see cref="ExportClassificationFormatDefinitionAttribute.DisplayName"/></summary>
		string DisplayName { get; }
	}

	/// <summary>
	/// Exports an <see cref="ClassificationFormatDefinition"/>
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportClassificationFormatDefinitionAttribute : ExportAttribute, IClassificationFormatDefinitionMetadata {
		/// <summary>
		/// Order, default is <see cref="EditorFormatDefinitionPriority.Default"/>
		/// </summary>
		public double Order { get; set; }

		/// <summary>
		/// Gets the classification name
		/// </summary>
		public string ClassificationTypeName { get; }

		/// <summary>
		/// Gets the display name
		/// </summary>
		public string DisplayName { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="classificationTypeName">Classification type name</param>
		/// <param name="displayName">Display name</param>
		public ExportClassificationFormatDefinitionAttribute(string classificationTypeName, string displayName)
			: base(typeof(ClassificationFormatDefinition)) {
			if (string.IsNullOrEmpty(classificationTypeName))
				throw new ArgumentOutOfRangeException(nameof(classificationTypeName));
			if (string.IsNullOrEmpty(displayName))
				throw new ArgumentOutOfRangeException(nameof(displayName));
			Order = EditorFormatDefinitionPriority.Default;
			ClassificationTypeName = classificationTypeName;
			DisplayName = displayName;
		}
	}
}
