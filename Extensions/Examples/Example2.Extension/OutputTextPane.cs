using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Output;
using dnSpy.Contracts.Text;

// Creates an Output window text pane where our log messages will go.
// Adds context menu commands.

namespace Example2.Extension {
	// Holds an instance of our logger text pane
	static class MyLogger {
		//TODO: Use your own GUID
		public static readonly Guid THE_GUID = new Guid("26F2F5B2-9C5A-4F99-A026-4946A068500F");

		public static IOutputTextPane Instance {
			get {
				if (_instance == null)
					throw new InvalidOperationException("Logger hasn't been initialized yet");
				return _instance;
			}
			set {
				if (_instance != null)
					throw new InvalidOperationException("Can't initialize the logger twice");
				_instance = value ?? throw new ArgumentNullException(nameof(value));
			}
		}
		static IOutputTextPane _instance;

		// This class initializes the Logger property. It gets auto loaded by dnSpy
		[ExportAutoLoaded(Order = double.MinValue)]
		sealed class InitializeLogger : IAutoLoaded {
			[ImportingConstructor]
			InitializeLogger(IOutputService outputService) {
				Instance = outputService.Create(THE_GUID, "My Logger");
				Instance.WriteLine("Logger initialized!");
			}
		}
	}

	sealed class LogEditorCtxMenuContext {
		public readonly IOutputTextPane TextPane;

		public LogEditorCtxMenuContext(IOutputTextPane pane) {
			TextPane = pane;
		}
	}

	abstract class LogEditorCtxMenuCommand : MenuItemBase<LogEditorCtxMenuContext> {
		protected sealed override object CachedContextKey => ContextKey;
		static readonly object ContextKey = new object();

		protected sealed override LogEditorCtxMenuContext CreateContext(IMenuItemContext context) {
			// Check if it's the Output window
			if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_LOG_TEXTEDITORCONTROL_GUID))
				return null;

			// Get the text pane if any
			var textPane = context.Find<IOutputTextPane>();
			if (textPane == null)
				return null;

			// Check if it's our logger text pane
			if (textPane.Guid != MyLogger.THE_GUID)
				return null;

			Debug.Assert(textPane == MyLogger.Instance);

			return new LogEditorCtxMenuContext(textPane);
		}
	}

	// GROUP_CTX_OUTPUT_USER_COMMANDS can be used for user commands, like our commands below:
	[ExportMenuItem(Header = "Write Hello to the Log", Group = MenuConstants.GROUP_CTX_OUTPUT_USER_COMMANDS, Order = 0)]
	sealed class WriteHelloCtxMenuCommand : LogEditorCtxMenuCommand {
		public override void Execute(LogEditorCtxMenuContext context) {
			context.TextPane.Write(TextColor.Blue, "H");
			context.TextPane.Write(TextColor.Red, "E");
			context.TextPane.Write(TextColor.Green, "L");
			context.TextPane.Write(TextColor.Yellow, "L");
			context.TextPane.Write(TextColor.Cyan, "O");
			context.TextPane.Write(TextColor.Gray, "!");
			context.TextPane.WriteLine();
		}
	}

	[ExportMenuItem(Header = "Open the Pod Bay Doors", Group = MenuConstants.GROUP_CTX_OUTPUT_USER_COMMANDS, Order = 10)]
	sealed class ShowExceptionMessagesCtxMenuCommand : LogEditorCtxMenuCommand {
		public override void Execute(LogEditorCtxMenuContext context) =>
			context.TextPane.WriteLine(TextColor.Error, "I'm afraid I can't do that.");
	}
}
