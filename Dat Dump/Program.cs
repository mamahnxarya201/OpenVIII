﻿namespace OpenVIII.Dat_Dump
{

    internal class Program
    {
        
        #region Methods

        private static void Main(string[] args)
        {
            Memory.Init(null, null, null);
            DumpEncounterInfo.Process();
            DumpMonsterAndCharacterDat.Process();

        }


        #endregion Methods
    }
}