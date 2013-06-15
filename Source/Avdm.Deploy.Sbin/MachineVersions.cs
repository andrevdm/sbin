using System;
using System.Collections.Generic;

namespace Avdm.Deploy.Sbin
{
    [Serializable]
    public class MachineVersions
    {
        public dynamic Id { get; set; }
        public string Key { get; set; }
        public List<MachineVersion> Value { get; set; }
    }
}