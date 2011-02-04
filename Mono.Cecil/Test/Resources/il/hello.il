.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89)
  .ver 2:0:0:0
}

.assembly Hello {}

.module Hello.dll

.class private auto ansi Foo {

	.method public specialname rtspecialname instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor ()
		ret
	}

	.method public static void Bar (int32 a, int32 b)
	{
		.locals init (int32 res)
		ldarg.0
		ldarg.1
		mul
		stloc.0
		ldloc.0
		call void Foo::Baz (int32)
		ret
	}

	.method public static void Baz (int32 a)
	{
		ret
	}

	.method public void Gazonk ()
	{
		ldarg 0
		pop
		ret
	}

	.method public static void PrintEmpty ()
	{
		ldsfld string [mscorlib]System.String::Empty
		call void [mscorlib]System.Console::WriteLine(string)
		ret
	}

	.method public static bool TestFilter (bool b) cil managed
	{
		.maxstack 2
		.locals init (bool flag)
	beginTry:
		newobj instance void [mscorlib]System.Exception::.ctor ()
		throw
		leave endCatch
	startFilter:
		pop
		ldarg.0
		endfilter
	startCatch:
		ldc.i4.1
		stloc.0
		leave return
		leave endCatch
	endCatch:
		L_001b: ldc.i4.0
		L_001c: stloc.0
	return:
		L_001d: ldloc.0
		L_001e: ret
		.try beginTry to startFilter filter startFilter handler startCatch to endCatch
	}

	.method public static !!T GetState<T>(string var, [opt] !!T defaultValue) cil managed
	{
		.param [2] = nullref

		ldarg.1
		ret
	}
}
