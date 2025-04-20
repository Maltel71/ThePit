using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : MonoBehaviour
{
    [Header("Våningsinställningar")]
    [SerializeField] private Transform[] floorPositions; // Positioner för varje våning
    [SerializeField] private float waitTimeAtFloor = 5f; // Tid att vänta vid varje våning i sekunder
    [SerializeField] private float moveSpeed = 2f; // Hastighet för hissen

    [Header("Debug")]
    [SerializeField] private int currentFloorIndex = 0; // Nuvarande våning (0 = högst upp)
    [SerializeField] private bool isMoving = false; // Om hissen rör sig eller väntar

    private bool isGoingDown = true; // Riktning (nedåt = true, uppåt = false)

    private void Start()
    {
        // Börja med att placera hissen på översta våningen
        if (floorPositions.Length > 0)
        {
            transform.position = floorPositions[0].position;
            StartCoroutine(MovePlatform());
        }
        else
        {
            Debug.LogError("Inga våningspositioner är inställda på PlatformController!");
        }
    }

    private IEnumerator MovePlatform()
    {
        while (true) // Fortsätt i all oändlighet
        {
            // Vänta på nuvarande våning
            isMoving = false;
            yield return new WaitForSeconds(waitTimeAtFloor);

            // Bestäm nästa våning
            isMoving = true;

            if (isGoingDown)
            {
                currentFloorIndex++;

                // Om vi nått botten, ändra riktning
                if (currentFloorIndex >= floorPositions.Length - 1)
                {
                    currentFloorIndex = floorPositions.Length - 1;
                    isGoingDown = false;
                }
            }
            else // Går uppåt
            {
                currentFloorIndex--;

                // Om vi nått toppen, ändra riktning
                if (currentFloorIndex <= 0)
                {
                    currentFloorIndex = 0;
                    isGoingDown = true;

                    // Alternativ: Om du vill att den ska starta om från toppen istället för att vända
                    // currentFloorIndex = 0;
                    // isGoingDown = true;
                }
            }

            // Flytta hissen till nästa våning
            Vector3 targetPosition = floorPositions[currentFloorIndex].position;

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    moveSpeed * Time.deltaTime
                );
                yield return null; // Vänta till nästa frame
            }

            // Säkerställ att vi är exakt på målpositionen
            transform.position = targetPosition;
        }
    }

    // Alternativ 1: Enklare version där hissen bara går uppifrån och ner
    private IEnumerator MovePlatformSimple()
    {
        while (true)
        {
            // Starta från toppen
            currentFloorIndex = 0;
            transform.position = floorPositions[currentFloorIndex].position;

            // Fortsätt nedåt genom alla våningar
            for (int i = 0; i < floorPositions.Length; i++)
            {
                // Vänta på våningen
                isMoving = false;
                yield return new WaitForSeconds(waitTimeAtFloor);
                isMoving = true;

                // Om vi inte är på sista våningen, flytta nedåt
                if (i < floorPositions.Length - 1)
                {
                    Vector3 targetPosition = floorPositions[i + 1].position;

                    while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
                    {
                        transform.position = Vector3.MoveTowards(
                            transform.position,
                            targetPosition,
                            moveSpeed * Time.deltaTime
                        );
                        yield return null;
                    }

                    transform.position = targetPosition;
                    currentFloorIndex = i + 1;
                }
            }

            // Vänta på sista våningen
            yield return new WaitForSeconds(waitTimeAtFloor);

            // Teleportera tillbaka till toppen (eller du kan animera detta om du vill)
            transform.position = floorPositions[0].position;
        }
    }

    // För debugging: Rita linjer mellan våningarna i Scene-vyn
    private void OnDrawGizmos()
    {
        if (floorPositions == null || floorPositions.Length <= 1)
            return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < floorPositions.Length - 1; i++)
        {
            if (floorPositions[i] != null && floorPositions[i + 1] != null)
            {
                Gizmos.DrawLine(floorPositions[i].position, floorPositions[i + 1].position);
            }
        }
    }
}