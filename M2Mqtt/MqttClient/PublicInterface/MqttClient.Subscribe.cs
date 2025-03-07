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
   Simonas Greicius - 2021 rework
*/

using System;

namespace Tevux.Protocols.Mqtt {
    public partial class MqttClient {
        public bool SubscribeAndWait(string topic, QosLevel qosLevel) {
            return InternalSubscribe(topic, qosLevel, true);
        }
        public void Subscribe(string topic, QosLevel qosLevel) {
            _ = InternalSubscribe(topic, qosLevel, false);
        }
        private bool InternalSubscribe(string topic, QosLevel qosLevel, bool waitForCompletion = false) {
            if (_isInitialized == false) { throw new InvalidOperationException("MqttClient has not been initialized. Call Initialize() method first."); }

            var subscribePacket = new SubscribePacket(topic, qosLevel);
            var isSubscribed = _subscriptionStateMachine.Subscribe(subscribePacket, waitForCompletion);

            return isSubscribed;
        }
    }
}
