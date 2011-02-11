@echo off

echo Generating with #Coco

cd Frames

cp ../CSharp/cs.ATG .
mono SharpCoco.exe -namespace ICSharpCode.NRefactory.Parser.CSharp cs.ATG
mv Parser.cs ../CSharp

cp ../VBNet/VBNET.ATG .
mono SharpCoco.exe -trace GIPXA -namespace ICSharpCode.NRefactory.Parser.VB VBNET.ATG
mv Parser.cs ../VBNet

rm cs.ATG
rm VBNET.ATG

cd ..
