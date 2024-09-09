using UnityEngine;
using UnityEngine.UI;

public class BlockInfoDisplay : MonoBehaviour
{
    public string blockInfo; // Information about the block to display as a tooltip
    public GameObject tooltipPrefab; // Prefab for the tooltip

    private GameObject tooltipInstance;

    void OnMouseDown()
    {
        // Display the tooltip when the block is clicked
        if (tooltipInstance == null)
        {
            tooltipInstance = Instantiate(tooltipPrefab, transform.position, Quaternion.identity);
            tooltipInstance.GetComponentInChildren<Text>().text = blockInfo;
        }
        else
        {
            Destroy(tooltipInstance); // Destroy tooltip if clicked again
        }
    }

    void OnMouseExit()
    {
        if (tooltipInstance != null)
        {
            Destroy(tooltipInstance); // Destroy tooltip when the mouse exits the block
        }
    }
}
