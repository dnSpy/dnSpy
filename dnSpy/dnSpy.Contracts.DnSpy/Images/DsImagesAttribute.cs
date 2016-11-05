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

namespace dnSpy.Contracts.Images {
	/// <summary>
	/// Contains image reference strings that can be used in attributes, eg. menu item attributes
	/// </summary>
	public static class DsImagesAttribute {
		const string Prefix = "img:" + DsImages.ImagesAssemblyName + ",";
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public const string Add = Prefix + DsImageStrings.Add;
		public const string AddReference = Prefix + DsImageStrings.AddReference;
		public const string Assembly = Prefix + DsImageStrings.Assembly;
		public const string AssemblyError = Prefix + DsImageStrings.AssemblyError;
		public const string AssemblyExe = Prefix + DsImageStrings.AssemblyExe;
		public const string AutoSizeOptimize = Prefix + DsImageStrings.AutoSizeOptimize;
		public const string Backwards = Prefix + DsImageStrings.Backwards;
		public const string Binary = Prefix + DsImageStrings.Binary;
		public const string BinaryFile = Prefix + DsImageStrings.BinaryFile;
		public const string Branch = Prefix + DsImageStrings.Branch;
		public const string BreakpointDisabled = Prefix + DsImageStrings.BreakpointDisabled;
		public const string BreakpointEnabled = Prefix + DsImageStrings.BreakpointEnabled;
		public const string BreakpointsWindow = Prefix + DsImageStrings.BreakpointsWindow;
		public const string BuildSolution = Prefix + DsImageStrings.BuildSolution;
		public const string CallReturnInstructionPointer = Prefix + DsImageStrings.CallReturnInstructionPointer;
		public const string CallStackWindow = Prefix + DsImageStrings.CallStackWindow;
		public const string Cancel = Prefix + DsImageStrings.Cancel;
		public const string CheckDot = Prefix + DsImageStrings.CheckDot;
		public const string ClassInternal = Prefix + DsImageStrings.ClassInternal;
		public const string ClassPrivate = Prefix + DsImageStrings.ClassPrivate;
		public const string ClassProtected = Prefix + DsImageStrings.ClassProtected;
		public const string ClassPublic = Prefix + DsImageStrings.ClassPublic;
		public const string ClassShortcut = Prefix + DsImageStrings.ClassShortcut;
		public const string ClearBreakpointGroup = Prefix + DsImageStrings.ClearBreakpointGroup;
		public const string ClearWindowContent = Prefix + DsImageStrings.ClearWindowContent;
		public const string CloseAll = Prefix + DsImageStrings.CloseAll;
		public const string CloseDocumentGroup = Prefix + DsImageStrings.CloseDocumentGroup;
		public const string CloseSolution = Prefix + DsImageStrings.CloseSolution;
		public const string ConstantInternal = Prefix + DsImageStrings.ConstantInternal;
		public const string ConstantPrivate = Prefix + DsImageStrings.ConstantPrivate;
		public const string ConstantProtected = Prefix + DsImageStrings.ConstantProtected;
		public const string ConstantPublic = Prefix + DsImageStrings.ConstantPublic;
		public const string ConstantSealed = Prefix + DsImageStrings.ConstantSealed;
		public const string ConstantShortcut = Prefix + DsImageStrings.ConstantShortcut;
		public const string Copy = Prefix + DsImageStrings.Copy;
		public const string CopyItem = Prefix + DsImageStrings.CopyItem;
		public const string CSFileNode = Prefix + DsImageStrings.CSFileNode;
		public const string CSInteractiveWindow = Prefix + DsImageStrings.CSInteractiveWindow;
		public const string CSProjectNode = Prefix + DsImageStrings.CSProjectNode;
		public const string CurrentInstructionPointer = Prefix + DsImageStrings.CurrentInstructionPointer;
		public const string Cursor = Prefix + DsImageStrings.Cursor;
		public const string Cut = Prefix + DsImageStrings.Cut;
		public const string DelegateInternal = Prefix + DsImageStrings.DelegateInternal;
		public const string DelegatePrivate = Prefix + DsImageStrings.DelegatePrivate;
		public const string DelegateProtected = Prefix + DsImageStrings.DelegateProtected;
		public const string DelegatePublic = Prefix + DsImageStrings.DelegatePublic;
		public const string DelegateShortcut = Prefix + DsImageStrings.DelegateShortcut;
		public const string Dialog = Prefix + DsImageStrings.Dialog;
		public const string DisableAllBreakpoints = Prefix + DsImageStrings.DisableAllBreakpoints;
		public const string DisassemblyWindow = Prefix + DsImageStrings.DisassemblyWindow;
		public const string DownloadNoColor = Prefix + DsImageStrings.DownloadNoColor;
		public const string DraggedCurrentInstructionPointer = Prefix + DsImageStrings.DraggedCurrentInstructionPointer;
		public const string Editor = Prefix + DsImageStrings.Editor;
		public const string EnableAllBreakpoints = Prefix + DsImageStrings.EnableAllBreakpoints;
		public const string EntryPoint = Prefix + DsImageStrings.EntryPoint;
		public const string EnumerationInternal = Prefix + DsImageStrings.EnumerationInternal;
		public const string EnumerationItemInternal = Prefix + DsImageStrings.EnumerationItemInternal;
		public const string EnumerationItemPrivate = Prefix + DsImageStrings.EnumerationItemPrivate;
		public const string EnumerationItemProtected = Prefix + DsImageStrings.EnumerationItemProtected;
		public const string EnumerationItemPublic = Prefix + DsImageStrings.EnumerationItemPublic;
		public const string EnumerationItemSealed = Prefix + DsImageStrings.EnumerationItemSealed;
		public const string EnumerationItemShortcut = Prefix + DsImageStrings.EnumerationItemShortcut;
		public const string EnumerationPrivate = Prefix + DsImageStrings.EnumerationPrivate;
		public const string EnumerationProtected = Prefix + DsImageStrings.EnumerationProtected;
		public const string EnumerationPublic = Prefix + DsImageStrings.EnumerationPublic;
		public const string EnumerationShortcut = Prefix + DsImageStrings.EnumerationShortcut;
		public const string EventInternal = Prefix + DsImageStrings.EventInternal;
		public const string EventPrivate = Prefix + DsImageStrings.EventPrivate;
		public const string EventProtected = Prefix + DsImageStrings.EventProtected;
		public const string EventPublic = Prefix + DsImageStrings.EventPublic;
		public const string EventSealed = Prefix + DsImageStrings.EventSealed;
		public const string EventShortcut = Prefix + DsImageStrings.EventShortcut;
		public const string ExceptionInternal = Prefix + DsImageStrings.ExceptionInternal;
		public const string ExceptionPrivate = Prefix + DsImageStrings.ExceptionPrivate;
		public const string ExceptionProtected = Prefix + DsImageStrings.ExceptionProtected;
		public const string ExceptionPublic = Prefix + DsImageStrings.ExceptionPublic;
		public const string ExceptionSettings = Prefix + DsImageStrings.ExceptionSettings;
		public const string ExceptionShortcut = Prefix + DsImageStrings.ExceptionShortcut;
		public const string ExtensionMethod = Prefix + DsImageStrings.ExtensionMethod;
		public const string FieldInternal = Prefix + DsImageStrings.FieldInternal;
		public const string FieldPrivate = Prefix + DsImageStrings.FieldPrivate;
		public const string FieldProtected = Prefix + DsImageStrings.FieldProtected;
		public const string FieldPublic = Prefix + DsImageStrings.FieldPublic;
		public const string FieldSealed = Prefix + DsImageStrings.FieldSealed;
		public const string FieldShortcut = Prefix + DsImageStrings.FieldShortcut;
		public const string Fill = Prefix + DsImageStrings.Fill;
		public const string Filter = Prefix + DsImageStrings.Filter;
		public const string FolderClosed = Prefix + DsImageStrings.FolderClosed;
		public const string FolderOpened = Prefix + DsImageStrings.FolderOpened;
		public const string Forwards = Prefix + DsImageStrings.Forwards;
		public const string GoToNext = Prefix + DsImageStrings.GoToNext;
		public const string GoToNextInList = Prefix + DsImageStrings.GoToNextInList;
		public const string GoToSourceCode = Prefix + DsImageStrings.GoToSourceCode;
		public const string Image = Prefix + DsImageStrings.Image;
		public const string IntellisenseKeyword = Prefix + DsImageStrings.IntellisenseKeyword;
		public const string InterfaceInternal = Prefix + DsImageStrings.InterfaceInternal;
		public const string InterfacePrivate = Prefix + DsImageStrings.InterfacePrivate;
		public const string InterfaceProtected = Prefix + DsImageStrings.InterfaceProtected;
		public const string InterfacePublic = Prefix + DsImageStrings.InterfacePublic;
		public const string InterfaceShortcut = Prefix + DsImageStrings.InterfaceShortcut;
		public const string Label = Prefix + DsImageStrings.Label;
		public const string Library = Prefix + DsImageStrings.Library;
		public const string LocalsWindow = Prefix + DsImageStrings.LocalsWindow;
		public const string LocalVariable = Prefix + DsImageStrings.LocalVariable;
		public const string MarkupTag = Prefix + DsImageStrings.MarkupTag;
		public const string MemoryWindow = Prefix + DsImageStrings.MemoryWindow;
		public const string Metadata = Prefix + DsImageStrings.Metadata;
		public const string MethodInternal = Prefix + DsImageStrings.MethodInternal;
		public const string MethodPrivate = Prefix + DsImageStrings.MethodPrivate;
		public const string MethodProtected = Prefix + DsImageStrings.MethodProtected;
		public const string MethodPublic = Prefix + DsImageStrings.MethodPublic;
		public const string MethodSealed = Prefix + DsImageStrings.MethodSealed;
		public const string MethodShortcut = Prefix + DsImageStrings.MethodShortcut;
		public const string ModuleFile = Prefix + DsImageStrings.ModuleFile;
		public const string ModuleInternal = Prefix + DsImageStrings.ModuleInternal;
		public const string ModulePrivate = Prefix + DsImageStrings.ModulePrivate;
		public const string ModuleProtected = Prefix + DsImageStrings.ModuleProtected;
		public const string ModulePublic = Prefix + DsImageStrings.ModulePublic;
		public const string ModulesWindow = Prefix + DsImageStrings.ModulesWindow;
		public const string MoveUp = Prefix + DsImageStrings.MoveUp;
		public const string Namespace = Prefix + DsImageStrings.Namespace;
		public const string NewClass = Prefix + DsImageStrings.NewClass;
		public const string NewDocument = Prefix + DsImageStrings.NewDocument;
		public const string NewEvent = Prefix + DsImageStrings.NewEvent;
		public const string NewField = Prefix + DsImageStrings.NewField;
		public const string NewImage = Prefix + DsImageStrings.NewImage;
		public const string NewItem = Prefix + DsImageStrings.NewItem;
		public const string NewMethod = Prefix + DsImageStrings.NewMethod;
		public const string NewProperty = Prefix + DsImageStrings.NewProperty;
		public const string NewWindow = Prefix + DsImageStrings.NewWindow;
		public const string NuGet = Prefix + DsImageStrings.NuGet;
		public const string OneLevelUp = Prefix + DsImageStrings.OneLevelUp;
		public const string OpenFolder = Prefix + DsImageStrings.OpenFolder;
		public const string OperatorInternal = Prefix + DsImageStrings.OperatorInternal;
		public const string OperatorPrivate = Prefix + DsImageStrings.OperatorPrivate;
		public const string OperatorProtected = Prefix + DsImageStrings.OperatorProtected;
		public const string OperatorPublic = Prefix + DsImageStrings.OperatorPublic;
		public const string OperatorSealed = Prefix + DsImageStrings.OperatorSealed;
		public const string OperatorShortcut = Prefix + DsImageStrings.OperatorShortcut;
		public const string Output = Prefix + DsImageStrings.Output;
		public const string Parameter = Prefix + DsImageStrings.Parameter;
		public const string Paste = Prefix + DsImageStrings.Paste;
		public const string Pause = Prefix + DsImageStrings.Pause;
		public const string Process = Prefix + DsImageStrings.Process;
		public const string Property = Prefix + DsImageStrings.Property;
		public const string PropertyInternal = Prefix + DsImageStrings.PropertyInternal;
		public const string PropertyPrivate = Prefix + DsImageStrings.PropertyPrivate;
		public const string PropertyProtected = Prefix + DsImageStrings.PropertyProtected;
		public const string PropertySealed = Prefix + DsImageStrings.PropertySealed;
		public const string PropertyShortcut = Prefix + DsImageStrings.PropertyShortcut;
		public const string QuestionMark = Prefix + DsImageStrings.QuestionMark;
		public const string Redo = Prefix + DsImageStrings.Redo;
		public const string Reference = Prefix + DsImageStrings.Reference;
		public const string Refresh = Prefix + DsImageStrings.Refresh;
		public const string RemoveCommand = Prefix + DsImageStrings.RemoveCommand;
		public const string Restart = Prefix + DsImageStrings.Restart;
		public const string Run = Prefix + DsImageStrings.Run;
		public const string RunOutline = Prefix + DsImageStrings.RunOutline;
		public const string Save = Prefix + DsImageStrings.Save;
		public const string SaveAll = Prefix + DsImageStrings.SaveAll;
		public const string Search = Prefix + DsImageStrings.Search;
		public const string Select = Prefix + DsImageStrings.Select;
		public const string Settings = Prefix + DsImageStrings.Settings;
		public const string Snippet = Prefix + DsImageStrings.Snippet;
		public const string Solution = Prefix + DsImageStrings.Solution;
		public const string SortAscending = Prefix + DsImageStrings.SortAscending;
		public const string SourceFileGroup = Prefix + DsImageStrings.SourceFileGroup;
		public const string SplitScreenHorizontally = Prefix + DsImageStrings.SplitScreenHorizontally;
		public const string SplitScreenVertically = Prefix + DsImageStrings.SplitScreenVertically;
		public const string StatusError = Prefix + DsImageStrings.StatusError;
		public const string StatusHidden = Prefix + DsImageStrings.StatusHidden;
		public const string StatusInformation = Prefix + DsImageStrings.StatusInformation;
		public const string StatusWarning = Prefix + DsImageStrings.StatusWarning;
		public const string StepInto = Prefix + DsImageStrings.StepInto;
		public const string StepOut = Prefix + DsImageStrings.StepOut;
		public const string StepOver = Prefix + DsImageStrings.StepOver;
		public const string Stop = Prefix + DsImageStrings.Stop;
		public const string String = Prefix + DsImageStrings.String;
		public const string StructureInternal = Prefix + DsImageStrings.StructureInternal;
		public const string StructurePrivate = Prefix + DsImageStrings.StructurePrivate;
		public const string StructureProtected = Prefix + DsImageStrings.StructureProtected;
		public const string StructurePublic = Prefix + DsImageStrings.StructurePublic;
		public const string StructureShortcut = Prefix + DsImageStrings.StructureShortcut;
		public const string TableViewNameOnly = Prefix + DsImageStrings.TableViewNameOnly;
		public const string Template = Prefix + DsImageStrings.Template;
		public const string TemplateInternal = Prefix + DsImageStrings.TemplateInternal;
		public const string TemplatePrivate = Prefix + DsImageStrings.TemplatePrivate;
		public const string TemplateProtected = Prefix + DsImageStrings.TemplateProtected;
		public const string TemplateShortcut = Prefix + DsImageStrings.TemplateShortcut;
		public const string TextFile = Prefix + DsImageStrings.TextFile;
		public const string Thread = Prefix + DsImageStrings.Thread;
		public const string ToggleAllBreakpoints = Prefix + DsImageStrings.ToggleAllBreakpoints;
		public const string ToolstripPanelBottom = Prefix + DsImageStrings.ToolstripPanelBottom;
		public const string ToolstripPanelLeft = Prefix + DsImageStrings.ToolstripPanelLeft;
		public const string ToolstripPanelRight = Prefix + DsImageStrings.ToolstripPanelRight;
		public const string ToolstripPanelTop = Prefix + DsImageStrings.ToolstripPanelTop;
		public const string Type = Prefix + DsImageStrings.Type;
		public const string Undo = Prefix + DsImageStrings.Undo;
		public const string UndoCheckBoxList = Prefix + DsImageStrings.UndoCheckBoxList;
		public const string UserDefinedDataType = Prefix + DsImageStrings.UserDefinedDataType;
		public const string VBFileNode = Prefix + DsImageStrings.VBFileNode;
		public const string VBInteractiveWindow = Prefix + DsImageStrings.VBInteractiveWindow;
		public const string VBProjectNode = Prefix + DsImageStrings.VBProjectNode;
		public const string Watch = Prefix + DsImageStrings.Watch;
		public const string WordWrap = Prefix + DsImageStrings.WordWrap;
		public const string WPFFile = Prefix + DsImageStrings.WPFFile;
		public const string XMLFile = Prefix + DsImageStrings.XMLFile;
		public const string XMLSchema = Prefix + DsImageStrings.XMLSchema;
		public const string XSLTransform = Prefix + DsImageStrings.XSLTransform;
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
	}
}
