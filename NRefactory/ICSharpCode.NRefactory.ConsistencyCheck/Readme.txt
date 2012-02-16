This is an automatic consistency check for NRefactory.
It loads a solution file and parses all the source code and referenced libraries,
and then performs a set of consistency checks.
These checks assume that the code is valid C# without any compilation errors,
so make sure to only pass in compilable source code.

Checks currently being performed:
 - Roundtripping test: parses C# code and outputs it again using CSharpOutputVisitor, checking that only whitespace is changing
 - ResolverTest: fully resolves all ASTs and validates that no errors are detected (no false positives in semantic error checking)
