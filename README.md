dnSpy
=====

For license info, authors and other credits, see README.txt.

dnSpy is ILSpy using dnlib. It should be able to read assemblies that ILSpy can't read since dnlib isn't as sensitive as Mono.Cecil when it comes to reading assemblies and invalid .NET metadata. The decompiler has also gotten some updates so it can handle some invalid input I discovered during testing.

Most of the porting work were done by [yck1509 (Ki)](https://github.com/yck1509). He split it up into several projects, but these have been merged into one project again so updating dnSpy to latest ILSpy version is as easy as possible.

BUILD instructions
==================

You need [dnlib](https://github.com/0xd4d/dnlib) and you must define `THREAD_SAFE` when compiling it. dnSpy will immediately exit if it detects that dnlib isn't thread safe.

Compile it with VS2010. I've not tried to compile it with VS2012-2013. If you try to compile it with VS2012 or later, you may need to remove a few projects, which haven't been ported yet:

* ILSpy.AddIn
* ICSharpCode.Decompiler.Tests
