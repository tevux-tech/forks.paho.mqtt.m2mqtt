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

#warning Maybe remove LINQ usage?
using System;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace uPLibrary.Networking.M2Mqtt {
    /// <summary>
    /// MQTT Client
    /// </summary>
    public partial class MqttClient {
        /// <summary>
        /// Delagate that defines event handler for PUBLISH message received
        /// </summary>
        public delegate void MqttMsgPublishEventHandler(object sender, MqttMsgPublishEventArgs e);

        /// <summary>
        /// Delegate that defines event handler for published message
        /// </summary>
        public delegate void MqttMsgPublishedEventHandler(object sender, MqttMsgPublishedEventArgs e);

        /// <summary>
        /// Delagate that defines event handler for subscribed topic
        /// </summary>
        public delegate void MqttMsgSubscribedEventHandler(object sender, MqttMsgSubscribedEventArgs e);

        /// <summary>
        /// Delagate that defines event handler for unsubscribed topic
        /// </summary>
        public delegate void MqttMsgUnsubscribedEventHandler(object sender, MqttMsgUnsubscribedEventArgs e);

        /// <summary>
        /// Delegate that defines event handler for cliet/peer disconnection
        /// </summary>
        public delegate void ConnectionClosedEventHandler(object sender, EventArgs e);

        // broker hostname (or ip address) and port
        private string _brokerHostName;
        private int _brokerPort;

        // running status of threads
        private bool _isRunning;


        // keep alive period (in ms)
        private int _keepAlivePeriod;
        // last communication time in ticks
        public int LastCommTime { get; private set; }

        public event MqttMsgPublishEventHandler MqttMsgPublishReceived = delegate { };
        public event MqttMsgPublishedEventHandler MqttMsgPublished = delegate { };
        public event MqttMsgSubscribedEventHandler MqttMsgSubscribed = delegate { };
        public event MqttMsgUnsubscribedEventHandler MqttMsgUnsubscribed = delegate { };
        public event ConnectionClosedEventHandler ConnectionClosed = delegate { };

        // channel to communicate over the network
        private IMqttNetworkChannel _channel;

        // reference to avoid access to singleton via property
        private MqttSettings _settings;

        // current message identifier generated
        private static ushort _messageIdCounter = 0;

        // connection is closing due to peer
        private bool _isConnectionClosing;

        private PingStateMachine _pingStateMachine = new PingStateMachine();
        private ConnectStateMachine _connectStateMachine = new ConnectStateMachine();
        private UnsubscribeStateMachine _unsubscribeStateMachine = new UnsubscribeStateMachine();
        private SubscribeStateMachine _subscribeStateMachine = new SubscribeStateMachine();
        private OutgoingPublishStateMachine _outgoingPublishStateMachine = new OutgoingPublishStateMachine();
        private IncomingPublishStateMachine _incomingPublishStateMachine = new IncomingPublishStateMachine();

        public bool IsConnected { get; private set; }
        public string ClientId { get; private set; }
        public bool CleanSession { get; private set; }
        public bool WillFlag { get; private set; }
        public QosLevel WillQosLevel { get; private set; }
        public string WillTopic { get; private set; }
        public string WillMessage { get; private set; }
        public MqttSettings Settings {
            get { return _settings; }
        }


        /// <summary>
        /// Finder class for PUBLISH message inside a queue
        /// </summary>
        internal class MqttMsgContextFinder {
            // PUBLISH message id
            internal ushort MessageId { get; set; }

            internal MqttMsgContextFinder(ushort messageId) {
                MessageId = messageId;

            }

            internal bool Find(object item) {
                var msgCtx = (MqttMsgContext)item;
                return (msgCtx.Message.Type == MqttMsgBase.MessageType.Publish) && (msgCtx.Message.MessageId == MessageId);
            }
        }
    }
}
