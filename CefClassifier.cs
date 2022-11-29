using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.AccessControl;
using Mono.Unix;

namespace CefDetector.Net
{
    public enum CefType
    {
        UNKNOWN,
        EDGE,
        CHROME,
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
        public static List<string> searchedDir = new();

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

        public static Dictionary<CefType, Func<DirectoryInfo, (bool isSucc, string file)>>
            CefBaseCheck = new()
                           {
                               {
                                   CefType.EDGE,
                                   ( dir ) =>
                                   {
                                       var file = Path.Join( dir.FullName, "msedge" );
                                       return ( File.Exists( file ), file );
                                   }
                               },
                               {
                                   CefType.CHROME,
                                   ( dir ) =>
                                   {
                                       var file = Path.Join( dir.FullName, "chrome" );
                                       return ( File.Exists( file ), file );
                                   }
                               },
                           };

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

        public static List<string> FilterExecuteBinaries( string[] files )
        {
            List<string> executeBinaries = new();
            foreach ( string file in files )
            {
                Mono.Unix.UnixFileInfo ufi = new(file);
                if ( ufi.FileAccessPermissions.HasFlag( FileAccessPermissions.UserExecute |
                                                        FileAccessPermissions.GroupExecute |
                                                        FileAccessPermissions.OtherExecute ) )
                {
                    executeBinaries.Add( file );
                }
            }

            return executeBinaries;
        }

        public static Dictionary<string, CefType> CheckDirCefType( string dirPath )
        {
            if ( !Directory.Exists( dirPath ) ) return new();

            DirectoryInfo dir = new DirectoryInfo( dirPath );
            foreach ( var (type, checkFunc) in CefBaseCheck )
            {
                var (isSucc, file) = checkFunc( dir );
                if ( isSucc )
                {
                    return new()
                           {
                               {file, type}
                           };
                }
            }

            return new();
        }

        public static CefType CheckBinaryCefType( string filePath )
        {
            if ( !File.Exists( filePath ) ) return CefType.UNKNOWN;

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
            var files = FilterExecuteBinaries( Directory.GetFiles( dir.FullName, "*", SearchOption.TopDirectoryOnly ) );
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
