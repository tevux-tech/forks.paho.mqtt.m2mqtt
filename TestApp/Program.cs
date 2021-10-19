/*
Copyright (c) 2021 Simonas Greicius

All rights reserved. This program and the accompanying materials
are made available under the terms of the Eclipse Public License v1.0
and Eclipse Distribution License v1.0 which accompany this distribution. 

The Eclipse Public License is available at 
   http://www.eclipse.org/legal/epl-v10.html
and the Eclipse Distribution License is available at 
   http://www.eclipse.org/org/documents/edl-v10.php.

Contributors:
   Simonas Greicius - creation of TestApp
*/

using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using NLog;
using Tevux.Protocols.Mqtt;

namespace TestApp {
    class Program {
        static void Main() {
            // Configure NLog.
            var config = new NLog.Config.LoggingConfiguration();
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
            config.AddRule(LogLevel.Trace, LogLevel.Fatal, logconsole);
            LogManager.Configuration = config;

            // Run the test program
            _ = new MyTest();

            Thread.Sleep(-1);
        }
    }

    public class MyTest {
        public MyTest() {
            var client = new MqttClient();
            client.Initialize();
            client.PublishReceived += HandlePublishReceived;
            client.Subscribed += HandleSubscribed;
            client.Unsubscribed += HandleUnsubscribed;

            var mqttCertificate = new X509Certificate(Encoding.UTF8.GetBytes(_mqttCertificateString));

            var networkOptions = new ChannelConnectionOptions();
            // Mosquitto server is very slow and congested. Replace with local instance, if possible.
            networkOptions.SetHostname("test.mosquitto.org");
            networkOptions.SetPort(1883);
            networkOptions.SetReconnection(true);

            var brokerOptions = new MqttConnectionOptions();
            brokerOptions.SetClientId("TestApp");
            brokerOptions.SetRetransmissionParameters(3, 3);

            client.ConnectAndWait(networkOptions, brokerOptions);

            while (client.IsConnected == false) {
                Console.WriteLine("Connecting...");
                Thread.Sleep(500);
            };

            client.Subscribe("temp/testapp", QosLevel.AtMostOnce);
            client.Subscribe("temp/test-publish2", QosLevel.ExactlyOnce);

            Thread.Sleep(1000);

            client.Publish("temp/test-publish0", Encoding.UTF8.GetBytes("That's a QOS 0 publish message."), QosLevel.AtMostOnce, false);
            client.Publish("temp/test-publish1", Encoding.UTF8.GetBytes("That's a QOS 1 publish message."), QosLevel.AtLeastOnce, false);
            client.Publish("temp/test-publish2", Encoding.UTF8.GetBytes("That's a QOS 2 publish message."), QosLevel.ExactlyOnce, false);

            Thread.Sleep(5000);

            client.Unsubscribe("temp/testapp");

            Console.ReadLine();

            client.DisconnectAndWait();
        }

        private void HandleUnsubscribed(object sender, UnsubscribedEventArgs e) {
            Console.WriteLine($"Unsubscribed: {e.Topic}");
        }

        private void HandleSubscribed(object sender, SubscribedEventArgs e) {
            Console.WriteLine($"Subscribed: {e.Topic}:{e.GrantedQosLevel}");
        }

        private void HandlePublishReceived(object sender, PublishReceivedEventArgs e) {
            Console.WriteLine($"Received: {Encoding.UTF8.GetString(e.Message)}");
        }

        private readonly string _mqttCertificateString = @"-----BEGIN CERTIFICATE-----
MIIEAzCCAuugAwIBAgIUBY1hlCGvdj4NhBXkZ/uLUZNILAwwDQYJKoZIhvcNAQEL
BQAwgZAxCzAJBgNVBAYTAkdCMRcwFQYDVQQIDA5Vbml0ZWQgS2luZ2RvbTEOMAwG
A1UEBwwFRGVyYnkxEjAQBgNVBAoMCU1vc3F1aXR0bzELMAkGA1UECwwCQ0ExFjAU
BgNVBAMMDW1vc3F1aXR0by5vcmcxHzAdBgkqhkiG9w0BCQEWEHJvZ2VyQGF0Y2hv
by5vcmcwHhcNMjAwNjA5MTEwNjM5WhcNMzAwNjA3MTEwNjM5WjCBkDELMAkGA1UE
BhMCR0IxFzAVBgNVBAgMDlVuaXRlZCBLaW5nZG9tMQ4wDAYDVQQHDAVEZXJieTES
MBAGA1UECgwJTW9zcXVpdHRvMQswCQYDVQQLDAJDQTEWMBQGA1UEAwwNbW9zcXVp
dHRvLm9yZzEfMB0GCSqGSIb3DQEJARYQcm9nZXJAYXRjaG9vLm9yZzCCASIwDQYJ
KoZIhvcNAQEBBQADggEPADCCAQoCggEBAME0HKmIzfTOwkKLT3THHe+ObdizamPg
UZmD64Tf3zJdNeYGYn4CEXbyP6fy3tWc8S2boW6dzrH8SdFf9uo320GJA9B7U1FW
Te3xda/Lm3JFfaHjkWw7jBwcauQZjpGINHapHRlpiCZsquAthOgxW9SgDgYlGzEA
s06pkEFiMw+qDfLo/sxFKB6vQlFekMeCymjLCbNwPJyqyhFmPWwio/PDMruBTzPH
3cioBnrJWKXc3OjXdLGFJOfj7pP0j/dr2LH72eSvv3PQQFl90CZPFhrCUcRHSSxo
E6yjGOdnz7f6PveLIB574kQORwt8ePn0yidrTC1ictikED3nHYhMUOUCAwEAAaNT
MFEwHQYDVR0OBBYEFPVV6xBUFPiGKDyo5V3+Hbh4N9YSMB8GA1UdIwQYMBaAFPVV
6xBUFPiGKDyo5V3+Hbh4N9YSMA8GA1UdEwEB/wQFMAMBAf8wDQYJKoZIhvcNAQEL
BQADggEBAGa9kS21N70ThM6/Hj9D7mbVxKLBjVWe2TPsGfbl3rEDfZ+OKRZ2j6AC
6r7jb4TZO3dzF2p6dgbrlU71Y/4K0TdzIjRj3cQ3KSm41JvUQ0hZ/c04iGDg/xWf
+pp58nfPAYwuerruPNWmlStWAXf0UTqRtg4hQDWBuUFDJTuWuuBvEXudz74eh/wK
sMwfu1HFvjy5Z0iMDU8PUDepjVolOCue9ashlS4EB5IECdSR2TItnAIiIwimx839
LdUdRudafMu5T5Xma182OC0/u/xRlEm+tvKGGmfFcN0piqVl8OrSPBgIlb+1IKJE
m/XriWr/Cq4h/JfB7NTsezVslgkBaoU=
-----END CERTIFICATE-----
";
    }
}
