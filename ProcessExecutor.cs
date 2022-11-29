using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CefDetector.Net
{
    public class ProcessExecutor : IDisposable
    {
        public event EventHandler<int>? OnExited;

        public event EventHandler<string>? OnOutputDataReceived;

        public event EventHandler<string>? OnErrorDataReceived;

        protected readonly Process _process;

        protected bool _started;

        public ProcessExecutor( string binPath ) : this( new ProcessStartInfo( binPath ) ) { }

        public ProcessExecutor( string binPath,
                                string arguments ) : this( new ProcessStartInfo( binPath, arguments ) ) { }

        public ProcessExecutor( ProcessStartInfo startInfo )
        {
            _process = new Process()
                       {
                           StartInfo = startInfo,
                           EnableRaisingEvents = true,
                       };
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardInput = true;
            _process.StartInfo.RedirectStandardError = true;
        }

        protected virtual void InitializeEvents()
        {
            _process.OutputDataReceived += ( sender,
                                             args ) =>
                                           {
                                               if ( args.Data != null )
                                               {
                                                   OnOutputDataReceived?.Invoke( sender, args.Data );
                                               }
                                           };
            _process.ErrorDataReceived += ( sender,
                                            args ) =>
                                          {
                                              if ( args.Data != null )
                                              {
                                                  OnErrorDataReceived?.Invoke( sender, args.Data );
                                              }
                                          };
            _process.Exited += ( sender,
                                 args ) =>
                               {
                                   if ( sender is Process process )
                                   {
                                       OnExited?.Invoke( sender, process.ExitCode );
                                   }
                                   else
                                   {
                                       OnExited?.Invoke( sender, _process.ExitCode );
                                   }
                               };
        }

        protected virtual void Start()
        {
            if ( _started )
            {
                return;
            }

            _started = true;

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            _process.WaitForExit();
        }

        public virtual async Task SendInput( string input )
        {
            try
            {
                await _process.StandardInput.WriteAsync( input! );
            }
            catch ( Exception e )
            {
                OnErrorDataReceived?.Invoke( _process, e.ToString() );
            }
        }

        public virtual int Execute()
        {
            InitializeEvents();
            Start();
            return _process.ExitCode;
        }

        public virtual async Task<int> ExecuteAsync()
        {
            InitializeEvents();
            return await Task.Run( () =>
                                   {
                                       Start();
                                       return _process.ExitCode;
                                   } )
                             .ConfigureAwait( false );
        }

        public virtual void Dispose()
        {
            _process.Dispose();
            OnExited = null;
            OnOutputDataReceived = null;
            OnErrorDataReceived = null;
        }
    }
}
