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
using System.ComponentModel.Composition;

namespace dnSpy.Contracts.Debugger.Engine.Evaluation {
	/// <summary>
	/// Creates <see cref="DbgEngineLanguage"/>s. Use <see cref="ExportDbgEngineLanguageProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class DbgEngineLanguageProvider {
		/// <summary>
		/// Gets the runtime display name, eg. ".NET"
		/// </summary>
		public abstract string RuntimeDisplayName { get; }

		/// <summary>
		/// Creates all languages
		/// </summary>
		/// <returns></returns>
		public abstract IEnumerable<DbgEngineLanguage> Create();
	}

	/// <summary>Metadata</summary>
	public interface IDbgEngineLanguageProviderMetadata {
		/// <summary>See <see cref="ExportDbgEngineLanguageProviderAttribute.RuntimeKindGuid"/></summary>
		string RuntimeKindGuid { get; }
		/// <summary>See <see cref="ExportDbgEngineLanguageProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgEngineLanguageProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgEngineLanguageProviderAttribute : ExportAttribute, IDbgEngineLanguageProviderMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="runtimeKindGuid">Runtime kind GUID, see <see cref="PredefinedDbgRuntimeKindGuids"/></param>
		/// <param name="order">Order</param>
		public ExportDbgEngineLanguageProviderAttribute(string runtimeKindGuid, double order = double.MaxValue)
			: base(typeof(DbgEngineLanguageProvider)) {
			RuntimeKindGuid = runtimeKindGuid ?? throw new ArgumentNullException(nameof(runtimeKindGuid));
			Order = order;
		}

		/// <summary>
		/// Runtime kind GUID, see <see cref="PredefinedDbgRuntimeKindGuids"/>
		/// </summary>
		public string RuntimeKindGuid { get; }

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}
}
