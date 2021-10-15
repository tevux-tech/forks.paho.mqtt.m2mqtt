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
   Simonas Greicius - 2021 rework
*/

namespace Tevux.Protocols.Mqtt {
    public partial class MqttClient {
        private bool _isInitialized;
        private double _lastCommunicationTime;
        private IMqttNetworkChannel _channel = new DummyChannel();
        private ChannelConnectionOptions _channelConnectionOptions = new ChannelConnectionOptions();
        private bool _isConnectionRequested;
        private NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        private readonly PingStateMachine _pingStateMachine = new PingStateMachine();
        private readonly ConnectStateMachine _connectStateMachine = new ConnectStateMachine();
        private readonly SubscriptionStateMachine _subscriptionStateMachine = new SubscriptionStateMachine();
        private readonly OutgoingPublishStateMachine _outgoingPublishStateMachine = new OutgoingPublishStateMachine();
        private readonly IncomingPublishStateMachine _incomingPublishStateMachine = new IncomingPublishStateMachine();

        public MqttConnectionOptions ConnectionOptions { get; private set; }
        public bool IsConnected { get; private set; }
    }
}
