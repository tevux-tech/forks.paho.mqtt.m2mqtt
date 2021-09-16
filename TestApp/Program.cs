using System;
using System.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Utility;

namespace TestApp {
    class Program {
        static void Main(string[] args) {
            Trace.TraceListener = (format, data) => { Console.WriteLine(format, data); };

            var myTest = new MyTest();

            Thread.Sleep(-1);
        }
    }

    public class MyTest {
        public MyTest() {

            var client = new MqttClient("172.16.0.2");
            client.MqttMsgPublishReceived += HandlePublishReceived;
            client.Connect("TestApp");

            client.Subscribe(new[] { "temp/testapp" }, new[] { QosLevel.AtMostOnce });

            Thread.Sleep(5000);

            client.Unsubscribe(new[] { "temp/testapp" });

            Thread.Sleep(1000);

            // client.Disconnect();
        }
        private void HandlePublishReceived(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs e) {

        }
    }
}
