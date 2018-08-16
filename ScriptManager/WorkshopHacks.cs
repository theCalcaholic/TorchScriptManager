using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sandbox.Engine.Networking;
using Sandbox.Game.Localization;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using VRage.GameServices;
using NLog;
using VRage;
using VRage.FileSystem;

namespace ScriptManager
{
    public class WorkshopHacks
    {
        private static Logger Log = LogManager.GetLogger("ScriptManager");

        public static async Task<MyWorkshop.SubscribedItem> GetScriptInfoAsync(ulong workshopId)
        {
            var taskCompletionSrc = new TaskCompletionSource<MyWorkshop.SubscribedItem>();
            var task = taskCompletionSrc.Task;
            //Action<MyWorkshop.SubscribedItem> callback = taskCompletionSrc.SetResult;

            MyGameService.GetPublishedFileDetails(new ulong[] { workshopId }, (bool success, string data) =>
            {
                if (!success)
                {
                    taskCompletionSrc.SetException(new Exception($"Could not retrieve Details for script with id '{workshopId}'."));
                    return;
                }
                try
                {
                    XmlReaderSettings settings = new XmlReaderSettings() { DtdProcessing = DtdProcessing.Parse };
                    using (XmlReader xmlReader = XmlReader.Create((TextReader)new StringReader(data), settings))
                    {
                        xmlReader.ReadToFollowing("result");
                        MyGameServiceCallResult serviceCallResult1 = (MyGameServiceCallResult)xmlReader.ReadElementContentAsInt();
                        if (serviceCallResult1 != MyGameServiceCallResult.OK)
                        {
                            taskCompletionSrc.SetException(
                                new Exception($"Failed to download script: result = {serviceCallResult1}"));
                            return;
                        }

                        xmlReader.ReadToFollowing("resultcount");
                        int numScripts = xmlReader.ReadElementContentAsInt();
                        if (numScripts != 1)
                        {
                            taskCompletionSrc.SetException(
                                new Exception($"Failed to download script details: Expected 1 result, got {numScripts}."));
                            return;
                        }

                        xmlReader.ReadToFollowing("publishedfileid");
                        ulong publishedFileId = Convert.ToUInt64(xmlReader.ReadElementContentAsString());
                        xmlReader.ReadToFollowing("result");
                        MyGameServiceCallResult serviceCallResult2 = (MyGameServiceCallResult)xmlReader.ReadElementContentAsInt();
                        if (serviceCallResult2 != MyGameServiceCallResult.OK)
                        {
                            taskCompletionSrc.SetException(
                                new Exception($"Failed to download script: id = {publishedFileId}, result = {serviceCallResult2}"));
                            return;
                        }

                        xmlReader.ReadToFollowing("consumer_app_id");
                        int appId = xmlReader.ReadElementContentAsInt();
                        if (appId != MyGameService.AppId)
                        {
                            taskCompletionSrc.SetException(new Exception($"Failed to download script: id = {workshopId}, wrong appid, got {appId}."));
                            return;
                        }

                        xmlReader.ReadToFollowing("file_size");
                        long expectedFileSize = xmlReader.ReadElementContentAsLong();
                        xmlReader.ReadToFollowing("title");
                        string title = xmlReader.ReadElementContentAsString();

                        xmlReader.ReadToFollowing("time_updated");
                        uint timeUpdated = (uint)xmlReader.ReadElementContentAsLong();
                        MyWorkshop.SubscribedItem script = new MyWorkshop.SubscribedItem()
                        {
                            Title = title,
                            PublishedFileId = publishedFileId,
                            TimeUpdated = timeUpdated,
                        };
                        taskCompletionSrc.SetResult(script);
                    }
                }
                catch (Exception e)
                {
                    taskCompletionSrc.SetException(new Exception($"Failed to fetch script information: {workshopId}\n{e.Message}"));
                }
            });

            return await task;

        }

        public static async Task<string> DownloadScriptAsync(MyWorkshop.SubscribedItem scriptInfo)
        {
            /*if( IsModUpToDateBlocking(scriptInfo) )
            {
                Log.Info($"Script {scriptInfo.Title} is up to date.");
                return null;
            }*/

            var taskCompletionSrc = new TaskCompletionSource<string>();
            var task = taskCompletionSrc.Task;

            Log.Info($"Downloading script '{scriptInfo.Title}'...");

            string scriptPath = Path.Combine(ScriptManagerPlugin.ScriptsPath, scriptInfo.PublishedFileId + ".sbs");
            Directory.CreateDirectory(ScriptManagerPlugin.ScriptsPath);
            var gameService = MyServiceManager.Instance.GetService<IMyGameService>();
            gameService.DownloadModForServer(scriptInfo.PublishedFileId, scriptPath, (Action<bool>)(success =>
            {
                Log.Info("Download completed!");
                if (!success)
                {
                    taskCompletionSrc.SetException(new Exception($"Could not download script: name = {scriptInfo.Title}"));
                    return;
                }

                string text = null;

                foreach (string file in MyFileSystem.GetFiles(scriptPath, ".cs", MySearchOption.AllDirectories))
                {
                    if (MyFileSystem.FileExists(file))
                    {
                        using (Stream stream = MyFileSystem.OpenRead(file))
                        {
                            using (StreamReader streamReader = new StreamReader(stream))
                            {
                                text = streamReader.ReadToEnd();
                            }
                        }
                    }
                }

                if (text == null)
                {
                    File.Delete(scriptPath);
                    taskCompletionSrc.SetException(new Exception("Failed to read script from downloaded file!"));
                    return;
                }

                taskCompletionSrc.SetResult(text);
            }));

            return await task;
        }
    }
}
