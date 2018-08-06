

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;

namespace OxygenControl
{
    public sealed class Program : MyGridProgram
    {

        /////////////////////////////////////////////////////////
        ///////////// Oxygen production Control /////////////////
        /////////////////// BY KRYPTUR ////////////////////////
        /////////////////////////////////////////////////////////

        // VERSION: 1.3.0
        // UPDATED TO REMOVE OBSOLETE BLOCKS

        // Start Production when below this value
        const float minLevel = 0.70f;

        // Stop Production when this value is reached
        const float maxLevel = 0.75f;

        // The blocks in this Group will be checked for oxygen level
        const string tankGroup = "oxyTanks";

        // These generators will be controlled
        // This can be OxygenGenerator or OxygenFarm
        const string generatorGroup = "oxyGenerators";

        // These AirVents will be used to depressurize the planet
        // to get oxygen.
        const string ventGroup = "oxyVents";

        // Text Panel Group to display oxygen status
        const string panelGroup = "OxygenLCD";

        ////////////////////////////////////////////////////////////////
        ///////////// DON'T CHANGE ANYTHING BELOW //////////////// 
        ////////////////////////////////////////////////////////////////


        // Enum: State
        const int none = 0;
        const int on = 1;
        const int off = 2;

        int state = none;

        Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        void updateLcd(float level, int num, int num2, int num3)
        {
            List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(groups);
            IMyTextPanel lcd;

            for (int i = 0; i < groups.Count; i++)
            {
                IMyBlockGroup group = groups[i];
                if (group.Name == panelGroup)
                {
                    List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>();
                    group.GetBlocks(groupBlocks);
                    for (int j = 0; j < groupBlocks.Count; j++)
                    {
                        lcd = groupBlocks[j] as IMyTextPanel;
                        lcd.SetValue("FontSize", 1.7f);
                        lcd.ShowPublicTextOnScreen();
                        lcd.WritePublicText("Oxygen Production:\n");
                        if (state == on)
                            lcd.WritePublicText("  Activated", true);
                        else
                            lcd.WritePublicText("  Deactivated", true);

                        string minPerc = (float)((int)(minLevel * 10000)) / 100 + "%";
                        string maxPerc = (float)((int)(maxLevel * 10000)) / 100 + "%";
                        lcd.WritePublicText("  -  [" + minPerc + "~" + maxPerc + "]\n", true);


                        lcd.WritePublicText("OxygenTanks Level:\n", true);
                        lcd.WritePublicText("  " + (float)((int)(level * 10000)) / 100 + "%\n", true);
                        lcd.WritePublicText("OxygenTanks Count:\n", true);
                        lcd.WritePublicText("  " + num + "\n", true);
                        lcd.WritePublicText("OxygenGenerator Count:\n", true);
                        lcd.WritePublicText("  " + num2 + "\n", true);
                        lcd.WritePublicText("AirVent Count:\n", true);
                        lcd.WritePublicText("  " + num3, true);
                    }
                }
            }
        }

        void Main(string argument)
        {
            List<IMyBlockGroup> groups = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(groups);

            int num = 0;
            int num2 = 0;
            int num3 = 0;
            float sum = 0;
            float newLevel = 0;

            for (int i = 0; i < groups.Count; i++)
            {
                IMyBlockGroup group = groups[i];
                if (group.Name == tankGroup)
                {
                    List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>();
                    group.GetBlocks(groupBlocks);
                    for (int j = 0; j < groupBlocks.Count; j++)
                    {
                        IMyGasTank tank = groupBlocks[j] as IMyGasTank;
                        num++;
                        sum += (float)tank.FilledRatio;
                    }
                }
            }

            if (num == 0)
                newLevel = 0;
            else
                newLevel = sum / num;

            if (state == none || (state == on && newLevel >= maxLevel) || (state == off && newLevel <= minLevel))
            {
                if (newLevel >= maxLevel)
                {
                    state = off;
                }
                else
                {
                    state = on;
                }
            }

            for (int i = 0; i < groups.Count; i++)
            {
                IMyBlockGroup group = groups[i];
                if (group.Name == generatorGroup)
                {
                    List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>();
                    group.GetBlocks(groupBlocks);
                    for (int j = 0; j < groupBlocks.Count; j++)
                    {
                        IMyFunctionalBlock gen = groupBlocks[j] as IMyFunctionalBlock;
                        gen.Enabled = (state == on);
                        num2++;
                    }
                }
            }
            for (int i = 0; i < groups.Count; i++)
            {
                IMyBlockGroup group = groups[i];
                if (group.Name == ventGroup)
                {
                    List<IMyTerminalBlock> groupBlocks = new List<IMyTerminalBlock>();
                    group.GetBlocks(groupBlocks);
                    for (int j = 0; j < groupBlocks.Count; j++)
                    {
                        IMyAirVent vent = groupBlocks[j] as IMyAirVent;
                        vent.ApplyAction("Depressurize_On");
                        vent.Enabled = (state == on);
                        num3++;
                    }
                }
            }

            updateLcd(newLevel, num, num2, num3);
        }
    }

}
