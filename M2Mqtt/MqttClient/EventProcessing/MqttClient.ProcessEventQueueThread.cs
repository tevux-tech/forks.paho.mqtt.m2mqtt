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

using System;
using System.Threading;

namespace Tevux.Protocols.Mqtt {
    public partial class MqttClient {
        private void ProcessEventQueueThread() {
            while (true) {
                while (_eventQueue.TryDequeue(out var dequeuedEvent)) {
                    var eventSource = (EventSource)dequeuedEvent;

                    if ((eventSource.SentPacket is SubscribePacket subscribePacket) && (eventSource.ReceivedPacket is SubackPacket subackPacket)) {
                        Subscribed?.Invoke(this, new SubscribedEventArgs(subscribePacket.Topic, subackPacket.GrantedQosLevel));
                    }
                    else if ((eventSource.SentPacket is UnsubscribePacket unsubscribePacket) && (eventSource.ReceivedPacket is UnsubackPacket unsubackPacket)) {
                        Unsubscribed?.Invoke(this, new UnsubscribedEventArgs(unsubscribePacket.Topic));
                    }
                    else if (eventSource.ReceivedPacket is PublishPacket publishReceivedPacket) {
                        PublishReceived?.Invoke(this, new PublishReceivedEventArgs(publishReceivedPacket.Topic, publishReceivedPacket.Message));
                    }
                    else if (eventSource.SentPacket is PublishPacket publishedPacket) {
                        Published?.Invoke(this, new PublishFinishedEventArgs(publishedPacket.Topic));
                    }
                    else if ((eventSource.SentPacket is ConnackPacket connectPacket) && (eventSource.ReceivedPacket is ConnackPacket connackPacket)) {
                        Connected?.Invoke(this, EventArgs.Empty);
                    }
                    else if (eventSource.SentPacket is DisconnectPacket) {
                        Disconnected?.Invoke(this, EventArgs.Empty);
                    }
                }

                Thread.Sleep(1);
            }
        }
    }
}
