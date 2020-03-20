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
using System.Linq;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Engine.CallStack;
using dnSpy.Contracts.Debugger.Exceptions;

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
		/// Creates an app domain. The engine has paused the program.
		/// </summary>
		/// <param name="internalAppDomain">App domain object created by the debug engine</param>
		/// <param name="name">New <see cref="DbgAppDomain.Name"/> value</param>
		/// <param name="id">New <see cref="DbgAppDomain.Id"/> value</param>
		/// <param name="messageFlags">Message flags</param>
		/// <returns></returns>
		public DbgEngineAppDomain CreateAppDomain(DbgInternalAppDomain internalAppDomain, string name, int id, DbgEngineMessageFlags messageFlags) =>
			CreateAppDomain<object>(internalAppDomain, name, id, messageFlags, null, null);

		/// <summary>
		/// Creates an app domain. The engine has paused the program.
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <param name="internalAppDomain">App domain object created by the debug engine</param>
		/// <param name="name">New <see cref="DbgAppDomain.Name"/> value</param>
		/// <param name="id">New <see cref="DbgAppDomain.Id"/> value</param>
		/// <param name="messageFlags">Message flags</param>
		/// <param name="data">Data to add to the <see cref="DbgAppDomain"/> or null if nothing gets added</param>
		/// <param name="onCreated">Called right after creating the app domain but before adding it to internal data structures. This can be null.</param>
		/// <returns></returns>
		public abstract DbgEngineAppDomain CreateAppDomain<T>(DbgInternalAppDomain internalAppDomain, string name, int id, DbgEngineMessageFlags messageFlags, T? data, Action<DbgEngineAppDomain>? onCreated = null) where T : class;

		/// <summary>
		/// Creates a module. The engine has paused the program.
		/// </summary>
		/// <param name="appDomain">New <see cref="DbgModule.AppDomain"/> value</param>
		/// <param name="internalModule">Module object created by the debug engine</param>
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
		/// <param name="messageFlags">Message flags</param>
		/// <returns></returns>
		public DbgEngineModule CreateModule(DbgAppDomain? appDomain, DbgInternalModule internalModule, bool isExe, ulong address, uint size, DbgImageLayout imageLayout, string name, string filename, bool isDynamic, bool isInMemory, bool? isOptimized, int order, DateTime? timestamp, string version, DbgEngineMessageFlags messageFlags) =>
			CreateModule<object>(appDomain, internalModule, isExe, address, size, imageLayout, name, filename, isDynamic, isInMemory, isOptimized, order, timestamp, version, messageFlags, null, null);

		/// <summary>
		/// Creates a module. The engine has paused the program.
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <param name="appDomain">New <see cref="DbgModule.AppDomain"/> value</param>
		/// <param name="internalModule">Module object created by the debug engine</param>
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
		/// <param name="messageFlags">Message flags</param>
		/// <param name="data">Data to add to the <see cref="DbgModule"/> or null if nothing gets added</param>
		/// <param name="onCreated">Called right after creating the module but before adding it to internal data structures. This can be null.</param>
		/// <returns></returns>
		public abstract DbgEngineModule CreateModule<T>(DbgAppDomain? appDomain, DbgInternalModule internalModule, bool isExe, ulong address, uint size, DbgImageLayout imageLayout, string name, string filename, bool isDynamic, bool isInMemory, bool? isOptimized, int order, DateTime? timestamp, string version, DbgEngineMessageFlags messageFlags, T? data, Action<DbgEngineModule>? onCreated = null) where T : class;

		/// <summary>
		/// Creates a thread. The engine has paused the program.
		/// </summary>
		/// <param name="appDomain">New <see cref="DbgThread.AppDomain"/> value</param>
		/// <param name="kind">New <see cref="DbgThread.Kind"/> value</param>
		/// <param name="id">New <see cref="DbgThread.Id"/> value</param>
		/// <param name="managedId">New <see cref="DbgThread.ManagedId"/> value</param>
		/// <param name="name">New <see cref="DbgThread.Name"/> value</param>
		/// <param name="suspendedCount">New <see cref="DbgThread.SuspendedCount"/> value</param>
		/// <param name="state">New <see cref="DbgThread.State"/> value</param>
		/// <param name="messageFlags">Message flags</param>
		/// <returns></returns>
		public DbgEngineThread CreateThread(DbgAppDomain? appDomain, string kind, ulong id, ulong? managedId, string? name, int suspendedCount, ReadOnlyCollection<DbgStateInfo> state, DbgEngineMessageFlags messageFlags) =>
			CreateThread<object>(appDomain, kind, id, managedId, name, suspendedCount, state, messageFlags, null, null);

		/// <summary>
		/// Creates a thread. The engine has paused the program.
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <param name="appDomain">New <see cref="DbgThread.AppDomain"/> value</param>
		/// <param name="kind">New <see cref="DbgThread.Kind"/> value</param>
		/// <param name="id">New <see cref="DbgThread.Id"/> value</param>
		/// <param name="managedId">New <see cref="DbgThread.ManagedId"/> value</param>
		/// <param name="name">New <see cref="DbgThread.Name"/> value</param>
		/// <param name="suspendedCount">New <see cref="DbgThread.SuspendedCount"/> value</param>
		/// <param name="state">New <see cref="DbgThread.State"/> value</param>
		/// <param name="messageFlags">Message flags</param>
		/// <param name="data">Data to add to the <see cref="DbgThread"/> or null if nothing gets added</param>
		/// <param name="onCreated">Called right after creating the thread but before adding it to internal data structures. This can be null.</param>
		/// <returns></returns>
		public abstract DbgEngineThread CreateThread<T>(DbgAppDomain? appDomain, string kind, ulong id, ulong? managedId, string? name, int suspendedCount, ReadOnlyCollection<DbgStateInfo> state, DbgEngineMessageFlags messageFlags, T? data, Action<DbgEngineThread>? onCreated = null) where T : class;

		/// <summary>
		/// Creates an exception. The engine has paused the program.
		/// </summary>
		/// <param name="id">Exception id</param>
		/// <param name="flags">Exception event flags</param>
		/// <param name="message">Exception message or null if it's not available</param>
		/// <param name="thread">Thread where exception was thrown or null if it's unknown</param>
		/// <param name="module">Module where exception was thrown or null if it's unknown</param>
		/// <param name="messageFlags">Message flags</param>
		/// <returns></returns>
		public DbgException CreateException(DbgExceptionId id, DbgExceptionEventFlags flags, string? message, DbgThread? thread, DbgModule? module, DbgEngineMessageFlags messageFlags) =>
			CreateException<object>(id, flags, message, thread, module, messageFlags, null, null);

		/// <summary>
		/// Creates an exception. The engine has paused the program.
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <param name="id">Exception id</param>
		/// <param name="flags">Exception event flags</param>
		/// <param name="message">Exception message or null if it's not available</param>
		/// <param name="thread">Thread where exception was thrown or null if it's unknown</param>
		/// <param name="module">Module where exception was thrown or null if it's unknown</param>
		/// <param name="messageFlags">Message flags</param>
		/// <param name="data">Data to add to the <see cref="DbgException"/> or null if nothing gets added</param>
		/// <param name="onCreated">Called right after creating the exception but before adding it to internal data structures. This can be null.</param>
		/// <returns></returns>
		public abstract DbgException CreateException<T>(DbgExceptionId id, DbgExceptionEventFlags flags, string? message, DbgThread? thread, DbgModule? module, DbgEngineMessageFlags messageFlags, T? data, Action<DbgException>? onCreated = null) where T : class;

		/// <summary>
		/// Value used when the bound breakpoint's address isn't known
		/// </summary>
		public const ulong BoundBreakpointNoAddress = ulong.MaxValue;

		/// <summary>
		/// Creates a bound breakpoint. This method returns null if there was no breakpoint matching <paramref name="location"/>.
		/// 
		/// To get notified when a bound breakpoint gets deleted, add custom data that implements <see cref="IDisposable"/>.
		/// </summary>
		/// <param name="location">Breakpoint location</param>
		/// <param name="module">Module or null if none</param>
		/// <param name="address">Address or <see cref="BoundBreakpointNoAddress"/> if unknown</param>
		/// <param name="message">Warning/error message or default if none</param>
		/// <returns></returns>
		public DbgEngineBoundCodeBreakpoint? Create(DbgCodeLocation location, DbgModule? module, ulong address, DbgEngineBoundCodeBreakpointMessage message) =>
			Create(new[] { new DbgBoundCodeBreakpointInfo<object>(location, module, address, message, null) }).FirstOrDefault();

		/// <summary>
		/// Creates a bound breakpoint. This method returns null if there was no breakpoint matching <paramref name="location"/>.
		/// 
		/// To get notified when a bound breakpoint gets deleted, add custom data that implements <see cref="IDisposable"/>.
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <param name="location">Breakpoint location</param>
		/// <param name="module">Module or null if none</param>
		/// <param name="address">Address or <see cref="BoundBreakpointNoAddress"/> if unknown</param>
		/// <param name="message">Warning/error message or default if none</param>
		/// <param name="data">Data to add to the <see cref="DbgEngineBoundCodeBreakpoint"/> or null if nothing gets added</param>
		/// <returns></returns>
		public DbgEngineBoundCodeBreakpoint? Create<T>(DbgCodeLocation location, DbgModule? module, ulong address, DbgEngineBoundCodeBreakpointMessage message, T? data) where T : class =>
			Create(new[] { new DbgBoundCodeBreakpointInfo<T>(location, module, address, message, data) }).FirstOrDefault();

		/// <summary>
		/// Creates bound breakpoints. Locations that don't match an existing breakpoint are ignored, and all user data
		/// are disposed if they implement <see cref="IDisposable"/>.
		/// 
		/// To get notified when a bound breakpoint gets deleted, add custom data that implements <see cref="IDisposable"/>.
		/// </summary>
		/// <typeparam name="T">Type of data</typeparam>
		/// <param name="infos">Bound breakpoints to create</param>
		/// <returns></returns>
		public abstract DbgEngineBoundCodeBreakpoint[] Create<T>(DbgBoundCodeBreakpointInfo<T>[] infos) where T : class;

		/// <summary>
		/// Creates a special stack frame that's displayed as [name], eg. [Managed to Native Transition]
		/// </summary>
		/// <param name="name">Name, eg. "Managed to Native Transition"</param>
		/// <param name="location">Location or null</param>
		/// <param name="module">Module or null</param>
		/// <param name="functionOffset">Function offset</param>
		/// <param name="functionToken">Function token</param>
		/// <returns></returns>
		public abstract DbgEngineStackFrame CreateSpecialStackFrame(string name, DbgCodeLocation? location = null, DbgModule? module = null, uint functionOffset = 0, uint functionToken = DbgEngineStackFrame.InvalidFunctionToken);
	}

	/// <summary>
	/// Bound breakpoint info
	/// </summary>
	/// <typeparam name="T">Type of data</typeparam>
	public readonly struct DbgBoundCodeBreakpointInfo<T> where T : class {
		/// <summary>
		/// Value used when the bound breakpoint's address isn't known
		/// </summary>
		public const ulong NoAddress = DbgObjectFactory.BoundBreakpointNoAddress;

		/// <summary>
		/// Gets the location
		/// </summary>
		public DbgCodeLocation Location { get; }

		/// <summary>
		/// Gets the module or null if none
		/// </summary>
		public DbgModule? Module { get; }

		/// <summary>
		/// Gets the address or <see cref="NoAddress"/> if it's not known
		/// </summary>
		public ulong Address { get; }

		/// <summary>
		/// Gets the warning/error message or default if none
		/// </summary>
		public DbgEngineBoundCodeBreakpointMessage Message { get; }

		/// <summary>
		/// Gets the data to add to the <see cref="DbgEngineBoundCodeBreakpoint"/> or null if nothing gets added.
		/// If the data implements <see cref="IDisposable"/>, it gets disposed when the bound breakpoint gets deleted.
		/// </summary>
		public T? Data { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="location">Location</param>
		/// <param name="module">Module or null if none</param>
		/// <param name="address">Address or <see cref="NoAddress"/> if it's not known</param>
		/// <param name="message">Warning/error message or default if none</param>
		/// <param name="data">Data to add to the <see cref="DbgBoundCodeBreakpoint"/> or null if nothing gets added.
		/// If the data implements <see cref="IDisposable"/>, it gets disposed when the bound breakpoint gets deleted.</param>
		public DbgBoundCodeBreakpointInfo(DbgCodeLocation location, DbgModule? module, ulong address, DbgEngineBoundCodeBreakpointMessage message, T? data) {
			Location = location ?? throw new ArgumentNullException(nameof(location));
			Module = module;
			Address = address;
			Message = message;
			Data = data;
		}
	}
}
