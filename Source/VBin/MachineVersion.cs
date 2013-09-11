using System;
using System.Text.RegularExpressions;

namespace VBin
{
    [Serializable]
    public class MachineVersion
    {
        private Regex m_machineRegex;

        public Regex MachineRegex 
        {
            get 
            {
                if( m_machineRegex == null )
                {
                    m_machineRegex = new Regex( Machine, RegexOptions.IgnoreCase );
                }

                return m_machineRegex;
            }
        }

        public string Machine { get; set; }
        public int Version { get; set; }
    }
}