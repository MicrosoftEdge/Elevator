//--------------------------------------------------------------
//
// Microsoft Edge Elevator
// Copyright(c) Microsoft Corporation
// All rights reserved.
//
// MIT License
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files(the ""Software""),
// to deal in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included
// in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS
// OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF
// OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
//--------------------------------------------------------------

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Elevator
{
    internal class Program
    {
        private const string DefaultTraceProfile = "DefaultTraceProfile.wprp";
        private static void Main(string[] args)
        {
            string traceProfile = "";

            if (args.Length > 0)
            {
                traceProfile = args[0];
            }
            else
            {
                Console.WriteLine("No tracing profile file was specified so setting {0} as the tracing profile...", DefaultTraceProfile);
                traceProfile = DefaultTraceProfile;
            }

            // If the trace profile doesn't exist exit the program.
            if (!File.Exists(traceProfile))
            {
                Console.WriteLine("Unable to find the trace profile \"{0}\". Make sure the path and name are correct.", traceProfile);
                Console.WriteLine("Exiting....");
                Environment.Exit(1);
            }

            CancellationTokenSource tokenSource = new CancellationTokenSource();

            Task task;
            using (var server = new ElevatorServer())
            {
                // run the control server in an asynchronous task            
#pragma warning disable CS4014 // We are using Task as a thread so await is not needed.
                task = new Task(() => RunTracingControlServer(server, traceProfile, tokenSource.Token), tokenSource.Token);
#pragma warning restore CS4014
                task.Start();

                Console.WriteLine("Press ESC to stop and exit the Tracing Controller.");

                // Wait until the user presses the ESC key.
                while (Console.ReadKey().Key != ConsoleKey.Escape)
                {
                }
                server.Shutdown();
                tokenSource.Cancel();
            }

            // cancel the server task and clean up before exiting
            try
            {
                task.Wait();
            }
            catch (OperationCanceledException)
            {
                // expected exception since we canceled the cancel token.
            }
            finally
            {
                tokenSource.Dispose();
            }
        }

        // This method runs the main loop of the tracing controller. 
        // It is run as an asynchronous task and takes a CancellationToken as a parameter.
        private static async Task RunTracingControlServer(ElevatorServer server, string traceProfile, CancellationToken cancelToken)
        {
            AutomateWPR wpr = new AutomateWPR(traceProfile);
            string etlFileName = "";
            string etlFolderPath = "";
            Regex invalidCharacters = new Regex(@"\W");

            Console.WriteLine("{0}: Tracing Controller Server starting....", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            while (!cancelToken.IsCancellationRequested)
            {
                bool isPassEnded = false;

                Console.WriteLine("{0}: Waiting for client connection.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                try
                {
                    await server.ConnectAsync(cancelToken);
                }
                catch (OperationCanceledException)
                {
                    continue;
                }

                // Begin interacting with the client
                while (!cancelToken.IsCancellationRequested && !isPassEnded)
                {
                    // A command line from the client is delimited by spaces
                    var messageTokens = await server.GetCommandAsync();

                    // the first token of the command line is the actual command
                    string command = messageTokens[0];

                    switch (command)
                    {
                        case Commands.START_PASS:
                            Console.WriteLine("{0}: Client is starting the test pass.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            // If there is more than one message token then the client has passed a folder path for where to save the ETL files.
                            if (messageTokens.Length > 1)
                            {
                                if (Directory.Exists(messageTokens[1]))
                                {
                                    etlFolderPath = messageTokens[1];
                                }
                                else
                                {
                                    throw new DirectoryNotFoundException("Passed in directory was not found! Directory: " + messageTokens[1]);
                                }
                            }
                            else
                            {
                                etlFolderPath = Directory.GetCurrentDirectory();
                            }
                            break;
                        case Commands.START_BROWSER:
                            string wprProfile = "defaultProfile";
                            bool useFileMode = true;
                            string recordingMode = "FileMode";

                            // If a client sends a message with 10 tokens then assume they passed a WPR profile name and the trace recording mode.
                            if (messageTokens.Length == 10)
                            {
                                // The seventh token is the WPR profile name.
                                wprProfile = messageTokens[7];

                                // Check if wprProfile contains any non-alphanumeric characters other than underscores.
                                if (invalidCharacters.IsMatch(wprProfile))
                                {
                                    Console.WriteLine("Invalid WPR Profile!");
                                    throw new Exception("The WPR Profile name is invalid!");
                                }

                                // The ninth token denotes the trace recording mode - either Memory or File.
                                if (messageTokens[9] == "Memory")
                                {
                                    useFileMode = false;
                                    recordingMode = "MemoryMode";
                                }
                                else
                                {
                                    useFileMode = true;
                                }
                            }

                            Console.WriteLine("{0}: -Starting- Iteration: {1}  Browser: {2}  Scenario: {3}  WPR Profile: {4}  TracingMode: {5}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), messageTokens[3], messageTokens[1], messageTokens[5], wprProfile, recordingMode);
                            Console.WriteLine("{0}: Starting tracing session.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

                            // first cancel any currently running trace sessions
                            wpr.CancelWPR();

                            // start tracing
                            wpr.StartWPR(wprProfile, useFileMode);

                            // create the ETL file name which we will use later
                            etlFileName = Path.Combine(etlFolderPath, messageTokens[1] + "_" + messageTokens[5] + "_" + messageTokens[3] + "_" + wprProfile + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".etl");

                            break;
                        case Commands.END_BROWSER:
                            Console.WriteLine("{0}: -Finished- Browser: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), messageTokens[1]);
                            Console.WriteLine("{0}: Stopping tracing session and saving as ETL: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), etlFileName);

                            // end tracing
                            wpr.StopWPR(etlFileName);

                            Console.WriteLine("{0}: Done saving ETL file: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), etlFileName);

                            break;
                        case Commands.END_PASS:
                            Console.WriteLine("{0}: Client test pass has ended.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            isPassEnded = true;

                            break;
                        case Commands.CANCEL_PASS:
                            Console.WriteLine("{0}: Cancelling tracing.", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                            wpr.CancelWPR();
                            break;
                        default:
                            throw new Exception($"Unknown command encountered: {command}");
                    } // switch (Command)

                    await server.AcknowledgeCommandAsync();
                } // while (!cancelToken.IsCancellationRequested && !isPassEnded)

                server.Disconnect();
            } // while (!cancelToken.IsCancellationRequested)
        }
    }
}