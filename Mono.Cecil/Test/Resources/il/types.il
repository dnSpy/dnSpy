.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89)
  .ver 2:0:0:0
}

.assembly Types {}

.module Types.dll

.class private auto ansi Types {

	.field public int32[,] rank_two
	.field public int32[0...,0...] rank_two_low_bound_zero
	.field public int32[-1...4] rank_one_low_bound_m1

	.method public specialname rtspecialname instance void .ctor () cil managed
	{
		ldarg.0
		call instance void [mscorlib]System.Object::.ctor ()
		ret
	}
}

.class interface private abstract auto ansi IFoo
{
}

.class interface private abstract auto ansi IBar
       implements IFoo
{
}

.class interface private abstract IBaz
       implements IBar, IFoo
{
}

.class private Fields {

	.field private static literal int32 int32_int16 = int16(0x0001)
	.field private static literal int16 int16_int32 = int32(0x00000001)
	.field private static literal char char_int16 = int16(0x0001)
	.field private static literal int16 int16_char = char(0x0073)
	.field private static literal int32 int32_nullref = nullref
}

.class public auto ansi CustomAttribute extends [mscorlib]System.Attribute
{
	.custom instance void CustomAttribute::.ctor() = ()

	.method public hidebysig specialname rtspecialname instance void .ctor()
	{
		ldarg.0
		call instance void [mscorlib]System.Attribute::.ctor()
		ret
	}
}
