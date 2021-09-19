/*
Copyright (c) 2013, 2014 Paolo Patierno

All rights reserved. This program and the accompanying materials
are made available under the terms of the Eclipse Public License v1.0
and Eclipse Distribution License v1.0 which accompany this distribution. 

The Eclipse Public License is available at 
   http://www.eclipse.org/legal/epl-v10.html
and the Eclipse Distribution License is available at 
   http://www.eclipse.org/org/documents/edl-v10.php.

Contributors:
   Paolo Patierno - initial API and implementation and/or initial documentation
*/

using System.Threading;
using uPLibrary.Networking.M2Mqtt.Messages;
using static uPLibrary.Networking.M2Mqtt.Messages.MqttMsgConnack;

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        /// <summary>
        /// Connect to broker
        /// </summary>
        /// <returns>Return code of CONNACK message from broker</returns>
        public ReturnCodes Connect() {
            return Connect(new ChannelConnectionOptions(), new MqttConnectionOptions());
        }

        public ReturnCodes Connect(ChannelConnectionOptions channelConnectionOptions) {
            return Connect(channelConnectionOptions, new MqttConnectionOptions());
        }


        /// <summary>
        /// Connect to broker
        /// </summary>
        /// <returns>Return code of CONNACK message from broker</returns>
        public ReturnCodes Connect(ChannelConnectionOptions channelConnectionOptions, MqttConnectionOptions mqttConnectionOptions) {
            var connectMessage = new MqttMsgConnect(mqttConnectionOptions);

            var isOk = true;

            if (_channel.TryConnect(channelConnectionOptions.Hostname, channelConnectionOptions.Port) == false) {
                isOk = false;
            };

            if (isOk) {
                LastCommTime = 0;
                _isConnectionClosing = false;
            }

            _connectStateMachine.Connect(connectMessage);
            while (_connectStateMachine.IsConnectionCompleted == false) {
                _connectStateMachine.Tick();
                Thread.Sleep(1000);
            }


            if (_connectStateMachine.ConnectionResult == ReturnCodes.Accepted) {
                ConnectionOptions = mqttConnectionOptions;

                _pingStateMachine.Reset();
                _connectStateMachine.Reset();

                // restore previous session
                RestoreSession();

                IsConnected = true;
            }

            return _connectStateMachine.ConnectionResult;
        }
    }
}
