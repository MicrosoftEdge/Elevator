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

using Elevator;
using System.Threading;
using Xunit;

namespace ElevatorTests
{
    public class ClientIntegrationTests
    {
        [Fact]
        public void StartPassMessage()
        {
            TestMessage(Commands.START_PASS);
        }

        [Fact]
        public void StartBrowserMessage()
        {
            TestMessage(Commands.START_BROWSER);
        }

        [Fact]
        public void EndBrowserMessage()
        {
            TestMessage(Commands.END_BROWSER);
        }

        [Fact]
        public void EndPassMessage()
        {
            TestMessage(Commands.END_PASS);
        }

        private void TestMessage(string message)
        {
            using (ElevatorServer server = new ElevatorServer())
            {
                using (CancellationTokenSource tokenSource = new CancellationTokenSource())
                {
                    var cancelToken = tokenSource.Token;
                    var serverConnectTask = server.ConnectAsync(cancelToken);

                    using (var client = ElevatorClient.Create(true))
                    {
                        client.ConnectAsync().Wait();

                        serverConnectTask.Wait();

                        var commandTask = server.GetCommandAsync();
                        var clientTask = client.SendControllerMessageAsync(message);
                        commandTask.Wait();

                        Assert.Equal(message, commandTask.Result[0]);

                        server.AcknowledgeCommandAsync().Wait();
                        Assert.Equal("OK", clientTask.Result);
                    }
                }
            }
        }
    }
}
