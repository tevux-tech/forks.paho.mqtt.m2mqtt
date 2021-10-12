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

using System;
using System.Threading;

namespace Tevux.Protocols.Mqtt {
    public partial class MqttClient {
        public bool Connect() {
            return Connect(new ChannelConnectionOptions(), new MqttConnectionOptions());
        }

        public bool Connect(ChannelConnectionOptions channelConnectionOptions) {
            return Connect(channelConnectionOptions, new MqttConnectionOptions());
        }

        public bool Connect(ChannelConnectionOptions channelConnectionOptions, MqttConnectionOptions mqttConnectionOptions) {
            if (_isInitialized == false) { throw new InvalidOperationException("MqttClient has not been initialized. Call Initialize() method first."); }

            if (channelConnectionOptions.IsTlsUsed) {
                _channel = new SecureTcpChannel(channelConnectionOptions);
            }
            else {
                _channel = new UnsecureTcpChannel(channelConnectionOptions);
            }

            ConnectionOptions = mqttConnectionOptions;

            var isOk = true;
            if (_channel.TryConnect() == false) {
                isOk = false;
            };

            if (isOk) {
                LastCommTime = 0;
                _isConnectionClosing = false;
            }

            if (isOk) {
                var connectPacket = new ConnectPacket(mqttConnectionOptions);
                _connectStateMachine.Connect(connectPacket);
                while (_connectStateMachine.IsConnectionCompleted == false) {
                    _connectStateMachine.Tick();
                    Thread.Sleep(1000);
                }
            }

            if (_connectStateMachine.IsConnectionSuccessful) {
                if (mqttConnectionOptions.IsCleanSession) {
                    _pingStateMachine.Reset();
                    _connectStateMachine.Reset();
                }

            }
            else {
                isOk = false;
            }

            IsConnected = isOk;

            return isOk;
        }
    }
}
