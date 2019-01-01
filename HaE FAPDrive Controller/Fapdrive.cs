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
    partial class Program
    {
        public class Fapdrive
        {
            private Scheduler internalScheduler;

            public List<IMyMotorStator> driveRotors;
            public List<IMyCargoContainer> sideA;
            public List<IMyCargoContainer> sideB;

            public double throttlePercentage = 0;
            public Vector3D driveDirection;
            public bool reverse = false;

            public Fapdrive(IMyBlockGroup group, string sideATag, string sideBTag)
            {
                internalScheduler = new Scheduler();

                driveRotors = new List<IMyMotorStator>();
                sideA = new List<IMyCargoContainer>();
                sideB = new List<IMyCargoContainer>();

                group.GetBlocksOfType(driveRotors);

                group.GetBlocksOfType(sideA, x => x.CustomName.Contains(sideATag));
                group.GetBlocksOfType(sideB, x => x.CustomName.Contains(sideBTag));

                driveDirection = driveRotors.First().WorldMatrix.Up;

                internalScheduler.AddTask(ThrustSequence());
            }

            public void Main()
            {
                internalScheduler.Main();
            }

            public void SetThrust(float throttlePercentage)
            {
                if (throttlePercentage < 0)
                {
                    throttlePercentage = -throttlePercentage;
                    reverse = true;
                } else
                {
                    reverse = false;
                }

                this.throttlePercentage = MyMath.Clamp(throttlePercentage, 0f, 1f);
            }

            public IEnumerator<bool> ThrustSequence()
            {
                while (true)
                {
                    if (reverse)
                    {
                        MoveMaterials(false);
                        SetRotors(-100);
                        yield return true;
                        MoveMaterials(true);
                        SetRotors(100);
                        yield return true;
                    } else
                    {
                        MoveMaterials(false);
                        SetRotors(100);
                        yield return true;
                        MoveMaterials(true);
                        SetRotors(-100);
                        yield return true;
                    }
                }
            }

            public void MoveMaterials(bool toSideB)
            {
                foreach(var contA in sideA)
                {
                    foreach (var contB in sideB)
                    {
                        if (toSideB)
                        {
                            MoveStuff(contA, contB, false);
                        } else
                        {
                            MoveStuff(contB, contA, true);
                        }
                    }
                }
            }

            public void MoveStuff(IMyCargoContainer source, IMyCargoContainer dest, bool fullAmount)
            {
                IMyInventory src = source.GetInventory();
                IMyInventory dst = dest.GetInventory();

                var items = src.GetItems();
                int itemCount = items.Count;
                for (int i = 0; i < itemCount; i++)
                {
                    if (!fullAmount)
                    {
                        var itemstomove = items[i].Amount * (float)throttlePercentage;
                        src.TransferItemTo(dst, i, null, true, itemstomove);
                    } else
                    {
                        src.TransferItemTo(dst, i);
                    }
                }
            }

            public void SetRotors(float offset)
            {
                foreach (var rotor in driveRotors)
                    rotor.SetValueFloat("Displacement", offset);
            }
        }
    }
}
