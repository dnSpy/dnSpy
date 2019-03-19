using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;

// This file adds custom document tab content when the user clicks on our new AssemblyChildNode tree node.
// This node is created by TreeNodeDataProvider.cs.

namespace Example2.Extension {
	[ExportDocumentTabContentFactory]
	sealed class AssemblyChildNodeTabContentFactory : IDocumentTabContentFactory {
		// Called to create a new IFileTabContent. If it's our new tree node, create a new IFileTabContent for it
		public DocumentTabContent Create(IDocumentTabContentFactoryContext context) {
			if (context.Nodes.Length == 1 && context.Nodes[0] is AssemblyChildNode)
				return new AssemblyChildNodeTabContent((AssemblyChildNode)context.Nodes[0]);
			return null;
		}

		//TODO: Use your own guid
		static readonly Guid GUID_SerializedContent = new Guid("FC6D2EC8-6FF8-4071-928E-EB07735A6402");

		public DocumentTabContent Deserialize(Guid guid, ISettingsSection section, IDocumentTabContentFactoryContext context) {
			if (guid == GUID_SerializedContent) {
				// Serialize() doesn't add anything extra to 'section', but if it did, you'd have to
				// get that info here and return null if the serialized data wasn't found.
				var node = context.Nodes.Length == 1 ? context.Nodes[0] as AssemblyChildNode : null;
				if (node != null)
					return new AssemblyChildNodeTabContent(node);
			}
			return null;
		}

		public Guid? Serialize(DocumentTabContent content, ISettingsSection section) {
			if (content is AssemblyChildNodeTabContent) {
				// There's nothing else we need to serialize it, but if there were, use 'section'
				// to write the info needed by Deserialize() above.
				return GUID_SerializedContent;
			}
			return null;
		}
	}

	sealed class AssemblyChildNodeTabContent : DocumentTabContent {
		// Returns all nodes used to generate the content
		public override IEnumerable<DocumentTreeNodeData> Nodes {
			get { yield return node; }
		}

		public override string Title => node.ToString();
		public override object ToolTip => node.ToString();

		readonly AssemblyChildNode node;

		public AssemblyChildNodeTabContent(AssemblyChildNode node) => this.node = node;

		// Called when the user opens a new tab. Override CanClone and return false if
		// Clone() isn't supported
		public override DocumentTabContent Clone() => new AssemblyChildNodeTabContent(node);

		// Gets called to create the UI context. It can be shared by any IFileTabContent in this tab.
		// Eg. there's only one text editor per tab, shared by all IFileTabContents that need a text
		// editor.
		public override DocumentTabUIContext CreateUIContext(IDocumentTabUIContextLocator locator) {
			// This custom view object is shared by all nodes of the same type. If we didn't want it
			// to be shared, we could use 'node' or 'this' as the key.
			var key = node.GetType();
			// var key = node;	// uncomment to not share it

			// If the UI object has already been created, use it, else create it. The object is
			// stored in a weak reference unless you use the other method override.
			return locator.Get(key, () => new AssemblyChildNodeUIContext());
		}

		public override void OnShow(IShowContext ctx) {
			// Get the real type, created by CreateUIContext() above.
			var uiCtx = (AssemblyChildNodeUIContext)ctx.UIContext;

			// You could initialize some stuff, eg. update its DataContext or whatever
			uiCtx.Initialize("some input"); // pretend we need to initialize something
		}
	}

	sealed class AssemblyChildNodeUIContext : DocumentTabUIContext {
		// The element inside UIObject that gets the focus when the tool window should be focused.
		// If it's not as easy as calling FocusedElement.Focus() to focus it, you must implement
		// dnSpy.Contracts.Controls.IFocusable.
		public override IInputElement FocusedElement => content;

		// The element in UIObject that gets the scale transform. null can be returned to disable scaling.
		public override FrameworkElement ZoomElement => content;

		// The UI object shown in the tab. Should be a WPF control (eg. UserControl) or a .NET object
		// with a DataTemplate.
		public override object UIObject => content;

		readonly ContentPresenter content;
		readonly AssemblyChildNodeVM vm;

		public AssemblyChildNodeUIContext() {
			vm = new AssemblyChildNodeVM();
			// A ContentPresenter + DataTemplate is used to show the VM, but you could of course use
			// a UserControl.
			content = new ContentPresenter {
				Focusable = true,
				Content = vm,
			};
		}

		sealed class MyUIState {
			public string Value1;
			public bool Value2;

			public MyUIState(string value1, bool value2) {
				Value1 = value1;
				Value2 = value2;
			}
		}

		// Optional:
		// Called to create an object that can be passed to RestoreUIState()
		public override object DeserializeUIState(ISettingsSection section) {
			var value1 = section.Attribute<string>(nameof(MyUIState.Value1));
			var value2 = section.Attribute<bool?>(nameof(MyUIState.Value2));
			if (value1 == null || value2 == null)
				return null;

			return new MyUIState(value1, value2.Value);
		}

		// Optional:
		// Saves the object returned by CreateUIState()
		public override void SerializeUIState(ISettingsSection section, object obj) {
			var d = obj as MyUIState;
			if (d == null)
				return;

			section.Attribute(nameof(d.Value1), d.Value1);
			section.Attribute(nameof(d.Value2), d.Value2);
		}

		// Optional:
		// Creates the UI state or returns null. This is an example, so return some random data
		public override object CreateUIState() => new MyUIState("Some string", true);

		// Optional:
		// Restores the UI state
		public override void RestoreUIState(object obj) {
			var d = obj as MyUIState;
			if (d == null)
				return;

			// Here's where you'd restore the UI state, eg position etc.
		}

		// Called by AssemblyChildNodeTabContent above to initialize it before it's shown again
		public void Initialize(string s) {
			// here we could initialize something before it's shown again, eg. initialize the DataContext
		}
	}

	sealed class AssemblyChildNodeVM : ViewModelBase {
		public string SomeMessage {
			get => someMessage;
			set {
				if (someMessage != value) {
					someMessage = value;
					OnPropertyChanged(nameof(SomeMessage));
				}
			}
		}
		string someMessage = "Hello World";
	}
}
