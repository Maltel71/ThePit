using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodItem : MonoBehaviour
{
    [SerializeField] private float foodValue = 20f; // Hur mycket hunger som återställs
    [SerializeField] private float healthValue = 0f; // Eventuell hälsa som återställs (valfritt)
    [SerializeField] private GameObject visualModel; // Visuell representation av mat
    [SerializeField] private bool disappearAfterEaten = true; // Om maten ska försvinna efter den ätits

    private void OnTriggerEnter(Collider other)
    {
        // Kolla om det är spelaren som kolliderar med maten
        if (other.CompareTag("Player"))
        {
            // Hitta spelarens status-skript
            PlayerStatus playerStatus = other.GetComponent<PlayerStatus>();

            if (playerStatus != null)
            {
                // Återställ hunger
                playerStatus.Eat(foodValue);

                // Återställ eventuell hälsa
                if (healthValue > 0)
                {
                    playerStatus.RestoreHealth(healthValue);
                }

                // Hantera matens visuella del
                if (disappearAfterEaten)
                {
                    // Förstör hela objektet
                    Destroy(gameObject);
                }
                else if (visualModel != null)
                {
                    // Dölj bara den visuella delen
                    visualModel.SetActive(false);

                    // Inaktivera collider
                    Collider foodCollider = GetComponent<Collider>();
                    if (foodCollider != null)
                    {
                        foodCollider.enabled = false;
                    }
                }
            }
        }
    }
}