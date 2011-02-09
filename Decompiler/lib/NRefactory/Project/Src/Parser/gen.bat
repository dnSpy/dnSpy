@echo off

goto old


:old
echo Generating with #Coco

cd Frames

copy ..\CSharp\cs.ATG
SharpCoco -namespace ICSharpCode.NRefactory.Parser.CSharp cs.ATG
move Parser.cs ..\CSharp

copy ..\VBNet\VBNET.ATG
SharpCoco -trace GIPXA -namespace ICSharpCode.NRefactory.Parser.VB VBNET.ATG
move Parser.cs ..\VBNet

del cs.ATG
del VBNET.ATG

:exit
pause
cd ..
