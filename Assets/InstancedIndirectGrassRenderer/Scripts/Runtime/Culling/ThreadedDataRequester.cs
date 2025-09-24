using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace IIGR.Utils
{
	[ExecuteAlways, InitializeOnLoad]
	public class ThreadedDataRequester : MonoBehaviour
	{
		private Queue<ThreadInfo> _dataQueue = new Queue<ThreadInfo>();

		public static ThreadedDataRequester Instance { get; private set; }

		private void Awake()
		{
			if (Instance != null)
				return;
			Instance = this;
		}


#if UNITY_EDITOR
		private void OnEnable()
		{
			Instance ??= this;
		}
#endif

		private void Update()
		{
			if (_dataQueue.Count == 0)
				return;

			while (_dataQueue.TryDequeue(out var threadInfo))
				threadInfo.Callback?.Invoke(threadInfo.Parameter);
		}

		public static void RequestData(Func<object> generateData, Action<object> callback)
		{
			ThreadStart threadStart = delegate
			{
				Instance.DataThread(generateData, callback);
			};

			var thread = new Thread(threadStart);
			thread.Start();
		}

		private void DataThread(Func<object> generateData, Action<object> callback)
		{
			object data = generateData();
			lock (_dataQueue)
			{
				_dataQueue.Enqueue(new ThreadInfo(callback, data));
			}
		}

		private struct ThreadInfo
		{
			public readonly Action<object> Callback;
			public readonly object Parameter;

			public ThreadInfo(Action<object> callback, object parameter)
			{
				Callback = callback;
				Parameter = parameter;
			}
		}
	}
}