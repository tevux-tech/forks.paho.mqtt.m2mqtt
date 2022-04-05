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
   Simonas Greicius - creation of state machine classes
*/

using System.Threading;

namespace Tevux.Protocols.Mqtt {
    public partial class MqttClient {
        private void MasterTickThread() {
            while (true) {
                if (_isDisconnectionRequested) {
                    if (_isWaitingForSocketToClose) {
                        if (_isReceiveThreadActive == false) {
                            _isWaitingForSocketToClose = false;
                            _isDisconnectionRequested = false;
                            if (_isDisconnectedByUser) {
                                _log.Info("Disconnected from {Server} as per user request.", _channelConnectionOptions.Hostname);
                                _isDisconnectedByUser = false;
                            }
                            else {
                                _log.Info("Disconnected from {Server} due to some network problems.", _channelConnectionOptions.Hostname);
                                if (_channelConnectionOptions.IsReconnectionEnabled) {
                                    _log.Info("Auto-reconnection is enabled.");
                                    _isConnectionRequested = true;
                                }
                            }
                            IsConnected = false;
                        }
                        else {
                            // TODO: Technically, it is possible for the state to stuck in this place, if the server will not close the connection.
                        }
                    }
                    else {
                        if (_isDisconnectedByUser) {
                            _log.Info($"Shutting connections down due to user request.");
                            var disconnect = new DisconnectPacket();
                            Send(disconnect);
                        }
                        else {
                            _log.Info($"Shutting connections down due to external sources.");
                            _channel.Close();
                        }

                        _isWaitingForSocketToClose = true;
                    }

                    // This is a dummy packet, so it just fits my the event queue.
                    _eventQueue.Enqueue(new EventSource(new DisconnectPacket(), null));
                }
                else if (IsConnected) {
                    _pingStateMachine.Tick(_lastCommunicationTime);
                    if (_pingStateMachine.IsBrokerAlive) {
                        _subscriptionStateMachine.Tick();
                        _outgoingPublishStateMachine.Tick();
                        _incomingPublishStateMachine.Tick();

                        Thread.Sleep(1000);
                    }
                    else {
                        _log.Error($"PINGREQ timeouted.");
                        CloseConnections();
                    }
                }
                else if (_isConnectionRequested) {
                    _log.Info($"Connection has been requested, so going for it.");
                    if (_channelConnectionOptions.IsTlsUsed) {
                        _channel = new SecureTcpChannel(_channelConnectionOptions);
                    }
                    else {
                        _channel = new UnsecureTcpChannel(_channelConnectionOptions);
                    }

                    var isOk = true;
                    if (_channel.TryConnect() == false) {
                        isOk = false;
                        _log.Error($"Cannot connect to channel {_channel.GetType()}.");
                    };

                    if (isOk) {
                        _lastCommunicationTime = 0;
                        _isDisconnectionRequested = false;
                    }

                    if (isOk) {
                        var connectPacket = new ConnectPacket(ConnectionOptions);
                        _connectStateMachine.Connect(connectPacket);
                        _log.Info($"Connecting to {_channelConnectionOptions}...");
                        Thread.Sleep(1000);
                        while (_connectStateMachine.IsConnectionCompleted == false) {
                            _log.Info($"Still connecting to {_channelConnectionOptions}...");
                            _connectStateMachine.Tick();
                            Thread.Sleep(1000);
                        }
                    }

                    if (_connectStateMachine.IsConnectionSuccessful) {
                        if (ConnectionOptions.IsCleanSession) {
                            _pingStateMachine.Reset();
                            _connectStateMachine.Reset();
                            _subscriptionStateMachine.Reset();
                            _incomingPublishStateMachine.Reset();
                            _outgoingPublishStateMachine.Reset();
                        }

                        // These are dummy packets, so it just fits my the event queue.
                        _eventQueue.Enqueue(new EventSource(new ConnackPacket(), new ConnackPacket()));

                        _log.Info($"Connected to {_channelConnectionOptions}.");
                    }
                    else {
                        isOk = false;
                        _log.Error($"could not connect to {_channelConnectionOptions}. If reconnection is enabled, will try again soon.");
                    }

                    IsConnected = isOk;
                    _isConnectionRequested = false;
                    Thread.Sleep(100);
                }
                else {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
