using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StackManager : MonoBehaviour
{
    public Transform[] stackPositions; // 3 positions for each stack on the table
    public GameObject glassPrefab, woodPrefab, stonePrefab;

    public void CreateStacks(List<StackData> stackData)
    {
        Dictionary<string, List<StackData>> groupedByGrade = new Dictionary<string, List<StackData>>();

        // Group data by grade
        foreach (var data in stackData)
        {
            if (!groupedByGrade.ContainsKey(data.grade))
            {
                groupedByGrade[data.grade] = new List<StackData>();
            }
            groupedByGrade[data.grade].Add(data);
        }

        int index = 0;
        foreach (var gradeData in groupedByGrade)
        {
            List<StackData> sortedStackData = gradeData.Value;
            sortedStackData.Sort((a, b) => {
                int domainComparison = a.domain.CompareTo(b.domain);
                if (domainComparison == 0)
                {
                    int clusterComparison = a.cluster.CompareTo(b.cluster);
                    return clusterComparison == 0 ? a.standardid.CompareTo(b.standardid) : clusterComparison;
                }
                return domainComparison;
            });

            CreateStack(sortedStackData, stackPositions[index]);
            index++;
        }
    }

    void CreateStack(List<StackData> stackData, Transform position)
    {
        for (int i = 0; i < stackData.Count; i++)
        {
            // Determine block type based on mastery level
            GameObject blockPrefab = glassPrefab; // Default to glass
            switch (stackData[i].mastery)
            {
                case 1:
                    blockPrefab = woodPrefab;
                    break;
                case 2:
                    blockPrefab = stonePrefab;
                    break;
            }

            // Calculate position for each block
            Vector3 blockPosition = position.position + new Vector3(0, i * 0.5f, 0); // Adjust 0.5f for appropriate block height

            // Alternate orientation: every second layer is rotated
            Quaternion blockRotation = Quaternion.identity;
            if (i % 2 == 1)
            {
                blockRotation = Quaternion.Euler(0, 90, 0); // Rotate 90 degrees around Y-axis
            }

            // Create the block and set its parent
            GameObject block = Instantiate(blockPrefab, blockPosition, blockRotation);
            block.transform.SetParent(position);

            // Position adjustment for Jenga style: Shift blocks in each layer
            if (i % 2 == 0)
            {
                // If it's an even layer, position blocks along the X-axis
                block.transform.localPosition += new Vector3((i % 3 - 1) * 1.1f, 0, 0);
            }
            else
            {
                // If it's an odd layer, position blocks along the Z-axis
                block.transform.localPosition += new Vector3(0, 0, (i % 3 - 1) * 1.1f);
            }
        }
    }

}
