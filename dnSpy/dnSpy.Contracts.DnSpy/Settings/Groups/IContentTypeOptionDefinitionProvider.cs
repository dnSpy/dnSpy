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
using System.ComponentModel.Composition;

namespace dnSpy.Contracts.Settings.Groups {
	/// <summary>
	/// Provides <see cref="ContentTypeOptionDefinition"/>s. Use <see cref="ExportContentTypeOptionDefinitionProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IContentTypeOptionDefinitionProvider {
		/// <summary>
		/// Returns the options
		/// </summary>
		/// <returns></returns>
		IEnumerable<ContentTypeOptionDefinition> GetOptions();
	}

	/// <summary>Metadata</summary>
	public interface IContentTypeOptionDefinitionProviderMetadata {
		/// <summary>See <see cref="ExportContentTypeOptionDefinitionProviderAttribute.Order"/></summary>
		double Order { get; }
		/// <summary>See <see cref="ExportContentTypeOptionDefinitionProviderAttribute.Group"/></summary>
		string Group { get; }
	}

	/// <summary>
	/// Exports a <see cref="IContentTypeOptionDefinitionProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportContentTypeOptionDefinitionProviderAttribute : ExportAttribute, IContentTypeOptionDefinitionProviderMetadata {
		/// <summary>Constructor</summary>
		/// <param name="group">Group, eg. <see cref="PredefinedTextViewGroupNames.CodeEditor"/></param>
		/// <param name="order">Order of this instanec</param>
		public ExportContentTypeOptionDefinitionProviderAttribute(string group, double order = double.MaxValue)
			: base(typeof(IContentTypeOptionDefinitionProvider)) {
			Group = group ?? throw new ArgumentNullException(nameof(group));
			Order = order;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; }

		/// <summary>
		/// Group, eg. <see cref="PredefinedTextViewGroupNames.CodeEditor"/>
		/// </summary>
		public string Group { get; }
	}
}
