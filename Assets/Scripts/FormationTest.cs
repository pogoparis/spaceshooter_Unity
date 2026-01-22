using UnityEngine;

public class FormationTest : MonoBehaviour
{
    void Start()
    {
        GetComponent<FormationController>().SpawnFormation();
    }
}
