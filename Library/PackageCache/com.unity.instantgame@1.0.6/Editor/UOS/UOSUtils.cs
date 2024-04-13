#if  IG_C301 || IG_C302 || IG_C303 || TUANJIE_2022_3_OR_NEWER // Auto generated by AddMacroForInstantGameFiles.exe

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

using UnityEditor;
using UnityEngine;

namespace Unity.AutoStreaming.CloudContentDelivery
{
    [InitializeOnLoad]
    internal class ServerCertificateSetup
    {
        static ServerCertificateSetup()
        {
#if !UNITY_2018_1_OR_NEWER
            ServicePointManager.ServerCertificateValidationCallback +=
                (sender, cert, chain, error) =>
            {
                if (cert.GetCertHashString() == "272A7463FBA1AA1C86481A14AB8F241DB4B80A6F")
                {
                    return true;
                }
                else
                {
                    return error == SslPolicyErrors.None;
                }
            };
#endif
        }
    }


    internal class Util
    {
        internal static int MaxThreadCount = 5;

        internal static HttpWebRequest GetHttpWebRequest4UOSThreaded(string uosAuthorizationToken, string url, string method, string requestBody = null)
        {
            ServicePointManager.Expect100Continue = false;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Proxy = null;

            request.Headers.Add("Authorization", "Basic " + uosAuthorizationToken);
            request.ContentType = "application/json";
            request.Method = method;
            request.Timeout = 200000;

            if (requestBody != null)
            {
                ASCIIEncoding encoding = new ASCIIEncoding();
                byte[] bodyByte = encoding.GetBytes(requestBody);
                request.ContentLength = bodyByte.Length;

                Stream newStream = request.GetRequestStream();
                newStream.Write(bodyByte, 0, bodyByte.Length);
                newStream.Close();
            }

            return request;
        }

        private static bool WangsuUploadFilesThreading(string uosAuthorizationToken, string uosAppId, string bucketUuid, Dictionary<string, EntryInfo> toBeUploads)
        {
            var allSuccess = true;
            int i = 0, a = 0;
            float total = toBeUploads.Count;
            Task<bool>[] taskArray = new Task<bool>[MaxThreadCount];
            foreach (var info in toBeUploads)
            {
                EditorUtility.DisplayProgressBar("AutoStreaming data uploading", "Uploading entries: " + (a++) + "/" + total, a / total);

                i = i % MaxThreadCount;
                if (taskArray[i] != null)
                {
                    i = Task.WaitAny(taskArray);
                    if (i == -1)
                        continue;
                    allSuccess = allSuccess && taskArray[i].Result;
                }

                taskArray[i++] = Task<bool>.Factory.StartNew(() =>
                {

                    if (!WangsuUtils.uploadMulti(uosAuthorizationToken, bucketUuid, info.Value))
                    {
                        Debug.LogError("Asset " + info.Value.full_path + " upload failed. Please try again after all upload finished.");
                        return false;
                    }

                    if (!Entry.CreateEntry(uosAuthorizationToken, uosAppId, bucketUuid, info.Value))
                    {
                        Debug.LogError("Failed creating entry " + info.Value.full_path + ". Please try again after all upload finished.");
                        return false;
                    }

                    return true;
                });
            }

            for (int j =0; j< MaxThreadCount; j++) 
            {
                if (taskArray[j] != null)
                {
                    taskArray[j].Wait();
                    allSuccess = allSuccess && taskArray[j].Result;
                }
            }

            EditorUtility.ClearProgressBar();
            return allSuccess;
        }
        internal static bool UploadFiles(string uosAuthorizationToken, string uosAppId, string bucketUuid, Dictionary<string, EntryInfo> toBeUploads)
        {
            return WangsuUploadFilesThreading(uosAuthorizationToken, uosAppId, bucketUuid, toBeUploads);
        }

        private static bool WangsuUpdateFilesThreading(string uosAuthorizationToken, string uosAppId, string bucketUuid, Dictionary<string, EntryInfo> toBeUpdates)
        {
            var allSuccess = true;

            int i =0,a = 0;
            float total = toBeUpdates.Count;
            Task<bool>[] taskArray = new Task<bool>[MaxThreadCount];
            foreach (var info in toBeUpdates)
            {
                EditorUtility.DisplayProgressBar("AutoStreaming data uploading", "Uploading entries: " + (a++) + "/" + total, a / total);

                i = i % MaxThreadCount;
                if (taskArray[i] != null)
                {
                    i = Task.WaitAny(taskArray);
                    if (i == -1)
                        continue;

                    allSuccess = allSuccess && taskArray[i].Result;
                }

                taskArray[i++] = Task<bool>.Factory.StartNew(() =>
                {
                    if (!WangsuUtils.uploadMulti(uosAuthorizationToken, bucketUuid, info.Value))
                    {
                        Debug.LogError("Asset " + info.Value.full_path + " upload failed. Please try again after all upload finished.");
                        return false;
                    }

                    if (!Entry.UpdateEntry(uosAuthorizationToken, uosAppId, bucketUuid, info.Key, info.Value))
                    {
                        Debug.LogError("Failed updating entry " + info.Value.full_path + ". Please try again after all upload finished.");
                        return false;
                    }

                    return true;
                });
            }

            for (int j = 0; j < MaxThreadCount; j++)
            {
                if (taskArray[j] != null)
                {
                    taskArray[j].Wait();
                    allSuccess = allSuccess && taskArray[j].Result;
                }
            }

            EditorUtility.ClearProgressBar();
            return allSuccess;
        }

        internal static bool UpdateFiles(string uosAuthorizationToken, string uosAppId, string bucketUuid, Dictionary<string, EntryInfo> toBeUpdates)
        {
            return WangsuUpdateFilesThreading(uosAuthorizationToken, uosAppId, bucketUuid, toBeUpdates);
        }

        public static string GetFileMD5(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(fs);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }

#if UNITY_WEBGL && (IG_C301 || IG_C302 || IG_C303 || TUANJIE_2022_3_OR_NEWER)

        public static string AppendHash128ToFileNameIfNeeded(string fullPath, string hash)
        {
            if (fullPath.StartsWith(Path.GetFullPath(ASBuildConstants.k_SceneABPath)) ||
                fullPath.StartsWith(Path.GetFullPath(ASBuildConstants.k_TextureABPath)) ||
                fullPath.StartsWith(Path.GetFullPath(ASBuildConstants.k_FontABPath)) ||
                fullPath.StartsWith(Path.GetFullPath(ASBuildConstants.k_AudioABPath)) ||
                fullPath.StartsWith(Path.GetFullPath(ASBuildConstants.k_CloudABPath)))
                return Path.GetFileNameWithoutExtension(fullPath) + "_" + hash + Path.GetExtension(fullPath);


            return Path.GetFileName(fullPath);
        }
#endif // UNITY_WEBGL && (IG_C301 || IG_C302 || IG_C303 || TUANJIE_2022_3_OR_NEWER)

        internal static Hash128 GetFileHash128(string fullPath) 
        {
            if (!File.Exists(fullPath))
                return new Hash128();

            byte[] data = File.ReadAllBytes(fullPath);
            return Hash128.Compute(data);
        
        }

    }
}

#endif  //  IG_C301 || IG_C302 || IG_C303 || TUANJIE_2022_3_OR_NEWER, Auto generated by AddMacroForInstantGameFiles.exe
