﻿using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReQueueClient
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class Data
    {
        public int Foo { get; set; }
    }
}
