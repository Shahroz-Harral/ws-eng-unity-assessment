using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using UnityEngine.Networking;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

public class SubmissionTool : EditorWindow
{
    private const string AssessmentType = "unity-game-development";
    private const string AssessmentVersion = "v2";
    private const string EncodedApiUrl = "iyuja327ulc6hq3xsypufut7bh0lygdq.ynzoqn-hey.hf-rnfg-1.ba.njf";

    private string userName;
    private string userEmail;
    private string submissionId;
    private string progressLabel = "";

    [MenuItem("Crossover/Submit")]
    public static void ShowWindow()
    {
        GetWindow<SubmissionTool>("Submit");
    }

    private void OnEnable()
    {
        userName = RunGitCommand("config user.name");
        userEmail = RunGitCommand("config user.email");
    }

    private void OnGUI()
    {
        GUILayout.Label("Submit your project", EditorStyles.boldLabel);
        
        userName = EditorGUILayout.TextField("Name:", userName);
        userEmail = EditorGUILayout.TextField("Email:", userEmail);
        EditorGUILayout.LabelField("Folder to submit:", RunGitCommand("rev-parse --show-toplevel"));

        EditorGUILayout.Space(10);
        
        if (GUILayout.Button("Click me to submit!"))
        {
            SubmitProject();
        }

        EditorGUILayout.LabelField("Progress:", progressLabel);

        if (!string.IsNullOrEmpty(submissionId))
        {
            EditorGUILayout.TextField("Submission ID:", submissionId);
            EditorGUILayout.HelpBox($"Please copy-paste this ID into the Crossover assessment page.", MessageType.Info);
        }
    }

    private void SubmitProject()
    {
        try
        {
            progressLabel = "Committing changes...";
            Repaint();
            // Commit changes
            RunGitCommand("add --all");
            RunGitCommand("commit --allow-empty -am \"chore(jenga): Prepares submission.\"");

            progressLabel = "Creating zip archive...";
            Repaint();
            // Create zip archive
            string zipPath = CreateZipArchive();
            
            if (EditorUtility.DisplayDialog("Confirm Submission", $"Is the zip file correct and ready for submission?\nZip file: {zipPath}", "Yes", "No"))
            {
                StartSubmission(zipPath);
            }
            else
            {
                progressLabel = "Submission cancelled.";
                Repaint();
            }
        }
        catch (Exception ex)
        {
            progressLabel = "Error during submission preparation.";
            Repaint();
            UnityEngine.Debug.LogError($"Error during submission preparation: {ex.Message}");
        }
    }

    private async void StartSubmission(string zipPath)
    {
        try
        {
            progressLabel = "Starting submission...";
            Repaint();
            string apiUrl = DecodeUrl(EncodedApiUrl);
            long fileSize = new FileInfo(zipPath).Length;

            using (var client = new HttpClient())
            {
                var content = new StringContent(JsonConvert.SerializeObject(new SubmissionData
                {
                    name = userName,
                    email = userEmail,
                    size = fileSize,
                    type = AssessmentType
                }), Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"https://{apiUrl}", content);
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"API request failed with status code: {response.StatusCode}");
                }
                var responseString = await response.Content.ReadAsStringAsync();
                var responseJson = JsonConvert.DeserializeObject<SubmissionResponse>(responseString);

                UploadFile(zipPath, responseJson);
            }
        }
        catch (Exception ex)
        {
            progressLabel = "Error starting submission.";
            Repaint();
            UnityEngine.Debug.LogError($"Error starting submission: {ex.Message}");
        }
    }

    private void UploadFile(string zipPath, SubmissionResponse responseJson)
    {
        try
        {
            progressLabel = "Uploading file...";
            Repaint();
            var uploadUrl = responseJson.upload.url;
            var fields = responseJson.upload.fields;
            UnityEngine.Debug.Log($"Uploading file to {uploadUrl} with fields: {fields}");

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
                    submissionId = responseJson.submissionId;
                    progressLabel = $"Submission successful";
                    Repaint();
                    UnityEngine.Debug.Log($"Submission successful, ID: {submissionId}");
                }
                else
                {
                    progressLabel = "Submission failed.";
                    Repaint();
                    UnityEngine.Debug.LogError($"Submission failed: {www.error}");
                }
                www.Dispose();
            };
        }
        catch (Exception ex)
        {
            progressLabel = "Error uploading file.";
            Repaint();
            UnityEngine.Debug.LogError($"Error uploading file: {ex.Message}");
        }
    }

    private string CreateZipArchive()
    {
        try
        {
            string outputFile = $"submission_{Regex.Replace(userName, @"[^a-zA-Z0-9+._-]", "")}.zip";
            string zipPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputFile));
            RunGitCommand($"archive --format=zip --output=\"{zipPath}\" HEAD Assets Packages ProjectSettings Demo");
            return zipPath;
        }
        catch (Exception ex)
        {
            throw new Exception($"Error creating zip archive: {ex.Message}");
        }
    }

    private string RunGitCommand(string arguments)
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
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output.Trim();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error running Git command: {ex.Message}");
        }
    }

    private string DecodeUrl(string encodedUrl)
    {
        return new string(encodedUrl.ToCharArray().Select(c =>
        {
            if (char.IsLetter(c))
            {
                char baseChar = char.IsUpper(c) ? 'A' : 'a';
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