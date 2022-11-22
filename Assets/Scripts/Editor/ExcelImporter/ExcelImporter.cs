#if !UNITY_EDITOR
#load "UnityEngine.csx"
#load "../../Code/Module/Config/ExcelManager.cs"
#r "System.Text.RegularExpressions"
#r "System.Diagnostics.Process"
#endif

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TaoTie;
#else
using static UnityEngine;
#endif
using Debug = UnityEngine.Debug;
using static ExcelManager;

public static class ExcelImporter
{
    public static readonly Uri DataRootUri = new Uri(Paths.DataRoot + "/");

    static readonly IReadOnlyList<string> GenerateDirs = new List<string>()
    {
        Paths.Tables,
        Paths.Enums,
        Paths.Beans,
        Paths.Code,
        Paths.Data,
        Paths.CustomTemplate,
        Paths.DataRoot,
    };
    static readonly IReadOnlyList<string> GenerateFiles = new List<string>()
    {
        Paths.DefineFile,
        Paths.GenerateBatFile,
        Paths.ImportLog,
    };

#if UNITY_EDITOR
    [MenuItem("Tools/Re-import all Excel")]
#endif
    public static async void Import()
    {
        const string logid = "Excel Importer";
        Debug.Log($"{logid}: 导入中...");

        foreach (var f in GenerateFiles)
        {
            if (Directory.Exists(f))
                Directory.Delete(f);
        }
        foreach (var d in GenerateDirs.Concat(GenerateFiles.Select(f => Path.GetDirectoryName(f))).Distinct())
        {
            if (!Directory.Exists(d))
            {
                if (File.Exists(d))
                    File.Delete(d);
                Directory.CreateDirectory(d);
            }
        }

        var files = new List<(string Type, string Path)>();
        files.AddRange(Directory.GetFiles(Paths.Tables, FileFilter, SearchOption.AllDirectories).Select(x => ("table", x)));
        files.AddRange(Directory.GetFiles(Paths.Enums, FileFilter, SearchOption.AllDirectories).Select(x => ("enum", x)));
        files.AddRange(Directory.GetFiles(Paths.Beans, FileFilter, SearchOption.AllDirectories).Select(x => ("bean", x)));
        using (var xml = File.Create(Paths.DefineFile))
        {
            using var xmlw = new StreamWriter(xml, Encoding.UTF8);
            xmlw.Write("<root>\r\n");
            xmlw.Write($"    <topmodule name=\"{nameof(cfg)}\"/>\r\n");
            xmlw.Write("    <group name=\"c\" default=\"1\"/>\r\n");
            foreach (var x in files)
                xmlw.Write($"    <importexcel name=\"{MakeRelativePath(DataRootUri, x.Path)}\" type=\"{x.Type}\"/>\r\n");
            xmlw.Write($"    <service name=\"all\" manager=\"{nameof(cfg.Tables)}\" group=\"c\"/>\r\n");
            xmlw.Write("</root>\r\n");
            xmlw.Flush();
        }

        if (Directory.Exists(Paths.Code))
        {
            foreach (var f in Directory.GetFiles(Paths.Code, "*.*", SearchOption.AllDirectories))
                File.Delete(f);
        }
        else Directory.CreateDirectory(Paths.Code);
        if (Directory.Exists(Paths.Data))
        {
            foreach (var f in Directory.GetFiles(Paths.Data, "*", SearchOption.AllDirectories))
                File.Delete(f);
        }
        else Directory.CreateDirectory(Paths.Data);
        var p = new Process
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "dotnet",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden,
                Arguments = $"\"{new Uri(Paths.Luban).AbsolutePath}\""
                          + $" -t \"{Paths.CustomTemplate}\""
                          + " -j cfg --"
                          + $" -d \"{Paths.DefineFile}\""
                          + $" --input_data_dir \"{Paths.DataRoot}/\""
                          + $" --output_data_dir \"{Paths.Data}/\""
                          + $" --output_code_dir \"{Paths.Code}/\""
                          + " --gen_types " + DataFormat switch
                          {
                              DataFormats.Binary => "code_cs_unity_bin,data_bin",
                              _ => "code_cs_unity_json,data_json",
                          }
                          + " -s all",
                WorkingDirectory = Paths.TempDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        };
#if !UNITY_EDITOR || DEBUG
        var bat = new FileInfo(Paths.GenerateBatFile);
        if (bat.Exists)
            bat.Delete();
        using (var bats = bat.Create())
        {
            using var batw = new StreamWriter(bats, Encoding.Default);
            batw.Write($"{p.StartInfo.FileName} {p.StartInfo.Arguments}");
        }
#endif
        var logpath = new Uri(Paths.ImportLog).AbsolutePath;
        if (File.Exists(logpath))
            File.Delete(logpath);
        var hasWarning = false;
        using (var log = File.Create(logpath))
        {
            using var logw = new StreamWriter(log, Encoding.UTF8);
            void WriteLog(object sender, DataReceivedEventArgs args)
            {
                logw.Write(args.Data);
                logw.Flush();
            }
            p.OutputDataReceived += WriteLog;
            p.ErrorDataReceived += WriteLog;
            if (!p.Start())
            {
                Debug.LogError($"{logid}: 导入失败：运行Luban失败");
                return;
            }
            p.WaitForExit();
            while (!p.StandardOutput.EndOfStream)
            {
                var msg = await p.StandardOutput.ReadLineAsync();
                if (!hasWarning)
                    hasWarning = HasWarning(msg);
                await logw.WriteLineAsync(msg);
            }
            while (!p.StandardError.EndOfStream)
            {
                var msg = await p.StandardError.ReadLineAsync();
                if (!hasWarning)
                    hasWarning = HasWarning(msg);
                await logw.WriteLineAsync(msg);
            }
        }
        if (p.ExitCode == 0)
        {
            if (hasWarning)
                Debug.Log($"{logid}: 导入成功，但检测到有警告，请检查日志输出文件: {logpath}");
            else
                Debug.Log($"{logid}: 导入成功");
        }
        else
        {
            Debug.LogError($"{logid}: 导入失败，请检查日志输出文件: {logpath}{Environment.NewLine}或手动运行 {bat.FullName} 检查");
        }

    }
}

#if !UNITY_EDITOR
ExcelImporter.Import();
#endif
