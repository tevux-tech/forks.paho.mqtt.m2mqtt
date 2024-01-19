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

using System.Threading;

namespace Tevux.Protocols.Mqtt {
    public partial class MqttClient {
        public void Initialize() {
            if (_isInitialized) { return; }

            _pingStateMachine.Initialize(this);
            _connectStateMachine.Initialize(this);
            _subscriptionStateMachine.Initialize(this);
            _outgoingPublishStateMachine.Initialize(this);
            _incomingPublishStateMachine.Initialize(this);

            new Thread(MasterTickThread) { Name = "MasterTickThread" }.Start();
            new Thread(ReceiveThread) { Name = "ReceiveTickThread" }.Start();
            new Thread(ProcessEventQueueThread) { Name = "ProcessEventQueueThread" }.Start();

            _isInitialized = true;
        }
    }
}
