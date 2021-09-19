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

namespace uPLibrary.Networking.M2Mqtt.Messages {
    /// <summary>
    /// Base class for all MQTT messages
    /// </summary>
    internal abstract class MqttMsgBase {
        public class MessageType {
            public const byte Connect = 0x01;
            public const byte ConAck = 0x02;
            public const byte Publish = 0x03;
            public const byte PubAck = 0x04;
            public const byte PubRec = 0x05;
            public const byte PubRel = 0x06;
            public const byte PubComp = 0x07;
            public const byte Subscribe = 0x08;
            public const byte SubAck = 0x09;
            public const byte Unsubscribe = 0x0A;
            public const byte UnsubAck = 0x0B;
            public const byte PingReq = 0x0C;
            public const byte PingResp = 0x0D;
            public const byte Disconnect = 0x0E;
        }

        public byte Type { get; set; }

        public ushort MessageId { get; set; }
    }
}
