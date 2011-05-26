// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.Text;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	internal enum BamlRecordType : byte
	{
		Unknown,
		DocumentStart,
		DocumentEnd,
		ElementStart,
		ElementEnd,
		Property,
		PropertyCustom,
		PropertyComplexStart,
		PropertyComplexEnd,
		PropertyArrayStart,
		PropertyArrayEnd,
		PropertyListStart,
		PropertyListEnd,
		PropertyDictionaryStart,
		PropertyDictionaryEnd,
		LiteralContent,
		Text,
		TextWithConverter,
		RoutedEvent,
		ClrEvent,
		XmlnsProperty,
		XmlAttribute,
		ProcessingInstruction,
		Comment,
		DefTag,
		DefAttribute,
		EndAttributes,
		PIMapping,
		AssemblyInfo,
		TypeInfo,
		TypeSerializerInfo,
		AttributeInfo,
		StringInfo,
		PropertyStringReference,
		PropertyTypeReference,
		PropertyWithExtension,
		PropertyWithConverter,
		DeferableContentStart,
		DefAttributeKeyString,
		DefAttributeKeyType,
		KeyElementStart,
		KeyElementEnd,
		ConstructorParametersStart,
		ConstructorParametersEnd,
		ConstructorParameterType,
		ConnectionId,
		ContentProperty,
		NamedElementStart,
		StaticResourceStart,
		StaticResourceEnd,
		StaticResourceId,
		TextWithId,
		PresentationOptionsAttribute,
		LineNumberAndPosition,
		LinePosition,
		OptimizedStaticResource,
		PropertyWithStaticResourceId,
		LastRecordType
	}
}
