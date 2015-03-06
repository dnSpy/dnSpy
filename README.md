dnSpy
=====

dnSpy is [ILSpy](https://github.com/icsharpcode/ILSpy) using [dnlib](https://github.com/0xd4d/dnlib). dnSpy is now able to open assemblies that ILSpy can't.

Extra features present in dnSpy
===============================

* More stable, can handle bad input that will crash ILSpy
* Updated syntax highlighting code
* Debugger
* Multifile assembly support
* Command line decompiler (dnspc.exe)
* Other minor updates / fixes

Debugger
========

This is a slightly updated debugger that was removed from ILSpy. It's lacking many useful features I plan to add in the future.

Updates to the Debugger
=======================

* Can debug VB code
* Can debug ILAst code (this language is only available in Debug builds)
* Only the current statement is highlighted instead of the whole line
* Other minor updates / fixes

Known issues
============

* Debugger break command doesn't always work. Keep breaking until it finally gets it.
* Stepping over an `endfinally` instruction causes the current line to be hidden. The reason is that the IP is unknown. Press F10 a few times and the IP should be known again.
* The decompiler doesn't preserve all IL ranges which could cause some weird things happening while debugging, eg. you press F10 but remain on the same statement.
* Debugger + IL mode: BPs can be set on any IL offset but the BPs are only triggered if they're at the start of statements (eg. offsets where the IL stack is empty and offsets following method calls).

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
SpecialCharacterBox
SearchResultMarker
CurrentLine
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
		LocalDefinition
		LocalReference
		CurrentStatement
		ReturnStatement
		BreakpointStatement
		DisabledBreakpointStatement
```

`fg` and `bg` take a color that can be human readable (eg. `Green`) or hexadecimal (eg. `#112233`) or a System.Windows.SystemColors brush name, eg. `SystemColors.ControlText` will use `System.Windows.SystemColors.ControlTextBrush`. Note that the `Brush` part must not be present in the string.
