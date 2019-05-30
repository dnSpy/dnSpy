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
using System.ComponentModel.Composition;

namespace dnSpy.Contracts.Debugger.Attach {
	/// <summary>
	/// Creates <see cref="AttachProgramOptionsProvider"/> instances. Use <see cref="ExportAttachProgramOptionsProviderFactoryAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class AttachProgramOptionsProviderFactory {
		/// <summary>
		/// Creates a new <see cref="AttachProgramOptionsProvider"/> or returns null
		/// </summary>
		/// <param name="allFactories">true if all factories are called, false if only some of them get called</param>
		/// <returns></returns>
		public abstract AttachProgramOptionsProvider? Create(bool allFactories);
	}

	/// <summary>Metadata</summary>
	public interface IAttachProgramOptionsProviderFactoryMetadata {
		/// <summary>See <see cref="ExportAttachProgramOptionsProviderFactoryAttribute.Name"/></summary>
		string Name { get; }
	}

	/// <summary>
	/// Exports an <see cref="AttachProgramOptionsProviderFactory"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportAttachProgramOptionsProviderFactoryAttribute : ExportAttribute, IAttachProgramOptionsProviderFactoryMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name, see <see cref="PredefinedAttachProgramOptionsProviderNames"/></param>
		public ExportAttachProgramOptionsProviderFactoryAttribute(string name)
			: base(typeof(AttachProgramOptionsProviderFactory)) => Name = name ?? throw new ArgumentNullException(nameof(name));

		/// <summary>
		/// Name, see <see cref="PredefinedAttachProgramOptionsProviderNames"/>
		/// </summary>
		public string Name { get; }
	}
}
