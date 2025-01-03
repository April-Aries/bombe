﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using Newtonsoft.Json;

namespace EDRPOC
{
    internal class Program
    {
        //const string SECRET = "2Fvw1LvEfaCIwJ8sQhiHFPR4krnXCzKM";
        const string SECRET = "2Fvw1LvEfaCIwJ8sQhiHFPR4krnXCzKM";

        // Dictionary to store process ID (PID) to executable filename mapping
        private static Dictionary<int, string> processIdToExeName = new Dictionary<int, string>();

        // Flag to ensure the answer is sent only once
        private static bool answerSent = false;

        static async Task Main(string[] args)
        {
            using (var kernelSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName))
            {
                Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e) { kernelSession.Dispose(); };

                kernelSession.EnableKernelProvider(
                    KernelTraceEventParser.Keywords.ImageLoad |
                    KernelTraceEventParser.Keywords.Process |
                    KernelTraceEventParser.Keywords.DiskFileIO |
                    KernelTraceEventParser.Keywords.FileIOInit |
                    KernelTraceEventParser.Keywords.FileIO |
                    KernelTraceEventParser.Keywords.Registry
                );

                kernelSession.Source.Kernel.ProcessStart += processStartedHandler;
                kernelSession.Source.Kernel.ProcessStop += processStoppedHandler;
                kernelSession.Source.Kernel.FileIORead += fileReadHandler;
                kernelSession.Source.Kernel.RegistryQuery += registryReadHandler;

                kernelSession.Source.Process();
            }
        }

        private static async void registryReadHandler(RegistryTraceData data)
        {
            // Check if the answer has already been sent
            if(answerSent) return;

            string keyName = data.KeyName;

            // Filter for the specific registry path
            if (!string.IsNullOrEmpty(keyName) && keyName.StartsWith(@"SOFTWARE\BOMBE"))
            {
                string exeName = null;
                lock (processIdToExeName)
                {
                    processIdToExeName.TryGetValue(data.ProcessID, out exeName);
                }
                Console.WriteLine($"[+] Registry read detected: {data.KeyName}, process {exeName} with PID {data.ProcessID}");

                // Send the executable filename to the server
                if (!string.IsNullOrEmpty(exeName))
                {
                    await SendAnswerToServer(JsonConvert.SerializeObject(
                        new
                        {
                            answer = exeName,
                            secret = SECRET
                        }
                    ));

                    // Set the flag to true to disable further handling
                    answerSent = true;
                }
            }
        }

        private static async void processStartedHandler(ProcessTraceData data)
        {
            lock (processIdToExeName)
            {
                processIdToExeName[data.ProcessID] = data.ImageFileName;
                Console.WriteLine($"[+] Process start: {data.ProcessID}.{data.ProcessName}");
            }

            if(!answerSent && data.ParentID != 0 && processIdToExeName.TryGetValue(data.ParentID, out var parentName))
            {
                if(data.ImageFileName.ToLower() == "cmd.exe")
                {
                    var args = data.CommandLine?.ToLower();
                    if(args != null && args.Contains("copy") && args.Contains("login data"))
                    {
                        Console.WriteLine("Suspicious cmd: {0}, process: {1} with pid {2}, parent process: {3}", data.CommandLine, data.ProcessName, data.ProcessID, parentName);
                        await SendAnswerToServer(JsonConvert.SerializeObject(
                            new
                            {
                                answer = parentName,
                                secret = SECRET
                            }
                        ));

                        // Set the flag to true to disable further handling
                        answerSent = true;
                    }
                }
            }
        }

        private static void processStoppedHandler(ProcessTraceData data)
        {
            lock (processIdToExeName)
            {
                processIdToExeName.Remove(data.ProcessID);
                Console.WriteLine($"[-] Process terminated: {data.ProcessID}.{data.ProcessName}");
            }
        }

        private static async void fileReadHandler(FileIOReadWriteTraceData data)
        {
            // Check if the answer has already been sent
            if (answerSent) return;

            // Define the full path to the target file
            string targetFilePath = ("C:\\Users\\bombe\\AppData\\Local\\bhrome\\Login Data").ToLower();

            if (data.FileName.ToLower().Equals(targetFilePath))
            {
                string exeName = null;
                lock (processIdToExeName)
                {
                    processIdToExeName.TryGetValue(data.ProcessID, out exeName);
                }

                if (exeName == null || !exeName.StartsWith("BOMBE_EDR_FLAG_")) return;

                Console.WriteLine("File read: {0}, process: {1} with pid {2}, exe: {3}", data.FileName, data.ProcessName, data.ProcessID, exeName);

                // Send the executable filename to the server
                if (!string.IsNullOrEmpty(exeName))
                {
                    await SendAnswerToServer(JsonConvert.SerializeObject(
                        new
                        {
                            answer = exeName,
                            secret = SECRET
                        }
                    ));

                    // Set the flag to true to disable further handling
                    answerSent = true;
                }
            }
        }

        private static async Task SendAnswerToServer(string jsonPayload)
        {
            using (HttpClient client = new HttpClient())
            {
                StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                try
                {
                    HttpResponseMessage response = await client.PostAsync("https://x.bombe.digitalplaguedoctors.com/submitEdrAns", content);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response: {responseBody}");
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Request error: {e.Message}");
                }

                Console.ReadKey();
            }
        }
    }
}
