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
using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    public partial class MqttClient {
        private void MasterTickThread() {
            while (true) {
                if (IsConnected) {
                    _pingStateMachine.Tick(_lastCommunicationTime);
                    if (_pingStateMachine.IsBrokerAlive) {
                        _subscriptionStateMachine.Tick();
                        _outgoingPublishStateMachine.Tick();
                        _incomingPublishStateMachine.Tick();
                    }
                    else {
                        Trace.WriteLine(TraceLevel.Error, "PING timeouted, beginning shutdown.");
                        CloseConnections();
                    }
                }
                else if (_isConnectionRequested) {
                    Trace.WriteLine(TraceLevel.Information, "Connecting...");
                    if (_channelConnectionOptions.IsTlsUsed) {
                        _channel = new SecureTcpChannel(_channelConnectionOptions);
                    }
                    else {
                        _channel = new UnsecureTcpChannel(_channelConnectionOptions);
                    }

                    var isOk = true;
                    if (_channel.TryConnect() == false) {
                        isOk = false;
                    };

                    if (isOk) {
                        _lastCommunicationTime = 0;
                        _isConnectionClosing = false;
                    }

                    if (isOk) {
                        var connectPacket = new ConnectPacket(ConnectionOptions);
                        _connectStateMachine.Connect(connectPacket);
                        while (_connectStateMachine.IsConnectionCompleted == false) {
                            _connectStateMachine.Tick();
                            Thread.Sleep(1000);
                        }
                    }

                    if (_connectStateMachine.IsConnectionSuccessful) {
                        if (ConnectionOptions.IsCleanSession) {
                            _pingStateMachine.Reset();
                            _connectStateMachine.Reset();
#warning need to reset publish state machine, too
                        }
                    }
                    else {
                        isOk = false;
                    }

                    IsConnected = isOk;
                    Thread.Sleep(100);
                }
                else {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
