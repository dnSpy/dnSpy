.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly BooleanConsumedAsInteger
{
  .hash algorithm 0x00008004
  .ver 1:0:0:0
}
.module BooleanConsumedAsInteger.exe
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000003    //  ILONLY 32BITREQUIRED

.class private auto ansi beforefieldinit BooleanConsumedAsInteger.Program extends [mscorlib]System.Object
{
  .method public hidebysig static void  Main(string[] args) cil managed
  {
    .entrypoint
    .maxstack  8
	
	ret
  }

  .method public hidebysig static int32 ReturnBoolAsInt() cil managed
  {
	ldnull
	ldnull
	call bool [mscorlib] System.Object::Equals(object, object)
	ret
  }

  .method public hidebysig static int32 BitwiseOperationOnBool() cil managed
  {
	ldnull
	ldnull
	call bool [mscorlib] System.Object::Equals(object, object)
	ldc.i4 255
	and
	ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Program::.ctor

} // end of class StackTests.Program


// =============================================================
