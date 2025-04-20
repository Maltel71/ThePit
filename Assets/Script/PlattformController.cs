using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : MonoBehaviour
{
    [Header("V�ningsinst�llningar")]
    [SerializeField] private Transform[] floorPositions; // Positioner f�r varje v�ning
    [SerializeField] private float waitTimeAtFloor = 5f; // Tid att v�nta vid varje v�ning i sekunder
    [SerializeField] private float moveSpeed = 2f; // Hastighet f�r hissen

    [Header("Debug")]
    [SerializeField] private int currentFloorIndex = 0; // Nuvarande v�ning (0 = h�gst upp)
    [SerializeField] private bool isMoving = false; // Om hissen r�r sig eller v�ntar

    private bool isGoingDown = true; // Riktning (ned�t = true, upp�t = false)

    private void Start()
    {
        // B�rja med att placera hissen p� �versta v�ningen
        if (floorPositions.Length > 0)
        {
            transform.position = floorPositions[0].position;
            StartCoroutine(MovePlatform());
        }
        else
        {
            Debug.LogError("Inga v�ningspositioner �r inst�llda p� PlatformController!");
        }
    }

    private IEnumerator MovePlatform()
    {
        while (true) // Forts�tt i all o�ndlighet
        {
            // V�nta p� nuvarande v�ning
            isMoving = false;
            yield return new WaitForSeconds(waitTimeAtFloor);

            // Best�m n�sta v�ning
            isMoving = true;

            if (isGoingDown)
            {
                currentFloorIndex++;

                // Om vi n�tt botten, �ndra riktning
                if (currentFloorIndex >= floorPositions.Length - 1)
                {
                    currentFloorIndex = floorPositions.Length - 1;
                    isGoingDown = false;
                }
            }
            else // G�r upp�t
            {
                currentFloorIndex--;

                // Om vi n�tt toppen, �ndra riktning
                if (currentFloorIndex <= 0)
                {
                    currentFloorIndex = 0;
                    isGoingDown = true;

                    // Alternativ: Om du vill att den ska starta om fr�n toppen ist�llet f�r att v�nda
                    // currentFloorIndex = 0;
                    // isGoingDown = true;
                }
            }

            // Flytta hissen till n�sta v�ning
            Vector3 targetPosition = floorPositions[currentFloorIndex].position;

            while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
            {
                transform.position = Vector3.MoveTowards(
                    transform.position,
                    targetPosition,
                    moveSpeed * Time.deltaTime
                );
                yield return null; // V�nta till n�sta frame
            }

            // S�kerst�ll att vi �r exakt p� m�lpositionen
            transform.position = targetPosition;
        }
    }

    // Alternativ 1: Enklare version d�r hissen bara g�r uppifr�n och ner
    private IEnumerator MovePlatformSimple()
    {
        while (true)
        {
            // Starta fr�n toppen
            currentFloorIndex = 0;
            transform.position = floorPositions[currentFloorIndex].position;

            // Forts�tt ned�t genom alla v�ningar
            for (int i = 0; i < floorPositions.Length; i++)
            {
                // V�nta p� v�ningen
                isMoving = false;
                yield return new WaitForSeconds(waitTimeAtFloor);
                isMoving = true;

                // Om vi inte �r p� sista v�ningen, flytta ned�t
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

            // V�nta p� sista v�ningen
            yield return new WaitForSeconds(waitTimeAtFloor);

            // Teleportera tillbaka till toppen (eller du kan animera detta om du vill)
            transform.position = floorPositions[0].position;
        }
    }

    // F�r debugging: Rita linjer mellan v�ningarna i Scene-vyn
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