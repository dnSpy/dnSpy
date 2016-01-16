.class public auto ansi abstract sealed FSharpUsingPatterns
	extends [mscorlib]System.Object
{
	// Methods
	.method public static 
		void sample1 () cil managed 
	{
		// Method begins at RVA 0x2050
		// Code size 53 (0x35)
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
			IL_000e: conv.u1
			IL_000f: callvirt instance void [mscorlib]System.IO.Stream::WriteByte(uint8)
			IL_0014: ldnull
			IL_0015: stloc.1
			IL_0016: leave.s IL_0032
		} // end .try
		finally
		{
			IL_0018: ldloc.0
			IL_0019: isinst [mscorlib]System.IDisposable
			IL_001e: stloc.2
			IL_001f: ldloc.2
			IL_0020: brfalse.s IL_0024

			IL_0022: br.s IL_0026

			IL_0024: br.s IL_002f

			IL_0026: ldloc.2
			IL_0027: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_002c: ldnull
			IL_002d: pop
			IL_002e: endfinally

			IL_002f: ldnull
			IL_0030: pop
			IL_0031: endfinally
		} // end handler

		IL_0032: ldloc.1
		IL_0033: pop
		IL_0034: ret
	} // end of method FSharpUsingPatterns::sample1

	.method public static 
		void sample2 () cil managed 
	{
		// Method begins at RVA 0x20a4
		// Code size 73 (0x49)
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
			IL_0018: conv.u1
			IL_0019: callvirt instance void [mscorlib]System.IO.Stream::WriteByte(uint8)
			IL_001e: ldstr "some text"
			IL_0023: call void [mscorlib]System.Console::WriteLine(string)
			IL_0028: ldnull
			IL_0029: stloc.1
			IL_002a: leave.s IL_0046
		} // end .try
		finally
		{
			IL_002c: ldloc.0
			IL_002d: isinst [mscorlib]System.IDisposable
			IL_0032: stloc.2
			IL_0033: ldloc.2
			IL_0034: brfalse.s IL_0038

			IL_0036: br.s IL_003a

			IL_0038: br.s IL_0043

			IL_003a: ldloc.2
			IL_003b: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_0040: ldnull
			IL_0041: pop
			IL_0042: endfinally

			IL_0043: ldnull
			IL_0044: pop
			IL_0045: endfinally
		} // end handler

		IL_0046: ldloc.1
		IL_0047: pop
		IL_0048: ret
	} // end of method FSharpUsingPatterns::sample2

	.method public static 
		void sample3 () cil managed 
	{
		// Method begins at RVA 0x210c
		// Code size 73 (0x49)
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
			IL_0018: conv.u1
			IL_0019: callvirt instance void [mscorlib]System.IO.Stream::WriteByte(uint8)
			IL_001e: ldnull
			IL_001f: stloc.1
			IL_0020: leave.s IL_003c
		} // end .try
		finally
		{
			IL_0022: ldloc.0
			IL_0023: isinst [mscorlib]System.IDisposable
			IL_0028: stloc.2
			IL_0029: ldloc.2
			IL_002a: brfalse.s IL_002e

			IL_002c: br.s IL_0030

			IL_002e: br.s IL_0039

			IL_0030: ldloc.2
			IL_0031: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_0036: ldnull
			IL_0037: pop
			IL_0038: endfinally

			IL_0039: ldnull
			IL_003a: pop
			IL_003b: endfinally
		} // end handler

		IL_003c: ldloc.1
		IL_003d: pop
		IL_003e: ldstr "some text"
		IL_0043: call void [mscorlib]System.Console::WriteLine(string)
		IL_0048: ret
	} // end of method FSharpUsingPatterns::sample3

	.method public static 
		void sample4 () cil managed 
	{
		// Method begins at RVA 0x2174
		// Code size 89 (0x59)
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
			IL_001e: leave.s IL_003a
		} // end .try
		finally
		{
			IL_0020: ldloc.1
			IL_0021: isinst [mscorlib]System.IDisposable
			IL_0026: stloc.3
			IL_0027: ldloc.3
			IL_0028: brfalse.s IL_002c

			IL_002a: br.s IL_002e

			IL_002c: br.s IL_0037

			IL_002e: ldloc.3
			IL_002f: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_0034: ldnull
			IL_0035: pop
			IL_0036: endfinally

			IL_0037: ldnull
			IL_0038: pop
			IL_0039: endfinally
		} // end handler

		IL_003a: ldloc.2
		IL_003b: stloc.0
		IL_003c: ldstr "read:"
		IL_0041: ldloca.s firstByte
		IL_0043: constrained. [mscorlib]System.Int32
		IL_0049: callvirt instance string [mscorlib]System.Object::ToString()
		IL_004e: call string [mscorlib]System.String::Concat(string, string)
		IL_0053: call void [mscorlib]System.Console::WriteLine(string)
		IL_0058: ret
	} // end of method FSharpUsingPatterns::sample4

	.method public static 
		void sample5 () cil managed 
	{
		// Method begins at RVA 0x21ec
		// Code size 155 (0x9b)
		.maxstack 5
		.locals init (
			[0] int32 firstByte,
			[1] class [mscorlib]System.IO.FileStream fs,
			[2] int32,
			[3] class [mscorlib]System.IDisposable,
			[4] int32 secondByte,
			[5] class [mscorlib]System.IO.FileStream fs,
			[6] int32,
			[7] int32,
			[8] int32,
			[9] class [mscorlib]System.IDisposable
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
			IL_001e: leave.s IL_003a
		} // end .try
		finally
		{
			IL_0020: ldloc.1
			IL_0021: isinst [mscorlib]System.IDisposable
			IL_0026: stloc.3
			IL_0027: ldloc.3
			IL_0028: brfalse.s IL_002c

			IL_002a: br.s IL_002e

			IL_002c: br.s IL_0037

			IL_002e: ldloc.3
			IL_002f: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_0034: ldnull
			IL_0035: pop
			IL_0036: endfinally

			IL_0037: ldnull
			IL_0038: pop
			IL_0039: endfinally
		} // end handler

		IL_003a: ldloc.2
		IL_003b: stloc.0
		IL_003c: nop
		IL_003d: ldstr "x.txt"
		IL_0042: call class [mscorlib]System.IO.FileStream [mscorlib]System.IO.File::OpenRead(string)
		IL_0047: stloc.s fs
		.try
		{
			IL_0049: ldloc.s fs
			IL_004b: callvirt instance int32 [mscorlib]System.IO.Stream::ReadByte()
			IL_0050: stloc.s 7
			IL_0052: ldloc.s 7
			IL_0054: stloc.s 8
			IL_0056: ldloc.s fs
			IL_0058: callvirt instance int32 [mscorlib]System.IO.Stream::ReadByte()
			IL_005d: stloc.s 6
			IL_005f: leave.s IL_007f
		} // end .try
		finally
		{
			IL_0061: ldloc.s fs
			IL_0063: isinst [mscorlib]System.IDisposable
			IL_0068: stloc.s 9
			IL_006a: ldloc.s 9
			IL_006c: brfalse.s IL_0070

			IL_006e: br.s IL_0072

			IL_0070: br.s IL_007c

			IL_0072: ldloc.s 9
			IL_0074: callvirt instance void [mscorlib]System.IDisposable::Dispose()
			IL_0079: ldnull
			IL_007a: pop
			IL_007b: endfinally

			IL_007c: ldnull
			IL_007d: pop
			IL_007e: endfinally
		} // end handler

		IL_007f: ldloc.s 6
		IL_0081: stloc.s secondByte
		IL_0083: ldstr "read: {0}, {1}"
		IL_0088: ldloc.0
		IL_0089: box [mscorlib]System.Int32
		IL_008e: ldloc.s secondByte
		IL_0090: box [mscorlib]System.Int32
		IL_0095: call void [mscorlib]System.Console::WriteLine(string, object, object)
		IL_009a: ret
	} // end of method FSharpUsingPatterns::sample5

} // end of class FSharpUsingPatterns
