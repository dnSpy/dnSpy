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
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Scripting.Debugger;
using dnSpy.Shared.Scripting;
using DBG = dndbg.Engine;

namespace dnSpy.Debugger.Scripting {
	sealed class Eval : IEval {
		readonly Debugger debugger;
		readonly IAppDomain appDomain;
		readonly DBG.DnEval eval;
		List<DBG.CorValue> valuesToKeep;

		public Eval(Debugger debugger, IAppDomain appDomain, DBG.DnEval eval) {
			if (appDomain == null)
				throw new InvalidOperationException("The thread has no owner AppDomain and can't be used to evaluate funcs");
			this.debugger = debugger;
			this.appDomain = appDomain;
			this.eval = eval;
			this.eval.SetNoTotalTimeout();
		}

		void Keep(DBG.CorValue v) {
			debugger.Dispatcher.VerifyAccess();
			if (v == null)
				return;
			if (valuesToKeep == null)
				valuesToKeep = new List<DBG.CorValue>();
			valuesToKeep.Add(v);
		}

		IDebuggerType GetPrimitiveType(CorElementType etype) {
			switch (etype) {
			case CorElementType.Void:		return appDomain.Void;
			case CorElementType.Boolean:	return appDomain.Boolean;
			case CorElementType.Char:		return appDomain.Char;
			case CorElementType.I1:			return appDomain.SByte;
			case CorElementType.U1:			return appDomain.Byte;
			case CorElementType.I2:			return appDomain.Int16;
			case CorElementType.U2:			return appDomain.UInt16;
			case CorElementType.I4:			return appDomain.Int32;
			case CorElementType.U4:			return appDomain.UInt32;
			case CorElementType.I8:			return appDomain.Int64;
			case CorElementType.U8:			return appDomain.UInt64;
			case CorElementType.R4:			return appDomain.Single;
			case CorElementType.R8:			return appDomain.Double;
			case CorElementType.String:		return appDomain.String;
			case CorElementType.TypedByRef:	return appDomain.TypedReference;
			case CorElementType.I:			return appDomain.IntPtr;
			case CorElementType.U:			return appDomain.UIntPtr;
			case CorElementType.Object:		return appDomain.Object;
			default: return null;
			}
		}

		public IDebuggerValue Box(IDebuggerValue value) {
			return debugger.Dispatcher.UI(() => {
				var valueTmp = (DebuggerValue)value;
				if (valueTmp.CorValue.IsGeneric) {
					var et = valueTmp.CorValue.ExactType;
					if (et != null && et.IsPrimitiveValueType) {
						var type = GetPrimitiveType((CorElementType)et.ElementType);
						Debug.Assert(type != null);
						if (type != null) {
							value = CreateNoConstructorUI(type);
							value.DereferencedValue.BoxedValue.Write(valueTmp.Read());
							return value;
						}
					}
				}

				var boxedValue = eval.Box(((DebuggerValue)value).CorValue);
				if (boxedValue == null)
					throw new ScriptException("Could not box the value");
				return new DebuggerValue(debugger, boxedValue);
			});
		}

		public IDebuggerValue CreateBox(object value) {
			return debugger.Dispatcher.UI(() => {
				var v = value as IDebuggerValue;
				if (v != null)
					return Box(v);

				if (value is bool)
					return CreateBox((bool)value);
				if (value is char)
					return CreateBox((char)value);
				if (value is sbyte)
					return CreateBox((sbyte)value);
				if (value is byte)
					return CreateBox((byte)value);
				if (value is short)
					return CreateBox((short)value);
				if (value is ushort)
					return CreateBox((ushort)value);
				if (value is int)
					return CreateBox((int)value);
				if (value is uint)
					return CreateBox((uint)value);
				if (value is long)
					return CreateBox((long)value);
				if (value is ulong)
					return CreateBox((ulong)value);
				if (value is float)
					return CreateBox((float)value);
				if (value is double)
					return CreateBox((double)value);
				if (value is IntPtr)
					return CreateBox((IntPtr)value);
				if (value is UIntPtr)
					return CreateBox((UIntPtr)value);
				if (value is decimal)
					return CreateBox((decimal)value);

				if (value is IDebuggerType)
					return CreateBox((IDebuggerType)value);
				if (value is Type)
					return CreateBox((Type)value);

				return Create(value);
			});
		}

		public IDebuggerValue CreateBox(bool value) {
			return debugger.Dispatcher.UI(() => {
				var refBoxValue = CreateNoConstructorUI(appDomain.Boolean);
				if (value != false) {
					var boxedValue = refBoxValue.CorValue.DereferencedValue.BoxedValue;
					boxedValue.WriteGenericValue(BitConverter.GetBytes(value));
				}
				return refBoxValue;
			});
		}

		public IDebuggerValue CreateBox(char value) {
			return debugger.Dispatcher.UI(() => {
				var refBoxValue = CreateNoConstructorUI(appDomain.Char);
				if (value != 0) {
					var boxedValue = refBoxValue.CorValue.DereferencedValue.BoxedValue;
					boxedValue.WriteGenericValue(BitConverter.GetBytes(value));
				}
				return refBoxValue;
			});
		}

		public IDebuggerValue CreateBox(sbyte value) {
			return debugger.Dispatcher.UI(() => {
				var refBoxValue = CreateNoConstructorUI(appDomain.SByte);
				if (value != 0) {
					var boxedValue = refBoxValue.CorValue.DereferencedValue.BoxedValue;
					boxedValue.WriteGenericValue(new byte[1] { (byte)value });
				}
				return refBoxValue;
			});
		}

		public IDebuggerValue CreateBox(byte value) {
			return debugger.Dispatcher.UI(() => {
				var refBoxValue = CreateNoConstructorUI(appDomain.Byte);
				if (value != 0) {
					var boxedValue = refBoxValue.CorValue.DereferencedValue.BoxedValue;
					boxedValue.WriteGenericValue(new byte[1] { value });
				}
				return refBoxValue;
			});
		}

		public IDebuggerValue CreateBox(short value) {
			return debugger.Dispatcher.UI(() => {
				var refBoxValue = CreateNoConstructorUI(appDomain.Int16);
				if (value != 0) {
					var boxedValue = refBoxValue.CorValue.DereferencedValue.BoxedValue;
					boxedValue.WriteGenericValue(BitConverter.GetBytes(value));
				}
				return refBoxValue;
			});
		}

		public IDebuggerValue CreateBox(ushort value) {
			return debugger.Dispatcher.UI(() => {
				var refBoxValue = CreateNoConstructorUI(appDomain.UInt16);
				if (value != 0) {
					var boxedValue = refBoxValue.CorValue.DereferencedValue.BoxedValue;
					boxedValue.WriteGenericValue(BitConverter.GetBytes(value));
				}
				return refBoxValue;
			});
		}

		public IDebuggerValue CreateBox(int value) {
			return debugger.Dispatcher.UI(() => {
				var refBoxValue = CreateNoConstructorUI(appDomain.Int32);
				if (value != 0) {
					var boxedValue = refBoxValue.CorValue.DereferencedValue.BoxedValue;
					boxedValue.WriteGenericValue(BitConverter.GetBytes(value));
				}
				return refBoxValue;
			});
		}

		public IDebuggerValue CreateBox(uint value) {
			return debugger.Dispatcher.UI(() => {
				var refBoxValue = CreateNoConstructorUI(appDomain.UInt32);
				if (value != 0) {
					var boxedValue = refBoxValue.CorValue.DereferencedValue.BoxedValue;
					boxedValue.WriteGenericValue(BitConverter.GetBytes(value));
				}
				return refBoxValue;
			});
		}

		public IDebuggerValue CreateBox(long value) {
			return debugger.Dispatcher.UI(() => {
				var refBoxValue = CreateNoConstructorUI(appDomain.Int64);
				if (value != 0) {
					var boxedValue = refBoxValue.CorValue.DereferencedValue.BoxedValue;
					boxedValue.WriteGenericValue(BitConverter.GetBytes(value));
				}
				return refBoxValue;
			});
		}

		public IDebuggerValue CreateBox(ulong value) {
			return debugger.Dispatcher.UI(() => {
				var refBoxValue = CreateNoConstructorUI(appDomain.UInt64);
				if (value != 0) {
					var boxedValue = refBoxValue.CorValue.DereferencedValue.BoxedValue;
					boxedValue.WriteGenericValue(BitConverter.GetBytes(value));
				}
				return refBoxValue;
			});
		}

		public IDebuggerValue CreateBox(float value) {
			return debugger.Dispatcher.UI(() => {
				var refBoxValue = CreateNoConstructorUI(appDomain.Single);
				if (value != 0) {
					var boxedValue = refBoxValue.CorValue.DereferencedValue.BoxedValue;
					boxedValue.WriteGenericValue(BitConverter.GetBytes(value));
				}
				return refBoxValue;
			});
		}

		public IDebuggerValue CreateBox(double value) {
			return debugger.Dispatcher.UI(() => {
				var refBoxValue = CreateNoConstructorUI(appDomain.Double);
				if (value != 0) {
					var boxedValue = refBoxValue.CorValue.DereferencedValue.BoxedValue;
					boxedValue.WriteGenericValue(BitConverter.GetBytes(value));
				}
				return refBoxValue;
			});
		}

		public IDebuggerValue CreateBox(IntPtr value) {
			return debugger.Dispatcher.UI(() => {
				var refBoxValue = CreateNoConstructorUI(appDomain.IntPtr);
				if (value != IntPtr.Zero) {
					var boxedValue = refBoxValue.CorValue.DereferencedValue.BoxedValue;
					var bytes = IntPtr.Size == 4 ?
							BitConverter.GetBytes(value.ToInt32()) :
							BitConverter.GetBytes(value.ToInt64());
					boxedValue.WriteGenericValue(bytes);
				}
				return refBoxValue;
			});
		}

		public IDebuggerValue CreateBox(UIntPtr value) {
			return debugger.Dispatcher.UI(() => {
				var refBoxValue = CreateNoConstructorUI(appDomain.UIntPtr);
				if (value != UIntPtr.Zero) {
					var boxedValue = refBoxValue.CorValue.DereferencedValue.BoxedValue;
					var bytes = UIntPtr.Size == 4 ?
							BitConverter.GetBytes(value.ToUInt32()) :
							BitConverter.GetBytes(value.ToUInt64());
					boxedValue.WriteGenericValue(bytes);
				}
				return refBoxValue;
			});
		}

		public IDebuggerValue CreateBox(decimal value) {
			return debugger.Dispatcher.UI(() => {
				var refBoxValue = CreateNoConstructorUI(appDomain.Decimal);
				if (value != 0) {
					var boxedValue = refBoxValue.CorValue.DereferencedValue.BoxedValue;
					boxedValue.WriteGenericValue(Utils.GetBytes(value));
				}
				return refBoxValue;
			});
		}

		public IDebuggerValue CreateBox(IDebuggerType type) {
			return debugger.Dispatcher.UI(() => CreateNoConstructorUI(type));
		}

		public IDebuggerValue CreateBox(Type type) {
			return debugger.Dispatcher.UI(() => CreateNoConstructorUI(FindTypeThrow(type)));
		}

		IDebuggerType FindTypeThrow(Type type) {
			var t = appDomain.GetType(type);
			if (t != null)
				return t;
			throw new ArgumentException(string.Format("Couldn't find type `{0}' in one of the loaded assemblies", type.FullName));
		}

		DBG.CorType[] CreateTypesThrow(object[] types) {
			if (types == null)
				return null;
			var res = new DBG.CorType[types.Length];
			for (int i = 0; i < res.Length; i++) {
				var type = types[i];
				var dt = type as DebuggerType;
				if (dt != null)
					res[i] = dt.CorType;
				else
					res[i] = ((DebuggerType)FindTypeThrow(type as Type)).CorType;
			}
			return res;
		}

		public IDebuggerValue Create(object value) {
			return debugger.Dispatcher.UI(() => {
				if (value == null)
					return CreateNull();

				var box = value as Box;
				if (box != null)
					return CreateBox(box.Value);

				var v = value as IDebuggerValue;
				if (v != null)
					return v;
				var s = value as string;
				if (s != null)
					return Create(s);

				if (value is bool)
					return Create((bool)value);
				if (value is char)
					return Create((char)value);
				if (value is sbyte)
					return Create((sbyte)value);
				if (value is byte)
					return Create((byte)value);
				if (value is short)
					return Create((short)value);
				if (value is ushort)
					return Create((ushort)value);
				if (value is int)
					return Create((int)value);
				if (value is uint)
					return Create((uint)value);
				if (value is long)
					return Create((long)value);
				if (value is ulong)
					return Create((ulong)value);
				if (value is float)
					return Create((float)value);
				if (value is double)
					return Create((double)value);
				if (value is IntPtr)
					return Create((IntPtr)value);
				if (value is UIntPtr)
					return Create((UIntPtr)value);
				if (value is decimal)
					return Create((decimal)value);

				if (value is Array) {
					if (value is bool[])
						return Create((bool[])value);
					if (value is char[])
						return Create((char[])value);
					if (value is sbyte[])
						return Create((sbyte[])value);
					if (value is byte[])
						return Create((byte[])value);
					if (value is short[])
						return Create((short[])value);
					if (value is ushort[])
						return Create((ushort[])value);
					if (value is int[])
						return Create((int[])value);
					if (value is uint[])
						return Create((uint[])value);
					if (value is long[])
						return Create((long[])value);
					if (value is ulong[])
						return Create((ulong[])value);
					if (value is float[])
						return Create((float[])value);
					if (value is double[])
						return Create((double[])value);
					if (value is string[])
						return Create((string[])value);
				}

				if (value is IDebuggerType)
					return Create((IDebuggerType)value);
				if (value is Type)
					return Create((Type)value);

				throw new ArgumentException("Invalid value, supported: integers, floating point numbers, strings, Type, IDebuggerType, IDebuggerValue, arrays, null, new Box(XXX): " + value);
			});
		}

		IDebuggerValue Create(DBG.CorValue v) {
			if (v == null)
				throw new InvalidOperationException("Could not create a new value");
			return new DebuggerValue(debugger, v);
		}

		public IDebuggerValue CreateNull() {
			return debugger.Dispatcher.UI(() => Create(eval.CreateNull()));
		}

		public IDebuggerValue Create(string s) {
			if (s == null)
				return CreateNull();
			return debugger.Dispatcher.UI(() => {
				int hr;
				var res = eval.CreateString(s, out hr);
				if (res == null)
					throw new ScriptException(string.Format("Could not create a string: 0x{0:X8}", hr));
				return res.Value.ToDebuggerValue(debugger);
			});
		}

		public IDebuggerValue Create(bool value) {
			return debugger.Dispatcher.UI(() => {
				var res = CreateNoConstructorUI(appDomain.Boolean).CorValue;
				Keep(res);
				Debug.Assert(res.DereferencedValue != null && res.DereferencedValue.BoxedValue != null);
				res = res.DereferencedValue.BoxedValue;
				if (value != false)
					res.WriteGenericValue(BitConverter.GetBytes(value));
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Create(char value) {
			return debugger.Dispatcher.UI(() => {
				var res = CreateNoConstructorUI(appDomain.Char).CorValue;
				Keep(res);
				Debug.Assert(res.DereferencedValue != null && res.DereferencedValue.BoxedValue != null);
				res = res.DereferencedValue.BoxedValue;
				if (value != 0)
					res.WriteGenericValue(BitConverter.GetBytes(value));
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Create(sbyte value) {
			return debugger.Dispatcher.UI(() => {
				var res = CreateNoConstructorUI(appDomain.SByte).CorValue;
				Keep(res);
				Debug.Assert(res.DereferencedValue != null && res.DereferencedValue.BoxedValue != null);
				res = res.DereferencedValue.BoxedValue;
				if (value != 0)
					res.WriteGenericValue(new byte[1] { (byte)value });
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Create(byte value) {
			return debugger.Dispatcher.UI(() => {
				var res = CreateNoConstructorUI(appDomain.Byte).CorValue;
				Keep(res);
				Debug.Assert(res.DereferencedValue != null && res.DereferencedValue.BoxedValue != null);
				res = res.DereferencedValue.BoxedValue;
				if (value != 0)
					res.WriteGenericValue(new byte[1] { value });
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Create(short value) {
			return debugger.Dispatcher.UI(() => {
				var res = CreateNoConstructorUI(appDomain.Int16).CorValue;
				Keep(res);
				Debug.Assert(res.DereferencedValue != null && res.DereferencedValue.BoxedValue != null);
				res = res.DereferencedValue.BoxedValue;
				if (value != 0)
					res.WriteGenericValue(BitConverter.GetBytes(value));
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Create(ushort value) {
			return debugger.Dispatcher.UI(() => {
				var res = CreateNoConstructorUI(appDomain.UInt16).CorValue;
				Keep(res);
				Debug.Assert(res.DereferencedValue != null && res.DereferencedValue.BoxedValue != null);
				res = res.DereferencedValue.BoxedValue;
				if (value != 0)
					res.WriteGenericValue(BitConverter.GetBytes(value));
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Create(int value) {
			return debugger.Dispatcher.UI(() => {
				var res = CreateNoConstructorUI(appDomain.Int32).CorValue;
				Keep(res);
				Debug.Assert(res.DereferencedValue != null && res.DereferencedValue.BoxedValue != null);
				res = res.DereferencedValue.BoxedValue;
				if (value != 0)
					res.WriteGenericValue(BitConverter.GetBytes(value));
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Create(uint value) {
			return debugger.Dispatcher.UI(() => {
				var res = CreateNoConstructorUI(appDomain.UInt32).CorValue;
				Keep(res);
				Debug.Assert(res.DereferencedValue != null && res.DereferencedValue.BoxedValue != null);
				res = res.DereferencedValue.BoxedValue;
				if (value != 0)
					res.WriteGenericValue(BitConverter.GetBytes(value));
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Create(long value) {
			return debugger.Dispatcher.UI(() => {
				var res = CreateNoConstructorUI(appDomain.Int64).CorValue;
				Keep(res);
				Debug.Assert(res.DereferencedValue != null && res.DereferencedValue.BoxedValue != null);
				res = res.DereferencedValue.BoxedValue;
				if (value != 0)
					res.WriteGenericValue(BitConverter.GetBytes(value));
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Create(ulong value) {
			return debugger.Dispatcher.UI(() => {
				var res = CreateNoConstructorUI(appDomain.UInt64).CorValue;
				Keep(res);
				Debug.Assert(res.DereferencedValue != null && res.DereferencedValue.BoxedValue != null);
				res = res.DereferencedValue.BoxedValue;
				if (value != 0)
					res.WriteGenericValue(BitConverter.GetBytes(value));
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Create(float value) {
			return debugger.Dispatcher.UI(() => {
				var res = CreateNoConstructorUI(appDomain.Single).CorValue;
				Keep(res);
				Debug.Assert(res.DereferencedValue != null && res.DereferencedValue.BoxedValue != null);
				res = res.DereferencedValue.BoxedValue;
				if (value != 0)
					res.WriteGenericValue(BitConverter.GetBytes(value));
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Create(double value) {
			return debugger.Dispatcher.UI(() => {
				var res = CreateNoConstructorUI(appDomain.Double).CorValue;
				Keep(res);
				Debug.Assert(res.DereferencedValue != null && res.DereferencedValue.BoxedValue != null);
				res = res.DereferencedValue.BoxedValue;
				if (value != 0)
					res.WriteGenericValue(BitConverter.GetBytes(value));
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Create(IntPtr value) {
			return debugger.Dispatcher.UI(() => {
				var res = CreateNoConstructorUI(appDomain.IntPtr).CorValue;
				Keep(res);
				Debug.Assert(res.DereferencedValue != null && res.DereferencedValue.BoxedValue != null);
				res = res.DereferencedValue.BoxedValue;
				if (value != IntPtr.Zero) {
					var bytes = IntPtr.Size == 4 ?
							BitConverter.GetBytes(value.ToInt32()) :
							BitConverter.GetBytes(value.ToInt64());
					res.WriteGenericValue(bytes);
				}
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Create(UIntPtr value) {
			return debugger.Dispatcher.UI(() => {
				var res = CreateNoConstructorUI(appDomain.UIntPtr).CorValue;
				Keep(res);
				Debug.Assert(res.DereferencedValue != null && res.DereferencedValue.BoxedValue != null);
				res = res.DereferencedValue.BoxedValue;
				if (value != UIntPtr.Zero) {
					var bytes = UIntPtr.Size == 4 ?
							BitConverter.GetBytes(value.ToUInt32()) :
							BitConverter.GetBytes(value.ToUInt64());
					res.WriteGenericValue(bytes);
				}
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Create(decimal value) {
			return debugger.Dispatcher.UI(() => {
				var res = CreateNoConstructorUI(appDomain.Decimal).CorValue;
				Keep(res);
				Debug.Assert(res.DereferencedValue != null && res.DereferencedValue.BoxedValue != null);
				res = res.DereferencedValue.BoxedValue;
				if (value != 0)
					res.WriteGenericValue(Utils.GetBytes(value));
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue CreateArray(IDebuggerType elementType, int length) {
			return debugger.Dispatcher.UI(() => {
				var res = eval.CreateSZArray(((DebuggerType)elementType).CorType, length);
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue CreateArray(Type elementType, int length) {
			return debugger.Dispatcher.UI(() => CreateArray(FindTypeThrow(elementType), length));
		}

		public unsafe IDebuggerValue Create(bool[] array) {
			return debugger.Dispatcher.UI(() => {
				var res = eval.CreateSZArray(((DebuggerType)appDomain.Boolean).CorType, array.Length);
				var ra = res.DereferencedValue;
				Debug.Assert(ra.IsArray && ra.ArrayCount == (uint)array.Length);
				if (array.Length > 0) {
					int totalSize = array.Length * 1;
					int writtenBytes = 0;
					var elemValue = ra.GetElementAtPosition(0);
					if (elemValue != null) {
						fixed (void* p = &array[0])
							writtenBytes = debugger.WriteMemory(elemValue.Address, new IntPtr(p), totalSize);
					}
					if (totalSize != writtenBytes)
						throw new ScriptException("Couldn't write all bytes to the array");
				}
				return new DebuggerValue(debugger, res);
			});
		}

		public unsafe IDebuggerValue Create(char[] array) {
			return debugger.Dispatcher.UI(() => {
				var res = eval.CreateSZArray(((DebuggerType)appDomain.Char).CorType, array.Length);
				var ra = res.DereferencedValue;
				Debug.Assert(ra.IsArray && ra.ArrayCount == (uint)array.Length);
				if (array.Length > 0) {
					int totalSize = array.Length * 2;
					int writtenBytes = 0;
					var elemValue = ra.GetElementAtPosition(0);
					if (elemValue != null) {
						fixed (void* p = &array[0])
							writtenBytes = debugger.WriteMemory(elemValue.Address, new IntPtr(p), totalSize);
					}
					if (totalSize != writtenBytes)
						throw new ScriptException("Couldn't write all bytes to the array");
				}
				return new DebuggerValue(debugger, res);
			});
		}

		public unsafe IDebuggerValue Create(sbyte[] array) {
			return debugger.Dispatcher.UI(() => {
				var res = eval.CreateSZArray(((DebuggerType)appDomain.SByte).CorType, array.Length);
				var ra = res.DereferencedValue;
				Debug.Assert(ra.IsArray && ra.ArrayCount == (uint)array.Length);
				if (array.Length > 0) {
					int totalSize = array.Length * 1;
					int writtenBytes = 0;
					var elemValue = ra.GetElementAtPosition(0);
					if (elemValue != null) {
						fixed (void* p = &array[0])
							writtenBytes = debugger.WriteMemory(elemValue.Address, new IntPtr(p), totalSize);
					}
					if (totalSize != writtenBytes)
						throw new ScriptException("Couldn't write all bytes to the array");
				}
				return new DebuggerValue(debugger, res);
			});
		}

		public unsafe IDebuggerValue Create(byte[] array) {
			return debugger.Dispatcher.UI(() => {
				var res = eval.CreateSZArray(((DebuggerType)appDomain.Byte).CorType, array.Length);
				var ra = res.DereferencedValue;
				Debug.Assert(ra.IsArray && ra.ArrayCount == (uint)array.Length);
				if (array.Length > 0) {
					int totalSize = array.Length * 1;
					int writtenBytes = 0;
					var elemValue = ra.GetElementAtPosition(0);
					if (elemValue != null) {
						fixed (void* p = &array[0])
							writtenBytes = debugger.WriteMemory(elemValue.Address, new IntPtr(p), totalSize);
					}
					if (totalSize != writtenBytes)
						throw new ScriptException("Couldn't write all bytes to the array");
				}
				return new DebuggerValue(debugger, res);
			});
		}

		public unsafe IDebuggerValue Create(short[] array) {
			return debugger.Dispatcher.UI(() => {
				var res = eval.CreateSZArray(((DebuggerType)appDomain.Int16).CorType, array.Length);
				var ra = res.DereferencedValue;
				Debug.Assert(ra.IsArray && ra.ArrayCount == (uint)array.Length);
				if (array.Length > 0) {
					int totalSize = array.Length * 2;
					int writtenBytes = 0;
					var elemValue = ra.GetElementAtPosition(0);
					if (elemValue != null) {
						fixed (void* p = &array[0])
							writtenBytes = debugger.WriteMemory(elemValue.Address, new IntPtr(p), totalSize);
					}
					if (totalSize != writtenBytes)
						throw new ScriptException("Couldn't write all bytes to the array");
				}
				return new DebuggerValue(debugger, res);
			});
		}

		public unsafe IDebuggerValue Create(ushort[] array) {
			return debugger.Dispatcher.UI(() => {
				var res = eval.CreateSZArray(((DebuggerType)appDomain.UInt16).CorType, array.Length);
				var ra = res.DereferencedValue;
				Debug.Assert(ra.IsArray && ra.ArrayCount == (uint)array.Length);
				if (array.Length > 0) {
					int totalSize = array.Length * 2;
					int writtenBytes = 0;
					var elemValue = ra.GetElementAtPosition(0);
					if (elemValue != null) {
						fixed (void* p = &array[0])
							writtenBytes = debugger.WriteMemory(elemValue.Address, new IntPtr(p), totalSize);
					}
					if (totalSize != writtenBytes)
						throw new ScriptException("Couldn't write all bytes to the array");
				}
				return new DebuggerValue(debugger, res);
			});
		}

		public unsafe IDebuggerValue Create(int[] array) {
			return debugger.Dispatcher.UI(() => {
				var res = eval.CreateSZArray(((DebuggerType)appDomain.Int32).CorType, array.Length);
				var ra = res.DereferencedValue;
				Debug.Assert(ra.IsArray && ra.ArrayCount == (uint)array.Length);
				if (array.Length > 0) {
					int totalSize = array.Length * 4;
					int writtenBytes = 0;
					var elemValue = ra.GetElementAtPosition(0);
					if (elemValue != null) {
						fixed (void* p = &array[0])
							writtenBytes = debugger.WriteMemory(elemValue.Address, new IntPtr(p), totalSize);
					}
					if (totalSize != writtenBytes)
						throw new ScriptException("Couldn't write all bytes to the array");
				}
				return new DebuggerValue(debugger, res);
			});
		}

		public unsafe IDebuggerValue Create(uint[] array) {
			return debugger.Dispatcher.UI(() => {
				var res = eval.CreateSZArray(((DebuggerType)appDomain.UInt32).CorType, array.Length);
				var ra = res.DereferencedValue;
				Debug.Assert(ra.IsArray && ra.ArrayCount == (uint)array.Length);
				if (array.Length > 0) {
					int totalSize = array.Length * 4;
					int writtenBytes = 0;
					var elemValue = ra.GetElementAtPosition(0);
					if (elemValue != null) {
						fixed (void* p = &array[0])
							writtenBytes = debugger.WriteMemory(elemValue.Address, new IntPtr(p), totalSize);
					}
					if (totalSize != writtenBytes)
						throw new ScriptException("Couldn't write all bytes to the array");
				}
				return new DebuggerValue(debugger, res);
			});
		}

		public unsafe IDebuggerValue Create(long[] array) {
			return debugger.Dispatcher.UI(() => {
				var res = eval.CreateSZArray(((DebuggerType)appDomain.Int64).CorType, array.Length);
				var ra = res.DereferencedValue;
				Debug.Assert(ra.IsArray && ra.ArrayCount == (uint)array.Length);
				if (array.Length > 0) {
					int totalSize = array.Length * 8;
					int writtenBytes = 0;
					var elemValue = ra.GetElementAtPosition(0);
					if (elemValue != null) {
						fixed (void* p = &array[0])
							writtenBytes = debugger.WriteMemory(elemValue.Address, new IntPtr(p), totalSize);
					}
					if (totalSize != writtenBytes)
						throw new ScriptException("Couldn't write all bytes to the array");
				}
				return new DebuggerValue(debugger, res);
			});
		}

		public unsafe IDebuggerValue Create(ulong[] array) {
			return debugger.Dispatcher.UI(() => {
				var res = eval.CreateSZArray(((DebuggerType)appDomain.UInt64).CorType, array.Length);
				var ra = res.DereferencedValue;
				Debug.Assert(ra.IsArray && ra.ArrayCount == (uint)array.Length);
				if (array.Length > 0) {
					int totalSize = array.Length * 8;
					int writtenBytes = 0;
					var elemValue = ra.GetElementAtPosition(0);
					if (elemValue != null) {
						fixed (void* p = &array[0])
							writtenBytes = debugger.WriteMemory(elemValue.Address, new IntPtr(p), totalSize);
					}
					if (totalSize != writtenBytes)
						throw new ScriptException("Couldn't write all bytes to the array");
				}
				return new DebuggerValue(debugger, res);
			});
		}

		public unsafe IDebuggerValue Create(float[] array) {
			return debugger.Dispatcher.UI(() => {
				var res = eval.CreateSZArray(((DebuggerType)appDomain.Single).CorType, array.Length);
				var ra = res.DereferencedValue;
				Debug.Assert(ra.IsArray && ra.ArrayCount == (uint)array.Length);
				if (array.Length > 0) {
					int totalSize = array.Length * 4;
					int writtenBytes = 0;
					var elemValue = ra.GetElementAtPosition(0);
					if (elemValue != null) {
						fixed (void* p = &array[0])
							writtenBytes = debugger.WriteMemory(elemValue.Address, new IntPtr(p), totalSize);
					}
					if (totalSize != writtenBytes)
						throw new ScriptException("Couldn't write all bytes to the array");
				}
				return new DebuggerValue(debugger, res);
			});
		}

		public unsafe IDebuggerValue Create(double[] array) {
			return debugger.Dispatcher.UI(() => {
				var res = eval.CreateSZArray(((DebuggerType)appDomain.Double).CorType, array.Length);
				var ra = res.DereferencedValue;
				Debug.Assert(ra.IsArray && ra.ArrayCount == (uint)array.Length);
				if (array.Length > 0) {
					int totalSize = array.Length * 8;
					int writtenBytes = 0;
					var elemValue = ra.GetElementAtPosition(0);
					if (elemValue != null) {
						fixed (void* p = &array[0])
							writtenBytes = debugger.WriteMemory(elemValue.Address, new IntPtr(p), totalSize);
					}
					if (totalSize != writtenBytes)
						throw new ScriptException("Couldn't write all bytes to the array");
				}
				return new DebuggerValue(debugger, res);
			});
		}

		public unsafe IDebuggerValue Create(string[] array) {
			return debugger.Dispatcher.UI(() => {
				var res = eval.CreateSZArray(((DebuggerType)appDomain.String).CorType, array.Length);
				if (array.Length > 0) {
					for (int i = 0; i < array.Length; i++) {
						var s = array[i];
						if (s == null)
							continue;
						// Always re-read it since it gets neutered
						var ra = res.DereferencedValue;
						var elem = ra.GetElementAtPosition(i);
						var newString = Create(s);
						elem.ReferenceAddress = newString.ReferenceAddress;
					}
				}
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Create(IDebuggerType type) {
			return debugger.Dispatcher.UI(() => {
				var res = CreateNoConstructorUI(type).CorValue;
				var dv = res.DereferencedValue;
				if (dv == null || dv.BoxedValue == null)
					return new DebuggerValue(debugger, res);
				Keep(res);
				res = res.DereferencedValue.BoxedValue;
				return new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Create(Type type) {
			return debugger.Dispatcher.UI(() => Create(FindTypeThrow(type)));
		}

		DebuggerValue CreateNoConstructorUI(IDebuggerType type) {
			debugger.Dispatcher.VerifyAccess();
			int hr;
			var res = eval.CreateDontCallConstructor(((DebuggerType)type).CorType, out hr);
			if (res == null)
				throw new ScriptException(string.Format("Could not create a type value: 0x{0:X8}", hr));
			return (DebuggerValue)res.Value.ToDebuggerValue(debugger);
		}

		public IDebuggerValue Create(IDebuggerMethod ctor, params object[] args) {
			return debugger.Dispatcher.UI(() => eval.CallConstructor(((DebuggerMethod)ctor).CorFunction, CreateArgs(args)).ToDebuggerValue(debugger));
		}

		public IDebuggerValue Create(object[] genericArgs, IDebuggerMethod ctor, params object[] args) {
			return debugger.Dispatcher.UI(() => eval.CallConstructor(((DebuggerMethod)ctor).CorFunction, CreateTypesThrow(genericArgs), CreateArgs(args)).ToDebuggerValue(debugger));
		}

		public IDebuggerValue Call(IDebuggerMethod method, params object[] args) {
			return debugger.Dispatcher.UI(() => eval.Call(((DebuggerMethod)method).CorFunction, CreateArgs(args)).ToDebuggerValue(debugger));
		}

		public IDebuggerValue Call(object[] genericArgs, IDebuggerMethod method, params object[] args) {
			return debugger.Dispatcher.UI(() => eval.Call(((DebuggerMethod)method).CorFunction, CreateTypesThrow(genericArgs), CreateArgs(args)).ToDebuggerValue(debugger));
		}

		DBG.CorValue[] CreateArgs(object[] args) {
			var res = new DBG.CorValue[args.Length];
			for (int i = 0; i < args.Length; i++) {
				var v = (DebuggerValue)Create(args[i]);
				res[i] = v.CorValue;
			}
			return res;
		}

		public string ToString(IDebuggerValue value) {
			return debugger.Dispatcher.UI(() => {
				var valueTmp = (DebuggerValue)value;
				var et = valueTmp.CorValue.ExactType;
				if (et != null && et.IsPrimitiveValueType)
					valueTmp = (DebuggerValue)Box(valueTmp);

				var v = valueTmp.CorValue;
				var objType = ((DebuggerType)appDomain.Object).CorType;
				var info = objType.GetToStringMethod();
				Debug.Assert(info != null);
				if (info == null)
					return null;
				var objCls = objType.Class;
				var corLibMod = objCls == null ? null : objCls.Module;
				var toStringFunc = corLibMod == null ? null : corLibMod.GetFunctionFromToken(info.Token);
				if (toStringFunc == null)
					return null;
				if (v.IsReference && v.Type == dndbg.COM.CorDebug.CorElementType.ByRef)
					v = v.NeuterCheckDereferencedValue;
				if (v != null && v.IsGeneric && !v.IsHeap && v.ExactType.IsValueType)
					Keep(v = eval.Box(v));
				if (v == null)
					return null;

				int hr;
				var res = eval.Call(toStringFunc, null, new DBG.CorValue[1] { v }, out hr);
				if (res == null || res.Value.WasException)
					return null;
				var rv = res.Value.ResultOrException;
				if (rv == null || rv.IsNull)
					return null;
				if (rv != null && rv.IsReference)
					rv = rv.NeuterCheckDereferencedValue;
				if (rv == null || !rv.IsString || rv.IsNull)
					return null;
				return rv.String;
			});
		}

		IDebuggerClass FindAssemblyClassThrow() {
			var cls = appDomain.CorLib.GetClass("System.Reflection.Assembly");
			if (cls == null)
				throw new ScriptException("Couldn't find System.Reflection.Assembly class");
			return cls;
		}

		IDebuggerMethod FindAssemblyLoadByteArrayThrow() {
			foreach (var method in FindAssemblyClassThrow().GetMethods("Load")) {
				var sig = method.MethodSig;
				if (sig.HasThis || sig.Params.Count != 1)
					continue;
				var t = sig.Params[0].RemovePinnedAndModifiers();
				if (t.ElementType != ElementType.SZArray)
					continue;
				if (t.Next.RemovePinnedAndModifiers().ElementType != ElementType.U1)
					continue;

				return method;
			}
			throw new ScriptException("Could not find System.Reflection.Assembly.Load(byte[])");
		}

		IDebuggerMethod FindAssemblyLoadStringThrow() {
			foreach (var method in FindAssemblyClassThrow().GetMethods("Load")) {
				var sig = method.MethodSig;
				if (sig.HasThis || sig.Params.Count != 1)
					continue;
				if (sig.Params[0].RemovePinnedAndModifiers().ElementType != ElementType.String)
					continue;

				return method;
			}
			throw new ScriptException("Could not find System.Reflection.Assembly.Load(string)");
		}

		IDebuggerMethod FindAssemblyLoadFromStringThrow() {
			foreach (var method in FindAssemblyClassThrow().GetMethods("LoadFrom")) {
				var sig = method.MethodSig;
				if (sig.HasThis || sig.Params.Count != 1)
					continue;
				if (sig.Params[0].RemovePinnedAndModifiers().ElementType != ElementType.String)
					continue;

				return method;
			}
			throw new ScriptException("Could not find System.Reflection.Assembly.LoadFrom(string)");
		}

		IDebuggerMethod FindAssemblyLoadFileStringThrow() {
			foreach (var method in FindAssemblyClassThrow().GetMethods("LoadFile")) {
				var sig = method.MethodSig;
				if (sig.HasThis || sig.Params.Count != 1)
					continue;
				if (sig.Params[0].RemovePinnedAndModifiers().ElementType != ElementType.String)
					continue;

				return method;
			}
			throw new ScriptException("Could not find System.Reflection.Assembly.LoadFile(string)");
		}

		public IDebuggerValue AssemblyLoad(byte[] rawAssembly) {
			return debugger.Dispatcher.UI(() => Call(FindAssemblyLoadByteArrayThrow(), rawAssembly));
		}

		public IDebuggerValue AssemblyLoad(string assemblyString) {
			return debugger.Dispatcher.UI(() => Call(FindAssemblyLoadStringThrow(), assemblyString));
		}

		public IDebuggerValue AssemblyLoadFrom(string assemblyFile) {
			return debugger.Dispatcher.UI(() => Call(FindAssemblyLoadFromStringThrow(), assemblyFile));
		}

		public IDebuggerValue AssemblyLoadFile(string filename) {
			return debugger.Dispatcher.UI(() => Call(FindAssemblyLoadFileStringThrow(), filename));
		}

		public void Dispose() {
			debugger.Dispatcher.UI(() => {
				if (valuesToKeep != null) {
					foreach (var v in valuesToKeep) {
						if (v.IsHandle)
							v.DisposeHandle();
					}
					valuesToKeep = null;
				}
				eval.Dispose();
			});
		}
	}
}
