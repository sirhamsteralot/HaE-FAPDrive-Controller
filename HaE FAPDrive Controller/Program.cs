using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        #region settings
        public INISerializer serializer = new INISerializer("Global Settings");
        public string groupTag { get { return (string)serializer.GetValue("groupTag"); }}
        public string sideATag { get { return (string)serializer.GetValue("sideATag"); } }
        public string sideBTag { get { return (string)serializer.GetValue("sideBTag"); } }
        public string shipControllerName { get { return (string)serializer.GetValue("shipControllerName"); } }
        public bool invertThrottle { get { return (bool)serializer.GetValue("invertThrottle"); } }
        public float throttleincrements { get { return (float)serializer.GetValue("throttleincrements"); } }
        #endregion

        public List<Fapdrive> fapDrives = new List<Fapdrive>();
        public float currentThrottle = 0;
        IMyShipController controller;


        public Program()
        {
            #region settings
            serializer.AddValue("groupTag", x => x, "[Fapdrive]");
            serializer.AddValue("sideATag", x => x, "[A]");
            serializer.AddValue("sideBTag", x => x, "[B]");
            serializer.AddValue("shipControllerName", x => x, "Control");
            serializer.AddValue("throttleincrements", x => float.Parse(x), 0.05f);
            serializer.AddValue("invertThrottle", x => bool.Parse(x), true);

            if (Me.CustomData == "")
            {
                string temp = Me.CustomData;
                serializer.FirstSerialization(ref temp);
                Me.CustomData = temp;
            }
            else
            {
                serializer.DeSerialize(Me.CustomData);
            }
            #endregion

            List<IMyBlockGroup> tempList = new List<IMyBlockGroup>();
            GridTerminalSystem.GetBlockGroups(tempList, x => x.Name.Contains(groupTag));
            foreach (var group in tempList)
            {
                Fapdrive drive = new Fapdrive(group, sideATag, sideBTag);
                Echo($"Registering fapdrive with:\n{drive.driveRotors.Count} rotors and {drive.sideA.Count + drive.sideB.Count} containers");
                fapDrives.Add(drive);
            }

            controller = GridTerminalSystem.GetBlockWithName(shipControllerName) as IMyShipController;

            Echo($"Drives found: {fapDrives.Count}");

            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            SetThrottleFromInput();

            if ((updateSource & (UpdateType.Terminal | UpdateType.Trigger)) != 0)
            {
                if (argument == "++")
                {
                    ThrottleDrives(currentThrottle + throttleincrements);
                } else if (argument == "--")
                {
                    ThrottleDrives(currentThrottle - throttleincrements);
                }

                float throttleVal = 0;
                if (float.TryParse(argument, out throttleVal))
                {
                    ThrottleDrives(throttleVal);
                }
            } else if (currentThrottle != 0)
            {
                foreach (var drive in fapDrives)
                {
                    drive.Main();
                }
            }
        }

        public void SetThrottleFromInput()
        {
            Vector3D controlVector = controller.MoveIndicator;

            double forward = controlVector.Dot(Vector3D.Forward);

            if (forward > 0)
            {
                ThrottleDrives(currentThrottle + throttleincrements);
            } else if (forward < 0)
            {
                ThrottleDrives(currentThrottle - throttleincrements);
            } else if (forward == 0)
            {
                ThrottleDrives(0);
            }
        }

        public void ThrottleDrives(float value)
        {
            currentThrottle = MyMath.Clamp(value, -1, 1);

            if (invertThrottle)
                value = -value;

            foreach (var drive in fapDrives)
            {
                drive.SetThrust(value);
            }
        }
    }
}