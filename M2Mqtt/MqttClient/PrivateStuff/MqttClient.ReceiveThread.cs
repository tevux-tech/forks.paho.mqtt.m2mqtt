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
using System.IO;
using System.Net.Sockets;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        /// <summary>
        /// Thread for receiving messages
        /// </summary>
        private void ReceiveThread() {
            var fixedHeaderFirstByte = new byte[1];
            byte msgType;

            while (_isRunning) {
                try {
                    // read first byte (fixed header)
                    var readBytes = _channel.Receive(fixedHeaderFirstByte);

                    if (readBytes > 0) {
                        // extract message type from received byte
                        msgType = (byte)((fixedHeaderFirstByte[0] & MqttMsgBase.FixedHeader.TypeMask) >> MqttMsgBase.FixedHeader.TypeOffset);

                        switch (msgType) {
                            case MqttMsgBase.MessageType.ConAck:
                                _msgReceived = MqttMsgConnack.Parse(fixedHeaderFirstByte[0], _channel);
                                Trace.WriteLine(TraceLevel.Frame, "RECV {0}", _msgReceived);
                                _syncEndReceiving.Set();
                                break;

                            case MqttMsgBase.MessageType.PingResp:
                                _msgReceived = MqttMsgPingResp.Parse(fixedHeaderFirstByte[0], _channel);
                                Trace.WriteLine(TraceLevel.Frame, "RECV {0}", _msgReceived);
                                _syncEndReceiving.Set();
                                break;

                            case MqttMsgBase.MessageType.SubAck:
                                // enqueue SUBACK message received (for QoS Level 1) into the internal queue
                                var suback = MqttMsgSuback.Parse(fixedHeaderFirstByte[0], _channel);
                                Trace.WriteLine(TraceLevel.Frame, "RECV {0}", suback);
                                EnqueueInternal(suback);
                                break;

                            case MqttMsgBase.MessageType.Publish:
                                var publish = MqttMsgPublish.Parse(fixedHeaderFirstByte[0], _channel);
                                Trace.WriteLine(TraceLevel.Frame, "RECV {0}", publish);
                                EnqueueInflight(publish, MqttMsgFlow.ToAcknowledge);
                                break;

                            case MqttMsgBase.MessageType.PubAck:
                                // enqueue PUBACK message received (for QoS Level 1) into the internal queue
                                var puback = MqttMsgPuback.Parse(fixedHeaderFirstByte[0], _channel);
                                Trace.WriteLine(TraceLevel.Frame, "RECV {0}", puback);
                                EnqueueInternal(puback);
                                break;

                            case MqttMsgBase.MessageType.PubRec:
                                // enqueue PUBREC message received (for QoS Level 2) into the internal queue
                                var pubrec = MqttMsgPubrec.Parse(fixedHeaderFirstByte[0], _channel);
                                Trace.WriteLine(TraceLevel.Frame, "RECV {0}", pubrec);
                                EnqueueInternal(pubrec);
                                break;

                            case MqttMsgBase.MessageType.PubRel:
                                // enqueue PUBREL message received (for QoS Level 2) into the internal queue
                                var pubrel = MqttMsgPubrel.Parse(fixedHeaderFirstByte[0], _channel);
                                Trace.WriteLine(TraceLevel.Frame, "RECV {0}", pubrel);
                                EnqueueInternal(pubrel);

                                break;

                            case MqttMsgBase.MessageType.PubComp:
                                // enqueue PUBCOMP message received (for QoS Level 2) into the internal queue
                                var pubcomp = MqttMsgPubcomp.Parse(fixedHeaderFirstByte[0], _channel);
                                Trace.WriteLine(TraceLevel.Frame, "RECV {0}", pubcomp);
                                EnqueueInternal(pubcomp);
                                break;

                            case MqttMsgBase.MessageType.UnsubAck:
                                // enqueue UNSUBACK message received (for QoS Level 1) into the internal queue
                                var unsuback = MqttMsgUnsuback.Parse(fixedHeaderFirstByte[0], _channel);
                                Trace.WriteLine(TraceLevel.Frame, "RECV {0}", unsuback);
                                EnqueueInternal(unsuback);
                                break;

                            case MqttMsgBase.MessageType.Connect:
                            case MqttMsgBase.MessageType.PingReq:
                            case MqttMsgBase.MessageType.Subscribe:
                            case MqttMsgBase.MessageType.Unsubscribe:
                            case MqttMsgBase.MessageType.Disconnect:
                            default:
                                // These message are meant for the broker, not client.
                                throw new MqttClientException(MqttClientErrorCode.WrongBrokerMessage);
                        }

                        _exReceiving = null;
                    }
                    // zero bytes read, peer gracefully closed socket
                    else {
                        // wake up thread that will notify connection is closing
                        OnConnectionClosing();
                    }
                }
                catch (Exception e) {

                    Trace.WriteLine(TraceLevel.Error, "Exception occurred: {0}", e.ToString());

                    _exReceiving = new MqttCommunicationException(e);

                    var close = false;
                    if (e.GetType() == typeof(MqttClientException)) {
                        // [v3.1.1] scenarios the receiver MUST close the network connection
                        var ex = e as MqttClientException;
                        close = ((ex.ErrorCode == MqttClientErrorCode.InvalidFlagBits) || (ex.ErrorCode == MqttClientErrorCode.InvalidProtocolName) || (ex.ErrorCode == MqttClientErrorCode.InvalidConnectFlags));
                    }
                    else if ((e.GetType() == typeof(IOException)) || (e.GetType() == typeof(SocketException)) || ((e.InnerException != null) && (e.InnerException.GetType() == typeof(SocketException)))) // added for SSL/TLS incoming connection that use SslStream that wraps SocketException
                    {
                        close = true;
                    }

                    if (close) {
                        // wake up thread that will notify connection is closing
                        OnConnectionClosing();
                    }
                }
            }
        }
    }


}
