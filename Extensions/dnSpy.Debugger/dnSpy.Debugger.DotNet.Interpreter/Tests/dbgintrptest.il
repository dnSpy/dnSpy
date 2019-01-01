/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

// "C:\Windows\Microsoft.NET\Framework\v4.0.30319\ilasm.exe" ..\dbgintrptest.il /nologo /quiet /dll /output=dnSpy.Debugger.DotNet.Interpreter.Tests.dll /key=..\..\..\dnSpy.snk

.assembly extern mscorlib {
	.publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
	.ver 4:0:0:0
}

.assembly dnSpy.Debugger.DotNet.Interpreter.Tests {
	.hash algorithm 0x00008004
	.ver 1:0:0:0
}

.module dnSpy.Debugger.DotNet.Interpreter.Tests.dll
.subsystem 0x0003
.corflags 0x00000001

/*
TODO: Test these instructions
box/unbox nullable types
cpblk
mkrefany
refanytype
refanyval
newobj
call
callvirt
constrained
ldftn
ldvirtftn
calli
arglist
jmp
tailcall
unbox
endfilter
endfinally
rethrow
throw
*/

.class private sequential ansi sealed beforefieldinit dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct extends [mscorlib]System.ValueType {
	.field public static int32 Counter
	.field public string Name
	.field public int32 Id

	.method public hidebysig specialname rtspecialname instance void .ctor(string, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		stfld		string dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Name
		ldarg.0
		ldarg.2
		stfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Id
		ret
	}

	.method public hidebysig instance int32 InstanceMethod1() cil managed {
		.maxstack	8
		ldarg.0
		constrained.	dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct
		callvirt	instance int32 [mscorlib]System.Object::GetHashCode()
		pop
		ldc.i4.s	123
		ret
	}

	.method public hidebysig instance int32 InstanceMethod2(int32) cil managed {
		.maxstack	8
		ldarg.0
		constrained.	dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct
		callvirt	instance int32 [mscorlib]System.Object::GetHashCode()
		pop
		ldarg.1
		ret
	}

	.method public hidebysig instance string InstanceMethod3(int32, string) cil managed {
		.maxstack	8
		ldarg.0
		constrained.	dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct
		callvirt	instance int32 [mscorlib]System.Object::GetHashCode()
		pop
		ldarg.2
		ret
	}

	.method public hidebysig instance float64 InstanceMethod4(int32, string, float64) cil managed {
		.maxstack	8
		ldarg.0
		constrained.	dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct
		callvirt	instance int32 [mscorlib]System.Object::GetHashCode()
		pop
		ldarg.3
		ret
	}

	.method public hidebysig static int32 StaticMethod1() cil managed {
		.maxstack	8
		ldc.i4.s	123
		ret
	}

	.method public hidebysig static int32 StaticMethod2(int32) cil managed {
		.maxstack	8
		ldarg.0
		ret
	}

	.method public hidebysig static string StaticMethod3(int32, string) cil managed {
		.maxstack	8
		ldarg.1
		ret
	}

	.method public hidebysig static float64 StaticMethod4(int32, string, float64) cil managed {
		.maxstack	8
		ldarg.2
		ret
	}
}

.class private auto ansi sealed beforefieldinit dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass extends [mscorlib]System.Object {
	.field public static int32 Counter
	.field public string Name
	.field public int32 Id

	.method public hidebysig specialname rtspecialname instance void .ctor() cil managed {
		.maxstack	8
		ldarg.0
		call		instance void [mscorlib]System.Object::.ctor()
		ret
	}

	.method public hidebysig specialname rtspecialname instance void .ctor(string, int32) cil managed {
		.maxstack	8
		ldarg.0
		call		instance void [mscorlib]System.Object::.ctor()
		ldarg.0
		ldarg.1
		stfld		string dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass::Name
		ldarg.0
		ldarg.2
		stfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass::Id
		ret
	}

	.method public hidebysig instance int32 InstanceMethod1() cil managed {
		.maxstack	8
		ldarg.0
		callvirt	instance int32 [mscorlib]System.Object::GetHashCode()
		pop
		ldc.i4.s	123
		ret
	}

	.method public hidebysig instance int32 InstanceMethod2(int32) cil managed {
		.maxstack	8
		ldarg.0
		callvirt	instance int32 [mscorlib]System.Object::GetHashCode()
		pop
		ldarg.1
		ret
	}

	.method public hidebysig instance string InstanceMethod3(int32, string) cil managed {
		.maxstack	8
		ldarg.0
		callvirt	instance int32 [mscorlib]System.Object::GetHashCode()
		pop
		ldarg.2
		ret
	}

	.method public hidebysig instance float64 InstanceMethod4(int32, string, float64) cil managed {
		.maxstack	8
		ldarg.0
		callvirt	instance int32 [mscorlib]System.Object::GetHashCode()
		pop
		ldarg.3
		ret
	}

	.method public hidebysig static int32 StaticMethod1() cil managed {
		.maxstack	8
		ldc.i4.s	123
		ret
	}

	.method public hidebysig static int32 StaticMethod2(int32) cil managed {
		.maxstack	8
		ldarg.0
		ret
	}

	.method public hidebysig static string StaticMethod3(int32, string) cil managed {
		.maxstack	8
		ldarg.1
		ret
	}

	.method public hidebysig static float64 StaticMethod4(int32, string, float64) cil managed {
		.maxstack	8
		ldarg.2
		ret
	}
}

.class private abstract auto ansi sealed beforefieldinit dnSpy.Debugger.DotNet.Interpreter.Tests.TestClass extends [mscorlib]System.Object {
	.method public hidebysig static void Test_RET__Void() cil managed {
		.maxstack	8
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4_M1() cil managed {
		.maxstack	8
		ldc.i4.m1
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4_0() cil managed {
		.maxstack	8
		ldc.i4.0
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4_1() cil managed {
		.maxstack	8
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4_2() cil managed {
		.maxstack	8
		ldc.i4.2
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4_3() cil managed {
		.maxstack	8
		ldc.i4.3
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4_4() cil managed {
		.maxstack	8
		ldc.i4.4
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4_5() cil managed {
		.maxstack	8
		ldc.i4.5
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4_6() cil managed {
		.maxstack	8
		ldc.i4.6
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4_7() cil managed {
		.maxstack	8
		ldc.i4.7
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4_8() cil managed {
		.maxstack	8
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4_S__MinValue() cil managed {
		.maxstack	8
		ldc.i4.s	-0x80
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4_S__MaxValue() cil managed {
		.maxstack	8
		ldc.i4.s	0x7F
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4_S__M1() cil managed {
		.maxstack	8
		ldc.i4.s	-1
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4_S__0() cil managed {
		.maxstack	8
		ldc.i4.s	0
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4_S__1() cil managed {
		.maxstack	8
		ldc.i4.s	1
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4__MinValue() cil managed {
		.maxstack	8
		ldc.i4		-0x80000000
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4__MaxValue() cil managed {
		.maxstack	8
		ldc.i4		0x7FFFFFFF
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4__M1() cil managed {
		.maxstack	8
		ldc.i4		-1
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4__0() cil managed {
		.maxstack	8
		ldc.i4		0
		ret
	}

	.method public hidebysig static int32 Test_LDC_I4__1() cil managed {
		.maxstack	8
		ldc.i4		1
		ret
	}

	.method public hidebysig static int64 Test_LDC_I8__MinValue() cil managed {
		.maxstack	8
		ldc.i8		-0x8000000000000000
		ret
	}

	.method public hidebysig static int64 Test_LDC_I8__MaxValue() cil managed {
		.maxstack	8
		ldc.i8		0x7FFFFFFFFFFFFFFF
		ret
	}

	.method public hidebysig static int64 Test_LDC_I8__M1() cil managed {
		.maxstack	8
		ldc.i8		-1
		ret
	}

	.method public hidebysig static int64 Test_LDC_I8__0() cil managed {
		.maxstack	8
		ldc.i8		0
		ret
	}

	.method public hidebysig static int64 Test_LDC_I8__1() cil managed {
		.maxstack	8
		ldc.i8		1
		ret
	}

	.method public hidebysig static float32 Test_LDC_R4__NaN() cil managed {
		.maxstack	8
		ldc.r4		(00 00 C0 FF)
		ret
	}

	.method public hidebysig static float32 Test_LDC_R4__Epsilon() cil managed {
		.maxstack	8
		ldc.r4		1.401298E-45
		ret
	}

	.method public hidebysig static float32 Test_LDC_R4__MinValue() cil managed {
		.maxstack	8
		ldc.r4		-3.40282347E+38
		ret
	}

	.method public hidebysig static float32 Test_LDC_R4__MaxValue() cil managed {
		.maxstack	8
		ldc.r4		3.40282347E+38
		ret
	}

	.method public hidebysig static float32 Test_LDC_R4__NegativeInfinity() cil managed {
		.maxstack	8
		ldc.r4		(00 00 80 FF)
		ret
	}

	.method public hidebysig static float32 Test_LDC_R4__PositiveInfinity() cil managed {
		.maxstack	8
		ldc.r4		(00 00 80 7F)
		ret
	}

	.method public hidebysig static float32 Test_LDC_R4__PositiveZero() cil managed {
		.maxstack	8
		ldc.r4		0.0
		ret
	}

	.method public hidebysig static float32 Test_LDC_R4__NegativeZero() cil managed {
		.maxstack	8
		ldc.r4		-0.0
		ret
	}

	.method public hidebysig static float32 Test_LDC_R4__M1() cil managed {
		.maxstack	8
		ldc.r4		-1
		ret
	}

	.method public hidebysig static float32 Test_LDC_R4__1() cil managed {
		.maxstack	8
		ldc.r4		1
		ret
	}

	.method public hidebysig static float64 Test_LDC_R8__NaN() cil managed {
		.maxstack	8
		ldc.r8		(00 00 00 00 00 00 F8 FF)
		ret
	}

	.method public hidebysig static float64 Test_LDC_R8__Epsilon() cil managed {
		.maxstack	8
		ldc.r8		4.94065645841247E-324
		ret
	}

	.method public hidebysig static float64 Test_LDC_R8__MinValue() cil managed {
		.maxstack	8
		ldc.r8		-1.7976931348623157E+308
		ret
	}

	.method public hidebysig static float64 Test_LDC_R8__MaxValue() cil managed {
		.maxstack	8
		ldc.r8		1.7976931348623157E+308
		ret
	}

	.method public hidebysig static float64 Test_LDC_R8__NegativeInfinity() cil managed {
		.maxstack	8
		ldc.r8		(00 00 00 00 00 00 F0 FF)
		ret
	}

	.method public hidebysig static float64 Test_LDC_R8__PositiveInfinity() cil managed {
		.maxstack	8
		ldc.r8		(00 00 00 00 00 00 F0 7F)
		ret
	}

	.method public hidebysig static float64 Test_LDC_R8__PositiveZero() cil managed {
		.maxstack	8
		ldc.r8		0.0
		ret
	}

	.method public hidebysig static float64 Test_LDC_R8__NegativeZero() cil managed {
		.maxstack	8
		ldc.r8		-0.0
		ret
	}

	.method public hidebysig static float64 Test_LDC_R8__M1() cil managed {
		.maxstack	8
		ldc.r8		-1
		ret
	}

	.method public hidebysig static float64 Test_LDC_R8__1() cil managed {
		.maxstack	8
		ldc.r8		1
		ret
	}

	.method public hidebysig static string Test_LDSTR() cil managed {
		.maxstack	8
		ldstr		"Hello, World!"
		ret
	}

	.method public hidebysig static string Test_LDNULL() cil managed {
		.maxstack	8
		ldnull
		ret
	}

	.method public hidebysig static int32 Test_LDARG_0(int32) cil managed {
		.maxstack	8
		ldarg.0
		ret
	}

	.method public hidebysig static int32 Test_LDARG_1(string, int32) cil managed {
		.maxstack	8
		ldarg.1
		ret
	}

	.method public hidebysig static int32 Test_LDARG_2(string, string, int32) cil managed {
		.maxstack	8
		ldarg.2
		ret
	}

	.method public hidebysig static int32 Test_LDARG_3(string, string, string, int32) cil managed {
		.maxstack	8
		ldarg.3
		ret
	}

	.method public hidebysig static int32 Test_LDARG_S__0(int32) cil managed {
		.maxstack	8
		ldarg.s		0
		ret
	}

	.method public hidebysig static int32 Test_LDARG_S__4(string, string, string, string, int32) cil managed {
		.maxstack	8
		ldarg.s		4
		ret
	}

	.method public hidebysig static int32 Test_LDARG_S__128(string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, int32) cil managed {
		.maxstack	8
		ldarg.s		0x80
		ret
	}

	.method public hidebysig static int32 Test_LDARG__0(int32) cil managed {
		.maxstack	8
		ldarg		0
		ret
	}

	.method public hidebysig static int32 Test_LDARG__4(string, string, string, string, int32) cil managed {
		.maxstack	8
		ldarg		4
		ret
	}

	.method public hidebysig static int32 Test_LDARG__128(string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, int32) cil managed {
		.maxstack	8
		ldarg.s		0x80
		ret
	}

	.method public hidebysig static int32 Test_LDLOC_0() cil managed {
		.maxstack	1
		.locals		init (int32)
		ldloc.0
		ret
	}

	.method public hidebysig static int32 Test_LDLOC_1() cil managed {
		.maxstack	1
		.locals		init (string, int32)
		ldloc.1
		ret
	}

	.method public hidebysig static int32 Test_LDLOC_2() cil managed {
		.maxstack	1
		.locals		init (string, string, int32)
		ldloc.2
		ret
	}

	.method public hidebysig static int32 Test_LDLOC_3() cil managed {
		.maxstack	1
		.locals		init (string, string, string, int32)
		ldloc.3
		ret
	}

	.method public hidebysig static int32 Test_LDLOC_S__0() cil managed {
		.maxstack	1
		.locals		init (int32)
		ldloc.s		0
		ret
	}

	.method public hidebysig static int32 Test_LDLOC_S__4() cil managed {
		.maxstack	1
		.locals		init (string, string, string, string, int32)
		ldloc.s		4
		ret
	}

	.method public hidebysig static int32 Test_LDLOC_S__128() cil managed {
		.maxstack	1
		.locals		init (string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, int32)
		ldloc.s		0x80
		ret
	}

	.method public hidebysig static int32 Test_LDLOC__0() cil managed {
		.maxstack	1
		.locals		init (int32)
		ldloc.s		0
		ret
	}

	.method public hidebysig static int32 Test_LDLOC__4() cil managed {
		.maxstack	1
		.locals		init (string, string, string, string, int32)
		ldloc.s		4
		ret
	}

	.method public hidebysig static int32 Test_LDLOC__128() cil managed {
		.maxstack	1
		.locals		init (string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, int32)
		ldloc.s		0x80
		ret
	}

	.method public hidebysig static int32 Test_STARG_S__0(int32, int32) cil managed {
		.maxstack	1
		.locals		init (int32, string)
		ldarg.1
		starg.s		0
		ldarg.0
		ret
	}

	.method public hidebysig static int32 Test_STARG_S__128(int32, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, int32) cil managed {
		.maxstack	1
		.locals		init (int32, string)
		ldarg.0
		starg.s		0x80
		ldarg		0x80
		ret
	}

	.method public hidebysig static int32 Test_STARG__0(int32, int32) cil managed {
		.maxstack	1
		.locals		init (int32, string)
		ldarg.1
		starg.s		0
		ldarg.0
		ret
	}

	.method public hidebysig static int32 Test_STARG__128(int32, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, int32) cil managed {
		.maxstack	1
		.locals		init (int32, string)
		ldarg.0
		starg.s		0x80
		ldarg		0x80
		ret
	}

	.method public hidebysig static int32 Test_STLOC_0(int32) cil managed {
		.maxstack	1
		.locals		init (int32)
		ldarg.0
		stloc.0
		ldloc.0
		ret
	}

	.method public hidebysig static int32 Test_STLOC_1(int32) cil managed {
		.maxstack	1
		.locals		init (string, int32)
		ldarg.0
		stloc.1
		ldloc.1
		ret
	}

	.method public hidebysig static int32 Test_STLOC_2(int32) cil managed {
		.maxstack	1
		.locals		init (string, string, int32)
		ldarg.0
		stloc.2
		ldloc.2
		ret
	}

	.method public hidebysig static int32 Test_STLOC_3(int32) cil managed {
		.maxstack	1
		.locals		init (string, string, string, int32)
		ldarg.0
		stloc.3
		ldloc.3
		ret
	}

	.method public hidebysig static int32 Test_STLOC_S__0(int32) cil managed {
		.maxstack	1
		.locals		init (int32)
		ldarg.0
		stloc.s		0
		ldloc.0
		ret
	}

	.method public hidebysig static int32 Test_STLOC_S__128(int32) cil managed {
		.maxstack	1
		.locals		init (string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, int32)
		ldarg.0
		stloc.s		0x80
		ldloc		0x80
		ret
	}

	.method public hidebysig static int32 Test_STLOC__0(int32) cil managed {
		.maxstack	1
		.locals		init (int32)
		ldarg.0
		stloc		0
		ldloc.0
		ret
	}

	.method public hidebysig static int32 Test_STLOC__128(int32) cil managed {
		.maxstack	1
		.locals		init (string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, string, int32)
		ldarg.0
		stloc		0x80
		ldloc		0x80
		ret
	}

	.method public hidebysig static void Test_NOP() cil managed {
		.maxstack	8
		nop
		ret
	}

	.method public hidebysig static native int Test_CONV_I__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_I1__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_I2__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_I4__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_I8__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_U__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_U1__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_U2__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_U4__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_U8__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.u8
		ret
	}

	.method public hidebysig static float32 Test_CONV_R4__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.r4
		ret
	}

	.method public hidebysig static float64 Test_CONV_R8__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.r8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I_UN__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i.un
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1_UN__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1.un
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2_UN__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2.un
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4_UN__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4.un
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8_UN__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8.un
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U_UN__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u.un
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1_UN__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1.un
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2_UN__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2.un
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4_UN__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4.un
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8_UN__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8.un
		ret
	}

	.method public hidebysig static float32 Test_CONV_R_UN__Boolean(bool) cil managed {
		.maxstack	8
		ldarg.0
		conv.r.un
		ret
	}

	.method public hidebysig static native int Test_CONV_I__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_I1__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_I2__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_I4__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_I8__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_U__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_U1__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_U2__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_U4__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_U8__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.u8
		ret
	}

	.method public hidebysig static float32 Test_CONV_R4__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.r4
		ret
	}

	.method public hidebysig static float64 Test_CONV_R8__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.r8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I_UN__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i.un
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1_UN__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1.un
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2_UN__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2.un
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4_UN__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4.un
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8_UN__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8.un
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U_UN__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u.un
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1_UN__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1.un
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2_UN__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2.un
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4_UN__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4.un
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8_UN__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8.un
		ret
	}

	.method public hidebysig static float32 Test_CONV_R_UN__Char(char) cil managed {
		.maxstack	8
		ldarg.0
		conv.r.un
		ret
	}

	.method public hidebysig static native int Test_CONV_I__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_I1__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_I2__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_I4__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_I8__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_U__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_U1__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_U2__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_U4__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_U8__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.u8
		ret
	}

	.method public hidebysig static float32 Test_CONV_R4__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.r4
		ret
	}

	.method public hidebysig static float64 Test_CONV_R8__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.r8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I_UN__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i.un
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1_UN__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1.un
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2_UN__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2.un
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4_UN__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4.un
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8_UN__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8.un
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U_UN__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u.un
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1_UN__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1.un
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2_UN__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2.un
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4_UN__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4.un
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8_UN__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8.un
		ret
	}

	.method public hidebysig static float32 Test_CONV_R_UN__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		conv.r.un
		ret
	}

	.method public hidebysig static native int Test_CONV_I__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_I1__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_I2__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_I4__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_I8__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_U__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_U1__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_U2__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_U4__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_U8__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.u8
		ret
	}

	.method public hidebysig static float32 Test_CONV_R4__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.r4
		ret
	}

	.method public hidebysig static float64 Test_CONV_R8__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.r8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I_UN__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i.un
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1_UN__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1.un
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2_UN__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2.un
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4_UN__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4.un
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8_UN__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8.un
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U_UN__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u.un
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1_UN__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1.un
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2_UN__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2.un
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4_UN__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4.un
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8_UN__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8.un
		ret
	}

	.method public hidebysig static float32 Test_CONV_R_UN__SByte(int8) cil managed {
		.maxstack	8
		ldarg.0
		conv.r.un
		ret
	}

	.method public hidebysig static native int Test_CONV_I__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_I1__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_I2__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_I4__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_I8__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_U__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_U1__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_U2__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_U4__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_U8__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.u8
		ret
	}

	.method public hidebysig static float32 Test_CONV_R4__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.r4
		ret
	}

	.method public hidebysig static float64 Test_CONV_R8__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.r8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I_UN__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i.un
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1_UN__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1.un
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2_UN__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2.un
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4_UN__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4.un
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8_UN__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8.un
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U_UN__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u.un
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1_UN__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1.un
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2_UN__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2.un
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4_UN__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4.un
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8_UN__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8.un
		ret
	}

	.method public hidebysig static float32 Test_CONV_R_UN__Int16(int16) cil managed {
		.maxstack	8
		ldarg.0
		conv.r.un
		ret
	}

	.method public hidebysig static native int Test_CONV_I__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_I1__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_I2__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_I4__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_I8__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_U__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_U1__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_U2__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_U4__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_U8__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.u8
		ret
	}

	.method public hidebysig static float32 Test_CONV_R4__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.r4
		ret
	}

	.method public hidebysig static float64 Test_CONV_R8__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.r8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I_UN__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i.un
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1_UN__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1.un
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2_UN__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2.un
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4_UN__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4.un
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8_UN__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8.un
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U_UN__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u.un
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1_UN__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1.un
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2_UN__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2.un
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4_UN__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4.un
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8_UN__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8.un
		ret
	}

	.method public hidebysig static float32 Test_CONV_R_UN__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		conv.r.un
		ret
	}

	.method public hidebysig static native int Test_CONV_I__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_I1__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_I2__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_I4__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_I8__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_U__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_U1__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_U2__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_U4__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_U8__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.u8
		ret
	}

	.method public hidebysig static float32 Test_CONV_R4__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.r4
		ret
	}

	.method public hidebysig static float64 Test_CONV_R8__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.r8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I_UN__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i.un
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1_UN__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1.un
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2_UN__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2.un
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4_UN__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4.un
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8_UN__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8.un
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U_UN__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u.un
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1_UN__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1.un
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2_UN__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2.un
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4_UN__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4.un
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8_UN__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8.un
		ret
	}

	.method public hidebysig static float32 Test_CONV_R_UN__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		conv.r.un
		ret
	}

	.method public hidebysig static native int Test_CONV_I__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_I1__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_I2__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_I4__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_I8__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_U__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_U1__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_U2__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_U4__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_U8__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.u8
		ret
	}

	.method public hidebysig static float32 Test_CONV_R4__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.r4
		ret
	}

	.method public hidebysig static float64 Test_CONV_R8__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.r8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I_UN__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i.un
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1_UN__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1.un
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2_UN__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2.un
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4_UN__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4.un
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8_UN__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8.un
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U_UN__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u.un
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1_UN__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1.un
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2_UN__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2.un
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4_UN__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4.un
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8_UN__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8.un
		ret
	}

	.method public hidebysig static float32 Test_CONV_R_UN__UIntPtr(native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		conv.r.un
		ret
	}

	.method public hidebysig static native int Test_CONV_I__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_I1__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_I2__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_I4__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_I8__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_U__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_U1__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_U2__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_U4__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_U8__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.u8
		ret
	}

	.method public hidebysig static float32 Test_CONV_R4__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.r4
		ret
	}

	.method public hidebysig static float64 Test_CONV_R8__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.r8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I_UN__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i.un
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1_UN__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1.un
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2_UN__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2.un
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4_UN__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4.un
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8_UN__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8.un
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U_UN__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u.un
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1_UN__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1.un
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2_UN__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2.un
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4_UN__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4.un
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8_UN__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8.un
		ret
	}

	.method public hidebysig static float32 Test_CONV_R_UN__Byte(uint8) cil managed {
		.maxstack	8
		ldarg.0
		conv.r.un
		ret
	}

	.method public hidebysig static native int Test_CONV_I__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_I1__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_I2__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_I4__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_I8__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_U__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_U1__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_U2__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_U4__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_U8__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.u8
		ret
	}

	.method public hidebysig static float32 Test_CONV_R4__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.r4
		ret
	}

	.method public hidebysig static float64 Test_CONV_R8__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.r8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I_UN__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i.un
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1_UN__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1.un
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2_UN__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2.un
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4_UN__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4.un
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8_UN__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8.un
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U_UN__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u.un
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1_UN__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1.un
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2_UN__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2.un
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4_UN__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4.un
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8_UN__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8.un
		ret
	}

	.method public hidebysig static float32 Test_CONV_R_UN__UInt16(uint16) cil managed {
		.maxstack	8
		ldarg.0
		conv.r.un
		ret
	}

	.method public hidebysig static native int Test_CONV_I__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_I1__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_I2__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_I4__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_I8__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_U__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_U1__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_U2__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_U4__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_U8__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.u8
		ret
	}

	.method public hidebysig static float32 Test_CONV_R4__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.r4
		ret
	}

	.method public hidebysig static float64 Test_CONV_R8__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.r8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I_UN__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i.un
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1_UN__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1.un
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2_UN__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2.un
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4_UN__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4.un
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8_UN__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8.un
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U_UN__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u.un
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1_UN__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1.un
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2_UN__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2.un
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4_UN__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4.un
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8_UN__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8.un
		ret
	}

	.method public hidebysig static float32 Test_CONV_R_UN__UInt32(uint32) cil managed {
		.maxstack	8
		ldarg.0
		conv.r.un
		ret
	}

	.method public hidebysig static native int Test_CONV_I__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_I1__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_I2__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_I4__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_I8__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_U__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_U1__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_U2__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_U4__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_U8__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.u8
		ret
	}

	.method public hidebysig static float32 Test_CONV_R4__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.r4
		ret
	}

	.method public hidebysig static float64 Test_CONV_R8__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.r8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I_UN__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i.un
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1_UN__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1.un
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2_UN__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2.un
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4_UN__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4.un
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8_UN__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8.un
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U_UN__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u.un
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1_UN__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1.un
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2_UN__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2.un
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4_UN__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4.un
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8_UN__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8.un
		ret
	}

	.method public hidebysig static float32 Test_CONV_R_UN__UInt64(uint64) cil managed {
		.maxstack	8
		ldarg.0
		conv.r.un
		ret
	}

	.method public hidebysig static native int Test_CONV_I__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_I1__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_I2__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_I4__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_I8__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_U__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_U1__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_U2__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_U4__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_U8__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.u8
		ret
	}

	.method public hidebysig static float32 Test_CONV_R4__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.r4
		ret
	}

	.method public hidebysig static float64 Test_CONV_R8__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.r8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I_UN__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i.un
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1_UN__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1.un
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2_UN__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2.un
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4_UN__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4.un
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8_UN__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8.un
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U_UN__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u.un
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1_UN__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1.un
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2_UN__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2.un
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4_UN__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4.un
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8_UN__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8.un
		ret
	}

	.method public hidebysig static float32 Test_CONV_R_UN__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		conv.r.un
		ret
	}

	.method public hidebysig static native int Test_CONV_I__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_I1__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_I2__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_I4__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_I8__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_U__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_U1__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_U2__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_U4__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_U8__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.u8
		ret
	}

	.method public hidebysig static float32 Test_CONV_R4__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.r4
		ret
	}

	.method public hidebysig static float64 Test_CONV_R8__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.r8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8
		ret
	}

	.method public hidebysig static native int Test_CONV_OVF_I_UN__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i.un
		ret
	}

	.method public hidebysig static int8 Test_CONV_OVF_I1_UN__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i1.un
		ret
	}

	.method public hidebysig static int16 Test_CONV_OVF_I2_UN__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i2.un
		ret
	}

	.method public hidebysig static int32 Test_CONV_OVF_I4_UN__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i4.un
		ret
	}

	.method public hidebysig static int64 Test_CONV_OVF_I8_UN__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.i8.un
		ret
	}

	.method public hidebysig static native unsigned int Test_CONV_OVF_U_UN__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u.un
		ret
	}

	.method public hidebysig static uint8 Test_CONV_OVF_U1_UN__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u1.un
		ret
	}

	.method public hidebysig static uint16 Test_CONV_OVF_U2_UN__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u2.un
		ret
	}

	.method public hidebysig static uint32 Test_CONV_OVF_U4_UN__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u4.un
		ret
	}

	.method public hidebysig static uint64 Test_CONV_OVF_U8_UN__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.ovf.u8.un
		ret
	}

	.method public hidebysig static float32 Test_CONV_R_UN__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		conv.r.un
		ret
	}

	.method public hidebysig static int32 Test_CEQ__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ceq
		ret
	}

	.method public hidebysig static int32 Test_CEQ__UInt32(uint32, uint32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ceq
		ret
	}

	.method public hidebysig static int32 Test_CEQ__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ceq
		ret
	}

	.method public hidebysig static int32 Test_CEQ__UInt64(uint64, uint64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ceq
		ret
	}

	.method public hidebysig static int32 Test_CEQ__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ceq
		ret
	}

	.method public hidebysig static int32 Test_CEQ__UIntPtr(native unsigned int, native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ceq
		ret
	}

	.method public hidebysig static int32 Test_CEQ__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ceq
		ret
	}

	.method public hidebysig static int32 Test_CEQ__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ceq
		ret
	}

	.method public hidebysig static int32 Test_CEQ__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ceq
		ret
	}

	.method public hidebysig static int32 Test_CEQ__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ceq
		ret
	}

	.method public hidebysig static int32 Test_CEQ__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ceq
		ret
	}

	.method public hidebysig static int32 Test_CGT__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt
		ret
	}

	.method public hidebysig static int32 Test_CGT__UInt32(uint32, uint32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt
		ret
	}

	.method public hidebysig static int32 Test_CGT__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt
		ret
	}

	.method public hidebysig static int32 Test_CGT__UInt64(uint64, uint64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt
		ret
	}

	.method public hidebysig static int32 Test_CGT__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt
		ret
	}

	.method public hidebysig static int32 Test_CGT__UIntPtr(native unsigned int, native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt
		ret
	}

	.method public hidebysig static int32 Test_CGT__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt
		ret
	}

	.method public hidebysig static int32 Test_CGT__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt
		ret
	}

	.method public hidebysig static int32 Test_CGT__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt
		ret
	}

	.method public hidebysig static int32 Test_CGT__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt
		ret
	}

	.method public hidebysig static int32 Test_CLT__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt
		ret
	}

	.method public hidebysig static int32 Test_CLT__UInt32(uint32, uint32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt
		ret
	}

	.method public hidebysig static int32 Test_CLT__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt
		ret
	}

	.method public hidebysig static int32 Test_CLT__UInt64(uint64, uint64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt
		ret
	}

	.method public hidebysig static int32 Test_CLT__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt
		ret
	}

	.method public hidebysig static int32 Test_CLT__UIntPtr(native unsigned int, native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt
		ret
	}

	.method public hidebysig static int32 Test_CLT__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt
		ret
	}

	.method public hidebysig static int32 Test_CLT__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt
		ret
	}

	.method public hidebysig static int32 Test_CLT__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt
		ret
	}

	.method public hidebysig static int32 Test_CLT__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt
		ret
	}

	.method public hidebysig static int32 Test_CGT_UN__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt.un
		ret
	}

	.method public hidebysig static int32 Test_CGT_UN__UInt32(uint32, uint32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt.un
		ret
	}

	.method public hidebysig static int32 Test_CGT_UN__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt.un
		ret
	}

	.method public hidebysig static int32 Test_CGT_UN__UInt64(uint64, uint64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt.un
		ret
	}

	.method public hidebysig static int32 Test_CGT_UN__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt.un
		ret
	}

	.method public hidebysig static int32 Test_CGT_UN__UIntPtr(native unsigned int, native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt.un
		ret
	}

	.method public hidebysig static int32 Test_CGT_UN__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt.un
		ret
	}

	.method public hidebysig static int32 Test_CGT_UN__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt.un
		ret
	}

	.method public hidebysig static int32 Test_CGT_UN__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt.un
		ret
	}

	.method public hidebysig static int32 Test_CGT_UN__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		cgt.un
		ret
	}

	.method public hidebysig static int32 Test_CLT_UN__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt.un
		ret
	}

	.method public hidebysig static int32 Test_CLT_UN__UInt32(uint32, uint32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt.un
		ret
	}

	.method public hidebysig static int32 Test_CLT_UN__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt.un
		ret
	}

	.method public hidebysig static int32 Test_CLT_UN__UInt64(uint64, uint64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt.un
		ret
	}

	.method public hidebysig static int32 Test_CLT_UN__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt.un
		ret
	}

	.method public hidebysig static int32 Test_CLT_UN__UIntPtr(native unsigned int, native unsigned int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt.un
		ret
	}

	.method public hidebysig static int32 Test_CLT_UN__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt.un
		ret
	}

	.method public hidebysig static int32 Test_CLT_UN__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt.un
		ret
	}

	.method public hidebysig static int32 Test_CLT_UN__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt.un
		ret
	}

	.method public hidebysig static int32 Test_CLT_UN__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		clt.un
		ret
	}

	.method public hidebysig static int32 Test_BR() cil managed {
		.maxstack	8
		br			f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		br		b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BR_S() cil managed {
		.maxstack	8
		br.s		f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		br.s	b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRFALSE() cil managed {
		.maxstack	8
		ldc.i4.0
		brfalse		f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.0
		brfalse		b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRFALSE_S() cil managed {
		.maxstack	8
		ldc.i4.0
		brfalse.s	f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.0
		brfalse.s	b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRTRUE() cil managed {
		.maxstack	8
		ldc.i4.1
		brtrue		f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.1
		brtrue		b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRTRUE_S() cil managed {
		.maxstack	8
		ldc.i4.1
		brtrue.s	f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.1
		brtrue.s	b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BEQ() cil managed {
		.maxstack	8
		ldc.i4.1
		ldc.i4.1
		beq			f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.1
		ldc.i4.1
		beq		b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BEQ_S() cil managed {
		.maxstack	8
		ldc.i4.1
		ldc.i4.1
		beq.s		f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.1
		ldc.i4.1
		beq.s		b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BNE_UN() cil managed {
		.maxstack	8
		ldc.i4.0
		ldc.i4.1
		bne.un		f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.0
		ldc.i4.1
		bne.un		b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BNE_UN_S() cil managed {
		.maxstack	8
		ldc.i4.0
		ldc.i4.1
		bne.un.s	f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.0
		ldc.i4.1
		bne.un.s	b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BGE() cil managed {
		.maxstack	8
		ldc.i4.1
		ldc.i4.0
		bge			f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.1
		ldc.i4.0
		bge			b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BGE_S() cil managed {
		.maxstack	8
		ldc.i4.1
		ldc.i4.0
		bge.s		f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.1
		ldc.i4.0
		bge.s		b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BGE_UN() cil managed {
		.maxstack	8
		ldc.i4.m1
		ldc.i4.0
		bge.un		f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.m1
		ldc.i4.0
		bge.un		b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BGE_UN_S() cil managed {
		.maxstack	8
		ldc.i4.m1
		ldc.i4.0
		bge.un.s	f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.m1
		ldc.i4.0
		bge.un.s	b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BGT() cil managed {
		.maxstack	8
		ldc.i4.1
		ldc.i4.0
		bgt			f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.1
		ldc.i4.0
		bgt			b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BGT_S() cil managed {
		.maxstack	8
		ldc.i4.1
		ldc.i4.0
		bgt.s		f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.1
		ldc.i4.0
		bgt.s		b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BGT_UN() cil managed {
		.maxstack	8
		ldc.i4.m1
		ldc.i4.1
		bgt.un		f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.m1
		ldc.i4.1
		bgt.un		b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BGT_UN_S() cil managed {
		.maxstack	8
		ldc.i4.m1
		ldc.i4.1
		bgt.un.s	f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.m1
		ldc.i4.1
		bgt.un.s	b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BLE() cil managed {
		.maxstack	8
		ldc.i4.0
		ldc.i4.1
		ble			f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.0
		ldc.i4.1
		ble			b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BLE_S() cil managed {
		.maxstack	8
		ldc.i4.0
		ldc.i4.1
		ble.s		f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.0
		ldc.i4.1
		ble.s		b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BLE_UN() cil managed {
		.maxstack	8
		ldc.i4.1
		ldc.i4.m1
		ble.un		f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.1
		ldc.i4.m1
		ble.un		b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BLE_UN_S() cil managed {
		.maxstack	8
		ldc.i4.1
		ldc.i4.m1
		ble.un.s	f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.1
		ldc.i4.m1
		ble.un.s	b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BLT() cil managed {
		.maxstack	8
		ldc.i4.0
		ldc.i4.1
		blt			f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.0
		ldc.i4.1
		blt			b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BLT_S() cil managed {
		.maxstack	8
		ldc.i4.0
		ldc.i4.1
		blt.s		f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.0
		ldc.i4.1
		blt.s		b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BLT_UN() cil managed {
		.maxstack	8
		ldc.i4.0
		ldc.i4.m1
		blt.un		f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.0
		ldc.i4.m1
		blt.un		b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BLT_UN_S() cil managed {
		.maxstack	8
		ldc.i4.0
		ldc.i4.m1
		blt.un.s	f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		ldc.i4.0
		ldc.i4.m1
		blt.un.s	b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRFALSE__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		brfalse		there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRFALSE_S__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		brfalse.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRFALSE__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		brfalse		there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRFALSE_S__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		brfalse.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRFALSE__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		brfalse		there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRFALSE_S__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		brfalse.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRFALSE__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		brfalse		there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRFALSE_S__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		brfalse.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRFALSE__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		brfalse		there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRFALSE_S__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		brfalse.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRFALSE__Object(object) cil managed {
		.maxstack	8
		ldarg.0
		brfalse		there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRFALSE_S__Object(object) cil managed {
		.maxstack	8
		ldarg.0
		brfalse.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRTRUE__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		brtrue		there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRTRUE_S__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		brtrue.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRTRUE__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		brtrue		there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRTRUE_S__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		brtrue.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRTRUE__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		brtrue		there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRTRUE_S__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		brtrue.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRTRUE__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		brtrue		there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRTRUE_S__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		brtrue.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRTRUE__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		brtrue		there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRTRUE_S__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		brtrue.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRTRUE__Object(object) cil managed {
		.maxstack	8
		ldarg.0
		brtrue		there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BRTRUE_S__Object(object) cil managed {
		.maxstack	8
		ldarg.0
		brtrue.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_BEQ__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		beq		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BEQ_S__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		beq.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BEQ__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		beq		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BEQ_S__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		beq.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BEQ__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		beq		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BEQ_S__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		beq.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BEQ__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		beq		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BEQ_S__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		beq.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BEQ__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		beq		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BEQ_S__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		beq.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BEQ__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		beq		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BEQ_S__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		beq.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BNE_UN__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bne.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BNE_UN_S__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bne.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BNE_UN__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bne.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BNE_UN_S__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bne.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BNE_UN__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bne.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BNE_UN_S__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bne.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BNE_UN__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bne.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BNE_UN_S__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bne.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BNE_UN__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bne.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BNE_UN_S__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bne.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BNE_UN__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bne.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BNE_UN_S__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bne.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_S__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_S__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_S__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_S__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_S__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_S__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_UN__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_UN_S__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_UN__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_UN_S__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_UN__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_UN_S__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_UN__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_UN_S__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_UN__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_UN_S__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_UN__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGE_UN_S__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bge.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_S__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_S__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_S__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_S__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_S__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_S__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_UN__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_UN_S__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_UN__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_UN_S__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_UN__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_UN_S__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_UN__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_UN_S__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_UN__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_UN_S__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_UN__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BGT_UN_S__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		bgt.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_S__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_S__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_S__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_S__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_S__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_S__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_UN__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_UN_S__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_UN__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_UN_S__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_UN__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_UN_S__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_UN__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_UN_S__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_UN__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_UN_S__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_UN__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLE_UN_S__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		ble.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_S__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_S__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_S__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_S__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_S__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_S__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_UN__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_UN_S__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_UN__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_UN_S__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_UN__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_UN_S__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_UN__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_UN_S__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_UN__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_UN_S__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_UN__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.un		there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_BLT_UN_S__Object(object, object) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		blt.un.s	there
		ldc.i4.0
		ret
there:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LEAVE() cil managed {
		.maxstack	8
		leave		f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		leave		b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_LEAVE_S() cil managed {
		.maxstack	8
		leave.s		f
		ldc.i4.0
		ret
b:
		ldc.i4.8
		ret
f:
		leave.s		b
		ldc.i4.1
		ret
	}

	.method public hidebysig static int32 Test_LEAVE__0() cil managed {
		.maxstack	8
		ldc.i4.1
		leave		f1
		ldc.i4.0
		ret
f1:
		ldc.i4.1
		ldc.i4.1
		leave		f2
		ldc.i4.1
		ret
f2:
		ldc.i4.1
		ldc.i4.1
		ldc.i4.1
		leave		f3
		ldc.i4.2
		ret
f3:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LEAVE_S__0() cil managed {
		.maxstack	8
		ldc.i4.1
		leave.s		f1
		ldc.i4.0
		ret
f1:
		ldc.i4.1
		ldc.i4.1
		leave.s		f2
		ldc.i4.1
		ret
f2:
		ldc.i4.1
		ldc.i4.1
		ldc.i4.1
		leave.s		f3
		ldc.i4.2
		ret
f3:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_SWITCH__0(int32) cil managed {
		.maxstack	8
		ldarg.0
		switch		()
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_SWITCH__1(int32) cil managed {
		.maxstack	8
		ldarg.0
		switch		(sw0)
		ldc.i4.8
		ret
sw0:
		ldc.i4.0
		ret
	}

	.method public hidebysig static int32 Test_SWITCH__2(int32) cil managed {
		.maxstack	8
		ldarg.0
		switch		(sw0, sw1)
		ldc.i4.8
		ret
sw1:
		ldc.i4.1
		ret
sw0:
		ldc.i4.0
		ret
	}

	.method public hidebysig static int32 Test_SWITCH__3(int32) cil managed {
		.maxstack	8
		ldarg.0
		switch		(sw0, sw1, sw2)
		ldc.i4.8
		ret
sw1:
		ldc.i4.1
		ret
sw0:
		ldc.i4.0
		ret
sw2:
		ldc.i4.2
		ret
	}

	.method public hidebysig static int32 Test_SWITCH__4(int32) cil managed {
		.maxstack	8
		br		start
sw3:
		ldc.i4.3
		ret
start:
		ldarg.0
		switch		(sw0, sw1, sw2, sw3)
		ldc.i4.8
		ret
sw1:
		ldc.i4.1
		ret
sw0:
		ldc.i4.0
		ret
sw2:
		ldc.i4.2
		ret
	}

	.method public hidebysig static int32 Test_SWITCH__5(int32) cil managed {
		.maxstack	8
		br		start
sw3:
		ldc.i4.3
		ret
start:
		ldarg.0
		switch		(sw0, sw1, sw2, sw3, sw4)
sw4:
		ldc.i4.8
		ret
sw1:
		ldc.i4.1
		ret
sw0:
		ldc.i4.0
		ret
sw2:
		ldc.i4.2
		ret
	}

	.method public hidebysig static int32 Test_SWITCH__6(int32) cil managed {
		.maxstack	8
		br		start
sw3:
		ldc.i4.3
		ret
start:
		ldarg.0
		switch		(sw0, sw1, sw2, sw3, sw4, sw5)
sw4:
		ldc.i4.8
		ret
sw1:
		ldc.i4.1
		ret
sw0:
		ldc.i4.0
		ret
sw5:
sw2:
		ldc.i4.2
		ret
	}

	.method public hidebysig static int32 Test_SWITCH__6_IntPtr(native int) cil managed {
		.maxstack	8
		br		start
sw3:
		ldc.i4.3
		ret
start:
		ldarg.0
		switch		(sw0, sw1, sw2, sw3, sw4, sw5)
sw4:
		ldc.i4.8
		ret
sw1:
		ldc.i4.1
		ret
sw0:
		ldc.i4.0
		ret
sw5:
sw2:
		ldc.i4.2
		ret
	}

	.method public hidebysig static int32 Test_SWITCH__6_Int64(int64) cil managed {
		.maxstack	8
		br		start
sw3:
		ldc.i4.3
		ret
start:
		ldarg.0
		switch		(sw0, sw1, sw2, sw3, sw4, sw5)
sw4:
		ldc.i4.8
		ret
sw1:
		ldc.i4.1
		ret
sw0:
		ldc.i4.0
		ret
sw5:
sw2:
		ldc.i4.2
		ret
	}

	.method public hidebysig static int32 Test_SWITCH__6_Single(float32) cil managed {
		.maxstack	8
		br		start
sw3:
		ldc.i4.3
		ret
start:
		ldarg.0
		switch		(sw0, sw1, sw2, sw3, sw4, sw5)
sw4:
		ldc.i4.8
		ret
sw1:
		ldc.i4.1
		ret
sw0:
		ldc.i4.0
		ret
sw5:
sw2:
		ldc.i4.2
		ret
	}

	.method public hidebysig static int32 Test_SWITCH__6_Double(float64) cil managed {
		.maxstack	8
		br		start
sw3:
		ldc.i4.3
		ret
start:
		ldarg.0
		switch		(sw0, sw1, sw2, sw3, sw4, sw5)
sw4:
		ldc.i4.8
		ret
sw1:
		ldc.i4.1
		ret
sw0:
		ldc.i4.0
		ret
sw5:
sw2:
		ldc.i4.2
		ret
	}

	.method public hidebysig static int32 Test_POP() cil managed {
		.maxstack	8
		ldc.i4.0
		pop
		ldnull
		pop
		ldc.r8		1
		pop
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_DUP(string, object) cil managed {
		.maxstack	8
		ldc.i4.0
		dup
		beq.s		f1
		ldc.i4.0
		ret
f1:

		ldc.i4.1
		dup
		beq.s		f2
		ldc.i4.1
		ret
f2:

		ldnull
		dup
		beq.s		f3
		ldc.i4.2
		ret
f3:

		ldarg.0
		dup
		beq.s		f4
		ldc.i4.3
		ret
f4:

		ldarg.1
		dup
		beq.s		f5
		ldc.i4.4
		ret
f5:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_AND__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		and
		ret
	}

	.method public hidebysig static int64 Test_AND__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		and
		ret
	}

	.method public hidebysig static native int Test_AND__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		and
		ret
	}

	.method public hidebysig static int32 Test_OR__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		or
		ret
	}

	.method public hidebysig static int64 Test_OR__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		or
		ret
	}

	.method public hidebysig static native int Test_OR__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		or
		ret
	}

	.method public hidebysig static int32 Test_XOR__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		xor
		ret
	}

	.method public hidebysig static int64 Test_XOR__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		xor
		ret
	}

	.method public hidebysig static native int Test_XOR__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		xor
		ret
	}

	.method public hidebysig static int32 Test_SHL__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		shl
		ret
	}

	.method public hidebysig static int64 Test_SHL__Int64(int64, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		shl
		ret
	}

	.method public hidebysig static native int Test_SHL__IntPtr(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		shl
		ret
	}

	.method public hidebysig static int32 Test_SHR__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		shr
		ret
	}

	.method public hidebysig static int64 Test_SHR__Int64(int64, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		shr
		ret
	}

	.method public hidebysig static native int Test_SHR__IntPtr(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		shr
		ret
	}

	.method public hidebysig static int32 Test_SHR_UN__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		shr.un
		ret
	}

	.method public hidebysig static int64 Test_SHR_UN__Int64(int64, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		shr.un
		ret
	}

	.method public hidebysig static native int Test_SHR_UN__IntPtr(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		shr.un
		ret
	}

	.method public hidebysig static int32 Test_NOT__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		not
		ret
	}

	.method public hidebysig static int64 Test_NOT__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		not
		ret
	}

	.method public hidebysig static native int Test_NOT__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		not
		ret
	}

	.method public hidebysig static int32 Test_NEG__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		neg
		ret
	}

	.method public hidebysig static int64 Test_NEG__Int64(int64) cil managed {
		.maxstack	8
		ldarg.0
		neg
		ret
	}

	.method public hidebysig static native int Test_NEG__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		neg
		ret
	}

	.method public hidebysig static float32 Test_NEG__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		neg
		ret
	}

	.method public hidebysig static float64 Test_NEG__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		neg
		ret
	}

	.method public hidebysig static int32 Test_ADD__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add
		ret
	}

	.method public hidebysig static int64 Test_ADD__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add
		ret
	}

	.method public hidebysig static native int Test_ADD__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add
		ret
	}

	.method public hidebysig static native int Test_ADD__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add
		ret
	}

	.method public hidebysig static native int Test_ADD__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add
		ret
	}

	.method public hidebysig static float32 Test_ADD__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add
		ret
	}

	.method public hidebysig static float64 Test_ADD__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add
		ret
	}

	.method public hidebysig static int32 Test_ADD_OVF__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add.ovf
		ret
	}

	.method public hidebysig static int64 Test_ADD_OVF__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add.ovf
		ret
	}

	.method public hidebysig static native int Test_ADD_OVF__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add.ovf
		ret
	}

	.method public hidebysig static native int Test_ADD_OVF__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add.ovf
		ret
	}

	.method public hidebysig static native int Test_ADD_OVF__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add.ovf
		ret
	}

	.method public hidebysig static float32 Test_ADD_OVF__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add.ovf
		ret
	}

	.method public hidebysig static float64 Test_ADD_OVF__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add.ovf
		ret
	}

	.method public hidebysig static int32 Test_ADD_OVF_UN__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add.ovf.un
		ret
	}

	.method public hidebysig static int64 Test_ADD_OVF_UN__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add.ovf.un
		ret
	}

	.method public hidebysig static native int Test_ADD_OVF_UN__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add.ovf.un
		ret
	}

	.method public hidebysig static native int Test_ADD_OVF_UN__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add.ovf.un
		ret
	}

	.method public hidebysig static native int Test_ADD_OVF_UN__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add.ovf.un
		ret
	}

	.method public hidebysig static float32 Test_ADD_OVF_UN__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add.ovf.un
		ret
	}

	.method public hidebysig static float64 Test_ADD_OVF_UN__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		add.ovf.un
		ret
	}

	.method public hidebysig static int32 Test_SUB__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub
		ret
	}

	.method public hidebysig static int64 Test_SUB__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub
		ret
	}

	.method public hidebysig static native int Test_SUB__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub
		ret
	}

	.method public hidebysig static native int Test_SUB__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub
		ret
	}

	.method public hidebysig static native int Test_SUB__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub
		ret
	}

	.method public hidebysig static float32 Test_SUB__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub
		ret
	}

	.method public hidebysig static float64 Test_SUB__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub
		ret
	}

	.method public hidebysig static int32 Test_SUB_OVF__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub.ovf
		ret
	}

	.method public hidebysig static int64 Test_SUB_OVF__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub.ovf
		ret
	}

	.method public hidebysig static native int Test_SUB_OVF__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub.ovf
		ret
	}

	.method public hidebysig static native int Test_SUB_OVF__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub.ovf
		ret
	}

	.method public hidebysig static native int Test_SUB_OVF__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub.ovf
		ret
	}

	.method public hidebysig static float32 Test_SUB_OVF__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub.ovf
		ret
	}

	.method public hidebysig static float64 Test_SUB_OVF__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub.ovf
		ret
	}

	.method public hidebysig static int32 Test_SUB_OVF_UN__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub.ovf.un
		ret
	}

	.method public hidebysig static int64 Test_SUB_OVF_UN__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub.ovf.un
		ret
	}

	.method public hidebysig static native int Test_SUB_OVF_UN__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub.ovf.un
		ret
	}

	.method public hidebysig static native int Test_SUB_OVF_UN__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub.ovf.un
		ret
	}

	.method public hidebysig static native int Test_SUB_OVF_UN__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub.ovf.un
		ret
	}

	.method public hidebysig static float32 Test_SUB_OVF_UN__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub.ovf.un
		ret
	}

	.method public hidebysig static float64 Test_SUB_OVF_UN__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		sub.ovf.un
		ret
	}

	.method public hidebysig static int32 Test_MUL__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul
		ret
	}

	.method public hidebysig static int64 Test_MUL__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul
		ret
	}

	.method public hidebysig static native int Test_MUL__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul
		ret
	}

	.method public hidebysig static native int Test_MUL__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul
		ret
	}

	.method public hidebysig static native int Test_MUL__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul
		ret
	}

	.method public hidebysig static float32 Test_MUL__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul
		ret
	}

	.method public hidebysig static float64 Test_MUL__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul
		ret
	}

	.method public hidebysig static int32 Test_MUL_OVF__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul.ovf
		ret
	}

	.method public hidebysig static int64 Test_MUL_OVF__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul.ovf
		ret
	}

	.method public hidebysig static native int Test_MUL_OVF__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul.ovf
		ret
	}

	.method public hidebysig static native int Test_MUL_OVF__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul.ovf
		ret
	}

	.method public hidebysig static native int Test_MUL_OVF__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul.ovf
		ret
	}

	.method public hidebysig static float32 Test_MUL_OVF__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul.ovf
		ret
	}

	.method public hidebysig static float64 Test_MUL_OVF__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul.ovf
		ret
	}

	.method public hidebysig static int32 Test_MUL_OVF_UN__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul.ovf.un
		ret
	}

	.method public hidebysig static int64 Test_MUL_OVF_UN__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul.ovf.un
		ret
	}

	.method public hidebysig static native int Test_MUL_OVF_UN__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul.ovf.un
		ret
	}

	.method public hidebysig static native int Test_MUL_OVF_UN__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul.ovf.un
		ret
	}

	.method public hidebysig static native int Test_MUL_OVF_UN__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul.ovf.un
		ret
	}

	.method public hidebysig static float32 Test_MUL_OVF_UN__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul.ovf.un
		ret
	}

	.method public hidebysig static float64 Test_MUL_OVF_UN__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		mul.ovf.un
		ret
	}

	.method public hidebysig static int32 Test_DIV__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		div
		ret
	}

	.method public hidebysig static int64 Test_DIV__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		div
		ret
	}

	.method public hidebysig static native int Test_DIV__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		div
		ret
	}

	.method public hidebysig static native int Test_DIV__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		div
		ret
	}

	.method public hidebysig static native int Test_DIV__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		div
		ret
	}

	.method public hidebysig static float32 Test_DIV__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		div
		ret
	}

	.method public hidebysig static float64 Test_DIV__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		div
		ret
	}

	.method public hidebysig static int32 Test_DIV_UN__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		div.un
		ret
	}

	.method public hidebysig static int64 Test_DIV_UN__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		div.un
		ret
	}

	.method public hidebysig static native int Test_DIV_UN__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		div.un
		ret
	}

	.method public hidebysig static native int Test_DIV_UN__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		div.un
		ret
	}

	.method public hidebysig static native int Test_DIV_UN__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		div.un
		ret
	}

	.method public hidebysig static float32 Test_DIV_UN__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		div.un
		ret
	}

	.method public hidebysig static float64 Test_DIV_UN__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		div.un
		ret
	}

	.method public hidebysig static int32 Test_REM__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		rem
		ret
	}

	.method public hidebysig static int64 Test_REM__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		rem
		ret
	}

	.method public hidebysig static native int Test_REM__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		rem
		ret
	}

	.method public hidebysig static native int Test_REM__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		rem
		ret
	}

	.method public hidebysig static native int Test_REM__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		rem
		ret
	}

	.method public hidebysig static float32 Test_REM__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		rem
		ret
	}

	.method public hidebysig static float64 Test_REM__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		rem
		ret
	}

	.method public hidebysig static int32 Test_REM_UN__Int32(int32, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		rem.un
		ret
	}

	.method public hidebysig static int64 Test_REM_UN__Int64(int64, int64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		rem.un
		ret
	}

	.method public hidebysig static native int Test_REM_UN__IntPtr(native int, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		rem.un
		ret
	}

	.method public hidebysig static native int Test_REM_UN__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		rem.un
		ret
	}

	.method public hidebysig static native int Test_REM_UN__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		rem.un
		ret
	}

	.method public hidebysig static float32 Test_REM_UN__Single(float32, float32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		rem.un
		ret
	}

	.method public hidebysig static float64 Test_REM_UN__Double(float64, float64) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		rem.un
		ret
	}

	.method public hidebysig static int32 Test_BOX_UNBOX() cil managed {
		.maxstack	2
		.locals		init (string)

		ldc.i4.1
		box			[mscorlib]System.Boolean
		dup
		brtrue	f1
		pop
		ldc.i4	-2
		ret
f1:
		unbox.any	[mscorlib]System.Boolean
		ldc.i4.1
		beq		f2
		ldc.i4	-3
		ret
f2:

		ldc.i4.2
		box			[mscorlib]System.Char
		dup
		brtrue	f3
		pop
		ldc.i4	-4
		ret
f3:
		unbox.any	[mscorlib]System.Char
		ldc.i4.2
		beq		f4
		ldc.i4	-5
		ret
f4:

		ldc.i4.3
		box			[mscorlib]System.SByte
		dup
		brtrue	f5
		pop
		ldc.i4	-6
		ret
f5:
		unbox.any	[mscorlib]System.SByte
		ldc.i4.3
		beq		f6
		ldc.i4	-7
		ret
f6:

		ldc.i4.4
		box			[mscorlib]System.Int16
		dup
		brtrue	f7
		pop
		ldc.i4	-8
		ret
f7:
		unbox.any	[mscorlib]System.Int16
		ldc.i4.4
		beq		f8
		ldc.i4	-9
		ret
f8:

		ldc.i4.5
		box			[mscorlib]System.Int32
		dup
		brtrue	f9
		pop
		ldc.i4	-10
		ret
f9:
		unbox.any	[mscorlib]System.Int32
		ldc.i4.5
		beq		f10
		ldc.i4	-11
		ret
f10:

		ldc.i4.6
		conv.i8
		box			[mscorlib]System.Int64
		dup
		brtrue	f11
		pop
		ldc.i4	-12
		ret
f11:
		unbox.any	[mscorlib]System.Int64
		ldc.i4.6
		conv.i8
		beq		f12
		ldc.i4	-13
		ret
f12:

		ldc.i4.7
		box			[mscorlib]System.Byte
		dup
		brtrue	f13
		pop
		ldc.i4	-14
		ret
f13:
		unbox.any	[mscorlib]System.Byte
		ldc.i4.7
		beq		f14
		ldc.i4	-15
		ret
f14:

		ldc.i4.8
		box			[mscorlib]System.UInt16
		dup
		brtrue	f15
		pop
		ldc.i4	-16
		ret
f15:
		unbox.any	[mscorlib]System.UInt16
		ldc.i4.8
		beq		f16
		ldc.i4	-17
		ret
f16:

		ldc.i4	9
		box			[mscorlib]System.UInt32
		dup
		brtrue	f17
		pop
		ldc.i4	-18
		ret
f17:
		unbox.any	[mscorlib]System.UInt32
		ldc.i4	9
		beq		f18
		ldc.i4	-19
		ret
f18:

		ldc.i4	10
		conv.u8
		box			[mscorlib]System.UInt64
		dup
		brtrue	f19
		pop
		ldc.i4	-20
		ret
f19:
		unbox.any	[mscorlib]System.UInt64
		ldc.i4	10
		conv.u8
		beq		f20
		ldc.i4	-21
		ret
f20:

		ldc.r4	123.456
		box			[mscorlib]System.Single
		dup
		brtrue	f21
		pop
		ldc.i4	-22
		ret
f21:
		unbox.any	[mscorlib]System.Single
		ldc.r4	123.456
		beq		f22
		ldc.i4	-23
		ret
f22:

		ldc.r8	123.456
		box			[mscorlib]System.Double
		dup
		brtrue	f23
		pop
		ldc.i4	-24
		ret
f23:
		unbox.any	[mscorlib]System.Double
		ldc.r8	123.456
		beq		f24
		ldc.i4	-25
		ret
f24:

		ldstr	"hello"
		stloc.0
		ldloc.0
		box			[mscorlib]System.String
		dup
		brtrue	f25
		pop
		ldc.i4	-26
		ret
f25:
		unbox.any	[mscorlib]System.String
		ldloc.0
		beq		f26
		ldc.i4	-27
		ret
f26:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_CASTCLASS() cil managed {
		.maxstack	8

		ldnull
		dup
		castclass	[mscorlib]System.String
		beq			f1
		ldc.i4.m1
		ret
f1:

		ldstr		"hello"
		dup
		castclass	[mscorlib]System.String
		beq			f2
		ldc.i4		-2
		ret
f2:

		ldstr		"hello"
		dup
		castclass	[mscorlib]System.Object
		beq			f3
		ldc.i4		-3
		ret
f3:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_ISINST() cil managed {
		.maxstack	8

		ldnull
		isinst		[mscorlib]System.String
		brfalse.s	f1
		ldc.i4.m1
		ret
f1:

		ldstr		"hello"
		isinst		[mscorlib]System.String
		brtrue.s	f2
		ldc.i4		-2
		ret
f2:

		ldstr		"hello"
		isinst		[mscorlib]System.Object
		brtrue.s	f3
		ldc.i4		-3
		ret
f3:

		ldstr		"hello"
		isinst		[mscorlib]System.IDisposable
		brfalse.s	f4
		ldc.i4		-4
		ret
f4:

		ldc.i4.0
		box			[mscorlib]System.Byte
		isinst		[mscorlib]System.Byte
		brtrue.s	f5
		ldc.i4		-5
		ret
f5:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_SIZEOF(int32) cil managed {
		.maxstack	8

		sizeof		[mscorlib]System.Boolean
		ldc.i4.1
		beq			f1
		ldc.i4.m1
		ret
f1:

		sizeof		[mscorlib]System.Char
		ldc.i4.2
		beq			f2
		ldc.i4		-2
		ret
f2:

		sizeof		[mscorlib]System.Byte
		ldc.i4.1
		beq			f3
		ldc.i4		-3
		ret
f3:

		sizeof		[mscorlib]System.SByte
		ldc.i4.1
		beq			f4
		ldc.i4		-4
		ret
f4:

		sizeof		[mscorlib]System.Int16
		ldc.i4.2
		beq			f40
		ldc.i4		-40
		ret
f40:

		sizeof		[mscorlib]System.UInt16
		ldc.i4.2
		beq			f5
		ldc.i4		-5
		ret
f5:

		sizeof		[mscorlib]System.Int32
		ldc.i4.4
		beq			f6
		ldc.i4		-6
		ret
f6:

		sizeof		[mscorlib]System.UInt32
		ldc.i4.4
		beq			f7
		ldc.i4		-7
		ret
f7:

		sizeof		[mscorlib]System.Int64
		ldc.i4.8
		beq			f8
		ldc.i4		-8
		ret
f8:

		sizeof		[mscorlib]System.UInt64
		ldc.i4.8
		beq			f9
		ldc.i4		-9
		ret
f9:

		sizeof		[mscorlib]System.Single
		ldc.i4.4
		beq			f10
		ldc.i4		-10
		ret
f10:

		sizeof		[mscorlib]System.Double
		ldc.i4.8
		beq			f11
		ldc.i4		-11
		ret
f11:

		sizeof		[mscorlib]System.Object
		ldarg.0
		beq			f12
		ldc.i4		-12
		ret
f12:

		sizeof		[mscorlib]System.String
		ldarg.0
		beq			f13
		ldc.i4		-13
		ret
f13:

		sizeof		[mscorlib]System.IntPtr
		ldarg.0
		beq			f14
		ldc.i4		-14
		ret
f14:

		sizeof		[mscorlib]System.UIntPtr
		ldarg.0
		beq			f15
		ldc.i4		-15
		ret
f15:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_NEWARR__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		newarr		[mscorlib]System.Int32
		isinst		int32[]
		brtrue.s	f1
		ldc.i4.0
		ret
f1:
		ldarg.0
		newarr		[mscorlib]System.String
		isinst		string[]
		brtrue.s	f2
		ldc.i4.1
		ret
f2:
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_NEWARR__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		newarr		[mscorlib]System.Int32
		isinst		int32[]
		brtrue.s	f1
		ldc.i4.0
		ret
f1:
		ldarg.0
		newarr		[mscorlib]System.String
		isinst		string[]
		brtrue.s	f2
		ldc.i4.1
		ret
f2:
		ldc.i4.8
		ret
	}

	.method public hidebysig static native int Test_LDLEN__Int32(int32) cil managed {
		.maxstack	8
		ldarg.0
		newarr		[mscorlib]System.Int32
		ldlen
		ret
	}

	.method public hidebysig static native int Test_LDLEN__IntPtr(native int) cil managed {
		.maxstack	8
		ldarg.0
		newarr		[mscorlib]System.Int32
		ldlen
		ret
	}

	.method public hidebysig static int32 Test_LDELEM_I__STELEM_I() cil managed {
		.maxstack	8
		ldc.i4.5
		newarr		[mscorlib]System.IntPtr

		dup
		ldc.i4.0
		ldelem.i
		ldc.i4.0
		conv.i
		beq			f1
		pop
		ldc.i4.m1
		ret
f1:

		dup
		ldc.i4.4
		ldelem.i
		ldc.i4.0
		conv.i
		beq			f2
		pop
		ldc.i4		-2
		ret
f2:

		dup
		ldc.i4.4
		ldc.i4.3
		conv.i
		stelem.i

		dup
		ldc.i4.4
		ldelem.i
		ldc.i4.3
		conv.i
		beq			f3
		pop
		ldc.i4		-3
		ret
f3:

		pop
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDELEM_I1__STELEM_I1() cil managed {
		.maxstack	8
		ldc.i4.5
		newarr		[mscorlib]System.SByte

		dup
		ldc.i4.0
		ldelem.i1
		ldc.i4.0
		beq			f1
		pop
		ldc.i4.m1
		ret
f1:

		dup
		ldc.i4.4
		ldelem.i1
		ldc.i4.0
		beq			f2
		pop
		ldc.i4		-2
		ret
f2:

		dup
		ldc.i4.4
		ldc.i4.3
		stelem.i1

		dup
		ldc.i4.4
		ldelem.i1
		ldc.i4.3
		beq			f3
		pop
		ldc.i4		-3
		ret
f3:

		pop
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDELEM_I2__STELEM_I2() cil managed {
		.maxstack	8
		ldc.i4.5
		newarr		[mscorlib]System.Int16

		dup
		ldc.i4.0
		ldelem.i2
		ldc.i4.0
		beq			f1
		pop
		ldc.i4.m1
		ret
f1:

		dup
		ldc.i4.4
		ldelem.i2
		ldc.i4.0
		beq			f2
		pop
		ldc.i4		-2
		ret
f2:

		dup
		ldc.i4.4
		ldc.i4.3
		stelem.i2

		dup
		ldc.i4.4
		ldelem.i2
		ldc.i4.3
		beq			f3
		pop
		ldc.i4		-3
		ret
f3:

		pop
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDELEM_I4__STELEM_I4() cil managed {
		.maxstack	8
		ldc.i4.5
		newarr		[mscorlib]System.Int32

		dup
		ldc.i4.0
		ldelem.i4
		ldc.i4.0
		beq			f1
		pop
		ldc.i4.m1
		ret
f1:

		dup
		ldc.i4.4
		ldelem.i4
		ldc.i4.0
		beq			f2
		pop
		ldc.i4		-2
		ret
f2:

		dup
		ldc.i4.4
		ldc.i4.3
		stelem.i4

		dup
		ldc.i4.4
		ldelem.i4
		ldc.i4.3
		beq			f3
		pop
		ldc.i4		-3
		ret
f3:

		pop
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDELEM_I8__STELEM_I8() cil managed {
		.maxstack	8
		ldc.i4.5
		newarr		[mscorlib]System.Int64

		dup
		ldc.i4.0
		ldelem.i8
		ldc.i4.0
		conv.i8
		beq			f1
		pop
		ldc.i4.m1
		ret
f1:

		dup
		ldc.i4.4
		ldelem.i8
		ldc.i4.0
		conv.i8
		beq			f2
		pop
		ldc.i4		-2
		ret
f2:

		dup
		ldc.i4.4
		ldc.i4.3
		conv.i8
		stelem.i8

		dup
		ldc.i4.4
		ldelem.i8
		ldc.i4.3
		conv.i8
		beq			f3
		pop
		ldc.i4		-3
		ret
f3:

		pop
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDELEM_U1__STELEM_I1() cil managed {
		.maxstack	8
		ldc.i4.5
		newarr		[mscorlib]System.Byte

		dup
		ldc.i4.0
		ldelem.u1
		ldc.i4.0
		beq			f1
		pop
		ldc.i4.m1
		ret
f1:

		dup
		ldc.i4.4
		ldelem.u1
		ldc.i4.0
		beq			f2
		pop
		ldc.i4		-2
		ret
f2:

		dup
		ldc.i4.4
		ldc.i4.3
		stelem.i1

		dup
		ldc.i4.4
		ldelem.u1
		ldc.i4.3
		beq			f3
		pop
		ldc.i4		-3
		ret
f3:

		pop
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDELEM_U2__STELEM_I2() cil managed {
		.maxstack	8
		ldc.i4.5
		newarr		[mscorlib]System.UInt16

		dup
		ldc.i4.0
		ldelem.u2
		ldc.i4.0
		beq			f1
		pop
		ldc.i4.m1
		ret
f1:

		dup
		ldc.i4.4
		ldelem.u2
		ldc.i4.0
		beq			f2
		pop
		ldc.i4		-2
		ret
f2:

		dup
		ldc.i4.4
		ldc.i4.3
		stelem.i2

		dup
		ldc.i4.4
		ldelem.u2
		ldc.i4.3
		beq			f3
		pop
		ldc.i4		-3
		ret
f3:

		pop
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDELEM_U4__STELEM_I4() cil managed {
		.maxstack	8
		ldc.i4.5
		newarr		[mscorlib]System.UInt32

		dup
		ldc.i4.0
		ldelem.u4
		ldc.i4.0
		beq			f1
		pop
		ldc.i4.m1
		ret
f1:

		dup
		ldc.i4.4
		ldelem.u4
		ldc.i4.0
		beq			f2
		pop
		ldc.i4		-2
		ret
f2:

		dup
		ldc.i4.4
		ldc.i4.3
		stelem.i4

		dup
		ldc.i4.4
		ldelem.u4
		ldc.i4.3
		beq			f3
		pop
		ldc.i4		-3
		ret
f3:

		pop
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDELEM_R4__STELEM_R4() cil managed {
		.maxstack	8
		ldc.i4.5
		newarr		[mscorlib]System.Single

		dup
		ldc.i4.0
		ldelem.r4
		ldc.r4		0
		beq			f1
		pop
		ldc.i4.m1
		ret
f1:

		dup
		ldc.i4.4
		ldelem.r4
		ldc.r4		0
		beq			f2
		pop
		ldc.i4		-2
		ret
f2:

		dup
		ldc.i4.4
		ldc.r4		123.456
		stelem.r4

		dup
		ldc.i4.4
		ldelem.r4
		ldc.r4		123.456
		beq			f3
		pop
		ldc.i4		-3
		ret
f3:

		pop
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDELEM_R8__STELEM_R8() cil managed {
		.maxstack	8
		ldc.i4.5
		newarr		[mscorlib]System.Double

		dup
		ldc.i4.0
		ldelem.r8
		ldc.r8		0
		beq			f1
		pop
		ldc.i4.m1
		ret
f1:

		dup
		ldc.i4.4
		ldelem.r8
		ldc.r8		0
		beq			f2
		pop
		ldc.i4		-2
		ret
f2:

		dup
		ldc.i4.4
		ldc.r8		123.456
		stelem.r8

		dup
		ldc.i4.4
		ldelem.r8
		ldc.r8		123.456
		beq			f3
		pop
		ldc.i4		-3
		ret
f3:

		pop
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDELEM_REF__STELEM_REF() cil managed {
		.maxstack	5
		.locals		init (string)

		ldc.i4.5
		newarr		[mscorlib]System.String

		dup
		ldc.i4.0
		ldelem.ref
		ldnull
		beq			f1
		pop
		ldc.i4.m1
		ret
f1:

		dup
		ldc.i4.4
		ldelem.ref
		ldnull
		beq			f2
		pop
		ldc.i4		-2
		ret
f2:

		dup
		ldc.i4.4
		ldstr		"hello"
		stloc.0
		ldloc.0
		stelem.ref

		dup
		ldc.i4.4
		ldelem.ref
		ldloc.0
		beq			f3
		pop
		ldc.i4		-3
		ret
f3:

		pop
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDELEM__STELEM() cil managed {
		.maxstack	5
		.locals		init (string)

		ldc.i4.5
		newarr		[mscorlib]System.String

		dup
		ldc.i4.0
		ldelem		[mscorlib]System.String
		ldnull
		beq			f1
		pop
		ldc.i4.m1
		ret
f1:

		dup
		ldc.i4.4
		ldelem		[mscorlib]System.String
		ldnull
		beq			f2
		pop
		ldc.i4		-2
		ret
f2:

		dup
		ldc.i4.4
		ldstr		"hello"
		stloc.0
		ldloc.0
		stelem		[mscorlib]System.String

		dup
		ldc.i4.4
		ldelem		[mscorlib]System.String
		ldloc.0
		beq			f3
		pop
		ldc.i4		-3
		ret
f3:

		pop
		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDARGA(int32, string) cil managed {
		.maxstack	8
		ldarga		0
		ldarga		0
		beq			f1
		ldc.i4.m1
		ret
f1:

		ldarga		1
		ldarga		1
		beq			f2
		ldc.i4		-2
		ret
f2:

		ldarga		0
		ldarga		1
		bne.un		f3
		ldc.i4		-3
		ret
f3:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDARGA_S(int32, string) cil managed {
		.maxstack	8
		ldarga.s	0
		ldarga.s	0
		beq			f1
		ldc.i4.m1
		ret
f1:

		ldarga.s	1
		ldarga.s	1
		beq			f2
		ldc.i4		-2
		ret
f2:

		ldarga.s	0
		ldarga.s	1
		bne.un		f3
		ldc.i4		-3
		ret
f3:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDLOCA() cil managed {
		.maxstack	2
		.locals		init (int32, string)
		ldloca		0
		ldloca		0
		beq			f1
		ldc.i4.m1
		ret
f1:

		ldloca		1
		ldloca		1
		beq			f2
		ldc.i4		-2
		ret
f2:

		ldloca		0
		ldloca		1
		bne.un		f3
		ldc.i4		-3
		ret
f3:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDLOCA_S() cil managed {
		.maxstack	2
		.locals		init (int32, string)
		ldloca.s	0
		ldloca.s	0
		beq			f1
		ldc.i4.m1
		ret
f1:

		ldloca.s	1
		ldloca.s	1
		beq			f2
		ldc.i4		-2
		ret
f2:

		ldloca.s	0
		ldloca.s	1
		bne.un		f3
		ldc.i4		-3
		ret
f3:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDELEMA() cil managed {
		.maxstack	4
		.locals		init (int32[])

		ldc.i4.5
		newarr		[mscorlib]System.Int32
		stloc.0

		ldloc.0
		ldc.i4.0
		ldelema		[mscorlib]System.Int32
		ldloc.0
		ldc.i4.0
		ldelema		[mscorlib]System.Int32
		beq			f1
		ldc.i4.m1
		ret
f1:

		ldloc.0
		ldc.i4.4
		ldelema		[mscorlib]System.Int32
		ldloc.0
		ldc.i4.4
		ldelema		[mscorlib]System.Int32
		beq			f2
		ldc.i4		-2
		ret
f2:

		ldloc.0
		ldc.i4.0
		ldelema		[mscorlib]System.Int32
		ldloc.0
		ldc.i4.4
		ldelema		[mscorlib]System.Int32
		bne.un		f3
		ldc.i4		-3
		ret
f3:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDIND_STIND() cil managed {
		.maxstack	2
		.locals		init (int8, uint8, int16, uint16, int32, uint32, int64, uint64, native int, native unsigned int, float32, float64, string, string)

		ldloca.s	0
		ldind.i1
		ldc.i4.0
		beq			f1
		ldc.i4.m1
		ret
f1:
		ldloca.s	0
		ldc.i4.3
		stind.i1
		ldloca.s	0
		ldind.i1
		ldc.i4.3
		beq			f2
		ldc.i4		-2
		ret
f2:

		ldloca.s	1
		ldind.u1
		ldc.i4.0
		beq			f3
		ldc.i4		-3
		ret
f3:
		ldloca.s	1
		ldc.i4.3
		stind.i1
		ldloca.s	1
		ldind.u1
		ldc.i4.3
		beq			f4
		ldc.i4		-4
		ret
f4:

		ldloca.s	2
		ldind.i2
		ldc.i4.0
		beq			f5
		ldc.i4		-5
		ret
f5:
		ldloca.s	2
		ldc.i4.3
		stind.i2
		ldloca.s	2
		ldind.i2
		ldc.i4.3
		beq			f6
		ldc.i4		-6
		ret
f6:

		ldloca.s	3
		ldind.u2
		ldc.i4.0
		beq			f7
		ldc.i4		-7
		ret
f7:
		ldloca.s	3
		ldc.i4.3
		stind.i2
		ldloca.s	3
		ldind.u2
		ldc.i4.3
		beq			f8
		ldc.i4		-8
		ret
f8:

		ldloca.s	4
		ldind.i4
		ldc.i4.0
		beq			f9
		ldc.i4		-9
		ret
f9:
		ldloca.s	4
		ldc.i4.3
		stind.i4
		ldloca.s	4
		ldind.i4
		ldc.i4.3
		beq			f10
		ldc.i4		-10
		ret
f10:

		ldloca.s	5
		ldind.u4
		ldc.i4.0
		beq			f11
		ldc.i4		-11
		ret
f11:
		ldloca.s	5
		ldc.i4.3
		stind.i4
		ldloca.s	5
		ldind.u4
		ldc.i4.3
		beq			f12
		ldc.i4		-12
		ret
f12:

		ldloca.s	6
		ldind.i8
		ldc.i4.0
		conv.i8
		beq			f13
		ldc.i4		-13
		ret
f13:
		ldloca.s	6
		ldc.i4.3
		conv.i8
		stind.i8
		ldloca.s	6
		ldind.i8
		ldc.i4.3
		conv.i8
		beq			f14
		ldc.i4		-14
		ret
f14:

		ldloca.s	7
		ldind.i8
		ldc.i4.0
		conv.u8
		beq			f15
		ldc.i4		-15
		ret
f15:
		ldloca.s	7
		ldc.i4.3
		conv.u8
		stind.i8
		ldloca.s	7
		ldind.i8
		ldc.i4.3
		conv.u8
		beq			f16
		ldc.i4		-16
		ret
f16:

		ldloca.s	8
		ldind.i
		ldc.i4.0
		conv.i
		beq			f17
		ldc.i4		-17
		ret
f17:
		ldloca.s	8
		ldc.i4.3
		conv.i
		stind.i
		ldloca.s	8
		ldind.i
		ldc.i4.3
		conv.i
		beq			f18
		ldc.i4		-18
		ret
f18:

		ldloca.s	9
		ldind.i
		ldc.i4.0
		conv.u
		beq			f19
		ldc.i4		-19
		ret
f19:
		ldloca.s	9
		ldc.i4.3
		conv.u
		stind.i
		ldloca.s	9
		ldind.i
		ldc.i4.3
		conv.u
		beq			f20
		ldc.i4		-20
		ret
f20:

		ldloca.s	10
		ldind.r4
		ldc.r4		0
		beq			f21
		ldc.i4		-21
		ret
f21:
		ldloca.s	10
		ldc.r4		123.456
		stind.r4
		ldloca.s	10
		ldind.r4
		ldc.r4		123.456
		beq			f22
		ldc.i4		-22
		ret
f22:

		ldloca.s	11
		ldind.r8
		ldc.r4		0
		beq			f23
		ldc.i4		-23
		ret
f23:
		ldloca.s	11
		ldc.r8		123.456
		stind.r8
		ldloca.s	11
		ldind.r8
		ldc.r8		123.456
		beq			f24
		ldc.i4		-24
		ret
f24:

		ldloca.s	12
		ldind.ref
		ldnull
		beq			f25
		ldc.i4		-25
		ret
f25:
		ldloca.s	12
		ldstr		"hello"
		stloc.s		13
		ldloc.s		13
		stind.ref
		ldloca.s	12
		ldind.ref
		ldloc.s		13
		beq			f26
		ldc.i4		-26
		ret
f26:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_READONLY() cil managed {
		.maxstack	8
		ldc.i4.4
		newarr		[mscorlib]System.Int32
		dup
		ldc.i4.1
		ldc.i4.8
		stelem.i4
		ldc.i4.1
		readonly.
		ldelema		int32
		ldind.i4
		ret
	}

	.method public hidebysig static int32 Test_UNALIGNED() cil managed {
		.maxstack	8
		ldc.i4.4
		newarr		[mscorlib]System.Int32
		dup
		ldc.i4.1
		ldc.i4.8
		stelem.i4
		ldc.i4.1
		readonly.
		ldelema		int32
		unaligned.	4
		ldind.i4
		ret
	}

	.method public hidebysig static int32 Test_VOLATILE() cil managed {
		.maxstack	8
		ldc.i4.4
		newarr		[mscorlib]System.Int32
		dup
		ldc.i4.1
		ldc.i4.8
		stelem.i4
		ldc.i4.1
		readonly.
		ldelema		int32
		volatile.
		ldind.i4
		ret
	}

	.method public hidebysig static int32 Test_UNALIGNED_VOLATILE() cil managed {
		.maxstack	8
		ldc.i4.4
		newarr		[mscorlib]System.Int32
		dup
		ldc.i4.1
		ldc.i4.8
		stelem.i4
		ldc.i4.1
		readonly.
		ldelema		int32
		dup
		unaligned.	4
		volatile.
		ldind.i4
		pop
		volatile.
		unaligned.	4
		ldind.i4
		ret
	}

	.method public hidebysig static int32 Test_INITOBJ() cil managed {
		.maxstack	8
		.locals		init (valuetype [mscorlib]System.Runtime.InteropServices.ComTypes.EXCEPINFO)

		ldloca.s	0
		initobj		[mscorlib]System.Runtime.InteropServices.ComTypes.EXCEPINFO

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDFLD_STFLD_LDFLDA() cil managed {
		.maxstack	8
		.locals		init (valuetype [mscorlib]System.Runtime.InteropServices.ComTypes.EXCEPINFO)

		ldloca.s	0
		initobj		[mscorlib]System.Runtime.InteropServices.ComTypes.EXCEPINFO

		ldloc.0
		ldfld		int32 [mscorlib]System.Runtime.InteropServices.ComTypes.EXCEPINFO::dwHelpContext
		brfalse.s	f1
		ldc.i4.m1
		ret
f1:

		ldloca.s	0
		ldc.i4.3
		stfld		int32 [mscorlib]System.Runtime.InteropServices.ComTypes.EXCEPINFO::dwHelpContext

		ldloc.0
		ldfld		int32 [mscorlib]System.Runtime.InteropServices.ComTypes.EXCEPINFO::dwHelpContext
		ldc.i4.3
		beq.s		f2
		ldc.i4		-2
		ret
f2:

		ldloca.s	0
		initobj		[mscorlib]System.Runtime.InteropServices.ComTypes.EXCEPINFO

		ldloc.0
		ldfld		int32 [mscorlib]System.Runtime.InteropServices.ComTypes.EXCEPINFO::dwHelpContext
		brfalse.s	f3
		ldc.i4		-3
		ret
f3:

		ldloca.s	0
		ldflda		int32 [mscorlib]System.Runtime.InteropServices.ComTypes.EXCEPINFO::dwHelpContext
		dup
		ldind.i4
		brfalse.s	f4
		pop
		ldc.i4		-4
		ret
f4:
		dup
		ldc.i4.3
		stind.i4

		ldind.i4
		ldc.i4.3
		beq.s		f5
		ldc.i4		-5
		ret
f5:

		ldloc.0
		ldfld		int32 [mscorlib]System.Runtime.InteropServices.ComTypes.EXCEPINFO::dwHelpContext
		ldc.i4.3
		beq.s		f6
		ldc.i4		-6
		ret
f6:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDSFLD_STSFLD_LDSFLDA() cil managed {
		.maxstack	8

		ldsfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Counter
		brfalse.s	f1
		ldc.i4.m1
		ret
f1:

		ldc.i4.3
		stsfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Counter

		ldsfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Counter
		ldc.i4.3
		beq.s		f2
		ldc.i4		-2
		ret
f2:

		ldsflda		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Counter
		dup
		ldind.i4
		ldc.i4.3
		beq.s		f3
		pop
		ldc.i4		-3
		ret
f3:
		dup
		ldc.i4.5
		stind.i4

		ldind.i4
		ldc.i4.5
		beq.s		f4
		ldc.i4		-4
		ret
f4:

		ldsfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Counter
		ldc.i4.5
		beq.s		f5
		ldc.i4		-5
		ret
f5:

		ldc.i4.0
		stsfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Counter

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDOBJ_STOBJ() cil managed {
		.maxstack	8
		.locals		init (int32, valuetype dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct, string, string, valuetype dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct)

		ldstr		"hello"
		stloc.2
		ldloca.s	2
		ldobj		[mscorlib]System.String
		ldloc.2
		beq.s		f1
		ldc.i4.m1
		ret
f1:
		ldstr		"bye"
		stloc.3
		ldloca.s	2
		ldloc.3
		stobj		[mscorlib]System.String
		ldloc.2
		ldloc.3
		beq			f2
		ldc.i4		-2
		ret
f2:

		ldc.i4		1234
		stloc.0
		ldloca.s	0
		ldobj		[mscorlib]System.Int32
		ldc.i4		1234
		beq.s		f3
		ldc.i4		-3
		ret
f3:
		ldloca.s	0
		ldc.i4		5678
		stobj		[mscorlib]System.Int32
		ldloc.0
		ldc.i4		5678
		beq			f4
		ldc.i4		-4
		ret
f4:

		ldloca.s	1
		initobj		dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct
		ldloca.s	1
		ldstr		"FieldName"
		stfld		string dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Name
		ldloca.s	1
		ldc.i4		0x1234
		stfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Id
		ldloca.s	1
		ldobj		dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct
		stloc.s		4

		ldloca.s	4
		ldfld		string dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Name
		ldloca.s	1
		ldfld		string dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Name
		beq.s		f5
		ldc.i4		-5
		ret
f5:

		ldloca.s	4
		ldfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Id
		ldc.i4		0x1234
		beq.s		f6
		ldc.i4		-6
		ret
f6:

		ldloca.s	1
		ldstr		"NewString"
		stfld		string dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Name

		ldloca.s	4
		ldfld		string dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Name
		ldloca.s	1
		ldfld		string dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Name
		bne.un.s	f7
		ldc.i4		-7
		ret
f7:

		ldloca.s	4
		ldloca.s	1
		ldobj		dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct
		stobj		dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct

		ldloca.s	4
		ldfld		string dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Name
		ldloca.s	1
		ldfld		string dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Name
		beq.s		f8
		ldc.i4		-8
		ret
f8:

		ldloca.s	4
		ldfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Id
		ldloca.s	1
		ldfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Id
		beq.s		f9
		ldc.i4		-9
		ret
f9:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_CPOBJ() cil managed {
		.maxstack	8
		.locals		init (int32, int32, string, string, valuetype dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct, valuetype dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct)

		ldc.i4		1234
		stloc.0
		ldloca.s	1
		ldloca.s	0
		cpobj		[mscorlib]System.Int32
		ldloc.0
		ldloc.1
		beq.s		f1
		ldc.i4.m1
		ret
f1:

		ldloc.0
		ldc.i4		1234
		beq.s		f10
		ldc.i4		-10
		ret
f10:

		ldstr		"hello"
		stloc.2
		ldloca.s	3
		ldloca.s	2
		cpobj		[mscorlib]System.String
		ldloc.2
		ldloc.3
		beq.s		f2
		ldc.i4		-2
		ret
f2:

		ldloca.s	4
		initobj		dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct
		ldloca.s	5
		initobj		dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct
		ldloca.s	4
		ldstr		"FieldName"
		stfld		string dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Name
		ldloca.s	4
		ldc.i4		0x1234
		stfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Id
		ldloca.s	5
		ldloca.s	4
		cpobj		dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct

		ldloca.s	4
		ldfld		string dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Name
		ldloca.s	5
		ldfld		string dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Name
		beq.s		f3
		ldc.i4		-3
		ret
f3:
		ldloca.s	4
		ldfld		string dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Name
		brtrue.s	f4
		ldc.i4		-4
		ret
f4:
		ldloca.s	4
		ldfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Id
		ldloca.s	5
		ldfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Id
		beq.s		f5
		ldc.i4		-5
		ret
f5:
		ldloca.s	4
		ldfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Id
		ldc.i4		0x1234
		beq.s		f6
		ldc.i4		-6
		ret
f6:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_NEWOBJ__Struct() cil managed {
		.maxstack	3
		.locals		init (valuetype dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct)

		ldnull
		ldc.i4.0
		newobj		instance void dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::.ctor(string, int32)
		ldfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Id
		brfalse.s	f1
		ldc.i4.m1
		ret
f1:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_NEWOBJ__Class() cil managed {
		.maxstack	3
		.locals		init (class dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass)

		ldnull
		ldc.i4.0
		newobj		instance void dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass::.ctor(string, int32)
		ldfld		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass::Id
		brfalse.s	f1
		ldc.i4.m1
		ret
f1:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_CALL__Static_Struct() cil managed {
		.maxstack	3
		.locals		init (string)

		call		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::StaticMethod1()
		ldc.i4.s	123
		beq.s		f1
		ldc.i4.m1
		ret
f1:

		ldc.i4		0x12345678
		call		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::StaticMethod2(int32)
		ldc.i4		0x12345678
		beq.s		f2
		ldc.i4		-2
		ret
f2:

		ldc.i4		0x9ABCDEF0
		call		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::StaticMethod2(int32)
		ldc.i4		0x9ABCDEF0
		beq.s		f3
		ldc.i4		-3
		ret
f3:

		ldstr		"hello"
		stloc.0
		ldc.i4		0x12345678
		ldloc.0
		call		string dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::StaticMethod3(int32, string)
		ldloc.0
		beq.s		f4
		ldc.i4		-4
		ret
f4:

		ldc.i4		0x12345678
		ldloc.0
		ldc.r8		1234567.891011
		call		float64 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::StaticMethod4(int32, string, float64)
		ldc.r8		1234567.891011
		beq.s		f5
		ldc.i4		-5
		ret
f5:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_CALL__Static_Class() cil managed {
		.maxstack	3
		.locals		init (string)

		call		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass::StaticMethod1()
		ldc.i4.s	123
		beq.s		f1
		ldc.i4.m1
		ret
f1:

		ldc.i4		0x12345678
		call		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass::StaticMethod2(int32)
		ldc.i4		0x12345678
		beq.s		f2
		ldc.i4		-2
		ret
f2:

		ldc.i4		0x9ABCDEF0
		call		int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass::StaticMethod2(int32)
		ldc.i4		0x9ABCDEF0
		beq.s		f3
		ldc.i4		-3
		ret
f3:

		ldstr		"hello"
		stloc.0
		ldc.i4		0x12345678
		ldloc.0
		call		string dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass::StaticMethod3(int32, string)
		ldloc.0
		beq.s		f4
		ldc.i4		-4
		ret
f4:

		ldc.i4		0x12345678
		ldloc.0
		ldc.r8		1234567.891011
		call		float64 dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass::StaticMethod4(int32, string, float64)
		ldc.r8		1234567.891011
		beq.s		f5
		ldc.i4		-5
		ret
f5:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_CALL__Instance_Struct() cil managed {
		.maxstack	4
		.locals		init (valuetype dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct, string)

		ldloca.s	0
		initobj		dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct

		ldloca.s	0
		call		instance int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::InstanceMethod1()
		ldc.i4.s	123
		beq.s		f1
		ldc.i4.m1
		ret
f1:

		ldloca.s	0
		ldc.i4		0x12345678
		call		instance int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::InstanceMethod2(int32)
		ldc.i4		0x12345678
		beq.s		f2
		ldc.i4		-2
		ret
f2:

		ldloca.s	0
		ldc.i4		0x9ABCDEF0
		call		instance int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::InstanceMethod2(int32)
		ldc.i4		0x9ABCDEF0
		beq.s		f3
		ldc.i4		-3
		ret
f3:

		ldstr		"hello"
		stloc.1
		ldloca.s	0
		ldc.i4		0x12345678
		ldloc.1
		call		instance string dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::InstanceMethod3(int32, string)
		ldloc.1
		beq.s		f4
		ldc.i4		-4
		ret
f4:

		ldloca.s	0
		ldc.i4		0x12345678
		ldloc.0
		ldc.r8		1234567.891011
		call		instance float64 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::InstanceMethod4(int32, string, float64)
		ldc.r8		1234567.891011
		beq.s		f5
		ldc.i4		-5
		ret
f5:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_CALL__Instance_Class() cil managed {
		.maxstack	4
		.locals		init (class dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass, string)

		newobj		instance void dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass::.ctor()
		stloc.0

		ldloc.0
		call		instance int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass::InstanceMethod1()
		ldc.i4.s	123
		beq.s		f1
		ldc.i4.m1
		ret
f1:

		ldloc.0
		ldc.i4		0x12345678
		call		instance int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass::InstanceMethod2(int32)
		ldc.i4		0x12345678
		beq.s		f2
		ldc.i4		-2
		ret
f2:

		ldloc.0
		ldc.i4		0x9ABCDEF0
		call		instance int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass::InstanceMethod2(int32)
		ldc.i4		0x9ABCDEF0
		beq.s		f3
		ldc.i4		-3
		ret
f3:

		ldstr		"hello"
		stloc.1
		ldloc.0
		ldc.i4		0x12345678
		ldloc.1
		call		instance string dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass::InstanceMethod3(int32, string)
		ldloc.1
		beq.s		f4
		ldc.i4		-4
		ret
f4:

		ldloc.0
		ldc.i4		0x12345678
		ldloc.0
		ldc.r8		1234567.891011
		call		instance float64 dnSpy.Debugger.DotNet.Interpreter.Tests.MyClass::InstanceMethod4(int32, string, float64)
		ldc.r8		1234567.891011
		beq.s		f5
		ldc.i4		-5
		ret
f5:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDTOKEN__Field() cil managed {
		.maxstack	1
		.locals		init (valuetype [mscorlib]System.RuntimeFieldHandle)

		ldtoken		field int32 dnSpy.Debugger.DotNet.Interpreter.Tests.MyStruct::Id
		stloc.0

		ldloca.s	0
		call		instance native int [mscorlib]System.RuntimeFieldHandle::get_Value()
		brtrue.s	f1
		ldc.i4.m1
		ret
f1:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDTOKEN__Method() cil managed {
		.maxstack	1
		.locals		init (valuetype [mscorlib]System.RuntimeMethodHandle)

		ldtoken		method instance int32 [mscorlib]System.String::get_Length()
		stloc.0

		ldloca.s	0
		call		instance native int [mscorlib]System.RuntimeMethodHandle::get_Value()
		brtrue.s	f1
		ldc.i4.m1
		ret
f1:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LDTOKEN__Type() cil managed {
		.maxstack	1
		.locals		init (valuetype [mscorlib]System.RuntimeTypeHandle)

		ldtoken		[mscorlib]System.String
		stloc.0

		ldloca.s	0
		call		instance native int [mscorlib]System.RuntimeTypeHandle::get_Value()
		brtrue.s	f1
		ldc.i4.m1
		ret
f1:

		ldc.i4.8
		ret
	}

	.method public hidebysig static int32 Test_LOCALLOC() cil managed {
		.maxstack	8
		.locals		init (native int)

		ldc.i4		128
		localloc
		stloc.0

		ldloc.0
		dup
		ldc.i4		0x12345678
		stind.i4
		ldind.i4
		ldc.i4		0x12345678
		beq.s		f1
		ldc.i4.m1
		ret
f1:

		ldloc.0
		ldc.i4.s	0x40
		add
		dup
		ldc.i4		0x9ABCDEF0
		stind.i4
		ldind.i4
		ldc.i4		0x9ABCDEF0
		beq.s		f2
		ldc.i4		-2
		ret
f2:

		ldloc.0
		ldc.i4.s	0x7C
		add
		dup
		ldc.i4		0x5AA59669
		stind.i4
		ldind.i4
		ldc.i4		0x5AA59669
		beq.s		f3
		ldc.i4		-3
		ret
f3:

		ldloc.0
		dup
		ldc.i4		0x12345681
		stind.i1
		ldind.i1
		ldc.i4		0xFFFFFF81
		beq.s		f4
		ldc.i4		-4
		ret
f4:

		ldloc.0
		dup
		ldc.i4		0x5AA58123
		stind.i2
		ldind.i2
		ldc.i4		0xFFFF8123
		beq.s		f5
		ldc.i4		-5
		ret
f5:

		ldloc.0
		dup
		ldc.i4		0x81234567
		stind.i4
		ldind.i4
		ldc.i4		0x81234567
		beq.s		f6
		ldc.i4		-6
		ret
f6:

		ldloc.0
		dup
		ldc.i8		0x123456789ABCDEF0
		stind.i8
		ldind.i8
		ldc.i8		0x123456789ABCDEF0
		beq.s		f7
		ldc.i4		-7
		ret
f7:

		ldloc.0
		dup
		ldc.i4		0xFFFFFF81
		stind.i1
		ldind.u1
		ldc.i4		0x00000081
		beq.s		f8
		ldc.i4		-8
		ret
f8:

		ldloc.0
		dup
		ldc.i4		0xFFFF8123
		stind.i2
		ldind.u2
		ldc.i4		0x00008123
		beq.s		f9
		ldc.i4		-9
		ret
f9:

		ldloc.0
		dup
		ldc.i4		0x81234567
		stind.i4
		ldind.u4
		ldc.i4		0x81234567
		beq.s		f10
		ldc.i4		-10
		ret
f10:

		ldloc.0
		dup
		ldc.r4		1234.567
		stind.r4
		ldind.r4
		ldc.r4		1234.567
		beq.s		f11
		ldc.i4		-11
		ret
f11:

		ldloc.0
		dup
		ldc.r8		13245678.9101112
		stind.r8
		ldind.r8
		ldc.r8		13245678.9101112
		beq.s		f12
		ldc.i4		-12
		ret
f12:

		ldloc.0
		dup
		ldc.i4		0x81234567
		conv.i
		stind.i
		ldind.i
		ldc.i4		0x81234567
		conv.i
		beq.s		f13
		ldc.i4		-13
		ret
f13:

		ldc.i4.8
		ret
	}

	.method public hidebysig static void Test_CKFINITE__Single(float32) cil managed {
		.maxstack	8
		ldarg.0
		ckfinite
		pop
		ret
	}

	.method public hidebysig static void Test_CKFINITE__Double(float64) cil managed {
		.maxstack	8
		ldarg.0
		ckfinite
		pop
		ret
	}

	.method public hidebysig static int32 Test_INITBLK() cil managed {
		.maxstack	8
		.locals		init (native int, int32)

		ldc.i4		128
		localloc
		stloc.0

		ldloc.0
		ldc.i4.s	0x5A
		ldc.i4		0x80
		initblk

		ldc.i4.m1
		stloc.1
l1:
		ldloc.1
		ldc.i4.1
		add
		dup
		stloc.1
		ldc.i4		0x80
		bge.s		l1x

		ldloc.0
		ldloc.1
		add
		ldind.u1
		ldc.i4.s	0x5A
		beq.s		l1
		ldc.i4.m1
		ret
l1x:

		ldloc.0
		ldc.i4.1
		add
		ldc.i4		0xA5
		ldc.i4.s	0x7E
		initblk

		ldc.i4.0
		stloc.1
l2:
		ldloc.1
		ldc.i4.1
		add
		dup
		stloc.1
		ldc.i4		0x7F
		bge.s		l2x

		ldloc.0
		ldloc.1
		add
		ldind.u1
		ldc.i4		0xA5
		beq.s		l2
		ldc.i4		-2
		ret
l2x:

		ldloc.0
		ldind.u1
		ldc.i4.s	0x5A
		beq.s		l3
		ldc.i4		-3
		ret
l3:

		ldloc.0
		ldc.i4.s	0x7F
		add
		ldind.u1
		ldc.i4.s	0x5A
		beq.s		l4
		ldc.i4		-4
		ret
l4:

		ldloc.0
		ldc.i4.1
		add
		pop

		ldc.i4.1
		ldloc.0
		add
		pop

		ldloc.0
		ldc.i4.1
		conv.i
		add
		pop

		ldc.i4.1
		conv.i
		ldloc.0
		add
		pop

		ldloc.0
		ldc.i4.1
		add.ovf
		pop

		ldc.i4.1
		ldloc.0
		add.ovf
		pop

		ldloc.0
		ldc.i4.1
		conv.i
		add.ovf
		pop

		ldc.i4.1
		conv.i
		ldloc.0
		add.ovf
		pop

		ldloc.0
		ldc.i4.1
		add.ovf.un
		pop

		ldc.i4.1
		ldloc.0
		add.ovf.un
		pop

		ldloc.0
		ldc.i4.1
		conv.i
		add.ovf.un
		pop

		ldc.i4.1
		conv.i
		ldloc.0
		add.ovf.un
		pop

		ldloc.0
		ldc.i4.1
		add
		ldc.i4.1
		sub
		pop

		ldloc.0
		ldc.i4.1
		add
		ldc.i4.1
		conv.i
		sub
		pop

		ldloc.0
		ldc.i4.1
		add
		ldc.i4.1
		sub.ovf
		pop

		ldloc.0
		ldc.i4.1
		add
		ldc.i4.1
		conv.i
		sub.ovf
		pop

		ldloc.0
		ldc.i4.1
		add
		ldc.i4.1
		sub.ovf.un
		pop

		ldloc.0
		ldc.i4.1
		add
		ldc.i4.1
		conv.i
		sub.ovf.un
		pop

		ldc.i4.8
		ret
	}

	.method public hidebysig static native int Test_AND__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		and
		ret
	}

	.method public hidebysig static native int Test_AND__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		and
		ret
	}

	.method public hidebysig static native int Test_OR__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		or
		ret
	}

	.method public hidebysig static native int Test_OR__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		or
		ret
	}

	.method public hidebysig static native int Test_XOR__IntPtr_Int32(native int, int32) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		xor
		ret
	}

	.method public hidebysig static native int Test_XOR__Int32_IntPtr(int32, native int) cil managed {
		.maxstack	8
		ldarg.0
		ldarg.1
		xor
		ret
	}
}
