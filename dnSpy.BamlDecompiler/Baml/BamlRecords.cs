/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.IO;

namespace dnSpy.BamlDecompiler.Baml {
	internal enum BamlRecordType : byte {
		ClrEvent = 0x13,
		Comment = 0x17,
		AssemblyInfo = 0x1c,
		AttributeInfo = 0x1f,
		ConstructorParametersStart = 0x2a,
		ConstructorParametersEnd = 0x2b,
		ConstructorParameterType = 0x2c,
		ConnectionId = 0x2d,
		ContentProperty = 0x2e,
		DefAttribute = 0x19,
		DefAttributeKeyString = 0x26,
		DefAttributeKeyType = 0x27,
		DeferableContentStart = 0x25,
		DefTag = 0x18,
		DocumentEnd = 0x2,
		DocumentStart = 0x1,
		ElementEnd = 0x4,
		ElementStart = 0x3,
		EndAttributes = 0x1a,
		KeyElementEnd = 0x29,
		KeyElementStart = 0x28,
		LastRecordType = 0x39,
		LineNumberAndPosition = 0x35,
		LinePosition = 0x36,
		LiteralContent = 0xf,
		NamedElementStart = 0x2f,
		OptimizedStaticResource = 0x37,
		PIMapping = 0x1b,
		PresentationOptionsAttribute = 0x34,
		ProcessingInstruction = 0x16,
		Property = 0x5,
		PropertyArrayEnd = 0xa,
		PropertyArrayStart = 0x9,
		PropertyComplexEnd = 0x8,
		PropertyComplexStart = 0x7,
		PropertyCustom = 0x6,
		PropertyDictionaryEnd = 0xe,
		PropertyDictionaryStart = 0xd,
		PropertyListEnd = 0xc,
		PropertyListStart = 0xb,
		PropertyStringReference = 0x21,
		PropertyTypeReference = 0x22,
		PropertyWithConverter = 0x24,
		PropertyWithExtension = 0x23,
		PropertyWithStaticResourceId = 0x38,
		RoutedEvent = 0x12,
		StaticResourceEnd = 0x31,
		StaticResourceId = 0x32,
		StaticResourceStart = 0x30,
		StringInfo = 0x20,
		Text = 0x10,
		TextWithConverter = 0x11,
		TextWithId = 0x33,
		TypeInfo = 0x1d,
		TypeSerializerInfo = 0x1e,
		XmlAttribute = 0x15,
		XmlnsProperty = 0x14
	}

	internal abstract class BamlRecord {
		public abstract BamlRecordType Type { get; }
		public long Position { get; internal set; }
		public abstract void Read(BamlBinaryReader reader);
		public abstract void Write(BamlBinaryWriter writer);
	}

	internal abstract class SizedBamlRecord : BamlRecord {
		public override void Read(BamlBinaryReader reader) {
			long pos = reader.BaseStream.Position;
			int size = reader.ReadEncodedInt();

			ReadData(reader, size - (int)(reader.BaseStream.Position - pos));
			Debug.Assert(reader.BaseStream.Position - pos == size);
		}

		int SizeofEncodedInt(int val) {
			if ((val & ~0x7F) == 0) {
				return 1;
			}
			if ((val & ~0x3FFF) == 0) {
				return 2;
			}
			if ((val & ~0x1FFFFF) == 0) {
				return 3;
			}
			if ((val & ~0xFFFFFFF) == 0) {
				return 4;
			}
			return 5;
		}

		public override void Write(BamlBinaryWriter writer) {
			long pos = writer.BaseStream.Position;
			WriteData(writer);
			var size = (int)(writer.BaseStream.Position - pos);
			size = SizeofEncodedInt(SizeofEncodedInt(size) + size) + size;
			writer.BaseStream.Position = pos;
			writer.WriteEncodedInt(size);
			WriteData(writer);
		}

		protected abstract void ReadData(BamlBinaryReader reader, int size);
		protected abstract void WriteData(BamlBinaryWriter writer);
	}

	internal interface IBamlDeferRecord {
		BamlRecord Record { get; set; }
		void ReadDefer(BamlDocument doc, int index, Func<long, BamlRecord> resolve);
		void WriteDefer(BamlDocument doc, int index, BinaryWriter wtr);
	}

	internal class XmlnsPropertyRecord : SizedBamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.XmlnsProperty; }
		}

		public string Prefix { get; set; }
		public string XmlNamespace { get; set; }
		public ushort[] AssemblyIds { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			Prefix = reader.ReadString();
			XmlNamespace = reader.ReadString();
			AssemblyIds = new ushort[reader.ReadUInt16()];
			for (int i = 0; i < AssemblyIds.Length; i++)
				AssemblyIds[i] = reader.ReadUInt16();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(Prefix);
			writer.Write(XmlNamespace);
			writer.Write((ushort)AssemblyIds.Length);
			foreach (ushort i in AssemblyIds)
				writer.Write(i);
		}
	}

	internal class PresentationOptionsAttributeRecord : SizedBamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.PresentationOptionsAttribute; }
		}

		public string Value { get; set; }
		public ushort NameId { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			Value = reader.ReadString();
			NameId = reader.ReadUInt16();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(Value);
			writer.Write(NameId);
		}
	}

	internal class PIMappingRecord : SizedBamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.PIMapping; }
		}

		public string XmlNamespace { get; set; }
		public string ClrNamespace { get; set; }
		public ushort AssemblyId { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			XmlNamespace = reader.ReadString();
			ClrNamespace = reader.ReadString();
			AssemblyId = reader.ReadUInt16();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(XmlNamespace);
			writer.Write(ClrNamespace);
			writer.Write(AssemblyId);
		}
	}

	internal class AssemblyInfoRecord : SizedBamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.AssemblyInfo; }
		}

		public ushort AssemblyId { get; set; }
		public string AssemblyFullName { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			AssemblyId = reader.ReadUInt16();
			AssemblyFullName = reader.ReadString();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(AssemblyId);
			writer.Write(AssemblyFullName);
		}
	}

	internal class PropertyRecord : SizedBamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.Property; }
		}

		public ushort AttributeId { get; set; }
		public string Value { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			AttributeId = reader.ReadUInt16();
			Value = reader.ReadString();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(AttributeId);
			writer.Write(Value);
		}
	}

	internal class PropertyWithConverterRecord : PropertyRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.PropertyWithConverter; }
		}

		public ushort ConverterTypeId { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			base.ReadData(reader, size);
			ConverterTypeId = reader.ReadUInt16();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			base.WriteData(writer);
			writer.Write(ConverterTypeId);
		}
	}

	internal class PropertyCustomRecord : SizedBamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.PropertyCustom; }
		}

		public ushort AttributeId { get; set; }
		public ushort SerializerTypeId { get; set; }
		public byte[] Data { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			long pos = reader.BaseStream.Position;
			AttributeId = reader.ReadUInt16();
			SerializerTypeId = reader.ReadUInt16();
			Data = reader.ReadBytes(size - (int)(reader.BaseStream.Position - pos));
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(AttributeId);
			writer.Write(SerializerTypeId);
			writer.Write(Data);
		}
	}

	internal class DefAttributeRecord : SizedBamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.DefAttribute; }
		}

		public string Value { get; set; }
		public ushort NameId { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			Value = reader.ReadString();
			NameId = reader.ReadUInt16();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(Value);
			writer.Write(NameId);
		}
	}

	internal class DefAttributeKeyStringRecord : SizedBamlRecord, IBamlDeferRecord {
		internal uint pos = 0xffffffff;

		public override BamlRecordType Type {
			get { return BamlRecordType.DefAttributeKeyString; }
		}

		public ushort ValueId { get; set; }
		public bool Shared { get; set; }
		public bool SharedSet { get; set; }

		public BamlRecord Record { get; set; }

		public void ReadDefer(BamlDocument doc, int index, Func<long, BamlRecord> resolve) {
			bool keys = true;
			do {
				switch (doc[index].Type) {
					case BamlRecordType.DefAttributeKeyString:
					case BamlRecordType.DefAttributeKeyType:
					case BamlRecordType.OptimizedStaticResource:
						keys = true;
						break;
					case BamlRecordType.StaticResourceStart:
						NavigateTree(doc, BamlRecordType.StaticResourceStart, BamlRecordType.StaticResourceEnd, ref index);
						keys = true;
						break;
					case BamlRecordType.KeyElementStart:
						NavigateTree(doc, BamlRecordType.KeyElementStart, BamlRecordType.KeyElementEnd, ref index);
						keys = true;
						break;
					default:
						keys = false;
						index--;
						break;
				}
				index++;
			} while (keys);
			Record = resolve(doc[index].Position + pos);
		}

		public void WriteDefer(BamlDocument doc, int index, BinaryWriter wtr) {
			bool keys = true;
			do {
				switch (doc[index].Type) {
					case BamlRecordType.DefAttributeKeyString:
					case BamlRecordType.DefAttributeKeyType:
					case BamlRecordType.OptimizedStaticResource:
						keys = true;
						break;
					case BamlRecordType.StaticResourceStart:
						NavigateTree(doc, BamlRecordType.StaticResourceStart, BamlRecordType.StaticResourceEnd, ref index);
						keys = true;
						break;
					case BamlRecordType.KeyElementStart:
						NavigateTree(doc, BamlRecordType.KeyElementStart, BamlRecordType.KeyElementEnd, ref index);
						keys = true;
						break;
					default:
						keys = false;
						index--;
						break;
				}
				index++;
			} while (keys);
			wtr.BaseStream.Seek(pos, SeekOrigin.Begin);
			wtr.Write((uint)(Record.Position - doc[index].Position));
		}

		protected override void ReadData(BamlBinaryReader reader, int size) {
			ValueId = reader.ReadUInt16();
			pos = reader.ReadUInt32();
			Shared = reader.ReadBoolean();
			SharedSet = reader.ReadBoolean();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(ValueId);
			pos = (uint)writer.BaseStream.Position;
			writer.Write((uint)0);
			writer.Write(Shared);
			writer.Write(SharedSet);
		}

		static void NavigateTree(BamlDocument doc, BamlRecordType start, BamlRecordType end, ref int index) {
			index++;
			while (true) //Assume there alway is a end
			{
				if (doc[index].Type == start)
					NavigateTree(doc, start, end, ref index);
				else if (doc[index].Type == end)
					return;
				index++;
			}
		}
	}

	internal class TypeInfoRecord : SizedBamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.TypeInfo; }
		}

		public ushort TypeId { get; set; }
		public ushort AssemblyId { get; set; }
		public string TypeFullName { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			TypeId = reader.ReadUInt16();
			AssemblyId = reader.ReadUInt16();
			TypeFullName = reader.ReadString();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(TypeId);
			writer.Write(AssemblyId);
			writer.Write(TypeFullName);
		}
	}

	internal class TypeSerializerInfoRecord : TypeInfoRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.TypeSerializerInfo; }
		}

		public ushort SerializerTypeId { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			base.ReadData(reader, size);
			SerializerTypeId = reader.ReadUInt16();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			base.WriteData(writer);
			writer.Write(SerializerTypeId);
		}
	}

	internal class AttributeInfoRecord : SizedBamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.AttributeInfo; }
		}

		public ushort AttributeId { get; set; }
		public ushort OwnerTypeId { get; set; }
		public byte AttributeUsage { get; set; }
		public string Name { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			AttributeId = reader.ReadUInt16();
			OwnerTypeId = reader.ReadUInt16();
			AttributeUsage = reader.ReadByte();
			Name = reader.ReadString();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(AttributeId);
			writer.Write(OwnerTypeId);
			writer.Write(AttributeUsage);
			writer.Write(Name);
		}
	}

	internal class StringInfoRecord : SizedBamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.StringInfo; }
		}

		public ushort StringId { get; set; }
		public string Value { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			StringId = reader.ReadUInt16();
			Value = reader.ReadString();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(StringId);
			writer.Write(Value);
		}
	}

	internal class TextRecord : SizedBamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.Text; }
		}

		public string Value { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			Value = reader.ReadString();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(Value);
		}
	}

	internal class TextWithConverterRecord : TextRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.TextWithConverter; }
		}

		public ushort ConverterTypeId { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			base.ReadData(reader, size);
			ConverterTypeId = reader.ReadUInt16();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			base.WriteData(writer);
			writer.Write(ConverterTypeId);
		}
	}

	internal class TextWithIdRecord : TextRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.TextWithId; }
		}

		public ushort ValueId { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			ValueId = reader.ReadUInt16();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(ValueId);
		}
	}

	internal class LiteralContentRecord : SizedBamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.LiteralContent; }
		}

		public string Value { get; set; }
		public uint Reserved0 { get; set; }
		public uint Reserved1 { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			Value = reader.ReadString();
			Reserved0 = reader.ReadUInt32();
			Reserved1 = reader.ReadUInt32();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(Value);
			writer.Write(Reserved0);
			writer.Write(Reserved1);
		}
	}

	internal class RoutedEventRecord : SizedBamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.RoutedEvent; }
		}

		public string Value { get; set; }
		public ushort AttributeId { get; set; }
		public uint Reserved1 { get; set; }

		protected override void ReadData(BamlBinaryReader reader, int size) {
			AttributeId = reader.ReadUInt16();
			Value = reader.ReadString();
		}

		protected override void WriteData(BamlBinaryWriter writer) {
			writer.Write(Value);
			writer.Write(AttributeId);
		}
	}

	internal class DocumentStartRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.DocumentStart; }
		}

		public bool LoadAsync { get; set; }
		public uint MaxAsyncRecords { get; set; }
		public bool DebugBaml { get; set; }

		public override void Read(BamlBinaryReader reader) {
			LoadAsync = reader.ReadBoolean();
			MaxAsyncRecords = reader.ReadUInt32();
			DebugBaml = reader.ReadBoolean();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(LoadAsync);
			writer.Write(MaxAsyncRecords);
			writer.Write(DebugBaml);
		}
	}

	internal class DocumentEndRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.DocumentEnd; }
		}

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	internal class ElementStartRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.ElementStart; }
		}

		public ushort TypeId { get; set; }
		public byte Flags { get; set; }

		public override void Read(BamlBinaryReader reader) {
			TypeId = reader.ReadUInt16();
			Flags = reader.ReadByte();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(TypeId);
			writer.Write(Flags);
		}
	}

	internal class ElementEndRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.ElementEnd; }
		}

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	internal class KeyElementStartRecord : DefAttributeKeyTypeRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.KeyElementStart; }
		}
	}

	internal class KeyElementEndRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.KeyElementEnd; }
		}

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	internal class ConnectionIdRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.ConnectionId; }
		}

		public uint ConnectionId { get; set; }

		public override void Read(BamlBinaryReader reader) {
			ConnectionId = reader.ReadUInt32();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(ConnectionId);
		}
	}

	internal class PropertyWithExtensionRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.PropertyWithExtension; }
		}

		public ushort AttributeId { get; set; }
		public ushort Flags { get; set; }
		public ushort ValueId { get; set; }

		public override void Read(BamlBinaryReader reader) {
			AttributeId = reader.ReadUInt16();
			Flags = reader.ReadUInt16();
			ValueId = reader.ReadUInt16();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(AttributeId);
			writer.Write(Flags);
			writer.Write(ValueId);
		}
	}

	internal class PropertyTypeReferenceRecord : PropertyComplexStartRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.PropertyTypeReference; }
		}

		public ushort TypeId { get; set; }

		public override void Read(BamlBinaryReader reader) {
			base.Read(reader);
			TypeId = reader.ReadUInt16();
		}

		public override void Write(BamlBinaryWriter writer) {
			base.Write(writer);
			writer.Write(TypeId);
		}
	}

	internal class PropertyStringReferenceRecord : PropertyComplexStartRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.PropertyStringReference; }
		}

		public ushort StringId { get; set; }

		public override void Read(BamlBinaryReader reader) {
			base.Read(reader);
			StringId = reader.ReadUInt16();
		}

		public override void Write(BamlBinaryWriter writer) {
			base.Write(writer);
			writer.Write(StringId);
		}
	}

	internal class PropertyWithStaticResourceIdRecord : StaticResourceIdRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.PropertyWithStaticResourceId; }
		}

		public ushort AttributeId { get; set; }

		public override void Read(BamlBinaryReader reader) {
			AttributeId = reader.ReadUInt16();
			base.Read(reader);
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(AttributeId);
			base.Write(writer);
		}
	}

	internal class ContentPropertyRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.ContentProperty; }
		}

		public ushort AttributeId { get; set; }

		public override void Read(BamlBinaryReader reader) {
			AttributeId = reader.ReadUInt16();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(AttributeId);
		}
	}

	internal class DefAttributeKeyTypeRecord : ElementStartRecord, IBamlDeferRecord {
		internal uint pos = 0xffffffff;

		public override BamlRecordType Type {
			get { return BamlRecordType.DefAttributeKeyType; }
		}

		public bool Shared { get; set; }
		public bool SharedSet { get; set; }

		public BamlRecord Record { get; set; }

		public void ReadDefer(BamlDocument doc, int index, Func<long, BamlRecord> resolve) {
			bool keys = true;
			do {
				switch (doc[index].Type) {
					case BamlRecordType.DefAttributeKeyString:
					case BamlRecordType.DefAttributeKeyType:
					case BamlRecordType.OptimizedStaticResource:
						keys = true;
						break;
					case BamlRecordType.StaticResourceStart:
						NavigateTree(doc, BamlRecordType.StaticResourceStart, BamlRecordType.StaticResourceEnd, ref index);
						keys = true;
						break;
					case BamlRecordType.KeyElementStart:
						NavigateTree(doc, BamlRecordType.KeyElementStart, BamlRecordType.KeyElementEnd, ref index);
						keys = true;
						break;
					default:
						keys = false;
						index--;
						break;
				}
				index++;
			} while (keys);
			Record = resolve(doc[index].Position + pos);
		}

		public void WriteDefer(BamlDocument doc, int index, BinaryWriter wtr) {
			bool keys = true;
			do {
				switch (doc[index].Type) {
					case BamlRecordType.DefAttributeKeyString:
					case BamlRecordType.DefAttributeKeyType:
					case BamlRecordType.OptimizedStaticResource:
						keys = true;
						break;
					case BamlRecordType.StaticResourceStart:
						NavigateTree(doc, BamlRecordType.StaticResourceStart, BamlRecordType.StaticResourceEnd, ref index);
						keys = true;
						break;
					case BamlRecordType.KeyElementStart:
						NavigateTree(doc, BamlRecordType.KeyElementStart, BamlRecordType.KeyElementEnd, ref index);
						keys = true;
						break;
					default:
						keys = false;
						index--;
						break;
				}
				index++;
			} while (keys);
			wtr.BaseStream.Seek(pos, SeekOrigin.Begin);
			wtr.Write((uint)(Record.Position - doc[index].Position));
		}

		public override void Read(BamlBinaryReader reader) {
			base.Read(reader);
			pos = reader.ReadUInt32();
			Shared = reader.ReadBoolean();
			SharedSet = reader.ReadBoolean();
		}

		public override void Write(BamlBinaryWriter writer) {
			base.Write(writer);
			pos = (uint)writer.BaseStream.Position;
			writer.Write((uint)0);
			writer.Write(Shared);
			writer.Write(SharedSet);
		}

		static void NavigateTree(BamlDocument doc, BamlRecordType start, BamlRecordType end, ref int index) {
			index++;
			while (true) {
				if (doc[index].Type == start)
					NavigateTree(doc, start, end, ref index);
				else if (doc[index].Type == end)
					return;
				index++;
			}
		}
	}

	internal class PropertyListStartRecord : PropertyComplexStartRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.PropertyListStart; }
		}
	}

	internal class PropertyListEndRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.PropertyListEnd; }
		}

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	internal class PropertyDictionaryStartRecord : PropertyComplexStartRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.PropertyDictionaryStart; }
		}
	}

	internal class PropertyDictionaryEndRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.PropertyDictionaryEnd; }
		}

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	internal class PropertyArrayStartRecord : PropertyComplexStartRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.PropertyArrayStart; }
		}
	}

	internal class PropertyArrayEndRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.PropertyArrayEnd; }
		}

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	internal class PropertyComplexStartRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.PropertyComplexStart; }
		}

		public ushort AttributeId { get; set; }

		public override void Read(BamlBinaryReader reader) {
			AttributeId = reader.ReadUInt16();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(AttributeId);
		}
	}

	internal class PropertyComplexEndRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.PropertyComplexEnd; }
		}

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	internal class ConstructorParametersStartRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.ConstructorParametersStart; }
		}

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	internal class ConstructorParametersEndRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.ConstructorParametersEnd; }
		}

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	internal class ConstructorParameterTypeRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.ConstructorParameterType; }
		}

		public ushort TypeId { get; set; }

		public override void Read(BamlBinaryReader reader) {
			TypeId = reader.ReadUInt16();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(TypeId);
		}
	}

	internal class DeferableContentStartRecord : BamlRecord, IBamlDeferRecord {
		long pos;
		internal uint size = 0xffffffff;

		public override BamlRecordType Type {
			get { return BamlRecordType.DeferableContentStart; }
		}

		public BamlRecord Record { get; set; }

		public void ReadDefer(BamlDocument doc, int index, Func<long, BamlRecord> resolve) {
			Record = resolve(pos + size);
		}

		public void WriteDefer(BamlDocument doc, int index, BinaryWriter wtr) {
			wtr.BaseStream.Seek(pos, SeekOrigin.Begin);
			wtr.Write((uint)(Record.Position - (pos + 4)));
		}

		public override void Read(BamlBinaryReader reader) {
			size = reader.ReadUInt32();
			pos = reader.BaseStream.Position;
		}

		public override void Write(BamlBinaryWriter writer) {
			pos = writer.BaseStream.Position;
			writer.Write((uint)0);
		}
	}

	internal class StaticResourceStartRecord : ElementStartRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.StaticResourceStart; }
		}
	}

	internal class StaticResourceEndRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.StaticResourceEnd; }
		}

		public override void Read(BamlBinaryReader reader) {
		}

		public override void Write(BamlBinaryWriter writer) {
		}
	}

	internal class StaticResourceIdRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.StaticResourceId; }
		}

		public ushort StaticResourceId { get; set; }

		public override void Read(BamlBinaryReader reader) {
			StaticResourceId = reader.ReadUInt16();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(StaticResourceId);
		}
	}

	internal class OptimizedStaticResourceRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.OptimizedStaticResource; }
		}

		public byte Flags { get; set; }
		public ushort ValueId { get; set; }

		public bool IsType {
			get { return (Flags & 1) != 0; }
		}

		public bool IsStatic {
			get { return (Flags & 2) != 0; }
		}

		public override void Read(BamlBinaryReader reader) {
			Flags = reader.ReadByte();
			ValueId = reader.ReadUInt16();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(Flags);
			writer.Write(ValueId);
		}
	}

	internal class LineNumberAndPositionRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.LineNumberAndPosition; }
		}

		public uint LineNumber { get; set; }
		public uint LinePosition { get; set; }

		public override void Read(BamlBinaryReader reader) {
			LineNumber = reader.ReadUInt32();
			LinePosition = reader.ReadUInt32();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(LineNumber);
			writer.Write(LinePosition);
		}
	}

	internal class LinePositionRecord : BamlRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.LinePosition; }
		}

		public uint LinePosition { get; set; }

		public override void Read(BamlBinaryReader reader) {
			LinePosition = reader.ReadUInt32();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(LinePosition);
		}
	}

	internal class NamedElementStartRecord : ElementStartRecord {
		public override BamlRecordType Type {
			get { return BamlRecordType.NamedElementStart; }
		}

		public string RuntimeName { get; set; }

		public override void Read(BamlBinaryReader reader) {
			TypeId = reader.ReadUInt16();
			RuntimeName = reader.ReadString();
		}

		public override void Write(BamlBinaryWriter writer) {
			writer.Write(TypeId);
			if (RuntimeName != null) {
				writer.Write(RuntimeName);
			}
		}
	}
}