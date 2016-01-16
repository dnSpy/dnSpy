.class public auto ansi abstract sealed FSharpUsingPatterns
	extends [mscorlib]System.Object
{
	// Methods
	.method public static 
		void sample1 () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 48 (0x30)
		.maxstack 4
		.locals init (
			[0] class [mscorlib]System.IO.FileStream fs,
			[1] class [mscorlib]System.Object,
			[2] class [mscorlib]System.IDisposable
		)

		IL_0000: nop
		IL_0001: ldstr "x.txt"
		IL_0006: call class [mscorlib]System.IO.FileStream [mscorlib]System.IO.File::Create(string)
		IL_000b: stloc.0
		.try
		{
			IL_000c: ldloc.0
			IL_000d: ldc.i4.1
			IL_000e: callvirt instance void [mscorlib]System.IO.Stream::WriteByte(uint8)
			IL_0013: ldnull
			IL_0014: stloc.1
			IL_0015: leave.s IL_002d
		} // end .try
		finally
		{
			IL_0017: ldloc.0
			IL_0018: isinst [mscorlib]System.IDisposable
			IL_001d: stloc.2
			IL_001e: ldloc.2
			IL_001f: brfalse.s IL_002a

			IL_0021: ldloc.2
			IL_0022: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_0027: ldnull
			IL_0028: pop
			IL_0029: endfinally

			IL_002a: ldnull
			IL_002b: pop
			IL_002c: endfinally
		} // end handler

		IL_002d: ldloc.1
		IL_002e: pop
		IL_002f: ret
	} // end of method FSharpUsingPatterns::sample1

	.method public static 
		void sample2 () cil managed 
	{
		// Method begins at RVA 0x209c
		// Code size 68 (0x44)
		.maxstack 4
		.locals init (
			[0] class [mscorlib]System.IO.FileStream fs,
			[1] class [mscorlib]System.Object,
			[2] class [mscorlib]System.IDisposable
		)

		IL_0000: nop
		IL_0001: ldstr "some text"
		IL_0006: call void [mscorlib]System.Console::WriteLine(string)
		IL_000b: ldstr "x.txt"
		IL_0010: call class [mscorlib]System.IO.FileStream [mscorlib]System.IO.File::Create(string)
		IL_0015: stloc.0
		.try
		{
			IL_0016: ldloc.0
			IL_0017: ldc.i4.2
			IL_0018: callvirt instance void [mscorlib]System.IO.Stream::WriteByte(uint8)
			IL_001d: ldstr "some text"
			IL_0022: call void [mscorlib]System.Console::WriteLine(string)
			IL_0027: ldnull
			IL_0028: stloc.1
			IL_0029: leave.s IL_0041
		} // end .try
		finally
		{
			IL_002b: ldloc.0
			IL_002c: isinst [mscorlib]System.IDisposable
			IL_0031: stloc.2
			IL_0032: ldloc.2
			IL_0033: brfalse.s IL_003e

			IL_0035: ldloc.2
			IL_0036: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_003b: ldnull
			IL_003c: pop
			IL_003d: endfinally

			IL_003e: ldnull
			IL_003f: pop
			IL_0040: endfinally
		} // end handler

		IL_0041: ldloc.1
		IL_0042: pop
		IL_0043: ret
	} // end of method FSharpUsingPatterns::sample2

	.method public static 
		void sample3 () cil managed 
	{
		// Method begins at RVA 0x20fc
		// Code size 68 (0x44)
		.maxstack 4
		.locals init (
			[0] class [mscorlib]System.IO.FileStream fs,
			[1] class [mscorlib]System.Object,
			[2] class [mscorlib]System.IDisposable
		)

		IL_0000: nop
		IL_0001: ldstr "some text"
		IL_0006: call void [mscorlib]System.Console::WriteLine(string)
		IL_000b: ldstr "x.txt"
		IL_0010: call class [mscorlib]System.IO.FileStream [mscorlib]System.IO.File::Create(string)
		IL_0015: stloc.0
		.try
		{
			IL_0016: ldloc.0
			IL_0017: ldc.i4.3
			IL_0018: callvirt instance void [mscorlib]System.IO.Stream::WriteByte(uint8)
			IL_001d: ldnull
			IL_001e: stloc.1
			IL_001f: leave.s IL_0037
		} // end .try
		finally
		{
			IL_0021: ldloc.0
			IL_0022: isinst [mscorlib]System.IDisposable
			IL_0027: stloc.2
			IL_0028: ldloc.2
			IL_0029: brfalse.s IL_0034

			IL_002b: ldloc.2
			IL_002c: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_0031: ldnull
			IL_0032: pop
			IL_0033: endfinally

			IL_0034: ldnull
			IL_0035: pop
			IL_0036: endfinally
		} // end handler

		IL_0037: ldloc.1
		IL_0038: pop
		IL_0039: ldstr "some text"
		IL_003e: call void [mscorlib]System.Console::WriteLine(string)
		IL_0043: ret
	} // end of method FSharpUsingPatterns::sample3

	.method public static 
		void sample4 () cil managed 
	{
		// Method begins at RVA 0x215c
		// Code size 85 (0x55)
		.maxstack 4
		.locals init (
			[0] int32 firstByte,
			[1] class [mscorlib]System.IO.FileStream fs,
			[2] int32,
			[3] class [mscorlib]System.IDisposable
		)

		IL_0000: nop
		IL_0001: ldstr "some text"
		IL_0006: call void [mscorlib]System.Console::WriteLine(string)
		IL_000b: nop
		IL_000c: ldstr "x.txt"
		IL_0011: call class [mscorlib]System.IO.FileStream [mscorlib]System.IO.File::OpenRead(string)
		IL_0016: stloc.1
		.try
		{
			IL_0017: ldloc.1
			IL_0018: callvirt instance int32 [mscorlib]System.IO.Stream::ReadByte()
			IL_001d: stloc.2
			IL_001e: leave.s IL_0036
		} // end .try
		finally
		{
			IL_0020: ldloc.1
			IL_0021: isinst [mscorlib]System.IDisposable
			IL_0026: stloc.3
			IL_0027: ldloc.3
			IL_0028: brfalse.s IL_0033

			IL_002a: ldloc.3
			IL_002b: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_0030: ldnull
			IL_0031: pop
			IL_0032: endfinally

			IL_0033: ldnull
			IL_0034: pop
			IL_0035: endfinally
		} // end handler

		IL_0036: ldloc.2
		IL_0037: stloc.0
		IL_0038: ldstr "read:"
		IL_003d: ldloca.s firstByte
		IL_003f: constrained. [mscorlib]System.Int32
		IL_0045: callvirt instance string [mscorlib]System.Object::ToString()
		IL_004a: call string [mscorlib]System.String::Concat(string, string)
		IL_004f: call void [mscorlib]System.Console::WriteLine(string)
		IL_0054: ret
	} // end of method FSharpUsingPatterns::sample4

	.method public static 
		void sample5 () cil managed 
	{
		// Method begins at RVA 0x21d0
		// Code size 134 (0x86)
		.maxstack 5
		.locals init (
			[0] int32 firstByte,
			[1] class [mscorlib]System.IO.FileStream fs,
			[2] int32 secondByte,
			[3] class [mscorlib]System.IDisposable,
			[4] int32,
			[5] int32
		)

		IL_0000: nop
		IL_0001: ldstr "some text"
		IL_0006: call void [mscorlib]System.Console::WriteLine(string)
		IL_000b: nop
		IL_000c: ldstr "x.txt"
		IL_0011: call class [mscorlib]System.IO.FileStream [mscorlib]System.IO.File::OpenRead(string)
		IL_0016: stloc.1
		.try
		{
			IL_0017: ldloc.1
			IL_0018: callvirt instance int32 [mscorlib]System.IO.Stream::ReadByte()
			IL_001d: stloc.2
			IL_001e: leave.s IL_0036
		} // end .try
		finally
		{
			IL_0020: ldloc.1
			IL_0021: isinst [mscorlib]System.IDisposable
			IL_0026: stloc.3
			IL_0027: ldloc.3
			IL_0028: brfalse.s IL_0033

			IL_002a: ldloc.3
			IL_002b: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_0030: ldnull
			IL_0031: pop
			IL_0032: endfinally

			IL_0033: ldnull
			IL_0034: pop
			IL_0035: endfinally
		} // end handler

		IL_0036: ldloc.2
		IL_0037: stloc.0
		IL_0038: nop
		IL_0039: ldstr "x.txt"
		IL_003e: call class [mscorlib]System.IO.FileStream [mscorlib]System.IO.File::OpenRead(string)
		IL_0043: stloc.1
		.try
		{
			IL_0044: ldloc.1
			IL_0045: callvirt instance int32 [mscorlib]System.IO.Stream::ReadByte()
			IL_004a: stloc.s 5
			IL_004c: ldloc.1
			IL_004d: callvirt instance int32 [mscorlib]System.IO.Stream::ReadByte()
			IL_0052: stloc.s 4
			IL_0054: leave.s IL_006c
		} // end .try
		finally
		{
			IL_0056: ldloc.1
			IL_0057: isinst [mscorlib]System.IDisposable
			IL_005c: stloc.3
			IL_005d: ldloc.3
			IL_005e: brfalse.s IL_0069

			IL_0060: ldloc.3
			IL_0061: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_0066: ldnull
			IL_0067: pop
			IL_0068: endfinally

			IL_0069: ldnull
			IL_006a: pop
			IL_006b: endfinally
		} // end handler

		IL_006c: ldloc.s 4
		IL_006e: stloc.2
		IL_006f: ldstr "read: {0}, {1}"
		IL_0074: ldloc.0
		IL_0075: box [mscorlib]System.Int32
		IL_007a: ldloc.2
		IL_007b: box [mscorlib]System.Int32
		IL_0080: call void [mscorlib]System.Console::WriteLine(string, object, object)
		IL_0085: ret
	} // end of method FSharpUsingPatterns::sample5

} // end of class FSharpUsingPatterns
