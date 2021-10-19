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
   Simonas Greicius - 2021 rework
*/

using System.Threading;

namespace Tevux.Protocols.Mqtt {
    public partial class MqttClient {
        private void ReceiveThread() {
            var fixedHeaderFirstByte = new byte[1];
            byte packetType = 0;

            while (true) {
                if (_channel.IsConnected && (_isDisconnectionRequested == false)) {
                    // read first byte (fixed header)
                    var isOk = _channel.TryReceive(fixedHeaderFirstByte);

                    if (isOk) {
                        // extract packet type from received byte
                        packetType = (byte)(fixedHeaderFirstByte[0] >> 4);
                        var flags = (byte)(fixedHeaderFirstByte[0] & 0x0F);

                        // Section 2.2. explains which bit can or cannot be set in the first byte.
                        if (packetType == ControlPacketBase.PacketTypes.Publish) {
                            // PUBLISH is the only packet that actually uses any flags, and it uses all 4 of them, see section 3.3.
                            // Remaining length is variable header (variable number bytes) plus the length of the payload.
                            // Thus, need to decode remaining length field itself first.
                            isOk = ControlPacketBase.TryDecodeRemainingLength(_channel, out var remainingLength);

                            byte[] variableHeaderAndPayloadBytes = null;
                            if (isOk) {
                                variableHeaderAndPayloadBytes = new byte[remainingLength];
                                isOk = _channel.TryReceive(variableHeaderAndPayloadBytes);
                            }

                            PublishPacket parsedPacket = null;
                            if (isOk) { isOk = PublishPacket.TryParse(flags, variableHeaderAndPayloadBytes, out parsedPacket); }
                            if (isOk) { _incomingPublishStateMachine.ProcessPacket(parsedPacket); }
                        }
                        else if ((packetType == ControlPacketBase.PacketTypes.Conack) && (flags == 0x00)) {
                            // Remaining length is always 2, see section 3.2.1.
                            // Thus, need to read 3 more bytes.
                            var lengthBytes = new byte[1];
                            var variableHeaderBytes = new byte[2];
                            ConnackPacket connackPacket = null;

                            isOk = _channel.TryReceive(lengthBytes);

                            if (isOk) { isOk = _channel.TryReceive(variableHeaderBytes); }
                            if (isOk) { isOk = ConnackPacket.TryParse(variableHeaderBytes, out connackPacket); }
                            if (isOk) { _connectStateMachine.ProcessPacket(connackPacket); }
                        }
                        else if ((packetType == ControlPacketBase.PacketTypes.Pingresp) && (flags == 0x00)) {
                            // Remaining length is always 0, see section 3.13.
                            // Thus, need to read 1 more byte, and discard it.
                            var lengthBytes = new byte[1];
                            PingrespPacket pingrespPacket = null;
                            isOk = _channel.TryReceive(lengthBytes);

                            if (isOk) { isOk = PingrespPacket.TryParse(out pingrespPacket); }
                            if (isOk) { _pingStateMachine.ProcessPacket(pingrespPacket); }
                        }
                        else if ((packetType == ControlPacketBase.PacketTypes.Suback) && (flags == 0x00)) {
                            // Remaining length is variable header (2 bytes) plus the length of the payload, see section 3.9.
                            // Thus, need to decode remaining length field itself first.
                            isOk = ControlPacketBase.TryDecodeRemainingLength(_channel, out var remainingLength);

                            var variableHeaderBytes = new byte[2];
                            byte[] payloadBytes = null;
                            SubackPacket subackPacket = null;

                            if (isOk) { isOk = _channel.TryReceive(variableHeaderBytes); }
                            if (isOk) {
                                payloadBytes = new byte[remainingLength - 2];
                                isOk = _channel.TryReceive(payloadBytes);
                            }
                            if (isOk) { isOk = SubackPacket.TryParse(variableHeaderBytes, payloadBytes, out subackPacket); }
                            if (isOk) { _subscriptionStateMachine.ProcessPacket(subackPacket); }
                        }
                        else if ((packetType == ControlPacketBase.PacketTypes.Puback) && (flags == 0x00)) {
                            // Remaining length is always 2, see section 3.4.
                            // Thus, need to read 3 more bytes.
                            var lengthBytes = new byte[1];
                            var variableHeaderBytes = new byte[2];
                            PubackPacket pubackPacket = null;

                            isOk = _channel.TryReceive(lengthBytes);
                            if (isOk) { isOk = _channel.TryReceive(variableHeaderBytes); }
                            if (isOk) { isOk = PubackPacket.TryParse(variableHeaderBytes, out pubackPacket); }
                            if (isOk) { _outgoingPublishStateMachine.ProcessPacket(pubackPacket); }
                        }
                        else if ((packetType == ControlPacketBase.PacketTypes.Pubrec) && (flags == 0x00)) {
                            // Remaining length is always 2, see section 3.5.
                            // Thus, need to read 3 more bytes.
                            var lengthBytes = new byte[1];
                            var variableHeaderBytes = new byte[2];
                            PubrecPacket pubrecPacket = null;

                            isOk = _channel.TryReceive(lengthBytes);
                            if (isOk) { isOk = _channel.TryReceive(variableHeaderBytes); }
                            if (isOk) { isOk = PubrecPacket.TryParse(variableHeaderBytes, out pubrecPacket); }
                            if (isOk) { _outgoingPublishStateMachine.ProcessPacket(pubrecPacket); }
                        }
                        else if ((packetType == ControlPacketBase.PacketTypes.Pubrel) && (flags == 0x02)) {
                            // Remaining length is always 2, see section 3.6.
                            // Thus, need to read 3 more bytes.
                            var lengthBytes = new byte[1];
                            var variableHeaderBytes = new byte[2];
                            PubrelPacket pubrelPacket = null;

                            isOk = _channel.TryReceive(lengthBytes);
                            if (isOk) { isOk = _channel.TryReceive(variableHeaderBytes); }
                            if (isOk) { isOk = PubrelPacket.TryParse(variableHeaderBytes, out pubrelPacket); }
                            if (isOk) { _incomingPublishStateMachine.ProcessPacket(pubrelPacket); }
                        }
                        else if ((packetType == ControlPacketBase.PacketTypes.Pubcomp) && (flags == 0x00)) {
                            // Remaining length is always 2, see section 3.7.
                            // Thus, need to read 3 more bytes.
                            var lengthBytes = new byte[1];
                            var variableHeaderBytes = new byte[2];
                            PubcompPacket pubcompPacket = null;

                            isOk = _channel.TryReceive(lengthBytes);
                            if (isOk) { isOk = _channel.TryReceive(variableHeaderBytes); }
                            if (isOk) { isOk = PubcompPacket.TryParse(variableHeaderBytes, out pubcompPacket); }
                            if (isOk) { _outgoingPublishStateMachine.ProcessPacket(pubcompPacket); }
                        }
                        else if ((packetType == ControlPacketBase.PacketTypes.Unsuback) && (flags == 0x00)) {
                            // Remaining length is always 2, see section 3.11.
                            // Thus, need to read 3 more bytes.
                            var lengthBytes = new byte[1];
                            var variableHeaderBytes = new byte[2];
                            UnsubackPacket unsubackPacket = null;

                            isOk = _channel.TryReceive(lengthBytes);
                            if (isOk) { isOk = _channel.TryReceive(variableHeaderBytes); }
                            if (isOk) { isOk = UnsubackPacket.TryParse(variableHeaderBytes, out unsubackPacket); }
                            if (isOk) { _subscriptionStateMachine.ProcessPacket(unsubackPacket); }
                        }
                        else {
                            // This is either a malformed packet header, or it is meant for server, not client.
                            // Either way, it is a protocol violation, so network connection should be closed.
                            // Although specification is not entirely clear what to do if a client receives a packet which is meant for a server.
                            // But I will go with section 4.8 and close the connection anyway.
                            _log.Error($"Malformed packet received. Beginning shutdown.");
                            CloseConnections();
                        }
                    }
                    else {
                        if (_isDisconnectionRequested) {
                            // This is be a transdient state, when Socket.TryRead() is unblocked because the socket is being closed at the server side after receiving DISCONNECT packet from us..
                            // It then reads 0 bytes. This not an error in such case.
                        }
                        else {
                            _log.Error($"Cannot receive needed data for packet type {packetType}. Something is wrong with the data channel.");
                            CloseConnections();
                        }
                    }
                }
                else {
                    Thread.Sleep(100);
                }
            }
        }
    }
}
