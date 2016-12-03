using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;

public class TerrainView : MonoBehaviour
{
	public CubeView m_cubePrefab;

	List<CubeView> m_views = new List<CubeView>();
	int m_size;
	Terrain m_terrain;

	void Start () {

	}

	public void Init(GameStartState startState)
	{
		foreach (var v in m_views)
		{
			Destroy(v.gameObject);
		}
		m_views.Clear();

		m_size = startState.config.mapSize;
		m_terrain = new Terrain(startState.config);
		m_terrain.Generate(startState.config.seed);

		for (int i = 0; i < m_size; i++)
		{
			for (int j = 0; j < m_size; j++)
			{
				var cube = (CubeView)Instantiate(m_cubePrefab, new Vector3(j, m_terrain.map[m_terrain.GetIdx(j, i)], i), Quaternion.identity);
				cube.transform.parent = this.transform;
				m_views.Add(cube);

				//TODO pool
			}
		}

		foreach (var change in startState.change.terrainChange)
		{
			ApplyChange(change);
		}
	}

	public void ApplyChange(TerrainChange change)
	{
		var trans = m_views[change.idx].transform;
		var lp = trans.localPosition;
		lp.y = change.height;
		trans.localPosition = lp;
	}

	// Update is called once per frame
	void Update () {
	
	}
}
