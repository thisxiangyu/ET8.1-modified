﻿using System;
using System.IO;
using UnityEngine;

namespace ET
{
    public static class PathHelper
    {     /// <summary>
        ///应用程序外部资源路径存放路径(热更新资源路径)
        /// </summary>
        public static string AppHotfixResPath
        {
            get
            {
                string name = Application.productName;
                string path = AppResPath;
                if (Application.isMobilePlatform)
                {
                    path = $"{Application.persistentDataPath}/{name}/";
                }
                return path;
            }
        }

        /// <summary>
        /// 应用程序内部资源路径存放路径
        /// </summary>
        public static string AppResPath
        {
            get
            {
                return Application.streamingAssetsPath;
            }
        }

        /// <summary>
        /// 应用程序内部资源路径存放路径(www/webrequest专用)
        /// </summary>
        public static string AppResPath4Web
        {
            get
            {
#if UNITY_IOS || UNITY_STANDALONE_OSX
                return $"file://{Application.streamingAssetsPath}";
#else
                return Application.streamingAssetsPath;
#endif

            }
        }

        /// <summary>
        /// 存档路径
        /// </summary>
        public static string SavePath
        {
            get
            {
                string name = Application.productName;
                string rootPath = AppDomain.CurrentDomain.BaseDirectory;
                if (Application.isMobilePlatform)
                {
                    rootPath = $"{Application.persistentDataPath}/{name}/";
                }
                string savePath = Path.Combine(rootPath, "MySave");
                return savePath;
            }
        }
    }
}
