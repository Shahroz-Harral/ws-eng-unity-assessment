using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace Jenga3DModule.Editor
{
    public class SubmissionTool : EditorWindow
    {
        private const string AssessmentType = "unity-game-development";
        private const string EncodedApiUrl = "iyuja327ulc6hq3xsypufut7bh0lygdq.ynzoqn-hey.hf-rnfg-1.ba.njf";

        private string _userName;
        private string _userEmail;
        private string _submissionId;

        [MenuItem("Crossover/Submit")]
        public static void ShowWindow()
        {
            var window = GetWindow<SubmissionTool>("Submit");
            window.minSize = new Vector2(450, 200);
            window.Show();
        }

        private void OnEnable()
        {
            _userName = RunGitCommand("config user.name");
            _userEmail = RunGitCommand("config user.email");
        }

        private void OnGUI()
        {
            GUILayout.Label("Submit your project", EditorStyles.boldLabel);
        
            _userName = EditorGUILayout.TextField("Name:", _userName);
            _userEmail = EditorGUILayout.TextField("Email:", _userEmail);
            EditorGUILayout.LabelField("Folder to submit:", RunGitCommand("rev-parse --show-toplevel"));

            EditorGUILayout.Space(10);
        
            if (GUILayout.Button("Click me to submit!"))
            {
                SubmitProject();
            }

            if (string.IsNullOrEmpty(_submissionId) == false)
            {
                EditorGUILayout.Space(10);
                GUIUtility.systemCopyBuffer = _submissionId;
                EditorGUILayout.HelpBox($"Submission ID copied to clipboard.\nPlease paste this ID into the Crossover assessment page.",
                    MessageType.Info);
                EditorGUILayout.TextField("Submission ID:", _submissionId);
            }
        }

        private static void ReportProgress(string message, float progress)
        {
            EditorUtility.DisplayProgressBar("Submitting project", message, progress);
        }

        private static void ReportError(string errorMessage)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Project submission error", errorMessage, "OK");
            Debug.LogError(errorMessage);
        }

        private void ReportSuccess(string submissionId)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Project submission", "Submission was successful.", "OK");
            Debug.Log($"Submission was successful, ID: {submissionId}");
            Repaint();
        }

        private void SubmitProject()
        {
            try
            {
                ReportProgress("Commiting changes...", 0);

                // Commit changes
                RunGitCommand("add --all");
                ReportProgress("Commiting changes...", 0.1f);
                RunGitCommand("commit --allow-empty -am \"chore(jenga): Prepares submission.\"");

                ReportProgress("Creating zip archive...", 0.2f);
                // Create zip archive
                var zipPath = CreateZipArchive();

                if (EditorUtility.DisplayDialog("Confirm Submission", $"Is the zip file correct and ready for submission?\nZip file: {zipPath}", "Yes", "No"))
                {
                    StartSubmission(zipPath);
                }
                else
                {
                    ReportError("Submission cancelled.");
                }
            }
            catch (Exception ex)
            {
                ReportError($"Error during submission preparation:\n{ex.Message}");
            }
        }
        
        private async void StartSubmission(string zipPath)
        {
            try
            {
                ReportProgress("Starting submission...", 0.4f);
                var apiUrl = DecodeUrl(EncodedApiUrl);
                var fileSize = new FileInfo(zipPath).Length;

                var content = new StringContent(JsonConvert.SerializeObject(new SubmissionData
                {
                    name = _userName,
                    email = _userEmail,
                    size = fileSize,
                    type = AssessmentType
                }), Encoding.UTF8, "application/json");

                using var client = new HttpClient();
                var response = await client.PostAsync($"https://{apiUrl}", content);
                if (response.IsSuccessStatusCode == false)
                {
                    throw new Exception($"API request failed with status code: {response.StatusCode}");
                }
                var responseString = await response.Content.ReadAsStringAsync();
                var responseJson = JsonConvert.DeserializeObject<SubmissionResponse>(responseString);

                UploadFile(zipPath, responseJson);
            }
            catch (Exception ex)
            {
                ReportError($"Error starting submission:\n{ex.Message}");
            }
        }

        private void UploadFile(string zipPath, SubmissionResponse responseJson)
        {
            try
            {
                ReportProgress("Uploading file...", 0.7f);
                var uploadUrl = responseJson.upload.url;
                var fields = responseJson.upload.fields;
                Debug.Log($"Uploading file to {uploadUrl} with fields:\n{JsonConvert.SerializeObject(fields)}");

                var form = new WWWForm();
                foreach (var field in fields)
                {
                    form.AddField(field.Key, field.Value);
                }
                form.AddBinaryData("file", File.ReadAllBytes(zipPath), Path.GetFileName(zipPath), "application/zip");

                var www = UnityWebRequest.Post(uploadUrl, form);
                var operation = www.SendWebRequest();

                operation.completed += op =>
                {
                    if (www.result == UnityWebRequest.Result.Success)
                    {
                        _submissionId = responseJson.submissionId;
                        ReportSuccess(_submissionId);
                    }
                    else
                    {
                        ReportError($"Submission failed:\n{www.error}");
                    }
                    www.Dispose();
                };
            }
            catch (Exception ex)
            {
                ReportError($"Error uploading file:\n{ex.Message}");
            }
        }

        private string CreateZipArchive()
        {
            try
            {
                var outputFile = $"submission_{Regex.Replace(_userName, @"[^a-zA-Z0-9+._-]", "")}.zip";
                var zipPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputFile));
                RunGitCommand($"archive --format=zip --output=\"{zipPath}\" HEAD Assets Packages ProjectSettings Demo");
                return zipPath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating zip archive: {ex.Message}");
            }
        }

        private static string RunGitCommand(string arguments)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = arguments,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output.Trim();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error running Git command: {ex.Message}");
            }
        }

        private static string DecodeUrl(string encodedUrl)
        {
            return new string(encodedUrl.ToCharArray().Select(c =>
            {
                if (char.IsLetter(c))
                {
                    var baseChar = char.IsUpper(c) ? 'A' : 'a';
                    return (char)((c - baseChar + 13) % 26 + baseChar);
                }
                return c;
            }).ToArray());
        }

        [Serializable]
        private class SubmissionData
        {
            public string name;
            public string email;
            public long size;
            public string type;
        }

        [Serializable]
        private class SubmissionResponse
        {
            public string submissionId;
            public UploadInfo upload;
        }

        [Serializable]
        private class UploadInfo
        {
            public string url;
            public Dictionary<string, string> fields;
        }
    }
}