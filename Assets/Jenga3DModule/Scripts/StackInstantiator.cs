using UnityEngine;
using System.Collections.Generic;

public class StackInstantiator : MonoBehaviour
{
    public GameObject glassBlockPrefab;  // Assign the Glass block prefab in the Inspector
    public GameObject woodBlockPrefab;   // Assign the Wood block prefab in the Inspector
    public GameObject stoneBlockPrefab;  // Assign the Stone block prefab in the Inspector
    public Transform[] stackPositions;   // Assign stack positions where stacks should be created

    private List<GameObject> blocks = new List<GameObject>();  // Keep track of instantiated blocks

    // Example data; replace this with your API data fetching method
    private List<BlockData> exampleBlocks = new List<BlockData>
    {
        new BlockData { id = 1, subject = "Math", grade = "6th Grade", mastery = 2, domainid = "RP", domain = "Ratios & Proportional Relationships", cluster = "Cluster 1", standardid = "CCSS.MATH.CONTENT.6.RP.A.1", standarddescription = "Description A" },
        new BlockData { id = 2, subject = "Math", grade = "6th Grade", mastery = 1, domainid = "G", domain = "Geometry", cluster = "Cluster 2", standardid = "CCSS.MATH.CONTENT.6.G.A.1", standarddescription = "Description B" },
        // Add more blocks here
    };

    void Start()
    {
        InstantiateStacks(exampleBlocks);
    }

    private void InstantiateStacks(List<BlockData> blockDataList)
    {
        // Group blocks by grade and order them
        var blocksByGrade = new Dictionary<string, List<BlockData>>();
        foreach (var block in blockDataList)
        {
            if (!blocksByGrade.ContainsKey(block.grade))
                blocksByGrade[block.grade] = new List<BlockData>();
            blocksByGrade[block.grade].Add(block);
        }

        int index = 0;
        foreach (var grade in blocksByGrade.Keys)
        {
            if (index >= stackPositions.Length) break;  // Ensure there are enough stack positions

            var blocksInGrade = blocksByGrade[grade];
            blocksInGrade.Sort((a, b) =>
                a.domain.CompareTo(b.domain) != 0 ? a.domain.CompareTo(b.domain) :
                a.cluster.CompareTo(b.cluster) != 0 ? a.cluster.CompareTo(b.cluster) :
                a.standardid.CompareTo(b.standardid));

            // Instantiate blocks for each grade
            InstantiateBlocks(blocksInGrade, stackPositions[index]);
            index++;
        }
    }

    private void InstantiateBlocks(List<BlockData> blocksData, Transform stackPosition)
    {
        float blockHeight = 0.5f;  // Height of each block
        float currentHeight = 0;   // Current height of the stack

        foreach (var blockData in blocksData)
        {
            GameObject blockPrefab;

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

            GameObject block = Instantiate(blockPrefab, stackPosition.position + new Vector3(0, currentHeight, 0), Quaternion.identity);
            block.name = $"{blockData.domain} - {blockData.cluster}";
            block.transform.SetParent(stackPosition);
            blocks.Add(block);
            currentHeight += blockHeight; // Increment height for the next block
        }
    }
}

[System.Serializable]
public class BlockData
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
