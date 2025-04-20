using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodItem : MonoBehaviour
{
    [SerializeField] private float foodValue = 20f; // Hur mycket hunger som �terst�lls
    [SerializeField] private float healthValue = 0f; // Eventuell h�lsa som �terst�lls (valfritt)
    [SerializeField] private GameObject visualModel; // Visuell representation av mat
    [SerializeField] private bool disappearAfterEaten = true; // Om maten ska f�rsvinna efter den �tits

    private void OnTriggerEnter(Collider other)
    {
        // Kolla om det �r spelaren som kolliderar med maten
        if (other.CompareTag("Player"))
        {
            // Hitta spelarens status-skript
            PlayerStatus playerStatus = other.GetComponent<PlayerStatus>();

            if (playerStatus != null)
            {
                // �terst�ll hunger
                playerStatus.Eat(foodValue);

                // �terst�ll eventuell h�lsa
                if (healthValue > 0)
                {
                    playerStatus.RestoreHealth(healthValue);
                }

                // Hantera matens visuella del
                if (disappearAfterEaten)
                {
                    // F�rst�r hela objektet
                    Destroy(gameObject);
                }
                else if (visualModel != null)
                {
                    // D�lj bara den visuella delen
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