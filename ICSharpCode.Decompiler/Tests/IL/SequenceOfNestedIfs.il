// Metadata version: v2.0.50727
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 2:0:0:0
}

.assembly SequenceOfNestedIfs
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module SequenceOfNestedIfs
// MVID: {DCEC8A87-5679-4EBE-89A3-51274D8B5446}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x01D60000


// =============== CLASS MEMBERS DECLARATION ===================

.class public auto ansi serializable beforefieldinit Material
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Material::.ctor

  .method public hidebysig specialname static 
          bool  op_Implicit(class Material m) cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldnull
    IL_0008:  ceq
    IL_000a:  ret
  } // end of method Material::op_Implicit

} // end of class Material

.class public auto ansi serializable beforefieldinit SequenceOfNestedIfs
       extends [mscorlib]System.Object
{
  .field public bool _clear
  .field public class Material _material
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void  [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method SequenceOfNestedIfs::.ctor

  .method public hidebysig virtual instance bool 
          CheckShader() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  ret
  } // end of method SequenceOfNestedIfs::CheckShader

  .method public hidebysig virtual instance void 
          CreateMaterials() cil managed
  {
    // Code size       168 (0xa8)
    .maxstack  13
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool SequenceOfNestedIfs::_clear
    IL_0006:  brtrue     IL_0026

    IL_000b:  ldarg.0
    IL_000c:  callvirt   instance bool SequenceOfNestedIfs::CheckShader()
    IL_0011:  brtrue     IL_001b

    IL_0016:  br         IL_00a7

    IL_001b:  ldarg.0
    IL_001c:  newobj     instance void Material::.ctor()
    IL_0021:  stfld      class Material SequenceOfNestedIfs::_material
    IL_0026:  ldarg.0
    IL_0027:  ldfld      class Material SequenceOfNestedIfs::_material
    IL_002c:  call       bool Material::op_Implicit(class Material)
    IL_0031:  brtrue     IL_0051

    IL_0036:  ldarg.0
    IL_0037:  callvirt   instance bool SequenceOfNestedIfs::CheckShader()
    IL_003c:  brtrue     IL_0046

    IL_0041:  br         IL_00a7

    IL_0046:  ldarg.0
    IL_0047:  newobj     instance void Material::.ctor()
    IL_004c:  stfld      class Material SequenceOfNestedIfs::_material
    IL_0051:  ldarg.0
    IL_0052:  ldfld      class Material SequenceOfNestedIfs::_material
    IL_0057:  call       bool Material::op_Implicit(class Material)
    IL_005c:  brtrue     IL_007c

    IL_0061:  ldarg.0
    IL_0062:  callvirt   instance bool SequenceOfNestedIfs::CheckShader()
    IL_0067:  brtrue     IL_0071

    IL_006c:  br         IL_00a7

    IL_0071:  ldarg.0
    IL_0072:  newobj     instance void Material::.ctor()
    IL_0077:  stfld      class Material SequenceOfNestedIfs::_material
    IL_007c:  ldarg.0
    IL_007d:  ldfld      class Material SequenceOfNestedIfs::_material
    IL_0082:  call       bool Material::op_Implicit(class Material)
    IL_0087:  brtrue     IL_00a7

    IL_008c:  ldarg.0
    IL_008d:  callvirt   instance bool SequenceOfNestedIfs::CheckShader()
    IL_0092:  brtrue     IL_009c

    IL_0097:  br         IL_00a7

    IL_009c:  ldarg.0
    IL_009d:  newobj     instance void Material::.ctor()
    IL_00a2:  stfld      class Material SequenceOfNestedIfs::_material
    IL_00a7:  ret
  } // end of method SequenceOfNestedIfs::CreateMaterials

} // end of class SequenceOfNestedIfs