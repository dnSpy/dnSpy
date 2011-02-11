@echo off

echo Generating with #Coco

cd Frames

copy ..\CSharp\cs.ATG
SharpCoco -namespace ICSharpCode.NRefactory.Parser.CSharp cs.ATG
move Parser.cs ..\CSharp

copy ..\VBNet\VBNET.ATG
SharpCoco -namespace ICSharpCode.NRefactory.Parser.VB VBNET.ATG
move Parser.cs ..\VBNet

del cs.ATG
del VBNET.ATG

pause
cd ..