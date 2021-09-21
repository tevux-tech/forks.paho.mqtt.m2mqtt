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

namespace Tevux.Protocols.Mqtt {
    internal class TransmissionContext {
        public ControlPacketBase PacketToSend { get; set; }

        /// <summary>
        /// Timestamp in ticks (for retry)
        /// </summary>
        public double Timestamp { get; set; }

        /// <summary>
        /// Attempt (for retry)
        /// </summary>
        public int AttemptNumber { get; set; } = 1;

        public bool IsFinished { get; set; } = false;
        public bool IsSucceeded { get; set; } = false;
        public virtual ushort PacketId { get { return PacketToSend.PacketId; } }

        public TransmissionContext(ControlPacketBase packetToSend, double timestamp) {
            PacketToSend = packetToSend;
            Timestamp = timestamp;
        }
    }

    internal class PublishTransmissionContext : TransmissionContext {
        public PublishPacket OriginalPublishPacket { get; set; }

        public override ushort PacketId { get { return OriginalPublishPacket.PacketId; } }

        public PublishTransmissionContext(PublishPacket originalPublishPacket, ControlPacketBase packetToSend, double timestamp) : base(packetToSend, timestamp) {
            OriginalPublishPacket = originalPublishPacket;
        }
    }
}
