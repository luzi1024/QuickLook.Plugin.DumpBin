using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;
using QuickLook.Common.Helpers;
using QuickLook.Common.Plugin;
using QuickLook.Plugin.TextViewer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Contexts;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;


namespace QuickLook.Plugin.DumpBin
{
    public class Plugin : IViewer
    {
		private static readonly HashSet<string> WellKnownExtensions = new(
		[
			".dll", ".lib", ".arx",
		]);

		public int Priority => 0;
		private static string pluginPath;
		private static int sizeWidth = 800;
		private static int sizeHeigh = 600;
		public void Init()
        {
			string assemblyPath = Assembly.GetExecutingAssembly().Location;

			// 获取目录（不含dll文件名）
			pluginPath = Path.GetDirectoryName(assemblyPath);
			//
			sizeWidth = SettingHelper.Get("width", 800, Assembly.GetExecutingAssembly().GetName().Name);
			sizeHeigh = SettingHelper.Get("heigh", 600, Assembly.GetExecutingAssembly().GetName().Name);
			SettingHelper.Set("width", sizeWidth, Assembly.GetExecutingAssembly().GetName().Name);
			SettingHelper.Set("heigh", sizeHeigh, Assembly.GetExecutingAssembly().GetName().Name);
		}

		public bool CanHandle(string path)
        {
			if (Directory.Exists(path))
				return false;
			return WellKnownExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }

        public void Prepare(string path, ContextObject context)
        {
            context.PreferredSize = new Size { Width = sizeWidth, Height = sizeHeigh };
        }

        public void View(string path, ContextObject context)
        {
			var viewer = new TextViewerPanel();
			
			viewer.IsReadOnly = true;
			viewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
			viewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
			// 要执行的命令
			string command = pluginPath + "/external/dumpbin.exe /EXPORTS " + path;
			// 创建进程对象
			Process process = new Process();

			// 配置进程启动信息
			process.StartInfo.FileName = "cmd.exe";
			process.StartInfo.Arguments = "/c " + command; // /c 表示执行完命令后关闭窗口
			process.StartInfo.RedirectStandardOutput = true;  // 重定向标准输出
			process.StartInfo.RedirectStandardError = true;   // 重定向标准错误（如果需要）
			process.StartInfo.UseShellExecute = false;        // 必须设置为 false 才能重定向
			process.StartInfo.CreateNoWindow = true;          // 不显示命令窗口

			try
			{
				// 启动进程
				process.Start();

				// 读取全部标准输出内容
				string output = process.StandardOutput.ReadToEnd();

				// 等待进程退出
				process.WaitForExit();

				// 输出结果
				viewer.Text = output;
			}
			catch (Exception ex)
			{
				viewer.Text = "执行命令时发生错误：" + ex.Message;
			}

			context.ViewerContent = viewer;
            context.Title = $"{Path.GetFileName(path)}";
            context.IsBusy = false;
		}

		public void Cleanup()
        {
		
		}
    }
}