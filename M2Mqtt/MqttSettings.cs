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

namespace uPLibrary.Networking.M2Mqtt {
    /// <summary>
    /// Settings class for the MQTT broker
    /// </summary>
    public class MqttSettings {
        // default port for MQTT protocol
        public const int BrokerDefaultPort = 1883;
        public const int BrokerDefaultSecurePort = 8883;
        // default timeout on receiving from client
        public const int DefaultTimeout = 30000;
        // max publish, subscribe and unsubscribe retry for QoS Level 1 or 2
        public const int AttemptsRetry = 3;
        // delay for retry publish, subscribe and unsubscribe for QoS Level 1 or 2
        public const int DelayRetry = 10000;
        // broker need to receive the first message (CONNECT)
        // within a reasonable amount of time after TCP/IP connection 
        public const int ConnectTimeout = 30000;
        // default inflight queue size
        public const int MaxInflightQueueSize = int.MaxValue;

        public int UnsecurePort { get; internal set; }
        public int SecurePort { get; internal set; }
        /// <summary>
        /// Timeout on client connection (before receiving CONNECT message)
        /// </summary>
        public int TimeoutOnConnection { get; internal set; }
        public int TimeoutOnReceiving { get; internal set; }
        public int AttemptsOnRetry { get; internal set; }
        public int DelayOnRetry { get; internal set; }
        public int InflightQueueSize { get; set; }

        /// <summary>
        /// Singleton instance of settings
        /// </summary>
        public static MqttSettings Instance { get; } = new MqttSettings();

        private MqttSettings() {
            UnsecurePort = BrokerDefaultPort;
            SecurePort = BrokerDefaultSecurePort;
            TimeoutOnReceiving = DefaultTimeout;
            AttemptsOnRetry = AttemptsRetry;
            DelayOnRetry = DelayRetry;
            TimeoutOnConnection = ConnectTimeout;
            InflightQueueSize = MaxInflightQueueSize;
        }
    }
}
