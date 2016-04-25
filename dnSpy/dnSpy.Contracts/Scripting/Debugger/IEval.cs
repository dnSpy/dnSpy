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
using System.Reflection;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// Calls a method in the debugged process. Call its <see cref="IDisposable.Dispose"/> method
	/// once you don't need the instance anymore so the debugger knows the evaluation has ended.
	/// </summary>
	public interface IEval : IDisposable {
		/// <summary>
		/// Creates a null value that can be passed to the debugged process
		/// </summary>
		/// <returns></returns>
		IDebuggerValue CreateNull();

		/// <summary>
		/// Box a value type
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Box(IDebuggerValue value);

		/// <summary>
		/// Creates a reference to a boxed value that can be passed to the debugged process
		/// </summary>
		/// <param name="value">A primitive type (ints, doubles, string, null),
		/// <see cref="IDebuggerValue"/>, <see cref="IDebuggerType"/>, <see cref="Type"/></param>
		/// <returns></returns>
		IDebuggerValue CreateBox(object value);

		/// <summary>
		/// Creates a reference to a boxed <see cref="bool"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(bool value);

		/// <summary>
		/// Creates a reference to a boxed <see cref="char"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(char value);

		/// <summary>
		/// Creates a reference to a boxed <see cref="sbyte"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(sbyte value);

		/// <summary>
		/// Creates a reference to a boxed <see cref="byte"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(byte value);

		/// <summary>
		/// Creates a reference to a boxed <see cref="short"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(short value);

		/// <summary>
		/// Creates a reference to a boxed <see cref="ushort"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(ushort value);

		/// <summary>
		/// Creates a reference to a boxed <see cref="int"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(int value);

		/// <summary>
		/// Creates a reference to a boxed <see cref="uint"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(uint value);

		/// <summary>
		/// Creates a reference to a boxed <see cref="long"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(long value);

		/// <summary>
		/// Creates a reference to a boxed <see cref="ulong"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(ulong value);

		/// <summary>
		/// Creates a reference to a boxed <see cref="float"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(float value);

		/// <summary>
		/// Creates a reference to a boxed <see cref="double"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(double value);

		/// <summary>
		/// Creates a reference to a boxed <see cref="IntPtr"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(IntPtr value);

		/// <summary>
		/// Creates a reference to a boxed <see cref="UIntPtr"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(UIntPtr value);

		/// <summary>
		/// Creates a reference to a boxed <see cref="decimal"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(decimal value);

		/// <summary>
		/// Creates a reference to a boxed type filled with 0s. The constructor isn't called.
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(IDebuggerType type);

		/// <summary>
		/// Creates a reference to a boxed type filled with 0s. The constructor isn't called.
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		IDebuggerValue CreateBox(Type type);

		/// <summary>
		/// Creates a <see cref="IDebuggerValue"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">A simple type (ints, doubles, string, null),
		/// arrays (<c>int[]</c>, <c>string[]</c>), <see cref="IDebuggerValue"/>,
		/// <see cref="IDebuggerType"/>, <see cref="Type"/>.
		/// Use <see cref="Debugger.Box"/> to box values or call <see cref="CreateBox(object)"/>.</param>
		/// <returns></returns>
		IDebuggerValue Create(object value);

		/// <summary>
		/// Creates a <see cref="bool"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Create(bool value);

		/// <summary>
		/// Creates a <see cref="char"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Create(char value);

		/// <summary>
		/// Creates a <see cref="sbyte"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Create(sbyte value);

		/// <summary>
		/// Creates a <see cref="byte"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Create(byte value);

		/// <summary>
		/// Creates a <see cref="short"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Create(short value);

		/// <summary>
		/// Creates a <see cref="ushort"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Create(ushort value);

		/// <summary>
		/// Creates a <see cref="int"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Create(int value);

		/// <summary>
		/// Creates a <see cref="uint"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Create(uint value);

		/// <summary>
		/// Creates a <see cref="long"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Create(long value);

		/// <summary>
		/// Creates a <see cref="ulong"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Create(ulong value);

		/// <summary>
		/// Creates a <see cref="float"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Create(float value);

		/// <summary>
		/// Creates a <see cref="double"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Create(double value);

		/// <summary>
		/// Creates a <see cref="IntPtr"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Create(IntPtr value);

		/// <summary>
		/// Creates a <see cref="UIntPtr"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Create(UIntPtr value);

		/// <summary>
		/// Creates a <see cref="decimal"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Create(decimal value);

		/// <summary>
		/// Creates a type filled with 0s. The constructor isn't called.
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		IDebuggerValue Create(IDebuggerType type);

		/// <summary>
		/// Creates a type filled with 0s. The constructor isn't called.
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		IDebuggerValue Create(Type type);

		/// <summary>
		/// Creates a <see cref="string"/> that can be passed to the debugged process
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		IDebuggerValue Create(string value);

		/// <summary>
		/// Creates an array. <paramref name="elementType"/> must be a primitive type (but not
		/// <see cref="IntPtr"/> or <see cref="UIntPtr"/>) or a reference type
		/// </summary>
		/// <param name="elementType">Array element type</param>
		/// <param name="length">Number of elements</param>
		/// <returns></returns>
		IDebuggerValue CreateArray(IDebuggerType elementType, int length);

		/// <summary>
		/// Creates an array. <paramref name="elementType"/> must be a primitive type (but not
		/// <see cref="IntPtr"/> or <see cref="UIntPtr"/>) or a reference type
		/// </summary>
		/// <param name="elementType">Array element type</param>
		/// <param name="length">Number of elements</param>
		/// <returns></returns>
		IDebuggerValue CreateArray(Type elementType, int length);

		/// <summary>
		/// Creates a <see cref="bool"/> array
		/// </summary>
		/// <param name="array">Data</param>
		/// <returns></returns>
		IDebuggerValue Create(bool[] array);

		/// <summary>
		/// Creates a <see cref="char"/> array
		/// </summary>
		/// <param name="array">Data</param>
		/// <returns></returns>
		IDebuggerValue Create(char[] array);

		/// <summary>
		/// Creates a <see cref="sbyte"/> array
		/// </summary>
		/// <param name="array">Data</param>
		/// <returns></returns>
		IDebuggerValue Create(sbyte[] array);

		/// <summary>
		/// Creates a <see cref="byte"/> array
		/// </summary>
		/// <param name="array">Data</param>
		/// <returns></returns>
		IDebuggerValue Create(byte[] array);

		/// <summary>
		/// Creates a <see cref="short"/> array
		/// </summary>
		/// <param name="array">Data</param>
		/// <returns></returns>
		IDebuggerValue Create(short[] array);

		/// <summary>
		/// Creates a <see cref="ushort"/> array
		/// </summary>
		/// <param name="array">Data</param>
		/// <returns></returns>
		IDebuggerValue Create(ushort[] array);

		/// <summary>
		/// Creates a <see cref="int"/> array
		/// </summary>
		/// <param name="array">Data</param>
		/// <returns></returns>
		IDebuggerValue Create(int[] array);

		/// <summary>
		/// Creates a <see cref="uint"/> array
		/// </summary>
		/// <param name="array">Data</param>
		/// <returns></returns>
		IDebuggerValue Create(uint[] array);

		/// <summary>
		/// Creates a <see cref="long"/> array
		/// </summary>
		/// <param name="array">Data</param>
		/// <returns></returns>
		IDebuggerValue Create(long[] array);

		/// <summary>
		/// Creates a <see cref="ulong"/> array
		/// </summary>
		/// <param name="array">Data</param>
		/// <returns></returns>
		IDebuggerValue Create(ulong[] array);

		/// <summary>
		/// Creates a <see cref="float"/> array
		/// </summary>
		/// <param name="array">Data</param>
		/// <returns></returns>
		IDebuggerValue Create(float[] array);

		/// <summary>
		/// Creates a <see cref="double"/> array
		/// </summary>
		/// <param name="array">Data</param>
		/// <returns></returns>
		IDebuggerValue Create(double[] array);

		/// <summary>
		/// Creates a <see cref="string"/> array
		/// </summary>
		/// <param name="array">Data</param>
		/// <returns></returns>
		IDebuggerValue Create(string[] array);

		/// <summary>
		/// Creates a new type instance. The returned type is a reference.
		/// If it's a value type, the returned type is a reference to a boxed value.
		/// </summary>
		/// <param name="ctor">Constructor to call</param>
		/// <param name="args">Arguments. Each argument is passed to <see cref="Create(object)"/>,
		/// see its documentation for more details.</param>
		/// <returns></returns>
		IDebuggerValue Create(IDebuggerMethod ctor, params object[] args);

		/// <summary>
		/// Creates a new type instance. The returned type is a reference.
		/// If it's a value type, the returned type is a reference to a boxed value.
		/// </summary>
		/// <param name="genericArgs">Generic type arguments (<see cref="Type"/> or <see cref="IDebuggerType"/> instances)</param>
		/// <param name="ctor">Constructor to call</param>
		/// <param name="args">Arguments. Each argument is passed to <see cref="Create(object)"/>,
		/// see its documentation for more details.</param>
		/// <returns></returns>
		IDebuggerValue Create(object[] genericArgs, IDebuggerMethod ctor, params object[] args);

		/// <summary>
		/// Calls <paramref name="method"/> in the debugged process
		/// </summary>
		/// <param name="method">Method to call</param>
		/// <param name="args">Arguments. If it's an instance method, the first argument is the
		/// <c>this</c> pointer. Each argument is passed to <see cref="Create(object)"/>, see its
		/// documentation for more details.</param>
		/// <returns></returns>
		IDebuggerValue Call(IDebuggerMethod method, params object[] args);

		/// <summary>
		/// Calls <paramref name="method"/> in the debugged process
		/// </summary>
		/// <param name="genericArgs">Generic type arguments followed by generic method arguments
		/// (<see cref="Type"/> or <see cref="IDebuggerType"/> instances)</param>
		/// <param name="method">Method to call</param>
		/// <param name="args">Arguments. If it's an instance method, the first argument is the
		/// <c>this</c> pointer. Each argument is passed to <see cref="Create(object)"/>, see its
		/// documentation for more details.</param>
		/// <returns></returns>
		IDebuggerValue Call(object[] genericArgs, IDebuggerMethod method, params object[] args);

		/// <summary>
		/// Calls the <c>ToString()</c> method
		/// </summary>
		/// <param name="value">Value</param>
		/// <returns></returns>
		string ToString(IDebuggerValue value);

		/// <summary>
		/// Loads the assembly in the debugged process by calling <see cref="Assembly.Load(byte[])"/>
		/// </summary>
		/// <param name="rawAssembly">Assembly bytes</param>
		/// <returns></returns>
		IDebuggerValue AssemblyLoad(byte[] rawAssembly);

		/// <summary>
		/// Loads the assembly in the debugged process by calling <see cref="Assembly.Load(string)"/>
		/// </summary>
		/// <param name="assemblyString">Assembly name</param>
		/// <returns></returns>
		IDebuggerValue AssemblyLoad(string assemblyString);

		/// <summary>
		/// Loads the assembly in the debugged process by calling <see cref="Assembly.LoadFrom(string)"/>
		/// </summary>
		/// <param name="assemblyFile">Assembly filename</param>
		/// <returns></returns>
		IDebuggerValue AssemblyLoadFrom(string assemblyFile);

		/// <summary>
		/// Loads the assembly in the debugged process by calling <see cref="Assembly.LoadFile(string)"/>
		/// </summary>
		/// <param name="filename">Assembly filename</param>
		/// <returns></returns>
		IDebuggerValue AssemblyLoadFile(string filename);
	}
}
