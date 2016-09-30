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
		public static ImageReference Add { get; } = new ImageReference(assembly, DsImageStrings.Add);
		public static ImageReference AddReference { get; } = new ImageReference(assembly, DsImageStrings.AddReference);
		public static ImageReference Assembly { get; } = new ImageReference(assembly, DsImageStrings.Assembly);
		public static ImageReference AssemblyError { get; } = new ImageReference(assembly, DsImageStrings.AssemblyError);
		public static ImageReference AssemblyExe { get; } = new ImageReference(assembly, DsImageStrings.AssemblyExe);
		public static ImageReference AutoSizeOptimize { get; } = new ImageReference(assembly, DsImageStrings.AutoSizeOptimize);
		public static ImageReference Backwards { get; } = new ImageReference(assembly, DsImageStrings.Backwards);
		public static ImageReference Binary { get; } = new ImageReference(assembly, DsImageStrings.Binary);
		public static ImageReference BinaryFile { get; } = new ImageReference(assembly, DsImageStrings.BinaryFile);
		public static ImageReference Branch { get; } = new ImageReference(assembly, DsImageStrings.Branch);
		public static ImageReference BreakpointDisabled { get; } = new ImageReference(assembly, DsImageStrings.BreakpointDisabled);
		public static ImageReference BreakpointEnabled { get; } = new ImageReference(assembly, DsImageStrings.BreakpointEnabled);
		public static ImageReference BreakpointsWindow { get; } = new ImageReference(assembly, DsImageStrings.BreakpointsWindow);
		public static ImageReference BuildSolution { get; } = new ImageReference(assembly, DsImageStrings.BuildSolution);
		public static ImageReference CallReturnInstructionPointer { get; } = new ImageReference(assembly, DsImageStrings.CallReturnInstructionPointer);
		public static ImageReference CallStackWindow { get; } = new ImageReference(assembly, DsImageStrings.CallStackWindow);
		public static ImageReference Cancel { get; } = new ImageReference(assembly, DsImageStrings.Cancel);
		public static ImageReference CheckDot { get; } = new ImageReference(assembly, DsImageStrings.CheckDot);
		public static ImageReference ClassInternal { get; } = new ImageReference(assembly, DsImageStrings.ClassInternal);
		public static ImageReference ClassPrivate { get; } = new ImageReference(assembly, DsImageStrings.ClassPrivate);
		public static ImageReference ClassProtected { get; } = new ImageReference(assembly, DsImageStrings.ClassProtected);
		public static ImageReference ClassPublic { get; } = new ImageReference(assembly, DsImageStrings.ClassPublic);
		public static ImageReference ClassShortcut { get; } = new ImageReference(assembly, DsImageStrings.ClassShortcut);
		public static ImageReference ClearBreakpointGroup { get; } = new ImageReference(assembly, DsImageStrings.ClearBreakpointGroup);
		public static ImageReference ClearWindowContent { get; } = new ImageReference(assembly, DsImageStrings.ClearWindowContent);
		public static ImageReference CloseAll { get; } = new ImageReference(assembly, DsImageStrings.CloseAll);
		public static ImageReference CloseDocumentGroup { get; } = new ImageReference(assembly, DsImageStrings.CloseDocumentGroup);
		public static ImageReference CloseSolution { get; } = new ImageReference(assembly, DsImageStrings.CloseSolution);
		public static ImageReference ConstantInternal { get; } = new ImageReference(assembly, DsImageStrings.ConstantInternal);
		public static ImageReference ConstantPrivate { get; } = new ImageReference(assembly, DsImageStrings.ConstantPrivate);
		public static ImageReference ConstantProtected { get; } = new ImageReference(assembly, DsImageStrings.ConstantProtected);
		public static ImageReference ConstantPublic { get; } = new ImageReference(assembly, DsImageStrings.ConstantPublic);
		public static ImageReference ConstantSealed { get; } = new ImageReference(assembly, DsImageStrings.ConstantSealed);
		public static ImageReference ConstantShortcut { get; } = new ImageReference(assembly, DsImageStrings.ConstantShortcut);
		public static ImageReference Copy { get; } = new ImageReference(assembly, DsImageStrings.Copy);
		public static ImageReference CopyItem { get; } = new ImageReference(assembly, DsImageStrings.CopyItem);
		public static ImageReference CSFileNode { get; } = new ImageReference(assembly, DsImageStrings.CSFileNode);
		public static ImageReference CSInteractiveWindow { get; } = new ImageReference(assembly, DsImageStrings.CSInteractiveWindow);
		public static ImageReference CSProjectNode { get; } = new ImageReference(assembly, DsImageStrings.CSProjectNode);
		public static ImageReference CurrentInstructionPointer { get; } = new ImageReference(assembly, DsImageStrings.CurrentInstructionPointer);
		public static ImageReference Cursor { get; } = new ImageReference(assembly, DsImageStrings.Cursor);
		public static ImageReference Cut { get; } = new ImageReference(assembly, DsImageStrings.Cut);
		public static ImageReference DelegateInternal { get; } = new ImageReference(assembly, DsImageStrings.DelegateInternal);
		public static ImageReference DelegatePrivate { get; } = new ImageReference(assembly, DsImageStrings.DelegatePrivate);
		public static ImageReference DelegateProtected { get; } = new ImageReference(assembly, DsImageStrings.DelegateProtected);
		public static ImageReference DelegatePublic { get; } = new ImageReference(assembly, DsImageStrings.DelegatePublic);
		public static ImageReference DelegateShortcut { get; } = new ImageReference(assembly, DsImageStrings.DelegateShortcut);
		public static ImageReference Dialog { get; } = new ImageReference(assembly, DsImageStrings.Dialog);
		public static ImageReference DisableAllBreakpoints { get; } = new ImageReference(assembly, DsImageStrings.DisableAllBreakpoints);
		public static ImageReference DisassemblyWindow { get; } = new ImageReference(assembly, DsImageStrings.DisassemblyWindow);
		public static ImageReference DownloadNoColor { get; } = new ImageReference(assembly, DsImageStrings.DownloadNoColor);
		public static ImageReference DraggedCurrentInstructionPointer { get; } = new ImageReference(assembly, DsImageStrings.DraggedCurrentInstructionPointer);
		public static ImageReference Editor { get; } = new ImageReference(assembly, DsImageStrings.Editor);
		public static ImageReference EnableAllBreakpoints { get; } = new ImageReference(assembly, DsImageStrings.EnableAllBreakpoints);
		public static ImageReference EntryPoint { get; } = new ImageReference(assembly, DsImageStrings.EntryPoint);
		public static ImageReference EnumerationInternal { get; } = new ImageReference(assembly, DsImageStrings.EnumerationInternal);
		public static ImageReference EnumerationItemInternal { get; } = new ImageReference(assembly, DsImageStrings.EnumerationItemInternal);
		public static ImageReference EnumerationItemPrivate { get; } = new ImageReference(assembly, DsImageStrings.EnumerationItemPrivate);
		public static ImageReference EnumerationItemProtected { get; } = new ImageReference(assembly, DsImageStrings.EnumerationItemProtected);
		public static ImageReference EnumerationItemPublic { get; } = new ImageReference(assembly, DsImageStrings.EnumerationItemPublic);
		public static ImageReference EnumerationItemSealed { get; } = new ImageReference(assembly, DsImageStrings.EnumerationItemSealed);
		public static ImageReference EnumerationItemShortcut { get; } = new ImageReference(assembly, DsImageStrings.EnumerationItemShortcut);
		public static ImageReference EnumerationPrivate { get; } = new ImageReference(assembly, DsImageStrings.EnumerationPrivate);
		public static ImageReference EnumerationProtected { get; } = new ImageReference(assembly, DsImageStrings.EnumerationProtected);
		public static ImageReference EnumerationPublic { get; } = new ImageReference(assembly, DsImageStrings.EnumerationPublic);
		public static ImageReference EnumerationShortcut { get; } = new ImageReference(assembly, DsImageStrings.EnumerationShortcut);
		public static ImageReference EventInternal { get; } = new ImageReference(assembly, DsImageStrings.EventInternal);
		public static ImageReference EventPrivate { get; } = new ImageReference(assembly, DsImageStrings.EventPrivate);
		public static ImageReference EventProtected { get; } = new ImageReference(assembly, DsImageStrings.EventProtected);
		public static ImageReference EventPublic { get; } = new ImageReference(assembly, DsImageStrings.EventPublic);
		public static ImageReference EventSealed { get; } = new ImageReference(assembly, DsImageStrings.EventSealed);
		public static ImageReference EventShortcut { get; } = new ImageReference(assembly, DsImageStrings.EventShortcut);
		public static ImageReference ExceptionInternal { get; } = new ImageReference(assembly, DsImageStrings.ExceptionInternal);
		public static ImageReference ExceptionPrivate { get; } = new ImageReference(assembly, DsImageStrings.ExceptionPrivate);
		public static ImageReference ExceptionProtected { get; } = new ImageReference(assembly, DsImageStrings.ExceptionProtected);
		public static ImageReference ExceptionPublic { get; } = new ImageReference(assembly, DsImageStrings.ExceptionPublic);
		public static ImageReference ExceptionSettings { get; } = new ImageReference(assembly, DsImageStrings.ExceptionSettings);
		public static ImageReference ExceptionShortcut { get; } = new ImageReference(assembly, DsImageStrings.ExceptionShortcut);
		public static ImageReference ExtensionMethod { get; } = new ImageReference(assembly, DsImageStrings.ExtensionMethod);
		public static ImageReference FieldInternal { get; } = new ImageReference(assembly, DsImageStrings.FieldInternal);
		public static ImageReference FieldPrivate { get; } = new ImageReference(assembly, DsImageStrings.FieldPrivate);
		public static ImageReference FieldProtected { get; } = new ImageReference(assembly, DsImageStrings.FieldProtected);
		public static ImageReference FieldPublic { get; } = new ImageReference(assembly, DsImageStrings.FieldPublic);
		public static ImageReference FieldSealed { get; } = new ImageReference(assembly, DsImageStrings.FieldSealed);
		public static ImageReference FieldShortcut { get; } = new ImageReference(assembly, DsImageStrings.FieldShortcut);
		public static ImageReference Fill { get; } = new ImageReference(assembly, DsImageStrings.Fill);
		public static ImageReference Filter { get; } = new ImageReference(assembly, DsImageStrings.Filter);
		public static ImageReference FolderClosed { get; } = new ImageReference(assembly, DsImageStrings.FolderClosed);
		public static ImageReference FolderOpened { get; } = new ImageReference(assembly, DsImageStrings.FolderOpened);
		public static ImageReference Forwards { get; } = new ImageReference(assembly, DsImageStrings.Forwards);
		public static ImageReference GoToNext { get; } = new ImageReference(assembly, DsImageStrings.GoToNext);
		public static ImageReference GoToNextInList { get; } = new ImageReference(assembly, DsImageStrings.GoToNextInList);
		public static ImageReference GoToSourceCode { get; } = new ImageReference(assembly, DsImageStrings.GoToSourceCode);
		public static ImageReference Image { get; } = new ImageReference(assembly, DsImageStrings.Image);
		public static ImageReference IntellisenseKeyword { get; } = new ImageReference(assembly, DsImageStrings.IntellisenseKeyword);
		public static ImageReference InterfaceInternal { get; } = new ImageReference(assembly, DsImageStrings.InterfaceInternal);
		public static ImageReference InterfacePrivate { get; } = new ImageReference(assembly, DsImageStrings.InterfacePrivate);
		public static ImageReference InterfaceProtected { get; } = new ImageReference(assembly, DsImageStrings.InterfaceProtected);
		public static ImageReference InterfacePublic { get; } = new ImageReference(assembly, DsImageStrings.InterfacePublic);
		public static ImageReference InterfaceShortcut { get; } = new ImageReference(assembly, DsImageStrings.InterfaceShortcut);
		public static ImageReference Label { get; } = new ImageReference(assembly, DsImageStrings.Label);
		public static ImageReference Library { get; } = new ImageReference(assembly, DsImageStrings.Library);
		public static ImageReference LocalsWindow { get; } = new ImageReference(assembly, DsImageStrings.LocalsWindow);
		public static ImageReference LocalVariable { get; } = new ImageReference(assembly, DsImageStrings.LocalVariable);
		public static ImageReference MarkupTag { get; } = new ImageReference(assembly, DsImageStrings.MarkupTag);
		public static ImageReference MemoryWindow { get; } = new ImageReference(assembly, DsImageStrings.MemoryWindow);
		public static ImageReference Metadata { get; } = new ImageReference(assembly, DsImageStrings.Metadata);
		public static ImageReference MethodInternal { get; } = new ImageReference(assembly, DsImageStrings.MethodInternal);
		public static ImageReference MethodPrivate { get; } = new ImageReference(assembly, DsImageStrings.MethodPrivate);
		public static ImageReference MethodProtected { get; } = new ImageReference(assembly, DsImageStrings.MethodProtected);
		public static ImageReference MethodPublic { get; } = new ImageReference(assembly, DsImageStrings.MethodPublic);
		public static ImageReference MethodSealed { get; } = new ImageReference(assembly, DsImageStrings.MethodSealed);
		public static ImageReference MethodShortcut { get; } = new ImageReference(assembly, DsImageStrings.MethodShortcut);
		public static ImageReference ModuleFile { get; } = new ImageReference(assembly, DsImageStrings.ModuleFile);
		public static ImageReference ModuleInternal { get; } = new ImageReference(assembly, DsImageStrings.ModuleInternal);
		public static ImageReference ModulePrivate { get; } = new ImageReference(assembly, DsImageStrings.ModulePrivate);
		public static ImageReference ModuleProtected { get; } = new ImageReference(assembly, DsImageStrings.ModuleProtected);
		public static ImageReference ModulePublic { get; } = new ImageReference(assembly, DsImageStrings.ModulePublic);
		public static ImageReference ModulesWindow { get; } = new ImageReference(assembly, DsImageStrings.ModulesWindow);
		public static ImageReference MoveUp { get; } = new ImageReference(assembly, DsImageStrings.MoveUp);
		public static ImageReference Namespace { get; } = new ImageReference(assembly, DsImageStrings.Namespace);
		public static ImageReference NewClass { get; } = new ImageReference(assembly, DsImageStrings.NewClass);
		public static ImageReference NewDocument { get; } = new ImageReference(assembly, DsImageStrings.NewDocument);
		public static ImageReference NewEvent { get; } = new ImageReference(assembly, DsImageStrings.NewEvent);
		public static ImageReference NewField { get; } = new ImageReference(assembly, DsImageStrings.NewField);
		public static ImageReference NewImage { get; } = new ImageReference(assembly, DsImageStrings.NewImage);
		public static ImageReference NewItem { get; } = new ImageReference(assembly, DsImageStrings.NewItem);
		public static ImageReference NewMethod { get; } = new ImageReference(assembly, DsImageStrings.NewMethod);
		public static ImageReference NewProperty { get; } = new ImageReference(assembly, DsImageStrings.NewProperty);
		public static ImageReference NewWindow { get; } = new ImageReference(assembly, DsImageStrings.NewWindow);
		public static ImageReference NuGet { get; } = new ImageReference(assembly, DsImageStrings.NuGet);
		public static ImageReference OneLevelUp { get; } = new ImageReference(assembly, DsImageStrings.OneLevelUp);
		public static ImageReference OpenFolder { get; } = new ImageReference(assembly, DsImageStrings.OpenFolder);
		public static ImageReference OperatorInternal { get; } = new ImageReference(assembly, DsImageStrings.OperatorInternal);
		public static ImageReference OperatorPrivate { get; } = new ImageReference(assembly, DsImageStrings.OperatorPrivate);
		public static ImageReference OperatorProtected { get; } = new ImageReference(assembly, DsImageStrings.OperatorProtected);
		public static ImageReference OperatorPublic { get; } = new ImageReference(assembly, DsImageStrings.OperatorPublic);
		public static ImageReference OperatorSealed { get; } = new ImageReference(assembly, DsImageStrings.OperatorSealed);
		public static ImageReference OperatorShortcut { get; } = new ImageReference(assembly, DsImageStrings.OperatorShortcut);
		public static ImageReference Output { get; } = new ImageReference(assembly, DsImageStrings.Output);
		public static ImageReference Parameter { get; } = new ImageReference(assembly, DsImageStrings.Parameter);
		public static ImageReference Paste { get; } = new ImageReference(assembly, DsImageStrings.Paste);
		public static ImageReference Pause { get; } = new ImageReference(assembly, DsImageStrings.Pause);
		public static ImageReference Process { get; } = new ImageReference(assembly, DsImageStrings.Process);
		public static ImageReference Property { get; } = new ImageReference(assembly, DsImageStrings.Property);
		public static ImageReference PropertyInternal { get; } = new ImageReference(assembly, DsImageStrings.PropertyInternal);
		public static ImageReference PropertyPrivate { get; } = new ImageReference(assembly, DsImageStrings.PropertyPrivate);
		public static ImageReference PropertyProtected { get; } = new ImageReference(assembly, DsImageStrings.PropertyProtected);
		public static ImageReference PropertySealed { get; } = new ImageReference(assembly, DsImageStrings.PropertySealed);
		public static ImageReference PropertyShortcut { get; } = new ImageReference(assembly, DsImageStrings.PropertyShortcut);
		public static ImageReference QuestionMark { get; } = new ImageReference(assembly, DsImageStrings.QuestionMark);
		public static ImageReference Redo { get; } = new ImageReference(assembly, DsImageStrings.Redo);
		public static ImageReference Reference { get; } = new ImageReference(assembly, DsImageStrings.Reference);
		public static ImageReference Refresh { get; } = new ImageReference(assembly, DsImageStrings.Refresh);
		public static ImageReference RemoveCommand { get; } = new ImageReference(assembly, DsImageStrings.RemoveCommand);
		public static ImageReference Restart { get; } = new ImageReference(assembly, DsImageStrings.Restart);
		public static ImageReference Run { get; } = new ImageReference(assembly, DsImageStrings.Run);
		public static ImageReference RunOutline { get; } = new ImageReference(assembly, DsImageStrings.RunOutline);
		public static ImageReference Save { get; } = new ImageReference(assembly, DsImageStrings.Save);
		public static ImageReference SaveAll { get; } = new ImageReference(assembly, DsImageStrings.SaveAll);
		public static ImageReference Search { get; } = new ImageReference(assembly, DsImageStrings.Search);
		public static ImageReference Select { get; } = new ImageReference(assembly, DsImageStrings.Select);
		public static ImageReference Settings { get; } = new ImageReference(assembly, DsImageStrings.Settings);
		public static ImageReference Snippet { get; } = new ImageReference(assembly, DsImageStrings.Snippet);
		public static ImageReference Solution { get; } = new ImageReference(assembly, DsImageStrings.Solution);
		public static ImageReference SourceFileGroup { get; } = new ImageReference(assembly, DsImageStrings.SourceFileGroup);
		public static ImageReference SplitScreenHorizontally { get; } = new ImageReference(assembly, DsImageStrings.SplitScreenHorizontally);
		public static ImageReference SplitScreenVertically { get; } = new ImageReference(assembly, DsImageStrings.SplitScreenVertically);
		public static ImageReference StatusError { get; } = new ImageReference(assembly, DsImageStrings.StatusError);
		public static ImageReference StatusHidden { get; } = new ImageReference(assembly, DsImageStrings.StatusHidden);
		public static ImageReference StatusInformation { get; } = new ImageReference(assembly, DsImageStrings.StatusInformation);
		public static ImageReference StatusWarning { get; } = new ImageReference(assembly, DsImageStrings.StatusWarning);
		public static ImageReference StepInto { get; } = new ImageReference(assembly, DsImageStrings.StepInto);
		public static ImageReference StepOut { get; } = new ImageReference(assembly, DsImageStrings.StepOut);
		public static ImageReference StepOver { get; } = new ImageReference(assembly, DsImageStrings.StepOver);
		public static ImageReference Stop { get; } = new ImageReference(assembly, DsImageStrings.Stop);
		public static ImageReference String { get; } = new ImageReference(assembly, DsImageStrings.String);
		public static ImageReference StructureInternal { get; } = new ImageReference(assembly, DsImageStrings.StructureInternal);
		public static ImageReference StructurePrivate { get; } = new ImageReference(assembly, DsImageStrings.StructurePrivate);
		public static ImageReference StructureProtected { get; } = new ImageReference(assembly, DsImageStrings.StructureProtected);
		public static ImageReference StructurePublic { get; } = new ImageReference(assembly, DsImageStrings.StructurePublic);
		public static ImageReference StructureShortcut { get; } = new ImageReference(assembly, DsImageStrings.StructureShortcut);
		public static ImageReference TableViewNameOnly { get; } = new ImageReference(assembly, DsImageStrings.TableViewNameOnly);
		public static ImageReference Template { get; } = new ImageReference(assembly, DsImageStrings.Template);
		public static ImageReference TemplateInternal { get; } = new ImageReference(assembly, DsImageStrings.TemplateInternal);
		public static ImageReference TemplatePrivate { get; } = new ImageReference(assembly, DsImageStrings.TemplatePrivate);
		public static ImageReference TemplateProtected { get; } = new ImageReference(assembly, DsImageStrings.TemplateProtected);
		public static ImageReference TemplateShortcut { get; } = new ImageReference(assembly, DsImageStrings.TemplateShortcut);
		public static ImageReference TextFile { get; } = new ImageReference(assembly, DsImageStrings.TextFile);
		public static ImageReference Thread { get; } = new ImageReference(assembly, DsImageStrings.Thread);
		public static ImageReference ToggleAllBreakpoints { get; } = new ImageReference(assembly, DsImageStrings.ToggleAllBreakpoints);
		public static ImageReference ToolstripPanelBottom { get; } = new ImageReference(assembly, DsImageStrings.ToolstripPanelBottom);
		public static ImageReference ToolstripPanelLeft { get; } = new ImageReference(assembly, DsImageStrings.ToolstripPanelLeft);
		public static ImageReference ToolstripPanelRight { get; } = new ImageReference(assembly, DsImageStrings.ToolstripPanelRight);
		public static ImageReference ToolstripPanelTop { get; } = new ImageReference(assembly, DsImageStrings.ToolstripPanelTop);
		public static ImageReference Type { get; } = new ImageReference(assembly, DsImageStrings.Type);
		public static ImageReference Undo { get; } = new ImageReference(assembly, DsImageStrings.Undo);
		public static ImageReference UndoCheckBoxList { get; } = new ImageReference(assembly, DsImageStrings.UndoCheckBoxList);
		public static ImageReference UserDefinedDataType { get; } = new ImageReference(assembly, DsImageStrings.UserDefinedDataType);
		public static ImageReference VBFileNode { get; } = new ImageReference(assembly, DsImageStrings.VBFileNode);
		public static ImageReference VBInteractiveWindow { get; } = new ImageReference(assembly, DsImageStrings.VBInteractiveWindow);
		public static ImageReference VBProjectNode { get; } = new ImageReference(assembly, DsImageStrings.VBProjectNode);
		public static ImageReference Watch { get; } = new ImageReference(assembly, DsImageStrings.Watch);
		public static ImageReference WordWrap { get; } = new ImageReference(assembly, DsImageStrings.WordWrap);
		public static ImageReference WPFFile { get; } = new ImageReference(assembly, DsImageStrings.WPFFile);
		public static ImageReference XMLFile { get; } = new ImageReference(assembly, DsImageStrings.XMLFile);
		public static ImageReference XMLSchema { get; } = new ImageReference(assembly, DsImageStrings.XMLSchema);
		public static ImageReference XSLTransform { get; } = new ImageReference(assembly, DsImageStrings.XSLTransform);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
