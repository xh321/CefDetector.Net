using System.Runtime.InteropServices;
using CefDetector.Net;

Dictionary<string, CefType> cefList = new();

string findCmd = "";
string findArg = "";
switch ( Environment.OSVersion.Platform )
{
    case PlatformID.Win32NT :
        var diskList = DriveInfo.GetDrives()
                                .Where( i => i.DriveType != DriveType.CDRom &&
                                             i.DriveType != DriveType.NoRootDirectory &&
                                             i.DriveType != DriveType.Network &&
                                             i.DriveType != DriveType.Unknown )
                                .Select( i => i.RootDirectory.FullName.TrimEnd( '\\' ) )
                                .ToList();
        findCmd = "cmd";
        findArg = "/c ";
        List<string> argList = new();
        foreach ( var disk in diskList )
        {
            argList.Add( $" {disk} && cd / && dir /B /S *_percent.pak" );
        }

        findArg += string.Join( '&', argList );
        break;
    case PlatformID.Unix :
    case PlatformID.MacOSX :
        findCmd = "find";
        findArg = " / -name *_percent.pak";
        break;
    case PlatformID.Other :
        PrettyPrinter.WriteWarning("初始化", "操作系统类型未知，尝试使用Unix系命令进行搜索……");
        findCmd = "find";
        findArg = " / -name *_percent.pak";
        break;
}

PrettyPrinter.WriteInfo( "初始化", "正在计算chrome内核个数，请等待，下面是列表：" );
using var executor = new ProcessExecutor( findCmd, findArg );
executor.OnOutputDataReceived += ( sender,
                                   str ) =>
                                 {
                                     if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) &&
                                          //Windows下不搜索回收站和OneDrive
                                          ( str.Contains( "$RECYCLE.BIN" ) || ( str.Contains( "OneDrive" ) ) ) &&
                                          !( str.Contains( "找不到文件" ) )
                                        )
                                     {
                                         return;
                                     }

                                     var result = CefClassifier.SearchDir( str );
                                     foreach ( var (file, type) in result )
                                     {
                                         if ( !cefList.ContainsKey( file ) )
                                         {
                                             cefList.Add( file, type );
                                             PrettyPrinter.WriteInfo(type.ToString(), "{0}", file);
                                         }
                                     }
                                 };
executor.OnErrorDataReceived += ( sender,
                                  str ) =>
                                {};

executor.Execute();
PrettyPrinter.WriteInfo( "喜报", $"您系统里总共有{cefList.Count}个chrome内核！（可能有重复计算）" );
