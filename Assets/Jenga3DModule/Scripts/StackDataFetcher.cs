using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class StackDataFetcher : MonoBehaviour
{
    public string apiUrl = "https://ga1vqcu3o1.execute-api.us-east-1.amazonaws.com/Assessment/stack";
    public List<StackData> stacks = new List<StackData>();

    public GameObject glassBlockPrefab;
    public GameObject woodBlockPrefab;
    public GameObject stoneBlockPrefab;
    public Transform[] stackPositions; // Assign positions for 6th, 7th, and 8th-grade stacks in the Inspector

    void Start()
    {
        StartCoroutine(FetchData());
    }

    IEnumerator FetchData()
    {
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            stacks = JsonConvert.DeserializeObject<List<StackData>>(jsonResponse);
            CreateStacks();
        }
        else
        {
            Debug.LogError("Error fetching data: " + request.error);
        }
    }

    void CreateStacks()
    {
        // Group the blocks by grade
        Dictionary<string, List<StackData>> stacksByGrade = new Dictionary<string, List<StackData>>();

        foreach (StackData stack in stacks)
        {
            if (!stacksByGrade.ContainsKey(stack.grade))
            {
                stacksByGrade[stack.grade] = new List<StackData>();
            }
            stacksByGrade[stack.grade].Add(stack);
        }

        // Instantiate stacks for each grade
        int gradeIndex = 0;
        foreach (var grade in stacksByGrade)
        {
            if (gradeIndex >= stackPositions.Length)
            {
                Debug.LogWarning("More grades than positions available!");
                break;
            }

            // Sort blocks within each grade by domain, cluster, and standardid
            grade.Value.Sort((a, b) =>
                a.domain.CompareTo(b.domain) != 0 ? a.domain.CompareTo(b.domain) :
                a.cluster.CompareTo(b.cluster) != 0 ? a.cluster.CompareTo(b.cluster) :
                a.standardid.CompareTo(b.standardid)
            );

            // Instantiate blocks in a Jenga configuration
            InstantiateBlocks(grade.Value, stackPositions[gradeIndex]);
            gradeIndex++;
        }
    }

    void InstantiateBlocks(List<StackData> blocksData, Transform stackPosition)
    {
        float blockWidth = 1.0f;  // Width of each block
        float blockHeight = 0.5f;  // Height of each block
        float offsetX = blockWidth / 2.0f; // X offset for Jenga configuration

        int layerCount = 0;
        int blockInLayer = 0;

        foreach (var blockData in blocksData)
        {
            GameObject blockPrefab;

            // Choose prefab based on mastery level
            switch (blockData.mastery)
            {
                case 0:
                    blockPrefab = glassBlockPrefab;
                    break;
                case 1:
                    blockPrefab = woodBlockPrefab;
                    break;
                case 2:
                    blockPrefab = stoneBlockPrefab;
                    break;
                default:
                    Debug.LogError("Unknown block mastery level!");
                    continue;
            }

            // Determine the position and rotation for Jenga configuration
            Vector3 blockPosition = stackPosition.position + new Vector3(
                blockInLayer % 2 == 0 ? -offsetX : offsetX,
                layerCount * blockHeight,
                blockInLayer % 2 == 0 ? offsetX : -offsetX
            );

            Quaternion blockRotation = Quaternion.Euler(0, blockInLayer % 2 == 0 ? 0 : 90, 0);

            // Instantiate block and set its parent
            GameObject block = Instantiate(blockPrefab, blockPosition, blockRotation);
            block.transform.SetParent(stackPosition);

            // Assign information to the block
            BlockInfoDisplay infoDisplay = block.AddComponent<BlockInfoDisplay>();
            infoDisplay.blockInfo = blockData.standarddescription;

            blockInLayer++;
            if (blockInLayer >= 3) // Jenga configuration: 3 blocks per layer
            {
                blockInLayer = 0;
                layerCount++;
            }
        }
    }
}

[System.Serializable]
public class StackData
{
    public int id;
    public string subject;
    public string grade;
    public int mastery;
    public string domainid;
    public string domain;
    public string cluster;
    public string standardid;
    public string standarddescription;
}
