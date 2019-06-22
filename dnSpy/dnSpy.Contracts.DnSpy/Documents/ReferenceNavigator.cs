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
using System.ComponentModel.Composition;

namespace dnSpy.Contracts.Documents {
	/// <summary>
	/// Shows a reference
	/// </summary>
	public abstract class ReferenceNavigatorService {
		/// <summary>
		/// Shows a reference. It can be called from any thread.
		/// </summary>
		/// <param name="reference">Reference. MEF exported <see cref="ReferenceConverter"/>s can convert this to another reference.</param>
		/// <param name="options">Options passed to <see cref="ReferenceNavigator"/>s, eg. <see cref="PredefinedReferenceNavigatorOptions"/></param>
		public abstract void GoTo(object? reference, object[]? options = null);
	}

	/// <summary>
	/// Predefined <see cref="ReferenceNavigator"/> options
	/// </summary>
	public static class PredefinedReferenceNavigatorOptions {
		/// <summary>
		/// Show the reference in a new tab
		/// </summary>
		public const string NewTab = nameof(NewTab);
	}

	/// <summary>
	/// Shows a reference. Use <see cref="ExportReferenceNavigatorAttribute"/> to export an instance.
	/// </summary>
	public abstract class ReferenceNavigator {
		/// <summary>
		/// Returns true if it showed the reference, and false if the next handler should get called.
		/// This method is called on the UI thread.
		/// </summary>
		/// <param name="reference">Reference. MEF exported <see cref="ReferenceConverter"/>s can convert this reference to another reference.</param>
		/// <param name="options">Options passed to <see cref="ReferenceNavigator"/>s, eg. <see cref="PredefinedReferenceNavigatorOptions"/></param>
		/// <returns></returns>
		public abstract bool GoTo(object reference, ReadOnlyCollection<object> options);
	}

	/// <summary>Metadata</summary>
	public interface IReferenceNavigatorMetadata {
		/// <summary>See <see cref="ExportReferenceNavigatorAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="ReferenceNavigator"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportReferenceNavigatorAttribute : ExportAttribute, IReferenceNavigatorMetadata {
		/// <summary>Constructor</summary>
		public ExportReferenceNavigatorAttribute()
			: base(typeof(ReferenceNavigator)) => Order = double.MaxValue;

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}

	/// <summary>
	/// Converts a reference passed to <see cref="ReferenceNavigatorService.GoTo(object, object[])"/>.  This
	/// new reference is passed to <see cref="ReferenceNavigator.GoTo(object, ReadOnlyCollection{object})"/>.
	/// Use <see cref="ExportReferenceConverterAttribute"/> to export an instance.
	/// </summary>
	public abstract class ReferenceConverter {
		/// <summary>
		/// Converts a reference. If null is written to <paramref name="reference"/>,
		/// <see cref="ReferenceNavigator.GoTo(object, ReadOnlyCollection{object})"/> won't get called.
		/// This method is called on the UI thread.
		/// </summary>
		/// <param name="reference">Reference</param>
		public abstract void Convert(ref object? reference);
	}

	/// <summary>Metadata</summary>
	public interface IReferenceConverterMetadata {
		/// <summary>See <see cref="ExportReferenceConverterAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="ReferenceConverter"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportReferenceConverterAttribute : ExportAttribute, IReferenceConverterMetadata {
		/// <summary>Constructor</summary>
		public ExportReferenceConverterAttribute()
			: base(typeof(ReferenceConverter)) => Order = double.MaxValue;

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
