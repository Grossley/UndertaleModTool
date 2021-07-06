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
        public void SoundCopyInternal()
        {
        }

        public UndertaleData LoadDonorDataFile()
        {
            ScriptMessage("Select the file to copy from");

            UndertaleData DonorData = null;
            string DonorDataPath = PromptLoadFile(null, null);
            if (DonorDataPath == null)
                throw new System.Exception("The donor data path was not set.");

            using (var stream = new FileStream(DonorDataPath, FileMode.Open, FileAccess.Read))
                DonorData = UndertaleIO.Read(stream, warning => ScriptMessage("A warning occured while trying to load " + DonorDataPath + ":\n" + warning));
            return DonorData;

        }
        public List<string> GetSplitStringsList(string assetType)
        {
            ScriptMessage("Enter the object(s) to copy");

            List<string> splitStringsList = new List<string>();
            string abc123 = "";
            abc123 = SimpleTextInput("Menu", "Enter name(s) of game objects", abc123, true);
            string[] subs = abc123.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var sub in subs)
            {
                splitStringsList.Add(sub);
            }
            return splitStringsList;
        }
        public List<UndertaleSound> GetSoundsList(List<string> splitStringsList, UndertaleData DonorData)
        {
            List<UndertaleSound> soundsList = new List<UndertaleSound>();
            for (var j = 0; j < splitStringsList.Count; j++)
            {
                foreach (UndertaleSound snd in DonorData.Sounds)
                {
                    if (splitStringsList[j].ToLower() == snd.Name.Content.ToLower())
                    {
                        soundsList.Add(snd);
                    }
                }
            }
            return soundsList;
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
        public string GetFolder(string path)
        {
            return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
        }

        public byte[] GetSoundData(UndertaleSound sound, UndertaleData dataToOperateOn)
        {
            if (sound.AudioFile != null)
                return sound.AudioFile.Data;

            if (sound.GroupID > dataToOperateOn.GetBuiltinSoundGroupID())
            {
                IList<UndertaleEmbeddedAudio> audioGroup = GetAudioGroupData(sound);
                if (audioGroup != null)
                    return audioGroup[sound.AudioID].Data;
            }
            return null;
        }
        public IList<UndertaleEmbeddedAudio> GetAudioGroupData(UndertaleSound sound)
        {
            Dictionary<string, IList<UndertaleEmbeddedAudio>> loadedAudioGroups = null;
            if (loadedAudioGroups == null)
                loadedAudioGroups = new Dictionary<string, IList<UndertaleEmbeddedAudio>>();

            string audioGroupName = sound.AudioGroup != null ? sound.AudioGroup.Name.Content : null;
            if (loadedAudioGroups.ContainsKey(audioGroupName))
                return loadedAudioGroups[audioGroupName];

            string groupFilePath = winFolder + "audiogroup" + sound.GroupID + ".dat";
            if (!File.Exists(groupFilePath))
                return null; // Doesn't exist.

            try
            {
                UndertaleData data = null;
                using (var stream = new FileStream(groupFilePath, FileMode.Open, FileAccess.Read))
                    data = UndertaleIO.Read(stream, warning => ScriptMessage("A warning occured while trying to load " + audioGroupName + ":\n" + warning));

                loadedAudioGroups[audioGroupName] = data.EmbeddedAudio;
                return data.EmbeddedAudio;
            }
            catch (Exception e)
            {
                ScriptMessage("An error occured while trying to load " + audioGroupName + ":\n" + e.Message);
                return null;
            }
        }

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
/*
            int copiedGameObjectsCount = 0;

            int audioID = -1;
            int audioGroupID = -1;
            int embAudioID = -1;
            bool usesAGRP = Data.AudioGroups.Count > 0;

            // Check code directory.
            string importFolder = PromptChooseDirectory("Import From Where");
            if (importFolder == null)
                throw new Exception("The import folder was not set.");

            //Comment these out when you implement proper replacement
            string sound_name = "";
            string fname = "";
            bool isOGG = Path.GetExtension(fname) == ".ogg";
            string AGRPname = "";
            bool needAGRP = false;
            bool soundExists = Data.Sounds.ByName(sound_name) != null;
            UndertaleSound existing_snd = Data.Sounds.ByName(sound_name);

            if (soundExists)
                audioGroupID = Data.Sounds.ByName(sound_name).GroupID;
            else if (usesAGRP && !isOGG)
            {
                needAGRP = true;
                UndertaleAudioGroup newAudioGroup = Data.AudioGroups.ByName(AGRPname);
                if (newAudioGroup != null)
                    audioGroupID = Data.AudioGroups.IndexOf(newAudioGroup);
                else
                {
                    File.WriteAllBytes(GetFolder(FilePath) + "audiogroup" + Data.AudioGroups.Count + ".dat", Convert.FromBase64String("Rk9STQwAAABBVURPBAAAAAAAAAA="));
                    newAudioGroup = new UndertaleAudioGroup();
                    newAudioGroup.Name = Data.Strings.MakeString(AGRPname);
                    Data.AudioGroups.Add(newAudioGroup);
                }
            }
            if (audioGroupID == 0) //If the audiogroup is zero then 
                needAGRP = false;

            UndertaleEmbeddedAudio soundData = null;

            if ((!isOGG && !needAGRP) || needAGRP)
            {
                soundData = new UndertaleEmbeddedAudio();
                soundData.Data = File.ReadAllBytes(Path.Combine(importFolder, fname));
                Data.EmbeddedAudio.Add(soundData);
                if (soundExists)
                    Data.EmbeddedAudio.Remove(existing_snd.AudioFile);
                embAudioID = Data.EmbeddedAudio.Count - 1;
            }
            if (needAGRP)
            {
                var audioGroupReadStream = new FileStream(GetFolder(FilePath) + "audiogroup" + audioGroupID.ToString() + ".dat", FileMode.Open, FileAccess.Read);
                UndertaleData audioGroupDat = UndertaleIO.Read(audioGroupReadStream);
                audioGroupReadStream.Dispose();
                audioGroupDat.EmbeddedAudio.Add(soundData);
                if (soundExists)
                    audioGroupDat.EmbeddedAudio.Remove(existing_snd.AudioFile);
                audioID = audioGroupDat.EmbeddedAudio.Count - 1;
                var audioGroupWriteStream = new FileStream(GetFolder(FilePath) + "audiogroup" + audioGroupID.ToString() + ".dat", FileMode.Create);
                UndertaleIO.Write(audioGroupWriteStream, audioGroupDat);
                audioGroupWriteStream.Dispose();
            }

            AudioEntryFlags flags = AudioEntryFlags.Regular;

            flags = AudioEntryFlags.Regular;
            audioID = -1;

            UndertaleEmbeddedAudio RaudioFile = null;
            RaudioFile = isOGG ? null : (needAGRP ? null : Data.EmbeddedAudio[embAudioID]);
            UndertaleAudioGroup groupID = null;
            groupID = (!usesAGRP) ? null: (needAGRP ? Data.AudioGroups[audioGroupID] : Data.AudioGroups[Data.GetBuiltinSoundGroupID()]);

            UndertaleSound snd_to_add;
            if (!soundExists)
            {
                snd_to_add = new UndertaleSound();
                Data.Sounds.Add(snd_to_add);
            }
            else
                snd_to_add = Data.Sounds.ByName(sound_name);
            snd_to_add.Name = Data.Strings.MakeString(sound_name);
            snd_to_add.Flags = flags;
            snd_to_add.Type = isOGG ? Data.Strings.MakeString(".ogg") : Data.Strings.MakeString(".wav");
            snd_to_add.File = Data.Strings.MakeString(fname);
            snd_to_add.Effects = 0;
            snd_to_add.Volume = 1.0F;
            snd_to_add.Pitch = 1.0F;
            snd_to_add.AudioID = audioID;
            snd_to_add.AudioFile = RaudioFile;
            snd_to_add.AudioGroup = groupID;
            snd_to_add.GroupID = needAGRP ? audioGroupID : Data.GetBuiltinSoundGroupID();
            ChangeSelection(snd_to_add);
*/
        }
    }
}
