using CefDetector.Net;

Dictionary<string, CefType> cefList = new();

Console.WriteLine("正在计算chrome内核个数，请等待，下面是列表：");
using var executor = new ProcessExecutor( "find", " / -name *_percent.pak" );
executor.OnOutputDataReceived += ( sender,
                                   str ) =>
                                 {
                                     var result = CefClassifier.SearchDir( str );
                                     foreach ( var (file, type) in result )
                                     {
                                         if ( !cefList.ContainsKey( file ) )
                                         {
                                             cefList.Add( file, type );
                                             Console.WriteLine( type + "|" + file );
                                         }
                                     }
                                 };
executor.OnErrorDataReceived += ( sender,
                                  str ) =>
                                {
                                    //Console.WriteLine( "ERR：" + str );
                                };
executor.Execute();
Console.WriteLine($"喜报：您系统里总共有{cefList.Count}个chrome内核！（可能有重复计算）");