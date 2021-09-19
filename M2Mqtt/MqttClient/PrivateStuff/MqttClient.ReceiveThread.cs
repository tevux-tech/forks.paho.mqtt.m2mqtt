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

namespace Tevux.Protocols.Mqtt {
    public partial class MqttClient {
        private void ReceiveThread() {
            var fixedHeaderFirstByte = new byte[1];
            byte msgType;

            while (true) {
                if (_channel.IsConnected) {
                    // read first byte (fixed header)
                    var isOk = _channel.TryReceive(fixedHeaderFirstByte);

                    if (isOk) {
                        // extract message type from received byte
                        msgType = (byte)(fixedHeaderFirstByte[0] >> 4);
                        var flags = (byte)(fixedHeaderFirstByte[0] & 0x0F);

                        // Section 2.2. explains which bit can or cannot be set in the first byte.
                        if (msgType == MqttMsgBase.MessageType.Publish) {
                            // PUBLISH is the only packet that actually uses any flags, and it uses all 4 of them, see section 3.3.
                            // Remaining length is variable header (variable number bytes) plus the length of the payload.
                            // Thus, need to decode remaining length field itself first.
                            isOk = MqttMsgBase.TryDecodeRemainingLength(_channel, out var remainingLength);

                            byte[] variableHeaderAndPayloadBytes = null;
                            if (isOk) {
                                variableHeaderAndPayloadBytes = new byte[remainingLength];
                                isOk = _channel.TryReceive(variableHeaderAndPayloadBytes);
                            }

                            MqttMsgPublish parsedMessage = null;
                            if (isOk) { isOk = MqttMsgPublish.TryParse(flags, variableHeaderAndPayloadBytes, out parsedMessage); }

                            if (isOk) { _incomingPublishStateMachine.ProcessMessage(parsedMessage); }
                        }
                        else if ((msgType == MqttMsgBase.MessageType.ConAck) && (flags == 0x00)) {
                            // Remaining length is always 2, see section 3.2.1.
                            // Thus, need to read 3 more bytes.
                            var lengthBytes = new byte[1];
                            _channel.TryReceive(lengthBytes);
                            var variableHeaderBytes = new byte[2];
                            _channel.TryReceive(variableHeaderBytes);

                            isOk = MqttMsgConnack.TryParse(variableHeaderBytes, out var parsedMessage);

                            _connectStateMachine.ProcessMessage(parsedMessage);
                        }
                        else if ((msgType == MqttMsgBase.MessageType.PingResp) && (flags == 0x00)) {
                            // Remaining length is always 0, see section 3.13.
                            // Thus, need to read 1 more byte, and discard it.
                            var lengthBytes = new byte[1];
                            _channel.TryReceive(lengthBytes);

                            isOk = MqttMsgPingResp.TryParse(out var parsedMessage);

                            _pingStateMachine.ProcessMessage(parsedMessage);
                        }
                        else if ((msgType == MqttMsgBase.MessageType.SubAck) && (flags == 0x00)) {
                            // Remaining length is variable header (2 bytes) plus the length of the payload, see section 3.9.
                            // Thus, need to decode remaining length field itself first.
                            isOk = MqttMsgBase.TryDecodeRemainingLength(_channel, out var remainingLength);

                            var variableHeaderBytes = new byte[2];
                            _channel.TryReceive(variableHeaderBytes);

                            var payloadBytes = new byte[remainingLength - 2];
                            _channel.TryReceive(payloadBytes);

                            // enqueue SUBACK message received (for QoS Level 1) into the internal queue
                            isOk = MqttMsgSuback.TryParse(variableHeaderBytes, payloadBytes, out var parsedMessage);

                            _subscribeStateMachine.ProcessMessage(parsedMessage);
                        }
                        else if ((msgType == MqttMsgBase.MessageType.PubAck) && (flags == 0x00)) {
                            // Remaining length is always 2, see section 3.4.
                            // Thus, need to read 3 more bytes.
                            var lengthBytes = new byte[1];
                            _channel.TryReceive(lengthBytes);

                            var variableHeaderBytes = new byte[2];
                            _channel.TryReceive(variableHeaderBytes);

                            isOk = MqttMsgPuback.TryParse(variableHeaderBytes, out var parsedMessage);

                            _outgoingPublishStateMachine.ProcessMessage(parsedMessage);
                        }
                        else if ((msgType == MqttMsgBase.MessageType.PubRec) && (flags == 0x00)) {
                            // Remaining length is always 2, see section 3.5.
                            // Thus, need to read 3 more bytes.
                            var lengthBytes = new byte[1];
                            _channel.TryReceive(lengthBytes);

                            var variableHeaderBytes = new byte[2];
                            _channel.TryReceive(variableHeaderBytes);

                            isOk = MqttMsgPubrec.TryParse(variableHeaderBytes, out var parsedMessage);

                            _outgoingPublishStateMachine.ProcessMessage(parsedMessage);
                        }
                        else if ((msgType == MqttMsgBase.MessageType.PubRel) && (flags == 0x02)) {
                            // Remaining length is always 2, see section 3.6.
                            // Thus, need to read 3 more bytes.
                            var lengthBytes = new byte[1];
                            _channel.TryReceive(lengthBytes);

                            var variableHeaderBytes = new byte[2];
                            _channel.TryReceive(variableHeaderBytes);

                            isOk = MqttMsgPubrel.TryParse(variableHeaderBytes, out var parsedMessage);

                            _incomingPublishStateMachine.ProcessMessage(parsedMessage);
                        }
                        else if ((msgType == MqttMsgBase.MessageType.PubComp) && (flags == 0x00)) {
                            // Remaining length is always 2, see section 3.7.
                            // Thus, need to read 3 more bytes.
                            var lengthBytes = new byte[1];
                            _channel.TryReceive(lengthBytes);

                            var variableHeaderBytes = new byte[2];
                            _channel.TryReceive(variableHeaderBytes);

                            isOk = MqttMsgPubcomp.TryParse(variableHeaderBytes, out var parsedMessage);

                            _outgoingPublishStateMachine.ProcessMessage(parsedMessage);
                        }
                        else if ((msgType == MqttMsgBase.MessageType.UnsubAck) && (flags == 0x00)) {
                            // Remaining length is always 2, see section 3.11.
                            // Thus, need to read 3 more bytes.
                            var lengthBytes = new byte[1];
                            _channel.TryReceive(lengthBytes);

                            var variableHeaderBytes = new byte[2];
                            _channel.TryReceive(variableHeaderBytes);

                            isOk = MqttMsgUnsuback.TryParse(variableHeaderBytes, out var parsedMessage);

                            _unsubscribeStateMachine.ProcessMessage(parsedMessage);
                        }
                        else {
                            // This is either a malformed message header, or it is meant for server, not client.
                            // Either way, it is a protocol violation, so network connection should be closed.
                            // Although specification is not entirely clear what to do if a client receives a messages which is meant for a server.
                            // But I will go with section 4.8 and close the connection anyway.

                            CloseConnections();
                        }
                    }
                    else {
                        // Cannot receive needed data. Something is wrong with the data channel.
                        CloseConnections();
                    }
                }
                else {
                    Thread.Sleep(100);
                }
            }
        }
    }
}
