using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ShinkaiModel.Networking {
	public static class Config {
		public const string connectKey = "NTR";
		public const int connectPort = 33322;
		public const int maxConnections = 128;
		public const int pingInterval = 1000;
		public const int disconnectTimeout = 20000;
	}
}
