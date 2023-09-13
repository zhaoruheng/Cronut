﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Cloud
{
    public class WatchEvent
    {
        public string filePath;
        public string oldFilePath;
        public int fileEvent;   //新建1  修改2  删除3  重命名4  不做操作0
    }
    class FileWatcher
    {
        FileSystemWatcher watcher;
        private Dictionary<string, DateTime> dateTimeDictionary = new Dictionary<string, DateTime>();
        public delegate void DelegateEventHander(object sender, WatchEvent we);
        public DelegateEventHander SendEvent;

		[DllImport("kernel32.dll")]
		public static extern IntPtr _lopen(string lpPathName, int iReadWrite);

		[DllImport("kernel32.dll")]
		public static extern bool CloseHandle(IntPtr hObject);

		public const int OF_READWRITE = 2;
		public const int OF_SHARE_DENY_NONE = 0x40;
		public readonly IntPtr HFILE_ERROR = new IntPtr(-1);

		public FileWatcher(string path, string filter)
        {
            watcher = new FileSystemWatcher();
            watcher.Path = path;
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite;
            watcher.Filter = filter;
            watcher.Changed += new FileSystemEventHandler(OnProcess);
            watcher.Created += new FileSystemEventHandler(OnProcess);
            watcher.Deleted += new FileSystemEventHandler(OnProcess);
            watcher.Renamed += new RenamedEventHandler(OnRenamed);
        }
        public void Start()
        {
            watcher.EnableRaisingEvents = true;
        }
        private bool CheckPath(string path)
        {
            if (File.Exists(path))
                return true;
            return false;
        }
        private void OnProcess(object sender, FileSystemEventArgs e)
        {
            WatchEvent we = new WatchEvent();
            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                we.fileEvent = 3;
                we.filePath = e.FullPath;
            }
            else if (!CheckPath(e.FullPath))
            {
                we.fileEvent = 0;
            }
            else if (e.ChangeType == WatcherChangeTypes.Created)
            {
                we.filePath = e.FullPath;
                we.fileEvent = 1; 
            }
            else if (e.ChangeType == WatcherChangeTypes.Changed)
            {
				if (IsLocked(e.FullPath))
					return;
				if (!dateTimeDictionary.ContainsKey(e.FullPath) || (dateTimeDictionary.ContainsKey(e.FullPath) && File.GetLastWriteTime(e.FullPath).Ticks - dateTimeDictionary[e.FullPath].Ticks > 1e7))
				{
					we.filePath = e.FullPath;
					we.fileEvent = 2;
					dateTimeDictionary[e.FullPath] = File.GetLastWriteTime(e.FullPath);
				}
				//we.filePath = e.FullPath;
				//we.fileEvent = 2;
				else
					we.fileEvent = 0;
			}
            else
                we.fileEvent = 0;
            SendEvent?.Invoke(this, we);
			Console.WriteLine(string.Format("type: {0}， filePath:{1}", we.fileEvent, we.filePath));
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            WatchEvent we = new WatchEvent();
            we.fileEvent = 4;
            we.filePath = e.FullPath;
            we.oldFilePath = e.OldFullPath;
            SendEvent?.Invoke(this, we);
			Console.WriteLine(string.Format("rename, oldName: {0} newName: {1}", we.oldFilePath, we.filePath));
        }

		private bool IsLocked(string fpath)
		{
			if (!File.Exists(fpath))
			{
				Console.WriteLine("文件都不存在!");
				return false;
			}
			IntPtr vHandle = _lopen(fpath, OF_READWRITE | OF_SHARE_DENY_NONE);
			if (vHandle == HFILE_ERROR)
			{
				Console.WriteLine("文件被占用！");
				return true;
			}
			CloseHandle(vHandle);
			Console.WriteLine("没有被占用！");
			return false;
		}
    }
}
