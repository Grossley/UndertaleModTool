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


namespace UndertaleModTool
{
    // Adding misc. scripting functions here
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        public bool SendAUMIMessage(IpcMessage_t ipMessage, ref IpcReply_t outReply)
        {
            // By Archie
            const int ReplySize = 132;

            // Create the pipe
            using var pPipeServer = new NamedPipeServerStream("AUMI-IPC", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            // Wait 1/8th of a second for AUMI to connect.
            // If it doesn't connect in time (which it should), just return false to avoid a deadlock.
            if (!pPipeServer.IsConnected)
            {
                pPipeServer.WaitForConnectionAsync();
                Thread.Sleep(125);
                if (!pPipeServer.IsConnected)
                {
                    pPipeServer.DisposeAsync();
                    return false;
                }
            }

            try
            {
                //Send the message
                pPipeServer.Write(ipMessage.RawBytes());
                pPipeServer.Flush();
            }
            catch (Exception e)
            {
                // Catch any errors that might arise if the connection is broken
                ScriptError("Could not write data to the pipe!\nError: " + e.Message);
                return false;
            }

            // Read the reply, the length of which is always a pre-set amount of bytes.
            byte[] bBuffer = new byte[ReplySize];
            pPipeServer.Read(bBuffer, 0, ReplySize);

            outReply = IpcReply_t.FromBytes(bBuffer);
            return true;
        }

        public void SoundCopy()
        {
            EnsureDataLoaded();

            string GetFolder(string path)
            {
                return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
            }

            int audioID = -1;
            int audioGroupID = -1;
            int embAudioID = -1;
            bool usesAGRP = (Data.AudioGroups.Count > 0);

            // Check code directory.
            string importFolder = PromptChooseDirectory("Import From Where");
            if (importFolder == null)
                throw new System.Exception("The import folder was not set.");

            bool isOGG = Path.GetExtension(fname) == ".ogg";
            string AGRPname = "";
            bool needAGRP = false;
            bool soundExists = (Data.Sounds.ByName(sound_name) != null);
            UndertaleSound existing_snd = Data.Sounds.ByName(sound_name);

            if (soundExists)
                audioGroupID = Data.Sounds.ByName(sound_name).GroupID;
            else if (usesAGRP && !isOGG)
            {
                needAGRP = true;
                UndertaleAudioGroup newAudioGroup = (Data.AudioGroups.ByName(AGRPname));
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

            if ((!isOGG && !needAGRP) || (needAGRP))
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

            UndertaleSound.AudioEntryFlags flags = UndertaleSound.AudioEntryFlags.Regular;

            flags = UndertaleSound.AudioEntryFlags.Regular;
            audioID = -1;

            UndertaleEmbeddedAudio RaudioFile = null;
            RaudioFile = (isOGG ? null : (needAGRP ? null : Data.EmbeddedAudio[embAudioID]));
            UndertaleAudioGroup groupID = null;
            groupID = ((!usesAGRP) ? null: (needAGRP ? Data.AudioGroups[audioGroupID] : Data.AudioGroups[Data.GetBuiltinSoundGroupID()]));

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
            snd_to_add.Type = (isOGG ? Data.Strings.MakeString(".ogg") : Data.Strings.MakeString(".wav"));
            snd_to_add.File = Data.Strings.MakeString(fname);
            snd_to_add.Effects = 0;
            snd_to_add.Volume = 1.0F;
            snd_to_add.Pitch = 1.0F;
            snd_to_add.AudioID = audioID;
            snd_to_add.AudioFile = RaudioFile;
            snd_to_add.AudioGroup = groupID;
            snd_to_add.GroupID = (needAGRP ? audioGroupID : Data.GetBuiltinSoundGroupID());
            ChangeSelection(snd_to_add);
        }
        public bool RunUMTScript(string path)
        {
            // By Grossley
            if (!File.Exists(path))
            {
                ScriptError(path + " does not exist!");
                return false;
            }
            RunScript(path);
            if (!ScriptExecutionSuccess)
                ScriptError("An error of type \"" + ScriptErrorType + "\" occurred. The error is:\n\n" + ScriptErrorMessage, ScriptErrorType);
            return ScriptExecutionSuccess;
        }
        public bool LintUMTScript(string path)
        {
            // By Grossley
            if (!File.Exists(path))
            {
                ScriptError(path + " does not exist!");
                return false;
            }
            try
            {
                CancellationTokenSource source = new CancellationTokenSource(100);
                CancellationToken token = source.Token;
                object test = CSharpScript.EvaluateAsync(File.ReadAllText(path), scriptOptions, this, typeof(IScriptInterface), token);
            }
            catch (CompilationErrorException exc)
            {
                ScriptError(exc.Message, "Script compile error");
                ScriptExecutionSuccess = false;
                ScriptErrorMessage = exc.Message;
                ScriptErrorType = "CompilationErrorException";
                return false;
            }
            catch (Exception)
            {
                // Using the 100 MS timer it can time out before successfully running, compilation errors are fast enough to get through.
                ScriptExecutionSuccess = true;
                ScriptErrorMessage = "";
                ScriptErrorType = "";
                return true;
            }
            return true;
        }
    }
}
