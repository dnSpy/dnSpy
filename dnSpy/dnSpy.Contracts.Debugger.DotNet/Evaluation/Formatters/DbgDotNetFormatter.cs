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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation.Formatters {
	/// <summary>
	/// Formats values, types, names. Use <see cref="ExportDbgDotNetFormatterAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class DbgDotNetFormatter {
		/// <summary>
		/// Formats an object ID
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="output">Destination</param>
		/// <param name="objectId">Object ID</param>
		public abstract void FormatName(DbgEvaluationContext context, ITextColorWriter output, DbgDotNetEngineObjectId objectId);
	}

	/// <summary>Metadata</summary>
	public interface IDbgDotNetFormatterMetadata {
		/// <summary>See <see cref="ExportDbgDotNetFormatterAttribute.LanguageGuid"/></summary>
		string LanguageGuid { get; }
		/// <summary>See <see cref="ExportDbgDotNetFormatterAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgDotNetFormatter"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgDotNetFormatterAttribute : ExportAttribute, IDbgDotNetFormatterMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="languageGuid">Language GUID, see <see cref="DbgDotNetLanguageGuids"/></param>
		/// <param name="order">Order</param>
		public ExportDbgDotNetFormatterAttribute(string languageGuid, double order = double.MaxValue)
			: base(typeof(DbgDotNetFormatter)) {
			LanguageGuid = languageGuid ?? throw new ArgumentNullException(nameof(languageGuid));
			Order = order;
		}

		/// <summary>
		/// Language GUID, see <see cref="DbgDotNetLanguageGuids"/>
		/// </summary>
		public string LanguageGuid { get; }

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}
}
