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

/// This module parses the sigcfg file and builds an object model based on signal details
/// 
/// Author: Laurie Heath
/// Updates : Rob Roeterdink
/// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MSTS.Parsers;
using ORTS.Common;

namespace MSTS.Formats
{
    public class SIGCFGFile
    {
        public IDictionary<string, LightTexture> LightTextures;
        public IDictionary<string, LightTableEntry> LightsTable;
        public IDictionary<string, SignalType> SignalTypes;
        public IDictionary<string, SignalShape> SignalShapes;
        public IList<string> ScriptFiles;

        public SIGCFGFile(string filenamewithpath)
        {
            using (STFReader stf = new STFReader(filenamewithpath, false))
                stf.ParseFile(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("lighttextures", ()=>{ LightTextures = ReadLightTextures(stf); }),
                    new STFReader.TokenProcessor("lightstab", ()=>{ LightsTable = ReadLightsTable(stf); }),
                    new STFReader.TokenProcessor("signaltypes", ()=>{ SignalTypes = ReadSignalTypes(stf); }),
                    new STFReader.TokenProcessor("signalshapes", ()=>{ SignalShapes = ReadSignalShapes(stf); }),
                    new STFReader.TokenProcessor("scriptfiles", ()=>{ ScriptFiles = ReadScriptFiles(stf); }),
                });
            Initialize<Dictionary<string, LightTexture>, IDictionary<string, LightTexture>>(ref LightTextures, "LightTextures", filenamewithpath);
            Initialize<Dictionary<string, LightTableEntry>, IDictionary<string, LightTableEntry>>(ref LightsTable, "LightsTab", filenamewithpath);
            Initialize<Dictionary<string, SignalType>, IDictionary<string, SignalType>>(ref SignalTypes, "SignalTypes", filenamewithpath);
            Initialize<Dictionary<string, SignalShape>, IDictionary<string, SignalShape>>(ref SignalShapes, "SignalShapes", filenamewithpath);
            Initialize<List<string>, IList<string>>(ref ScriptFiles, "ScriptFiles", filenamewithpath);
        }

        static void Initialize<T, U>(ref U field, string name, string file) where T : U, new()
        {
            if (field == null)
            {
                field = new T();
                Trace.TraceWarning("Ignored missing {1} in {0}", file, name);
            }
        }

        static IDictionary<string, LightTexture> ReadLightTextures(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            var lightTextures = new Dictionary<string, LightTexture>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("lighttex", ()=>{
                    if (lightTextures.Count >= count)
                        STFException.TraceWarning(stf, "Skipped extra LightTex");
                    else
                    {
                        var lightTexture = new LightTexture(stf);
                        if (lightTextures.ContainsKey(lightTexture.Name))
                            STFException.TraceWarning(stf, "Skipped duplicate LightTex " + lightTexture.Name);
                        else
                            lightTextures.Add(lightTexture.Name, lightTexture);
                    }
                }),
            });
            if (lightTextures.Count < count)
                STFException.TraceWarning(stf, (count - lightTextures.Count).ToString() + " missing LightTex(s)");
            return lightTextures;
        }

        static IDictionary<string, LightTableEntry> ReadLightsTable(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            var lightsTable = new Dictionary<string, LightTableEntry>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("lightstabentry", ()=>{
                    if (lightsTable.Count >= count)
                        STFException.TraceWarning(stf, "Skipped extra LightsTabEntry");
                    else
                    {
                        var lightsTableEntry = new LightTableEntry(stf);
                        if (lightsTable.ContainsKey(lightsTableEntry.Name))
                            STFException.TraceWarning(stf, "Skipped duplicate LightsTabEntry " + lightsTableEntry.Name);
                        else
                            lightsTable.Add(lightsTableEntry.Name, lightsTableEntry);
                    }
                }),
            });
            if (lightsTable.Count < count)
                STFException.TraceWarning(stf, (count - lightsTable.Count).ToString() + " missing LightsTabEntry(s)");
            return lightsTable;
        }

        static IDictionary<string, SignalType> ReadSignalTypes(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            var signalTypes = new Dictionary<string, SignalType>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signaltype", ()=>{
                    if (signalTypes.Count >= count)
                        STFException.TraceWarning(stf, "Skipped extra SignalType");
                    else
                    {
                        var signalType = new SignalType(stf);
                        if (signalTypes.ContainsKey(signalType.Name))
                            STFException.TraceWarning(stf, "Skipped duplicate SignalType " + signalType.Name);
                        else
                            signalTypes.Add(signalType.Name, signalType);
                    }
                }),
            });
            if (signalTypes.Count < count)
                STFException.TraceWarning(stf, (count - signalTypes.Count).ToString() + " missing SignalType(s)");
            return signalTypes;
        }

        static IDictionary<string, SignalShape> ReadSignalShapes(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            var signalShapes = new Dictionary<string, SignalShape>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signalshape", ()=>{
                        if (signalShapes.Count >= count)
                            STFException.TraceWarning(stf, "Skipped extra SignalShape");
                        else
                        {
                            var signalShape = new SignalShape(stf);
                            if (signalShapes.ContainsKey(signalShape.ShapeFileName))
                                STFException.TraceWarning(stf, "Skipped duplicate SignalShape " + signalShape.ShapeFileName);
                            else
                                signalShapes.Add(signalShape.ShapeFileName, signalShape);
                        }
                }),
            });
            if (signalShapes.Count < count)
                STFException.TraceWarning(stf, (count - signalShapes.Count).ToString() + " missing SignalShape(s)");
            return signalShapes;
        }

        static IList<string> ReadScriptFiles(STFReader stf)
        {
            stf.MustMatch("(");
            var scriptFiles = new List<string>();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("scriptfile", ()=>{ scriptFiles.Add(stf.ReadStringBlock(null)); }),
            });
            return scriptFiles;
        }
    }

    public class LightTexture
    {
        public readonly string Name, TextureFile;
        public readonly float u0, v0, u1, v1;

        public LightTexture(STFReader stf)
        {
            stf.MustMatch("(");
            Name = stf.ReadString().ToLowerInvariant();
            TextureFile = stf.ReadString();
            u0 = stf.ReadFloat(STFReader.UNITS.None, null);
            v0 = stf.ReadFloat(STFReader.UNITS.None, null);
            u1 = stf.ReadFloat(STFReader.UNITS.None, null);
            v1 = stf.ReadFloat(STFReader.UNITS.None, null);
            stf.SkipRestOfBlock();
        }
    }

    public class LightTableEntry
    {
        public readonly string Name;
        public byte a, r, g, b;   // colour

        public LightTableEntry(STFReader stf)
        {
            stf.MustMatch("(");
            Name = stf.ReadString().ToLowerInvariant();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("colour", ()=>{
				    stf.MustMatch("(");
                    a = (byte)stf.ReadUInt(null);
                    r = (byte)stf.ReadUInt(null);
                    g = (byte)stf.ReadUInt(null);
                    b = (byte)stf.ReadUInt(null);
                    stf.SkipRestOfBlock();
                }),
            });
        }
    }

    public class SignalType
    {
        public enum FnTypes
        {
            Normal,
            Distance,
            Repeater,
            Shunting,
            Info,
            Speed,
            Alert,
        }

        public readonly string Name;
        public FnTypes FnType;
        public bool Abs, NoGantry, Semaphore;  // Don't know what Abs is for but found in Marias Pass route
        public float FlashTimeOn = 1, FlashTimeOff = 1;  // On/Off duration for flashing light. (In seconds.)
        public string LightTextureName = "";
        public IList<SignalLight> Lights;
        public IDictionary<string, SignalDrawState> DrawStates;
        public IList<SignalAspect> Aspects;
        public int NumClearAhead_MSTS;
        public int NumClearAhead_ORTS;
        public float SemaphoreInfo = -1; //[Rob Roeterdink] default -1 as 0 is active value

        public SignalType(FnTypes reqType, MstsSignalAspect reqAspect)
        /// <summary>
        /// constructor for dummy entries
        /// </summary>
        {
            FnType = reqType;
            Name = "UNDEFINED";
            Semaphore = false;
            DrawStates = new Dictionary<string, SignalDrawState>();
            DrawStates.Add("CLEAR", new SignalDrawState("CLEAR", 1));
            Aspects = new List<SignalAspect>();
            Aspects.Add(new SignalAspect(reqAspect, "CLEAR"));
        }

        public SignalType(STFReader stf)
        {
            stf.MustMatch("(");
            Name = stf.ReadString().ToLowerInvariant();
            int numClearAhead = -2;
            int numdefs = 0;

            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signalfntype", ()=>{ FnType = ReadFnType(stf); }),  //[Rob Roeterdink] value was not passed
                new STFReader.TokenProcessor("signallighttex", ()=>{ LightTextureName = stf.ReadStringBlock("").ToLowerInvariant(); }),
                new STFReader.TokenProcessor("signallights", ()=>{ Lights = ReadLights(stf); }),
                new STFReader.TokenProcessor("signaldrawstates", ()=>{ DrawStates = ReadDrawStates(stf); }),
                new STFReader.TokenProcessor("signalaspects", ()=>{ Aspects = ReadAspects(stf); }),
                new STFReader.TokenProcessor("signalnumclearahead", ()=>{ numClearAhead = numClearAhead >= -1 ? numClearAhead : stf.ReadIntBlock(null); numdefs++;}),
                new STFReader.TokenProcessor("semaphoreinfo", ()=>{ SemaphoreInfo = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("sigflashduration", ()=>{
                    stf.MustMatch("(");
                    FlashTimeOn = stf.ReadFloat(STFReader.UNITS.None, null);
                    FlashTimeOff = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("signalflags", ()=>{
                    stf.MustMatch("(");
                    while (!stf.EndOfBlock())
                        switch (stf.ReadString().ToLower())
                        {
                            case "abs": Abs = true; break;
                            case "no_gantry": NoGantry = true; break;
                            case "semaphore": Semaphore = true; break;
                            default: stf.StepBackOneItem(); STFException.TraceInformation(stf, "Skipped unknown SignalType flag " + stf.ReadString()); break;
                        }
                }),
            });
            NumClearAhead_MSTS = numdefs == 1 ? numClearAhead : -2;
            NumClearAhead_ORTS = numdefs == 2 ? numClearAhead : -2;
        }

        static FnTypes ReadFnType(STFReader stf)
        {
            var type = stf.ReadStringBlock(null);
            try
            {
                return (FnTypes)Enum.Parse(typeof(FnTypes), type, true);
            }
            catch (ArgumentException)
            {
                STFException.TraceInformation(stf, "Skipped unknown SignalFnType " + type);
                return FnTypes.Info;
            }
        }

        static IList<SignalLight> ReadLights(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            var lights = new List<SignalLight>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signallight", ()=>{
                    if (lights.Count >= lights.Capacity)
                        STFException.TraceWarning(stf, "Skipped extra SignalLight");
                    else
                        lights.Add(new SignalLight(stf));
                }),
            });
            lights.Sort(SignalLight.Comparer);
            for (var i = 0; i < lights.Count; i++)
                if (lights[i].Index != i)
                    STFException.TraceWarning(stf, "Invalid SignalLight index; expected " + i + ", got " + lights[i].Index);
            return lights;
        }

        static IDictionary<string, SignalDrawState> ReadDrawStates(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            var drawStates = new Dictionary<string, SignalDrawState>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signaldrawstate", ()=>{
                    if (drawStates.Count >= count)
                        STFException.TraceWarning(stf, "Skipped extra SignalDrawState");
                    else
                    {
                        var drawState = new SignalDrawState(stf);
                        if (drawStates.ContainsKey(drawState.Name))
                        {
                            string TempNew = String.Copy("DST");
                            TempNew = String.Concat(TempNew,drawStates.Count.ToString());
                            drawStates.Add(TempNew, drawState);
                            STFException.TraceInformation(stf, "Duplicate SignalDrawState name \'"+drawState.Name+"\', using name \'"+TempNew+"\' instead");
                        }
                        else
                        {
                            drawStates.Add(drawState.Name, drawState);
                        }
                    }
                }),
            });
            if (drawStates.Count < count)
                STFException.TraceWarning(stf, (count - drawStates.Count).ToString() + " missing SignalDrawState(s)");
            return drawStates;
        }

        static IList<SignalAspect> ReadAspects(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            var aspects = new List<SignalAspect>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signalaspect", ()=>{
                    if (aspects.Count >= aspects.Capacity)
                        STFException.TraceWarning(stf, "Skipped extra SignalAspect");
                    else
                    {
                        var aspect = new SignalAspect(stf);
                        if (aspects.Any(sa => sa.Aspect == aspect.Aspect))
                            STFException.TraceWarning(stf, "Skipped duplicate SignalAspect " + aspect.Aspect);
                        else
                            aspects.Add(aspect);
                    }
                }),
            });
            return aspects;
        }

        /// <summary>
        /// This method returns the default draw state for the specified aspect or -1 if none.
        /// </summary>
        public int def_draw_state(MstsSignalAspect state)
        {
            for (int i = 0; i < Aspects.Count; i++)
            {
                if (state == Aspects[i].Aspect)
                {
                    return DrawStates[Aspects[i].DrawStateName].Index;
                }
            }
            return -1;
        }

        /// <summary>
        /// This method returns the next least restrictive aspect from the one specified.
        /// </summary>
        public MstsSignalAspect GetNextLeastRestrictiveState(MstsSignalAspect state)
        {
            MstsSignalAspect targetState = MstsSignalAspect.UNKNOWN;
            MstsSignalAspect leastState = MstsSignalAspect.STOP;

            for (int i = 0; i < Aspects.Count; i++)
            {
                if (Aspects[i].Aspect > leastState) leastState = Aspects[i].Aspect;
                if (Aspects[i].Aspect > state && Aspects[i].Aspect < targetState) targetState = Aspects[i].Aspect;
            }
            if (targetState == MstsSignalAspect.UNKNOWN) return leastState; else return targetState;
        }

        /// <summary>
        /// This method returns the most restrictive aspect for this signal type.
        /// </summary>
        public MstsSignalAspect GetMostRestrictiveAspect()
        {
            MstsSignalAspect targetAspect = MstsSignalAspect.UNKNOWN;
            for (int i = 0; i < Aspects.Count; i++)
            {
                if (Aspects[i].Aspect < targetAspect) targetAspect = Aspects[i].Aspect;
            }
            if (targetAspect == MstsSignalAspect.UNKNOWN) return MstsSignalAspect.STOP; else return targetAspect;
        }

        /// <summary>
        /// This method returns the least restrictive aspect for this signal type.
        /// [Rob Roeterdink] added for basic signals without script
        /// </summary>
        public MstsSignalAspect GetLeastRestrictiveAspect()
        {
            MstsSignalAspect targetAspect = MstsSignalAspect.STOP;
            for (int i = 0; i < Aspects.Count; i++)
            {
                if (Aspects[i].Aspect > targetAspect) targetAspect = Aspects[i].Aspect;
            }
            if (targetAspect > MstsSignalAspect.CLEAR_2) return MstsSignalAspect.CLEAR_2; else return targetAspect;
        }

        /// <summary>
        /// This method returns the lowest speed limit linked to the aspect
        /// </summary>
        public float GetSpeedLimitMpS(MstsSignalAspect aspect)
        {
            for (int i = 0; i < Aspects.Count; i++)
                if (Aspects[i].Aspect == aspect)
                    return Aspects[i].SpeedMpS;
            return -1;
        }

    }

    public class SignalLight
    {
        public readonly uint Index;
        public readonly string Name;
        public float X, Y, Z;      // position
        public float Radius;
        public bool SemaphoreChange;

        public SignalLight(STFReader stf)
        {
            stf.MustMatch("(");
            Index = stf.ReadUInt(null);
            Name = stf.ReadString().ToLowerInvariant();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("radius", ()=>{ Radius = stf.ReadFloatBlock(STFReader.UNITS.None, null); }),
                new STFReader.TokenProcessor("position", ()=>{
                    stf.MustMatch("(");
                    X = stf.ReadFloat(STFReader.UNITS.None, null);
                    Y = stf.ReadFloat(STFReader.UNITS.None, null);
                    Z = stf.ReadFloat(STFReader.UNITS.None, null);
                    stf.SkipRestOfBlock();
                }),
                new STFReader.TokenProcessor("signalflags", ()=>{
                    stf.MustMatch("(");
                    while (!stf.EndOfBlock())
                        switch (stf.ReadString().ToLower())
                        {
                            case "semaphore_change": SemaphoreChange = true; break;
                            default: stf.StepBackOneItem(); STFException.TraceInformation(stf, "Skipped unknown SignalLight flag " + stf.ReadString()); break;
                        }
                }),
            });
        }

        public static int Comparer(SignalLight lightA, SignalLight lightB)
        {
            return (int)lightA.Index - (int)lightB.Index;
        }
    }

    public class SignalDrawState
    {
        public readonly int Index;
        public readonly string Name;
        public IList<SignalDrawLight> DrawLights;
        public float SemaphorePos;

        public SignalDrawState(string reqName, int reqIndex)
        /// <summary>
        /// constructor for dummy entries
        /// </summary>
        {
            Index = reqIndex;
            Name = String.Copy(reqName);
            DrawLights = null;
        }

        public SignalDrawState(STFReader stf)
        {
            stf.MustMatch("(");
            Index = stf.ReadInt(null);
            Name = stf.ReadString().ToLowerInvariant();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("drawlights", ()=>{ DrawLights = ReadDrawLights(stf); }),
                new STFReader.TokenProcessor("semaphorepos", ()=>{ SemaphorePos = stf.ReadFloatBlock(STFReader.UNITS.None, 0); }),
            });
        }

        static IList<SignalDrawLight> ReadDrawLights(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            var drawLights = new List<SignalDrawLight>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("drawlight", ()=>{
                    if (drawLights.Count >= drawLights.Capacity)
                        STFException.TraceWarning(stf, "Skipped extra DrawLight");
                    else
                        drawLights.Add(new SignalDrawLight(stf));
                }),
            });
            return drawLights;
        }

        public static int Comparer(SignalDrawState drawStateA, SignalDrawState drawStateB)
        {
            return (int)drawStateA.Index - (int)drawStateB.Index;
        }
    }

    public class SignalDrawLight
    {
        public readonly uint LightIndex;
        public bool Flashing;

        public SignalDrawLight(STFReader stf)
        {
            stf.MustMatch("(");
            LightIndex = stf.ReadUInt(null);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signalflags", ()=>{
                    stf.MustMatch("(");
                    while (!stf.EndOfBlock())
                        switch (stf.ReadString().ToLower())
                        {
                            case "flashing": Flashing = true; break;
                            default: stf.StepBackOneItem(); STFException.TraceInformation(stf, "Skipped unknown DrawLight flag " + stf.ReadString()); break;
                        }
                }),
            });
        }
    }

    public class SignalAspect
    {
        public readonly MstsSignalAspect Aspect;
        public readonly string DrawStateName;
        public float SpeedMpS;  // Speed limit for this aspect. -1 if track speed is to be used
        public bool Asap;  // Set to true if SignalFlags ASAP option specified

        public SignalAspect(MstsSignalAspect reqAspect, string reqName)
        /// <summary>
        /// constructor for dummy entries
        /// </summary>
        {
            Aspect = reqAspect;
            DrawStateName = String.Copy(reqName);
            SpeedMpS = -1;
            Asap = false;
        }

        public SignalAspect(STFReader stf)
        {
            SpeedMpS = -1;
            stf.MustMatch("(");
            string aspectName = stf.ReadString();
            try
            {
                Aspect = (MstsSignalAspect)Enum.Parse(typeof(MstsSignalAspect), aspectName, true);
            }
            catch (ArgumentException)
            {
                STFException.TraceInformation(stf, "Skipped unknown signal aspect " + aspectName);
                Aspect = MstsSignalAspect.UNKNOWN;
            }
            DrawStateName = stf.ReadString().ToLowerInvariant();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("speedmph", ()=>{ SpeedMpS = MpS.FromMpH(stf.ReadFloatBlock(STFReader.UNITS.None, 0)); }),
                new STFReader.TokenProcessor("speedkph", ()=>{ SpeedMpS = MpS.FromKpH(stf.ReadFloatBlock(STFReader.UNITS.None, 0)); }),
                new STFReader.TokenProcessor("signalflags", ()=>{
                    stf.MustMatch("(");
                    while (!stf.EndOfBlock())
                        switch (stf.ReadString().ToLower())
                        {
                            case "asap": Asap = true; break;
                            default: stf.StepBackOneItem(); STFException.TraceInformation(stf, "Skipped unknown DrawLight flag " + stf.ReadString()); break;
                        }
                }),
            });
        }
    }

    public class SignalShape
    {
        public string ShapeFileName, Description;
        public IList<SignalSubObj> SignalSubObjs;

        public SignalShape(STFReader stf)
        {
            stf.MustMatch("(");
            ShapeFileName = stf.ReadString().ToUpper();
            Description = stf.ReadString();
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signalsubobjs", ()=>{ SignalSubObjs = ReadSignalSubObjects(stf); }),
            });
        }

        static IList<SignalSubObj> ReadSignalSubObjects(STFReader stf)
        {
            stf.MustMatch("(");
            int count = stf.ReadInt(null);
            var signalSubObjects = new List<SignalSubObj>(count);
            stf.ParseBlock(new STFReader.TokenProcessor[] {
                new STFReader.TokenProcessor("signalsubobj", ()=>{
                    if (signalSubObjects.Count >= count)
                        STFException.TraceWarning(stf, "Skipped extra SignalSubObj");
                    else
                    {
                        SignalSubObj signalSubObject = new SignalSubObj(stf);
                        if (signalSubObject.Index != signalSubObjects.Count)
                            STFException.TraceWarning(stf, "Invalid SignalSubObj index; expected " + signalSubObjects.Count + ", got " + signalSubObject.Index);
                        signalSubObjects.Add(signalSubObject);
                    }
                }),
            });
            if (signalSubObjects.Count < count)
                STFException.TraceWarning(stf, (count - signalSubObjects.Count).ToString() + " missing SignalSubObj(s)");
            return signalSubObjects;
        }

        public class SignalSubObj
        {
            public static IList<string> SignalSubTypes =
                    new[] {"DECOR","SIGNAL_HEAD","DUMMY1","DUMMY2",
				"NUMBER_PLATE","GRADIENT_PLATE","USER1","USER2","USER3","USER4"};
            // made public for access from SIGSCR processing
            // Altered to match definition in MSTS

            public readonly int Index;
            public readonly string MatrixName;        // Name of the group within the signal shape which defines this head
            public readonly string Description;      // 
            public int SignalSubType = -1;  // Signal sub type: -1 if not specified;
            public string SignalSubSignalType;
            public bool Optional;
            public bool Default;
            public bool BackFacing;
            public bool JunctionLink;

            public SignalSubObj(STFReader stf)
            {
                stf.MustMatch("(");
                Index = stf.ReadInt(null);
                MatrixName = stf.ReadString().ToUpper();
                Description = stf.ReadString();
                stf.ParseBlock(new STFReader.TokenProcessor[] {
                    new STFReader.TokenProcessor("sigsubtype", ()=>{ SignalSubType = SignalSubTypes.IndexOf(stf.ReadStringBlock(null).ToUpper()); }),
                    new STFReader.TokenProcessor("sigsubstype", ()=>{ SignalSubSignalType = stf.ReadStringBlock(null).ToLowerInvariant(); }),
                    new STFReader.TokenProcessor("signalflags", ()=>{
                        stf.MustMatch("(");
                        while (!stf.EndOfBlock())
                            switch (stf.ReadString().ToLower())
                            {
                                case "optional": Optional = true; break;
                                case "default": Default = true; break;
                                case "back_facing": BackFacing = true; break;
                                case "jn_link": JunctionLink = true; break;
                                default: stf.StepBackOneItem(); STFException.TraceInformation(stf, "Skipped unknown SignalSubObj flag " + stf.ReadString()); break;
                            }
                    }),
                });
            }
        }
    }
}