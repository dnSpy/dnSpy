// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TextView;

namespace ICSharpCode.ILSpy
{
	static class AboutPage
	{
		static AvailableVersionInfo latestAvailableVersion;
		
		public static void Display(DecompilerTextView textView)
		{
			AvalonEditTextOutput output = new AvalonEditTextOutput();
			output.WriteLine("ILSpy version " + RevisionClass.FullVersion);
			output.AddUIElement(
				delegate {
					StackPanel stackPanel = new StackPanel();
					stackPanel.Orientation = Orientation.Horizontal;
					if (latestAvailableVersion == null) {
						Button button = new Button();
						button.Content = "Check for updates";
						button.Cursor = Cursors.Arrow;
						stackPanel.Children.Add(button);
						
						button.Click += delegate {
							button.Content = "Checking...";
							button.IsEnabled = false;
							GetLatestVersion().ContinueWith(
								delegate (Task<AvailableVersionInfo> task) {
									try {
										latestAvailableVersion = task.Result;
										stackPanel.Children.Clear();
										ShowAvailableVersion(latestAvailableVersion, stackPanel);
									} catch (Exception ex) {
										AvalonEditTextOutput exceptionOutput = new AvalonEditTextOutput();
										exceptionOutput.WriteLine(ex.ToString());
										textView.Show(exceptionOutput);
									}
								}, TaskScheduler.FromCurrentSynchronizationContext());
						};
					} else {
						// we already retrieved the latest version sometime earlier
						ShowAvailableVersion(latestAvailableVersion, stackPanel);
					}
					return stackPanel;
				});
			output.WriteLine();
			output.WriteLine();
			using (Stream s = typeof(AboutPage).Assembly.GetManifestResourceStream(typeof(AboutPage), "README.txt")) {
				using (StreamReader r = new StreamReader(s)) {
					string line;
					while ((line = r.ReadLine()) != null)
						output.WriteLine(line);
				}
			}
			textView.Show(output);
		}
		
		static void ShowAvailableVersion(AvailableVersionInfo availableVersion, StackPanel stackPanel)
		{
			Version currentVersion = new Version(RevisionClass.Major + "." + RevisionClass.Minor + "." + RevisionClass.Build + "." + RevisionClass.Revision);
			if (currentVersion == availableVersion.Version) {
				stackPanel.Children.Add(new Image { Width = 16, Height = 16, Source = Images.OK, Margin = new Thickness(4,0,4,0) });
				stackPanel.Children.Add(
					new TextBlock { Text = "You are using the latest release.",
						VerticalAlignment = VerticalAlignment.Bottom
					});
			} else if (currentVersion < availableVersion.Version) {
				stackPanel.Children.Add(
					new TextBlock {
						Text = "Version " + availableVersion.Version + " is available.",
						Margin = new Thickness(0,0,8,0),
						VerticalAlignment = VerticalAlignment.Bottom
					});
				if (availableVersion.DownloadUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
				    || availableVersion.DownloadUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
				{
					Button button = new Button();
					button.Content = "Download";
					button.Cursor = Cursors.Arrow;
					button.Click += delegate {
						Process.Start(availableVersion.DownloadUrl);
					};
					stackPanel.Children.Add(button);
				}
			} else {
				stackPanel.Children.Add(new TextBlock { Text = "You are using a nightly builds newer than the latest release." });
			}
		}
		
		static Task<AvailableVersionInfo> GetLatestVersion()
		{
			TaskCompletionSource<AvailableVersionInfo> tcs = new TaskCompletionSource<AvailableVersionInfo>();
			tcs.SetException(new NotImplementedException());
			//tcs.SetResult(new AvailableVersionInfo { Version = new Version(0,2,0,37), DownloadUrl = "http://www.ilspy.net/" });
			return tcs.Task;
		}
		
		sealed class AvailableVersionInfo
		{
			public Version Version;
			public string DownloadUrl;
		}
	}
}
