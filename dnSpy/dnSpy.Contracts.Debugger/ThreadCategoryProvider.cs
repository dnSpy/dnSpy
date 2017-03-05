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

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Provides thread categories and images. Use <see cref="ExportThreadCategoryProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class ThreadCategoryProvider {
		/// <summary>
		/// Returns the thread category info or null
		/// </summary>
		/// <param name="kind">Thread kind, see <see cref="PredefinedThreadKinds"/></param>
		/// <returns></returns>
		public abstract ThreadCategoryInfo? GetCategory(string kind);
	}

	/// <summary>Metadata</summary>
	public interface IThreadCategoryProviderMetadata {
		/// <summary>See <see cref="ExportThreadCategoryProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="ThreadCategoryProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportThreadCategoryProviderAttribute : ExportAttribute, IThreadCategoryProviderMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="order">Order</param>
		public ExportThreadCategoryProviderAttribute(double order = double.MaxValue)
			: base(typeof(ThreadCategoryProvider)) => Order = order;

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}

	/// <summary>
	/// Thread category info
	/// </summary>
	public struct ThreadCategoryInfo {
		/// <summary>
		/// Image (an ImageReference struct)
		/// </summary>
		public object Image { get; }

		/// <summary>
		/// Category
		/// </summary>
		public string Category { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="image">Image</param>
		/// <param name="category">Category</param>
		public ThreadCategoryInfo(object image, string category) {
			Image = image;
			Category = category ?? throw new ArgumentNullException(nameof(category));
		}
	}
}
