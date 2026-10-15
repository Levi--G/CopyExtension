using Avalonia.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;

namespace CopyExtension
{
    internal class SingleInstance()
    {
        private const string MutexName = "MUTEX_CopyExtensionSingleInstance";
        private const string PipeName = @"LOCAL\CopyExtensionServerPipe";
        Cmd cmd = new();
        private Mutex? mutex;
        CancellationTokenSource cts = new();

        public void Execute(string[] args)
        {
            try
            {
                var parsed = cmd.Parse(args);
                if (parsed.Errors.Any())
                {
                    cmd.Execute(parsed);
                    return;
                }
                if (!IsFirstInstance())
                {
                    int tries = 0;
                    do
                    {
                        if (ConnectPipeClient(args))
                        {
                            return;
                        }
                        Thread.Sleep(100);
                        tries++;
                    } while (!IsFirstInstance() && tries <= 5);
                    if (!IsFirstInstance())
                    {
                        Program.Logger.Log(this, "FATAL: Unable to get mutex and unable to send message, the remote process might have crashed!");
                    }
                }
                StartPipeServer();
                cmd.Execute(parsed);
            }
            finally
            {
                Cleanup();
            }
        }
        void Cleanup()
        {
            cts.Cancel();
            mutex?.Dispose();
        }
        void StartPipeServer()
        {
            Task.Run(async () =>
            {
                try
                {
                    // Create a new pipe accessible by local authenticated users, disallow network
                    var sidNetworkService = new SecurityIdentifier(WellKnownSidType.NetworkServiceSid, null);
                    var sidWorld = new SecurityIdentifier(WellKnownSidType.WorldSid, null);

                    var pipeSecurity = new PipeSecurity();

                    // Deny network access to the pipe
                    var accessRule = new PipeAccessRule(sidNetworkService, PipeAccessRights.ReadWrite, AccessControlType.Deny);
                    pipeSecurity.AddAccessRule(accessRule);

                    // Current user is the owner
                    SecurityIdentifier? sidOwner = WindowsIdentity.GetCurrent().Owner;
                    if (sidOwner != null)
                    {
                        accessRule = new PipeAccessRule(sidOwner, PipeAccessRights.ReadWrite, AccessControlType.Allow);
                        pipeSecurity.AddAccessRule(accessRule);
                    }
                    var token = cts.Token;
                    var errors = 0;
                    while (!token.IsCancellationRequested && errors < 100)
                    {
                        using var s = NamedPipeServerStreamAcl.Create(PipeName, PipeDirection.In, 1, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, pipeSecurity);
                        try
                        {
                            await s.WaitForConnectionAsync(token);
                            string? command = null;
                            using (var sw = new StreamReader(s))
                            {
                                var icts = CancellationTokenSource.CreateLinkedTokenSource(token);
                                icts.CancelAfter(2000);
                                var itoken = icts.Token;
                                try
                                {
                                    command = await sw.ReadLineAsync(icts.Token);
                                    Program.Logger.Log(this, "Received command: " + command);
                                }
                                catch (OperationCanceledException ex) when (ex.CancellationToken == itoken) { }
                            }
                            if (!string.IsNullOrEmpty(command))
                            {
                                _ = Task.Run(() =>
                                {
                                    var parsed = cmd.Parse(command);
                                    if (!parsed.Errors.Any())
                                    {
                                        cmd.Execute(parsed);
                                    }
                                });
                            }
                        }
                        catch (IOException e)
                        {
                            Program.Logger.Log(this, e);
                            await Task.Delay(100);
                            errors++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Program.Logger.Log(this, ex);
                }
            });
        }
        bool ConnectPipeClient(string[] args)
        {
            var cmd = Cmd.GetCommandline(args);
            try
            {
                using var s = new NamedPipeClientStream(".", PipeName, PipeDirection.Out, PipeOptions.None);
                s.Connect(1000);
                if (s.IsConnected)
                {
                    using var sw = new StreamWriter(s);
                    //Program.Logger.Log(this, $"Sending: {cmd}");
                    sw.WriteLine(cmd);
                    sw.Flush();
                    s.WaitForPipeDrain();
                    return true;
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Log(this, ex);
            }
            return false;
        }
        private bool IsFirstInstance()
        {
            if (mutex == null)
            {
                mutex = new Mutex(true, MutexName, out var _firstApplicationInstance);
                if (!_firstApplicationInstance)
                {
                    mutex.Dispose();
                    mutex = null;
                    return false;
                }
            }
            return true;
        }
    }
}
