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
   Simonas Greicius - adaptation for state machine logic in 2021
*/

namespace Tevux.Protocols.Mqtt {
    /// <summary>
    /// QoS 2 is a two-stage process, so by the time PUBREL/PUBCOMP packets come into play, 
    /// original PUBLISH packet is long gone. But we need to preserve the original packet so 
    /// an appropriate event can be raised once entire exchange process is complete.
    /// </summary>
    internal class PublishTransmissionContext : TransmissionContext {
        public PublishTransmissionContext(PublishPacket originalPublishPacket, ControlPacketBase packetToSend, double timestamp) : base(packetToSend, timestamp) {
            OriginalPublishPacket = originalPublishPacket;
        }

        public PublishPacket OriginalPublishPacket { get; set; }

        public override ushort PacketId { get { return OriginalPublishPacket.PacketId; } }
    }

    /// <summary>
    /// This context is used to temporarily preserve states while packets are being exchanged between client and broker 
    /// (for example, for ACK packets or retry count).
    /// </summary>
    internal class TransmissionContext {
        public TransmissionContext(ControlPacketBase packetToSend, double timestamp) {
            PacketToSend = packetToSend;
            Timestamp = timestamp;
        }

        public int AttemptNumber { get; set; } = 1;
        public bool IsFinished { get; set; } = false;
        public bool IsSucceeded { get; set; } = false;
        public virtual ushort PacketId { get { return PacketToSend.PacketId; } }
        public ControlPacketBase PacketToSend { get; set; }
        public double Timestamp { get; set; }
    }
}
