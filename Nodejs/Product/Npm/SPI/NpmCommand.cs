﻿/* ****************************************************************************
 *
 * Copyright (c) Microsoft Corporation. 
 *
 * This source code is subject to terms and conditions of the Apache License, Version 2.0. A 
 * copy of the license can be found in the License.html file at the root of this distribution. If 
 * you cannot locate the Apache License, Version 2.0, please send an email to 
 * vspython@microsoft.com. By using this source code in any fashion, you are agreeing to be bound 
 * by the terms of the Apache License, Version 2.0.
 *
 * You must not remove this notice, or any other, from this software.
 *
 * ***************************************************************************/

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.NodejsTools.Npm.SPI{
    internal abstract class NpmCommand{
        private readonly string _fullPathToRootPackageDirectory;
        private string _pathToNpm;
        private bool _useFallbackIfNpmNotFound;
        private bool _cancelled;
        private Process _process;
        private StringBuilder _output = new StringBuilder();
        private StringBuilder _error = new StringBuilder();
        private object _bufferLock = new object();

        protected NpmCommand(
            string fullPathToRootPackageDirectory,
            string pathToNpm = null,
            bool useFallbackIfNpmNotFound = true){
            _fullPathToRootPackageDirectory = fullPathToRootPackageDirectory;
            _pathToNpm = pathToNpm;
            _useFallbackIfNpmNotFound = useFallbackIfNpmNotFound;
        }

        protected string Arguments { get; set; }

        private string GetPathToNpm(){
            if (null == _pathToNpm || ! File.Exists(_pathToNpm)){
                if (_useFallbackIfNpmNotFound){
                    string match = null;
                    foreach (var potential in Environment.GetEnvironmentVariable("path").Split(Path.PathSeparator))
                    {
                        var path = Path.Combine(potential, "npm.cmd");
                        if (File.Exists(path))
                        {
                            if (null == match ||
                                path.Contains(
                                    string.Format(
                                        "{0}nodejs{1}",
                                        Path.DirectorySeparatorChar,
                                        Path.DirectorySeparatorChar)))
                            {
                                match = path;
                            }
                        }
                    }

                    if (null != match){
                        _pathToNpm = match;
                    }

                    //  That second condition deals with the situation where no match is found.
                    if (null == _pathToNpm || ! File.Exists(_pathToNpm)){
                        throw new NpmNotFoundException(
                            string.Format(
                                "Cannot find npm.cmd at '{0}' nor on your system PATH. Ensure node.js is installed.",
                                _pathToNpm));
                    }
                } else{
                    throw new NpmNotFoundException(
                        string.Format("Cannot find npm.cmd at specified path: {0}", _pathToNpm));
                }
            }
            return _pathToNpm;
        }

        private void CopyEnvironmentVariables(ProcessStartInfo target){
            foreach (DictionaryEntry kvp in Environment.GetEnvironmentVariables()){
                target.EnvironmentVariables[(string) kvp.Key] = (string) kvp.Value;
            }
        }

        private ProcessStartInfo BuildStartInfo(){
            var info = new ProcessStartInfo(GetPathToNpm(), Arguments);
            info.WorkingDirectory = _fullPathToRootPackageDirectory;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            CopyEnvironmentVariables(info);

            return info;
        }

        public string StandardOutput{
            get{
                lock (_bufferLock){
                    return _output.ToString();
                }
            }
        }

        public string StandardError{
            get{
                lock (_bufferLock){
                    return _error.ToString();
                }
            }
        }

        public void CancelCurrentTask(){
            if (null != _process){
                try{
                    _process.Kill();
                } catch (Win32Exception){} catch (InvalidOperationException){}
                _cancelled = true;
            }
        }

        private void WaitForExit(){
            while (!_process.HasExited && !_cancelled){
                _process.WaitForExit(5000);
            }
        }

        public virtual async Task<bool> ExecuteAsync(){
            using (_process = new Process()){
                _process.StartInfo = BuildStartInfo();

                try{
                    _process.Start();
                } catch (Win32Exception we){
                    throw new NpmExecutionException(
                        string.Format("Error executing npm - unable to start the npm process: {0}", we.Message),
                        we);
                }

                _process.ErrorDataReceived += _process_ErrorDataReceived;
                _process.OutputDataReceived += _process_OutputDataReceived;

                _process.BeginErrorReadLine();
                _process.BeginOutputReadLine();

                await Task.Run(() => WaitForExit());
            }

            return true;
        }

        private void AppendToBuffer(StringBuilder buffer, DataReceivedEventArgs e){
            lock (_bufferLock){
                if (buffer.Length > 0){
                    buffer.Append(Environment.NewLine);
                }

                var data = e.Data;
                if (!string.IsNullOrEmpty(data)){
                    buffer.Append(Encoding.UTF8.GetString(Console.OutputEncoding.GetBytes(e.Data)));
                }
            }
        }

        void _process_OutputDataReceived(object sender, DataReceivedEventArgs e){
            AppendToBuffer(_output, e);
        }

        void _process_ErrorDataReceived(object sender, DataReceivedEventArgs e){
            AppendToBuffer(_error, e);
        }
    }
}