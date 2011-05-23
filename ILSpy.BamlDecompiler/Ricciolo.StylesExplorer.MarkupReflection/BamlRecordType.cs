// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.Text;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	internal enum BamlRecordType : byte
	{
		AssemblyInfo = 0x1c,
		AttributeInfo = 0x1f,
		ClrEvent = 0x13,
		Comment = 0x17,
		ConnectionId = 0x2d,
		ConstructorParametersEnd = 0x2b,
		ConstructorParametersStart = 0x2a,
		ConstructorParameterType = 0x2c,
		ContentProperty = 0x2e,
		DefAttribute = 0x19,
		DefAttributeKeyString = 0x26,
		DefAttributeKeyType = 0x27,
		DeferableContentStart = 0x25,
		DefTag = 0x18,
		DocumentEnd = 2,
		DocumentStart = 1,
		ElementEnd = 4,
		ElementStart = 3,
		EndAttributes = 0x1a,
		KeyElementEnd = 0x29,
		KeyElementStart = 40,
		LastRecordType = 0x39,
		LineNumberAndPosition = 0x35,
		LinePosition = 0x36,
		LiteralContent = 15,
		NamedElementStart = 0x2f,
		OptimizedStaticResource = 0x37,
		PIMapping = 0x1b,
		PresentationOptionsAttribute = 0x34,
		ProcessingInstruction = 0x16,
		Property = 5,
		PropertyArrayEnd = 10,
		PropertyArrayStart = 9,
		PropertyComplexEnd = 8,
		PropertyComplexStart = 7,
		PropertyCustom = 6,
		PropertyDictionaryEnd = 14,
		PropertyDictionaryStart = 13,
		PropertyListEnd = 12,
		PropertyListStart = 11,
		PropertyStringReference = 0x21,
		PropertyTypeReference = 0x22,
		PropertyWithConverter = 0x24,
		PropertyWithExtension = 0x23,
		PropertyWithStaticResourceId = 0x38,
		RoutedEvent = 0x12,
		StaticResourceEnd = 0x31,
		StaticResourceId = 50,
		StaticResourceStart = 0x30,
		StringInfo = 0x20,
		Text = 0x10,
		TextWithConverter = 0x11,
		TextWithId = 0x33,
		TypeInfo = 0x1d,
		TypeSerializerInfo = 30,
		Unknown = 0,
		XmlAttribute = 0x15,
		XmlnsProperty = 20
	}

}
