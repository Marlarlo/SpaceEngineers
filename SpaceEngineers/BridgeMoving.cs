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

namespace SpaceEngineers
{
    public sealed class Program : MyGridProgram
    {
        //=======================================================================
        //////////////////////////BEGIN//////////////////////////////////////////
        //=======================================================================

        //SOUND NOT PLAYING BECAUSE I THINK THE SOUND FILE ISN'T KEPT BETWEEN LOADS
        //Script works without argument??? If used before?
        //Timer block needs to be switched back on without starting up again...somehow...
        List<IMyTerminalBlock> doors = new List<IMyTerminalBlock>(0);
        List<IMyTerminalBlock> dpanels = new List<IMyTerminalBlock>(0);
        List<IMyTerminalBlock> speakers = new List<IMyTerminalBlock>(0);
        List<IMyTerminalBlock> lights = new List<IMyTerminalBlock>(0);
        List<IMyTerminalBlock> TargetBlocks = new List<IMyTerminalBlock>(0);
        List<IMyTerminalBlock> pistons = new List<IMyTerminalBlock>(0);
        List<IMyTerminalBlock> tpanels = new List<IMyTerminalBlock>(0);
        IMyTimerBlock timer;
        bool inProgress = false;

        public void Main(string argument)
        {
            if (inProgress)
            {
                WriteToPanels("Piston Status report...", tpanels, false);
                inProgress = false; //Set it to false so the script can set it to true if any pistons are in motion
                for (int i = 0; i < pistons.Count; i++)
                {
                    IMyPistonBase tempPist = pistons[i] as IMyPistonBase;
                    PistonStatus pistStat = tempPist.Status;
                    string strPiston = pistStat.ToString();
                    WriteToPanels(strPiston, tpanels);
                    if (String.Equals("Extending", strPiston) || String.Equals("Retracting", strPiston))
                    {
                        inProgress = true;
                    }
                }

                if(inProgress)
                    {
                    timer.ApplyAction("Start");
                    return;
                    }
                else
                {
                    //Stop timer
                    timer.ApplyAction("Stop");

                    //Close doors
                    for (int i = 0; i < doors.Count; i++)
                    {
                        doors[i].ApplyAction("Open_On");
                    }

                    //Stop speakers
                    for (int i = 0; i < speakers.Count; i++)
                    {
                        speakers[i].ApplyAction("StopSound");
                    }

                    //Return lights to green
                    for (int i = 0; i < lights.Count; i++)
                    {
                        lights[i].SetValue("Color", VRageMath.Color.Green);
                        lights[i].SetValue("Blink Interval", (Single)0);
                        lights[i].SetValue("Blink Lenght", (Single)100);
                    }
                    WriteToPanels("Movement finished. Program stopping.", dpanels);

                    //Clear values inside all the class variables and reset inProgress to false
                    doors.Clear();
                    dpanels.Clear();
                    speakers.Clear();
                    lights.Clear();
                    TargetBlocks.Clear();
                    pistons.Clear();
                    tpanels.Clear();
                    timer = null;
                    inProgress = false;
                    return;
                }
             }

            inProgress = true;
            //Getting the blocks from the string input in the argument
                try
                {
                    GridTerminalSystem.GetBlockGroupWithName(argument).GetBlocks(TargetBlocks);
                }
                catch (NullReferenceException)
                {
                    return;
                }
                
            if (TargetBlocks.Count == 0)
            {
                return;
            }
            //Iterating through and sorting the terminal blocks into their lists (doors, lights etc)
            
            for (int i = 0; i < TargetBlocks.Count; i++) 
            {
                string BlockName = TargetBlocks[i].CustomName;

                if (BlockName.Contains("Door"))
                {
                    doors.Add(TargetBlocks[i]);
                }

                if (BlockName.Contains("Debug Panel"))
                {
                    dpanels.Add(TargetBlocks[i]);
                }

                if (BlockName.Contains("Sound"))
                {
                    speakers.Add(TargetBlocks[i]);
                }

                if (BlockName.Contains("Light"))
                {
                    lights.Add(TargetBlocks[i]);
                }

                if (BlockName.Contains("Piston"))
                {
                    pistons.Add(TargetBlocks[i]);
                }

                if (BlockName.Contains("Text Panel"))
                {
                    tpanels.Add(TargetBlocks[i]);
                }

                if (BlockName.Contains("Timer"))
                {
                   timer = TargetBlocks[i] as IMyTimerBlock;
                }
            }

            //Writing the blocknames to the panels
            WriteToPanels("Writing target block list...", dpanels, false);
            for (int i = 0; i < TargetBlocks.Count; i++)
            {
                WriteToPanels(TargetBlocks[i].CustomName, dpanels);
            }
            WriteToPanels("Executing Actions...", dpanels);
                      
            //Do stuff with the catagories
            //Doors closing
            try
            {
                for (int i = 0; i < doors.Count; i++)
                {
                    doors[i].ApplyAction("Open_Off");
                }
            }
            catch (NullReferenceException)
            {
                WriteToPanels("No doors found", dpanels);
            }

            //Speaking going
			try
			{
				for (int i = 0; i < speakers.Count; i++)
				{
					speakers[i].ApplyAction("PlaySound");
				}
			}
			catch (NullReferenceException)
			{
                WriteToPanels("No speakers found", dpanels);
			}

            //Lights change colour
			try
			{				
				for (int i = 0; i < lights.Count; i++)
				{
					lights[i].SetValue("Color", VRageMath.Color.Red);
					lights[i].SetValue("Blink Interval", (Single)2);
					lights[i].SetValue("Blink Lenght", (Single)30);
				}
			}
			catch (NullReferenceException)
			{
                WriteToPanels("No lights found", dpanels);
			}
			
            //Pistons reversing
			try
			{
				for (int i = 0; i < pistons.Count; i++)
				{
					pistons[i].ApplyAction("Reverse");
				}
			}
			catch (NullReferenceException)
			{
                WriteToPanels("ERROR: No pistons found. Program exiting.", dpanels);
				return;
			}

            //At the end, start the timer block to start the code again
            timer.ApplyAction("Start");
        }

        public void WriteToPanels(string msg, List<IMyTerminalBlock> panels, bool append = true)
        {
				for (int i = 0; i < panels.Count; i++)
					{
						IMyTextPanel panel = (panels[i] as IMyTextPanel);

						//Append the text       
						panel.WritePublicText(msg + "\n", append);
						panel.ShowPublicTextOnScreen();
					}
        }
        

        public GroupInfo GetStorage()
        {
            GroupInfo groupInfo = new GroupInfo { };
            string[] AllValues = new string[] { };
            AllValues = Storage.Split(';');
            for (int i = 0; i < AllValues.Length; i++)
                {
                    groupInfo.GroupName[i] = AllValues[i];
                    groupInfo.GroupStatus[i] = AllValues[i++];
                }

            return groupInfo;
        }

        public struct GroupInfo
        {
            public string[] GroupName;
            public string[] GroupStatus;
        }

        //=======================================================================
        //////////////////////////END////////////////////////////////////////////
        //=======================================================================
    }
}