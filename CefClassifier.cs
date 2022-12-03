using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Security.AccessControl;

namespace CefDetector.Net
{
    /// <summary>
    /// CEF类型枚举
    /// </summary>
    public enum CefType
    {
        UNKNOWN,
        EDGE,
        CHROME,
        JCEF,
        LIBCEF,
        ELECTRON,
        ELECTRON2,
        CEF_SHARP,
        NWJS,
        MINI_ELECTRON,
        MINI_BLINK
    }

    public static class CefClassifier
    {
        /// <summary>
        /// 储存已经搜索过的目录（不重复搜索）
        /// </summary>
        public static List<string> searchedDir = new();

        /// <summary>
        /// 根据文件字节中的特征码确定CEF类型
        /// </summary>
        public static Dictionary<CefType, byte[]> CefByteFingerprint = new()
                                                                       {
                                                                           {
                                                                               CefType.LIBCEF,
                                                                               Encoding.UTF8
                                                                                       .GetBytes( "cef_string_utf8_to_utf16" )
                                                                           },
                                                                           {
                                                                               CefType.ELECTRON,
                                                                               Encoding.UTF8
                                                                                       .GetBytes( "third_party/electron_node" )
                                                                           },
                                                                           {
                                                                               CefType.ELECTRON2,
                                                                               Encoding.UTF8
                                                                                       .GetBytes( "register_atom_browser_web_contents" )
                                                                           },
                                                                           {
                                                                               CefType.CEF_SHARP,
                                                                               Encoding.UTF8
                                                                                       .GetBytes( "CefSharp.Internals" )
                                                                           },
                                                                           {
                                                                               CefType.NWJS,
                                                                               Encoding.UTF8.GetBytes( "url-nwjs" )
                                                                           },
                                                                           {
                                                                               CefType.MINI_ELECTRON,
                                                                               Encoding.UTF8
                                                                                       .GetBytes( "napi_create_buffer" )
                                                                           },
                                                                           {
                                                                               CefType.MINI_BLINK,
                                                                               Encoding.UTF8.GetBytes( "miniblink" )
                                                                           }
                                                                       };

        /// <summary>
        /// 根据自定义的函数确认CEF类型
        /// </summary>
        public static Dictionary<(CefType type, PlatformID platform), Func<DirectoryInfo, (bool isSucc, string file)>>
            CefCustomCheck = new()
                             {
                                 {
                                     ( CefType.EDGE, PlatformID.Win32NT ),
                                     ( dir ) =>
                                     {
                                         var file = Path.Join( dir.FullName, "msedge.exe" );
                                         return ( File.Exists( file ), new FileInfo( file ).FullName );
                                     }
                                 },
                                 {
                                     ( CefType.EDGE, PlatformID.Unix ),
                                     ( dir ) =>
                                     {
                                         string file = "";
                                         if ( RuntimeInformation.IsOSPlatform( OSPlatform.OSX ) )
                                         {
                                             //Contents/Frameworks/Microsoft Edge Framework.framework/Versions/107.0.1418.62
                                             //Contents/Frameworks/Microsoft Edge Framework.framework/Versions/107.0.1418.62/Resources
                                             file = Path.Join( dir.Parent.FullName, "Microsoft Edge Framework" );
                                         }
                                         else
                                         {
                                             file = Path.Join( dir.FullName, "msedge" );
                                         }

                                         return ( File.Exists( file ), new FileInfo( file ).FullName );
                                     }
                                 },
                                 {
                                     ( CefType.CHROME, PlatformID.Win32NT ),
                                     ( dir ) =>
                                     {
                                         var file1 = Path.Join( dir.FullName, "chrome_pwa_launcher.exe" );
                                         var file2 = Path.Join( dir.FullName, "../chrome.exe" );
                                         var file3 = Path.Join( dir.FullName, "chrome.exe" );
                                         return
                                             ( File.Exists( file1 ) &&
                                               ( File.Exists( file2 ) || File.Exists( file3 ) ),
                                               File.Exists( file2 )
                                                   ? new FileInfo( file2 ).FullName
                                                   : new FileInfo( file3 ).FullName );
                                     }
                                 },
                                 {
                                     ( CefType.CHROME, PlatformID.Unix ),
                                     ( dir ) =>
                                     {
                                         //TODO: MacOS可能需要修改
                                         var file = Path.Join( dir.FullName, "chrome" );
                                         return ( File.Exists( file ), new FileInfo( file ).FullName );
                                     }
                                 },

                                 {
                                     ( CefType.JCEF, PlatformID.Win32NT ),
                                     ( dir ) =>
                                     {
                                         var file = Path.Join( dir.FullName, "../bin/jcef.dll" );
                                         return ( File.Exists( file ), new FileInfo( file ).FullName );
                                     }
                                 },
                                 {
                                     ( CefType.JCEF, PlatformID.Unix ),
                                     ( dir ) =>
                                     {
                                         //TODO: 需要修改
                                         var file = Path.Join( dir.FullName, "jrt-fs.jar" );
                                         return ( File.Exists( file ), new FileInfo( file ).FullName );
                                     }
                                 },
                             };

        /// <summary>
        /// 确认某文件中是否包含某些字节
        /// </summary>
        /// <param name="file"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        private static bool FileByteContains( byte[] file,
                                              byte[] search )
        {
            return Parallel.For( 0,
                                 file.Length - search.Length,
                                 ( i,
                                   loopState ) =>
                                 {
                                     if ( file[ i ] == search[ 0 ] )
                                     {
                                         byte[] localCache = new byte[ search.Length ];
                                         Array.Copy( file, i, localCache, 0, search.Length );
                                         if ( localCache.SequenceEqual( search ) )
                                             loopState.Stop();
                                     }
                                 } )
                           .IsCompleted ==
                   false;
        }

        /// <summary>
        /// 过滤出带有可执行权限的文件或者可执行文件（Windows），减少后续判断次数
        /// </summary>
        /// <param name="files">文件列表</param>
        /// <returns></returns>
        public static List<string> FilterExecuteBinaries( List<string> files )
        {
            if ( Environment.OSVersion.Platform == PlatformID.Unix )
            {
                List<string> executeBinaries = new();
                foreach ( string file in files )
                {
                    FileInfo fi = new FileInfo( file );
                    if ( fi.UnixFileMode.HasFlag( UnixFileMode.UserExecute |
                                                  UnixFileMode.GroupExecute |
                                                  UnixFileMode.OtherExecute ) )
                    {
                        executeBinaries.Add( file );
                    }
                }

                return executeBinaries;
            }
            else if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
            {
                return files.Where( i => i.EndsWith( ".exe" ) ).ToList();
            }
            else
            {
                return files.ToList();
            }
        }

        /// <summary>
        /// 根据文件夹结构特征直接推断出程序
        /// </summary>
        /// <param name="dirPath">目录</param>
        /// <returns></returns>
        public static Dictionary<string, CefType> CheckDirCefType( string dirPath )
        {
            if ( !Directory.Exists( dirPath ) ) return new();

            DirectoryInfo dir = new DirectoryInfo( dirPath );
            foreach ( var (cefType, checkFunc) in CefCustomCheck )
            {
                //筛选操作系统
                if ( cefType.platform != Environment.OSVersion.Platform ) continue;

                var (isSucc, file) = checkFunc( dir );
                if ( isSucc )
                {
                    return new()
                           {
                               {file, cefType.type}
                           };
                }
            }

            return new();
        }

        /// <summary>
        /// 读取二进制文件字节，根据特征码判断CEF类型
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns></returns>
        public static CefType CheckBinaryCefType( string filePath )
        {
            if ( !File.Exists( filePath ) ) return CefType.UNKNOWN;
            try
            {
                byte[] fileContent = File.ReadAllBytes( filePath );
                CefType result = CefType.UNKNOWN;

                //浏览器文件字节检查
                foreach ( var (type, bytes) in CefByteFingerprint )
                {
                    if ( FileByteContains( fileContent, bytes ) )
                    {
                        return type;
                    }
                }

                return result;
            }
            catch { }

            return CefType.UNKNOWN;
        }

        /// <summary>
        /// 遍历文件所在目录（甚至上几级目录）来判断目录中CEF的类型
        /// </summary>
        /// <param name="cefPath">特征文件路径（pak文件等）</param>
        /// <returns></returns>
        public static Dictionary<string, CefType> SearchDir( string cefPath )
        {
            if ( !File.Exists( cefPath ) ) return new();
            Dictionary<string, CefType> cefBinaries = new();

            var dir = new FileInfo( cefPath ).Directory!;

            //不搜索之前搜索过的目录
            if ( searchedDir.Contains( dir.FullName ) )
            {
                return new();
            }
            else
            {
                searchedDir.Add( dir.FullName );
            }

            //先根据目录检查
            var dirCheckResult = CheckDirCefType( dir.FullName );
            if ( dirCheckResult.Any() )
            {
                return dirCheckResult;
            }

            //再搜索目录内所有文件，检查字节
            var currDir = new DirectoryInfo( dir.FullName );
            var files = new List<string>();
            do
            {
                files = FilterExecuteBinaries( currDir.GetFiles( "*", SearchOption.TopDirectoryOnly )
                                                      .Select( i => i.FullName )
                                                      .ToList() );

                //如果没搜到可执行文件，则向上一层目录搜索
                currDir = currDir.Parent;
                if ( currDir == null )
                {
                    //已经到根目录了，则搜不到了
                    return new();
                }
            } while ( !files.Any() );

            foreach ( var file in files )
            {
                if ( !cefBinaries.ContainsKey( file ) )
                {
                    var result = CheckBinaryCefType( file );
                    if ( result != CefType.UNKNOWN )
                    {
                        cefBinaries.Add( file, result );
                    }
                }
            }

            return cefBinaries;
        }
    }
}
