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
    }
}
