using System;
using ShinkaiModel.Core;

namespace ShinkaiServer {
	public static class Program {
		private static Server server;

		static void Main(string[] args) {
			Log.SetLevel(Log.LogLevel.ConsoleInfo | Log.LogLevel.ConsoleDebug);

			var sleeper = new Helpers.Sleeper(12);
			server = new Server();
			server.Start();

			while (true) {
				server.Update();
				sleeper.Update();
			}
		}
	}
}
