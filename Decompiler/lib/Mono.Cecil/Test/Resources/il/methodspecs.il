
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 2:0:0:0
}

.assembly MethodSpecs
{
}

.module MethodSpecs.dll

.class private auto ansi beforefieldinit Tamtam
       extends [mscorlib]System.Object
{
  .method private hidebysig static void  Foo<TFoo>(!!TFoo tf) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Tamtam::Foo

  .method private hidebysig static void  Bar() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldc.i4.2
    IL_0001:  call       void Tamtam::Foo<int32>(!!0)
    IL_0006:  ret
  } // end of method Tamtam::Bar

  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Tamtam::.ctor

} // end of class Tamtam
