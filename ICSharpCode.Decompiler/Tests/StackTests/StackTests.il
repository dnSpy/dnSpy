.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )
  .ver 4:0:0:0
}
.assembly StackTests
{
  .hash algorithm 0x00008004
  .ver 1:0:4059:39717
}
.module StackTests.exe
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000003    //  ILONLY 32BITREQUIRED

.class private auto ansi beforefieldinit StackTests.Program extends [mscorlib]System.Object
{
  .method public hidebysig static void  Main(string[] args) cil managed
  {
    .entrypoint
    .maxstack  8
	
	ldc.i4.0
	call string StackTests.Program::Test1(bool cond)
	call void [mscorlib]System.Console::WriteLine(string) // false
	
	ldc.i4.1
	call string StackTests.Program::Test1(bool cond)
	call void [mscorlib]System.Console::WriteLine(string) // true
	
	ldc.i4.0
	ldc.i4.0
	ldc.i4.0
	call int32 StackTests.Program::Test2(int32 switch1, int32 br1, int32 br2)
	call void [mscorlib]System.Console::WriteLine(int32)  // 11
	
	ldc.i4.0
	ldc.i4.1
	ldc.i4.0
	call int32 StackTests.Program::Test2(int32 switch1, int32 br1, int32 br2)
	call void [mscorlib]System.Console::WriteLine(int32)  // 21
	
	ldc.i4.1
	ldc.i4.1
	ldc.i4.1
	call int32 StackTests.Program::Test2(int32 switch1, int32 br1, int32 br2)
	call void [mscorlib]System.Console::WriteLine(int32)  // 32
	
	ldc.i4.2
	ldc.i4.1
	ldc.i4.0
	call int32 StackTests.Program::Test2(int32 switch1, int32 br1, int32 br2)
	call void [mscorlib]System.Console::WriteLine(int32)  // 23
	
	ret
  }

  .method public hidebysig static string Test1(bool cond) cil managed
  {
	ldarg.0
	brtrue TRUE
	
	FALSE:
	ldstr "false"
	br EXIT
		
	TRUE:
	ldstr "true"
		
	EXIT:
	ret
  }

  .method public hidebysig static int32 Test2(int32 switch1, int32 br1, int32 br2) cil managed
  {
    ldarg.0
    switch (ENTRY1, ENTRY2, ENTRY3)
	ldc.i4.0
    ret

    ENTRY1:
	ldc.i4.1
    br BRANCH1

    ENTRY2:
	ldc.i4.2
    br BRANCH1

    ENTRY3:
	ldc.i4.3
    br BRANCH2
	
	BRANCH1:
	ldarg.1
	brtrue BRANCH2
	
	EXIT1:
	ldc.i4 10
	add
	ret
	
	BRANCH2:
	ldarg.2
	brtrue.s EXIT3
	
	EXIT2:
	ldc.i4 20
	add
	ret
	
	EXIT3:
	ldc.i4 30
	add
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
