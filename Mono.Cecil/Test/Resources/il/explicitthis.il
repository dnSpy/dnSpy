.assembly extern mscorlib
{
  .ver 0:0:0:0
}

.assembly fptr
{
  .ver 0:0:0:0
}
.module fptr.exe

.method public static void  Main() cil managed
{
	.entrypoint
	.locals init (class MakeDecision d, method instance explicit int32 *(class MakeDecision, int32) m, int32 i)

	ldc.i4.1
	ldc.i4 42
	newobj     instance void MakeDecision::.ctor(bool, int32)
	stloc d

	ldc.i4.0
	stloc i
	br test

loop:
	ldloc d
	call       instance method instance explicit int32 *(class MakeDecision, int32) MakeDecision::Decide()
	stloc m

	ldloc d
	ldc.i4.1
	ldloc m
	calli      instance int32(int32)
	call       void [mscorlib]System.Console::WriteLine(int32)

	ldloc i
	ldc.i4.1
	add
	stloc i

test:
	ldloc i
	ldc.i4 10
	blt loop

	ret
}

.class public auto ansi sealed MakeDecision
       extends [mscorlib]System.Object
{
	.field private bool Oscillate
	.field private int32 Value

	.method public instance method instance explicit int32 *(class MakeDecision, int32) Decide() cil managed
	{
		.locals init (bool t)

		ldarg.0
		ldfld     bool MakeDecision::Oscillate
		stloc t

		ldarg.0
		ldloc t
		ldc.i4.0
		ceq
		stfld bool MakeDecision::Oscillate

		ldloc t
		brfalse subs

		ldftn      instance int32 MakeDecision::Add(int32)
		ret

	subs:
		ldftn      instance int32 MakeDecision::Sub(int32)
		ret
	}

	.method public int32  Add(int32 i) cil managed
	{
		ldarg.0
		ldfld      int32 MakeDecision::Value
		ldarg      i
		add
		ret
	}

	.method public int32  Sub(int32 i) cil managed
	{
		ldarg.0
		ldfld      int32 MakeDecision::Value
		ldarg      i
		sub
		ret
	}

	.method public hidebysig specialname rtspecialname  instance void  .ctor(bool s, int32 val) cil managed
	{
		ldarg.0
		ldarg s
		stfld      bool MakeDecision::Oscillate

		ldarg.0
		ldarg val
		stfld      int32 MakeDecision::Value

		ldarg.0
		call       instance void [mscorlib]System.Object::.ctor()
		ret
	}
}
