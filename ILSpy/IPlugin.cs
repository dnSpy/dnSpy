
namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// A plugin can implement this interface to get notified at startup
	/// </summary>
	public interface IPlugin
	{
		/// <summary>
		/// Called when MainWindow has been loaded
		/// </summary>
		void OnLoaded();
	}
}
