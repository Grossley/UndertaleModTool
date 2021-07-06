using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using UndertaleModLib;
using UndertaleModLib.Scripting;
using System.Security.Cryptography;
using UndertaleModLib.Models;
using UndertaleModLib.Decompiler;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System.Reflection;
using Newtonsoft.Json;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System.IO.Pipes;
using System.Windows.Forms;
using UndertaleModLib;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleSound;
using static UndertaleModLib.UndertaleData;
using System.Collections.Generic;
using UndertaleModLib.Compiler;

namespace UndertaleModTool
{
    // Adding misc. scripting functions here
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        public void SoundCopy()
        {
            EnsureDataLoaded();

            if ((Data.AudioGroups.ByName("audiogroup_default") == null) && Data.GeneralInfo.Major >= 2)
            {
                throw new Exception("Currently loaded data file has no \"audiogroup_default\" but it is GMS2 or greater. AudioGroups count: " + Data.AudioGroups.Count.ToString());
            }
            UndertaleData DonorData = LoadDonorDataFile();
            if (DonorData == null)
            {
                ScriptError("Donor data file does not exist!");
                return;
            }
            if ((DonorData.AudioGroups.ByName("audiogroup_default") == null) && DonorData.GeneralInfo.Major >= 2)
            {
                throw new Exception("This donor data file has no \"audiogroup_default\" but it is GMS2 or greater. AudioGroups count: " + DonorData.AudioGroups.Count.ToString());
            }
            List<string> splitStringsList = GetSplitStringsList("sound");
            List<UndertaleSound> soundsList = GetSoundsList(splitStringsList, DonorData);
            foreach (UndertaleSound snd in soundsList)
            {
                UndertaleSound nativeSND = Data.Sounds.ByName(snd.Name.Content);
                UndertaleSound donorSND = DonorData.Sounds.ByName(snd.Name.Content);
                if (nativeSND == null)
                {
                    nativeSND = new UndertaleSound();
                    nativeSND.Name = Data.Strings.MakeString(donorSND.Name.Content);
                    Data.Sounds.Add(nativeSND);
                }
                if (donorSND.Name != null)
                    nativeSND.Name = Data.Strings.MakeString(donorSND.Name.Content);
                if (donorSND.Type != null)
                    nativeSND.Type = Data.Strings.MakeString(donorSND.Type.Content);
                if (donorSND.File != null)
                    nativeSND.File = Data.Strings.MakeString(donorSND.File.Content);
                nativeSND.Flags = donorSND.Flags;
                if (donorSND.Effects != null)
                    nativeSND.Effects = donorSND.Effects;
                if (donorSND.Volume != null)
                    nativeSND.Volume = donorSND.Volume;
                if (donorSND.Preload != null)
                    nativeSND.Preload = donorSND.Preload;
                if (donorSND.Pitch != null)
                    nativeSND.Pitch = donorSND.Pitch;
                HandleAudioGroups(donorSND, nativeSND);
            }
        }
        public void ProcessAllSoundsToUseAudioGroupDefault()
        {
            var newAudioGroup = new UndertaleAudioGroup();
            newAudioGroup.Name = Data.Strings.MakeString("audiogroup_default");
            Data.AudioGroups.Add(newAudioGroup);
            for (var i = 0; i < Data.Sounds.Count; i++)
            {
                Data.Sounds[i].AudioGroup = newAudioGroup;
            }
        }
        public void HandleAudioGroups(UndertaleSound donorSND, UndertaleSound nativeSND)
        {
            if (!Data.FORM.Chunks.ContainsKey("AGRP")) // No way to add
            {
                return;
            }
            if (donorSND.AudioGroup != null)
            {
                UndertaleAudioGroup audoToGive = Data.AudioGroups.ByName(donorSND.AudioGroup.Name.Content);
                if (audoToGive == null)
                {
                    if (!(donorSND.AudioGroup.Name.Content == "audiogroup_default"))
                    {
                        if (Data.AudioGroups.ByName("audiogroup_default") == null && Data.AudioGroups.Count == 0)
                        {
                            if (ScriptQuestion("You are trying to add a non-default audio group but no audiogroups exist yet. Move all sounds into the default audio group and create a new audio group?"))
                                ProcessAllSoundsToUseAudioGroupDefault();
                            else
                                return;
                        }
                        else if (Data.AudioGroups.ByName("audiogroup_default") == null)
                        {
                            throw new Exception("Count is non-zero but audiogroup_default does not exist.");
                        }
                        File.WriteAllBytes(Path.Combine(GetFolder(FilePath), "audiogroup" + Data.AudioGroups.Count.ToString() + ".dat"), Convert.FromBase64String("Rk9STQwAAABBVURPBAAAAAAAAAA="));
                    }
                    var newAudioGroup = new UndertaleAudioGroup();
                    newAudioGroup.Name = Data.Strings.MakeString(donorSND.AudioGroup.Name.Content);
                    Data.AudioGroups.Add(newAudioGroup);
                }
                else
                    nativeSND.AudioGroup = audoToGive;
            }
            return;
        }
    }
}
