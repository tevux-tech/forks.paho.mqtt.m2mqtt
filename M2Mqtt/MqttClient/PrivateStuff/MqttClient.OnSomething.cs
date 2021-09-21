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
        /// <summary>
        /// Wrapper method for raising PUBLISH message received event
        /// </summary>
        /// <param name="publish">PUBLISH message received</param>
        internal void OnMqttMsgPublishReceived(PublishPacket publish) {

        }

        /// <summary>
        /// Wrapper method for raising published message event
        /// </summary>
        /// <param name="packetId">Message identifier for published message</param>
        /// <param name="isPublished">Publish flag</param>
        internal void OnMqttMsgPublished(ushort packetId, bool isPublished) {
            // Published?.Invoke(this, new PublishFinishedEventArgs(receivedPacket.PacketId, true));
        }

        internal void OnPacketAcknowledged(ControlPacketBase sentPacket, ControlPacketBase receivedPacket) {
            // Creating a separate thread because those events are raised from state machines,
            // and I cannot let the end user to block them by attaching a long-running handlers.
#warning Probably want to use a single persistent thread?..
            new Thread(() => {
                if ((sentPacket is SubscribePacket subscribePacket) && (receivedPacket is SubackPacket subackPacket)) {
                    Subscribed?.Invoke(this, new SubscribedEventArgs(subscribePacket.Topic, subackPacket.GrantedQosLevel));
                }
                else if ((sentPacket is UnsubscribePacket unsubscribePacket) && (receivedPacket is UnsubackPacket unsubackPacket)) {
                    Unsubscribed?.Invoke(this, new UnsubscribedEventArgs(unsubscribePacket.Topic));
                }
                else if (receivedPacket is PublishPacket publishReceivedPacket) {
                    PublishReceived?.Invoke(this, new PublishReceivedEventArgs(publishReceivedPacket.Topic, publishReceivedPacket.Message));
                }
            }).Start();
        }

        /// <summary>
        /// Wrapper method for peer/client disconnection
        /// </summary>
        private void OnConnectionClosed() {
            ConnectionClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
