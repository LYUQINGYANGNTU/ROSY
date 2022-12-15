using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Azure.Devices.Client;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Azure.Devices;
using InHouseRobot_Body;
using Newtonsoft.Json;
using VoiceAssistantClient;
using VoiceAssistantClient;

namespace VoiceAssistantClient
{
    internal class robotnetwork
    {
        private static ServiceClient s_serviceClient;

        // Connection string for your IoT Hub
        // az iot hub show-connection-string --hub-name {your iot hub name} --policy-name service
        private static string s_connectionString = "HostName=robotnetwork.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=epdTWXHqZZhN8+l4I2mTTOoxjXQt+DAKfVlMILk+n8o=";

        private static DeviceClient s_deviceClient;
        private static readonly Microsoft.Azure.Devices.Client.TransportType s_transportType = Microsoft.Azure.Devices.Client.TransportType.Mqtt;

        //private static string s_connectionString = "HostName=robotnetwork.azure-devices.net;DeviceId=robot1;SharedAccessKey=TOjgoBGwvDPj2MJVMTmG5NxHx+i37cbcA6OJTeZwv1g=";

        public async static void sendcommunicationcommand(string status)
        {

            s_serviceClient = ServiceClient.CreateFromConnectionString(s_connectionString);

            await CommunicationInvokeMethodAsync(status);

            s_serviceClient.Dispose();
        }

        private void ValidateConnectionString(string[] args)
        {
            if (args.Any())
            {
                try
                {
                    var cs = Microsoft.Azure.Devices.Client.IotHubConnectionStringBuilder.Create(args[0]);
                    s_connectionString = cs.ToString();
                }
                catch (Exception)
                {
                    Console.WriteLine($"Error: Unrecognizable parameter '{args[0]}' as connection string.");
                    Environment.Exit(1);
                }
            }
            else
            {
                try
                {
                    _ = Microsoft.Azure.Devices.Client.IotHubConnectionStringBuilder.Create(s_connectionString);
                }
                catch (Exception)
                {
                    Console.WriteLine("This sample needs a device connection string to run. Program.cs can be edited to specify it, or it can be included on the command-line as the only parameter.");
                    Environment.Exit(1);
                }
            }
        }

        // Handle the direct method call
        private static Task<MethodResponse> Request(MethodRequest methodRequest, object userContext)
        {
            var data = Encoding.UTF8.GetString(methodRequest.Data);

            dynamic subdata = JsonConvert.DeserializeObject(methodRequest.Data.ToString());

            switch (methodRequest.Name)
            {
                case "navigation":
                    string goal = subdata.goal.ToString();

                    GlobalData.isNavigating = true;
                    GlobalData.RobotisReturning = false;
                    GlobalData.goallocation = goal;
                    GlobalData.Navitothegoalposition = true;

                    MainWindow.ChatbotRestartTimer.Stop();
                    MainWindow.NaviResumeTimer.Stop();

                    if (GlobalData.TourMode)
                    {
                        TourHelper.ResumeTimer.Stop();
                        TourHelper.ReturnTimer.Stop();
                        TourHelper.TourisInterruptedbyNavi = true;
                    }

                    BaseHelper.Go(goal);

                    break;
                case "direction":
                    BaseHelper.Move(methodRequest.Data.ToString());
                    break;
                case "vision":

                    string modelname = subdata.model.ToString();
                    string modelswitch = subdata.visionswitch.ToString();
                    if (modelswitch == "on" || modelswitch == "off")
                    {
                        GlobalData.visionswitch(modelname,modelswitch);
                    }
                    break;
                case "chatbot":
                    string chatbotswitch = subdata.chatbotswitch.ToString();

                    break;
                case "themeselect":
                    string themename = subdata.themename.ToString();

                    break;
                case "announcement":
                    string script = subdata.robotscript.ToString();
                    GlobalData.broadcastscript = script;
                    break;
                case "pathroutine":
                    string path = subdata.chatbotswitch.ToString();

                    GlobalData.TourMode = true;
                    TourHelper.GetTourInfo();
                    TourHelper.GoFirstPoint();
                    break;
                case "broadcast":

                    break;
                default:
                    break;
            }

            Debug.WriteLine(data);
            // Acknowlege the direct method call with a 200 success message
            string result = $"{{\"result\":\"Executed direct method: {methodRequest.Name}\"}}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));

        }

        private async static Task CommunicationInvokeMethodAsync(string status)
        {
            var methodInvocation = new CloudToDeviceMethod("communication")
            {

                ResponseTimeout = TimeSpan.FromSeconds(30),
            };

            if (status == "enable")
            {
                methodInvocation.SetPayloadJson("{\"status\": \"enable\"}");
            }
            else if (status == "disable")
            {
                methodInvocation.SetPayloadJson("{\"status\": \"disable\"}");
            }

            try
            {
                // Invoke the direct method asynchronously and get the response from the simulated device.
                var response = await s_serviceClient.InvokeDeviceMethodAsync("head1", methodInvocation);

                Debug.WriteLine($"\nResponse status: {response.Status}, payload:\n\t{response.GetPayloadAsJson()}");
            }

            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
