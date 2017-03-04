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

namespace dnSpy.Contracts.Debugger.Engine {
	/// <summary>
	/// A class that can update a <see cref="DbgModule"/>
	/// </summary>
	public abstract class DbgEngineModule {
		/// <summary>
		/// Gets the module
		/// </summary>
		public abstract DbgModule Module { get; }

		/// <summary>
		/// Removes the module and disposes of it
		/// </summary>
		public abstract void Remove();

		/// <summary>
		/// Properties to update
		/// </summary>
		[Flags]
		public enum UpdateOptions {
			/// <summary>
			/// Update <see cref="DbgModule.IsExe"/>
			/// </summary>
			IsExe				= 0x00000001,

			/// <summary>
			/// Update <see cref="DbgModule.Address"/>
			/// </summary>
			Address				= 0x00000002,

			/// <summary>
			/// Update <see cref="DbgModule.Size"/>
			/// </summary>
			Size				= 0x00000004,

			/// <summary>
			/// Update <see cref="DbgModule.ImageLayout"/>
			/// </summary>
			ImageLayout			= 0x00000008,

			/// <summary>
			/// Update <see cref="DbgModule.Name"/>
			/// </summary>
			Name				= 0x00000010,

			/// <summary>
			/// Update <see cref="DbgModule.Filename"/>
			/// </summary>
			Filename			= 0x00000020,

			/// <summary>
			/// Update <see cref="DbgModule.RealFilename"/>
			/// </summary>
			RealFilename		= 0x00000040,

			/// <summary>
			/// Update <see cref="DbgModule.IsDynamic"/>
			/// </summary>
			IsDynamic			= 0x00000080,

			/// <summary>
			/// Update <see cref="DbgModule.IsInMemory"/>
			/// </summary>
			IsInMemory			= 0x00000100,

			/// <summary>
			/// Update <see cref="DbgModule.IsOptimized"/>
			/// </summary>
			IsOptimized			= 0x00000200,

			/// <summary>
			/// Update <see cref="DbgModule.Order"/>
			/// </summary>
			Order				= 0x00000400,

			/// <summary>
			/// Update <see cref="DbgModule.Timestamp"/>
			/// </summary>
			Timestamp			= 0x00000800,

			/// <summary>
			/// Update <see cref="DbgModule.Version"/>
			/// </summary>
			Version				= 0x00001000,
		}

		/// <summary>
		/// Updates <see cref="DbgModule.IsExe"/>
		/// </summary>
		/// <param name="isExe">New value</param>
		public void UpdateIsExe(bool isExe) => Update(UpdateOptions.IsExe, isExe: isExe);

		/// <summary>
		/// Updates <see cref="DbgModule.Address"/>
		/// </summary>
		/// <param name="address">New value</param>
		public void UpdateAddress(ulong address) => Update(UpdateOptions.Address, address: address);

		/// <summary>
		/// Updates <see cref="DbgModule.Size"/>
		/// </summary>
		/// <param name="size">New value</param>
		public void UpdateSize(uint size) => Update(UpdateOptions.Size, size: size);

		/// <summary>
		/// Updates <see cref="DbgModule.ImageLayout"/>
		/// </summary>
		/// <param name="imageLayout">New value</param>
		public void UpdateImageLayout(DbgImageLayout imageLayout) => Update(UpdateOptions.ImageLayout, imageLayout: imageLayout);

		/// <summary>
		/// Updates <see cref="DbgModule.Name"/>
		/// </summary>
		/// <param name="name">New value</param>
		public void UpdateName(string name) => Update(UpdateOptions.Name, name: name);

		/// <summary>
		/// Updates <see cref="DbgModule.Filename"/>
		/// </summary>
		/// <param name="filename">New value</param>
		public void UpdateFilename(string filename) => Update(UpdateOptions.Filename, filename: filename);

		/// <summary>
		/// Updates <see cref="DbgModule.RealFilename"/>
		/// </summary>
		/// <param name="realFilename">New value</param>
		public void UpdateRealFilename(string realFilename) => Update(UpdateOptions.RealFilename, realFilename: realFilename);

		/// <summary>
		/// Updates <see cref="DbgModule.IsDynamic"/>
		/// </summary>
		/// <param name="isDynamic">New value</param>
		public void UpdateIsDynamic(bool isDynamic) => Update(UpdateOptions.IsDynamic, isDynamic: isDynamic);

		/// <summary>
		/// Updates <see cref="DbgModule.IsInMemory"/>
		/// </summary>
		/// <param name="isInMemory">New value</param>
		public void UpdateIsInMemory(bool isInMemory) => Update(UpdateOptions.IsInMemory, isInMemory: isInMemory);

		/// <summary>
		/// Updates <see cref="DbgModule.IsOptimized"/>
		/// </summary>
		/// <param name="isOptimized">New value</param>
		public void UpdateIsOptimized(bool? isOptimized) => Update(UpdateOptions.IsOptimized, isOptimized: isOptimized);

		/// <summary>
		/// Updates <see cref="DbgModule.Order"/>
		/// </summary>
		/// <param name="order">New value</param>
		public void UpdateOrder(int order) => Update(UpdateOptions.Order, order: order);

		/// <summary>
		/// Updates <see cref="DbgModule.Timestamp"/>
		/// </summary>
		/// <param name="timestamp">New value</param>
		public void UpdateTimestamp(DateTime? timestamp) => Update(UpdateOptions.Timestamp, timestamp: timestamp);

		/// <summary>
		/// Updates <see cref="DbgModule.Version"/>
		/// </summary>
		/// <param name="version">New value</param>
		public void UpdateVersion(string version) => Update(UpdateOptions.Version, version: version);

		/// <summary>
		/// Updates <see cref="DbgModule"/> properties
		/// </summary>
		/// <param name="options">Options</param>
		/// <param name="isExe">New <see cref="DbgModule.IsExe"/> value</param>
		/// <param name="address">New <see cref="DbgModule.Address"/> value</param>
		/// <param name="size">New <see cref="DbgModule.Size"/> value</param>
		/// <param name="imageLayout">New <see cref="DbgModule.ImageLayout"/> value</param>
		/// <param name="name">New <see cref="DbgModule.Name"/> value</param>
		/// <param name="filename">New <see cref="DbgModule.Filename"/> value</param>
		/// <param name="realFilename">New <see cref="DbgModule.RealFilename"/> value</param>
		/// <param name="isDynamic">New <see cref="DbgModule.IsDynamic"/> value</param>
		/// <param name="isInMemory">New <see cref="DbgModule.IsInMemory"/> value</param>
		/// <param name="isOptimized">New <see cref="DbgModule.IsOptimized"/> value</param>
		/// <param name="order">New <see cref="DbgModule.Order"/> value</param>
		/// <param name="timestamp">New <see cref="DbgModule.Timestamp"/> value</param>
		/// <param name="version">New <see cref="DbgModule.Version"/> value</param>
		public abstract void Update(UpdateOptions options, bool isExe = false, ulong address = 0, uint size = 0, DbgImageLayout imageLayout = 0, string name = null, string filename = null, string realFilename = null, bool isDynamic = false, bool isInMemory = false, bool? isOptimized = null, int order = 0, DateTime? timestamp = null, string version = null);
	}
}
