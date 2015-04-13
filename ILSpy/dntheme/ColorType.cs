/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using ICSharpCode.NRefactory;

namespace ICSharpCode.ILSpy.dntheme
{
	public enum ColorType
	{
		/// <summary>
		/// default text (in text editor)
		/// </summary>
		Text,

		/// <summary>
		/// {}
		/// </summary>
		Brace,

		/// <summary>
		/// +-/etc and other special chars like ,; etc
		/// </summary>
		Operator,

		/// <summary>
		/// numbers
		/// </summary>
		Number,

		/// <summary>
		/// code comments
		/// </summary>
		Comment,

		/// <summary>
		/// XML tag in XML doc comment. This includes the "///" (C#) or "'''" (VB) part
		/// </summary>
		XmlDocTag,

		/// <summary>
		/// XML attribute in an XML doc tag
		/// </summary>
		XmlDocAttribute,

		/// <summary>
		/// XML doc comments. Whatever is not an XML doc tag / attribute
		/// </summary>
		XmlDocComment,

		/// <summary>
		/// keywords
		/// </summary>
		Keyword,

		/// <summary>
		/// "strings"
		/// </summary>
		String,

		/// <summary>
		/// chars ('a', 'b')
		/// </summary>
		Char,

		/// <summary>
		/// Any part of a namespace
		/// </summary>
		NamespacePart,

		/// <summary>
		/// classes (not keyword-classes eg "int")
		/// </summary>
		Type,

		/// <summary>
		/// static types
		/// </summary>
		StaticType,

		/// <summary>
		/// delegates
		/// </summary>
		Delegate,

		/// <summary>
		/// enums
		/// </summary>
		Enum,

		/// <summary>
		/// interfaces
		/// </summary>
		Interface,

		/// <summary>
		/// value types
		/// </summary>
		ValueType,

		/// <summary>
		/// type generic parameters
		/// </summary>
		TypeGenericParameter,

		/// <summary>
		/// method generic parameters
		/// </summary>
		MethodGenericParameter,

		/// <summary>
		/// instance methods
		/// </summary>
		InstanceMethod,

		/// <summary>
		/// static methods
		/// </summary>
		StaticMethod,

		/// <summary>
		/// extension methods
		/// </summary>
		ExtensionMethod,

		/// <summary>
		/// instance fields
		/// </summary>
		InstanceField,

		/// <summary>
		/// enum fields
		/// </summary>
		EnumField,

		/// <summary>
		/// constant fields (not enum fields)
		/// </summary>
		LiteralField,

		/// <summary>
		/// static fields
		/// </summary>
		StaticField,

		/// <summary>
		/// instance events
		/// </summary>
		InstanceEvent,

		/// <summary>
		/// static events
		/// </summary>
		StaticEvent,

		/// <summary>
		/// instance properties
		/// </summary>
		InstanceProperty,

		/// <summary>
		/// static properties
		/// </summary>
		StaticProperty,

		/// <summary>
		/// method locals
		/// </summary>
		Local,

		/// <summary>
		/// method parameters
		/// </summary>
		Parameter,

		/// <summary>
		/// labels
		/// </summary>
		Label,

		/// <summary>
		/// opcodes
		/// </summary>
		OpCode,

		/// <summary>
		/// IL directive (.sometext)
		/// </summary>
		ILDirective,

		/// <summary>
		/// IL module names, eg. [module]SomeClass
		/// </summary>
		ILModule,

		/// <summary>
		/// Default text in all windows
		/// </summary>
		DefaultText,

		/// <summary>
		/// <see cref="Brace"/> and <see cref="Operator"/>
		/// </summary>
		Punctuation,

		/// <summary>
		/// Any XML
		/// </summary>
		Xml,

		/// <summary>
		/// Literals: strings = (DnColorTypes)TextTokenType.strings, chars = (DnColorTypes)TextTokenType.chars, numbers
		/// </summary>
		Literal,

		/// <summary>
		/// Types = (DnColorTypes)TextTokenType.Types, methods = (DnColorTypes)TextTokenType.methods, keywords = (DnColorTypes)TextTokenType.keywords, etc
		/// </summary>
		Identifier,

		/// <summary>
		/// type/method generic parameter
		/// </summary>
		GenericParameter,

		/// <summary>
		/// methods
		/// </summary>
		Method,

		/// <summary>
		/// fields
		/// </summary>
		Field,

		/// <summary>
		/// events
		/// </summary>
		Event,

		/// <summary>
		/// properties
		/// </summary>
		Property,

		/// <summary>
		/// <see cref="Local"/> or <see cref="Parameter"/>
		/// </summary>
		Variable,

		/// <summary>
		/// Line number (only foreground color)
		/// </summary>
		LineNumber,

		/// <summary>
		/// XML comment
		/// </summary>
		XmlComment,

		/// <summary>
		/// XML CData
		/// </summary>
		XmlCData,

		/// <summary>
		/// XML doc type
		/// </summary>
		XmlDocType,

		/// <summary>
		/// XML declaration
		/// </summary>
		XmlDeclaration,

		/// <summary>
		/// XML tag
		/// </summary>
		XmlTag,

		/// <summary>
		/// XML attribute name
		/// </summary>
		XmlAttributeName,

		/// <summary>
		/// XML attribute value
		/// </summary>
		XmlAttributeValue,

		/// <summary>
		/// XML entity
		/// </summary>
		XmlEntity,

		/// <summary>
		/// XML broken entity
		/// </summary>
		XmlBrokenEntity,

		/// <summary>
		/// Link (http, https, mailto, etc)
		/// </summary>
		Link,

		/// <summary>
		/// Selected text
		/// </summary>
		Selection,

		/// <summary>
		/// Local definition
		/// </summary>
		LocalDefinition,

		/// <summary>
		/// Local reference
		/// </summary>
		LocalReference,

		/// <summary>
		/// Current statement (debugger)
		/// </summary>
		CurrentStatement,

		/// <summary>
		/// Return statement (debugger)
		/// </summary>
		ReturnStatement,

		/// <summary>
		/// A selected return statement (it's been double clicked in the call stack window) (debugger)
		/// </summary>
		SelectedReturnStatement,

		/// <summary>
		/// Breakpoint statement (debugger)
		/// </summary>
		BreakpointStatement,

		/// <summary>
		/// Disabled breakpoint statement (debugger)
		/// </summary>
		DisabledBreakpointStatement,

		/// <summary>
		/// Special character box color. Only background color is used
		/// </summary>
		SpecialCharacterBox,

		/// <summary>
		/// Search result marker. Only background color is used.
		/// </summary>
		SearchResultMarker,

		/// <summary>
		/// Current line. Foreground color is border color.
		/// </summary>
		CurrentLine,

		/// <summary>
		/// Must be last
		/// </summary>
		Last,
	}
}
