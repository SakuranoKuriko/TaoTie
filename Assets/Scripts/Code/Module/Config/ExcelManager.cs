#if UNITY_5_3_OR_NEWER
using System.Collections.Generic;
using System.Text;
using Bright.Serialization;
using TaoTie;
using YooAsset;
using SimpleJSON;
using UnityEngine;
#else
#load "../../../Editor/ExcelImporter/UnityEngine.csx"
#r "System.Text.RegularExpressions"
using static UnityEngine;
#endif
using System;
using System.Text.RegularExpressions;
using System.IO;

// 占位符，生成代码时会生成到这个命名空间下
// 外部脚本导表时无法定义命名空间，只能定义为类
// 要修改需要同时修改以下条件编译中的两个名称
#if UNITY_5_3_OR_NEWER
namespace cfg
#else
public static class cfg
#endif
{
    // 占位符，生成代码时会生成到这个类中
    public partial class Tables { }
}

public class ExcelManager
#if UNITY_5_3_OR_NEWER
    : IManager
#endif
{
    public enum DataFormats
    {
        Json,
        Binary
    }

    /// <summary>数据表格文件搜索过滤器</summary>
    public static string FileFilter { get; set; } = "*.xlsx";

    /// <summary>输出数据文件格式</summary>
    public static DataFormats DataFormat { get; set; } = DataFormats.Json;

    /// <summary>检测日志输出中是否有输出警告的正则表达式</summary>
    public static Regex HasWarningRegex { get; set; } = new Regex(@"(不存在|警告|Warn)", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
    /// <summary>检测日志输出中是否有输出警告</summary>
    public static bool HasWarning(string msg) => HasWarningRegex.IsMatch(msg);

    /// <summary>导入时使用的各种路径</summary>
    public static class Paths
    {
        /// <summary>Luban主程序路径</summary>
        public static readonly string Luban = $"{Application.dataPath}/../Tools/Luban/Luban.ClientServer/Luban.ClientServer.dll";
        /// <summary>Luban 自定义模板搜索路径</summary>
        public static readonly string CustomTemplate = $"{Application.dataPath}/../Tools/Luban/CustomTemplate";
        /// <summary>数据表格存放的根路径</summary>
        public static readonly string DataRoot = $"{Application.dataPath}/../Excel";
        /// <summary>代码生成路径</summary>
        public static readonly string Code = $"{Application.dataPath}/Scripts/Code/Module/Generate/Excel";
        /// <summary>数据生成路径</summary>
        public static readonly string Data = $"{Application.dataPath}/AssetsPackage/Excel";
        /// <summary>临时文件路径</summary>
        public static readonly string TempDir = $"{Application.dataPath}/../Temp/Luban";
        /// <summary>导入日志路径+文件名</summary>
        public static readonly string ImportLog = $"{TempDir}/Luban_ImportLog.log";
        /// <summary>导入定义文件路径</summary>
        public static readonly string DefineFile = $"{TempDir}/Luban_Define_Root.xml";
        /// <summary>Table类表存放的路径</summary>
        public static readonly string Tables = $"{DataRoot}/{nameof(Tables)}";
        /// <summary>Enum类表存放的路径</summary>
        public static readonly string Enums = $"{DataRoot}/{nameof(Enums)}";
        /// <summary>Bean类表存放的路径</summary>
        public static readonly string Beans = $"{DataRoot}/{nameof(Beans)}";
        /// <summary>指令转储文件</summary>
        public static readonly string GenerateBatFile = $"{TempDir}/Luban_generate.bat";
        private static readonly Regex RemovalRedundantSeparatorsRegex = new Regex(@"[\\/]+", RegexOptions.Compiled);
        public static readonly string DataPathPrefix = GetSubpaths(RemovalRedundantSeparatorsRegex.Replace(MakeRelativePath(new Uri($"{Application.dataPath}/"), Data), "/"));
        public static string GetSubpaths(string path) => path.Contains('/') ? path.Substring(path.IndexOf('/') + 1) : path;
    }

    public static string MakeRelativePath(Uri src, string target) => Uri.UnescapeDataString(src.MakeRelativeUri(new Uri(target)).ToString());

#if UNITY_5_3_OR_NEWER
    public cfg.Tables Tables { get; set; }

    private byte[] LoadCfg(string file)
    {
        if (!LoadedAssets.ContainsKey(file))
        {
            var handle = YooAssets.LoadAssetSync<TextAsset>($"{Paths.DataPathPrefix}/{file}");
            LoadedAssets[file] = (handle.AssetObject as TextAsset, handle);
            handle.Release();
        }
        return LoadedAssets[file].Asset.bytes;
    }

    private ByteBuf LoadByteBuf(string file) => new ByteBuf(LoadCfg($"{file}.bytes"));
    private JSONNode LoadJson(string file) => JSON.Parse(Encoding.UTF8.GetString(LoadCfg($"{file}.json")));

    public cfg.Tables Load()
    {
        var tablesCtor = typeof(cfg.Tables).GetMethod("Load");
        if (tablesCtor == null)
            throw new NullReferenceException("无法获取Excel配置表定义，请检查是否导入了Excel配置表");
        var cfgType = tablesCtor.GetParameters()[0].ParameterType.GetGenericArguments()[1];
        // 根据cfg.Tables的构造函数的Loader的返回值类型决定使用json还是ByteBuf Loader
        return Tables = (cfg.Tables)tablesCtor.Invoke(null, new object[]{
                cfgType switch
                {
                    var t when t == typeof(ByteBuf) => (Func<string, ByteBuf>)LoadByteBuf,
                    var t when t == typeof(JSONNode) => (Func<string, JSONNode>)LoadJson,
                    _ => throw new NotImplementedException($"未实现{cfgType.FullName}类型配置数据的读取，请在此错误抛出位置实现它")
                }
            });

    }

    private IDictionary<string, (TextAsset Asset, AssetOperationHandle Handle)> LoadedAssets { get; set; }


    public void Init()
    {
        Debug.Log($"{nameof(ExcelManager)} Init...");
        LoadedAssets = new Dictionary<string, (TextAsset Asset, AssetOperationHandle Handle)>();
        Tables = Load();
        var assets = LoadedAssets;
        LoadedAssets = null;
        foreach (var (asset, handle) in assets.Values)
            handle.Release();
        Debug.Log($"{nameof(ExcelManager)} Inited.");
    }

    public void Destroy() { }
#endif
}
