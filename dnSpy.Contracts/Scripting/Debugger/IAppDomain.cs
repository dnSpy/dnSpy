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
using System.Collections.Generic;
using System.Reflection;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// An AppDomain in the debugged process
	/// </summary>
	public interface IAppDomain {
		/// <summary>
		/// AppDomain Id
		/// </summary>
		int Id { get; }

		/// <summary>
		/// true if the debugger is attached to the AppDomain
		/// </summary>
		bool IsAttached { get; }

		/// <summary>
		/// true if the threads are running freely
		/// </summary>
		bool IsRunning { get; }

		/// <summary>
		/// AppDomain name
		/// </summary>
		string Name { get; }

		/// <summary>
		/// true if the AppDomain has exited
		/// </summary>
		bool HasExited { get; }

		/// <summary>
		/// Gets all threads
		/// </summary>
		IEnumerable<IDebuggerThread> Threads { get; }

		/// <summary>
		/// Gets all assemblies
		/// </summary>
		IEnumerable<IDebuggerAssembly> Assemblies { get; }

		/// <summary>
		/// Gets all modules
		/// </summary>
		IEnumerable<IDebuggerModule> Modules { get; }

		/// <summary>
		/// Gets the core module (mscorlib)
		/// </summary>
		IDebuggerModule CorLib { get; }

		/// <summary>
		/// Finds a module
		/// </summary>
		/// <param name="module">Module</param>
		/// <returns></returns>
		IDebuggerModule GetModule(Module module);

		/// <summary>
		/// Finds a module
		/// </summary>
		/// <param name="name">Module name</param>
		/// <returns></returns>
		IDebuggerModule GetModule(ModuleName name);

		/// <summary>
		/// Finds a module
		/// </summary>
		/// <param name="name">Full path, filename, or filename without extension of module</param>
		/// <returns></returns>
		IDebuggerModule GetModuleByName(string name);

		/// <summary>
		/// Finds an assembly
		/// </summary>
		/// <param name="asm">Assembly</param>
		/// <returns></returns>
		IDebuggerAssembly GetAssembly(Assembly asm);

		/// <summary>
		/// Finds an assembly
		/// </summary>
		/// <param name="name">Full path, filename, or filename without extension of assembly, or
		/// assembly simple name or assembly full name</param>
		/// <returns></returns>
		IDebuggerAssembly GetAssembly(string name);

		/// <summary>
		/// Finds a class
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <returns></returns>
		IDebuggerClass GetClass(string modName, string className);

		/// <summary>
		/// Finds a method
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <param name="methodName">Method name</param>
		/// <returns></returns>
		IDebuggerMethod GetMethod(string modName, string className, string methodName);

		/// <summary>
		/// Finds a field
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <param name="fieldName">Field name</param>
		/// <returns></returns>
		IDebuggerField GetField(string modName, string className, string fieldName);

		/// <summary>
		/// Finds a property
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <param name="propertyName">Property name</param>
		/// <returns></returns>
		IDebuggerProperty GetProperty(string modName, string className, string propertyName);

		/// <summary>
		/// Finds an event
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <param name="eventName">Event name</param>
		/// <returns></returns>
		IDebuggerEvent GetEvent(string modName, string className, string eventName);

		/// <summary>
		/// Finds a method
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="token">Method token</param>
		/// <returns></returns>
		IDebuggerMethod GetMethod(string modName, uint token);

		/// <summary>
		/// Finds a field
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="token">Field token</param>
		/// <returns></returns>
		IDebuggerField GetField(string modName, uint token);

		/// <summary>
		/// Finds a property
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="token">Property token</param>
		/// <returns></returns>
		IDebuggerProperty GetProperty(string modName, uint token);

		/// <summary>
		/// Finds an event
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="token">Event token</param>
		/// <returns></returns>
		IDebuggerEvent GetEvent(string modName, uint token);

		/// <summary>
		/// Finds a type
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <returns></returns>
		IDebuggerType GetType(string modName, string className);

		/// <summary>
		/// Finds a type
		/// </summary>
		/// <param name="modName">Full path, filename, or filename without extension of module</param>
		/// <param name="className">Class name</param>
		/// <param name="genericArguments">Generic arguments</param>
		/// <returns></returns>
		IDebuggerType GetType(string modName, string className, params IDebuggerType[] genericArguments);

		/// <summary>
		/// Finds a type
		/// </summary>
		/// <param name="type">A type that must exist in one of the loaded assemblies in the
		/// debugged process.</param>
		/// <returns></returns>
		IDebuggerType GetType(Type type);

		/// <summary>
		/// Gets a field
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		IDebuggerField GetField(FieldInfo field);

		/// <summary>
		/// Gets a method
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		IDebuggerMethod GetMethod(MethodBase method);

		/// <summary>
		/// Gets a property
		/// </summary>
		/// <param name="prop">Property</param>
		/// <returns></returns>
		IDebuggerProperty GetProperty(PropertyInfo prop);

		/// <summary>
		/// Gets an event
		/// </summary>
		/// <param name="evt">Event</param>
		/// <returns></returns>
		IDebuggerEvent GetEvent(EventInfo evt);

		/// <summary>
		/// Creates a function pointer type
		/// </summary>
		/// <param name="types">Function types. The first type is the return type.</param>
		/// <returns></returns>
		IDebuggerType CreateFnPtr(params IDebuggerType[] types);

		/// <summary>
		/// Creates a function pointer type
		/// </summary>
		/// <param name="types">Function types. The first type is the return type.</param>
		/// <returns></returns>
		IDebuggerType CreateFunctionPointer(params IDebuggerType[] types);

		/// <summary>
		/// Gets type <see cref="Void"/>
		/// </summary>
		IDebuggerType Void { get; }

		/// <summary>
		/// Gets type <see cref="bool"/>
		/// </summary>
		IDebuggerType Boolean { get; }

		/// <summary>
		/// Gets type <see cref="char"/>
		/// </summary>
		IDebuggerType Char { get; }

		/// <summary>
		/// Gets type <see cref="sbyte"/>
		/// </summary>
		IDebuggerType SByte { get; }

		/// <summary>
		/// Gets type <see cref="byte"/>
		/// </summary>
		IDebuggerType Byte { get; }

		/// <summary>
		/// Gets type <see cref="short"/>
		/// </summary>
		IDebuggerType Int16 { get; }

		/// <summary>
		/// Gets type <see cref="ushort"/>
		/// </summary>
		IDebuggerType UInt16 { get; }

		/// <summary>
		/// Gets type <see cref="int"/>
		/// </summary>
		IDebuggerType Int32 { get; }

		/// <summary>
		/// Gets type <see cref="uint"/>
		/// </summary>
		IDebuggerType UInt32 { get; }

		/// <summary>
		/// Gets type <see cref="long"/>
		/// </summary>
		IDebuggerType Int64 { get; }

		/// <summary>
		/// Gets type <see cref="ulong"/>
		/// </summary>
		IDebuggerType UInt64 { get; }

		/// <summary>
		/// Gets type <see cref="float"/>
		/// </summary>
		IDebuggerType Single { get; }

		/// <summary>
		/// Gets type <see cref="double"/>
		/// </summary>
		IDebuggerType Double { get; }

		/// <summary>
		/// Gets type <see cref="string"/>
		/// </summary>
		IDebuggerType String { get; }

		/// <summary>
		/// Gets type <see cref="TypedReference"/>
		/// </summary>
		IDebuggerType TypedReference { get; }

		/// <summary>
		/// Gets type <see cref="IntPtr"/>
		/// </summary>
		IDebuggerType IntPtr { get; }

		/// <summary>
		/// Gets type <see cref="UIntPtr"/>
		/// </summary>
		IDebuggerType UIntPtr { get; }

		/// <summary>
		/// Gets type <see cref="Object"/>
		/// </summary>
		IDebuggerType Object { get; }

		/// <summary>
		/// Gets type <see cref="Decimal"/>
		/// </summary>
		IDebuggerType Decimal { get; }

		/// <summary>
		/// Gets the CLR AppDomain object or null if it hasn't been constructed yet
		/// </summary>
		IDebuggerValue CLRObject { get; }

		/// <summary>
		/// Sets the debug state of all managed threads
		/// </summary>
		/// <param name="state">New state</param>
		/// <param name="thread">Thread to exempt from the new state or null</param>
		void SetAllThreadsDebugState(ThreadState state, IDebuggerThread thread = null);
	}
}
