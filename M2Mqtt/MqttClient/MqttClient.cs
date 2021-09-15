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

#warning Maybe remove LINQ usage?
using System;
using System.Collections;
using System.Threading;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Session;
using uPLibrary.Networking.M2Mqtt.Utility;

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
        // event for raising received message event
        private AutoResetEvent _receiveEventWaitHandle;

        // event for starting process inflight queue asynchronously
        private AutoResetEvent _inflightWaitHandle;

        // event for signaling synchronous receive
        AutoResetEvent _syncEndReceiving;
        // message received
        MqttMsgBase _msgReceived;

        // exeption thrown during receiving
        Exception _exReceiving;

        // keep alive period (in ms)
        private int _keepAlivePeriod;
        // events for signaling on keep alive thread
        private AutoResetEvent _keepAliveEvent;
        private AutoResetEvent _keepAliveEventEnd;
        // last communication time in ticks
        private int _lastCommTime;

        public event MqttMsgPublishEventHandler MqttMsgPublishReceived = delegate { };
        public event MqttMsgPublishedEventHandler MqttMsgPublished = delegate { };
        public event MqttMsgSubscribedEventHandler MqttMsgSubscribed = delegate { };
        public event MqttMsgUnsubscribedEventHandler MqttMsgUnsubscribed = delegate { };
        public event ConnectionClosedEventHandler ConnectionClosed = delegate { };

        // channel to communicate over the network
        private IMqttNetworkChannel _channel;

        // inflight messages queue
        private Queue _inflightQueue;
        // internal queue for received messages about inflight messages
        private Queue _internalQueue;
        // internal queue for dispatching events
        private ConcurrentQueue _eventQueue = new ConcurrentQueue();
        // session
        private MqttSession _session;

        // reference to avoid access to singleton via property
        private MqttSettings _settings;

        // current message identifier generated
        private ushort _messageIdCounter = 0;

        // connection is closing due to peer
        private bool _isConnectionClosing;


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
            // message flow into inflight queue
            internal MqttMsgFlow Flow { get; set; }

            internal MqttMsgContextFinder(ushort messageId, MqttMsgFlow flow) {
                MessageId = messageId;
                Flow = flow;
            }

            internal bool Find(object item) {
                var msgCtx = (MqttMsgContext)item;
                return ((msgCtx.Message.Type == MqttMsgBase.MessageType.Publish) &&
                        (msgCtx.Message.MessageId == MessageId) &&
                        msgCtx.Flow == Flow);

            }
        }
    }
}
