﻿// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
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

/* DIESEL LOCOMOTIVE CLASSES
 * 
 * The Locomotive is represented by two classes:
 *  MSTSDieselLocomotiveSimulator - defines the behaviour, ie physics, motion, power generated etc
 *  MSTSDieselLocomotiveViewer - defines the appearance in a 3D viewer.  The viewer doesn't
 *  get attached to the car until it comes into viewing range.
 *  
 * Both these classes derive from corresponding classes for a basic locomotive
 *  LocomotiveSimulator - provides for movement, basic controls etc
 *  LocomotiveViewer - provides basic animation for running gear, wipers, etc
 * 
 */

//#define ALLOW_ORTS_SPECIFIC_ENG_PARAMETERS

using Microsoft.Xna.Framework.Graphics;
using Orts.Formats.Msts;
using Orts.Parsers.Msts;
using Orts.Simulation.Physics;
using Orts.Simulation.RollingStocks.SubSystems.Controllers;
using Orts.Simulation.RollingStocks.SubSystems.PowerSupplies;
using Orts.Simulation.RollingStocks.SubSystems.PowerTransmissions;
using ORTS.Common;
using System;
using System.IO;
using System.Text;
using Event = Orts.Common.Event;

namespace Orts.Simulation.RollingStocks
{
    ///////////////////////////////////////////////////
    ///   SIMULATION BEHAVIOUR
    ///////////////////////////////////////////////////

    /// <summary>
    /// Adds physics and control for a diesel locomotive
    /// </summary>
    public class MSTSDieselLocomotive : MSTSLocomotive
    {
        public float IdleRPM;
        public float MaxRPM;
        public float MaxRPMChangeRate;
        public float PercentChangePerSec = .2f;
        public float InitialExhaust;
        public float InitialMagnitude;
        public float MaxExhaust = 2.8f;
        public float MaxMagnitude = 1.5f;
        public float EngineRPMderivation;
        float EngineRPMold;
        float EngineRPMRatio; // used to compute Variable1 and Variable2

        public MSTSNotchController FuelController = new MSTSNotchController(0, 1, 0.1f);
        public float MaxDieselLevelL = 5000.0f;
        public float DieselLevelL
        {
            get { return FuelController.CurrentValue * MaxDieselLevelL; }
            set { FuelController.CurrentValue = value / MaxDieselLevelL; }
        }
        public float DieselUsedPerHourAtMaxPowerL = 1.0f;
        public float DieselUsedPerHourAtIdleL = 1.0f;
        public float DieselFlowLps;
        float DieselWeightKgpL = 0.8f; //per liter
        float InitialMassKg = 100000.0f;

        public float EngineRPM;
        public SmoothedData ExhaustParticles = new SmoothedData(1);
        public SmoothedData ExhaustMagnitude = new SmoothedData(1);
        public SmoothedData ExhaustColorR = new SmoothedData(1);
        public SmoothedData ExhaustColorG = new SmoothedData(1);
        public SmoothedData ExhaustColorB = new SmoothedData(1);
        public Color ExhaustTransientColor = Color.Black;
        public Color ExhaustDecelColor = Color.WhiteSmoke;
        public Color ExhaustSteadyColor = Color.Gray;

        public float DieselOilPressurePSI = 0f;
        public float DieselMinOilPressurePSI = 40f;
        public float DieselMaxOilPressurePSI = 120f;
        public float DieselTemperatureDeg = 40f;
        public float DieselMaxTemperatureDeg = 100.0f;
        public DieselEngine.Cooling DieselEngineCooling = DieselEngine.Cooling.Proportional;

        public DieselEngines DieselEngines;

        public GearBox GearBox = new GearBox();

        public MSTSDieselLocomotive(Simulator simulator, string wagFile)
            : base(simulator, wagFile)
        {
            PowerOn = true;
            RefillImmediately();
        }

        /// <summary>
        /// Parse the wag file parameters required for the simulator and viewer classes
        /// </summary>
        public override void Parse(string lowercasetoken, STFReader stf)
        {
            switch (lowercasetoken)
            {
                case "engine(dieselengineidlerpm": IdleRPM = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselenginemaxrpm": MaxRPM = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselenginemaxrpmchangerate": MaxRPMChangeRate = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;

                case "engine(effects(dieselspecialeffects": ParseEffects(lowercasetoken, stf); break;
                case "engine(dieselsmokeeffectinitialsmokerate": InitialExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselsmokeeffectinitialmagnitude": InitialMagnitude = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselsmokeeffectmaxsmokerate": MaxExhaust = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(dieselsmokeeffectmaxmagnitude": MaxMagnitude = stf.ReadFloatBlock(STFReader.UNITS.None, null); break;
                case "engine(ortsdieselengines": DieselEngines = new DieselEngines(this, stf); break;
                case "engine(maxdiesellevel": MaxDieselLevelL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "engine(dieselusedperhouratmaxpower": DieselUsedPerHourAtMaxPowerL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "engine(dieselusedperhouratidle": DieselUsedPerHourAtIdleL = stf.ReadFloatBlock(STFReader.UNITS.Volume, null); break;
                case "engine(maxoilpressure": DieselMaxOilPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, 120f); break;
                case "engine(ortsminoilpressure": DieselMinOilPressurePSI = stf.ReadFloatBlock(STFReader.UNITS.PressureDefaultPSI, 40f); break;
                case "engine(maxtemperature": DieselMaxTemperatureDeg = stf.ReadFloatBlock(STFReader.UNITS.TemperatureDifference, 100f); break;
                case "engine(ortsdieselcooling": DieselEngineCooling = (DieselEngine.Cooling)stf.ReadInt((int)DieselEngine.Cooling.Proportional); break;
                default:
                    GearBox.Parse(lowercasetoken, stf);
                    base.Parse(lowercasetoken, stf); break;
            }

            if (IdleRPM != 0 && MaxRPM != 0 && MaxRPMChangeRate != 0)
            {
                PercentChangePerSec = MaxRPMChangeRate / (MaxRPM - IdleRPM);
                EngineRPM = IdleRPM;
            }
        }

        public override void LoadFromWagFile(string wagFilePath)
        {
            base.LoadFromWagFile(wagFilePath);

            if (DieselEngines == null)
                DieselEngines = new DieselEngines(this);

            if (DieselEngines.Count == 0)
            {
                DieselEngines.Add(new DieselEngine());

                DieselEngines[0].InitFromMSTS(this);
                DieselEngines[0].Initialize(true);
            }

            InitialMassKg = MassKG;
        }

        /// <summary>
        /// This initializer is called when we are making a new copy of a locomotive already
        /// loaded in memory.  We use this one to speed up loading by eliminating the
        /// need to parse the wag file multiple times.
        /// NOTE:  you must initialize all the same variables as you parsed above
        /// </summary>
        public override void Copy(MSTSWagon copy)
        {
            base.Copy(copy);  // each derived level initializes its own variables

            MSTSDieselLocomotive locoCopy = (MSTSDieselLocomotive)copy;
            EngineRPM = locoCopy.EngineRPM;
            IdleRPM = locoCopy.IdleRPM;
            MaxRPM = locoCopy.MaxRPM;
            MaxRPMChangeRate = locoCopy.MaxRPMChangeRate;

            PercentChangePerSec = locoCopy.PercentChangePerSec;

            EngineRPMderivation = locoCopy.EngineRPMderivation;
            EngineRPMold = locoCopy.EngineRPMold;

            MaxDieselLevelL = locoCopy.MaxDieselLevelL;
            DieselUsedPerHourAtMaxPowerL = locoCopy.DieselUsedPerHourAtMaxPowerL;
            DieselUsedPerHourAtIdleL = locoCopy.DieselUsedPerHourAtIdleL;

            DieselFlowLps = 0.0f;
            InitialMassKg = MassKG;

            if (this.CarID.StartsWith("0"))
                DieselLevelL = locoCopy.DieselLevelL;
            else
                DieselLevelL = locoCopy.MaxDieselLevelL;

            if (locoCopy.GearBoxController != null)
                GearBoxController = new MSTSNotchController(locoCopy.GearBoxController);

            DieselEngines = new DieselEngines(locoCopy.DieselEngines, this);
            foreach (DieselEngine de in DieselEngines)
            {
                de.Initialize(true);
            }
        }

        public override void Initialize()
        {
            if ((GearBox != null) && (GearBoxController == null))
            {
                if (!GearBox.IsInitialized)
                    GearBox = null;
                else
                {
                    foreach (DieselEngine de in DieselEngines)
                    {
                        if (de.GearBox == null)
                            de.GearBox = new GearBox(GearBox, de);
                        //if (this.Train.TrainType == Train.TRAINTYPE.AI)
                        //    de.GearBox.GearBoxOperation = GearBoxOperation.Automatic;
                    }
                    GearBoxController = new MSTSNotchController(DieselEngines[0].GearBox.NumOfGears + 1);
                }
            }

            DieselEngines.Initialize(false);

            base.Initialize();
        }

        /// <summary>
        /// We are saving the game.  Save anything that we'll need to restore the 
        /// status later.
        /// </summary>
        public override void Save(BinaryWriter outf)
        {
            // for example
            // outf.Write(Pan);
            base.Save(outf);
            outf.Write(DieselLevelL);
            ControllerFactory.Save(GearBoxController, outf);
            DieselEngines.Save(outf);
        }

        /// <summary>
        /// We are restoring a saved game.  The TrainCar class has already
        /// been initialized.   Restore the game state.
        /// </summary>
        public override void Restore(BinaryReader inf)
        {
            base.Restore(inf);
            DieselLevelL = inf.ReadSingle();
            ControllerFactory.Restore(GearBoxController, inf);
            DieselEngines.Restore(inf);
        }

        //================================================================================================//
        /// <summary>
        /// Set starting conditions  when initial speed > 0 
        /// 

        public override void InitializeMoving()
        {
            base.InitializeMoving();
            WheelSpeedMpS = SpeedMpS;
            DynamicBrakePercent = -1;
            if (DieselEngines[0].GearBox != null && GearBoxController != null) 
            {
                DieselEngines[0].GearBox.InitializeMoving();
                DieselEngines[0].InitializeMoving();
                if (IsLeadLocomotive())
                {
                    Train.MUGearboxGearIndex = DieselEngines[0].GearBox.CurrentGearIndex+1;
                    Train.AITrainGearboxGearIndex = DieselEngines[0].GearBox.CurrentGearIndex+1;
                }
                GearBoxController.CurrentNotch = Train.MUGearboxGearIndex;
                GearboxGearIndex = DieselEngines[0].GearBox.CurrentGearIndex+1;
                GearBoxController.SetValue((float)GearBoxController.CurrentNotch);
            }
            ThrottleController.SetValue(Train.MUThrottlePercent / 100);
        }

        /// <summary>
        /// This is a periodic update to calculate physics 
        /// parameters and update the base class's MotiveForceN 
        /// and FrictionForceN values based on throttle settings
        /// etc for the locomotive.
        /// </summary>
        public override void Update(float elapsedClockSeconds)
        {
            if (this.Train.TrainType == Train.TRAINTYPE.AI || this.Train.TrainType == Train.TRAINTYPE.AI_PLAYERHOSTING)
            {
                foreach (DieselEngine de in DieselEngines)
                {
                    if (de.EngineStatus != DieselEngine.Status.Running)
                        de.Initialize(true);
                    if(de.GearBox != null)
                        de.GearBox.GearBoxOperation = GearBoxOperation.Automatic;
                }
            }

            TrainBrakeController.Update(elapsedClockSeconds);
            if( TrainBrakeController.UpdateValue > 0.0 ) {
                Simulator.Confirmer.Update(CabControl.TrainBrake, CabSetting.Increase, GetTrainBrakeStatus());
            }
            if( TrainBrakeController.UpdateValue < 0.0 ) {
                Simulator.Confirmer.Update(CabControl.TrainBrake, CabSetting.Decrease, GetTrainBrakeStatus());
            }

            if( EngineBrakeController != null ) {
                EngineBrakeController.Update( elapsedClockSeconds );
                if( EngineBrakeController.UpdateValue > 0.0 ) {
                    Simulator.Confirmer.Update(CabControl.EngineBrake, CabSetting.Increase, GetEngineBrakeStatus());
                }
                if( EngineBrakeController.UpdateValue < 0.0 ) {
                    Simulator.Confirmer.Update(CabControl.EngineBrake, CabSetting.Decrease, GetEngineBrakeStatus());
                }
            }

            if (DynamicBrakeController != null && (DynamicBrakePercent >= 0 || IsLeadLocomotive() && DynamicBrakeIntervention >= 0))
            {
                if (!DynamicBrake)
                {
                    if (DynamicBrakeController.CommandStartTime + DynamicBrakeDelayS < Simulator.ClockTime)
                    {
                        DynamicBrake = true; // Engage
                        if (IsLeadLocomotive())
                            Simulator.Confirmer.ConfirmWithPerCent(CabControl.DynamicBrake, DynamicBrakeController.CurrentValue * 100);
                    }
                    else if (IsLeadLocomotive())
                        Simulator.Confirmer.Confirm(CabControl.DynamicBrake, CabSetting.On); // Keeping status string on screen so user knows what's happening
                }
                else if (this.IsLeadLocomotive())
                {
                    DynamicBrakeController.Update(elapsedClockSeconds);
                    DynamicBrakePercent = (DynamicBrakeIntervention < 0 ? DynamicBrakeController.CurrentValue : DynamicBrakeIntervention) * 100.0f;

                    if (DynamicBrakeIntervention < 0 && PreviousDynamicBrakeIntervention >= 0 && DynamicBrakePercent == 0)
                        DynamicBrakePercent = -1;
                    PreviousDynamicBrakeIntervention = DynamicBrakeIntervention;
                }
                else
                    DynamicBrakeController.Update(elapsedClockSeconds);
            }
            else if (DynamicBrakeController != null && DynamicBrakePercent < 0 && (DynamicBrakeIntervention < 0 || !IsLeadLocomotive()) && DynamicBrake)
            {
                // <CScomment> accordingly to shown documentation dynamic brake delay is required only when engaging
                //            if (DynamicBrakeController.CommandStartTime + DynamicBrakeDelayS < Simulator.ClockTime)
                //             {
                DynamicBrake = false; // Disengage
                if (IsLeadLocomotive())
                    Simulator.Confirmer.Confirm(CabControl.DynamicBrake, CabSetting.Off);
                //           }
                //           else if (IsLeadLocomotive())
                //               Simulator.Confirmer.Confirm(CabControl.DynamicBrake, CabSetting.On); // Keeping status string on screen so user knows what's happening
            }




            //Currently the ThrottlePercent is global to the entire train
            //So only the lead locomotive updates it, the others only updates the controller (actually useless)
            if (this.IsLeadLocomotive() || (!AcceptMUSignals))
            {
                var throttleCurrentNotch = ThrottleController.CurrentNotch;
                ThrottleController.Update(elapsedClockSeconds);
                if (ThrottleController.CurrentNotch < throttleCurrentNotch && ThrottleController.ToZero)
                    SignalEvent(Event.ThrottleChange);
                ThrottlePercent = (ThrottleIntervention < 0 ? ThrottleController.CurrentValue : ThrottleIntervention) * 100.0f;

                if (GearBoxController != null)
                {
                    GearboxGearIndex = (int)GearBoxController.Update(elapsedClockSeconds);
                }
            }
            else
            {
                ThrottleController.Update(elapsedClockSeconds);
                if (GearBoxController != null)
                {
                    GearBoxController.Update(elapsedClockSeconds);
                }
            }
            LocalThrottlePercent = ThrottlePercent;

#if INDIVIDUAL_CONTROL
			//this train is remote controlled, with mine as a helper, so I need to send the controlling information, but not the force.
			if (MultiPlayer.MPManager.IsMultiPlayer() && this.Train.TrainType == Train.TRAINTYPE.REMOTE && this == Program.Simulator.PlayerLocomotive)
			{
				//cannot control train brake as it is the remote's job to do so
				if ((EngineBrakeController != null && EngineBrakeController.UpdateValue != 0.0) || (DynamicBrakeController != null && DynamicBrakeController.UpdateValue != 0.0) || ThrottleController.UpdateValue != 0.0)
				{
					controlUpdated = true;
				}
				ThrottlePercent = ThrottleController.Update(elapsedClockSeconds) * 100.0f;
				return; //done, will go back and send the message to the remote train controller
			}

			if (MultiPlayer.MPManager.IsMultiPlayer() && this.notificationReceived == true)
			{
				ThrottlePercent = ThrottleController.CurrentValue * 100.0f;
				this.notificationReceived = false;
			}
#endif
			
			// TODO  this is a wild simplification for diesel electric
            //float e = (EngineRPM - IdleRPM) / (MaxRPM - IdleRPM); //
            float throttleNum = ThrottlePercent / 100f;

            if (!this.Simulator.UseAdvancedAdhesion)
                AbsWheelSpeedMpS = AbsSpeedMpS;

            foreach (DieselEngine de in DieselEngines)
            {
                if (de.EngineStatus == DieselEngine.Status.Running)
                    de.DemandedThrottlePercent = ThrottlePercent;
                else
                    de.DemandedThrottlePercent = 0f;

                if (Direction == Direction.Reverse)
                    PrevMotiveForceN *= -1f;

                if ((de.EngineStatus == DieselEngine.Status.Running) && (ThrottlePercent > 0))
                {
                    de.OutputPowerW = (PrevMotiveForceN > 0 ? PrevMotiveForceN * AbsSpeedMpS : 0) / DieselEngines.NumOfActiveEngines;
                }
                else
                    de.OutputPowerW = 0.0f;
                de.Update(elapsedClockSeconds);

                if (de.GearBox != null)
                {
                    if ((this.IsLeadLocomotive()))
                    {
                        if (de.GearBox.GearBoxOperation == GearBoxOperation.Manual)
                        {
                            if (GearBoxController.CurrentNotch > 0)
                                de.GearBox.NextGear = de.GearBox.Gears[GearBoxController.CurrentNotch - 1];
                            else
                                de.GearBox.NextGear = null;
                        }
                    }
                    else
                    {
                        if (de.GearBox.GearBoxOperation == GearBoxOperation.Manual)
                        {
                            if (GearboxGearIndex > 0)
                                de.GearBox.NextGear = de.GearBox.Gears[GearboxGearIndex - 1];
                            else
                                de.GearBox.NextGear = null;
                        }
                    }
                    if (de.GearBox.CurrentGear == null)
                        de.OutputPowerW = 0f;

                    de.GearBox.Update(elapsedClockSeconds);
                }
            }

            //Initial smoke, when locomotive is started:
            

            ExhaustParticles.Update(elapsedClockSeconds, DieselEngines[0].ExhaustParticles);
            ExhaustMagnitude.Update(elapsedClockSeconds, DieselEngines[0].ExhaustMagnitude);
            ExhaustColorR.Update(elapsedClockSeconds, DieselEngines[0].ExhaustColor.R);
            ExhaustColorG.Update(elapsedClockSeconds, DieselEngines[0].ExhaustColor.G);
            ExhaustColorB.Update(elapsedClockSeconds, DieselEngines[0].ExhaustColor.B);

            PowerOn = DieselEngines.PowerOn;
            AuxPowerOn = DieselEngines.PowerOn;

            if (PowerOn)
            {
                if (TractiveForceCurves == null)
                {
                    float maxForceN = Math.Min(throttleNum * MaxForceN, AbsWheelSpeedMpS == 0.0f ? ( throttleNum * MaxForceN ) : ( throttleNum * DieselEngines.MaxOutputPowerW / AbsWheelSpeedMpS));
                    //float maxForceN = MaxForceN * t;
                    float maxPowerW = 0.98f * DieselEngines.MaxOutputPowerW;      //0.98 added to let the diesel engine handle the adhesion-caused jittering

                    if (DieselEngines.HasGearBox)
                    {
                        MotiveForceN = DieselEngines.MotiveForceN;
                    }
                    else
                    {
                        
                        if (maxForceN * AbsWheelSpeedMpS > maxPowerW)
                            maxForceN = maxPowerW / AbsWheelSpeedMpS;

                        //if (AbsSpeedMps > MaxSpeedMpS)
                        //    maxForceN = 0;
                        if (AbsSpeedMpS > MaxSpeedMpS - 0.05f)
                            maxForceN = 20 * (MaxSpeedMpS - AbsSpeedMpS) * maxForceN;
                        if (AbsSpeedMpS > (MaxSpeedMpS))
                            maxForceN = 0;
                        MotiveForceN = maxForceN;
                    }
                }
                else
                {
                    if (throttleNum > (DieselEngines.MaxOutputPowerW / DieselEngines.MaxPowerW))
                        throttleNum = (DieselEngines.MaxOutputPowerW / DieselEngines.MaxPowerW);
                    MotiveForceN = TractiveForceCurves.Get(throttleNum, AbsWheelSpeedMpS);
                    if (MotiveForceN < 0)
                        MotiveForceN = 0;
                }
                //if (t == 0)
                //    DieselFlowLps = DieselUsedPerHourAtIdleL / 3600.0f;
                //else
                //    DieselFlowLps = ((DieselUsedPerHourAtMaxPowerL - DieselUsedPerHourAtIdleL) * t + DieselUsedPerHourAtIdleL) / 3600.0f;
                DieselFlowLps = DieselEngines.DieselFlowLps;
                DieselLevelL -= DieselEngines.DieselFlowLps * elapsedClockSeconds;
                if (DieselLevelL <= 0.0f)
                {
                    PowerOn = false;
                    SignalEvent(Event.EnginePowerOff);
                }
                MassKG = InitialMassKg - MaxDieselLevelL * DieselWeightKgpL + DieselLevelL * DieselWeightKgpL;
            }

            if (DynamicBrakePercent > 0 && DynamicBrakeForceCurves != null)
            {
                float f = DynamicBrakeForceCurves.Get(.01f * DynamicBrakePercent, AbsWheelSpeedMpS);
                if (f > 0)
                {
                    MotiveForceN -= (SpeedMpS > 0 ? 1 : -1) * f;
                    switch (Direction)
                    {
                        case Direction.Forward:
                            //MotiveForceN *= 1;     //Not necessary
                            break;
                        case Direction.Reverse:
                            MotiveForceN *= -1;
                            break;
                        case Direction.N:
                        default:
                            MotiveForceN *= 0;
                            break;
                    }
                }
                //if (Flipped)
                //    MotiveForceN *= -1f;
            }

            if (MaxForceN > 0 && MaxContinuousForceN > 0)
            {
                MotiveForceN *= 1 - (MaxForceN - MaxContinuousForceN) / (MaxForceN * MaxContinuousForceN) * AverageForceN;
                float w = (ContinuousForceTimeFactor - elapsedClockSeconds) / ContinuousForceTimeFactor;
                if (w < 0)
                    w = 0;
                AverageForceN = w * AverageForceN + (1 - w) * MotiveForceN;
            }

            if (Train.IsPlayerDriven)
            {
                if (this.IsLeadLocomotive())
                {
                    switch (Direction)
                    {
                        case Direction.Forward:
                            //MotiveForceN *= 1;     //Not necessary
                            break;
                        case Direction.Reverse:
                            MotiveForceN *= -1;
                            break;
                        case Direction.N:
                        default:
                            MotiveForceN *= 0;
                            break;
                    }
                    ConfirmWheelslip( elapsedClockSeconds );
                }
                else
                {
                    // When not LeadLocomotive; check if lead is in Neutral
                    // if so this loco will have no motive force

                    var LeadLocomotive = Simulator.PlayerLocomotive;

                    if (LeadLocomotive == null) { }
                    else if (LeadLocomotive.Direction == Direction.N)
                        MotiveForceN *= 0;
                    else
                    {
                        switch (Direction)
                        {
                            case Direction.Forward:
                                MotiveForceN *= 1;     //Not necessary
                                break;
                            case Direction.Reverse:
                                MotiveForceN *= -1;
                                break;
                            case Direction.N:
                            default:
                                MotiveForceN *= 0;
                                break;
                        }
                    }
                } // end when not lead loco
            }// end player locomotive

            else // for AI locomotives
            {
                foreach (DieselEngine de in DieselEngines)
                    de.Start();
                switch (Direction)
                {
                    case Direction.Reverse:
                        MotiveForceN *= -1;
                        break;
                    default:
                        break;
                }
            }// end AI locomotive

            switch (this.Train.TrainType)
            {
                case Train.TRAINTYPE.AI:
                case Train.TRAINTYPE.AI_PLAYERHOSTING:
                    if (!PowerOn)
                        PowerOn = true;
                    //LimitMotiveForce(elapsedClockSeconds);    //calls the advanced physics
                    LimitMotiveForce();                         //let's call the basic physics instead for now
                    if (Train.IsActualPlayerTrain) FilteredMotiveForceN = CurrentFilter.Filter(MotiveForceN, elapsedClockSeconds);
                    WheelSpeedMpS = Flipped ? -AbsSpeedMpS : AbsSpeedMpS;            //make the wheels go round
                    break;
                case Train.TRAINTYPE.STATIC:
                case Train.TRAINTYPE.INTENDED_PLAYER:
                    break;
                case Train.TRAINTYPE.PLAYER:
                case Train.TRAINTYPE.AI_PLAYERDRIVEN:
                case Train.TRAINTYPE.REMOTE:
                    // For notched throttle controls (e.g. Dash 9 found on Marias Pass) UpdateValue is always 0.0
                    if (ThrottleController.UpdateValue != 0.0)
                    {
                        Simulator.Confirmer.UpdateWithPerCent(
                            CabControl.Throttle,
                            ThrottleController.UpdateValue > 0 ? CabSetting.Increase : CabSetting.Decrease,
                            ThrottleController.CurrentValue * 100);
                    }
                    if (DynamicBrakeController != null && DynamicBrakeController.UpdateValue != 0.0)
                    {
                        Simulator.Confirmer.UpdateWithPerCent(
                            CabControl.DynamicBrake,
                            DynamicBrakeController.UpdateValue > 0 ? CabSetting.Increase : CabSetting.Decrease,
                            DynamicBrakeController.CurrentValue * 100);
                    }

                    //Force is filtered due to inductance
                    if (elapsedClockSeconds > 0)
                    {
                        FilteredMotiveForceN = CurrentFilter.Filter(MotiveForceN, elapsedClockSeconds);
                        MotiveForceN = FilteredMotiveForceN;
                    }

                    LimitMotiveForce(elapsedClockSeconds);

                    if (WheelslipCausesThrottleDown && WheelSlip)
                        ThrottleController.SetValue(0.0f);
                    break;
                default:
                    break;

            }

            EngineRPMRatio = (DieselEngines[0].RealRPM - DieselEngines[0].IdleRPM) / (DieselEngines[0].MaxRPM - DieselEngines[0].IdleRPM);

            Variable1 = ThrottlePercent / 100.0f; 
            // else Variable1 = MotiveForceN / MaxForceN; // Gearbased, Variable1 proportional to motive force
            // allows for motor volume proportional to effort.

            // Refined Variable2 setting to graduate
            if (Variable2 != EngineRPMRatio)
            {
                // We must avoid Variable2 to run outside of [0, 1] range, even temporarily (because of multithreading)
                Variable2 = EngineRPMRatio < Variable2 ?
                    Math.Max(Math.Max(Variable2 - elapsedClockSeconds * PercentChangePerSec, EngineRPMRatio), 0) :
                    Math.Min(Math.Min(Variable2 + elapsedClockSeconds * PercentChangePerSec, EngineRPMRatio), 1);
            }

            EngineRPM = Variable2 * (MaxRPM - IdleRPM) + IdleRPM;

            if (DynamicBrakePercent > 0)
            {
                if (MaxDynamicBrakeForceN == 0)
                    Variable3 = DynamicBrakePercent / 100f;
                else
                    Variable3 = Math.Abs(MotiveForceN) / MaxDynamicBrakeForceN;
            }
            else
                Variable3 = 0;

            if (elapsedClockSeconds > 0.0f)
            {
                EngineRPMderivation = (EngineRPM - EngineRPMold)/elapsedClockSeconds;
                EngineRPMold = EngineRPM;
            }

            if (MainResPressurePSI < CompressorRestartPressurePSI && AuxPowerOn && !CompressorIsOn)
                SignalEvent(Event.CompressorOn);
            else if ((MainResPressurePSI > MaxMainResPressurePSI || !AuxPowerOn) && CompressorIsOn)
                SignalEvent(Event.CompressorOff);
            if (CompressorIsOn)
                MainResPressurePSI += elapsedClockSeconds * MainResChargingRatePSIpS;
            
            if (Train.IsPlayerDriven && this.IsLeadLocomotive())
                TrainControlSystem.Update();

            FuelController.Update(elapsedClockSeconds);
            if (FuelController.UpdateValue > 0.0)
                Simulator.Confirmer.UpdateWithPerCent(CabControl.DieselFuel, CabSetting.Increase, FuelController.CurrentValue * 100);

            PrevMotiveForceN = MotiveForceN;
            base.UpdateParent(elapsedClockSeconds); // Calls the Update() method in the parent class MSTSLocomotive which calls Update() on its parent MSTSWagon which calls ...
        }

        public override void ChangeGearUp()
        {
            if (DieselEngines[0].GearBox != null)
            {
                if (DieselEngines[0].GearBox.GearBoxOperation == GearBoxOperation.Semiautomatic)
                {
                    DieselEngines[0].GearBox.AutoGearUp();
                    GearBoxController.SetValue((float)DieselEngines[0].GearBox.NextGearIndex);
                }
            }
        }

        public override void ChangeGearDown()
        {
            
                if (DieselEngines[0].GearBox != null)
                {
                    if (DieselEngines[0].GearBox.GearBoxOperation == GearBoxOperation.Semiautomatic)
                    {
                        DieselEngines[0].GearBox.AutoGearDown();
                        GearBoxController.SetValue((float)DieselEngines[0].GearBox.NextGearIndex);
                    }
                }
        }
       
        public override float GetDataOf(CabViewControl cvc)
        {
            float data = 0;

            switch (cvc.ControlType)
            {
                case CABViewControlTypes.GEARS:
                    if (DieselEngines.HasGearBox)
                        data = DieselEngines[0].GearBox.CurrentGearIndex + 1;
                    break;
                case CABViewControlTypes.FUEL_GAUGE:
                    if (cvc.Units == CABViewControlUnits.GALLONS)
                        data = L.ToGUS(DieselLevelL);
                    else
                        data = DieselLevelL;
                    break;
                default:
                    data = base.GetDataOf(cvc);
                    break;
            }

            return data;
        }

        public override string GetStatus()
        {
            var status = new StringBuilder();
            status.AppendFormat("{0} = {1}\n", Simulator.Catalog.GetString("Engine"),
                Simulator.Catalog.GetParticularString("Engine", GetStringAttribute.GetPrettyName(DieselEngines[0].EngineStatus)));

            if (DieselEngines.HasGearBox)
                status.AppendFormat("{0} = {1}\n", Simulator.Catalog.GetString("Gear"), DieselEngines[0].GearBox.CurrentGearIndex < 0
                    ? Simulator.Catalog.GetParticularString("Gear", "N")
                    : (DieselEngines[0].GearBox.CurrentGearIndex + 1).ToString());

            return status.ToString();
        }

        public override string GetDebugStatus()
        {
            var status = new StringBuilder(base.GetDebugStatus());

            if (DieselEngines.HasGearBox)
                status.AppendFormat("\t{0} {1}", Simulator.Catalog.GetString("Gear"), DieselEngines[0].GearBox.CurrentGearIndex);
            status.AppendFormat("\t{0} {1}\t\t\t{2}", Simulator.Catalog.GetString("Fuel"), FormatStrings.FormatFuelVolume(DieselLevelL, IsMetric, IsUK), DieselEngines.GetStatus());
            return status.ToString();
        }

        /// <summary>
        /// Catch the signal to start or stop the diesel
        /// </summary>
        public void StartStopDiesel()
        {
            if (!this.IsLeadLocomotive() && (this.ThrottlePercent == 0))
                PowerOn = !PowerOn;
        }

        public override void SetPower(bool ToState)
        {
            if (ToState)
            {
                foreach (DieselEngine engine in DieselEngines)
                    engine.Start();
                SignalEvent(Event.EnginePowerOn);
            }
            else
            {
                foreach (DieselEngine engine in DieselEngines)
                    engine.Stop();
                SignalEvent(Event.EnginePowerOff);
            }

            base.SetPower(ToState);
        }

        /// <summary>
        /// Returns the controller which refills from the matching pickup point.
        /// </summary>
        /// <param name="type">Pickup type</param>
        /// <returns>Matching controller or null</returns>
        public override MSTSNotchController GetRefillController(uint type)
        {
            MSTSNotchController controller = null;
            if (type == (uint)PickupType.FuelDiesel) return FuelController;
            return controller;
        }

        /// <summary>
        /// Sets coal and water supplies to full immediately.
        /// Provided in case route lacks pickup points for diesel oil.
        /// </summary>
        public override void RefillImmediately()
        {
            FuelController.CurrentValue = 1.0f;
        }

        /// <summary>
        /// Returns the fraction of diesel oil already in tank.
        /// </summary>
        /// <param name="pickupType">Pickup type</param>
        /// <returns>0.0 to 1.0. If type is unknown, returns 0.0</returns>
        public override float GetFilledFraction(uint pickupType)
        {
            if (pickupType == (uint)PickupType.FuelDiesel)
            {
                return FuelController.CurrentValue;
            }
            return 0f;
        }

       /// <summary>
        /// Restores the type of gearbox, that was forced to
        /// automatic for AI trains
        /// </summary>
        public override void SwitchToPlayerControl()
        {
            foreach (DieselEngine de in DieselEngines)
            {
                if (de.GearBox != null)
                    de.GearBox.GearBoxOperation = de.GearBox.OriginalGearBoxOperation;
            }
            if (DieselEngines[0].GearBox != null && GearBoxController != null)
            {
                GearBoxController.CurrentNotch = DieselEngines[0].GearBox.CurrentGearIndex + 1;
                GearboxGearIndex = DieselEngines[0].GearBox.CurrentGearIndex + 1;
                GearBoxController.SetValue((float)GearBoxController.CurrentNotch);
            }

        }
    } // class DieselLocomotive
}
