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
using System.IO;
using System.Linq;
using System.Text;
using dndbg.Engine;
using dnSpy.Contracts.Scripting;
using dnSpy.Contracts.Scripting.Debugger;

namespace dnSpy.Debugger.Scripting {
	sealed class DebuggerValue : IDebuggerValue {
		[Flags]
		enum VFlags : ushort {
			Generic			= 0x0001,
			Reference		= 0x0002,
			Handle			= 0x0004,
			Array			= 0x0008,
			Box				= 0x0010,
			String			= 0x0020,
			Object			= 0x0040,
			Context			= 0x0080,
			ComObject		= 0x0100,
			ExObject		= 0x0200,
			Heap			= 0x0400,
			Null			= 0x0800,
			ValueClass		= 0x1000,
		}

		public ulong Address => address;
		public uint ArrayCount => debugger.Dispatcher.UI(() => this.CorValue.ArrayCount);
		public CorElementType ArrayElementType => debugger.Dispatcher.UI(() => (CorElementType)this.CorValue.ArrayElementType);
		public uint[] BaseIndicies => debugger.Dispatcher.UI(() => this.CorValue.BaseIndicies);

		public IDebuggerValue BoxedValue => debugger.Dispatcher.UI(() => {
			var res = this.CorValue.BoxedValue;
			return res == null ? null : new DebuggerValue(debugger, res);
		});

		public IDebuggerClass Class => debugger.Dispatcher.UI(() => {
			var cls = this.CorValue.Class;
			return cls == null ? null : new DebuggerClass(debugger, cls);
		});

		public IDebuggerValue DereferencedValue => debugger.Dispatcher.UI(() => {
			var res = this.CorValue.NeuterCheckDereferencedValue;
			return res == null ? null : new DebuggerValue(debugger, res);
		});

		public uint[] Dimensions => debugger.Dispatcher.UI(() => this.CorValue.Dimensions);
		public CorElementType ElementType => elementType;
		public CorElementType ElementTypeOrEnumUnderlyingType => debugger.Dispatcher.UI(() => (CorElementType)this.CorValue.TypeOrEnumUnderlyingType);
		public IEnumerable<ExceptionObjectStackFrame> ExceptionObjectStackFrames => debugger.Dispatcher.UIIter(GetExceptionObjectStackFramesUI);

		IEnumerable<ExceptionObjectStackFrame> GetExceptionObjectStackFramesUI() {
			foreach (var o in CorValue.ExceptionObjectStackFrames)
				yield return new ExceptionObjectStackFrame(debugger.FindModuleUI(o.Module), o.IP, o.Token, o.IsLastForeignExceptionFrame);
		}

		public DebugHandleType HandleType => debugger.Dispatcher.UI(() => (DebugHandleType)this.CorValue.HandleType);
		public bool HasBaseIndicies => debugger.Dispatcher.UI(() => this.CorValue.HasBaseIndicies);
		public bool IsArray => (vflags & VFlags.Array) != 0;
		public bool IsBox => (vflags & VFlags.Box) != 0;
		public bool IsComObject => (vflags & VFlags.ComObject) != 0;
		public bool IsContext => (vflags & VFlags.Context) != 0;
		public bool IsExceptionObject => (vflags & VFlags.ExObject) != 0;
		public bool CanReadWrite => (vflags & VFlags.Generic) != 0;
		public bool IsHandle => (vflags & VFlags.Handle) != 0;
		public bool IsHeap => (vflags & VFlags.Heap) != 0;
		public bool IsNeutered => debugger.Dispatcher.UI(() => this.CorValue.IsNeutered);
		public bool IsNull => (vflags & VFlags.Null) != 0;
		public bool IsObject => (vflags & VFlags.Object) != 0;
		public bool IsReference => (vflags & VFlags.Reference) != 0;
		public bool IsString => (vflags & VFlags.String) != 0;
		public bool IsValueClass => (vflags & VFlags.ValueClass) != 0;
		public uint Rank => debugger.Dispatcher.UI(() => this.CorValue.Rank);

		public ulong ReferenceAddress {
			get { return debugger.Dispatcher.UI(() => CorValue.ReferenceAddress); }
			set { debugger.Dispatcher.UI(() => CorValue.ReferenceAddress = value); }
		}

		public ulong Size => size;
		public string String => debugger.Dispatcher.UI(() => this.CorValue.String);
		public uint StringLength => debugger.Dispatcher.UI(() => this.CorValue.StringLength);

		public IDebuggerType Type => debugger.Dispatcher.UI(() => {
			var type = this.CorValue.ExactType;
			return type == null ? null : new DebuggerType(debugger, type);
		});

		public ValueResult Value => debugger.Dispatcher.UI(() => {
			var res = this.CorValue.Value;
			return res.IsValid ? new ValueResult(res.Value) : new ValueResult();
		});

		public CorValue CorValue { get; }

		readonly Debugger debugger;
		readonly int hashCode;
		readonly ulong address;
		readonly ulong size;
		readonly VFlags vflags;
		readonly CorElementType elementType;

		public DebuggerValue(Debugger debugger, CorValue value) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			CorValue = value;
			hashCode = value.GetHashCode();
			address = value.Address;
			size = value.Size;
			elementType = (CorElementType)value.ElementType;
			vflags = 0;
			if (value.IsGeneric)
				vflags |= VFlags.Generic;
			if (value.IsReference) {
				vflags |= VFlags.Reference;
				if (value.IsNull)
					vflags |= VFlags.Null;
			}
			if (value.IsHandle)
				vflags |= VFlags.Handle;
			if (value.IsArray)
				vflags |= VFlags.Array;
			if (value.IsBox)
				vflags |= VFlags.Box;
			if (value.IsString)
				vflags |= VFlags.String;
			if (value.IsObject) {
				vflags |= VFlags.Object;
				if (value.IsValueClass)
					vflags |= VFlags.ValueClass;
			}
			if (value.IsContext)
				vflags |= VFlags.Context;
			if (value.IsComObject)
				vflags |= VFlags.ComObject;
			if (value.IsExceptionObject)
				vflags |= VFlags.ExObject;
			if (value.IsHeap)
				vflags |= VFlags.Heap;
		}

		public IDebuggerValue CreateHandle(DebugHandleType type) => debugger.Dispatcher.UI(() => {
			var corValue = this.CorValue.CreateHandle((dndbg.COM.CorDebug.CorDebugHandleType)type);
			return corValue == null ? null : new DebuggerValue(debugger, corValue);
		});

		public bool DisposeHandle() => debugger.Dispatcher.UI(() => this.CorValue.DisposeHandle());

		public IDebuggerValue GetElement(int[] indices) => debugger.Dispatcher.UI(() => {
			var corValue = this.CorValue.GetElement(indices);
			return corValue == null ? null : new DebuggerValue(debugger, corValue);
		});

		public IDebuggerValue GetElement(uint[] indices) => debugger.Dispatcher.UI(() => {
			var corValue = this.CorValue.GetElement(indices);
			return corValue == null ? null : new DebuggerValue(debugger, corValue);
		});

		public IDebuggerValue GetElementAtPosition(int index) => debugger.Dispatcher.UI(() => {
			var corValue = this.CorValue.GetElementAtPosition(index);
			return corValue == null ? null : new DebuggerValue(debugger, corValue);
		});

		public IDebuggerValue GetElementAtPosition(uint index) => debugger.Dispatcher.UI(() => {
			var corValue = this.CorValue.GetElementAtPosition(index);
			return corValue == null ? null : new DebuggerValue(debugger, corValue);
		});

		public IDebuggerValue GetFieldValue(string name, bool checkBaseClasses = true) => debugger.Dispatcher.UI(() => {
			var corValue = this.CorValue.GetFieldValue(name, checkBaseClasses);
			return corValue == null ? null : new DebuggerValue(debugger, corValue);
		});

		CorValue ReadField(IDebuggerClass cls, uint token) {
			var v = CorValue;
			if (v.IsReference)
				v = v.DereferencedValue;
			if (v != null && v.IsBox)
				v = v.BoxedValue;
			if (v == null)
				return null;
			Debug.Assert(v.IsObject);
			return v.GetFieldValue(((DebuggerClass)cls).CorClass, token);
		}

		public IDebuggerValue GetFieldValue(IDebuggerClass cls, uint token) => debugger.Dispatcher.UI(() => {
			var res = ReadField(cls, token);
			return res == null ? null : new DebuggerValue(debugger, res);
		});

		public bool GetNullableValue(out IDebuggerValue value) {
			IDebuggerValue valueTmp = null;
			bool res = debugger.Dispatcher.UI(() => {
				CorValue corValue;
				bool res2 = CorValue.GetNullableValue(out corValue);
				valueTmp = corValue == null ? null : new DebuggerValue(debugger, corValue);
				return res2;
			});
			value = valueTmp;
			return res;
		}

		public IDebuggerValue Read(IDebuggerField field) => debugger.Dispatcher.UI(() => {
			var res = ReadField(field.Class, field.Token);
			return res == null ? null : new DebuggerValue(debugger, res);
		});

		public IDebuggerValue Read(uint token) => debugger.Dispatcher.UI(() => {
			var field = Type?.GetFields()?.FirstOrDefault(a => a.Token == token);
			if (field == null)
				return null;
			var res = ReadField(field.Class, field.Token);
			return res == null ? null : new DebuggerValue(debugger, res);
		});

		public IDebuggerValue Read(string name, bool checkBaseClasses) => debugger.Dispatcher.UI(() => {
			var field = Type?.GetField(name, checkBaseClasses);
			if (field == null)
				return null;
			var res = ReadField(field.Class, field.Token);
			return res == null ? null : new DebuggerValue(debugger, res);
		});

		object[] CreateArguments(object[] args) {
			var res = new object[args.Length + 1];
			res[0] = new Box(this);
			for (int i = 0; i < args.Length; i++)
				res[i + 1] = args[i];
			return res;
		}

		public IDebuggerValue Call(IDebuggerThread thread, IDebuggerMethod method, params object[] args) =>
			debugger.Dispatcher.UI(() => thread.Call(method, CreateArguments(args)));
		public IDebuggerValue Call(IDebuggerThread thread, object[] genericArgs, IDebuggerMethod method, params object[] args) =>
			debugger.Dispatcher.UI(() => thread.Call(genericArgs, method, CreateArguments(args)));
		public IDebuggerValue Call(IDebuggerThread thread, string modName, string className, string methodName, params object[] args) =>
			debugger.Dispatcher.UI(() => thread.Call(modName, className, methodName, CreateArguments(args)));
		public IDebuggerValue Call(IDebuggerThread thread, string modName, uint token, params object[] args) =>
			debugger.Dispatcher.UI(() => thread.Call(modName, token, CreateArguments(args)));
		public IDebuggerValue Call(IDebuggerThread thread, object[] genericArgs, string modName, string className, string methodName, params object[] args) =>
			debugger.Dispatcher.UI(() => thread.Call(genericArgs, modName, className, methodName, CreateArguments(args)));
		public IDebuggerValue Call(IDebuggerThread thread, object[] genericArgs, string modName, uint token, params object[] args) =>
			debugger.Dispatcher.UI(() => thread.Call(genericArgs, modName, token, CreateArguments(args)));
		public byte[] Read() => debugger.Dispatcher.UI(() => this.CorValue.ReadGenericValue());
		public bool Write(byte[] data) => debugger.Dispatcher.UI(() => this.CorValue.WriteGenericValue(data) == 0);

		public bool ReadBoolean() => debugger.Dispatcher.UI(() => {
			var d = this.CorValue.ReadGenericValue();
			if (d == null || d.Length != 1)
				return false;
			return BitConverter.ToBoolean(d, 0);
		});

		public char ReadChar() => debugger.Dispatcher.UI(() => {
			var d = this.CorValue.ReadGenericValue();
			if (d == null || d.Length != 2)
				return (char)0;
			return BitConverter.ToChar(d, 0);
		});

		public sbyte ReadSByte() => debugger.Dispatcher.UI(() => {
			var d = this.CorValue.ReadGenericValue();
			if (d == null || d.Length != 1)
				return (sbyte)0;
			return (sbyte)d[0];
		});

		public short ReadInt16() => debugger.Dispatcher.UI(() => {
			var d = this.CorValue.ReadGenericValue();
			if (d == null || d.Length != 2)
				return (short)0;
			return BitConverter.ToInt16(d, 0);
		});

		public int ReadInt32() => debugger.Dispatcher.UI(() => {
			var d = this.CorValue.ReadGenericValue();
			if (d == null || d.Length != 4)
				return 0;
			return BitConverter.ToInt32(d, 0);
		});

		public long ReadInt64() => debugger.Dispatcher.UI(() => {
			var d = this.CorValue.ReadGenericValue();
			if (d == null || d.Length != 8)
				return 0;
			return BitConverter.ToInt64(d, 0);
		});

		public byte ReadByte() => debugger.Dispatcher.UI(() => {
			var d = this.CorValue.ReadGenericValue();
			if (d == null || d.Length != 1)
				return (byte)0;
			return d[0];
		});

		public ushort ReadUInt16() => debugger.Dispatcher.UI(() => {
			var d = this.CorValue.ReadGenericValue();
			if (d == null || d.Length != 2)
				return (ushort)0;
			return BitConverter.ToUInt16(d, 0);
		});

		public uint ReadUInt32() => debugger.Dispatcher.UI(() => {
			var d = this.CorValue.ReadGenericValue();
			if (d == null || d.Length != 4)
				return (uint)0;
			return BitConverter.ToUInt32(d, 0);
		});

		public ulong ReadUInt64() => debugger.Dispatcher.UI(() => {
			var d = this.CorValue.ReadGenericValue();
			if (d == null || d.Length != 8)
				return (ulong)0;
			return BitConverter.ToUInt64(d, 0);
		});

		public float ReadSingle() => debugger.Dispatcher.UI(() => {
			var d = this.CorValue.ReadGenericValue();
			if (d == null || d.Length != 4)
				return 0;
			return BitConverter.ToSingle(d, 0);
		});

		public double ReadDouble() => debugger.Dispatcher.UI(() => {
			var d = this.CorValue.ReadGenericValue();
			if (d == null || d.Length != 8)
				return 0;
			return BitConverter.ToDouble(d, 0);
		});

		public decimal ReadDecimal() => debugger.Dispatcher.UI(() => Utils.ToDecimal(this.CorValue.ReadGenericValue()));
		public void Write(bool value) => debugger.Dispatcher.UI(() => this.CorValue.WriteGenericValue(BitConverter.GetBytes(value)));
		public void Write(char value) => debugger.Dispatcher.UI(() => this.CorValue.WriteGenericValue(BitConverter.GetBytes(value)));
		public void Write(sbyte value) => debugger.Dispatcher.UI(() => this.CorValue.WriteGenericValue(new byte[] { (byte)value }));
		public void Write(short value) => debugger.Dispatcher.UI(() => this.CorValue.WriteGenericValue(BitConverter.GetBytes(value)));
		public void Write(int value) => debugger.Dispatcher.UI(() => this.CorValue.WriteGenericValue(BitConverter.GetBytes(value)));
		public void Write(long value) => debugger.Dispatcher.UI(() => this.CorValue.WriteGenericValue(BitConverter.GetBytes(value)));
		public void Write(byte value) => debugger.Dispatcher.UI(() => this.CorValue.WriteGenericValue(new byte[] { value }));
		public void Write(ushort value) => debugger.Dispatcher.UI(() => this.CorValue.WriteGenericValue(BitConverter.GetBytes(value)));
		public void Write(uint value) => debugger.Dispatcher.UI(() => this.CorValue.WriteGenericValue(BitConverter.GetBytes(value)));
		public void Write(ulong value) => debugger.Dispatcher.UI(() => this.CorValue.WriteGenericValue(BitConverter.GetBytes(value)));
		public void Write(float value) => debugger.Dispatcher.UI(() => this.CorValue.WriteGenericValue(BitConverter.GetBytes(value)));
		public void Write(double value) => debugger.Dispatcher.UI(() => this.CorValue.WriteGenericValue(BitConverter.GetBytes(value)));
		public void Write(decimal value) => debugger.Dispatcher.UI(() => this.CorValue.WriteGenericValue(Utils.GetBytes(value)));

		CorValue GetDataValue() {
			var v = CorValue;
			for (int i = 0; i < 2; i++) {
				if (!v.IsReference)
					break;
				if (v.IsNull)
					return null;
				if (v.ElementType == dndbg.COM.CorDebug.CorElementType.Ptr || v.ElementType == dndbg.COM.CorDebug.CorElementType.FnPtr)
					return null;
				v = v.NeuterCheckDereferencedValue;
				if (v == null)
					return null;
			}
			if (v.IsReference)
				return null;
			if (v.IsBox) {
				v = v.BoxedValue;
				if (v == null)
					return null;
			}
			return v;
		}

		public byte[] SaveData() {
			return debugger.Dispatcher.UI(() => {
				byte[] data;
				int? dataIndex = null, dataSize = null;
				var v = GetDataValue();
				if (v == null)
					return Array.Empty<byte>();
				if (v.IsString) {
					var s = v.String;
					data = s == null ? null : Encoding.Unicode.GetBytes(s);
				}
				else if (v.IsArray) {
					if (v.ArrayCount == 0)
						data = Array.Empty<byte>();
					else {
						var elemValue = v.GetElementAtPosition(0);
						ulong elemSize = elemValue?.Size ?? 0;
						ulong elemAddr = elemValue?.Address ?? 0;
						ulong addr = v.Address;
						ulong totalSize = elemSize * v.ArrayCount;
						if (elemAddr == 0 || elemAddr < addr || elemAddr - addr > int.MaxValue || totalSize > int.MaxValue)
							return Array.Empty<byte>();
						data = v.ReadGenericValue();
						dataIndex = (int)(elemAddr - addr);
						dataSize = (int)totalSize;
					}
				}
				else
					data = v.ReadGenericValue();
				if (data == null)
					return Array.Empty<byte>();

				if (dataIndex == null)
					dataIndex = 0;
				if (dataSize == null)
					dataSize = data.Length - dataIndex.Value;
				var data2 = new byte[dataSize.Value];
				Array.Copy(data, dataIndex.Value, data2, 0, data2.Length);
				return data2;
			});
		}

		public void SaveData(Stream stream) {
			var bytes = SaveData();
			stream.Write(bytes, 0, bytes.Length);
		}

		public void SaveData(string filename) {
			using (var stream = File.Create(filename))
				SaveData(stream);
		}

		public ulong GetArrayDataAddress() {
			ulong elemSize;
			return GetArrayDataAddress(out elemSize);
		}

		public ulong GetArrayDataAddress(out ulong elemSize2) {
			ulong elemSizeTmp = 0;
			var res = debugger.Dispatcher.UI(() => {
				var v = GetDataValue();
				if (v == null)
					return 0UL;
				if (!v.IsArray)
					return 0UL;
				if (v.ArrayCount == 0)
					return 0UL;

				var elemValue = v.GetElementAtPosition(0);
				ulong elemSize = elemValue?.Size ?? 0;
				ulong elemAddr = elemValue?.Address ?? 0;
				ulong addr = v.Address;
				ulong totalSize = elemSize * v.ArrayCount;
				if (elemAddr == 0 || elemAddr < addr || elemAddr - addr > int.MaxValue || totalSize > int.MaxValue)
					return 0UL;

				ulong dataIndex = elemAddr - addr;
				elemSizeTmp = elemSize;
				return v.Address + dataIndex;
			});
			elemSize2 = elemSizeTmp;
			return res;
		}

		public override bool Equals(object obj) => (obj as DebuggerValue)?.CorValue == CorValue;
		public override int GetHashCode() => hashCode;
		const TypePrinterFlags DEFAULT_FLAGS = TypePrinterFlags.Default;
		public void WriteTo(IOutputWriter output) => debugger.Dispatcher.UI(() => Write(output, Value, (TypeFormatFlags)DEFAULT_FLAGS));
		public void Write(IOutputWriter output, ValueResult valueResult, TypeFormatFlags flags) =>
			debugger.Dispatcher.UI(() => valueResult.ToCorValueResult().Write(new OutputWriterConverter(output), this.CorValue, (TypePrinterFlags)flags));
		public string ToString(ValueResult valueResult, TypeFormatFlags flags) => debugger.Dispatcher.UI(() => valueResult.ToCorValueResult().ToString(this.CorValue, (TypePrinterFlags)flags));
		public void Write(IOutputWriter output, TypeFormatFlags flags) =>
			debugger.Dispatcher.UI(() => CorValue.Write(new OutputWriterConverter(output), (TypePrinterFlags)flags));
		public void WriteType(IOutputWriter output, IDebuggerClass cls, TypeFormatFlags flags) =>
			debugger.Dispatcher.UI(() => CorValue.WriteType(new OutputWriterConverter(output), ((DebuggerClass)cls).CorClass, (TypePrinterFlags)flags));
		public void WriteType(IOutputWriter output, IDebuggerType type, TypeFormatFlags flags) =>
			debugger.Dispatcher.UI(() => CorValue.WriteType(new OutputWriterConverter(output), ((DebuggerType)type).CorType, (TypePrinterFlags)flags));
		public string ToString(TypeFormatFlags flags) => debugger.Dispatcher.UI(() => this.CorValue.ToString((TypePrinterFlags)flags));
		public override string ToString() => debugger.Dispatcher.UI(() => this.CorValue.ToString(DEFAULT_FLAGS));
	}
}
