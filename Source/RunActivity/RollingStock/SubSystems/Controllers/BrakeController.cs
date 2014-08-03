﻿// COPYRIGHT 2010, 2011, 2012, 2013 by the Open Rails project.
// 
// This file is part of Open Rails.
// 
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.IO;
using MSTS.Formats;
using MSTS.Parsers;
using ORTS.Common;
using ORTS.Scripting.Api;

namespace ORTS
{
    public class ScriptedBrakeController : IController
    {
        readonly MSTSLocomotive Locomotive;
        readonly Simulator Simulator;

        public bool Activated;
        string ScriptName = "MSTS";
        BrakeController Script;
        public List<MSTSNotch> Notches = new List<MSTSNotch>();

        private bool emergencyBrakingPushButton = false;
        private bool tcsEmergencyBraking = false;
        private bool tcsFullServiceBraking = false;
        public bool EmergencyBraking
        {
            get
            {
                return emergencyBrakingPushButton || tcsEmergencyBraking;
            }
        }
        public bool EmergencyBrakingPushButton
        {
            get
            {
                return emergencyBrakingPushButton;
            }
            set
            {
                if (Script != null)
                {
                    if (value && !emergencyBrakingPushButton && !tcsEmergencyBraking)
                        Simulator.Confirmer.Confirm(CabControl.EmergencyBrake, CabSetting.On);
                    else if (!value && emergencyBrakingPushButton && !tcsEmergencyBraking)
                        Simulator.Confirmer.Confirm(CabControl.EmergencyBrake, CabSetting.Off);

                    emergencyBrakingPushButton = value;
                }
            }
        }
        public bool TCSEmergencyBraking
        {
            get
            {
                return tcsEmergencyBraking;
            }
            set
            {
                if (Script != null)
                {
                    if (value && !emergencyBrakingPushButton && !tcsEmergencyBraking)
                        Simulator.Confirmer.Confirm(CabControl.EmergencyBrake, CabSetting.On);
                    else if (!value && !emergencyBrakingPushButton && tcsEmergencyBraking)
                        Simulator.Confirmer.Confirm(CabControl.EmergencyBrake, CabSetting.Off);

                    tcsEmergencyBraking = value;
                }
            }
        }
        public bool TCSFullServiceBraking
        {
            get
            {
                return tcsFullServiceBraking;
            }
            set
            {
                if (value && !tcsFullServiceBraking)
                    Simulator.Confirmer.Confirm(CabControl.TrainBrake, CabSetting.On);

                tcsFullServiceBraking = value;
            }
        }

        public float MaxPressurePSI { get; private set; }
        public float ReleaseRatePSIpS { get; private set; }
        public float QuickReleaseRatePSIpS { get; private set; }
        public float ApplyRatePSIpS { get; private set; }
        public float EmergencyRatePSIpS { get; private set; }
        public float FullServReductionPSI { get; private set; }
        public float MinReductionPSI { get; private set; }

        public float CurrentValue { get; set; }
        public float MinimumValue { get; set; }
        public float MaximumValue { get; set; }
        public float StepSize { get; set; }
        public float UpdateValue { get; set; }
        public double CommandStartTime { get; set; }

        public ScriptedBrakeController(MSTSLocomotive locomotive)
        {
            Simulator = locomotive.Simulator;
            Locomotive = locomotive;

            MaxPressurePSI = 90;
            ReleaseRatePSIpS = 5;
            QuickReleaseRatePSIpS = 10;
            ApplyRatePSIpS = 2;
            EmergencyRatePSIpS = 10;
            FullServReductionPSI = 26;
            MinReductionPSI = 6;
        }

        public ScriptedBrakeController(ScriptedBrakeController controller, MSTSLocomotive locomotive)
        {
            Simulator = locomotive.Simulator;
            Locomotive = locomotive;

            ScriptName = controller.ScriptName;
            MaxPressurePSI = controller.MaxPressurePSI;
            ReleaseRatePSIpS = controller.ReleaseRatePSIpS;
            QuickReleaseRatePSIpS = controller.QuickReleaseRatePSIpS;
            ApplyRatePSIpS = controller.ApplyRatePSIpS;
            EmergencyRatePSIpS = controller.EmergencyRatePSIpS;
            FullServReductionPSI = controller.FullServReductionPSI;
            MinReductionPSI = controller.MinReductionPSI;

            CurrentValue = controller.CurrentValue;
            MinimumValue = controller.MinimumValue;
            MaximumValue = controller.MaximumValue;
            StepSize = controller.StepSize;

            controller.Notches.ForEach(
                (item) => { Notches.Add(new MSTSNotch(item)); }
            );

            Initialize();
        }

        public ScriptedBrakeController Clone(MSTSLocomotive locomotive)
        {
            return new ScriptedBrakeController(this, locomotive);
        }

        public void Parse(STFReader stf)
        {
            Parse(stf.Tree.ToLower(), stf);
        }

        public void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(trainbrakescontrollermaxsystempressure":
                case "engine(enginebrakescontrollermaxsystempressure":
                    MaxPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null);
                    break;

                case "engine(trainbrakescontrollermaxreleaserate":
                case "engine(enginebrakescontrollermaxreleaserate":    
                    ReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null);
                    break;

                case "engine(trainbrakescontrollermaxquickreleaserate":
                case "engine(enginebrakescontrollermaxquickreleaserate":
                    QuickReleaseRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null);
                    break;

                case "engine(trainbrakescontrollermaxapplicationrate":
                case "engine(enginebrakescontrollermaxapplicationrate":
                    ApplyRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null);
                    break;

                case "engine(trainbrakescontrolleremergencyapplicationrate":
                case "engine(enginebrakescontrolleremergencyapplicationrate":
                    EmergencyRatePSIpS = stf.ReadFloatBlock(STFReader.UNITS.PressureRateDefaultPSIpS, null);
                    break;

                case "engine(trainbrakescontrollerfullservicepressuredrop":
                case "engine(enginebrakescontrollerfullservicepressuredrop":
                    FullServReductionPSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null);
                    break;

                case "engine(trainbrakescontrollerminpressurereduction":
                case "engine(enginebrakescontrollerminpressurereduction":
                    MinReductionPSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, null);
                    break;

                case "engine(enginecontrollers(brake_train":
                case "engine(enginecontrollers(brake_engine":
                    stf.MustMatch("(");
                    MinimumValue = stf.ReadFloat(STFReader.UNITS.None, null);
                    MaximumValue = stf.ReadFloat(STFReader.UNITS.None, null);
                    StepSize = stf.ReadFloat(STFReader.UNITS.None, null);
                    CurrentValue = stf.ReadFloat(STFReader.UNITS.None, null);
                    string token = stf.ReadItem(); // s/b numnotches
                    if (string.Compare(token, "NumNotches", true) != 0) // handle error in gp38.eng where extra parameter provided before NumNotches statement 
                        stf.ReadItem();
                    stf.MustMatch("(");
                    stf.ReadInt(null);
                    stf.ParseBlock(new STFReader.TokenProcessor[] {
                        new STFReader.TokenProcessor("notch", ()=>{
                            stf.MustMatch("(");
                            float value = stf.ReadFloat(STFReader.UNITS.None, null);
                            int smooth = stf.ReadInt(null);
                            string type = stf.ReadString();
                            Notches.Add(new MSTSNotch(value, smooth, type, stf));
                            if (type != ")") stf.SkipRestOfBlock();
                        }),
                    });
                    break;

                case "engine(ortstrainbrakecontroller":
                case "engine(ortsenginebrakecontroller":
                    if (Locomotive.Train as AITrain == null)
                    {
                        ScriptName = stf.ReadStringBlock(null);
                    }
                    break;
            }
        }

        public void Initialize()
        {
            if (!Activated)
            {
                if (ScriptName != null && ScriptName != "MSTS")
                {
                    var pathArray = new string[] { Path.Combine(Path.GetDirectoryName(Locomotive.WagFilePath), "Script") };
                    Script = Simulator.ScriptManager.Load(pathArray, ScriptName) as BrakeController;
                }
                if (Script == null)
                {
                    Script = new MSTSBrakeController() as BrakeController;
                }

                // AbstractScriptClass
                Script.ClockTime = () => (float)Simulator.ClockTime;
                Script.DistanceM = () => Locomotive.DistanceM;

                // BrakeController
                Script.GraduatedRelease = () => Simulator.Settings.GraduatedRelease;
                Script.EmergencyBrakingPushButton = () => EmergencyBrakingPushButton;
                Script.TCSEmergencyBraking = () => TCSEmergencyBraking;
                Script.TCSFullServiceBraking = () => TCSFullServiceBraking;

                Script.MainReservoirPressureBar = () =>
                {
                    if (Locomotive.Train != null)
                        return Bar.FromPSI(Locomotive.Train.BrakeLine2PressurePSI);
                    else
                        return float.MaxValue;
                };
                Script.MaxPressureBar = () => Bar.FromPSI(MaxPressurePSI);
                Script.ReleaseRateBarpS = () => BarpS.FromPSIpS(ReleaseRatePSIpS);
                Script.QuickReleaseRateBarpS = () => BarpS.FromPSIpS(QuickReleaseRatePSIpS);
                Script.ApplyRateBarpS = () => BarpS.FromPSIpS(ApplyRatePSIpS);
                Script.EmergencyRateBarpS = () => BarpS.FromPSIpS(EmergencyRatePSIpS);
                Script.FullServReductionBar = () => Bar.FromPSI(FullServReductionPSI);
                Script.MinReductionBar = () => Bar.FromPSI(MinReductionPSI);
                Script.CurrentValue = () => CurrentValue;
                Script.MinimumValue = () => MinimumValue;
                Script.MaximumValue = () => MaximumValue;
                Script.StepSize = () => StepSize;
                Script.UpdateValue = () => UpdateValue;
                Script.Notches = () => Notches;

                Script.SetCurrentValue = (value) => CurrentValue = value;
                Script.SetUpdateValue = (value) => UpdateValue = value;

                Script.Initialize();
            }
        }

        public float Update(float elapsedClockSeconds)
        {
            if (Script != null)
                return Script.Update(elapsedClockSeconds);
            else
                return 0;
        }

        public void UpdatePressure(ref float pressurePSI, float elapsedClockSeconds, ref float epPressurePSI)
        {
            if (Script != null)
            {
                // Conversion is needed until the pressures of the brake system are converted to bar.
                float pressureBar = Bar.FromPSI(pressurePSI);
                float epPressureBar = Bar.FromPSI(epPressurePSI);
                Script.UpdatePressure(ref pressureBar, elapsedClockSeconds, ref epPressureBar);
                pressurePSI = Bar.ToPSI(pressureBar);
                epPressurePSI = Bar.ToPSI(epPressureBar);
            }
        }

        public void UpdateEngineBrakePressure(ref float pressurePSI, float elapsedClockSeconds)
        {
            if (Script != null)
            {
                // Conversion is needed until the pressures of the brake system are converted to bar.
                float pressureBar = Bar.FromPSI(pressurePSI);
                Script.UpdateEngineBrakePressure(ref pressureBar, elapsedClockSeconds);
                pressurePSI = Bar.ToPSI(pressureBar);
            }
        }

        public void SendEvent(BrakeControllerEvent evt)
        {
            if (Script != null)
                Script.HandleEvent(evt);
        }

        public void SendEvent(BrakeControllerEvent evt, float? value)
        {
            if (Script != null)
                Script.HandleEvent(evt, value);
        }

        public void StartIncrease()
        {
            SendEvent(BrakeControllerEvent.StartIncrease);
        }

        public void StopIncrease()
        {
            SendEvent(BrakeControllerEvent.StopIncrease);
        }

        public void StartDecrease()
        {
            SendEvent(BrakeControllerEvent.StartDecrease);
        }

        public void StopDecrease()
        {
            SendEvent(BrakeControllerEvent.StopDecrease);
        }

        public void StartIncrease(float? target)
        {
            SendEvent(BrakeControllerEvent.StartIncrease, target);
        }

        public void StartDecrease(float? target)
        {
            SendEvent(BrakeControllerEvent.StartDecrease, target);
        }

        public float SetPercent(float percent)
        {
            SendEvent(BrakeControllerEvent.SetCurrentPercent, percent);
            return CurrentValue;
        }

        public void SetValue(float v)
        {
            SendEvent(BrakeControllerEvent.SetCurrentValue, v);
        }

        public bool IsValid()
        {
            if (Script != null)
                return Script.IsValid();
            else
                return true;
        }

        public string GetStatus()
        {
            if (Script != null)
            {
                string state = ControllerStateDictionary.Dict[Script.GetState()];
                string fraction = GetStateFraction();

                if (String.IsNullOrEmpty(state) && String.IsNullOrEmpty(fraction))
                    return String.Empty;
                else if (!String.IsNullOrEmpty(state) && String.IsNullOrEmpty(fraction))
                    return state;
                else if (String.IsNullOrEmpty(state) && !String.IsNullOrEmpty(fraction))
                    return fraction;
                else
                    return String.Format("{0} {1}", state, fraction);
            }
            else
                return String.Empty;
        }

        private string GetStateFraction()
        {
            if (Script != null)
            {
                float? fraction = Script.GetStateFraction();

                if (fraction != null)
                    return String.Format("{0:F0}%", 100 * (fraction ?? 0));
                else
                    return String.Empty;
            }
            else
            {
                return String.Empty;
            }
        }

        public void Save(BinaryWriter outf)
        {
            outf.Write((int)ControllerTypes.BrakeController);

            outf.Write(CurrentValue);
        }

        public void Restore(BinaryReader inf)
        {
            SendEvent(BrakeControllerEvent.SetCurrentValue, inf.ReadSingle());
        }
    }
}