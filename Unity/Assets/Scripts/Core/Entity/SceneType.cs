using System;

namespace ET
{
	[Flags]
	public enum SceneType: long
	{
		/*<< n符号的意思是把一个整数以二进制的方式左移n位,比如1<<2就是100,而1<<5就是100000,以此类推*/

		None = 0,
		Main = 1, // 主纤程,一个进程一个, 初始化从这里开始
		NetInner = 1 << 2, // 负责进程间消息通信
		Realm = 1 << 3,
		Gate = 1 << 4,
		Http = 1 << 5,
		Location = 1 << 6,
		Map = 1 << 7,
		Router = 1 << 8,
		RouterManager = 1 << 9,
		Robot = 1 << 10,
		BenchmarkClient = 1 << 11,
		BenchmarkServer = 1 << 12,
		Match = 1 << 14,
		Room = 1 << 15,
		LockStepClient = 1 << 16,
		LockStepServer = 1 << 17,
		RoomRoot = 1 << 18,
		Watcher = 1 << 19,

        AccountHub = 1 << 20,

        // 客户端或Share(注意long类型最多左移64位)
        Demo = 1 << 30,
		Current = 1L << 31,
		LockStep = 1L << 32,
		LockStepView = 1L << 33,
		DemoView = 1L << 34,
		NetClient = 1L << 35,

        Road = 1L << 36,
        MusicGame = 1L << 37,
        IO = 1L << 38,

        All = long.MaxValue,
	}

	public static class SceneTypeHelper
	{
		public static bool HasSameFlag(this SceneType a, SceneType b)
		{
			if (((ulong) a & (ulong) b) == 0)
			{
				return false;
			}
			return true;
		}
	}
}