/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.Contracts.Settings;

namespace dnSpy.Contracts.Files.Tabs {
	/// <summary>
	/// <see cref="IFileTabContent"/> factory. Use <see cref="ExportFileTabContentFactoryAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IFileTabContentFactory {
		/// <summary>
		/// Creates a <see cref="IFileTabContent"/> instance or returns null
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		IFileTabContent Create(IFileTabContentFactoryContext context);

		/// <summary>
		/// Serializes a <see cref="IFileTabContent"/> instance. Returns a unique guid if it was
		/// serialized, else null
		/// </summary>
		/// <param name="content">Content</param>
		/// <param name="section">Section to use</param>
		/// <returns></returns>
		Guid? Serialize(IFileTabContent content, ISettingsSection section);

		/// <summary>
		/// Deserializes a <see cref="IFileTabContent"/> instance. Returns null if <paramref name="guid"/>
		/// isn't supported.
		/// </summary>
		/// <param name="guid">Guid, this is the return value of <see cref="Serialize(IFileTabContent, ISettingsSection)"/></param>
		/// <param name="section">Section with serialized content</param>
		/// <param name="context">Context</param>
		/// <returns></returns>
		IFileTabContent Deserialize(Guid guid, ISettingsSection section, IFileTabContentFactoryContext context);
	}

	/// <summary>Metadata</summary>
	public interface IFileTabContentFactoryMetadata {
		/// <summary>See <see cref="ExportFileTabContentFactoryAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IFileTabContentFactory"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportFileTabContentFactoryAttribute : ExportAttribute, IFileTabContentFactoryMetadata {
		/// <summary>Constructor</summary>
		public ExportFileTabContentFactoryAttribute()
			: base(typeof(IFileTabContentFactory)) {
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
