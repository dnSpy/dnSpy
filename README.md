dnSpy
=====

dnSpy is a .NET assembly editor, decompiler, and debugger forked from [ILSpy](https://github.com/icsharpcode/ILSpy).

Extra features present in dnSpy
===============================

* Assembly editor
* Debugger
* Tabs
* Horizontal/vertical tab groups
* Themes (blue, dark, light and high contrast)
* Updated syntax highlighting code
* Multifile assembly support
* Command line decompiler (dnspc.exe)
* More stable, can handle bad input that will crash ILSpy
* Other minor updates / fixes

Debugger
========

Updates to the Debugger
=======================

* Improved stepping
* C#, VB, IL code can be debugged
* Breakpoints are automatically saved
* Current statement is highlighted instead of the whole line
* Many other minor updates / fixes

Known issues
============

* Debugger + IL mode: BPs can be set on any IL offset but the BPs are only triggered if they're at the start of statements (eg. offsets where the IL stack is empty and offsets following method calls).
* Debugger can't debug iterator methods (yield return).

Keyboard shortcuts
==================

Key | Description
--- | -----------
Ctrl+F		| (Text view) Search
F3			| (Text view) Find next match
Shift+F3	| (Text view) Find previous match
ESC			| (Text view) Remove selected markers or close search box
Backspace	| Navigate back in history
Alt+Left Arrow | Navigate back in history
Alt+Right Arrow | Navigate forward in history
F5			| (Debugger) Continue debugging
Shift+F5	| (Debugger) Stop debugging
Ctrl+Shift+F5 | (Debugger) Restart debugged program
F9			| (Text view) Toggle breakpoint at caret
Ctrl+F9		| (Text view) Toggle enable/disable breakpoint at caret
Ctrl+Shift+F9 | Delete all breakpoints
F10			| (Debugger) Step over
Ctrl+Shift+F10 | (Debugger) Set next statement
F11			| (Debugger) Step into next method
Shift+F11	| (Debugger) Step out of current method
Ctrl+Pause	| (Debugger) Break
Alt+*		| (Debugger) Show next statement
Ctrl+D		| (Text view) Go to token
Ctrl+G		| (Text view) Go to line
Ctrl+T		| Open a new tab
Ctrl+W		| Close current tab
Ctrl+F4		| Close current tab
Ctrl+Tab	| Go to next tab
Ctrl+Shift+Tab | Go to previous tab
Ctrl+K		| Open search pane
Ctrl+T		| (Search pane) Select Type
Ctrl+M		| (Search pane) Select Member
Ctrl+S		| (Search pane) Select Literal
F12			| (Text view) Follow reference at caret
Enter		| (Text view) Follow reference at caret
Ctrl+F12	| (Text view) Follow reference at caret in a new tab
Ctrl+Enter	| (Text view) Follow reference at caret in a new tab
Ctrl+Click	| (Text view) Follow the clicked reference in a new tab
Ctrl+Alt+W	| (Text view) Toggle word wrap
Shift+Dbl Click| (BP/Call stack/Search/etc windows) Open BP/method/etc in a new tab
Ctrl+C		| (Text view) Copy selected text
Ctrl+S		| Save code
Ctrl+Shift+S| Save all modified assemblies and netmodules
Ctrl+O		| Open assembly
Ctrl+Z		| (Assembly Editor) Undo
Ctrl+Y		| (Assembly Editor) Redo
Ctrl+Shift+Z| (Assembly Editor) Redo
Ctrl++		| (Text view) Zoom In
Ctrl+-		| (Text view) Zoom Out
Ctrl+0		| (Text view) Zoom Reset
Ctrl+Scroll Wheel| (Text view) Zoom In/Out
Alt+Click	| (Text view) Don't follow the clicked reference so it's possible to start selecting text without being taken to the definition. References are only followed if none of Ctrl, Alt and Shift are pressed or if Ctrl is pressed.
F7			| Give text editor keyboard focus
Ctrl+Alt+0	| Give text editor keyboard focus
Ctrl+Alt+L	| Give tree view keyboard focus
Ctrl+Alt+B	| Show Breakpoints window
Alt+F9		| Show Breakpoints window
Ctrl+Alt+C	| Show Call Stack window
Alt+7		| Show Call Stack window
Shift+Alt+Enter | Toggle full screen mode
Tab			| (Text view) Move to the next reference. Does nothing if the caret is not on a reference.
Shift+Tab	| (Text view) Move to the previous reference. Does nothing if the caret is not on a reference.


Credits
=======

For license info, authors and other credits, see README.txt.

Build instructions
==================

Dependencies:

* [dnlib](https://github.com/0xd4d/dnlib)
* [NRefactory](https://github.com/0xd4d/NRefactory)
* [AvalonEdit](https://github.com/0xd4d/AvalonEdit)

Make sure you get the source code from the above links because eg. NRefactory and AvalonEdit have been slightly modified. You must define `THREAD_SAFE` when compiling dnlib. dnSpy will immediately exit if it detects that dnlib isn't thread safe.

Use Visual Studio 2010 or later or run `debugbuild.bat` / `releasebuild.bat` to build it once you have all dependencies.

You need the Visual Studio SDK to build ILSpy.AddIn.
