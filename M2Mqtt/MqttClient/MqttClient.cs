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
using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    /// <summary>
    /// MQTT Client
    /// </summary>
    public partial class MqttClient {
        /// <summary>
        /// Delegate that defines event handler for cliet/peer disconnection
        /// </summary>
        public delegate void ConnectionClosedEventHandler(object sender, EventArgs e);

        public double LastCommTime { get; private set; }

        public event PublishReceivedEventHandler PublishReceived = delegate { };
        public event PublishedEventHandler Published = delegate { };
        public event SubscribedEventHandler Subscribed = delegate { };
        public event UnsubscribedEventHandler Unsubscribed = delegate { };
        public event ConnectionClosedEventHandler ConnectionClosed = delegate { };

        // channel to communicate over the network
        private IMqttNetworkChannel _channel;

        // connection is closing due to peer
        private bool _isConnectionClosing;

        private readonly PingStateMachine _pingStateMachine = new PingStateMachine();
        private readonly ConnectStateMachine _connectStateMachine = new ConnectStateMachine();
        private readonly SubscriptionStateMachine _subscriptionStateMachine = new SubscriptionStateMachine();
        private readonly OutgoingPublishStateMachine _outgoingPublishStateMachine = new OutgoingPublishStateMachine();
        private readonly IncomingPublishStateMachine _incomingPublishStateMachine = new IncomingPublishStateMachine();

        readonly ConcurrentQueue _eventQueue = new ConcurrentQueue();

        private bool _isInitialized;

        public MqttConnectionOptions ConnectionOptions { get; private set; }
        public bool IsConnected { get; private set; }
    }
}
