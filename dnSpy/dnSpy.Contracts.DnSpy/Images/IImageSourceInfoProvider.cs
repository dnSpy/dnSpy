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
using System.Reflection;

namespace dnSpy.Contracts.Images {
	/// <summary>
	/// Converts <see cref="ImageReference"/>s to <see cref="ImageSourceInfo"/>s. Use
	/// <see cref="ExportImageSourceInfoProviderAttribute"/> to export an instance.
	/// There's usually no need to export an instance since the default implementation is
	/// usually enough.
	/// </summary>
	public interface IImageSourceInfoProvider {
		/// <summary>
		/// Returns all <see cref="ImageSourceInfo"/>s or null if the next (or default)
		/// <see cref="IImageSourceInfoProvider"/> instance should be asked.
		/// </summary>
		/// <param name="name">Name from <see cref="ImageReference.Name"/> but with any options removed from the string</param>
		/// <returns></returns>
		ImageSourceInfo[] GetImageSourceInfos(string name);
	}

	/// <summary>Metadata</summary>
	public interface IImageSourceInfoProviderMetadata {
		/// <summary>See <see cref="ExportImageSourceInfoProviderAttribute.Type"/></summary>
		Type Type { get; }
		/// <summary>See <see cref="ExportImageSourceInfoProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IImageSourceInfoProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportImageSourceInfoProviderAttribute : ExportAttribute, IImageSourceInfoProviderMetadata {
		/// <summary>Constructor</summary>
		/// <param name="type">Some type in the assembly, eg. this <see cref="IImageSourceInfoProvider"/> type itself</param>
		public ExportImageSourceInfoProviderAttribute(Type type)
			: this(type, double.MaxValue) {
		}

		/// <summary>Constructor</summary>
		/// <param name="type">Some type in the assembly, eg. this <see cref="IImageSourceInfoProvider"/> type itself</param>
		/// <param name="order">Order of this instance</param>
		public ExportImageSourceInfoProviderAttribute(Type type, double order)
			: base(typeof(IImageSourceInfoProvider)) {
			if (type == null)
				throw new ArgumentNullException(nameof(type));
			Type = type;
			Order = order;
		}

		/// <summary>Constructor</summary>
		/// <param name="order">Order of this instance</param>
		internal ExportImageSourceInfoProviderAttribute(double order) {
			Order = order;
		}

		/// <summary>
		/// Gets the type
		/// </summary>
		public Type Type { get; }

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; }
	}
}
