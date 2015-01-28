dnSpy
=====

For license info, authors and other credits, see README.txt.

dnSpy is [ILSpy](https://github.com/icsharpcode/ILSpy) using dnlib. dnSpy is now able to open assemblies that ILSpy can't.

Most of the porting work were done by [yck1509 (Ki)](https://github.com/yck1509). He split it up into several projects, but these have been merged into one project again so updating dnSpy to latest ILSpy version is as easy as possible. I fixed the remaining porting todos and fixed stuff I discovered during testing.

A command line tool using dnSpy (dnspc.exe) has also been added.

Build instructions
==================

You need [dnlib](https://github.com/0xd4d/dnlib) and you must define `THREAD_SAFE` when compiling it. dnSpy will immediately exit if it detects that dnlib isn't thread safe.

Compile it with VS2010. I've not tried to compile it with VS2012-2013. If you try to compile it with VS2012 or later, you may need to remove a few projects, which haven't been ported yet:

* ILSpy.AddIn
* ICSharpCode.Decompiler.Tests
