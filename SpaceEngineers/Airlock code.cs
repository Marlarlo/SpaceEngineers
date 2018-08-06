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

namespace AirlockCode
{
    public sealed class Program : MyGridProgram
    {
        //=======================================================================
        //////////////////////////BEGIN//////////////////////////////////////////
        //=======================================================================
        public List<IMyTerminalBlock> TargetBlocks;
        public List<IMyTextPanel> TextPanels;
        public List<IMyTextPanel> DebugPanels;
        public List<IMyLightingBlock> Lights;
        public List<IMySensorBlock> Sensors;
        public List<IMySoundBlock> Speakers;
        public List<IMyDoor> DoorsInner;
        public List<IMyDoor> DoorsOuter;
        public List<IMyAirVent> Vents;
        public IMyTimerBlock Timer;
        string Operation;
        string AirlockName;
        string Status;
        string ProgramStatus;
        bool CanSeal;
        bool SensorDetected;
        bool DoorsClosed;
        bool TimerOn;
        float O2LevelAve;

        public Program()
        {
            Init();
        }

        public void Init()
        {
            TargetBlocks = new List<IMyTerminalBlock>(0);
            TextPanels = new List<IMyTextPanel>(0);
            DebugPanels = new List<IMyTextPanel>(0);
            Lights = new List<IMyLightingBlock>(0);
            Sensors = new List<IMySensorBlock>(0);
            Speakers = new List<IMySoundBlock>(0);
            DoorsInner = new List<IMyDoor>(0);
            DoorsOuter = new List<IMyDoor>(0);
            Vents = new List<IMyAirVent>(0);
            Operation = "Intial Value";
            AirlockName = "Initial Value";
            Status = "Initial Value";
            ProgramStatus = "Initial Value";
            CanSeal = false;
            SensorDetected = false;
            DoorsClosed = false;
            TimerOn = true;
            O2LevelAve = 0;
        }
        
        public void Main(string argument)
        {
            //Parse the argument and get blocks. Ignored if the airlock is already in a process
            if (!(String.Equals(argument, "")))
            {
                string InputName;
                string InputOp;
                Parsing parseArg = new Parsing();
                parseArg.ParseArgument(argument, out InputName, out InputOp);

                if (String.Equals(InputName, AirlockName) && String.Equals(InputOp, Operation))
                {
                    return;
                    //You've hit the button more than once......
                }
                else
                {
                    //New airlock setup
                    Init(); //Re-initialise variables
                    AirlockName = InputName;
                    Operation = InputOp;

                    AddTargetBlocks(AirlockName);
                    ProgramStatus = "Button Press";

                    //Initial airlock setup
                    TargetBlocks.ForEach(x => x.ApplyAction("OnOff_On"));
                    Vents.ForEach(x => x.ApplyAction("Depressurize_Off"));
                }
            }

            //Status finding & what to do
            PressureStatus();
            SensorStatus();
            DoorStatus();

            switch (Operation)
            {
                case "Exit":
                    {
                        ExitMethod();
                        break;
                    }
                case "Entry":
                    {
                        EntryMethod();
                        break;
                    }

                default:
                    break;
            }

            //Write current statuses
            DebugPanels.ForEach(x => x.WritePublicText("Program Status finding " + "\n", false));
            DebugPanels.ForEach(x => x.WritePublicText("Airlock Name: " + AirlockName + "\n", true));
            DebugPanels.ForEach(x => x.WritePublicText("Operation: " + Operation + "\n", true));
            DebugPanels.ForEach(x => x.WritePublicText("Pressure status: " + Status + "\n", true));
            DebugPanels.ForEach(x => x.WritePublicText("O2 Level: " + O2LevelAve + "\n", true));
            DebugPanels.ForEach(x => x.WritePublicText("Seal status: " + CanSeal.ToString() + "\n", true));
            DebugPanels.ForEach(x => x.WritePublicText("Program status: " + ProgramStatus + "\n", true));
            DebugPanels.ForEach(x => x.WritePublicText("Sensor detects someone?: " + SensorDetected.ToString() + "\n", true));
            DebugPanels.ForEach(x => x.WritePublicText("Doors closed?: " + DoorsClosed.ToString() + "\n", true));

            //Timer actions below
            if (TimerOn)
            {
                Timer.ApplyAction("Start");
            }
            else
            {
                Timer.ApplyAction("Stop");
                Init(); //Reset variables to default/empty lists
                return;
            }
        }

        class Parsing
        {
            public void ParseArgument(string input, out string name, out string op)
            {
                name = input.Split(' ')[0];
                op = input.Split(' ')[1];
            }
        }

        public void AddTargetBlocks(string name)
        {
            //Get target blocks
            try
            {
                GridTerminalSystem.GetBlockGroupWithName(AirlockName).GetBlocks(TargetBlocks);
            }
            catch (NullReferenceException)
            {
                return;
            }
            List<String> keys = new List<string> { "Inner Door", "Outer Door", "Vent", "Sensor", "Debug Panel", "Text Panel", "Light", "Sound", "Timer" };
            for (int i = 0; i < TargetBlocks.Count; i++)
            {
                string BlockName = TargetBlocks[i].CustomName;
                string KeyResult = keys.Find(x => BlockName.Contains(x));
                switch (KeyResult)
                {
                    case "Inner Door":
                        DoorsInner.Add(TargetBlocks[i] as IMyDoor);
                        break;
                    case "Outer Door":
                        DoorsOuter.Add(TargetBlocks[i] as IMyDoor);
                        break;
                    case "Vent":
                        Vents.Add(TargetBlocks[i] as IMyAirVent);
                        break;
                    case "Sensor":
                        Sensors.Add(TargetBlocks[i] as IMySensorBlock);
                        break;
                    case "Debug Panel":
                        DebugPanels.Add(TargetBlocks[i] as IMyTextPanel);
                        break;
                    case "Text Panel":
                        TextPanels.Add(TargetBlocks[i] as IMyTextPanel);
                        break;
                    case "Light":
                        Lights.Add(TargetBlocks[i] as IMyLightingBlock);
                        break;
                    case "Sound":
                        Speakers.Add(TargetBlocks[i] as IMySoundBlock);
                        break;
                    case "Timer":
                        Timer = TargetBlocks[i] as IMyTimerBlock;
                        break;
                    default:
                        break;
                }
            }
        }

        public void PressureStatus()
        {
            if ((Vents.Exists(x => !(x.CanPressurize))))
            {
                CanSeal = false;
            }
            else
            {
                CanSeal = true;
            }

            float O2LevelMax = 0;
            float O2LevelMin = 1;
            O2LevelAve = 0;
            bool Depressure = false;
            for (int i = 0; i < Vents.Count; i++)
            {
                if (O2LevelMin > Vents[i].GetOxygenLevel())
                {
                    O2LevelMin = Vents[i].GetOxygenLevel();
                }
                if (O2LevelMax < Vents[i].GetOxygenLevel())
                {
                    O2LevelMax = Vents[i].GetOxygenLevel();
                }
                O2LevelAve += Vents[i].GetOxygenLevel();
                if (Vents[i].IsDepressurizing)
                {
                    Depressure = true;
                }
            }

            O2LevelAve /= Vents.Count;

            if (O2LevelMin == 1 && !Depressure)
            {
                Status = "Pressurised";
                return;
            }

            if (O2LevelMin > 0 && !Depressure)
            {
                Status = "Pressurising";
                return;
            }

            if (O2LevelMax > 0)
            {
                Status = "Depressurising";
                return;
            }

            if (O2LevelMax == 0)
            {
                Status = "Depressurised";
                return;
            }

            Status = "Unknown airlock status...";
            return;

        }

        public void SensorStatus()
        {
            if (Sensors.Exists(x => x.IsActive))
            {
                SensorDetected = true;
            }
            else
            {
                SensorDetected = false;
            }
            return;
        }

        public void DoorStatus()
        {
            if (!(DoorsInner.Exists(x => x.OpenRatio > 0) || DoorsOuter.Exists(x => x.OpenRatio > 0)))
            {
                DoorsClosed = true;
            }
            else
            {
                DoorsClosed = false;
            }
            return;
        }

        public void ExitMethod()
        {
            switch (ProgramStatus)
            {
                case "Button Press":
                    {
                        switch (Status)
                        {
                            case "Pressurising":
                                {
                                    SealAirlock();
                                    return;
                                }
                            case "Pressurised":
                                {
                                    DoorsInner.ForEach(x => x.ApplyAction("OnOff_On")); //Turn inner doors on
                                    DoorsInner.ForEach(x => x.ApplyAction("Open_On")); //Open all inner doors since its pressurised
                                    ProgramStatus = "Sensor Waiting"; //Sensor is now waiting for player to enter
                                    return;
                                }
                            case "Depressurising":
                                {
                                    Vents.ForEach(x => x.ApplyAction("Depressurize_Off")); //Turning the depressurising vents off
                                    SealAirlock();
                                    return;
                                }
                            case "Depressurised":
                                {
                                    SealAirlock();
                                    return;
                                }
                            default:
                                {
                                    return;
                                }
                        }
                    }

                case "Sensor Waiting":
                    {
                        switch (Status)
                        {
                            case "Pressurising":
                                {
                                    if (SensorDetected)
                                    {
                                        SealAirlock();
                                    }

                                    if (DoorsClosed)
                                    {
                                        SealAirlock();
                                        Vents.ForEach(x => x.ApplyAction("Depressurize_On"));
                                        ProgramStatus = "Waiting for depressurisation...";
                                    }
                                    return;
                                }
                            case "Pressurised":
                                {
                                    if (SensorDetected)
                                    {
                                        SealAirlock();
                                    }

                                    if (DoorsClosed)
                                    {
                                        SealAirlock();
                                        Vents.ForEach(x => x.ApplyAction("Depressurize_On"));
                                        ProgramStatus = "Waiting for depressurisation...";
                                    }
                                    return;
                                }
                            case "Depressurising":
                                {
                                    SealAirlock();
                                    return;
                                }
                            case "Depressurised":
                                {
                                    SealAirlock();
                                    return;
                                }

                            default:
                                {
                                    return;
                                }
                        }
                    }

                case "Waiting for depressurisation...":
                    {
                        switch (Status)
                        {
                            case "Pressurising":
                                {
                                    Vents.ForEach(x => x.ApplyAction("Depressurize_On"));
                                    SealAirlock();
                                    return;
                                }
                            case "Pressurised":
                                {
                                    Vents.ForEach(x => x.ApplyAction("Depressurize_On"));
                                    SealAirlock();
                                    return;
                                }
                            case "Depressurising":
                                {
                                    SealAirlock();
                                    return;
                                }
                            case "Depressurised":
                                {
                                    ProgramStatus = "Program finished - airlock fully depressurised";
                                    DoorsOuter.ForEach(x => x.ApplyAction("OnOff_On"));
                                    DoorsOuter.ForEach(x => x.ApplyAction("Open_On"));
                                    TimerOn = false;
                                    return;
                                }

                            default:
                                {
                                    return;
                                }
                        }
                    }
            }
            return;
        }

        public void EntryMethod()
        {
            switch (ProgramStatus)
            {
                case "Button Press":
                    {
                        switch (Status)
                        {
                            case "Pressurising":
                                {
                                    SealAirlock();
                                    if (DoorsClosed)
                                    {
                                        Vents.ForEach(x => x.ApplyAction("Depressurize_On")); //Turning the depressurising vents on (although they should already be on if its depressurising)
                                    }
                                    return;
                                }
                            case "Pressurised":
                                {
                                    SealAirlock();
                                    if (DoorsClosed)
                                    {
                                        Vents.ForEach(x => x.ApplyAction("Depressurize_On")); //Turning the depressurising vents on (although they should already be on if its depressurising)
                                    }
                                    return;
                                }
                            case "Depressurising":
                                {
                                    SealAirlock();
                                    if(DoorsClosed)
                                    {
                                        Vents.ForEach(x => x.ApplyAction("Depressurize_On")); //Turning the depressurising vents on (although they should already be on if its depressurising)
                                    }
                                    return;
                                }
                            case "Depressurised":
                                {
                                    DoorsOuter.ForEach(x => x.ApplyAction("OnOff_On"));
                                    DoorsOuter.ForEach(x => x.ApplyAction("Open_On"));
                                    ProgramStatus = "Sensor Waiting";
                                    return;
                                }
                            default:
                                {
                                    return;
                                }
                        }
                    }

                case "Sensor Waiting":
                    {
                        if(SensorDetected)
                        {
                            SealAirlock();
                        }

                        if(DoorsClosed)
                        {
                            SealAirlock();
                            Vents.ForEach(x => x.ApplyAction("Depressurize_Off"));
                            ProgramStatus = "Waiting for Pressurisation...";
                        }
                        return;
                    }

                case "Waiting for Pressurisation...":
                    {
                        switch (Status)
                        {
                            case "Pressurising":
                                {
                                    SealAirlock();
                                    return;
                                }
                            case "Pressurised":
                                {
                                    DoorsInner.ForEach(x => x.ApplyAction("OnOff_On")); //Pressurised, open inner doors (outer doors should be still sealed). Finish program.
                                    DoorsInner.ForEach(x => x.ApplyAction("Open_On"));
                                    TimerOn = false;
                                    ProgramStatus = "Program finished - room pressurised";
                                    return;
                                }
                            case "Depressurising":
                                {
                                    Vents.ForEach(x => x.ApplyAction("Depressurize_Off")); //Shouldn't be depressurising at this stage. Seal the airlock and try to pressurise.
                                    SealAirlock();
                                    return;
                                }
                            case "Depressurised":
                                {
                                    Vents.ForEach(x => x.ApplyAction("Depressurize_Off"));
                                    SealAirlock();
                                    return;
                                }

                            default:
                                {
                                    return;
                                }
                        }
                    }
            }
            return;
        }

        public void SealAirlock()
        {
            DoorStatus();

            if((DoorsInner.Exists(x => !(x.Enabled) && x.OpenRatio != 0)) || DoorsOuter.Exists(x => !(x.Enabled) && x.OpenRatio != 0)) //Check to see if a door is open AND off.
            {
                List<IMyDoor> DefuncDoors = new List<IMyDoor>(0);
                if ((DoorsInner.Exists(x => !(x.Enabled) && x.OpenRatio != 0)))
                {
                    DefuncDoors.AddList(DoorsInner.FindAll(x => !(x.Enabled) && x.OpenRatio != 0));
                }
                if(DoorsOuter.Exists(x => !(x.Enabled) && x.OpenRatio != 0))
                {
                    DefuncDoors.AddList(DoorsOuter.FindAll(x => !(x.Enabled) && x.OpenRatio != 0));
                }
                DefuncDoors.ForEach(x => x.ApplyAction("OnOff_On"));  
            }

            DoorsInner.ForEach(x => x.ApplyAction("Open_Off"));
            DoorsOuter.ForEach(x => x.ApplyAction("Open_Off"));

            if (DoorsClosed)
            {
                DoorsInner.ForEach(x => x.ApplyAction("OnOff_Off"));
                DoorsOuter.ForEach(x => x.ApplyAction("OnOff_Off"));
            }
            return;
        }
        
    }
}

    
