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

using System.Windows.Controls;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Search;
using dnSpy.Contracts.Tabs;
using dnSpy.Contracts.ToolWindows;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Menus {
	/// <summary>
	/// Constants
	/// </summary>
	public static class MenuConstants {
		/// <summary>Guid of context menu</summary>
		public const string CTX_MENU_GUID = "CB53CCAF-9EE3-411E-A03A-561E7D8470EC";

		/// <summary>Guid of app menu</summary>
		public const string APP_MENU_GUID = "3D87660F-DA21-48B9-9022-C76F0E588E1F";

		/// <summary>Guid of app menu: File</summary>
		public const string APP_MENU_FILE_GUID = "DC3B8109-21BB-40E8-9999-FC6526C3DD15";

		/// <summary>Guid of app menu: Edit</summary>
		public const string APP_MENU_EDIT_GUID = "BC6AE088-F941-4F4B-B976-42A09866C94A";

		/// <summary>Guid of app menu: View</summary>
		public const string APP_MENU_VIEW_GUID = "235BDFD8-A065-4E89-B041-C40A90526AF9";

		/// <summary>Guid of app menu: Themes</summary>
		public const string APP_MENU_THEMES_GUID = "D34C16A1-1940-4EAD-A4CD-3E00148E5FB3";

		/// <summary>Guid of app menu: Debug</summary>
		public const string APP_MENU_DEBUG_GUID = "62B311D0-D77E-4718-86C3-14BA031C47DF";

		/// <summary>Guid of app menu: Window</summary>
		public const string APP_MENU_WINDOW_GUID = "5904BD1D-1EF3-424F-B531-FE6BCF2FC9D4";

		/// <summary>Guid of app menu: Help</summary>
		public const string APP_MENU_HELP_GUID = "52504C1B-7C35-464A-A35D-6D9F59E035D9";

		/// <summary>Guid of text editor icon bar</summary>
		public const string TEXTEDITOR_ICONBAR_GUID = "998FA81E-5B86-43C1-A043-41CDDE090477";

		/// <summary>App menu order: File</summary>
		public const double ORDER_APP_MENU_FILE = 0;

		/// <summary>App menu order: Edit</summary>
		public const double ORDER_APP_MENU_EDIT = 1000;

		/// <summary>App menu order: View</summary>
		public const double ORDER_APP_MENU_VIEW = 2000;

		/// <summary>App menu order: Themes</summary>
		public const double ORDER_APP_MENU_THEMES = 3000;

		/// <summary>App menu order: Debug</summary>
		public const double ORDER_APP_MENU_DEBUG = 4000;

		/// <summary>App menu order: Window</summary>
		public const double ORDER_APP_MENU_WINDOW = 1000000;

		/// <summary>App menu order: Help</summary>
		public const double ORDER_APP_MENU_HELP = 1001000;

		/// <summary>An unknown object</summary>
		public static readonly string GUIDOBJ_UNKNOWN_GUID = "9BD7C228-91A0-4140-8E8B-AB0450B418CA";

		/// <summary>Files treeview</summary>
		public static readonly string GUIDOBJ_FILES_TREEVIEW_GUID = "F64505EB-6D8B-4332-B697-73B2D1EE6C37";

		/// <summary>Analyzer's treeview</summary>
		public static readonly string GUIDOBJ_ANALYZER_TREEVIEW_GUID = "4C7D6317-C84A-42E6-A582-FCE3ED35EBE6";

		/// <summary>Search ListBox</summary>
		public static readonly string GUIDOBJ_SEARCH_GUID = "7B460F9C-424D-48B3-8FD3-72CEE8DD58E5";

		/// <summary>Treeview nodes array (<see cref="ITreeNodeData"/>[])</summary>
		public static readonly string GUIDOBJ_TREEVIEW_NODES_ARRAY_GUID = "B116BABD-BD8B-4870-968A-D1871CC21638";

		/// <summary><see cref="ISearchResult"/></summary>
		public static readonly string GUIDOBJ_SEARCHRESULT_GUID = "50CD0058-6406-4ACA-A386-1A4E07561C62";

		/// <summary><see cref="CodeReference"/></summary>
		public static readonly string GUIDOBJ_CODE_REFERENCE_GUID = "751F4075-D420-4196-BCF0-A0149A8948A4";

		/// <summary>Files <see cref="TabControl"/></summary>
		public static readonly string GUIDOBJ_FILES_TABCONTROL_GUID = "AB1B4BCE-D8C1-43BE-8822-C124FBCAC260";

		/// <summary><see cref="ITabGroup"/></summary>
		public static readonly string GUIDOBJ_TABGROUP_GUID = "87B2F94A-D80B-45FD-BB31-71E390CA6C01";

		/// <summary><see cref="IToolWindowGroup"/></summary>
		public static readonly string GUIDOBJ_TOOLWINDOWGROUP_GUID = "3E9743F1-A2E0-4C5A-B463-3E8CF6D677E4";

		/// <summary>Tool window <see cref="TabControl"/></summary>
		public static readonly string GUIDOBJ_TOOLWINDOW_TABCONTROL_GUID = "33FEE79F-7998-4D63-8E6F-B3AD86134960";

		/// <summary>Text editor control</summary>
		public static readonly string GUIDOBJ_TEXTEDITORCONTROL_GUID = "7F9E85C9-05B5-43FE-9CD1-20E61B183454";

		/// <summary><see cref="ITextEditorUIContext"/></summary>
		public static readonly string GUIDOBJ_TEXTEDITORUICONTEXT_GUID = "848AC3FB-7D67-4427-A604-86BFF539E527";

		/// <summary><see cref="TextEditorLocation"/></summary>
		public static readonly string GUIDOBJ_TEXTEDITORLOCATION_GUID = "0C5E61BF-FC0D-47F7-8C49-69BD93CD11B5";

		/// <summary><c>HexBox</c></summary>
		public static readonly string GUIDOBJ_HEXBOX_GUID = "6D0D8103-1D91-4815-94C3-9AAB41D3175B";

		/// <summary>Text editor icon bar</summary>
		public static readonly string GUIDOBJ_TEXTEDITOR_ICONBAR_GUID = "7B29633F-85AD-41F6-BD09-9989BB55B7E2";

		/// <summary><see cref="IIconBarObject"/></summary>
		public static readonly string GUIDOBJ_IICONBAROBJECT_GUID = "16307E6A-7986-477C-9035-18356742E375";

		/// <summary>Group: App Menu: File, Group: Save</summary>
		public const string GROUP_APP_MENU_FILE_SAVE = "0,557C4B2D-5966-41AF-BFCA-D0A36DB5D6D8";

		/// <summary>Group: App Menu: File, Group: Open</summary>
		public const string GROUP_APP_MENU_FILE_OPEN = "1000,636D9C45-00A9-461F-8947-E01755929A5B";

		/// <summary>Group: App Menu: File, Group: Exit</summary>
		public const string GROUP_APP_MENU_FILE_EXIT = "1000000,6EBA065B-5A1E-4DD4-B91A-339F2D2ED66E";

		/// <summary>Group: App Menu: Edit, Group: Undo/Redo</summary>
		public const string GROUP_APP_MENU_EDIT_UNDO = "0,3DFFD4E1-CFD9-442D-B1E5-E1E98AB8766B";

		/// <summary>Group: App Menu: Edit, Group: Find</summary>
		public const string GROUP_APP_MENU_EDIT_FIND = "1000,240D24B1-1A37-41B8-8A9A-94CD72C08145";

		/// <summary>Group: App Menu: Edit, Group: AsmEditor Delete</summary>
		public const string GROUP_APP_MENU_EDIT_ASMED_DELETE = "2000,F483414D-5CA0-4CE3-9FB2-BFB21987D9F4";

		/// <summary>Group: App Menu: Edit, Group: AsmEditor Misc</summary>
		public const string GROUP_APP_MENU_EDIT_ASMED_MISC = "3000,3DCA360E-3CCD-4F27-AF50-A254CD5F9C83";

		/// <summary>Group: App Menu: Edit, Group: AsmEditor New</summary>
		public const string GROUP_APP_MENU_EDIT_ASMED_NEW = "4000,178A6FD0-2F22-466D-8F2E-664E5531F50B";

		/// <summary>Group: App Menu: Edit, Group: AsmEditor Settings</summary>
		public const string GROUP_APP_MENU_EDIT_ASMED_SETTINGS = "5000,69EA4DD7-8220-43A5-9812-F1EC221AD7D8";

		/// <summary>Group: App Menu: Edit, Group: Hex</summary>
		public const string GROUP_APP_MENU_EDIT_HEX = "6000,6D8CA476-8D3D-468E-A895-40F3A9D5A25C";

		/// <summary>Group: App Menu: Edit, Group: Hex MD</summary>
		public const string GROUP_APP_MENU_EDIT_HEX_MD = "7000,36F0A9CA-5D14-4F56-8F64-ED3628FB5F30";

		/// <summary>Group: App Menu: Edit, Group: Hex MD Go To</summary>
		public const string GROUP_APP_MENU_EDIT_HEX_GOTO_MD = "8000,1E0213F3-0578-43D9-A12D-14AE30EFD0EA";

		/// <summary>Group: App Menu: Edit, Group: Hex Copy</summary>
		public const string GROUP_APP_MENU_EDIT_HEX_COPY = "9000,32791A7F-4CFC-49D2-B066-A611A9E362DB";

		/// <summary>Group: App Menu: View, Group: Options</summary>
		public const string GROUP_APP_MENU_VIEW_OPTS = "0,FCBA133F-F62B-4DB2-BEC9-5AE11C95873B";

		/// <summary>Group: App Menu: View, Group: Tool Windows</summary>
		public const string GROUP_APP_MENU_VIEW_WINDOWS = "1000,599D070A-521E-4A1B-80DB-62C9B0AB48FA";

		/// <summary>Group: App Menu: View, Group: Options dlg</summary>
		public const string GROUP_APP_MENU_VIEW_OPTSDLG = "1000000,AAA7FF98-47CD-4ABF-8824-EE20A283EEB3";

		/// <summary>Group: App Menu: Themes, Group: Themes</summary>
		public const string GROUP_APP_MENU_THEMES_THEMES = "0,AAE0CE90-DB6E-4E8D-9E1B-9BF7ABBDBB32";

		/// <summary>Group: App Menu: Debug, Group: Start</summary>
		public const string GROUP_APP_MENU_DEBUG_START = "0,118A7201-7560-443E-B2F6-7F6369A253A2";

		/// <summary>Group: App Menu: Debug, Group: Continue/Stop/etc commands</summary>
		public const string GROUP_APP_MENU_DEBUG_CONTINUE = "1000,E9AEB324-1425-4CBF-8998-B1796A16AA06";

		/// <summary>Group: App Menu: Debug, Group: Step commands</summary>
		public const string GROUP_APP_MENU_DEBUG_STEP = "2000,5667E48E-5E33-46E9-9661-98B979D65F5D";

		/// <summary>Group: App Menu: Debug, Group: Breakpoint commands</summary>
		public const string GROUP_APP_MENU_DEBUG_BREAKPOINTS = "3000,2684EC1B-45A7-4412-BCBF-81345845FF54";

		/// <summary>Group: App Menu: Debug, Group: Show Debug Windows</summary>
		public const string GROUP_APP_MENU_DEBUG_SHOW = "4000,26D43C29-2EDF-4094-8993-10B7CECEAACA";

		/// <summary>Group: App Menu: Window, Group: Window</summary>
		public const string GROUP_APP_MENU_WINDOW_WINDOW = "0,27A8834B-D6BF-4267-803D-15DECAFAEA05";

		/// <summary>Group: App Menu: Window, Group: Tab Groups</summary>
		public const string GROUP_APP_MENU_WINDOW_TABGROUPS = "1000,3890B3CB-2DE5-4745-A8F8-61A379485345";

		/// <summary>Group: App Menu: Window, Group: Tab Groups Close commands</summary>
		public const string GROUP_APP_MENU_WINDOW_TABGROUPSCLOSE = "2000,11548593-C399-4EA8-B944-60603BE1FD4B";

		/// <summary>Group: App Menu: Window, Group: Tab Groups Vert/Horiz commands</summary>
		public const string GROUP_APP_MENU_WINDOW_TABGROUPSVERT = "3000,7E948EE4-59EA-47F2-B1C8-C5A5DB6F13B9";

		/// <summary>Group: App Menu: Window, Group: All Windows</summary>
		public const string GROUP_APP_MENU_WINDOW_ALLWINDOWS = "1000000,0BBFA4E5-3C54-41E9-BC74-69ADDC09CECC";

		/// <summary>Group: App Menu: Help, Group: Links</summary>
		public const string GROUP_APP_MENU_HELP_LINKS = "0,35CCC7A7-D1C0-4F70-AAFC-7E7CD90B4735";

		/// <summary>Group: App Menu: Help, Group: About</summary>
		public const string GROUP_APP_MENU_HELP_ABOUT = "1000000,835F06B5-67FB-4D01-8920-9D9E2FED9238";

		/// <summary>Group: Context Menu, Type: Code, Group: Tabs</summary>
		public const string GROUP_CTX_CODE_TABS = "0,3576E74B-8D4D-47EE-9925-462B1007C879";

		/// <summary>Group: Context Menu, Type: Code, Group: Debug</summary>
		public const string GROUP_CTX_CODE_DEBUG = "1000,46C39BDA-35F5-4416-AAE2-A2FE05645F79";

		/// <summary>Group: Context Menu, Type: Code, Group: AsmEditor Save</summary>
		public const string GROUP_CTX_CODE_ASMED_SAVE = "2000,57ED92C1-3292-47DD-99CD-FB777DDF1276";

		/// <summary>Group: Context Menu, Type: Code, Group: AsmEditor Delete</summary>
		public const string GROUP_CTX_CODE_ASMED_DELETE = "3000,7A3E4F42-37A5-4A85-B403-62E6CD091E1D";

		/// <summary>Group: Context Menu, Type: Code, Group: AsmEditor New</summary>
		public const string GROUP_CTX_CODE_ASMED_NEW = "4000,15776B90-55EF-4ABE-9EC8-FB4A1E49A76F";

		/// <summary>Group: Context Menu, Type: Code, Group: AsmEditor Settings</summary>
		public const string GROUP_CTX_CODE_ASMED_SETTINGS = "5000,4E4FF711-D262-452D-BA1A-38A6D9951CE2";

		/// <summary>Group: Context Menu, Type: Code, Group: AsmEditor IL ED</summary>
		public const string GROUP_CTX_CODE_ASMED_ILED = "6000,5DD87F08-FB00-4D00-9503-29590A8079CE";

		/// <summary>Group: Context Menu, Type: Code, Group: Tokens</summary>
		public const string GROUP_CTX_CODE_TOKENS = "7000,096957CB-B94D-4A47-AC6D-DBF4C63C6955";

		/// <summary>Group: Context Menu, Type: Code, Group: Hex</summary>
		public const string GROUP_CTX_CODE_HEX = "9000,81BEEEAD-9498-4AD5-B387-006E93FD4014";

		/// <summary>Group: Context Menu, Type: Code, Group: Hex MD</summary>
		public const string GROUP_CTX_CODE_HEX_MD = "10000,0BE33A51-E400-4E3D-9B48-FF91E4A78303";

		/// <summary>Group: Context Menu, Type: Code, Group: Hex Copy</summary>
		public const string GROUP_CTX_CODE_HEX_COPY = "12000,E18271DD-7571-4509-9A7D-37E283BCF7C2";

		/// <summary>Group: Context Menu, Type: Code, Group: Debug RT</summary>
		public const string GROUP_CTX_CODE_DEBUGRT = "13000,5A9207C0-C0E5-464D-B7A2-FB29ADA9C090";

		/// <summary>Group: Context Menu, Type: Code, Group: Other</summary>
		public const string GROUP_CTX_CODE_OTHER = "14000,47308D41-FCAD-4518-9859-AD67C2B912BB";

		/// <summary>Group: Context Menu, Type: Code, Group: Editor</summary>
		public const string GROUP_CTX_CODE_EDITOR = "15000,FD52ABD1-6DB2-48C3-A5DB-809ECE5EBBB2";

		/// <summary>Group: Context Menu, Type: HexBox, Group: Show commands</summary>
		public const string GROUP_CTX_HEXBOX_SHOW = "0,D49C0D59-BAC6-476C-B5C7-66E8CE6CBD83";

		/// <summary>Group: Context Menu, Type: HexBox, Group: Edit</summary>
		public const string GROUP_CTX_HEXBOX_EDIT = "1000,05719F94-21C1-4C73-8931-929E6A9B6A69";

		/// <summary>Group: Context Menu, Type: HexBox, Group: Options</summary>
		public const string GROUP_CTX_HEXBOX_OPTS = "2000,DFE105B4-78B1-4227-8A91-A7C8D4B00495";

		/// <summary>Group: Context Menu, Type: HexBox, Group: Copy</summary>
		public const string GROUP_CTX_HEXBOX_COPY = "3000,CD7D55BB-5ED8-4E8D-B588-AD19AB771105";

		/// <summary>Group: Context Menu, Type: Tabs, Group: Close/New commands</summary>
		public const string GROUP_CTX_TABS_CLOSE = "0,FABC0864-6B57-4C49-A1AF-6015F7CFB5F4";

		/// <summary>Group: Context Menu, Type: Tabs, Group: Tab Groups</summary>
		public const string GROUP_CTX_TABS_GROUPS = "1000,1F89B1F4-8A1F-41FC-8B19-AF3F36AE806E";

		/// <summary>Group: Context Menu, Type: Tabs, Group: Tab Groups Close commands</summary>
		public const string GROUP_CTX_TABS_GROUPSCLOSE = "2000,80871274-20F2-4A51-8697-C3439781CA40";

		/// <summary>Group: Context Menu, Type: Tabs, Group: Tab Groups Vert/Horiz commands</summary>
		public const string GROUP_CTX_TABS_GROUPSVERT = "3000,15174C91-6EA8-47E3-880E-FCDF607974F1";

		/// <summary>Group: Context Menu, Type: Tool Windows, Group: Close commands</summary>
		public const string GROUP_CTX_TOOLWINS_CLOSE = "0,D6F31BC9-2474-44B9-8786-D3044F6F402C";

		/// <summary>Group: Context Menu, Type: Tool Windows, Group: Tab Groups</summary>
		public const string GROUP_CTX_TOOLWINS_GROUPS = "1000,32E1C678-7889-499D-8BC3-C22160E7E2AC";

		/// <summary>Group: Context Menu, Type: Tool Windows, Group: Tab Groups Close commands</summary>
		public const string GROUP_CTX_TOOLWINS_GROUPSCLOSE = "2000,61D665C4-B55D-45BF-B592-85D174C0A1E7";

		/// <summary>Group: Context Menu, Type: Tool Windows, Group: Tab Groups Vert/Horiz commands</summary>
		public const string GROUP_CTX_TOOLWINS_GROUPSVERT = "3000,3F438576-672F-4865-B581-759D5DC678D5";

		/// <summary>Group: Context Menu, Type: Search, Group: Tabs</summary>
		public const string GROUP_CTX_SEARCH_TABS = "0,249A0912-68BE-4468-931A-055726958EA4";

		/// <summary>Group: Context Menu, Type: Search, Group: Tokens</summary>
		public const string GROUP_CTX_SEARCH_TOKENS = "1000,8B57D21D-8109-424A-A337-DB61BE361ED4";

		/// <summary>Group: Context Menu, Type: Search, Group: Other</summary>
		public const string GROUP_CTX_SEARCH_OTHER = "2000,255AE50D-3638-4128-808D-FC8910BA9279";

		/// <summary>Group: Context Menu, Type: Search, Group: Options</summary>
		public const string GROUP_CTX_SEARCH_OPTIONS = "10000,2A261412-7DCD-4CD1-B936-783C67476E99";

		/// <summary>Group: Context Menu, Type: Analyzer, Group: Tabs</summary>
		public const string GROUP_CTX_ANALYZER_TABS = "0,BC8D4C75-B5BC-4964-9A3C-E9EE33F928B0";

		/// <summary>Group: Context Menu, Type: Analyzer, Group: Tokens</summary>
		public const string GROUP_CTX_ANALYZER_TOKENS = "1000,E3FB23EB-EFA8-4C80-ACCD-DCB714BBAFC7";

		/// <summary>Group: Context Menu, Type: Analyzer, Group: Other</summary>
		public const string GROUP_CTX_ANALYZER_OTHER = "2000,A766D535-4069-4AF7-801E-F4B87A2D0F84";

		/// <summary>Group: Context Menu, Type: Analyzer, Group: Options</summary>
		public const string GROUP_CTX_ANALYZER_OPTIONS = "10000,FD6E5D84-A83C-4D0A-8A77-EE755DE76999";

		/// <summary>Group: Context Menu, Type: Files, Group: Tabs</summary>
		public const string GROUP_CTX_FILES_TABS = "0,3FEF128B-8320-4ED0-B03B-0932FCCDA98E";

		/// <summary>Group: Context Menu, Type: Files, Group: AsmEditor Save</summary>
		public const string GROUP_CTX_FILES_ASMED_SAVE = "1000,9495E6B9-0C5C-484A-9354-A5D19A5010DE";

		/// <summary>Group: Context Menu, Type: Files, Group: AsmEditor Delete</summary>
		public const string GROUP_CTX_FILES_ASMED_DELETE = "2000,17B24EE5-C1C0-441D-9B6F-C7632AF4C539";

		/// <summary>Group: Context Menu, Type: Files, Group: AsmEditor Misc</summary>
		public const string GROUP_CTX_FILES_ASMED_MISC = "3000,928EDD44-E4A9-4EA9-93FF-55709943A088";

		/// <summary>Group: Context Menu, Type: Files, Group: AsmEditor New</summary>
		public const string GROUP_CTX_FILES_ASMED_NEW = "4000,05FD56B0-CAF9-48E1-9CED-5221E8A13140";

		/// <summary>Group: Context Menu, Type: Files, Group: AsmEditor Settings</summary>
		public const string GROUP_CTX_FILES_ASMED_SETTINGS = "5000,2247C4DB-73B8-4926-96EB-1C16EAF4A3E4";

		/// <summary>Group: Context Menu, Type: Files, Group: AsmEditor IL ED</summary>
		public const string GROUP_CTX_FILES_ASMED_ILED = "6000,9E0E8539-751E-47EA-A0E9-EAB3A45724E3";

		/// <summary>Group: Context Menu, Type: Files, Group: Tokens</summary>
		public const string GROUP_CTX_FILES_TOKENS = "7000,C98101AD-1A59-42AE-B446-16545F39DC7A";

		/// <summary>Group: Context Menu, Type: Files, Group: Debug RT</summary>
		public const string GROUP_CTX_FILES_DEBUGRT = "9000,9A151E30-AC16-4745-A819-24AA199E82CB";

		/// <summary>Group: Context Menu, Type: Files, Group: Debug</summary>
		public const string GROUP_CTX_FILES_DEBUG = "10000,080A553F-F066-41DC-9CC6-B4CCF2C48675";

		/// <summary>Group: Context Menu, Type: Files, Group: Other</summary>
		public const string GROUP_CTX_FILES_OTHER = "11000,15776535-8A1D-4255-8C3D-331163324C7C";

		/// <summary>Group: Context Menu, Type: Debugger/Breakpoints, Group: Copy</summary>
		public const string GROUP_CTX_DBG_BPS_COPY = "0,FB604477-5E55-4B55-91A4-0E06762FED83";

		/// <summary>Group: Context Menu, Type: Debugger/Breakpoints, Group: Code</summary>
		public const string GROUP_CTX_DBG_BPS_CODE = "1000,5918522A-B51A-430D-8351-561FF0618AB3";

		/// <summary>Group: Context Menu, Type: Debugger/Breakpoints, Group: Options</summary>
		public const string GROUP_CTX_DBG_BPS_OPTS = "10000,E326374F-8D4F-4CC4-B454-BB3F2C585299";

		/// <summary>Group: Context Menu, Type: Debugger/CallStack, Group: Copy</summary>
		public const string GROUP_CTX_DBG_CALLSTACK_COPY = "0,FA7DD7BA-CC6B-46F4-8838-F8015B586911";

		/// <summary>Group: Context Menu, Type: Debugger/CallStack, Group: Frame</summary>
		public const string GROUP_CTX_DBG_CALLSTACK_FRAME = "1000,5F24F714-41CB-4111-89C1-BCA9734115B0";

		/// <summary>Group: Context Menu, Type: Debugger/CallStack, Group: Hex Options</summary>
		public const string GROUP_CTX_DBG_CALLSTACK_HEXOPTS = "2000,66C60524-E129-491D-A8A8-7939B567BC3A";

		/// <summary>Group: Context Menu, Type: Debugger/CallStack, Group: Options</summary>
		public const string GROUP_CTX_DBG_CALLSTACK_OPTS = "3000,8B40E062-CACD-4BF0-BFE2-6003400E9DC8";

		/// <summary>Group: Context Menu, Type: Debugger/Exceptions, Group: Copy</summary>
		public const string GROUP_CTX_DBG_EXCEPTIONS_COPY = "0,836ECA3F-DD93-4843-B752-B81D4A67F1A7";

		/// <summary>Group: Context Menu, Type: Debugger/Exceptions, Group: Add</summary>
		public const string GROUP_CTX_DBG_EXCEPTIONS_ADD = "1000,27050687-6367-48C4-A036-E6E368965BB4";

		/// <summary>Group: Context Menu, Type: Debugger/Locals, Group: Copy</summary>
		public const string GROUP_CTX_DBG_LOCALS_COPY = "0,28D5E753-B1D0-415A-A8C8-7D8F1AC27592";

		/// <summary>Group: Context Menu, Type: Debugger/Locals, Group: Values</summary>
		public const string GROUP_CTX_DBG_LOCALS_VALUES = "1000,33D69C4B-ACB2-4131-8154-CB413EF9D8BA";

		/// <summary>Group: Context Menu, Type: Debugger/Locals, Group: Hex Options</summary>
		public const string GROUP_CTX_DBG_LOCALS_HEXOPTS = "2000,C8143511-5CEA-47A8-B334-0A83D7C85108";

		/// <summary>Group: Context Menu, Type: Debugger/Locals, Group: Tree</summary>
		public const string GROUP_CTX_DBG_LOCALS_TREE = "3000,A3E4126C-A23C-4902-9033-723C52374ECF";

		/// <summary>Group: Context Menu, Type: Debugger/Locals, Group: Options</summary>
		public const string GROUP_CTX_DBG_LOCALS_OPTS = "4000,A599080F-6572-4CB1-B97B-63763D2E2F56";

		/// <summary>Group: Context Menu, Type: Debugger/Modules, Group: Copy</summary>
		public const string GROUP_CTX_DBG_MODULES_COPY = "0,A43EAAA4-2729-418A-B5B8-39237D2E998D";

		/// <summary>Group: Context Menu, Type: Debugger/Modules, Group: Go To</summary>
		public const string GROUP_CTX_DBG_MODULES_GOTO = "1000,D981D937-B196-42F9-8AB8-FED62E3C4C43";

		/// <summary>Group: Context Menu, Type: Debugger/Modules, Group: Hex Options</summary>
		public const string GROUP_CTX_DBG_MODULES_HEXOPTS = "2000,4ABA3476-C88E-47F4-B299-46FE12C38AA3";

		/// <summary>Group: Context Menu, Type: Debugger/Modules, Group: Directories</summary>
		public const string GROUP_CTX_DBG_MODULES_DIRS = "3000,84F6531F-567B-43F8-9251-5566244F00A7";

		/// <summary>Group: Context Menu, Type: Debugger/Modules, Group: Save</summary>
		public const string GROUP_CTX_DBG_MODULES_SAVE = "4000,1B07EE10-60B5-442A-9EC5-63C3D20F5A9E";

		/// <summary>Group: Context Menu, Type: Debugger/Threads, Group: Copy</summary>
		public const string GROUP_CTX_DBG_THREADS_COPY = "0,F11E427D-6B88-44B9-ACFF-4D8AD8131DC0";

		/// <summary>Group: Context Menu, Type: Debugger/Threads, Group: Hex Options</summary>
		public const string GROUP_CTX_DBG_THREADS_HEXOPTS = "1000,960A6F14-846D-42EE-BD1E-4C1C91ECB21F";

		/// <summary>Group: Context Menu, Type: Debugger/Threads, Group: Commands</summary>
		public const string GROUP_CTX_DBG_THREADS_CMDS = "2000,B7B20F2D-6FE1-4415-BC4A-D92B31EE9342";

		/// <summary>Group: Text Editor Icon Bar, Type: Debugger, Group: Breakpoints</summary>
		public const string GROUP_TEXTEDITOR_ICONBAR_DEBUG_BPS = "0,02808659-957B-4E18-BB41-C5C61ACF5535";
	}
}
