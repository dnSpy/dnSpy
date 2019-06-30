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
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;
using dnSpy.BamlDecompiler.Baml;
using dnSpy.BamlDecompiler.Xaml;

namespace dnSpy.BamlDecompiler.Handlers {
	internal class PropertyCustomHandler : IHandler {
		public BamlRecordType Type => BamlRecordType.PropertyCustom;

		enum IntegerCollectionType : byte {
			Unknown,
			Consecutive,
			U1,
			U2,
			I4
		}

		string Deserialize(XamlContext ctx, XElement elem, KnownTypes ser, byte[] value) {
			using (BinaryReader reader = new BinaryReader(new MemoryStream(value))) {
				switch (ser) {
					case KnownTypes.DependencyPropertyConverter: {
						if (value.Length == 2) {
							var property = ctx.ResolveProperty(reader.ReadUInt16());
							return ctx.ToString(elem, property.ToXName(ctx, elem, false));
						}
						else {
							var type = ctx.ResolveType(reader.ReadUInt16());
							var name = reader.ReadString();
							var typeName = ctx.ToString(elem, type);
							return typeName + "." + name;
						}
					}

					case KnownTypes.EnumConverter: {
						uint enumVal = reader.ReadUInt32();
						// TODO: Convert to enum names
						return enumVal.ToString("D", CultureInfo.InvariantCulture);
					}

					case KnownTypes.BooleanConverter: {
						Debug.Assert(value.Length == 1);
						return (reader.ReadByte() == 1).ToString(CultureInfo.InvariantCulture);
					}

					case KnownTypes.XamlBrushSerializer: {
						switch (reader.ReadByte()) {
							case 1: // KnownSolidColor
								return string.Format(CultureInfo.InvariantCulture, "#{0:X8}", reader.ReadUInt32());
							case 2: // OtherColor
								return reader.ReadString();
						}
						break;
					}

					case KnownTypes.XamlPathDataSerializer:
						return XamlPathDeserializer.Deserialize(reader);

					case KnownTypes.XamlPoint3DCollectionSerializer:
					case KnownTypes.XamlVector3DCollectionSerializer: {
						var sb = new StringBuilder();
						var count = reader.ReadUInt32();
						for (uint i = 0; i < count; i++) {
							sb.AppendFormat(CultureInfo.InvariantCulture, "{0:R},{1:R},{2:R} ",
								reader.ReadXamlDouble(),
								reader.ReadXamlDouble(),
								reader.ReadXamlDouble());
						}
						return sb.ToString().Trim();
					}

					case KnownTypes.XamlPointCollectionSerializer: {
						var sb = new StringBuilder();
						var count = reader.ReadUInt32();
						for (uint i = 0; i < count; i++) {
							sb.AppendFormat(CultureInfo.InvariantCulture, "{0:R},{1:R} ",
								reader.ReadXamlDouble(),
								reader.ReadXamlDouble());
						}
						return sb.ToString().Trim();
					}

					case KnownTypes.XamlInt32CollectionSerializer: {
						var sb = new StringBuilder();
						var type = (IntegerCollectionType)reader.ReadByte();
						var count = reader.ReadInt32();

						switch (type) {
							case IntegerCollectionType.Consecutive: {
								var start = reader.ReadInt32();
								for (int i = 0; i < count; i++)
									sb.AppendFormat(CultureInfo.InvariantCulture, "{0:D}", start + i);
							}
								break;
							case IntegerCollectionType.U1: {
								for (int i = 0; i < count; i++)
									sb.AppendFormat(CultureInfo.InvariantCulture, "{0:D}", reader.ReadByte());
							}
								break;
							case IntegerCollectionType.U2: {
								for (int i = 0; i < count; i++)
									sb.AppendFormat(CultureInfo.InvariantCulture, "{0:D}", reader.ReadUInt16());
							}
								break;
							case IntegerCollectionType.I4: {
								for (int i = 0; i < count; i++)
									sb.AppendFormat(CultureInfo.InvariantCulture, "{0:D}", reader.ReadInt32());
							}
								break;
							default:
								throw new NotSupportedException(type.ToString());
						}
						return sb.ToString().Trim();
					}
				}
			}
			throw new NotSupportedException(ser.ToString());
		}

		public BamlElement Translate(XamlContext ctx, BamlNode node, BamlElement parent) {
			var record = (PropertyCustomRecord)((BamlRecordNode)node).Record;
			var serTypeId = ((short)record.SerializerTypeId & 0xfff);
			bool valueType = ((short)record.SerializerTypeId & 0x4000) == 0x4000;

			var elemType = parent.Xaml.Element.Annotation<XamlType>();
			var xamlProp = ctx.ResolveProperty(record.AttributeId);

			string value = Deserialize(ctx, parent.Xaml, (KnownTypes)serTypeId, record.Data);
			var attr = new XAttribute(xamlProp.ToXName(ctx, parent.Xaml, xamlProp.IsAttachedTo(elemType)), value);
			parent.Xaml.Element.Add(attr);

			return null;
		}
	}
}
