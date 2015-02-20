dnSpy
=====

dnSpy is [ILSpy](https://github.com/icsharpcode/ILSpy) using [dnlib](https://github.com/0xd4d/dnlib). dnSpy is now able to open assemblies that ILSpy can't.

Differences between dnSpy and ILSpy
===================================

* More stable, can handle bad input that will crash ILSpy
* Updated syntax highlighting code
* Debugger
* Multifile assembly support
* Command line decompiler (dnspc.exe)
* Other minor updates / fixes

Credits
=======

For license info, authors and other credits, see README.txt.

Build instructions
==================

You need [dnlib](https://github.com/0xd4d/dnlib) and you must define `THREAD_SAFE` when compiling it. dnSpy will immediately exit if it detects that dnlib isn't thread safe.

Use Visual Studio 2010 or later or run `debugbuild.bat` / `releasebuild.bat` to build it.

You need the Visual Studio SDK to build ILSpy.AddIn.

Themes
======
dnSpy looks for *.dntheme files in a dntheme sub directory.

```XML
<theme name="dark" menu-name="_Dark" sort="100">
	<colors>
		<color name="defaulttext" fg="#D0D0D0" bg="#252525" />
		<color name="brace" fg="#A6B8DB" />
...
	</colors>
</theme>
```

`<theme>` `sort` attribute is used to sort the files in the menu.

`<color>` must have a `name` attribute. It is case insensitive and must be the name of a valid `ColorType` enum value, see below. Foreground (`fg`), background (`bg`), `italics` / `bold`  (`true` or `false`) can be used to override the inherited attribute. A `defaulttext` `<color>` should always be specified and should have both a `fg` and a `bg` attribute. `ColorType` inheritance hierarchy (see `Theme.cs`):

```
Selection
DefaultText
	Text
		Punctuation
			Brace
			Operator
		Comment
		Xml
			XmlDocTag
			XmlDocAttribute
			XmlDocComment
			XmlComment
			XmlCData
			XmlDocType
			XmlDeclaration
			XmlTag
			XmlAttributeName
			XmlAttributeValue
			XmlEntity
			XmlBrokenEntity
		Literal
			Number
			String
			Char
		Identifier
			Keyword
			NamespacePart
			Type
				StaticType
				Delegate
				Enum
				Interface
				ValueType
			GenericParameter
				TypeGenericParameter
				MethodGenericParameter
			Method
				InstanceMethod
				StaticMethod
				ExtensionMethod
			Field
				InstanceField
				EnumField
				LiteralField
				StaticField
			Event
				InstanceEvent
				StaticEvent
			Property
				InstanceProperty
				StaticProperty
			Variable
				Local
				Parameter
			Label
			OpCode
			ILDirective
			ILModule
		LineNumber
		Link
```

`fg` and `bg` take a color that can be human readable (eg. `Green`) or hexadecimal (eg. `#112233`) or a System.Windows.SystemColors brush name, eg. `SystemColors.ControlText` will use `System.Windows.SystemColors.ControlTextBrush`. Note that the `Brush` part must not be present in the string.
