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
using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Debugger.Engine {
	/// <summary>
	/// Used by a <see cref="DbgEngine"/> to create new modules, threads, etc.
	/// </summary>
	/// <remarks>
	/// The engines don't need to worry about locks, or raising events in the correct thread,
	/// or updating collections in the right thread, or closing dbg objects, etc...
	/// It's taken care of by dnSpy.
	/// </remarks>
	public abstract class DbgObjectFactory {
		/// <summary>
		/// Gets the debug manager
		/// </summary>
		public abstract DbgManager DbgManager { get; }

		/// <summary>
		/// Gets the process
		/// </summary>
		public DbgProcess Process => Runtime.Process;

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public abstract DbgRuntime Runtime { get; }

		/// <summary>
		/// Creates an app domain
		/// </summary>
		/// <param name="name">New <see cref="DbgAppDomain.Name"/> value</param>
		/// <param name="id">New <see cref="DbgAppDomain.Id"/> value</param>
		/// <returns></returns>
		public DbgEngineAppDomain CreateAppDomain(string name, int id) =>
			CreateAppDomain<object>(name, id, null);

		/// <summary>
		/// Creates an app domain
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <param name="name">New <see cref="DbgAppDomain.Name"/> value</param>
		/// <param name="id">New <see cref="DbgAppDomain.Id"/> value</param>
		/// <param name="data">Data to add to the <see cref="DbgAppDomain"/> or null if nothing gets added</param>
		/// <returns></returns>
		public abstract DbgEngineAppDomain CreateAppDomain<T>(string name, int id, T data) where T : class;

		/// <summary>
		/// Creates a module
		/// </summary>
		/// <param name="appDomain">New <see cref="DbgModule.AppDomain"/> value</param>
		/// <param name="isExe">New <see cref="DbgModule.IsExe"/> value</param>
		/// <param name="address">New <see cref="DbgModule.Address"/> value</param>
		/// <param name="size">New <see cref="DbgModule.Size"/> value</param>
		/// <param name="imageLayout">New <see cref="DbgModule.ImageLayout"/> value</param>
		/// <param name="name">New <see cref="DbgModule.Name"/> value</param>
		/// <param name="filename">New <see cref="DbgModule.Filename"/> value</param>
		/// <param name="isDynamic">New <see cref="DbgModule.IsDynamic"/> value</param>
		/// <param name="isInMemory">New <see cref="DbgModule.IsInMemory"/> value</param>
		/// <param name="isOptimized">New <see cref="DbgModule.IsOptimized"/> value</param>
		/// <param name="order">New <see cref="DbgModule.Order"/> value</param>
		/// <param name="timestamp">New <see cref="DbgModule.Timestamp"/> value</param>
		/// <param name="version">New <see cref="DbgModule.Version"/> value</param>
		/// <returns></returns>
		public DbgEngineModule CreateModule(DbgAppDomain appDomain, bool isExe, ulong address, uint size, DbgImageLayout imageLayout, string name, string filename, bool isDynamic, bool isInMemory, bool? isOptimized, int order, DateTime? timestamp, string version) =>
			CreateModule<object>(appDomain, isExe, address, size, imageLayout, name, filename, isDynamic, isInMemory, isOptimized, order, timestamp, version, null);

		/// <summary>
		/// Creates a module
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <param name="appDomain">New <see cref="DbgModule.AppDomain"/> value</param>
		/// <param name="isExe">New <see cref="DbgModule.IsExe"/> value</param>
		/// <param name="address">New <see cref="DbgModule.Address"/> value</param>
		/// <param name="size">New <see cref="DbgModule.Size"/> value</param>
		/// <param name="imageLayout">New <see cref="DbgModule.ImageLayout"/> value</param>
		/// <param name="name">New <see cref="DbgModule.Name"/> value</param>
		/// <param name="filename">New <see cref="DbgModule.Filename"/> value</param>
		/// <param name="isDynamic">New <see cref="DbgModule.IsDynamic"/> value</param>
		/// <param name="isInMemory">New <see cref="DbgModule.IsInMemory"/> value</param>
		/// <param name="isOptimized">New <see cref="DbgModule.IsOptimized"/> value</param>
		/// <param name="order">New <see cref="DbgModule.Order"/> value</param>
		/// <param name="timestamp">New <see cref="DbgModule.Timestamp"/> value</param>
		/// <param name="version">New <see cref="DbgModule.Version"/> value</param>
		/// <param name="data">Data to add to the <see cref="DbgModule"/> or null if nothing gets added</param>
		/// <returns></returns>
		public abstract DbgEngineModule CreateModule<T>(DbgAppDomain appDomain, bool isExe, ulong address, uint size, DbgImageLayout imageLayout, string name, string filename, bool isDynamic, bool isInMemory, bool? isOptimized, int order, DateTime? timestamp, string version, T data) where T : class;

		/// <summary>
		/// Creates a thread
		/// </summary>
		/// <param name="appDomain">New <see cref="DbgThread.AppDomain"/> value</param>
		/// <param name="kind">New <see cref="DbgThread.Kind"/> value</param>
		/// <param name="id">New <see cref="DbgThread.Id"/> value</param>
		/// <param name="managedId">New <see cref="DbgThread.ManagedId"/> value</param>
		/// <param name="name">New <see cref="DbgThread.Name"/> value</param>
		/// <param name="state">New <see cref="DbgThread.State"/> value</param>
		/// <returns></returns>
		public DbgEngineThread CreateThread(DbgAppDomain appDomain, string kind, int id, int? managedId, string name, ReadOnlyCollection<DbgStateInfo> state) =>
			CreateThread<object>(appDomain, kind, id, managedId, name, state, null);

		/// <summary>
		/// Creates a thread
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <param name="appDomain">New <see cref="DbgThread.AppDomain"/> value</param>
		/// <param name="kind">New <see cref="DbgThread.Kind"/> value</param>
		/// <param name="id">New <see cref="DbgThread.Id"/> value</param>
		/// <param name="managedId">New <see cref="DbgThread.ManagedId"/> value</param>
		/// <param name="name">New <see cref="DbgThread.Name"/> value</param>
		/// <param name="state">New <see cref="DbgThread.State"/> value</param>
		/// <param name="data">Data to add to the <see cref="DbgThread"/> or null if nothing gets added</param>
		/// <returns></returns>
		public abstract DbgEngineThread CreateThread<T>(DbgAppDomain appDomain, string kind, int id, int? managedId, string name, ReadOnlyCollection<DbgStateInfo> state, T data) where T : class;
	}
}
