﻿/*
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
using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    public partial class MqttClient {
        private readonly ConcurrentQueue _eventQueue = new ConcurrentQueue();

        public event PublishReceivedEventHandler PublishReceived = delegate { };
        public event PublishedEventHandler Published = delegate { };
        public event SubscribedEventHandler Subscribed = delegate { };
        public event UnsubscribedEventHandler Unsubscribed = delegate { };
        public event EventHandler Disconnected = delegate { };
        public event EventHandler Connected = delegate { };

        internal void OnPacketAcknowledged(ControlPacketBase sentPacket, ControlPacketBase receivedPacket) {
            // Creating a separate thread because those events are raised from state machines,
            // and I cannot let the end user to block them by attaching a long-running handlers.
            _eventQueue.Enqueue(new EventSource(sentPacket, receivedPacket));
        }
    }
}
