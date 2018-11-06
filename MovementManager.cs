using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class MovementManager : MonoBehaviour {

	[Header("Main Camera")]
	[SerializeField] [Tooltip("Finds at Runtime")] Camera cam;




    [Header("Status And Debug")]
    [SerializeField] bool showMoveMarkers = false;
	[SerializeField] [Tooltip("Select before run")] GameObject locMarker;
	[SerializeField] [Tooltip("Finds at Runtime")] NavMeshAgent navMeshAgent;
	[SerializeField] MouseManager mouseManager;
	[SerializeField] private int UnitIndex;
	[SerializeField] private float degreeFromCenter;
	[SerializeField] private float radiusFromCenter;
	[SerializeField] private Vector3 Center;
	[SerializeField] private Vector3[] GroupCoord;
	[SerializeField] [Tooltip("Finds at Runtime")] private int ground_layer;
	[SerializeField] private bool ActiveClosestPathfinding;
	
	
	// Use this for initialization
	void Start ()
	{
        if (!Debug.isDebugBuild) { showMoveMarkers = false; }
		ground_layer = LayerMask.GetMask("Ground");
		if(!(mouseManager = GetComponent<MouseManager>())){Debug.Log(transform.name + "could not find Mouse Manager on self"); }
		if (!(cam = Camera.main)) { Debug.Log(transform.name + " could not find Main Camera in Scene"); }
		radiusFromCenter = 2.5f;
	}

	public void Move()
	{
		if (mouseManager.PublicMoveableSelected.Count == 1)
		{
			MoveUnit();
		}
		else if(mouseManager.PublicMoveableSelected.Count > 1)
		{
			MoveUnits();
		}
		else
			print("moveableSelected is empty, Nothing to move.");
	}

    public void Move(GameObject thisUnit)
    {
        MoveUnit(thisUnit);
    }

    /// <summary>
    /// If only one unit in selection, then that unit moves to cursor point.
    /// </summary>
    void MoveUnit()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground_layer))
        {
            GameObject unit = mouseManager.PublicMoveableSelected.First();
            if (navMeshAgent = unit.GetComponent<NavMeshAgent>())
            {
                navMeshAgent.SetDestination(hit.point);
                if (showMoveMarkers) { Instantiate(locMarker, hit.point + (new Vector3(0f, 0.001f, 0f)), new Quaternion(0, 0, 0, 0)); }
            }
        }
    }

	public void StopUnit(GameObject unit)
	{
		if (unit.GetComponent<NavMeshAgent>())
		{
			unit.GetComponent<NavMeshAgent>().SetDestination(unit.transform.position);
		}
	}
	

    void MoveUnit(GameObject thisUnit)
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground_layer))
        {
            if (navMeshAgent = thisUnit.GetComponent<NavMeshAgent>())
            {
	            navMeshAgent.isStopped = false;
                navMeshAgent.SetDestination(hit.point);
                if (showMoveMarkers) { Instantiate(locMarker, hit.point + (new Vector3(0f, 0.001f, 0f)), new Quaternion(0, 0, 0, 0)); }
            }
        }
    }

    /// <summary>
    /// If more than one unit, a destination path is calculated around the cursor point for each unit in the selection list. 
    /// </summary>
    void MoveUnits()
	{
		List<GameObject> moveableUnits = GetComponent<MouseManager>().PublicMoveableSelected;
		List<GameObject> closestUnits = new List<GameObject>();
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		RaycastHit hit;


		if (Physics.Raycast(ray, out hit, Mathf.Infinity, ground_layer))
		{
			GroupCoord = CalculateGroupMove(moveableUnits, ref closestUnits, hit.point); // Call below method
			UnitIndex = 0;
			List<GameObject> ChosenList = new List<GameObject>();
			if (ActiveClosestPathfinding)
			{
				ChosenList = closestUnits;
			}
			else
			{
				ChosenList = moveableUnits;
				
			}
			foreach (GameObject unit in ChosenList)
			{
				CommitToDestination(unit, GroupCoord[UnitIndex]);
				UnitIndex++;
			}
		}
	}
	void CommitToDestination(GameObject unit, Vector3 destination)
	{
		NavMeshHit navHit;
		navMeshAgent = unit.GetComponent<NavMeshAgent>();

				navMeshAgent.SetDestination(GroupCoord[UnitIndex]);
                if (showMoveMarkers) { Instantiate(locMarker, GroupCoord[UnitIndex] + (new Vector3(0f, 0.001f, 0f)), new Quaternion(0, 0, 0, 0)); }
				
		//while(navMeshAgent.pathPending){}//wait for path to be calculated
		if (!NavMesh.SamplePosition(GroupCoord[UnitIndex],out navHit ,1.0f,NavMesh.AllAreas)) //If modified path is invalid, oh well
		{
			//set original point as new destination
			navMeshAgent.isStopped = false;
			navMeshAgent.SetDestination(Center);
			unit.GetComponent<AttributeManager>().targetLocation = destination;
		}
		else
		{
			navMeshAgent.isStopped = false;
			navMeshAgent.SetDestination(GroupCoord[UnitIndex]);
		}

	}
	Vector3[] CalculateGroupMove(List<GameObject> Units,ref List<GameObject> ClosestOrderUnits, Vector3 position)
	{
		Vector3[] UnitMoveCoord = new Vector3[25];
		ClosestOrderUnits = ClosestUnits(Units, position);

		//After reordering is complete, begin navigation
		UnitIndex = 0;
		degreeFromCenter = 0.0f;
		Center = position;
		float radian = 0.0f;
		float x_pos = 0;
		float z_pos = 0;
		foreach (var unit in  ClosestOrderUnits)
		{
			if (UnitIndex == 0)
			{
				UnitMoveCoord[UnitIndex] = position;
				UnitIndex++;
			}

			else if (UnitIndex < 9) // between 2-9 units selected 
			{
				degreeFromCenter += 45;
				radian = degreeFromCenter * Mathf.Deg2Rad;

				x_pos = Mathf.Cos(radian);
				if (x_pos < 0.1f && x_pos > -0.1f)
					x_pos = 0;
				z_pos = Mathf.Sin(radian);
				if (z_pos < 0.1f && z_pos > -0.1f)
					z_pos = 0;

				UnitMoveCoord[UnitIndex] = Center + new Vector3(x_pos, 0, z_pos) * radiusFromCenter;
				UnitIndex++;
			}
			else if(UnitIndex < 25)// between 10-25 units selected
			{
				degreeFromCenter += 22.5f;
				radian = degreeFromCenter * Mathf.Deg2Rad;
				x_pos = Mathf.Cos(radian);
				if (x_pos < 0.1f && x_pos > -0.1f)
					x_pos = 0;
				z_pos = Mathf.Sin(radian);
				if (z_pos < 0.1f && z_pos > -0.1f)
					z_pos = 0;
				float radiusFromCenter2 = radiusFromCenter * 2;

				UnitMoveCoord[UnitIndex] = Center + new Vector3(x_pos, 0, z_pos) * radiusFromCenter2;
				UnitIndex++;
			}
		}
		//returns the array of the new calculated Vector3 positions
		return UnitMoveCoord;
	}

	public List<Vector3> FormationCalculate(Vector3 position, int numberUnits)
	{
		float degree1=0f;
		UnitIndex = 0;
		degreeFromCenter = 0.0f;
		Center = position;
		float radian = 0.0f;
		float x_pos = 0;
		float z_pos = 0;
		var UnitFormationCoord = new List<Vector3>();

		switch (numberUnits)
		{
			case 1:
				UnitFormationCoord[0] = Center;
				break;
			case 2:
				degreeFromCenter = 180;
				break;
			case 3:
				degreeFromCenter = 120;
				break;
			case 4:
				degreeFromCenter = 90;
				break;
			case 5:
				degreeFromCenter = 72;
				break;
			case 6:
				degreeFromCenter = 60;
				break;
		}

		for(int i=0;i<numberUnits;i++)
		{
				degree1 += degreeFromCenter;
				radian = degree1* Mathf.Deg2Rad;
				x_pos = Mathf.Cos(radian);
				if (x_pos < 0.1f && x_pos > -0.1f)
					x_pos = 0;
				z_pos = Mathf.Sin(radian);
				if (z_pos < 0.1f && z_pos > -0.1f)
					z_pos = 0;
				UnitFormationCoord.Add(Center + new Vector3(x_pos, 0, z_pos) * radiusFromCenter);
		}

		return UnitFormationCoord;

	}
	

	List<GameObject> ClosestUnits(List<GameObject> UnitList, Vector3 Position)
	{
		List<GameObject> modifiedList = new List<GameObject>();
		bool closer = false;

			foreach (GameObject unit in UnitList)
			{
				//Iterates through currect selection list and reorders them based on their relative distance to the desired position

				if (modifiedList.Any()) //The closest unit will be first in the list
				{
					int lisNum = modifiedList.Count;
					for (int index=0; index<lisNum;index++)
					{
						GameObject inputtedUnit = modifiedList[index];
						closer = false;
						//Is the Vector distance (not path distance) of 1 unit less than or equal the current unit in the list?
						if (Vector3.Distance(Position, unit.transform.position) <=
						    Vector3.Distance(Position, inputtedUnit.transform.position))
						{
							modifiedList.Insert(index, unit);
							closer = true;
							break;
						}
					}
					if (!closer)
						modifiedList.Add(unit);
				}
				else //If no unit exists in list to compare with, insert as first unit.
				{
					modifiedList.Add(unit);
				}
			}

		return modifiedList;
	}


}
