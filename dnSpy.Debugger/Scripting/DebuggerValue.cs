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
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Scripting.Debugger;
using dnSpy.Shared.Scripting;

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

		public ulong Address {
			get { return address; }
		}

		public uint ArrayCount {
			get { return debugger.Dispatcher.UI(() => value.ArrayCount); }
		}

		public CorElementType ArrayElementType {
			get { return debugger.Dispatcher.UI(() => (CorElementType)value.ArrayElementType); }
		}

		public uint[] BaseIndicies {
			get { return debugger.Dispatcher.UI(() => value.BaseIndicies); }
		}

		public IDebuggerValue BoxedValue {
			get {
				return debugger.Dispatcher.UI(() => {
					var res = value.BoxedValue;
					return res == null ? null : new DebuggerValue(debugger, res);
				});
			}
		}

		public IDebuggerClass Class {
			get {
				return debugger.Dispatcher.UI(() => {
					var cls = value.Class;
					return cls == null ? null : new DebuggerClass(debugger, cls);
				});
			}
		}

		public IDebuggerValue DereferencedValue {
			get {
				return debugger.Dispatcher.UI(() => {
					var res = value.NeuterCheckDereferencedValue;
					return res == null ? null : new DebuggerValue(debugger, res);
				});
			}
		}

		public uint[] Dimensions {
			get { return debugger.Dispatcher.UI(() => value.Dimensions); }
		}

		public CorElementType ElementType {
			get { return elementType; }
		}

		public CorElementType ElementTypeOrEnumUnderlyingType {
			get { return debugger.Dispatcher.UI(() => (CorElementType)value.TypeOrEnumUnderlyingType); }
		}

		public IEnumerable<ExceptionObjectStackFrame> ExceptionObjectStackFrames {
			get { return debugger.Dispatcher.UIIter(GetExceptionObjectStackFramesUI); }
		}

		IEnumerable<ExceptionObjectStackFrame> GetExceptionObjectStackFramesUI() {
			foreach (var o in value.ExceptionObjectStackFrames)
				yield return new ExceptionObjectStackFrame(debugger.FindModuleUI(o.Module), o.IP, o.Token, o.IsLastForeignExceptionFrame);
		}

		public DebugHandleType HandleType {
			get { return debugger.Dispatcher.UI(() => (DebugHandleType)value.HandleType); }
		}

		public bool HasBaseIndicies {
			get { return debugger.Dispatcher.UI(() => value.HasBaseIndicies); }
		}

		public bool IsArray {
			get { return (this.vflags & VFlags.Array) != 0; }
		}

		public bool IsBox {
			get { return (this.vflags & VFlags.Box) != 0; }
		}

		public bool IsComObject {
			get { return (this.vflags & VFlags.ComObject) != 0; }
		}

		public bool IsContext {
			get { return (this.vflags & VFlags.Context) != 0; }
		}

		public bool IsExceptionObject {
			get { return (this.vflags & VFlags.ExObject) != 0; }
		}

		public bool CanReadWrite {
			get { return (this.vflags & VFlags.Generic) != 0; }
		}

		public bool IsHandle {
			get { return (this.vflags & VFlags.Handle) != 0; }
		}

		public bool IsHeap {
			get { return (this.vflags & VFlags.Heap) != 0; }
		}

		public bool IsNeutered {
			get { return debugger.Dispatcher.UI(() => value.IsNeutered); }
		}

		public bool IsNull {
			get { return (this.vflags & VFlags.Null) != 0; }
		}

		public bool IsObject {
			get { return (this.vflags & VFlags.Object) != 0; }
		}

		public bool IsReference {
			get { return (this.vflags & VFlags.Reference) != 0; }
		}

		public bool IsString {
			get { return (this.vflags & VFlags.String) != 0; }
		}

		public bool IsValueClass {
			get { return (this.vflags & VFlags.ValueClass) != 0; }
		}

		public uint Rank {
			get { return debugger.Dispatcher.UI(() => value.Rank); }
		}

		public ulong ReferenceAddress {
			get { return debugger.Dispatcher.UI(() => value.ReferenceAddress); }
			set { debugger.Dispatcher.UI(() => this.value.ReferenceAddress = value); }
		}

		public ulong Size {
			get { return size; }
		}

		public string String {
			get { return debugger.Dispatcher.UI(() => value.String); }
		}

		public uint StringLength {
			get { return debugger.Dispatcher.UI(() => value.StringLength); }
		}

		public IDebuggerType Type {
			get {
				return debugger.Dispatcher.UI(() => {
					var type = value.ExactType;
					return type == null ? null : new DebuggerType(debugger, type);
				});
			}
		}

		public ValueResult Value {
			get {
				return debugger.Dispatcher.UI(() => {
					var res = value.Value;
					return res.IsValid ? new ValueResult(res.Value) : new ValueResult();
				});
			}
		}

		public CorValue CorValue {
			get { return value; }
		}
		readonly CorValue value;

		readonly Debugger debugger;
		readonly int hashCode;
		readonly ulong address;
		readonly ulong size;
		readonly VFlags vflags;
		readonly CorElementType elementType;

		public DebuggerValue(Debugger debugger, CorValue value) {
			debugger.Dispatcher.VerifyAccess();
			this.debugger = debugger;
			this.value = value;
			this.hashCode = value.GetHashCode();
			this.address = value.Address;
			this.size = value.Size;
			this.elementType = (CorElementType)value.Type;
			this.vflags = 0;
			if (value.IsGeneric)
				this.vflags |= VFlags.Generic;
			if (value.IsReference) {
				this.vflags |= VFlags.Reference;
				if (value.IsNull)
					this.vflags |= VFlags.Null;
			}
			if (value.IsHandle)
				this.vflags |= VFlags.Handle;
			if (value.IsArray)
				this.vflags |= VFlags.Array;
			if (value.IsBox)
				this.vflags |= VFlags.Box;
			if (value.IsString)
				this.vflags |= VFlags.String;
			if (value.IsObject) {
				this.vflags |= VFlags.Object;
				if (value.IsValueClass)
					this.vflags |= VFlags.ValueClass;
			}
			if (value.IsContext)
				this.vflags |= VFlags.Context;
			if (value.IsComObject)
				this.vflags |= VFlags.ComObject;
			if (value.IsExceptionObject)
				this.vflags |= VFlags.ExObject;
			if (value.IsHeap)
				this.vflags |= VFlags.Heap;
		}

		public IDebuggerValue CreateHandle(DebugHandleType type) {
			return debugger.Dispatcher.UI(() => {
				var corValue = value.CreateHandle((dndbg.COM.CorDebug.CorDebugHandleType)type);
				return corValue == null ? null : new DebuggerValue(debugger, corValue);
			});
		}

		public bool DisposeHandle() {
			return debugger.Dispatcher.UI(() => value.DisposeHandle());
		}

		public IDebuggerValue GetElement(int[] indices) {
			return debugger.Dispatcher.UI(() => {
				var corValue = value.GetElement(indices);
				return corValue == null ? null : new DebuggerValue(debugger, corValue);
			});
		}

		public IDebuggerValue GetElement(uint[] indices) {
			return debugger.Dispatcher.UI(() => {
				var corValue = value.GetElement(indices);
				return corValue == null ? null : new DebuggerValue(debugger, corValue);
			});
		}

		public IDebuggerValue GetElementAtPosition(int index) {
			return debugger.Dispatcher.UI(() => {
				var corValue = value.GetElementAtPosition(index);
				return corValue == null ? null : new DebuggerValue(debugger, corValue);
			});
		}

		public IDebuggerValue GetElementAtPosition(uint index) {
			return debugger.Dispatcher.UI(() => {
				var corValue = value.GetElementAtPosition(index);
				return corValue == null ? null : new DebuggerValue(debugger, corValue);
			});
		}

		public IDebuggerValue GetFieldValue(string name, bool checkBaseClasses = true) {
			return debugger.Dispatcher.UI(() => {
				var corValue = value.GetFieldValue(name, checkBaseClasses);
				return corValue == null ? null : new DebuggerValue(debugger, corValue);
			});
		}

		CorValue ReadField(IDebuggerClass cls, uint token) {
			var v = value;
			if (v.IsReference)
				v = v.DereferencedValue;
			if (v != null && v.IsBox)
				v = v.BoxedValue;
			if (v == null)
				return null;
			Debug.Assert(v.IsObject);
			return v.GetFieldValue(((DebuggerClass)cls).CorClass, token);
		}

		public IDebuggerValue GetFieldValue(IDebuggerClass cls, uint token) {
			return debugger.Dispatcher.UI(() => {
				var res = ReadField(cls, token);
				return res == null ? null : new DebuggerValue(debugger, res);
			});
		}

		public bool GetNullableValue(out IDebuggerValue value) {
			IDebuggerValue valueTmp = null;
			bool res = debugger.Dispatcher.UI(() => {
				CorValue corValue;
				bool res2 = this.value.GetNullableValue(out corValue);
				valueTmp = corValue == null ? null : new DebuggerValue(debugger, corValue);
				return res2;
			});
			value = valueTmp;
			return res;
		}

		public IDebuggerValue Read(IDebuggerField field) {
			return debugger.Dispatcher.UI(() => {
				var res = ReadField(field.Class, field.Token);
				return res == null ? null : new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Read(uint token) {
			return debugger.Dispatcher.UI(() => {
				var type = Type;
				var field = type == null ? null : Type.GetFields().FirstOrDefault(a => a.Token == token);
				if (field == null)
					return null;
				var res = ReadField(field.Class, field.Token);
				return res == null ? null : new DebuggerValue(debugger, res);
			});
		}

		public IDebuggerValue Read(string name, bool checkBaseClasses) {
			return debugger.Dispatcher.UI(() => {
				var type = Type;
				var field = type == null ? null : Type.GetField(name, checkBaseClasses);
				if (field == null)
					return null;
				var res = ReadField(field.Class, field.Token);
				return res == null ? null : new DebuggerValue(debugger, res);
			});
		}

		object[] CreateArguments(object[] args) {
			var res = new object[args.Length + 1];
			res[0] = new Box(this);
			for (int i = 0; i < args.Length; i++)
				res[i + 1] = args[i];
			return res;
		}

		public IDebuggerValue Call(IDebuggerThread thread, IDebuggerMethod method, params object[] args) {
			return debugger.Dispatcher.UI(() => thread.Call(method, CreateArguments(args)));
		}

		public IDebuggerValue Call(IDebuggerThread thread, object[] genericArgs, IDebuggerMethod method, params object[] args) {
			return debugger.Dispatcher.UI(() => thread.Call(genericArgs, method, CreateArguments(args)));
		}

		public IDebuggerValue Call(IDebuggerThread thread, string modName, string className, string methodName, params object[] args) {
			return debugger.Dispatcher.UI(() => thread.Call(modName, className, methodName, CreateArguments(args)));
		}

		public IDebuggerValue Call(IDebuggerThread thread, string modName, uint token, params object[] args) {
			return debugger.Dispatcher.UI(() => thread.Call(modName, token, CreateArguments(args)));
		}

		public IDebuggerValue Call(IDebuggerThread thread, object[] genericArgs, string modName, string className, string methodName, params object[] args) {
			return debugger.Dispatcher.UI(() => thread.Call(genericArgs, modName, className, methodName, CreateArguments(args)));
		}

		public IDebuggerValue Call(IDebuggerThread thread, object[] genericArgs, string modName, uint token, params object[] args) {
			return debugger.Dispatcher.UI(() => thread.Call(genericArgs, modName, token, CreateArguments(args)));
		}

		public byte[] Read() {
			return debugger.Dispatcher.UI(() => value.ReadGenericValue());
		}

		public bool Write(byte[] data) {
			return debugger.Dispatcher.UI(() => value.WriteGenericValue(data) == 0);
		}

		public bool ReadBoolean() {
			return debugger.Dispatcher.UI(() => {
				var d = value.ReadGenericValue();
				if (d == null || d.Length != 1)
					return false;
				return BitConverter.ToBoolean(d, 0);
			});
		}

		public char ReadChar() {
			return debugger.Dispatcher.UI(() => {
				var d = value.ReadGenericValue();
				if (d == null || d.Length != 2)
					return (char)0;
				return BitConverter.ToChar(d, 0);
			});
		}

		public sbyte ReadSByte() {
			return debugger.Dispatcher.UI(() => {
				var d = value.ReadGenericValue();
				if (d == null || d.Length != 1)
					return (sbyte)0;
				return (sbyte)d[0];
			});
		}

		public short ReadInt16() {
			return debugger.Dispatcher.UI(() => {
				var d = value.ReadGenericValue();
				if (d == null || d.Length != 2)
					return (short)0;
				return BitConverter.ToInt16(d, 0);
			});
		}

		public int ReadInt32() {
			return debugger.Dispatcher.UI(() => {
				var d = value.ReadGenericValue();
				if (d == null || d.Length != 4)
					return (int)0;
				return BitConverter.ToInt32(d, 0);
			});
		}

		public long ReadInt64() {
			return debugger.Dispatcher.UI(() => {
				var d = value.ReadGenericValue();
				if (d == null || d.Length != 8)
					return (long)0;
				return BitConverter.ToInt64(d, 0);
			});
		}

		public byte ReadByte() {
			return debugger.Dispatcher.UI(() => {
				var d = value.ReadGenericValue();
				if (d == null || d.Length != 1)
					return (byte)0;
				return d[0];
			});
		}

		public ushort ReadUInt16() {
			return debugger.Dispatcher.UI(() => {
				var d = value.ReadGenericValue();
				if (d == null || d.Length != 2)
					return (ushort)0;
				return BitConverter.ToUInt16(d, 0);
			});
		}

		public uint ReadUInt32() {
			return debugger.Dispatcher.UI(() => {
				var d = value.ReadGenericValue();
				if (d == null || d.Length != 4)
					return (uint)0;
				return BitConverter.ToUInt32(d, 0);
			});
		}

		public ulong ReadUInt64() {
			return debugger.Dispatcher.UI(() => {
				var d = value.ReadGenericValue();
				if (d == null || d.Length != 8)
					return (ulong)0;
				return BitConverter.ToUInt64(d, 0);
			});
		}

		public float ReadSingle() {
			return debugger.Dispatcher.UI(() => {
				var d = value.ReadGenericValue();
				if (d == null || d.Length != 4)
					return (float)0;
				return BitConverter.ToSingle(d, 0);
			});
		}

		public double ReadDouble() {
			return debugger.Dispatcher.UI(() => {
				var d = value.ReadGenericValue();
				if (d == null || d.Length != 8)
					return (double)0;
				return BitConverter.ToDouble(d, 0);
			});
		}

		public decimal ReadDecimal() {
			return debugger.Dispatcher.UI(() => Utils.ToDecimal(value.ReadGenericValue()));
		}

		public void Write(bool value) {
			debugger.Dispatcher.UI(() => this.value.WriteGenericValue(BitConverter.GetBytes(value)));
		}

		public void Write(char value) {
			debugger.Dispatcher.UI(() => this.value.WriteGenericValue(BitConverter.GetBytes(value)));
		}

		public void Write(sbyte value) {
			debugger.Dispatcher.UI(() => this.value.WriteGenericValue(new byte[] { (byte)value }));
		}

		public void Write(short value) {
			debugger.Dispatcher.UI(() => this.value.WriteGenericValue(BitConverter.GetBytes(value)));
		}

		public void Write(int value) {
			debugger.Dispatcher.UI(() => this.value.WriteGenericValue(BitConverter.GetBytes(value)));
		}

		public void Write(long value) {
			debugger.Dispatcher.UI(() => this.value.WriteGenericValue(BitConverter.GetBytes(value)));
		}

		public void Write(byte value) {
			debugger.Dispatcher.UI(() => this.value.WriteGenericValue(new byte[] { value }));
		}

		public void Write(ushort value) {
			debugger.Dispatcher.UI(() => this.value.WriteGenericValue(BitConverter.GetBytes(value)));
		}

		public void Write(uint value) {
			debugger.Dispatcher.UI(() => this.value.WriteGenericValue(BitConverter.GetBytes(value)));
		}

		public void Write(ulong value) {
			debugger.Dispatcher.UI(() => this.value.WriteGenericValue(BitConverter.GetBytes(value)));
		}

		public void Write(float value) {
			debugger.Dispatcher.UI(() => this.value.WriteGenericValue(BitConverter.GetBytes(value)));
		}

		public void Write(double value) {
			debugger.Dispatcher.UI(() => this.value.WriteGenericValue(BitConverter.GetBytes(value)));
		}

		public void Write(decimal value) {
			debugger.Dispatcher.UI(() => this.value.WriteGenericValue(Utils.GetBytes(value)));
		}

		CorValue GetDataValue() {
			var v = value;
			for (int i = 0; i < 2; i++) {
				if (!v.IsReference)
					break;
				if (v.IsNull)
					return null;
				if (v.Type == dndbg.COM.CorDebug.CorElementType.Ptr || v.Type == dndbg.COM.CorDebug.CorElementType.FnPtr)
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
					return new byte[0];
				if (v.IsString) {
					var s = v.String;
					data = s == null ? null : Encoding.Unicode.GetBytes(s);
				}
				else if (v.IsArray) {
					if (v.ArrayCount == 0)
						data = new byte[0];
					else {
						var elemValue = v.GetElementAtPosition(0);
						ulong elemSize = elemValue == null ? 0 : elemValue.Size;
						ulong elemAddr = elemValue == null ? 0 : elemValue.Address;
						ulong addr = v.Address;
						ulong totalSize = elemSize * v.ArrayCount;
						if (elemAddr == 0 || elemAddr < addr || elemAddr - addr > int.MaxValue || totalSize > int.MaxValue)
							return new byte[0];
						data = v.ReadGenericValue();
						dataIndex = (int)(elemAddr - addr);
						dataSize = (int)totalSize;
					}
				}
				else
					data = v.ReadGenericValue();
				if (data == null)
					return new byte[0];

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
				ulong elemSize = elemValue == null ? 0 : elemValue.Size;
				ulong elemAddr = elemValue == null ? 0 : elemValue.Address;
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

		public override bool Equals(object obj) {
			var other = obj as DebuggerValue;
			return other != null && other.value == value;
		}

		public override int GetHashCode() {
			return hashCode;
		}

		public void Write(ISyntaxHighlightOutput output, ValueResult valueResult, TypeFormatFlags flags) {
			debugger.Dispatcher.UI(() => valueResult.ToCorValueResult().Write(new OutputConverter(output), this.value, (TypePrinterFlags)flags));
		}

		public string ToString(ValueResult valueResult, TypeFormatFlags flags) {
			return debugger.Dispatcher.UI(() => valueResult.ToCorValueResult().ToString(this.value, (TypePrinterFlags)flags));
		}

		public void Write(ISyntaxHighlightOutput output, TypeFormatFlags flags) {
			debugger.Dispatcher.UI(() => value.Write(new OutputConverter(output), (TypePrinterFlags)flags));
		}

		public void WriteType(ISyntaxHighlightOutput output, IDebuggerClass cls, TypeFormatFlags flags) {
			debugger.Dispatcher.UI(() => value.WriteType(new OutputConverter(output), ((DebuggerClass)cls).CorClass, (TypePrinterFlags)flags));
		}

		public void WriteType(ISyntaxHighlightOutput output, IDebuggerType type, TypeFormatFlags flags) {
			debugger.Dispatcher.UI(() => value.WriteType(new OutputConverter(output), ((DebuggerType)type).CorType, (TypePrinterFlags)flags));
		}

		public string ToString(TypeFormatFlags flags) {
			return debugger.Dispatcher.UI(() => value.ToString((TypePrinterFlags)flags));
		}

		public override string ToString() {
			return debugger.Dispatcher.UI(() => value.ToString());
		}
	}
}
