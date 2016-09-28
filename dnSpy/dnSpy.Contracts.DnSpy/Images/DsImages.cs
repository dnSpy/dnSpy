/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Reflection;

namespace dnSpy.Contracts.Images {
	/// <summary>
	/// Image references to images used by dnSpy
	/// </summary>
	public static class DsImages {
		static readonly Assembly assembly = System.Reflection.Assembly.Load("dnSpy");
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public static ImageReference Add { get; } = new ImageReference(assembly, DsImagesStrings.Add);
		public static ImageReference AddReference { get; } = new ImageReference(assembly, DsImagesStrings.AddReference);
		public static ImageReference Assembly { get; } = new ImageReference(assembly, DsImagesStrings.Assembly);
		public static ImageReference AssemblyError { get; } = new ImageReference(assembly, DsImagesStrings.AssemblyError);
		public static ImageReference AssemblyExe { get; } = new ImageReference(assembly, DsImagesStrings.AssemblyExe);
		public static ImageReference AutoSizeOptimize { get; } = new ImageReference(assembly, DsImagesStrings.AutoSizeOptimize);
		public static ImageReference Backwards { get; } = new ImageReference(assembly, DsImagesStrings.Backwards);
		public static ImageReference Binary { get; } = new ImageReference(assembly, DsImagesStrings.Binary);
		public static ImageReference BinaryFile { get; } = new ImageReference(assembly, DsImagesStrings.BinaryFile);
		public static ImageReference Branch { get; } = new ImageReference(assembly, DsImagesStrings.Branch);
		public static ImageReference BreakpointDisabled { get; } = new ImageReference(assembly, DsImagesStrings.BreakpointDisabled);
		public static ImageReference BreakpointEnabled { get; } = new ImageReference(assembly, DsImagesStrings.BreakpointEnabled);
		public static ImageReference BreakpointsWindow { get; } = new ImageReference(assembly, DsImagesStrings.BreakpointsWindow);
		public static ImageReference BuildSolution { get; } = new ImageReference(assembly, DsImagesStrings.BuildSolution);
		public static ImageReference CallReturnInstructionPointer { get; } = new ImageReference(assembly, DsImagesStrings.CallReturnInstructionPointer);
		public static ImageReference CallStackWindow { get; } = new ImageReference(assembly, DsImagesStrings.CallStackWindow);
		public static ImageReference Cancel { get; } = new ImageReference(assembly, DsImagesStrings.Cancel);
		public static ImageReference CheckDot { get; } = new ImageReference(assembly, DsImagesStrings.CheckDot);
		public static ImageReference ClassInternal { get; } = new ImageReference(assembly, DsImagesStrings.ClassInternal);
		public static ImageReference ClassPrivate { get; } = new ImageReference(assembly, DsImagesStrings.ClassPrivate);
		public static ImageReference ClassProtected { get; } = new ImageReference(assembly, DsImagesStrings.ClassProtected);
		public static ImageReference ClassPublic { get; } = new ImageReference(assembly, DsImagesStrings.ClassPublic);
		public static ImageReference ClassShortcut { get; } = new ImageReference(assembly, DsImagesStrings.ClassShortcut);
		public static ImageReference ClearBreakpointGroup { get; } = new ImageReference(assembly, DsImagesStrings.ClearBreakpointGroup);
		public static ImageReference ClearWindowContent { get; } = new ImageReference(assembly, DsImagesStrings.ClearWindowContent);
		public static ImageReference CloseAll { get; } = new ImageReference(assembly, DsImagesStrings.CloseAll);
		public static ImageReference CloseDocumentGroup { get; } = new ImageReference(assembly, DsImagesStrings.CloseDocumentGroup);
		public static ImageReference CloseSolution { get; } = new ImageReference(assembly, DsImagesStrings.CloseSolution);
		public static ImageReference ConstantInternal { get; } = new ImageReference(assembly, DsImagesStrings.ConstantInternal);
		public static ImageReference ConstantPrivate { get; } = new ImageReference(assembly, DsImagesStrings.ConstantPrivate);
		public static ImageReference ConstantProtected { get; } = new ImageReference(assembly, DsImagesStrings.ConstantProtected);
		public static ImageReference ConstantPublic { get; } = new ImageReference(assembly, DsImagesStrings.ConstantPublic);
		public static ImageReference ConstantSealed { get; } = new ImageReference(assembly, DsImagesStrings.ConstantSealed);
		public static ImageReference ConstantShortcut { get; } = new ImageReference(assembly, DsImagesStrings.ConstantShortcut);
		public static ImageReference Copy { get; } = new ImageReference(assembly, DsImagesStrings.Copy);
		public static ImageReference CopyItem { get; } = new ImageReference(assembly, DsImagesStrings.CopyItem);
		public static ImageReference CSFileNode { get; } = new ImageReference(assembly, DsImagesStrings.CSFileNode);
		public static ImageReference CSInteractiveWindow { get; } = new ImageReference(assembly, DsImagesStrings.CSInteractiveWindow);
		public static ImageReference CSProjectNode { get; } = new ImageReference(assembly, DsImagesStrings.CSProjectNode);
		public static ImageReference CurrentInstructionPointer { get; } = new ImageReference(assembly, DsImagesStrings.CurrentInstructionPointer);
		public static ImageReference Cursor { get; } = new ImageReference(assembly, DsImagesStrings.Cursor);
		public static ImageReference Cut { get; } = new ImageReference(assembly, DsImagesStrings.Cut);
		public static ImageReference DelegateInternal { get; } = new ImageReference(assembly, DsImagesStrings.DelegateInternal);
		public static ImageReference DelegatePrivate { get; } = new ImageReference(assembly, DsImagesStrings.DelegatePrivate);
		public static ImageReference DelegateProtected { get; } = new ImageReference(assembly, DsImagesStrings.DelegateProtected);
		public static ImageReference DelegatePublic { get; } = new ImageReference(assembly, DsImagesStrings.DelegatePublic);
		public static ImageReference DelegateShortcut { get; } = new ImageReference(assembly, DsImagesStrings.DelegateShortcut);
		public static ImageReference Dialog { get; } = new ImageReference(assembly, DsImagesStrings.Dialog);
		public static ImageReference DisableAllBreakpoints { get; } = new ImageReference(assembly, DsImagesStrings.DisableAllBreakpoints);
		public static ImageReference DisassemblyWindow { get; } = new ImageReference(assembly, DsImagesStrings.DisassemblyWindow);
		public static ImageReference DownloadNoColor { get; } = new ImageReference(assembly, DsImagesStrings.DownloadNoColor);
		public static ImageReference DraggedCurrentInstructionPointer { get; } = new ImageReference(assembly, DsImagesStrings.DraggedCurrentInstructionPointer);
		public static ImageReference Editor { get; } = new ImageReference(assembly, DsImagesStrings.Editor);
		public static ImageReference EnableAllBreakpoints { get; } = new ImageReference(assembly, DsImagesStrings.EnableAllBreakpoints);
		public static ImageReference EntryPoint { get; } = new ImageReference(assembly, DsImagesStrings.EntryPoint);
		public static ImageReference EnumerationInternal { get; } = new ImageReference(assembly, DsImagesStrings.EnumerationInternal);
		public static ImageReference EnumerationItemInternal { get; } = new ImageReference(assembly, DsImagesStrings.EnumerationItemInternal);
		public static ImageReference EnumerationItemPrivate { get; } = new ImageReference(assembly, DsImagesStrings.EnumerationItemPrivate);
		public static ImageReference EnumerationItemProtected { get; } = new ImageReference(assembly, DsImagesStrings.EnumerationItemProtected);
		public static ImageReference EnumerationItemPublic { get; } = new ImageReference(assembly, DsImagesStrings.EnumerationItemPublic);
		public static ImageReference EnumerationItemSealed { get; } = new ImageReference(assembly, DsImagesStrings.EnumerationItemSealed);
		public static ImageReference EnumerationItemShortcut { get; } = new ImageReference(assembly, DsImagesStrings.EnumerationItemShortcut);
		public static ImageReference EnumerationPrivate { get; } = new ImageReference(assembly, DsImagesStrings.EnumerationPrivate);
		public static ImageReference EnumerationProtected { get; } = new ImageReference(assembly, DsImagesStrings.EnumerationProtected);
		public static ImageReference EnumerationPublic { get; } = new ImageReference(assembly, DsImagesStrings.EnumerationPublic);
		public static ImageReference EnumerationShortcut { get; } = new ImageReference(assembly, DsImagesStrings.EnumerationShortcut);
		public static ImageReference EventInternal { get; } = new ImageReference(assembly, DsImagesStrings.EventInternal);
		public static ImageReference EventPrivate { get; } = new ImageReference(assembly, DsImagesStrings.EventPrivate);
		public static ImageReference EventProtected { get; } = new ImageReference(assembly, DsImagesStrings.EventProtected);
		public static ImageReference EventPublic { get; } = new ImageReference(assembly, DsImagesStrings.EventPublic);
		public static ImageReference EventSealed { get; } = new ImageReference(assembly, DsImagesStrings.EventSealed);
		public static ImageReference EventShortcut { get; } = new ImageReference(assembly, DsImagesStrings.EventShortcut);
		public static ImageReference ExceptionInternal { get; } = new ImageReference(assembly, DsImagesStrings.ExceptionInternal);
		public static ImageReference ExceptionPrivate { get; } = new ImageReference(assembly, DsImagesStrings.ExceptionPrivate);
		public static ImageReference ExceptionProtected { get; } = new ImageReference(assembly, DsImagesStrings.ExceptionProtected);
		public static ImageReference ExceptionPublic { get; } = new ImageReference(assembly, DsImagesStrings.ExceptionPublic);
		public static ImageReference ExceptionSettings { get; } = new ImageReference(assembly, DsImagesStrings.ExceptionSettings);
		public static ImageReference ExceptionShortcut { get; } = new ImageReference(assembly, DsImagesStrings.ExceptionShortcut);
		public static ImageReference ExtensionMethod { get; } = new ImageReference(assembly, DsImagesStrings.ExtensionMethod);
		public static ImageReference FieldInternal { get; } = new ImageReference(assembly, DsImagesStrings.FieldInternal);
		public static ImageReference FieldPrivate { get; } = new ImageReference(assembly, DsImagesStrings.FieldPrivate);
		public static ImageReference FieldProtected { get; } = new ImageReference(assembly, DsImagesStrings.FieldProtected);
		public static ImageReference FieldPublic { get; } = new ImageReference(assembly, DsImagesStrings.FieldPublic);
		public static ImageReference FieldSealed { get; } = new ImageReference(assembly, DsImagesStrings.FieldSealed);
		public static ImageReference FieldShortcut { get; } = new ImageReference(assembly, DsImagesStrings.FieldShortcut);
		public static ImageReference Fill { get; } = new ImageReference(assembly, DsImagesStrings.Fill);
		public static ImageReference Filter { get; } = new ImageReference(assembly, DsImagesStrings.Filter);
		public static ImageReference FolderClosed { get; } = new ImageReference(assembly, DsImagesStrings.FolderClosed);
		public static ImageReference FolderOpened { get; } = new ImageReference(assembly, DsImagesStrings.FolderOpened);
		public static ImageReference Forwards { get; } = new ImageReference(assembly, DsImagesStrings.Forwards);
		public static ImageReference GoToNext { get; } = new ImageReference(assembly, DsImagesStrings.GoToNext);
		public static ImageReference GoToNextInList { get; } = new ImageReference(assembly, DsImagesStrings.GoToNextInList);
		public static ImageReference GoToSourceCode { get; } = new ImageReference(assembly, DsImagesStrings.GoToSourceCode);
		public static ImageReference Image { get; } = new ImageReference(assembly, DsImagesStrings.Image);
		public static ImageReference IntellisenseKeyword { get; } = new ImageReference(assembly, DsImagesStrings.IntellisenseKeyword);
		public static ImageReference InterfaceInternal { get; } = new ImageReference(assembly, DsImagesStrings.InterfaceInternal);
		public static ImageReference InterfacePrivate { get; } = new ImageReference(assembly, DsImagesStrings.InterfacePrivate);
		public static ImageReference InterfaceProtected { get; } = new ImageReference(assembly, DsImagesStrings.InterfaceProtected);
		public static ImageReference InterfacePublic { get; } = new ImageReference(assembly, DsImagesStrings.InterfacePublic);
		public static ImageReference InterfaceShortcut { get; } = new ImageReference(assembly, DsImagesStrings.InterfaceShortcut);
		public static ImageReference Label { get; } = new ImageReference(assembly, DsImagesStrings.Label);
		public static ImageReference Library { get; } = new ImageReference(assembly, DsImagesStrings.Library);
		public static ImageReference LocalsWindow { get; } = new ImageReference(assembly, DsImagesStrings.LocalsWindow);
		public static ImageReference LocalVariable { get; } = new ImageReference(assembly, DsImagesStrings.LocalVariable);
		public static ImageReference MarkupTag { get; } = new ImageReference(assembly, DsImagesStrings.MarkupTag);
		public static ImageReference MemoryWindow { get; } = new ImageReference(assembly, DsImagesStrings.MemoryWindow);
		public static ImageReference Metadata { get; } = new ImageReference(assembly, DsImagesStrings.Metadata);
		public static ImageReference MethodInternal { get; } = new ImageReference(assembly, DsImagesStrings.MethodInternal);
		public static ImageReference MethodPrivate { get; } = new ImageReference(assembly, DsImagesStrings.MethodPrivate);
		public static ImageReference MethodProtected { get; } = new ImageReference(assembly, DsImagesStrings.MethodProtected);
		public static ImageReference MethodPublic { get; } = new ImageReference(assembly, DsImagesStrings.MethodPublic);
		public static ImageReference MethodSealed { get; } = new ImageReference(assembly, DsImagesStrings.MethodSealed);
		public static ImageReference MethodShortcut { get; } = new ImageReference(assembly, DsImagesStrings.MethodShortcut);
		public static ImageReference ModuleFile { get; } = new ImageReference(assembly, DsImagesStrings.ModuleFile);
		public static ImageReference ModuleInternal { get; } = new ImageReference(assembly, DsImagesStrings.ModuleInternal);
		public static ImageReference ModulePrivate { get; } = new ImageReference(assembly, DsImagesStrings.ModulePrivate);
		public static ImageReference ModuleProtected { get; } = new ImageReference(assembly, DsImagesStrings.ModuleProtected);
		public static ImageReference ModulePublic { get; } = new ImageReference(assembly, DsImagesStrings.ModulePublic);
		public static ImageReference ModulesWindow { get; } = new ImageReference(assembly, DsImagesStrings.ModulesWindow);
		public static ImageReference MoveUp { get; } = new ImageReference(assembly, DsImagesStrings.MoveUp);
		public static ImageReference Namespace { get; } = new ImageReference(assembly, DsImagesStrings.Namespace);
		public static ImageReference NewClass { get; } = new ImageReference(assembly, DsImagesStrings.NewClass);
		public static ImageReference NewDocument { get; } = new ImageReference(assembly, DsImagesStrings.NewDocument);
		public static ImageReference NewEvent { get; } = new ImageReference(assembly, DsImagesStrings.NewEvent);
		public static ImageReference NewField { get; } = new ImageReference(assembly, DsImagesStrings.NewField);
		public static ImageReference NewImage { get; } = new ImageReference(assembly, DsImagesStrings.NewImage);
		public static ImageReference NewItem { get; } = new ImageReference(assembly, DsImagesStrings.NewItem);
		public static ImageReference NewMethod { get; } = new ImageReference(assembly, DsImagesStrings.NewMethod);
		public static ImageReference NewProperty { get; } = new ImageReference(assembly, DsImagesStrings.NewProperty);
		public static ImageReference NewWindow { get; } = new ImageReference(assembly, DsImagesStrings.NewWindow);
		public static ImageReference NuGet { get; } = new ImageReference(assembly, DsImagesStrings.NuGet);
		public static ImageReference OneLevelUp { get; } = new ImageReference(assembly, DsImagesStrings.OneLevelUp);
		public static ImageReference OpenFolder { get; } = new ImageReference(assembly, DsImagesStrings.OpenFolder);
		public static ImageReference OperatorInternal { get; } = new ImageReference(assembly, DsImagesStrings.OperatorInternal);
		public static ImageReference OperatorPrivate { get; } = new ImageReference(assembly, DsImagesStrings.OperatorPrivate);
		public static ImageReference OperatorProtected { get; } = new ImageReference(assembly, DsImagesStrings.OperatorProtected);
		public static ImageReference OperatorPublic { get; } = new ImageReference(assembly, DsImagesStrings.OperatorPublic);
		public static ImageReference OperatorSealed { get; } = new ImageReference(assembly, DsImagesStrings.OperatorSealed);
		public static ImageReference OperatorShortcut { get; } = new ImageReference(assembly, DsImagesStrings.OperatorShortcut);
		public static ImageReference Output { get; } = new ImageReference(assembly, DsImagesStrings.Output);
		public static ImageReference Parameter { get; } = new ImageReference(assembly, DsImagesStrings.Parameter);
		public static ImageReference Paste { get; } = new ImageReference(assembly, DsImagesStrings.Paste);
		public static ImageReference Pause { get; } = new ImageReference(assembly, DsImagesStrings.Pause);
		public static ImageReference Process { get; } = new ImageReference(assembly, DsImagesStrings.Process);
		public static ImageReference Property { get; } = new ImageReference(assembly, DsImagesStrings.Property);
		public static ImageReference PropertyInternal { get; } = new ImageReference(assembly, DsImagesStrings.PropertyInternal);
		public static ImageReference PropertyPrivate { get; } = new ImageReference(assembly, DsImagesStrings.PropertyPrivate);
		public static ImageReference PropertyProtected { get; } = new ImageReference(assembly, DsImagesStrings.PropertyProtected);
		public static ImageReference PropertySealed { get; } = new ImageReference(assembly, DsImagesStrings.PropertySealed);
		public static ImageReference PropertyShortcut { get; } = new ImageReference(assembly, DsImagesStrings.PropertyShortcut);
		public static ImageReference QuestionMark { get; } = new ImageReference(assembly, DsImagesStrings.QuestionMark);
		public static ImageReference Redo { get; } = new ImageReference(assembly, DsImagesStrings.Redo);
		public static ImageReference Reference { get; } = new ImageReference(assembly, DsImagesStrings.Reference);
		public static ImageReference Refresh { get; } = new ImageReference(assembly, DsImagesStrings.Refresh);
		public static ImageReference RemoveCommand { get; } = new ImageReference(assembly, DsImagesStrings.RemoveCommand);
		public static ImageReference Restart { get; } = new ImageReference(assembly, DsImagesStrings.Restart);
		public static ImageReference Run { get; } = new ImageReference(assembly, DsImagesStrings.Run);
		public static ImageReference RunOutline { get; } = new ImageReference(assembly, DsImagesStrings.RunOutline);
		public static ImageReference Save { get; } = new ImageReference(assembly, DsImagesStrings.Save);
		public static ImageReference SaveAll { get; } = new ImageReference(assembly, DsImagesStrings.SaveAll);
		public static ImageReference Search { get; } = new ImageReference(assembly, DsImagesStrings.Search);
		public static ImageReference Select { get; } = new ImageReference(assembly, DsImagesStrings.Select);
		public static ImageReference Settings { get; } = new ImageReference(assembly, DsImagesStrings.Settings);
		public static ImageReference Snippet { get; } = new ImageReference(assembly, DsImagesStrings.Snippet);
		public static ImageReference Solution { get; } = new ImageReference(assembly, DsImagesStrings.Solution);
		public static ImageReference SourceFileGroup { get; } = new ImageReference(assembly, DsImagesStrings.SourceFileGroup);
		public static ImageReference SplitScreenHorizontally { get; } = new ImageReference(assembly, DsImagesStrings.SplitScreenHorizontally);
		public static ImageReference SplitScreenVertically { get; } = new ImageReference(assembly, DsImagesStrings.SplitScreenVertically);
		public static ImageReference StatusError { get; } = new ImageReference(assembly, DsImagesStrings.StatusError);
		public static ImageReference StatusHidden { get; } = new ImageReference(assembly, DsImagesStrings.StatusHidden);
		public static ImageReference StatusInformation { get; } = new ImageReference(assembly, DsImagesStrings.StatusInformation);
		public static ImageReference StatusWarning { get; } = new ImageReference(assembly, DsImagesStrings.StatusWarning);
		public static ImageReference StepInto { get; } = new ImageReference(assembly, DsImagesStrings.StepInto);
		public static ImageReference StepOut { get; } = new ImageReference(assembly, DsImagesStrings.StepOut);
		public static ImageReference StepOver { get; } = new ImageReference(assembly, DsImagesStrings.StepOver);
		public static ImageReference Stop { get; } = new ImageReference(assembly, DsImagesStrings.Stop);
		public static ImageReference String { get; } = new ImageReference(assembly, DsImagesStrings.String);
		public static ImageReference StructureInternal { get; } = new ImageReference(assembly, DsImagesStrings.StructureInternal);
		public static ImageReference StructurePrivate { get; } = new ImageReference(assembly, DsImagesStrings.StructurePrivate);
		public static ImageReference StructureProtected { get; } = new ImageReference(assembly, DsImagesStrings.StructureProtected);
		public static ImageReference StructurePublic { get; } = new ImageReference(assembly, DsImagesStrings.StructurePublic);
		public static ImageReference StructureShortcut { get; } = new ImageReference(assembly, DsImagesStrings.StructureShortcut);
		public static ImageReference TableViewNameOnly { get; } = new ImageReference(assembly, DsImagesStrings.TableViewNameOnly);
		public static ImageReference Template { get; } = new ImageReference(assembly, DsImagesStrings.Template);
		public static ImageReference TemplateInternal { get; } = new ImageReference(assembly, DsImagesStrings.TemplateInternal);
		public static ImageReference TemplatePrivate { get; } = new ImageReference(assembly, DsImagesStrings.TemplatePrivate);
		public static ImageReference TemplateProtected { get; } = new ImageReference(assembly, DsImagesStrings.TemplateProtected);
		public static ImageReference TemplateShortcut { get; } = new ImageReference(assembly, DsImagesStrings.TemplateShortcut);
		public static ImageReference TextFile { get; } = new ImageReference(assembly, DsImagesStrings.TextFile);
		public static ImageReference Thread { get; } = new ImageReference(assembly, DsImagesStrings.Thread);
		public static ImageReference ToggleAllBreakpoints { get; } = new ImageReference(assembly, DsImagesStrings.ToggleAllBreakpoints);
		public static ImageReference ToolstripPanelBottom { get; } = new ImageReference(assembly, DsImagesStrings.ToolstripPanelBottom);
		public static ImageReference ToolstripPanelLeft { get; } = new ImageReference(assembly, DsImagesStrings.ToolstripPanelLeft);
		public static ImageReference ToolstripPanelRight { get; } = new ImageReference(assembly, DsImagesStrings.ToolstripPanelRight);
		public static ImageReference ToolstripPanelTop { get; } = new ImageReference(assembly, DsImagesStrings.ToolstripPanelTop);
		public static ImageReference Type { get; } = new ImageReference(assembly, DsImagesStrings.Type);
		public static ImageReference Undo { get; } = new ImageReference(assembly, DsImagesStrings.Undo);
		public static ImageReference UndoCheckBoxList { get; } = new ImageReference(assembly, DsImagesStrings.UndoCheckBoxList);
		public static ImageReference UserDefinedDataType { get; } = new ImageReference(assembly, DsImagesStrings.UserDefinedDataType);
		public static ImageReference VBFileNode { get; } = new ImageReference(assembly, DsImagesStrings.VBFileNode);
		public static ImageReference VBInteractiveWindow { get; } = new ImageReference(assembly, DsImagesStrings.VBInteractiveWindow);
		public static ImageReference VBProjectNode { get; } = new ImageReference(assembly, DsImagesStrings.VBProjectNode);
		public static ImageReference Watch { get; } = new ImageReference(assembly, DsImagesStrings.Watch);
		public static ImageReference WordWrap { get; } = new ImageReference(assembly, DsImagesStrings.WordWrap);
		public static ImageReference WPFFile { get; } = new ImageReference(assembly, DsImagesStrings.WPFFile);
		public static ImageReference XMLFile { get; } = new ImageReference(assembly, DsImagesStrings.XMLFile);
		public static ImageReference XMLSchema { get; } = new ImageReference(assembly, DsImagesStrings.XMLSchema);
		public static ImageReference XSLTransform { get; } = new ImageReference(assembly, DsImagesStrings.XSLTransform);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
