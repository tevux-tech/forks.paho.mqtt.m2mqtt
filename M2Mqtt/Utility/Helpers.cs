﻿/*
Copyright (c) 2021 Simonas Greicius

All rights reserved. This program and the accompanying materials
are made available under the terms of the Eclipse Public License v1.0
and Eclipse Distribution License v1.0 which accompany this distribution. 

The Eclipse Public License is available at 
   http://www.eclipse.org/legal/epl-v10.html
and the Eclipse Distribution License is available at 
   http://www.eclipse.org/org/documents/edl-v10.php.

Contributors:
   Simonas Greicius - initial implementation and/or initial documentation
*/

namespace Tevux.Protocols.Mqtt {
    internal class Helpers {
        /// <summary>
        /// Returns current time in seconds (since 0000-01-01).
        /// </summary>
        public static double GetCurrentTime() {
            return System.DateTime.Now.Ticks / System.TimeSpan.TicksPerSecond;
        }
    }
}
